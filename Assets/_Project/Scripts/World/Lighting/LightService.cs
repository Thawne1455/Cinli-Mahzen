using System;
using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.Core.Events;
using CinliMahzen.Core.Net;
using UnityEngine;

namespace CinliMahzen.World
{
    /// <summary>
    /// The house lights (Oyun §7, Teknik §4.8, TODO 3.4), plain C#. The real <see cref="ILightService"/>: lamps are the house's LampMarkers,
    /// rooms are lit while any of their lamps is On (<see cref="RoomService.SetRoomLight"/>, so <see cref="IRoomService.LightOf"/> follows and the
    /// possession / sabotage rules see it).
    /// <code>
    /// SetLampAuthority(lamp, state)  (evil jinn's burst, good jinn's light / repair) ─► LightsChanged{lamp, state}
    /// ToggleRoomAuthority(room)      (the human's wall switch)                      ─► LightsChanged for every lamp that flips
    /// LightsChanged (every machine)  ─► lamp state, room light, RoomLightChangedEvt, <see cref="LampChanged"/> (visuals)
    /// </code>
    /// A lamp id that is not a LampMarker (the PD_Lamp possessable hanging there) resolves to the nearest LampMarker in the room that contains the
    /// entity, so other modules never need to know the markers. A Broken lamp cannot be switched On (repair = SetLampAuthority(Off) first).
    /// A new round turns every lamp Off and mends the broken ones.
    /// </summary>
    public sealed class LightService : ILightService
    {
        private const string Category = "Light";

        private readonly INetBridge _net;
        private readonly HouseIndex _index;
        private readonly RoomService _rooms;
        private readonly Func<NetId, Vector3?> _entityPosition;
        private readonly LightStateCore _core = new LightStateCore();
        private readonly List<NetId> _planLamps = new List<NetId>(4);
        private readonly List<LampState> _planStates = new List<LampState>(4);
        private readonly Action<NetMsg> _onChanged;
        private readonly Action<NetMsg> _onReqLamp;
        private readonly Action<RoundStartedEvt> _onRoundStarted;
        private bool _attached;

        /// <param name="entityPosition">World position of any registered entity (used to place a non-marker lamp id), or null.</param>
        public LightService(INetBridge net, HouseIndex index, RoomService rooms, Func<NetId, Vector3?> entityPosition)
        {
            _net = net ?? throw new ArgumentNullException(nameof(net));
            _index = index ?? throw new ArgumentNullException(nameof(index));
            _rooms = rooms ?? throw new ArgumentNullException(nameof(rooms));
            _entityPosition = entityPosition;
            for (int i = 0; i < index.Lamps.Count; i++)
                _core.AddLamp(index.Lamps[i].NetId, index.ResolveRoom(index.Lamps[i]));
            _onChanged = OnLightsChanged;
            _onReqLamp = OnReqLamp;
            _onRoundStarted = OnRoundStarted;
        }

        /// <summary>A lamp changed on this machine (after the room light was updated).</summary>
        public event Action<NetId, LampState> LampChanged;

        public LightStateCore State => _core;

        public void Attach()
        {
            if (_attached)
                return;
            _attached = true;
            _net.Register(MsgCode.LightsChanged, _onChanged);
            _net.Register(MsgCode.ReqLampLight, _onReqLamp);
            _net.Register(MsgCode.ReqLampRepair, _onReqLamp);
            EventBus.Subscribe(_onRoundStarted);
        }

        public void Detach()
        {
            if (!_attached)
                return;
            _attached = false;
            _net.Unregister(MsgCode.LightsChanged, _onChanged);
            _net.Unregister(MsgCode.ReqLampLight, _onReqLamp);
            _net.Unregister(MsgCode.ReqLampRepair, _onReqLamp);
            EventBus.Unsubscribe(_onRoundStarted);
        }

        // ---- authority ----

        public void SetLampAuthority(NetId lamp, LampState state)
        {
            Debug.Assert(_net.IsAuthority, "SetLampAuthority must only run on the authority");
            NetId marker = Resolve(lamp);
            if (!marker.IsValid)
            {
                CMLog.Warn(Category, "SetLampAuthority: " + lamp + " is not near any lamp - ignored");
                return;
            }
            LampState current = _core.StateOf(marker);
            if (current == state)
                return;
            if (current == LampState.Broken && state == LampState.On)
            {
                CMLog.Info(Category, marker + " is broken - it must be repaired before it can light");
                return;
            }
            _net.Broadcast(LightsChangedMsg.Encode(marker, state));
        }

        /// <summary>Authority only. The wall switch of <paramref name="room"/>: all its working lamps on, or all off when any was on. Returns lamps changed.</summary>
        public int ToggleRoomAuthority(int room)
        {
            Debug.Assert(_net.IsAuthority, "ToggleRoomAuthority must only run on the authority");
            _planLamps.Clear();
            _planStates.Clear();
            _core.PlanToggle(room, _planLamps, _planStates);
            for (int i = 0; i < _planLamps.Count; i++)
                _net.Broadcast(LightsChangedMsg.Encode(_planLamps[i], _planStates[i]));
            return _planLamps.Count;
        }

