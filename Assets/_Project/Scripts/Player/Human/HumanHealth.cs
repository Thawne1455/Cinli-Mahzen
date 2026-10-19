using System;
using System.Globalization;
using CinliMahzen.Core;
using CinliMahzen.Core.Events;
using CinliMahzen.Core.Net;
using UnityEngine;

namespace CinliMahzen.Player
{
    /// <summary>
    /// Human HP, fainting and Lemon Cologne revive (Teknik §5.3) — the MonoBehaviour around <see cref="HumanHealthCore"/>.
    /// Sits on P_Human next to <see cref="HumanController"/>; the HumanHitbox trigger child (layer 9) finds it with
    /// <c>GetComponentInParent&lt;IDamageable&gt;()</c>.
    ///
    /// <para>Flow (Request → authority → broadcast → everyone applies):
    /// <see cref="ApplyDamageAuthority"/>, <c>Update</c> (natural revive) and <see cref="ReviveWithCologneAuthority"/> run the rules in
    /// <see cref="HumanHealthCore"/> on the authority and only broadcast <c>HumanDamaged</c> / <c>HumanFainted</c> / <c>HumanRevived</c>.
    /// The message handlers run on every client, the authority included: they update the <see cref="HumanHealthMirror"/>,
    /// put the body into / out of <see cref="HumanMotorState.Fainted"/> (and the local camera into the faint view),
    /// start knockdowns, and publish <see cref="HumanDamagedEvt"/> / <see cref="HumanFaintedEvt"/> / <see cref="HumanRevivedEvt"/>.</para>
    ///
    /// <para>Also the <see cref="IHumanStatusQuery"/> implementation: registered in OnEnable, the stub is back after OnDisable.</para>
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(HumanController))]
    public sealed class HumanHealth : MonoBehaviour, IDamageable, IHumanStatusQuery
    {
        private const string DebugStateKey = "health";

        private static HumanHealth s_debugOwner;

        private HumanController _human;
        private INetBridge _net;
        private GameBalanceConfig _config;
        private HumanHealthCore _core;
        private HumanHealthMirror _mirror;

        // Cached once so Register/Unregister/Subscribe never allocate a new delegate.
        private Action<NetMsg> _onDamaged;
        private Action<NetMsg> _onFainted;
        private Action<NetMsg> _onRevived;
        private Action<RoundStartedEvt> _onRoundStarted;

        /// <summary>The pawn's id: damage requests address the human by it.</summary>
        public NetId NetId => _human != null ? _human.NetId : NetId.Invalid;

        /// <summary>
        /// Debug cheat (F5 / <c>CinliMahzen/Debug/Toggle Immortal Human</c>): the authority drops every hit before the health rules
        /// run, so the human can neither be hurt nor faint. Not part of the game state; never set it outside debug tools.
        /// </summary>
        public bool Immortal { get; set; }

        public int MaxHp => _mirror != null ? _mirror.MaxHp : 0;

        /// <summary>Mirrored HP (every client). 0 while fainted.</summary>
        public int Hp => _mirror != null ? _mirror.Hp : 0;

        /// <summary>Mirrored faint count this round (every client).</summary>
        public int Faints => _mirror != null ? _mirror.Faints : 0;

        // ---- IHumanStatusQuery ----

        public Vector3 Position => transform.position;

        /// <summary>Mirrored: true from the HumanFainted message until the HumanRevived message.</summary>
        public bool IsFainted => _mirror != null && _mirror.IsFainted;

        /// <summary>Always false until the fixed lantern arrives (TODO 3.8 HumanLantern): it will report the Blessed Oil strong mode here.</summary>
        public bool IsBlessedLanternActive => false;

        public bool IsInLanternSafeZone(Vector3 worldPos)
        {
            if (!IsBlessedLanternActive || _config == null)
                return false;
            float radius = _config.LanternSafeZoneRadius;
            return (worldPos - transform.position).sqrMagnitude <= radius * radius;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_debugOwner = null;
        }

        private void Awake()
        {
            _human = GetComponent<HumanController>();
            _onDamaged = OnDamagedMsg;
            _onFainted = OnFaintedMsg;
            _onRevived = OnRevivedMsg;
            _onRoundStarted = OnRoundStarted;
        }

        private void OnEnable()
        {
            _net = GameServices.Net;
            _config = GameServices.Config;
            if (_net == null || _config == null)
            {
                CMLog.Error("Health", name + ": GameServices.Net / Config missing (is the bootstrap running?) — health is inert");
                _net = null;
                return;
            }

            // The core only decides on the authority; every machine builds one so a host change finds it ready.
            _core = new HumanHealthCore(HumanHealthSettings.FromConfig(_config));
            _mirror = new HumanHealthMirror(_config.HumanMaxHp);

            _net.Register(MsgCode.HumanDamaged, _onDamaged);
            _net.Register(MsgCode.HumanFainted, _onFainted);
            _net.Register(MsgCode.HumanRevived, _onRevived);
            EventBus.Subscribe(_onRoundStarted);

            GameServices.Register<IHumanStatusQuery>(this);
            s_debugOwner = this;
            DebugState.Register(DebugStateKey, BuildDebugJson);
        }

        private void OnDisable()
        {
            if (_net != null)
            {
                _net.Unregister(MsgCode.HumanDamaged, _onDamaged);
                _net.Unregister(MsgCode.HumanFainted, _onFainted);
                _net.Unregister(MsgCode.HumanRevived, _onRevived);
                _net = null;
            }
            EventBus.Unsubscribe(_onRoundStarted);
            GameServices.Unregister<IHumanStatusQuery>(this);
            if (s_debugOwner == this)
            {
                DebugState.Unregister(DebugStateKey);
                s_debugOwner = null;
            }
        }

        private void Update()
        {
            if (_core == null || _net == null || !_net.IsAuthority)
                return;

            // Natural revive once FaintDuration is over. Offline this reaches the handlers in the same frame.
            if (_core.Tick(_net.Time))
                _net.Broadcast(HumanRevivedMsg.Encode(false, _core.Hp));
        }

        // ---- Authority ----

        /// <summary>
        /// Authority only (Teknik §4.5). Runs <see cref="HumanHealthCore.ApplyDamage"/> and broadcasts the outcome: an applied hit sends
        /// HumanDamaged; HP 0 also sends HumanFainted (the faint that reaches FaintsToLose too — the score still counts it).
        /// Ignored hits (invulnerable, already fainted, <see cref="Immortal"/>) send nothing.
        /// </summary>
        public void ApplyDamageAuthority(in DamageInfo info)
        {
            INetBridge net = _net;
            Debug.Assert(net != null && net.IsAuthority, "ApplyDamageAuthority must only run on the authority");
            if (net == null || !net.IsAuthority || _core == null)
                return;

            if (Immortal)
            {
                CMLog.Info("Health", "Hit '" + info.CauseId + "' ignored: Immortal Human is on");
                return;
            }

            DamageResult result = _core.ApplyDamage(info, net.Time);
            if (!result.IsApplied)
                return;

            net.Broadcast(HumanDamagedMsg.Encode(info, result.HpAfter));
            if (result.Outcome != DamageOutcome.Fainted && result.Outcome != DamageOutcome.Lost)
                return;

            // The MatchController ends the round from HumanFaintedEvt.FaintCount >= FaintsToLose (the Lost faint included), in the same frame.
            net.Broadcast(HumanFaintedMsg.Encode(info, result.FaintCount));

            // The quest item drop on fainting is done by Objectives' ObjectiveFlowCore, which listens to the same HumanFaintedEvt (TODO 2.7).
        }

        /// <summary>
        /// Authority only. Lemon Cologne (Teknik §5.3): wakes a fainted, not-lost human at once with full HP and broadcasts
        /// HumanRevived{byCologne=true}. Returns false (nothing sent) when the human is not fainted or already lost.
        /// Range / item checks belong to the caller (the Cologne action, TODO 1.8); this method does not look at distance.
        /// </summary>
        public bool ReviveWithCologneAuthority()
        {
            INetBridge net = _net;
            Debug.Assert(net != null && net.IsAuthority, "ReviveWithCologneAuthority must only run on the authority");
            if (net == null || !net.IsAuthority || _core == null)
                return false;

            if (!_core.TryReviveWithCologne())
                return false;

            net.Broadcast(HumanRevivedMsg.Encode(true, _core.Hp));
            return true;
        }

        // ---- Round reset ----

        /// <summary>
        /// New round: full HP, no faints, standing. Runs on every client when <see cref="RoundStartedEvt"/> arrives (Match broadcasts
        /// the round setup, so this needs no message of its own); the authority also resets the rules core. Safe to call directly.
        /// Publishes no HumanRevivedEvt: a round start is not a revive.
        /// </summary>
        public void ResetForRound()
        {
            if (_mirror == null)
                return;

            _mirror.Reset();
            if (_core != null && _net != null && _net.IsAuthority)
                _core.ResetForRound();
            _human.SetFainted(false);
        }

        private void OnRoundStarted(RoundStartedEvt evt)
        {
            ResetForRound();
        }

        // ---- Handlers (every client) ----

        private void OnDamagedMsg(NetMsg msg)
        {
            if (!HumanDamagedMsg.TryDecode(msg, out DamageInfo info, out int hpAfter))
            {
                CMLog.Warn("Health", "Malformed HumanDamaged message ignored");
                return;
            }

            _mirror.ApplyDamaged(hpAfter);

            // Fainting wins over a knockdown, so only a hit that left HP knocks the human down.
            if (hpAfter > 0 && (info.Flags & DamageFlags.Knockdown) != 0)
            {
                float seconds = info.KnockdownTime > 0f ? info.KnockdownTime : _config.KnockdownTime;
                _human.Motor.KnockDown(_net.Time + seconds);
            }

            EventBus.Publish(new HumanDamagedEvt(info, hpAfter));
        }

        private void OnFaintedMsg(NetMsg msg)
        {
            if (!HumanFaintedMsg.TryDecode(msg, out DamageInfo info, out int faintCount))
            {
                CMLog.Warn("Health", "Malformed HumanFainted message ignored");
                return;
            }

            _mirror.ApplyFainted(faintCount);
            _human.SetFainted(true);
            EventBus.Publish(new HumanFaintedEvt(info, faintCount));
        }

        private void OnRevivedMsg(NetMsg msg)
        {
            if (!HumanRevivedMsg.TryDecode(msg, out bool byCologne, out int hp))
            {
                CMLog.Warn("Health", "Malformed HumanRevived message ignored");
                return;
            }

            if (!_mirror.ApplyRevived(hp))
                CMLog.Warn("Health", "HumanRevived arrived while not fainted (missed HumanFainted?) - authority state applied anyway");
            _human.SetFainted(false);
            EventBus.Publish(new HumanRevivedEvt(byCologne));
        }

        // ---- Debug ----

        private string BuildDebugJson()
        {
            if (_mirror == null)
                return "null";

            CultureInfo ci = CultureInfo.InvariantCulture;
            return "{\"hp\":" + _mirror.Hp.ToString(ci)
                + ",\"maxHp\":" + _mirror.MaxHp.ToString(ci)
                + ",\"faints\":" + _mirror.Faints.ToString(ci)
                + ",\"fainted\":" + (_mirror.IsFainted ? "true" : "false")
                + ",\"immortal\":" + (Immortal ? "true" : "false")
                + "}";
        }
    }
}
