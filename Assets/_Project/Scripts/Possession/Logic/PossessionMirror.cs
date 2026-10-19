using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using CinliMahzen.Core;
using CinliMahzen.Core.Events;
using CinliMahzen.Core.Net;

namespace CinliMahzen.Possession
{
    /// <summary>
    /// Every machine's view of who is inside what (Teknik §6.2, TODO 1.7), plain C#. Applies the authority's broadcasts
    /// (<c>PossessBegan</c>, <c>PossessCompleted</c>, <c>PossessEnded</c>) and publishes <see cref="PossessionChangedEvt"/> for each change
    /// (input map, camera, HUD and the object's own telegraph listen to it). <c>PossessDenied</c> only raises <see cref="Denied"/>.
    /// A new round (<see cref="RoundStartedEvt"/>) clears everything (Spent objects become usable again); every jinn still inside gets a Free event.
    /// The action runner (TODO 1.8) moves Lurking ⇄ Charging ⇄ Recovering with <see cref="SetPhase"/>.
    /// </summary>
    public sealed class PossessionMirror
    {
        private readonly INetBridge _net;
        private readonly Dictionary<NetId, Entry> _objects = new Dictionary<NetId, Entry>();
        private readonly Dictionary<PlayerId, NetId> _objectByPlayer = new Dictionary<PlayerId, NetId>();
        private readonly List<PossessionChangedEvt> _released = new List<PossessionChangedEvt>(4);

        private readonly Action<NetMsg> _onBegan;
        private readonly Action<NetMsg> _onCompleted;
        private readonly Action<NetMsg> _onEnded;
        private readonly Action<NetMsg> _onDenied;
        private readonly Action<RoundStartedEvt> _onRoundStarted;
        private bool _attached;

        private struct Entry
        {
            public PlayerId Occupant;
            public PossessionPhase Phase;
            public double EnterEndTime;
        }

        public PossessionMirror(INetBridge net)
        {
            _net = net ?? throw new ArgumentNullException(nameof(net));
            _onBegan = OnBegan;
            _onCompleted = OnCompleted;
            _onEnded = OnEnded;
            _onDenied = OnDenied;
            _onRoundStarted = OnRoundStarted;
        }

        /// <summary>A possession request was refused (raised on every machine; compare the player with the local one).</summary>
        public event Action<PlayerId, NetId, PossessDenyReason> Denied;

        public void Attach()
        {
            if (_attached)
                return;
            _attached = true;
            _net.Register(MsgCode.PossessBegan, _onBegan);
            _net.Register(MsgCode.PossessCompleted, _onCompleted);
            _net.Register(MsgCode.PossessEnded, _onEnded);
            _net.Register(MsgCode.PossessDenied, _onDenied);
            EventBus.Subscribe(_onRoundStarted);
        }

        public void Detach()
        {
            if (!_attached)
                return;
            _attached = false;
            _net.Unregister(MsgCode.PossessBegan, _onBegan);
            _net.Unregister(MsgCode.PossessCompleted, _onCompleted);
            _net.Unregister(MsgCode.PossessEnded, _onEnded);
            _net.Unregister(MsgCode.PossessDenied, _onDenied);
            EventBus.Unsubscribe(_onRoundStarted);
        }

        // ---- queries ----

        /// <summary>Phase of <paramref name="obj"/> on this machine (unknown objects are Free).</summary>
        public PossessionPhase PhaseOf(NetId obj)
        {
            return _objects.TryGetValue(obj, out Entry e) ? e.Phase : PossessionPhase.Free;
        }

        /// <summary>True while a jinn is entering or inside <paramref name="obj"/>.</summary>
        public bool IsPossessed(NetId obj)
        {
            return IsOccupied(PhaseOf(obj));
        }

        public PlayerId PossessorOf(NetId obj)
        {
            return _objects.TryGetValue(obj, out Entry e) && IsOccupied(e.Phase) ? e.Occupant : PlayerId.None;
        }

        /// <summary>The object <paramref name="player"/> is entering or inside.</summary>
        public bool TryGetObjectOf(PlayerId player, out NetId obj)
        {
            return _objectByPlayer.TryGetValue(player, out obj);
        }

        /// <summary>Net.Time at which entering ends (0 when the object is not Entering).</summary>
        public double EnterEndTimeOf(NetId obj)
        {
            return _objects.TryGetValue(obj, out Entry e) && e.Phase == PossessionPhase.Entering ? e.EnterEndTime : 0d;
        }

        public static bool IsOccupied(PossessionPhase phase)
        {
            return phase == PossessionPhase.Entering || phase == PossessionPhase.Lurking
                || phase == PossessionPhase.Charging || phase == PossessionPhase.Recovering;
        }