        /// <summary>
        /// Authority only (TODO 3.5). The good jinn <paramref name="who"/> lights an Off lamp (<paramref name="repair"/> false) or repairs a Broken one
        /// (true; it lights up again). Checks: role GoodJinn, the lamp state, and the jinn within <paramref name="range"/> of the lamp (its pawn
        /// position from <paramref name="pawnPosition"/>). Returns whether a LightsChanged was sent.
        /// </summary>
        public bool ProcessLampRequestAuthority(PlayerId who, NetId lamp, bool repair, Vector3? pawnPosition, float range)
        {
            Debug.Assert(_net.IsAuthority, "ProcessLampRequestAuthority must only run on the authority");
            IPlayerRegistry players = GameServices.Players;
            if (players == null || players.GetRole(who) != Role.GoodJinn || !_core.IsLamp(lamp))
                return false;
            LampState state = _core.StateOf(lamp);
            if (repair ? state != LampState.Broken : state != LampState.Off)
                return false;
            if (!pawnPosition.HasValue || (PositionOf(lamp) - pawnPosition.Value).magnitude > range)
            {
                CMLog.Info(Category, (repair ? "Repair" : "Light") + " of " + lamp + " by " + who + " refused: out of range");
                return false;
            }
            _net.Broadcast(LightsChangedMsg.Encode(lamp, LampState.On));
            return true;
        }

        /// <summary>World position of a LampMarker (Vector3.zero for an unknown id).</summary>
        public Vector3 PositionOf(NetId lamp)
        {
            IReadOnlyList<HouseSpotEntry> lamps = _index.Lamps;
            for (int i = 0; i < lamps.Count; i++)
            {
                if (lamps[i].NetId == lamp)
                    return lamps[i].Position;
            }
            return Vector3.zero;
        }

        /// <summary>
        /// The LampMarker a lamp id means: the id itself when it is one; otherwise the marker closest to the entity's position within the room
        /// that contains it. Invalid when none fits.
        /// </summary>
        public NetId Resolve(NetId lamp)
        {
            if (_core.IsLamp(lamp))
                return lamp;
            Vector3? position = _entityPosition != null ? _entityPosition(lamp) : null;
            if (!position.HasValue)
                return NetId.Invalid;
            int room = _index.RoomOf(position.Value);
            NetId best = NetId.Invalid;
            float bestDistance = float.MaxValue;
            IReadOnlyList<HouseSpotEntry> lamps = _index.Lamps;
            for (int i = 0; i < lamps.Count; i++)
            {
                if (_index.ResolveRoom(lamps[i]) != room)
                    continue;
                float d = (lamps[i].Position - position.Value).sqrMagnitude;
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = lamps[i].NetId;
                }
            }
            return best;
        }

        // ---- every machine ----

        private void OnLightsChanged(NetMsg msg)
        {
            if (!LightsChangedMsg.TryDecode(msg, out NetId lamp, out LampState state))
            {
                CMLog.Warn(Category, "Malformed LightsChanged ignored");
                return;
            }
            if (!_core.Set(lamp, state))
                return;
            int room = _core.RoomOfLamp(lamp);
            RoomLightState light = _core.LightOf(room);
            if (_rooms.LightOf(room) != light && _rooms.SetRoomLight(room, light))
                EventBus.Publish(new RoomLightChangedEvt(room, light));
            LampChanged?.Invoke(lamp, state);
        }

        private void OnReqLamp(NetMsg msg)
        {
            if (!_net.IsAuthority)
                return;
            if (!ReqLampMsg.TryDecode(msg, out NetId lamp, out bool repair))
            {
                CMLog.Warn(Category, "Malformed lamp request ignored");
                return;
            }
            GameBalanceConfig config = GameServices.Config;
            float range = config != null ? config.LampLightRange + config.NetRangeTolerance : LampLightView.DefaultRange;
            ProcessLampRequestAuthority(msg.Sender, lamp, repair, PawnPosition(msg.Sender), range);
        }

        /// <summary>Scene search over PawnBase by owner; on a request only.</summary>
        private static Vector3? PawnPosition(PlayerId who)
        {
            PawnBase[] pawns = UnityEngine.Object.FindObjectsByType<PawnBase>(FindObjectsSortMode.None);
            for (int i = 0; i < pawns.Length; i++)
            {
                if (pawns[i].Owner == who)
                    return pawns[i].transform.position;
            }
            return null;
        }

        private void OnRoundStarted(RoundStartedEvt e)
        {
            _core.Reset();
            for (int i = 0; i < _index.Rooms.Count; i++)
            {
                int room = _index.Rooms[i].Id;
                if (_rooms.LightOf(room) == RoomLightState.On && _rooms.SetRoomLight(room, RoomLightState.Off))
                    EventBus.Publish(new RoomLightChangedEvt(room, RoomLightState.Off));
            }
            for (int i = 0; i < _core.LampCount; i++)
                LampChanged?.Invoke(_core.LampAt(i), LampState.Off);
        }
    }
}
