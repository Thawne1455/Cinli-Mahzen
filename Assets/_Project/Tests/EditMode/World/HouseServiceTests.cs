using System.Collections.Generic;
using System.Text.RegularExpressions;
using CinliMahzen.Core;
using CinliMahzen.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CinliMahzen.Tests.EditMode.World
{
    /// <summary>
    /// TODO 2.2: <see cref="HouseService"/> reading real marker components (GameObjects in EditMode; Awake does not run there, so the
    /// tests call Index()/Release() themselves). The little house: a hall room, a garden room, a lock-free stage gate, a front
    /// door gate with a lock, two lamps, a key hiding place, spawns and a puzzle slot.
    /// </summary>
    public class HouseServiceTests
    {
        private readonly List<GameObject> _created = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            GameServices.ResetToStubs();
            GameServices.Register<IEntityRegistry>(new EntityRegistry());
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _created.Count; i++)
            {
                if (_created[i] != null)
                {
                    Object.DestroyImmediate(_created[i]);
                }
            }
            _created.Clear();
            GameServices.ResetToStubs();
        }

        private sealed class LittleHouse
        {
            public HouseService Service;
            public LampMarker LampA;
            public LampMarker LampB;
            public StageGate FrontDoor;
            public KeyHideSpot KeyHide;
            public PuzzleSlot Slot;
            public RoomMarker Hall;
        }

        private GameObject NewObject(string name, Transform parent, Vector3 localPosition)
        {
            var go = new GameObject(name);
            if (parent == null)
            {
                _created.Add(go);
            }
            else
            {
                go.transform.SetParent(parent, false);
            }
            go.transform.localPosition = localPosition;
            return go;
        }

        private LittleHouse CreateHouse(string rootName, Vector3 rootPosition, bool reverseOrder)
        {
            var house = new LittleHouse();
            GameObject root = NewObject(rootName, null, rootPosition);
            house.Service = root.AddComponent<HouseService>();

            // Room volumes (local space of the room object, which sits at the root).
            GameObject hall = NewObject("Room_Hall", root.transform, Vector3.zero);
            house.Hall = hall.AddComponent<RoomMarker>();
            house.Hall.Configure(1, "hall", 0, false);
            SetBox(hall.GetComponent<BoxCollider>(), new Vector3(5f, 2f, 5f), new Vector3(10f, 4f, 10f));

            GameObject garden = NewObject("Room_Garden", root.transform, Vector3.zero);
            garden.AddComponent<RoomMarker>().Configure(0, "garden", 0, true);
            SetBox(garden.GetComponent<BoxCollider>(), new Vector3(0f, 6f, -10f), new Vector3(40f, 12f, 20f));

            GameObject markers = NewObject("Markers", root.transform, Vector3.zero);
            markers.AddComponent<StageZone>().Configure(0, new[] { 0 });
            // (a second zone on a child object)
            var stage1 = NewObject("Stage_1", markers.transform, Vector3.zero);
            stage1.AddComponent<StageZone>().Configure(1, new[] { 1 });

            // The marker objects are created in forward or reverse order: NetIds must not depend on sibling order.
            var builders = new List<System.Action>
            {
                () =>
                {
                    GameObject a = NewObject("Lamp_A", markers.transform, new Vector3(5f, 3f, 5f));
                    house.LampA = a.AddComponent<LampMarker>();
                    house.LampA.Configure(1);
                },
                () =>
                {
                    GameObject b = NewObject("Lamp_B", markers.transform, new Vector3(2f, 3f, 2f));
                    house.LampB = b.AddComponent<LampMarker>();
                    house.LampB.Configure(1);
                },
                () =>
                {
                    GameObject gate = NewObject("Gate", markers.transform, new Vector3(5f, 0f, 0f));
                    GameObject door = NewObject("DoorPiece", gate.transform, new Vector3(0f, 0f, 0.5f));
                    house.FrontDoor = gate.AddComponent<StageGate>();
                    house.FrontDoor.Configure(0, door.transform);
                    gate.AddComponent<GateLock>().Configure("house_key");
                },
                () =>
                {
                    GameObject hide = NewObject("KeyHide", markers.transform, new Vector3(0f, 0.5f, -5f));
                    house.KeyHide = hide.AddComponent<KeyHideSpot>();
                },
                () =>
                {
                    GameObject slot = NewObject("Slot", markers.transform, new Vector3(2f, 1.5f, 5f));
                    GameObject reward = NewObject("RewardPoint", slot.transform, new Vector3(0f, -0.5f, 1f));
                    house.Slot = slot.AddComponent<PuzzleSlot>();
                    house.Slot.Configure(1, 1, PuzzleSlotSize.Wall, new[] { PuzzleType.Paintings, PuzzleType.Wires }, reward.transform);
                },
                () =>
                {
                    NewObject("Human", markers.transform, new Vector3(0f, 0.1f, -4f)).AddComponent<HumanSpawnMarker>();
                    NewObject("JinnA", markers.transform, new Vector3(3f, 1.6f, 3f)).AddComponent<JinnSpawnMarker>();
                    NewObject("JinnB", markers.transform, new Vector3(7f, 1.6f, 7f)).AddComponent<JinnSpawnMarker>();
                }
            };
            if (reverseOrder)
            {
                builders.Reverse();
            }
            for (int i = 0; i < builders.Count; i++)
            {
                builders[i]();
            }
            return house;
        }

        private static void SetBox(BoxCollider box, Vector3 center, Vector3 size)
        {
            box.isTrigger = true;
            box.center = center;
            box.size = size;
        }

        // ------------------------------------------------------------------------------------------------

        [Test]
        public void Index_GivesNetIdsFromTheSortedPathsRelativeToTheService()
        {
            LittleHouse house = CreateHouse("P_House", Vector3.zero, false);

            Assert.IsTrue(house.Service.Index());

            // Ordinal order of the keys: Markers/Gate#StageGate < Markers/KeyHide#KeyHideSpot < Markers/Lamp_A#LampMarker
            // < Markers/Lamp_B#LampMarker < Markers/Slot#PuzzleSlot.
            Assert.AreEqual(new NetId(1000), house.FrontDoor.NetId);
            Assert.AreEqual(new NetId(1001), house.KeyHide.NetId);
            Assert.AreEqual(new NetId(1002), house.LampA.NetId);
            Assert.AreEqual(new NetId(1003), house.LampB.NetId);
            Assert.AreEqual(new NetId(1004), house.Slot.NetId);
        }

        [Test]
        public void Index_NetIds_DoNotDependOnSiblingOrderRootNameOrPosition()
        {
            LittleHouse first = CreateHouse("P_House", Vector3.zero, false);
            LittleHouse second = CreateHouse("P_House (1)", new Vector3(100f, 0f, 50f), true);

            Assert.IsTrue(first.Service.Index());
            first.Service.Release();
            Assert.IsTrue(second.Service.Index());

            Assert.AreEqual(first.FrontDoor.NetId.Value, second.FrontDoor.NetId.Value);
            Assert.AreEqual(first.KeyHide.NetId.Value, second.KeyHide.NetId.Value);
            Assert.AreEqual(first.LampA.NetId.Value, second.LampA.NetId.Value);
            Assert.AreEqual(first.LampB.NetId.Value, second.LampB.NetId.Value);
            Assert.AreEqual(first.Slot.NetId.Value, second.Slot.NetId.Value);
        }

        [Test]
        public void Index_BuildsThePlainDataIndex_FromTheMarkers()
        {
            LittleHouse house = CreateHouse("P_House", Vector3.zero, false);
            house.Service.Index();
            HouseIndex index = house.Service.HouseIndex;

            Assert.IsTrue(house.Service.IsIndexed);
            Assert.AreEqual(2, index.Rooms.Count);
            Assert.AreEqual(2, index.Stages.Count);
            Assert.AreEqual(1, index.PuzzleSlots.Count);
            Assert.AreEqual(1, index.Gates.Count);
            Assert.AreEqual(2, index.Lamps.Count);
            Assert.AreEqual(1, index.KeyHideSpots.Count);
            Assert.AreEqual(1, index.HumanSpawns.Count);
            Assert.AreEqual(2, index.JinnSpawns.Count);

            HousePuzzleSlotEntry slot = index.PuzzleSlots[0];
            Assert.AreEqual(1, slot.StageIndex);
            Assert.AreEqual(1, slot.RoomId);
            Assert.AreEqual(PuzzleSlotSize.Wall, slot.Size);
            Assert.IsTrue(slot.Allows(PuzzleType.Wires));
            Assert.IsFalse(slot.Allows(PuzzleType.Statue));
            Assert.AreEqual(house.Slot.RewardPoint.position, slot.RewardPosition);
            Assert.AreEqual(new Vector3(2f, 1.5f, 5f), slot.Position);

            HouseGateEntry gate = index.Gates[0];
            Assert.AreEqual(0, gate.StageIndex);
            Assert.AreEqual("house_key", gate.RequiredItemId);
            Assert.AreEqual(new Vector3(5f, 0f, 0.5f), gate.Position, "the gate is where its door piece is");
        }

        [Test]
        public void Index_ReadsRoomBoxesInWorldSpace()
        {
            LittleHouse house = CreateHouse("P_House", new Vector3(100f, 0f, 50f), false);
            house.Service.Index();
            var rooms = GameServices.Get<IRoomService>();

            Assert.AreEqual(1, rooms.RoomOf(new Vector3(105f, 2f, 55f)), "hall, moved with the house");
            Assert.AreEqual(-1, rooms.RoomOf(new Vector3(5f, 2f, 5f)), "where the hall would be without the offset");
            Assert.AreEqual(0, rooms.RoomOf(new Vector3(100f, 3f, 40f)), "garden");
        }

        [Test]
        public void Index_RegistersTheHouseServices()
        {
            LittleHouse house = CreateHouse("P_House", Vector3.zero, false);
            Assert.IsInstanceOf<StubHouseInfo>(GameServices.House);
            Assert.IsInstanceOf<StubRoomService>(GameServices.Get<IRoomService>());

            house.Service.Index();

            Assert.IsInstanceOf<HouseInfo>(GameServices.House);
            Assert.IsInstanceOf<RoomService>(GameServices.Get<IRoomService>());
            Assert.AreSame(house.Service.Info, GameServices.House);
            Assert.AreSame(house.Service.RoomState, GameServices.Get<IRoomService>());
            Assert.AreEqual(new Vector3(0f, 0.1f, -4f), GameServices.House.HumanSpawn);
            Assert.AreEqual(2, GameServices.House.JinnSpawns.Count);
            Assert.AreEqual(1, GameServices.Get<IRoomService>().RoomOf(new Vector3(5f, 2f, 5f)));
            Assert.AreEqual(RoomLightState.Off, GameServices.Get<IRoomService>().LightOf(1));
        }

        [Test]
        public void Release_PutsTheStubsBack_AndAllowsIndexingAgain()
        {
            LittleHouse house = CreateHouse("P_House", Vector3.zero, false);
            house.Service.Index();

            house.Service.Release();

            Assert.IsFalse(house.Service.IsIndexed);
            Assert.IsInstanceOf<StubHouseInfo>(GameServices.House);
            Assert.IsInstanceOf<StubRoomService>(GameServices.Get<IRoomService>());
            Assert.IsTrue(house.Service.Index(), "a released service can index again");
            Assert.IsInstanceOf<HouseInfo>(GameServices.House);
        }

        [Test]
        public void Index_RegistersTheNetMarkersAsEntities_ReleaseRemovesThem()
        {
            LittleHouse house = CreateHouse("P_House", Vector3.zero, false);
            house.Service.Index();

            LampMarker found;
            Assert.IsTrue(GameServices.Entities.TryGet(house.LampA.NetId, out found));
            Assert.AreSame(house.LampA, found);
            StageGate gate;
            Assert.IsTrue(GameServices.Entities.TryGet(house.FrontDoor.NetId, out gate));

            NetId lampId = house.LampA.NetId;
            house.Service.Release();

            Assert.IsFalse(GameServices.Entities.TryGet(lampId, out found));
        }

        [Test]
        public void Index_Twice_IsIgnored()
        {
            LittleHouse house = CreateHouse("P_House", Vector3.zero, false);
            house.Service.Index();
            HouseIndex first = house.Service.HouseIndex;
            LogAssert.Expect(LogType.Warning, new Regex("called twice"));

            Assert.IsTrue(house.Service.Index());

            Assert.AreSame(first, house.Service.HouseIndex);
        }

        [Test]
        public void Index_TwoMarkersWithTheSamePath_FailsAndLeavesTheStubs()
        {
            LittleHouse house = CreateHouse("P_House", Vector3.zero, false);
            house.LampA.gameObject.AddComponent<LampMarker>().Configure(1); // same object, same type: same key
            LogAssert.Expect(LogType.Error, new Regex("cannot build the house index"));

            Assert.IsFalse(house.Service.Index());

            Assert.IsFalse(house.Service.IsIndexed);
            StringAssert.Contains("Lamp_A", house.Service.LastError);
            Assert.IsInstanceOf<StubHouseInfo>(GameServices.House);
            Assert.IsInstanceOf<StubRoomService>(GameServices.Get<IRoomService>());
            Assert.IsFalse(house.LampB.NetId.IsValid, "no NetId was handed out");
        }

        [Test]
        public void TryBuildIndex_ChangesNothing()
        {
            LittleHouse house = CreateHouse("P_House", Vector3.zero, false);

            HouseIndex index;
            string error;
            Assert.IsTrue(house.Service.TryBuildIndex(out index, out error));

            Assert.IsNotNull(index);
            Assert.AreEqual(5, CountNetEntries(index));
            Assert.IsFalse(house.Service.IsIndexed);
            Assert.IsFalse(house.LampA.NetId.IsValid, "markers keep NetId 0");
            Assert.IsInstanceOf<StubHouseInfo>(GameServices.House);
            LampMarker none;
            Assert.IsFalse(GameServices.Entities.TryGet(new NetId(1002), out none));
        }

        private static int CountNetEntries(HouseIndex index)
        {
            var all = new List<HouseNetEntry>();
            index.CollectNetEntries(all);
            return all.Count;
        }

        [Test]
        public void TryBuildIndex_ReportsDuplicatePaths()
        {
            LittleHouse house = CreateHouse("P_House", Vector3.zero, false);
            house.KeyHide.gameObject.AddComponent<KeyHideSpot>();

            HouseIndex index;
            string error;
            Assert.IsFalse(house.Service.TryBuildIndex(out index, out error));

            Assert.IsNull(index);
            StringAssert.Contains("KeyHide", error);
        }

        [Test]
        public void RoomMarker_CollectsEveryEnabledBox_AndSkipsDisabledOnes()
        {
            GameObject go = NewObject("Room_L", null, new Vector3(10f, 0f, 0f));
            RoomMarker marker = go.AddComponent<RoomMarker>();
            SetBox(go.GetComponent<BoxCollider>(), new Vector3(2f, 2f, 6f), new Vector3(4f, 4f, 12f));
            BoxCollider arm = go.AddComponent<BoxCollider>();
            SetBox(arm, new Vector3(8f, 2f, 2f), new Vector3(8f, 4f, 4f));
            BoxCollider off = go.AddComponent<BoxCollider>();
            SetBox(off, new Vector3(100f, 2f, 2f), new Vector3(1f, 1f, 1f));
            off.enabled = false;

            var boxes = new List<Bounds>();
            marker.CollectWorldBoxes(boxes);

            Assert.AreEqual(2, boxes.Count);
            Assert.AreEqual(new Vector3(12f, 2f, 6f), boxes[0].center, "moved by the object position");
            Assert.AreEqual(new Vector3(4f, 4f, 12f), boxes[0].size);
            Assert.AreEqual(new Vector3(18f, 2f, 2f), boxes[1].center);
        }

        [Test]
        public void RoomMarker_Configure_SetsItsValues()
        {
            GameObject go = NewObject("Room_Hall", null, Vector3.zero);
            RoomMarker marker = go.AddComponent<RoomMarker>();

            marker.Configure(7, "hall", 1, true);

            Assert.AreEqual(7, marker.RoomId);
            Assert.AreEqual("hall", marker.RoomKey);
            Assert.AreEqual(1, marker.Floor);
            Assert.IsTrue(marker.IsOutdoor);
            Assert.AreEqual("room.hall", marker.NameKey);
        }

        [Test]
        public void PuzzleSlot_WithoutRewardPoint_UsesItsOwnTransform()
        {
            GameObject go = NewObject("Slot", null, new Vector3(1f, 2f, 3f));
            PuzzleSlot slot = go.AddComponent<PuzzleSlot>();
            slot.Configure(1, 1, PuzzleSlotSize.Table, new[] { PuzzleType.Statue }, null);

            Assert.AreSame(go.transform, slot.RewardPoint);
            Assert.AreEqual(new Vector3(1f, 2f, 3f), slot.RewardPoint.position);
        }

        [Test]
        public void StageGate_WithoutDoor_UsesItsOwnTransform()
        {
            GameObject go = NewObject("Gate", null, Vector3.zero);
            StageGate gate = go.AddComponent<StageGate>();
            gate.Configure(2, null);

            Assert.AreSame(go.transform, gate.Door);
            Assert.AreEqual(2, gate.StageIndex);
        }

        [Test]
        public void GateLock_RequiresAStageGateOnTheSameObject()
        {
            GameObject go = NewObject("Gate", null, Vector3.zero);

            go.AddComponent<GateLock>().Configure("cellar_key");

            Assert.IsNotNull(go.GetComponent<StageGate>(), "RequireComponent adds the gate");
            Assert.AreEqual("cellar_key", go.GetComponent<GateLock>().RequiredItemId);
        }
    }
}
