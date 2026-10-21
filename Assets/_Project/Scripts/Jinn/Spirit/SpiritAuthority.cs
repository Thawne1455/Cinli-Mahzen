using System;
using System.Collections.Generic;
using System.Globalization;
using CinliMahzen.Core;
using CinliMahzen.Core.Events;
using CinliMahzen.Core.Net;
using UnityEngine;

namespace CinliMahzen.Jinn
{
    /// <summary>
    /// Authority side of the reference focus and the Spirit Bump (Teknik §4.3, §6.1, §6.5), plain C#. Requests only reach the authority;
    /// a non-authority client ignores them. Built once by <c>SpiritBootstrap</c> (GameBootstrap hook), the way InteractionAuthority is.
    /// <list type="bullet">
    /// <item><c>ReqFocusReference{focusing}</c> → the sender must be the good jinn → <see cref="ReferenceFocusState"/> changes →
    /// <c>ReferenceFocusChanged{player, focusing}</c>. A request that changes nothing sends nothing.</item>
    /// <item><c>ReqSpiritBump{goodJinn}</c> → <see cref="SpiritBumpRules.Validate"/> (roles, focus flag, the sender's cooldown, distance
    /// within <c>SpiritBumpRange + NetRangeTolerance</c>, measured between the two pawns' positions) → the focus flag is cleared →
    /// <c>ReferenceFocusChanged{good, false}</c>, then <c>SpiritBumped{evil, good}</c>; the cooldown starts. A refused request is only logged.</item>
    /// </list>
    /// A new round (<see cref="RoundStartedEvt"/>) or new roles (<see cref="IPlayerRegistry.RolesChanged"/>) clear every focus flag — each one
    /// broadcast as "stopped" so the pawn mirrors stay in step — and every cooldown.
    /// Energy (<c>FocusReferenceCost</c>) joins the focus rules with TODO 3.5.
    /// </summary>
    public sealed class SpiritAuthority
    {
        private const string LogCategory = "Spirit";

        private readonly INetBridge _net;
        private readonly IPlayerRegistry _players;
        private readonly SpiritBumpSettings _settings;
        private readonly Func<PlayerId, Vector3?> _pawnPosition;
        private readonly ReferenceFocusState _focus = new ReferenceFocusState();
        private readonly double[] _bumpCooldownUntil = new double[PlayerId.None.Value];
        private readonly List<PlayerId> _cleared = new List<PlayerId>(4);

        // Cached once so Register/Unregister/Subscribe never allocate a new delegate.
        private readonly Action<NetMsg> _onReqFocus;
        private readonly Action<NetMsg> _onReqBump;
        private readonly Action _onRolesChanged;
        private readonly Action<RoundStartedEvt> _onRoundStarted;
        private bool _attached;

        /// <param name="pawnPosition">World position of a player's spirit pawn, or null when it has none. Only called when a bump request arrives.</param>
        public SpiritAuthority(INetBridge net, IPlayerRegistry players, SpiritBumpSettings settings, Func<PlayerId, Vector3?> pawnPosition)
        {
            _net = net ?? throw new ArgumentNullException(nameof(net));
            _players = players ?? throw new ArgumentNullException(nameof(players));
            _pawnPosition = pawnPosition ?? throw new ArgumentNullException(nameof(pawnPosition));
            _settings = settings;

            _onReqFocus = OnReqFocusReference;
            _onReqBump = OnReqSpiritBump;
            _onRolesChanged = OnRolesChanged;
            _onRoundStarted = OnRoundStarted;
        }

        /// <summary>Authoritative focus flags (read-only use: debug, tests).</summary>
        public ReferenceFocusState Focus => _focus;

        /// <summary>When <paramref name="evilJinn"/> may bump again (<see cref="INetBridge.Time"/> basis). 0 = never bumped / reset.</summary>
        public double GetBumpCooldownUntil(PlayerId evilJinn)
        {
            return evilJinn.IsValid ? _bumpCooldownUntil[evilJinn.Value] : 0d;
        }

        public void Attach()
        {
            if (_attached)
                return;
            _attached = true;
            _net.Register(MsgCode.ReqFocusReference, _onReqFocus);
            _net.Register(MsgCode.ReqSpiritBump, _onReqBump);
            _players.RolesChanged += _onRolesChanged;
            EventBus.Subscribe(_onRoundStarted);
        }

        public void Detach()
        {
            if (!_attached)
                return;
            _attached = false;
            _net.Unregister(MsgCode.ReqFocusReference, _onReqFocus);
            _net.Unregister(MsgCode.ReqSpiritBump, _onReqBump);
            _players.RolesChanged -= _onRolesChanged;
            EventBus.Unsubscribe(_onRoundStarted);
        }

        /// <summary>
        /// Authority only. Applies a focus request of <paramref name="who"/> and broadcasts <c>ReferenceFocusChanged</c> when the flag changes.
        /// Returns true when it changed; false when the sender is not the good jinn or the flag already had that value.
        /// </summary>
        public bool ProcessFocusRequestAuthority(PlayerId who, bool focusing)
        {
            Debug.Assert(_net.IsAuthority, "ProcessFocusRequestAuthority must only run on the authority");
            if (!_net.IsAuthority)
                return false;

            if (_players.GetRole(who) != Role.GoodJinn)
            {
                CMLog.Info(LogCategory, "ReqFocusReference from " + who + " rejected: only the good jinn can focus");
                return false;
            }
            // TODO 3.5: focusing costs FocusReferenceCost per second; an empty tank cannot start it.
            if (focusing && GameServices.Get<IJinnEnergyService>().Get(who) <= 0f)
            {
                CMLog.Info(LogCategory, "ReqFocusReference from " + who + " rejected: no energy");
                return false;
            }
            if (!_focus.TrySet(who, focusing))
                return false;

            _net.Broadcast(ReferenceFocusChangedMsg.Encode(who, focusing));
            CMLog.Info(LogCategory, who + " focus " + (focusing ? "on" : "off"));
            FocusChanged?.Invoke(who, focusing);
            return true;
        }

