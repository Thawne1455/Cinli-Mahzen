using System;
using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.Core.Events;
using CinliMahzen.Core.Net;
using UnityEngine;

namespace CinliMahzen.Player
{
    /// <summary>
    /// Authority side of the human's lantern pulse and kick (Oyun §5.1, Teknik §5.4, TODO 3.8), plain C#. Requests only reach the authority.
    /// <code>
    /// ReqLanternPulse{eye, dir} ─► human + strong lantern (Blessed Oil) + LanternPulseCooldown ─► IPossessionQuery.GetPossessedInCone(eye, dir,
    ///                              LanternPulseRange, LanternPulseAngle) ─► LanternPulsed{player, objects, now + LanternPulseDuration}
    /// ReqKick{eye, dir}         ─► human + KickCooldown ─► the nearest possessed object in front within KickRange ─► IKickable.OnKickedAuthority
    ///                              (Possession ejects and stuns the jinn) ─► KickResult{player, obj or 0}
    /// </code>
    /// The strong lantern follows <see cref="BlessedOilCollectedEvt"/>; a new round makes it weak again and clears the cooldowns.
    /// </summary>
    public sealed class HumanAbilityAuthority
    {
        private const string Category = "Human";
        private const float KickConeAngle = 45f;

        private readonly INetBridge _net;
        private readonly IPlayerRegistry _players;
        private readonly IEntityRegistry _entities;
        private readonly GameBalanceConfig _config;
        private readonly double[] _pulseReadyAt = new double[256];
        private readonly double[] _kickReadyAt = new double[256];
        private readonly bool[] _strong = new bool[256];
        private readonly List<NetId> _cone = new List<NetId>(8);
        private readonly Action<NetMsg> _onPulse;
        private readonly Action<NetMsg> _onKick;
        private readonly Action<BlessedOilCollectedEvt> _onOil;
        private readonly Action<RoundStartedEvt> _onRoundStarted;
        private bool _attached;

        /// <summary>Optional: where an entity is (for "nearest in front"); defaults to the entity registry's component transforms.</summary>
        public Func<NetId, Vector3?> EntityPosition { get; set; }

        public HumanAbilityAuthority(INetBridge net, IPlayerRegistry players, IEntityRegistry entities, GameBalanceConfig config)
        {
            _net = net ?? throw new ArgumentNullException(nameof(net));
            _players = players ?? throw new ArgumentNullException(nameof(players));
            _entities = entities;
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _onPulse = OnReqPulse;
            _onKick = OnReqKick;
            _onOil = e => { if (e.Collector.IsValid) _strong[e.Collector.Value] = true; };
            _onRoundStarted = e => Reset();
            EntityPosition = DefaultEntityPosition;
        }

        public bool IsStrong(PlayerId human)
        {
            return human.IsValid && _strong[human.Value];
        }

        public void Attach()
        {
            if (_attached)
                return;
            _attached = true;
            _net.Register(MsgCode.ReqLanternPulse, _onPulse);
            _net.Register(MsgCode.ReqKick, _onKick);
            EventBus.Subscribe(_onOil);
            EventBus.Subscribe(_onRoundStarted);
        }

        public void Detach()
        {
            if (!_attached)
                return;
            _attached = false;
            _net.Unregister(MsgCode.ReqLanternPulse, _onPulse);
            _net.Unregister(MsgCode.ReqKick, _onKick);
            EventBus.Unsubscribe(_onOil);
            EventBus.Unsubscribe(_onRoundStarted);
        }

        /// <summary>Authority only. Returns how many possessed objects the pulse lit (-1 = refused).</summary>
        public int ProcessPulseAuthority(PlayerId who, Vector3 eye, Vector3 direction)
        {
            Debug.Assert(_net.IsAuthority, "ProcessPulseAuthority must only run on the authority");
            double now = _net.Time;
            if (_players.GetRole(who) != Role.Human || !IsStrong(who) || now < _pulseReadyAt[who.Value])
                return -1;
            _pulseReadyAt[who.Value] = now + _config.LanternPulseCooldown;

            GameServices.Get<IPossessionQuery>().GetPossessedInCone(eye, direction, _config.LanternPulseRange, _config.LanternPulseAngle, _cone);
            var ids = new int[_cone.Count];
            for (int i = 0; i < ids.Length; i++)
                ids[i] = _cone[i].Value;
            _net.Broadcast(HumanActionMsgs.EncodePulsed(who, ids, now + _config.LanternPulseDuration));
            return ids.Length;
        }

        /// <summary>Authority only. Returns the kicked object (Invalid = nothing possessed in front, or refused).</summary>
        public NetId ProcessKickAuthority(PlayerId who, Vector3 eye, Vector3 direction)
        {
            Debug.Assert(_net.IsAuthority, "ProcessKickAuthority must only run on the authority");
            double now = _net.Time;
            if (_players.GetRole(who) != Role.Human || now < _kickReadyAt[who.Value])
                return NetId.Invalid;
            _kickReadyAt[who.Value] = now + _config.KickCooldown;

            GameServices.Get<IPossessionQuery>().GetPossessedInCone(eye, direction, _config.KickRange + _config.NetRangeTolerance, KickConeAngle, _cone);
            NetId target = Nearest(eye);
            if (target.IsValid && _entities != null && _entities.TryGet(target, out IKickable kickable))
                kickable.OnKickedAuthority(who);
            else
                target = NetId.Invalid;
            _net.Broadcast(HumanActionMsgs.EncodeKickResult(who, target));
            CMLog.Info(Category, who + " kicked " + (target.IsValid ? target.ToString() : "nothing"));
            return target;
        }

        private NetId Nearest(Vector3 eye)
        {
            NetId best = NetId.Invalid;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < _cone.Count; i++)
            {
                Vector3? at = EntityPosition(_cone[i]);
                float d = at.HasValue ? (at.Value - eye).sqrMagnitude : float.MaxValue - 1f;
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = _cone[i];
                }
            }
            return best;
        }

        private Vector3? DefaultEntityPosition(NetId id)
        {
            return _entities != null && _entities.TryGet(id, out Component c) && c != null ? c.transform.position : (Vector3?)null;
        }

        private void OnReqPulse(NetMsg msg)
        {
            if (_net.IsAuthority && HumanActionMsgs.TryDecodeAimRequest(msg, MsgCode.ReqLanternPulse, out Vector3 eye, out Vector3 dir))
                ProcessPulseAuthority(msg.Sender, eye, dir);
        }

        private void OnReqKick(NetMsg msg)
        {
            if (_net.IsAuthority && HumanActionMsgs.TryDecodeAimRequest(msg, MsgCode.ReqKick, out Vector3 eye, out Vector3 dir))
                ProcessKickAuthority(msg.Sender, eye, dir);
        }

        private void Reset()
        {
            Array.Clear(_pulseReadyAt, 0, _pulseReadyAt.Length);
            Array.Clear(_kickReadyAt, 0, _kickReadyAt.Length);
            Array.Clear(_strong, 0, _strong.Length);
        }
    }
}