        /// <summary>True for the phases in which the jinn is fully inside (camera orbit, possessed input).</summary>
        public static bool IsInside(PossessionPhase phase)
        {
            return phase == PossessionPhase.Lurking || phase == PossessionPhase.Charging || phase == PossessionPhase.Recovering;
        }

        // ---- mutation ----

        /// <summary>
        /// Moves an occupied object between the inside phases (Lurking, Charging, Recovering) and publishes the change. Used by the action
        /// runner (TODO 1.8) on every machine. False when the object is not occupied or the phase is not an inside phase.
        /// </summary>
        public bool SetPhase(NetId obj, PossessionPhase phase)
        {
            if (!IsInside(phase) || !_objects.TryGetValue(obj, out Entry e) || !IsInside(e.Phase))
                return false;
            if (e.Phase == phase)
                return true;
            e.Phase = phase;
            _objects[obj] = e;
            EventBus.Publish(new PossessionChangedEvt(e.Occupant, obj, phase));
            return true;
        }

        /// <summary>Forgets every possession (new round, scene unload). No events.</summary>
        public void Clear()
        {
            _objects.Clear();
            _objectByPlayer.Clear();
        }

        // ---- handlers ----

        private void OnBegan(NetMsg msg)
        {
            if (!PossessBeganMsg.TryDecode(msg, out PlayerId player, out NetId obj, out double enterEnd))
            {
                CMLog.Warn("Possession", "Malformed PossessBegan ignored");
                return;
            }
            Set(player, obj, PossessionPhase.Entering, enterEnd);
        }

        private void OnCompleted(NetMsg msg)
        {
            if (!PossessCompletedMsg.TryDecode(msg, out PlayerId player, out NetId obj))
            {
                CMLog.Warn("Possession", "Malformed PossessCompleted ignored");
                return;
            }
            Set(player, obj, PossessionPhase.Lurking, 0d);
        }

        private void OnEnded(NetMsg msg)
        {
            if (!PossessEndedMsg.TryDecode(msg, out PlayerId player, out NetId obj, out PossessEndReason _, out bool spent))
            {
                CMLog.Warn("Possession", "Malformed PossessEnded ignored");
                return;
            }

            if (_objectByPlayer.TryGetValue(player, out NetId current) && current == obj)
                _objectByPlayer.Remove(player);

            PossessionPhase phase = spent ? PossessionPhase.Spent : PossessionPhase.Free;
            _objects[obj] = new Entry { Occupant = PlayerId.None, Phase = phase, EnterEndTime = 0d };
            EventBus.Publish(new PossessionChangedEvt(player, obj, phase));
        }

        private void OnDenied(NetMsg msg)
        {
            if (!PossessDeniedMsg.TryDecode(msg, out PlayerId player, out NetId obj, out PossessDenyReason reason))
            {
                CMLog.Warn("Possession", "Malformed PossessDenied ignored");
                return;
            }
            if (player == _net.LocalPlayer)
                CMLog.Info("Possession", "Possess " + obj + " refused: " + reason);
            Denied?.Invoke(player, obj, reason);
        }

        private void OnRoundStarted(RoundStartedEvt evt)
        {
            // Listeners (input map, camera, telegraph) must see every jinn come out: publish Free for each occupied object, then forget.
            _released.Clear();
            foreach (KeyValuePair<PlayerId, NetId> pair in _objectByPlayer)
                _released.Add(new PossessionChangedEvt(pair.Key, pair.Value, PossessionPhase.Free));
            Clear();
            for (int i = 0; i < _released.Count; i++)
                EventBus.Publish(_released[i]);
        }

        private void Set(PlayerId player, NetId obj, PossessionPhase phase, double enterEnd)
        {
            // A player is in one object at a time; a stale entry elsewhere (lost message) is dropped.
            if (_objectByPlayer.TryGetValue(player, out NetId previous) && previous != obj)
                _objects[previous] = new Entry { Occupant = PlayerId.None, Phase = PossessionPhase.Free, EnterEndTime = 0d };

            _objectByPlayer[player] = obj;
            _objects[obj] = new Entry { Occupant = player, Phase = phase, EnterEndTime = enterEnd };
            EventBus.Publish(new PossessionChangedEvt(player, obj, phase));
        }

        /// <summary>JSON array for Dump State ("possession"): every object that is not Free.</summary>
        public string BuildDebugJson()
        {
            var sb = new StringBuilder("[");
            bool first = true;
            foreach (KeyValuePair<NetId, Entry> pair in _objects)
            {
                if (pair.Value.Phase == PossessionPhase.Free)
                    continue;
                if (!first)
                    sb.Append(',');
                first = false;
                sb.Append("{\"obj\":").Append(pair.Key.Value.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"phase\":\"").Append(pair.Value.Phase.ToString())
                    .Append("\",\"by\":\"").Append(pair.Value.Occupant.ToString()).Append("\"}");
            }
            return sb.Append(']').ToString();
        }
    }
}
