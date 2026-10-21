using System;
using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.Core.Events;
using CinliMahzen.Core.Net;
using UnityEngine;

namespace CinliMahzen.Possession
{
    /// <summary>
    /// Authority side of possession (Teknik §6.2, TODO 1.7), plain C#. Wraps <see cref="PossessionArbiterCore"/> and turns its decisions
    /// into broadcasts every machine's <see cref="PossessionMirror"/> applies:
    /// <code>
    /// ReqPossess{obj}  ─► arbiter.TryPossess ─► PossessBegan{player, obj, enterEnd}   (or PossessDenied{player, obj, reason})
    /// Tick: Entering ends                     ─► PossessCompleted{player, obj}
    /// ReqUnpossess     ─► arbiter.Release     ─► PossessEnded{player, obj, Voluntary, spent}
    /// Tick: evil jinn in a lit room (not a lamp) for LitRoomEjectDelay ─► PossessEnded{.., RoomLit, ..}
    /// Kick (IKickable) ─► eject + possession lock for KickStun          ─► PossessEnded{.., Kicked, ..}
    /// Tick: single-use recovery ends          ─► PossessEnded{.., Spent, true}
    /// </code>
    /// The kick lock is kept here and ORed into the world's <see cref="IPossessionWorld.IsPossessionLocked"/>, so the arbiter refuses a
    /// stunned jinn with <see cref="PossessDenyReason.Stunned"/>. A new round (<see cref="RoundStartedEvt"/>) resets every object, lock and
    /// timer silently: the mirrors reset on the same event.
    /// <para>Charging / recovering (actions) are driven by the action runner of TODO 1.8 through <see cref="Arbiter"/>;
    /// <see cref="TransitionOccurred"/> reports the timer transitions this class does not handle itself.</para>
    /// </summary>
    public sealed class PossessionAuthority
    {
        private const string LogCategory = "Possession";

        private readonly INetBridge _net;
        private readonly PossessionArbiterCore _arbiter;
        private readonly LockingWorld _world;
        private readonly float _litEjectDelay;
        private readonly float _kickStun;
        private readonly List<PossessionTransitionInfo> _transitions = new List<PossessionTransitionInfo>(8);
        private readonly Dictionary<NetId, double> _litSince = new Dictionary<NetId, double>();
        private readonly List<NetId> _scratch = new List<NetId>(8);

        // Cached once so Register/Unregister/Subscribe never allocate a new delegate.
        private readonly Action<NetMsg> _onReqPossess;
        private readonly Action<NetMsg> _onReqUnpossess;
        private readonly Action<NetMsg> _onReqExorcise;
        private readonly CooldownTracker _exorciseCooldowns = new CooldownTracker();
        private ExorciseSettings _exorcise;
        private readonly Action<RoundStartedEvt> _onRoundStarted;
        private bool _attached;

        /// <param name="net">Required.</param>
        /// <param name="world">Required: roles, match phase, light and distances.</param>
        /// <param name="rules">PossessRange / tolerance / PossessTime / ReenterCooldown.</param>
        /// <param name="litEjectDelay"><c>LitRoomEjectDelay</c> (s).</param>
        /// <param name="kickStun"><c>KickStun</c> (s): how long a kicked jinn cannot possess anything.</param>
        public PossessionAuthority(INetBridge net, IPossessionWorld world, PossessionRules rules, float litEjectDelay, float kickStun)
        {
            _net = net ?? throw new ArgumentNullException(nameof(net));
            if (world == null)
                throw new ArgumentNullException(nameof(world));
            _world = new LockingWorld(world);
            _arbiter = new PossessionArbiterCore(_world, rules);
            _litEjectDelay = litEjectDelay;
            _kickStun = kickStun;

            _onReqPossess = OnReqPossess;
            _onReqUnpossess = OnReqUnpossess;
            _onReqExorcise = OnReqExorcise;
            _onRoundStarted = OnRoundStarted;
        }

        /// <summary>Builds the authority with the values of <paramref name="config"/>.</summary>
        public static PossessionAuthority FromConfig(INetBridge net, IPossessionWorld world, GameBalanceConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            var rules = new PossessionRules(config.PossessRange, config.NetRangeTolerance, config.PossessTime, config.ReenterCooldown);
            var authority = new PossessionAuthority(net, world, rules, config.LitRoomEjectDelay, config.KickStun);
            authority.Exorcise = new ExorciseSettings(config.ExorciseRange + config.NetRangeTolerance, config.ExorciseCooldown,
                config.ExorciseStun, config.ExorcisePossessLock);
            return authority;
        }