        /// <summary>Authority: a focus flag changed (the energy drain follows it, TODO 3.5).</summary>
        public event Action<PlayerId, bool> FocusChanged;

        /// <summary>Authority only. Ends <paramref name="goodJinn"/>'s focus (its energy ran out). No-op when it was not focusing.</summary>
        public void StopFocusAuthority(PlayerId goodJinn)
        {
            if (!_net.IsAuthority || !_focus.TrySet(goodJinn, false))
                return;
            _net.Broadcast(ReferenceFocusChangedMsg.Encode(goodJinn, false));
            CMLog.Info(LogCategory, goodJinn + " focus off (out of energy)");
            FocusChanged?.Invoke(goodJinn, false);
        }

        /// <summary>
        /// Authority only. Validates a Spirit Bump of <paramref name="evilJinn"/> on <paramref name="goodJinn"/>. When accepted
        /// (<see cref="SpiritBumpDenyReason.None"/>): the good jinn's focus flag is cleared, <c>ReferenceFocusChanged{good, false}</c> and
        /// <c>SpiritBumped</c> are broadcast and the evil jinn's cooldown starts. Otherwise nothing is sent and the reason is returned.
        /// </summary>
        public SpiritBumpDenyReason ProcessBumpRequestAuthority(PlayerId evilJinn, PlayerId goodJinn)
        {
            Debug.Assert(_net.IsAuthority, "ProcessBumpRequestAuthority must only run on the authority");
            if (!_net.IsAuthority)
                return SpiritBumpDenyReason.NotAuthority;

            SpiritBumpDenyReason reason = Resolve(evilJinn, goodJinn);
            if (reason != SpiritBumpDenyReason.None)
            {
                CMLog.Info(LogCategory, "ReqSpiritBump " + evilJinn + " -> " + goodJinn + " rejected: " + reason);
                return reason;
            }

            double now = _net.Time;
            _bumpCooldownUntil[evilJinn.Value] = now + _settings.Cooldown;
            _focus.TrySet(goodJinn, false);
            _net.Broadcast(ReferenceFocusChangedMsg.Encode(goodJinn, false));
            FocusChanged?.Invoke(goodJinn, false);
            _net.Broadcast(SpiritBumpedMsg.Encode(evilJinn, goodJinn));
            CMLog.Info(LogCategory, "SpiritBump " + evilJinn + " -> " + goodJinn + " accepted: focus cut, next bump of " + evilJinn + " after t=" + (now + _settings.Cooldown).ToString("0.00", CultureInfo.InvariantCulture));
            return SpiritBumpDenyReason.None;
        }

        private SpiritBumpDenyReason Resolve(PlayerId evilJinn, PlayerId goodJinn)
        {
            Role evilRole = _players.GetRole(evilJinn);
            Role goodRole = _players.GetRole(goodJinn);
            bool focusing = _focus.IsFocusing(goodJinn);
            double cooldownUntil = evilJinn.IsValid ? _bumpCooldownUntil[evilJinn.Value] : 0d;

            // Validate wants the distance up front. An unknown position is NaN, which fails the range check (the last rule); it is then reported as NoPawn.
            Vector3? evilPosition = _pawnPosition(evilJinn);
            Vector3? goodPosition = _pawnPosition(goodJinn);
            bool havePositions = evilPosition.HasValue && goodPosition.HasValue;
            float distance = havePositions ? Vector3.Distance(evilPosition.Value, goodPosition.Value) : float.NaN;

            SpiritBumpDenyReason reason = SpiritBumpRules.Validate(
                evilRole, goodRole, focusing, distance, _settings.Range, _settings.Tolerance, _net.Time, cooldownUntil);
            if (reason == SpiritBumpDenyReason.OutOfRange && !havePositions)
                return SpiritBumpDenyReason.NoPawn;
            return reason;
        }

        private void OnReqFocusReference(NetMsg msg)
        {
            if (!_net.IsAuthority)
                return;
            if (!ReqFocusReferenceMsg.TryDecode(msg, out bool focusing))
            {
                CMLog.Warn(LogCategory, "Malformed ReqFocusReference ignored");
                return;
            }
            ProcessFocusRequestAuthority(msg.Sender, focusing);
        }

        private void OnReqSpiritBump(NetMsg msg)
        {
            if (!_net.IsAuthority)
                return;
            if (!ReqSpiritBumpMsg.TryDecode(msg, out PlayerId goodJinn))
            {
                CMLog.Warn(LogCategory, "Malformed ReqSpiritBump ignored");
                return;
            }
            ProcessBumpRequestAuthority(msg.Sender, goodJinn);
        }

        private void OnRolesChanged()
        {
            ResetForRound();
        }

        private void OnRoundStarted(RoundStartedEvt evt)
        {
            ResetForRound();
        }

        // Pawns are respawned for a new round, but a pawn that survives must not keep a stale focus mirror: every cleared flag is broadcast.
        private void ResetForRound()
        {
            Array.Clear(_bumpCooldownUntil, 0, _bumpCooldownUntil.Length);

            _cleared.Clear();
            _focus.ClearAll(_cleared);
            if (!_net.IsAuthority)
                return;
            for (int i = 0; i < _cleared.Count; i++)
            {
                _net.Broadcast(ReferenceFocusChangedMsg.Encode(_cleared[i], false));
                FocusChanged?.Invoke(_cleared[i], false);
            }
        }
    }
}
