using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.Core.Events;
using CinliMahzen.Core.Net;
using CinliMahzen.World;
using CinliMahzen.World.Graybox;
using NUnit.Framework;
using UnityEngine;

namespace CinliMahzen.Tests.EditMode.World
{
    /// <summary>TODO 3.4: <see cref="LightStateCore"/> and <see cref="LightService"/> over the graybox house index and an offline bridge.</summary>
    public class LightServiceTests
    {
        private double _now;
        private OfflineNetBridge _bridge;
        private HouseIndex _index;
        private RoomService _rooms;
        private LightService _lights;
        private readonly Dictionary<NetId, Vector3> _entities = new Dictionary<NetId, Vector3>();
        private readonly List<RoomLightChangedEvt> _roomEvents = new List<RoomLightChangedEvt>();
        private readonly List<NetMsg> _sent = new List<NetMsg>();

        private static readonly int Kitchen = GrayboxMarkers.RoomIdOf("kitchen");
        private static readonly int Hall = GrayboxMarkers.RoomIdOf("hall");

        [SetUp]
        public void SetUp()
        {
            EventBus.ClearAll();
            _now = 0d;
            _bridge = new OfflineNetBridge(() => _now, () => new PlayerId(0));
            _index = GrayboxHouseIndexFactory.BuildDefault();
            _rooms = new RoomService(_index, () => _now);
            _entities.Clear();
            _lights = new LightService(_bridge, _index, _rooms, id => _entities.TryGetValue(id, out Vector3 p) ? p : (Vector3?)null);
            _lights.Attach();
            _roomEvents.Clear();
            _sent.Clear();
            EventBus.Subscribe<RoomLightChangedEvt>(_roomEvents.Add);
            _bridge.Register(MsgCode.LightsChanged, _sent.Add);
        }

        [TearDown]
        public void TearDown()
        {
            _lights.Detach();
            EventBus.ClearAll();
        }

        private NetId LampOf(int room)
        {
            for (int i = 0; i < _index.Lamps.Count; i++)
            {
                if (_index.ResolveRoom(_index.Lamps[i]) == room)
                    return _index.Lamps[i].NetId;
            }
            Assert.Fail("no lamp in room " + room);
            return NetId.Invalid;
        }

        [Test]
        public void HouseStartsDark_AndEveryLampIsKnown()
        {
            Assert.AreEqual(_index.Lamps.Count, _lights.State.LampCount);
            Assert.AreEqual(RoomLightState.Off, _rooms.LightOf(Kitchen));
        }

        [Test]
        public void Switch_LightsTheRoom_ThenTurnsItOff()
        {
            Assert.Greater(_lights.ToggleRoomAuthority(Kitchen), 0);

            Assert.AreEqual(RoomLightState.On, _rooms.LightOf(Kitchen));
            Assert.AreEqual(RoomLightState.Off, _rooms.LightOf(Hall), "only that room");
            Assert.AreEqual(1, _roomEvents.Count);
            Assert.AreEqual(Kitchen, _roomEvents[0].Room);

            _lights.ToggleRoomAuthority(Kitchen);
            Assert.AreEqual(RoomLightState.Off, _rooms.LightOf(Kitchen));
        }

        [Test]
        public void BrokenLamp_GivesNoLight_AndCannotBeSwitchedOn()
        {
            NetId lamp = LampOf(Kitchen);
            _lights.ToggleRoomAuthority(Kitchen);

            _lights.SetLampAuthority(lamp, LampState.Broken);
            Assert.AreEqual(RoomLightState.Off, _rooms.LightOf(Kitchen), "the burst darkens the room");

            _sent.Clear();
            _lights.ToggleRoomAuthority(Kitchen);
            _lights.SetLampAuthority(lamp, LampState.On);
            Assert.AreEqual(0, _sent.Count, "a broken lamp stays broken");
            Assert.AreEqual(LampState.Broken, _lights.State.StateOf(lamp));

            _lights.SetLampAuthority(lamp, LampState.Off); // repair (TODO 3.5)
            _lights.ToggleRoomAuthority(Kitchen);
            Assert.AreEqual(RoomLightState.On, _rooms.LightOf(Kitchen));
        }

        [Test]
        public void ANonMarkerLampId_ResolvesToTheNearestLampMarkerInItsRoom()
        {
            NetId marker = LampOf(Kitchen);
            Vector3 markerPos = Vector3.zero;
            for (int i = 0; i < _index.Lamps.Count; i++)
            {
                if (_index.Lamps[i].NetId == marker)
                    markerPos = _index.Lamps[i].Position;
            }
            var possessableLamp = new NetId(40123);
            _entities[possessableLamp] = markerPos + new Vector3(0.1f, -0.2f, 0f);

            Assert.AreEqual(marker, _lights.Resolve(possessableLamp));
            _lights.ToggleRoomAuthority(Kitchen);
            _lights.SetLampAuthority(possessableLamp, LampState.Broken);
            Assert.AreEqual(LampState.Broken, _lights.State.StateOf(marker));
            Assert.AreEqual(NetId.Invalid, _lights.Resolve(new NetId(49999)), "unknown entity");
        }

        [Test]
        public void LampChanged_IsRaisedForVisuals()
        {
            var changes = new List<LampState>();
            _lights.LampChanged += (id, state) => changes.Add(state);

            _lights.ToggleRoomAuthority(Kitchen);

            CollectionAssert.Contains(changes, LampState.On);
        }

        [Test]
        public void NewRound_TurnsEverythingOff_AndMendsBrokenLamps()
        {
            NetId lamp = LampOf(Kitchen);
            _lights.ToggleRoomAuthority(Hall);
            _lights.SetLampAuthority(lamp, LampState.Broken);

            EventBus.Publish(new RoundStartedEvt(1, 5));

            Assert.AreEqual(RoomLightState.Off, _rooms.LightOf(Hall));
            Assert.AreEqual(LampState.Off, _lights.State.StateOf(lamp));
        }

        [Test]
        public void LightsChanged_RoundTrip_AndRejectsUnknownStates()
        {
            Assert.IsTrue(LightsChangedMsg.TryDecode(LightsChangedMsg.Encode(new NetId(1002), LampState.Broken), out NetId lamp, out LampState state));
            Assert.AreEqual(new NetId(1002), lamp);
            Assert.AreEqual(LampState.Broken, state);
            Assert.IsFalse(LightsChangedMsg.TryDecode(new NetMsg { Code = MsgCode.LightsChanged, Payload = new object[] { 1002, (byte)9 } }, out _, out _));
        }
    }
}