        /// <summary>The rules and per-object state machines (the action runner of TODO 1.8 drives Charging / Recovering through it).</summary>
        public PossessionArbiterCore Arbiter => _arbiter;

        /// <summary>The good jinn's exorcism (TODO 3.5). Default: range 0 = always out of range.</summary>
        public ExorciseSettings Exorcise
        {
            get => _exorcise;
            set => _exorcise = value;
        }

        /// <summary>Timer transitions other than EnterCompleted / SpentEjected (ChargeReady, RecoverFinished) — for the action runner.</summary>
        public event Action<PossessionTransitionInfo> TransitionOccurred;

        public void Attach()
        {
            if (_attached)
                return;
            _attached = true;
            _net.Register(MsgCode.ReqPossess, _onReqPossess);
            _net.Register(MsgCode.ReqUnpossess, _onReqUnpossess);
            _net.Register(MsgCode.ReqExorcise, _onReqExorcise);
            EventBus.Subscribe(_onRoundStarted);
        }

        public void Detach()
        {
            if (!_attached)
                return;
            _attached = false;
            _net.Unregister(MsgCode.ReqPossess, _onReqPossess);
            _net.Unregister(MsgCode.ReqUnpossess, _onReqUnpossess);
            _net.Unregister(MsgCode.ReqExorcise, _onReqExorcise);
            EventBus.Unsubscribe(_onRoundStarted);
        }

        // ---- registry ----

        /// <summary>Registers an object (every machine registers the same objects; only the authority's arbiter decides).</summary>
        public void Register(NetId obj, PossessableTraits traits)
        {
            _arbiter.Register(obj, traits);
        }

        /// <summary>
        /// Removes an object. On the authority a jinn that was inside is told with <c>PossessEnded(Forced)</c>.
        /// </summary>
        public void Unregister(NetId obj)
        {
            PlayerId occupant = _arbiter.Unregister(obj);
            _litSince.Remove(obj);
            if (occupant.IsValid && _net.IsAuthority)
                _net.Broadcast(PossessEndedMsg.Encode(occupant, obj, PossessEndReason.Forced, false));
        }

        /// <summary>True while <paramref name="player"/> is stunned by a kick (<see cref="INetBridge.Time"/> basis).</summary>
        public bool IsKickLocked(PlayerId player, double now)
        {
            return _world.IsKickLocked(player, now);
        }

        // ---- authority entry points ----

        /// <summary>
        /// Authority only. Possession request of <paramref name="who"/>: on success the object starts Entering and <c>PossessBegan</c> goes
        /// out; otherwise <c>PossessDenied</c> with the reason. Returns the arbiter's answer.
        /// </summary>
        public PossessDenyReason ProcessPossessAuthority(PlayerId who, NetId obj)
        {
            Debug.Assert(_net.IsAuthority, "ProcessPossessAuthority must only run on the authority");
            double now = _net.Time;
            PossessDenyReason reason = _arbiter.TryPossess(who, obj, now, out double enterEnd);
            if (reason != PossessDenyReason.None)
            {
                CMLog.Info(LogCategory, "Possess " + who + " -> " + obj + " denied: " + reason);
                _net.Broadcast(PossessDeniedMsg.Encode(who, obj, reason));
                return reason;
            }

            _net.Broadcast(PossessBeganMsg.Encode(who, obj, enterEnd));
            CMLog.Info(LogCategory, "Possess " + who + " -> " + obj + " began");
            return PossessDenyReason.None;
        }

        /// <summary>Authority only. <paramref name="who"/> leaves its object (voluntary). False when it was not in anything.</summary>
        public bool ProcessUnpossessAuthority(PlayerId who)
        {
            Debug.Assert(_net.IsAuthority, "ProcessUnpossessAuthority must only run on the authority");
            return ReleaseAndBroadcast(who, PossessEndReason.Voluntary);
        }

        /// <summary>
        /// Authority only. Throws whoever is in <paramref name="obj"/> out with <paramref name="reason"/> (exorcism, room lit, debug).
        /// Returns the ejected player or <see cref="PlayerId.None"/>.
        /// </summary>
        public PlayerId EjectAuthority(NetId obj, PossessEndReason reason)
        {
            Debug.Assert(_net.IsAuthority, "EjectAuthority must only run on the authority");
            PlayerId occupant = _arbiter.PossessorOf(obj);
            if (!occupant.IsValid || !_arbiter.IsPossessed(obj))
                return PlayerId.None;
            ReleaseAndBroadcast(occupant, reason);
            return occupant;
        }

