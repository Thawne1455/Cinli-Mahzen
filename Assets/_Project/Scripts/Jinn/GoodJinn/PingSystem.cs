using System;
using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.Core.Events;
using CinliMahzen.Core.Net;
using UnityEngine;

namespace CinliMahzen.Jinn
{
    /// <summary>
    /// The good jinn's ping (Oyun §5.2, TODO 3.5), plain C#. Authority: <c>ReqPing{point}</c> from the good jinn, at most every
    /// <c>PingCooldown</c> → <c>PingPlaced{player, point, now + PingDuration}</c>. Every machine keeps the live pings (at most
    /// <c>PingMaxActive</c>; a new one pushes the oldest out) and tells the scene through <see cref="PingAdded"/> / <see cref="PingRemoved"/>.
    /// Expired pings are dropped by <see cref="Tick"/>. A new round clears them.
    /// </summary>
    public sealed class PingSystem
    {
        public readonly struct Ping
        {
            public readonly int Id;
            public readonly PlayerId By;
            public readonly Vector3 Point;
            public readonly double Until;

            public Ping(int id, PlayerId by, Vector3 point, double until)
            {
                Id = id;
                By = by;
                Point = point;
                Until = until;
            }
        }

        private readonly INetBridge _net;
        private readonly IPlayerRegistry _players;
        private readonly float _duration;
        private readonly float _cooldown;
        private readonly int _maxActive;
        private readonly List<Ping> _active = new List<Ping>(4);
        private readonly double[] _readyAt = new double[256];
        private readonly Action<NetMsg> _onReq;
        private readonly Action<NetMsg> _onPlaced;
        private readonly Action<RoundStartedEvt> _onRoundStarted;
        private int _nextId = 1;
        private bool _attached;

        public PingSystem(INetBridge net, IPlayerRegistry players, float duration, float cooldown, int maxActive)
        {
            _net = net ?? throw new ArgumentNullException(nameof(net));
            _players = players ?? throw new ArgumentNullException(nameof(players));
            _duration = duration;
            _cooldown = cooldown;
            _maxActive = Math.Max(1, maxActive);
            _onReq = OnReqPing;
            _onPlaced = OnPingPlaced;
            _onRoundStarted = e => Clear();
        }

        public static PingSystem FromConfig(INetBridge net, IPlayerRegistry players, GameBalanceConfig config)
        {
            return new PingSystem(net, players, config.PingDuration, config.PingCooldown, config.PingMaxActive);
        }

        public event Action<Ping> PingAdded;
        public event Action<int> PingRemoved;

        public IReadOnlyList<Ping> Active => _active;

        public void Attach()
        {
            if (_attached)
                return;
            _attached = true;
            _net.Register(MsgCode.ReqPing, _onReq);
            _net.Register(MsgCode.PingPlaced, _onPlaced);
            EventBus.Subscribe(_onRoundStarted);
        }

        public void Detach()
        {
            if (!_attached)
                return;
            _attached = false;
            _net.Unregister(MsgCode.ReqPing, _onReq);
            _net.Unregister(MsgCode.PingPlaced, _onPlaced);
            EventBus.Unsubscribe(_onRoundStarted);
        }

        /// <summary>Authority only. Places a ping of <paramref name="who"/> at <paramref name="point"/>. False: not the good jinn, or on cooldown.</summary>
        public bool ProcessPingAuthority(PlayerId who, Vector3 point)
        {
            Debug.Assert(_net.IsAuthority, "ProcessPingAuthority must only run on the authority");
            if (!who.IsValid || _players.GetRole(who) != Role.GoodJinn)
                return false;
            double now = _net.Time;
            if (now < _readyAt[who.Value])
                return false;
            _readyAt[who.Value] = now + _cooldown;
            _net.Broadcast(PingMsg.EncodePlaced(who, point, now + _duration));
            return true;
        }

        /// <summary>Every machine, every frame (any clock): drops expired pings.</summary>
        public void Tick(double now)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (now >= _active[i].Until)
                {
                    int id = _active[i].Id;
                    _active.RemoveAt(i);
                    PingRemoved?.Invoke(id);
                }
            }
        }

        private void OnReqPing(NetMsg msg)
        {
            if (!_net.IsAuthority || !PingMsg.TryDecodeRequest(msg, out Vector3 point))
                return;
            ProcessPingAuthority(msg.Sender, point);
        }

        private void OnPingPlaced(NetMsg msg)
        {
            if (!PingMsg.TryDecodePlaced(msg, out PlayerId by, out Vector3 point, out double until))
            {
                CMLog.Warn("Ping", "Malformed PingPlaced ignored");
                return;
            }
            while (_active.Count >= _maxActive)
            {
                int oldest = _active[0].Id;
                _active.RemoveAt(0);
                PingRemoved?.Invoke(oldest);
            }
            var ping = new Ping(_nextId++, by, point, until);
            _active.Add(ping);
            PingAdded?.Invoke(ping);
        }

        private void Clear()
        {
            Array.Clear(_readyAt, 0, _readyAt.Length);
            for (int i = _active.Count - 1; i >= 0; i--)
                PingRemoved?.Invoke(_active[i].Id);
            _active.Clear();
        }
    }
}