        /// <summary>
        /// Authority only. The human kicked <paramref name="obj"/>: an occupant is ejected (<see cref="PossessEndReason.Kicked"/>) and cannot
        /// possess anything for <c>KickStun</c>. Returns the ejected player or <see cref="PlayerId.None"/> (empty object: nothing happens).
        /// </summary>
        public PlayerId KickAuthority(NetId obj, PlayerId by)
        {
            Debug.Assert(_net.IsAuthority, "KickAuthority must only run on the authority");
            PlayerId occupant = EjectAuthority(obj, PossessEndReason.Kicked);
            if (occupant.IsValid)
            {
                _world.LockUntil(occupant, _net.Time + _kickStun);
                _net.Broadcast(JinnStunnedMsg.Encode(occupant, _net.Time + _kickStun));
                CMLog.Info(LogCategory, by + " kicked " + obj + ": " + occupant + " stunned for " + _kickStun + " s");
            }
            return occupant;
        }

        /// <summary>
        /// Authority only (TODO 3.5). The good jinn <paramref name="by"/> exorcises <paramref name="obj"/>: it must be the good jinn, off cooldown,
        /// within range of an object an evil jinn is in. The evil jinn is ejected (<see cref="PossessEndReason.Exorcised"/>), stunned for
        /// <c>ExorciseStun</c> (JinnStunned) and cannot possess for <c>ExorcisePossessLock</c>; the good jinn's cooldown starts. Returns the
        /// ejected player or <see cref="PlayerId.None"/> (refused: nothing happens).
        /// </summary>
        public PlayerId ProcessExorciseAuthority(PlayerId by, NetId obj)
        {
            Debug.Assert(_net.IsAuthority, "ProcessExorciseAuthority must only run on the authority");
            double now = _net.Time;
            if (_world.GetRole(by) != Role.GoodJinn)
                return Refuse(by, obj, "only the good jinn exorcises");
            long key = CooldownTracker.Key(by.Value, 0);
            if (!_exorciseCooldowns.IsReady(key, now))
                return Refuse(by, obj, "cooldown");
            PlayerId occupant = _arbiter.PossessorOf(obj);
            if (!occupant.IsValid || !_arbiter.IsPossessed(obj) || _world.GetRole(occupant) != Role.EvilJinn)
                return Refuse(by, obj, "no evil jinn inside");
            if (!_world.TryGetDistance(by, obj, out float distance) || distance > _exorcise.Range)
                return Refuse(by, obj, "out of range");

            EjectAuthority(obj, PossessEndReason.Exorcised);
            _world.LockUntil(occupant, now + Math.Max(_exorcise.PossessLock, _exorcise.Stun));
            _net.Broadcast(JinnStunnedMsg.Encode(occupant, now + _exorcise.Stun));
            _exorciseCooldowns.Start(key, now, _exorcise.Cooldown);
            CMLog.Info(LogCategory, by + " exorcised " + occupant + " from " + obj);
            return occupant;
        }

        /// <summary>When <paramref name="goodJinn"/> may exorcise again (negative infinity: never used this round).</summary>
        public double ExorciseReadyAt(PlayerId goodJinn)
        {
            return _exorciseCooldowns.ReadyAt(CooldownTracker.Key(goodJinn.Value, 0));
        }

        private static PlayerId Refuse(PlayerId by, NetId obj, string why)
        {
            CMLog.Info(LogCategory, "Exorcise of " + obj + " by " + by + " refused: " + why);
            return PlayerId.None;
        }

        /// <summary>
        /// Authority only, once per frame. Advances the entering / recovery timers (broadcasting PossessCompleted and Spent endings) and
        /// ejects evil jinns whose object's room has been lit for <c>LitRoomEjectDelay</c>. Allocation-free once warm.
        /// </summary>
        public void TickAuthority(double now)
        {
            if (!_net.IsAuthority)
                return;

            _transitions.Clear();
            _arbiter.Tick(now, _transitions);
            for (int i = 0; i < _transitions.Count; i++)
            {
                PossessionTransitionInfo t = _transitions[i];
                switch (t.Kind)
                {
                    case PossessableTransition.EnterCompleted:
                        _net.Broadcast(PossessCompletedMsg.Encode(t.Player, t.Object));
                        break;
                    case PossessableTransition.SpentEjected:
                        _litSince.Remove(t.Object);
                        _net.Broadcast(PossessEndedMsg.Encode(t.Player, t.Object, PossessEndReason.Spent, true));
                        break;
                    default:
                        TransitionOccurred?.Invoke(t);
                        break;
                }
            }

            UpdateLitEjects(now);
        }

        // ---- internals ----

        private bool ReleaseAndBroadcast(PlayerId who, PossessEndReason reason)
        {
            if (!_arbiter.Release(who, _net.Time, out NetId obj))
                return false;

            _litSince.Remove(obj);
            bool spent = _arbiter.TryGetMachine(obj, out PossessableStateMachine sm) && sm.IsSpent;
            _net.Broadcast(PossessEndedMsg.Encode(who, obj, reason, spent));
            CMLog.Info(LogCategory, who + " left " + obj + " (" + reason + (spent ? ", spent" : string.Empty) + ")");
            return true;
        }

        private void UpdateLitEjects(double now)
        {
            _scratch.Clear();
            for (int i = 0; i < _arbiter.ObjectCount; i++)
            {
                NetId obj = _arbiter.GetObjectAt(i);
                PossessableStateMachine sm = _arbiter.GetMachineAt(i);
                bool exposed = sm.IsOccupied
                    && _world.GetRole(sm.Occupant) == Role.EvilJinn
                    && _arbiter.TryGetTraits(obj, out PossessableTraits traits) && !traits.IsLamp
                    && _world.LightOfObject(obj) == RoomLightState.On;

                if (!exposed)
                {
                    _litSince.Remove(obj);
                    continue;
                }
                if (!_litSince.TryGetValue(obj, out double since))
                {
                    _litSince[obj] = now;
                    continue;
                }
                if (now - since >= _litEjectDelay)
                    _scratch.Add(obj);
            }

            for (int i = 0; i < _scratch.Count; i++)
                EjectAuthority(_scratch[i], PossessEndReason.RoomLit);
        }

        private void OnReqPossess(NetMsg msg)
        {
            if (!_net.IsAuthority)
                return;
            if (!ReqPossessMsg.TryDecode(msg, out NetId obj))
            {
                CMLog.Warn(LogCategory, "Malformed ReqPossess from " + msg.Sender + " ignored");
                return;
            }
            ProcessPossessAuthority(msg.Sender, obj);
        }

        private void OnReqUnpossess(NetMsg msg)
        {
            if (!_net.IsAuthority)
                return;
            if (!ReqUnpossessMsg.TryDecode(msg))
            {
                CMLog.Warn(LogCategory, "Malformed ReqUnpossess from " + msg.Sender + " ignored");
                return;
            }
            ProcessUnpossessAuthority(msg.Sender);
        }

        private void OnReqExorcise(NetMsg msg)
        {
            if (!_net.IsAuthority)
                return;
            if (!ReqExorciseMsg.TryDecode(msg, out NetId obj))
            {
                CMLog.Warn(LogCategory, "Malformed ReqExorcise from " + msg.Sender + " ignored");
                return;
            }
            ProcessExorciseAuthority(msg.Sender, obj);
        }

        private void OnRoundStarted(RoundStartedEvt evt)
        {
            _exorciseCooldowns.Reset();
            _arbiter.ResetRound();
            _world.ClearLocks();
            _litSince.Clear();
        }

        /// <summary>The outside world plus the kick locks of this authority.</summary>
        private sealed class LockingWorld : IPossessionWorld
        {
            private readonly IPossessionWorld _inner;
            private readonly double[] _lockedUntil = new double[256];

            public LockingWorld(IPossessionWorld inner)
            {
                _inner = inner;
            }

            public bool IsPlaying => _inner.IsPlaying;
            public bool JinnsAwake => _inner.JinnsAwake;

            public Role GetRole(PlayerId player) => _inner.GetRole(player);
            public RoomLightState LightOfObject(NetId obj) => _inner.LightOfObject(obj);
            public bool TryGetDistance(PlayerId player, NetId obj, out float distance) => _inner.TryGetDistance(player, obj, out distance);

            public bool IsPossessionLocked(PlayerId player, double now)
            {
                return IsKickLocked(player, now) || _inner.IsPossessionLocked(player, now);
            }

            public bool IsKickLocked(PlayerId player, double now)
            {
                return now < _lockedUntil[player.Value];
            }

            public void LockUntil(PlayerId player, double until)
            {
                if (until > _lockedUntil[player.Value])
                    _lockedUntil[player.Value] = until;
            }

            public void ClearLocks()
            {
                Array.Clear(_lockedUntil, 0, _lockedUntil.Length);
            }
        }
    }
}
