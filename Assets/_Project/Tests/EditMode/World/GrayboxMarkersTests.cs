using System;
using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.World;
using CinliMahzen.World.Graybox;
using NUnit.Framework;
using UnityEngine;

namespace CinliMahzen.Tests.EditMode.World
{
    /// <summary>
    /// TODO 2.2: the marker data of the shipped graybox house (<see cref="GrayboxMarkers.Default"/>), checked on paper: it is turned
    /// into a plain <see cref="HouseIndex"/> with <see cref="GrayboxHouseIndexFactory"/> and must pass <see cref="HouseValidator"/>
    /// without errors. Pure C#, no Unity objects.
    /// </summary>
    public class GrayboxMarkersTests
    {
        private static readonly string[] AllItemIds = { "house_key", "blessed_oil", "fuse_key", "paint", "shovel", "cellar_key" };

        private static GrayboxLayout Layout
        {
            get { return GrayboxLayout.Default; }
        }

        private static GrayboxMarkers Markers
        {
            get { return GrayboxMarkers.Default; }
        }

        private static HouseIndex Index()
        {
            return GrayboxHouseIndexFactory.BuildDefault();
        }

        private static List<GrayboxMarkerSpec> SpecsOf(GrayboxMarkerKind kind)
        {
            var result = new List<GrayboxMarkerSpec>();
            for (int i = 0; i < Markers.Specs.Count; i++)
            {
                if (Markers.Specs[i].Kind == kind)
                {
                    result.Add(Markers.Specs[i]);
                }
            }
            return result;
        }

        private static string Dump(List<HouseIssue> issues)
        {
            var lines = new List<string>();
            for (int i = 0; i < issues.Count; i++)
            {
                lines.Add(issues[i].ToString());
            }
            return lines.Count == 0 ? "(no issues)" : "validator said:\n  " + string.Join("\n  ", lines);
        }

        // ------------------------------------------------------------------------------------------------
        // the whole house
        // ------------------------------------------------------------------------------------------------

        [Test]
        public void DefaultHouse_PassesTheValidator_WithoutErrors()
        {
            List<HouseIssue> issues = HouseValidator.Validate(Index(), AllItemIds);

            Assert.AreEqual(0, HouseValidator.CountErrors(issues), Dump(issues));
        }

        [Test]
        public void DefaultHouse_OnlyWarnsAboutTheCellar()
        {
            List<HouseIssue> issues = HouseValidator.Validate(Index(), AllItemIds);

            Assert.AreEqual(2, HouseValidator.CountWarnings(issues), Dump(issues));
            var texts = new List<string>();
            for (int i = 0; i < issues.Count; i++)
            {
                texts.Add(issues[i].Message);
            }
            Assert.IsTrue(texts.Exists(t => t.Contains("DigSpots")), Dump(issues));
            Assert.IsTrue(texts.Exists(t => t.Contains("stage 2 has no StageGate")), Dump(issues));
        }

        [Test]
        public void DefaultHouse_HasTheMarkerCountsOfTheContract()
        {
            HouseIndex index = Index();

            Assert.AreEqual(Layout.Rooms.Count, index.Rooms.Count, "one RoomMarker per room");
            Assert.AreEqual(3, index.Stages.Count);
            Assert.GreaterOrEqual(CountOfStage(index, 1), HouseContract.MinPuzzleSlotsPerStage);
            Assert.GreaterOrEqual(CountOfStage(index, 2), HouseContract.MinPuzzleSlotsPerStage);
            Assert.GreaterOrEqual(index.ReferenceSlots.Count, HouseContract.MinReferenceSlots);
            Assert.AreEqual(2, index.Gates.Count);
            Assert.GreaterOrEqual(index.KeyHideSpots.Count, HouseContract.MinKeyHideSpots);
            Assert.GreaterOrEqual(index.CologneSpots.Count, HouseContract.MinCologneSpawnSpots);
            Assert.AreEqual(0, index.DigSpots.Count, "the cellar arrives with TODO 3.7");
            Assert.AreEqual(HouseContract.HumanSpawnCount, index.HumanSpawns.Count);
            Assert.AreEqual(HouseContract.JinnSpawnCount, index.JinnSpawns.Count);
            Assert.AreEqual(1, index.ToolShedItems.Count);
            Assert.AreEqual(HouseContract.BlessedOilItemId, index.ToolShedItems[0].ItemId);
        }

        private static int CountOfStage(HouseIndex index, int stage)
        {
            int count = 0;
            for (int i = 0; i < index.PuzzleSlots.Count; i++)
            {
                if (index.PuzzleSlots[i].StageIndex == stage)
                {
                    count++;
                }
            }
            return count;
        }

        // ------------------------------------------------------------------------------------------------
        // rooms
        // ------------------------------------------------------------------------------------------------

        [Test]
        public void EveryLayoutRoom_HasAStableUniqueId_AndNoStaleIdsRemain()
        {
            var seen = new HashSet<int>();
            for (int i = 0; i < Layout.Rooms.Count; i++)
            {
                string key = Layout.Rooms[i].Id;
                int id = GrayboxMarkers.RoomIdOf(key);
                Assert.GreaterOrEqual(id, 0, key + " has no id in the table");
                Assert.IsTrue(seen.Add(id), "duplicate id for " + key);
                Assert.AreEqual(key, GrayboxMarkers.RoomKeyOf(id));
            }
            Assert.AreEqual(-1, GrayboxMarkers.RoomIdOf("no_such_room"));
            Assert.IsNull(GrayboxMarkers.RoomKeyOf(-1));
            Assert.IsNull(GrayboxMarkers.RoomKeyOf(Layout.Rooms.Count));
            Assert.AreEqual(Layout.Rooms.Count, seen.Count, "the id table has rooms the layout does not");
        }

        [Test]
        public void RoomIds_AreTheDocumentedStableValues()
        {
            // Ids are wire values (RoundPlan, IRoomService): they only ever grow at the end of the table.
            Assert.AreEqual(0, GrayboxMarkers.RoomIdOf("garden"));
            Assert.AreEqual(1, GrayboxMarkers.RoomIdOf("shed"));
            Assert.AreEqual(2, GrayboxMarkers.RoomIdOf("hall"));
            Assert.AreEqual(12, GrayboxMarkers.RoomIdOf("study"));
        }

        [Test]
        public void RoomEntries_CarryFloorAndOutdoorFlagOfTheLayout()
        {
            HouseIndex index = Index();

            for (int i = 0; i < Layout.Rooms.Count; i++)
            {
                GrayboxRoom room = Layout.Rooms[i];
                HouseRoomEntry entry = index.FindRoom(GrayboxMarkers.RoomIdOf(room.Id));
                Assert.IsNotNull(entry, room.Id);
                Assert.AreEqual(room.Id, entry.Key);
                Assert.AreEqual(room.Floor, entry.Floor, room.Id);
                Assert.AreEqual(room.IsOutdoor, entry.IsOutdoor, room.Id);
                Assert.AreEqual(room.Rects.Count, entry.Boxes.Count, room.Id + ": one box per rectangle");
            }
        }

        [Test]
        public void EveryCellOfEveryRoom_ResolvesToItsRoom()
        {
            HouseIndex index = Index();
            var cells = new List<GrayboxCell>();

            for (int i = 0; i < Layout.Rooms.Count; i++)
            {
                GrayboxRoom room = Layout.Rooms[i];
                int id = GrayboxMarkers.RoomIdOf(room.Id);
                cells.Clear();
                room.AppendCellsTo(cells);
                for (int c = 0; c < cells.Count; c++)
                {
                    var center = new Vector3((cells[c].X + 0.5f) * GrayboxLayout.CellSize, room.Floor * GrayboxLayout.FloorHeight + 1f,
                        (cells[c].Z + 0.5f) * GrayboxLayout.CellSize);
                    Assert.AreEqual(id, index.RoomOf(center), room.Id + " cell " + cells[c]);
                }
            }
        }

        [Test]
        public void StairShaft_BelongsToTheStairwellOnBothFloors()
        {
            HouseIndex index = Index();
            GrayboxStairs stairs = Layout.Stairs[0];
            int stairwell = GrayboxMarkers.RoomIdOf(Layout.RoomAt(GrayboxLayout.GroundFloor, stairs.Start).Id);
            float x = (stairs.Start.X + 0.5f) * GrayboxLayout.CellSize;
            float z = (stairs.Start.Z + 0.5f) * GrayboxLayout.CellSize;

            Assert.AreEqual(stairwell, index.RoomOf(new Vector3(x, 0.5f, z)), "bottom of the stairs");
            Assert.AreEqual(stairwell, index.RoomOf(new Vector3(x, GrayboxLayout.FloorHeight - 0.1f, z)), "top of the stairs");
            Assert.AreEqual(stairwell, index.RoomOf(new Vector3(x, GrayboxLayout.FloorHeight + 1f, z)), "open shaft above");
        }

        [Test]
        public void PointsOutsideTheHouse_AreInNoRoom()
        {
            HouseIndex index = Index();

            Assert.AreEqual(-1, index.RoomOf(new Vector3(-200f, 1f, 0f)));
            Assert.AreEqual(-1, index.RoomOf(new Vector3(10f, -2f, 8f)), "below the ground");
            Assert.AreEqual(-1, index.RoomOf(new Vector3(10f, 30f, 8f)), "far above the roof");
        }

        [Test]
        public void StageZones_ListTheRoomsOfEachLayoutStage()
        {
            HouseIndex index = Index();

            for (int i = 0; i < Layout.Rooms.Count; i++)
            {
                GrayboxRoom room = Layout.Rooms[i];
                Assert.AreEqual(room.Stage, index.StageOfRoom(GrayboxMarkers.RoomIdOf(room.Id)), room.Id);
            }
            Assert.AreEqual(2, index.FindStage(0).RoomIds.Count, "garden + shed");
        }

        // ------------------------------------------------------------------------------------------------
        // point markers
        // ------------------------------------------------------------------------------------------------

        [Test]
        public void EveryRoomBoundMarker_StandsInsideTheRoomItNames()
        {
            HouseIndex index = Index();

            for (int i = 0; i < Markers.Specs.Count; i++)
            {
                GrayboxMarkerSpec spec = Markers.Specs[i];
                if (spec.Kind == GrayboxMarkerKind.Room || spec.Kind == GrayboxMarkerKind.StageZone || spec.Kind == GrayboxMarkerKind.StageGate)
                {
                    continue;
                }
                Assert.AreEqual(GrayboxMarkers.RoomIdOf(spec.RoomKey), index.RoomOf(spec.Position), spec.Path + " at " + spec.Position + " is not in room " + spec.RoomKey);
            }
        }

        [Test]
        public void EveryRewardPoint_IsInTheRoomOfItsPuzzleSlot()
        {
            HouseIndex index = Index();
            List<GrayboxMarkerSpec> slots = SpecsOf(GrayboxMarkerKind.PuzzleSlot);

            Assert.IsNotEmpty(slots);
            for (int i = 0; i < slots.Count; i++)
            {
                Assert.AreEqual(GrayboxMarkers.RoomIdOf(slots[i].RoomKey), index.RoomOf(slots[i].RewardPosition), slots[i].Path);
            }
        }

        [Test]
        public void MarkerPaths_AreUnique_AndNamesUniqueInsideTheirParent()
        {
            var paths = new HashSet<string>();
            for (int i = 0; i < Markers.Specs.Count; i++)
            {
                Assert.IsTrue(paths.Add(Markers.Specs[i].Path), "duplicate hierarchy path " + Markers.Specs[i].Path);
                Assert.IsNotEmpty(Markers.Specs[i].Name);
            }
        }

        [Test]
        public void MarkerPaths_FollowTheBuilderHierarchy()
        {
            GrayboxMarkerSpec hall = null;
            GrayboxMarkerSpec lamp = null;
            GrayboxMarkerSpec zone = null;
            for (int i = 0; i < Markers.Specs.Count; i++)
            {
                GrayboxMarkerSpec spec = Markers.Specs[i];
                if (spec.Kind == GrayboxMarkerKind.Room && spec.RoomKey == "hall") hall = spec;
                if (spec.Kind == GrayboxMarkerKind.Lamp && spec.RoomKey == "hall") lamp = spec;
                if (spec.Kind == GrayboxMarkerKind.StageZone && spec.Stage == 1) zone = spec;
            }

            Assert.AreEqual("Floor0/Room_Hall", hall.Path);
            Assert.AreEqual("Floor0/Room_Hall/Markers/Lamp_01", lamp.Path);
            Assert.AreEqual("Markers/Stage_1", zone.Path);
        }

        [Test]
        public void NetIds_AreDeterministic_AndInTheLevelRange()
        {
            HouseIndex first = GrayboxHouseIndexFactory.Build(GrayboxMarkers.Create(GrayboxLayout.Default));
            HouseIndex second = GrayboxHouseIndexFactory.BuildDefault();
            var a = new List<HouseNetEntry>();
            var b = new List<HouseNetEntry>();
            first.CollectNetEntries(a);
            second.CollectNetEntries(b);

            Assert.AreEqual(a.Count, b.Count);
            int lowest = int.MaxValue;
            int highest = int.MinValue;
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].Path, b[i].Path);
                Assert.AreEqual(a[i].NetId, b[i].NetId, a[i].Path);
                lowest = Math.Min(lowest, a[i].NetId.Value);
                highest = Math.Max(highest, a[i].NetId.Value);
            }
            Assert.AreEqual(NetId.LevelMin, lowest);
            Assert.AreEqual(NetId.LevelMin + a.Count - 1, highest, "ids are consecutive");
            Assert.Less(highest, NetId.RuntimeMin);
        }

        [Test]
        public void EveryIndoorRoom_HasALampAndASwitch_TheGardenHasNone()
        {
            HouseIndex index = Index();

            for (int i = 0; i < Layout.Rooms.Count; i++)
            {
                GrayboxRoom room = Layout.Rooms[i];
                int id = GrayboxMarkers.RoomIdOf(room.Id);
                int lamps = 0;
                int switches = 0;
                for (int s = 0; s < index.Lamps.Count; s++) if (index.Lamps[s].RoomId == id) lamps++;
                for (int s = 0; s < index.LightSwitches.Count; s++) if (index.LightSwitches[s].RoomId == id) switches++;

                if (room.IsOutdoor)
                {
                    Assert.AreEqual(0, lamps + switches, room.Id);
                }
                else
                {
                    Assert.GreaterOrEqual(lamps, 1, room.Id + " lamps");
                    Assert.GreaterOrEqual(switches, 1, room.Id + " switches");
                }
            }
        }

        [Test]
        public void LightSwitches_SitNextToADoorOfTheirRoom()
        {
            // "Next to the door": within 2.5 m of the centre of one of the room's doors (same floor).
            const float maxDistance = 2.5f;
            List<GrayboxMarkerSpec> switches = SpecsOf(GrayboxMarkerKind.LightSwitch);
            Assert.IsNotEmpty(switches);

            for (int i = 0; i < switches.Count; i++)
            {
                GrayboxMarkerSpec spec = switches[i];
                GrayboxRoom room = Layout.FindRoom(spec.RoomKey);
                float nearest = float.MaxValue;
                for (int d = 0; d < Layout.Doors.Count; d++)
                {
                    GrayboxDoor door = Layout.Doors[d];
                    if (door.Floor != room.Floor || (!room.Contains(door.CellA) && !room.Contains(door.CellB)))
                    {
                        continue;
                    }
                    var center = new Vector3(door.Edge.CenterCellX * GrayboxLayout.CellSize, 0f, door.Edge.CenterCellZ * GrayboxLayout.CellSize);
                    var flat = new Vector3(spec.Position.x, 0f, spec.Position.z);
                    nearest = Math.Min(nearest, Vector3.Distance(center, flat));
                }
                Assert.LessOrEqual(nearest, maxDistance, spec.Path + " is " + nearest + " m from the nearest door of " + spec.RoomKey);
            }
        }

        [Test]
        public void KeyHideSpots_StandOnTheGardenPropsThatCanHideAKey()
        {
            int expected = 0;
            for (int i = 0; i < Layout.Props.Count; i++)
            {
                if (Layout.Props[i].Kind != GrayboxPropKind.Tree)
                {
                    expected++;
                }
            }

            HouseIndex index = Index();

            Assert.AreEqual(expected, index.KeyHideSpots.Count);
            Assert.GreaterOrEqual(index.KeyHideSpots.Count, HouseContract.MinKeyHideSpots);
            for (int i = 0; i < index.KeyHideSpots.Count; i++)
            {
                Assert.AreEqual(HouseContract.GardenStage, index.StageOfRoom(index.ResolveRoom(index.KeyHideSpots[i])), index.KeyHideSpots[i].Path);
            }
        }

        [Test]
        public void CologneSpots_IncludeTheBathroom_AndStayInsideTheHouse()
        {
            HouseIndex index = Index();
            int bathroom = GrayboxMarkers.RoomIdOf("bathroom");
            bool inBathroom = false;

            for (int i = 0; i < index.CologneSpots.Count; i++)
            {
                inBathroom |= index.CologneSpots[i].RoomId == bathroom;
                Assert.GreaterOrEqual(index.StageOfRoom(index.CologneSpots[i].RoomId), 1, index.CologneSpots[i].Path);
            }
            Assert.IsTrue(inBathroom);
        }

        [Test]
        public void HumanSpawn_IsInTheFrontYard_FacingTheFrontDoor()
        {
            HouseIndex index = Index();
            GrayboxMarkerSpec spawn = SpecsOf(GrayboxMarkerKind.HumanSpawn)[0];
            GrayboxMarkerSpec gate = SpecsOf(GrayboxMarkerKind.StageGate)[0];

            Assert.AreEqual(GrayboxMarkers.RoomIdOf("garden"), index.RoomOf(spawn.Position));
            Assert.AreEqual(gate.Position.x, spawn.Position.x, 0.01f, "straight in front of the door");
            Assert.Less(spawn.Position.z, gate.Position.z, "south of the door (the door faces -Z)");
            Assert.AreEqual(GrayboxDirection.North.YawDegrees(), spawn.YawDegrees, "looks north, at the house");
        }

        [Test]
        public void JinnSpawns_AreInThreeDifferentHouseRooms()
        {
            HouseIndex index = Index();
            var rooms = new HashSet<int>();

            for (int i = 0; i < index.JinnSpawns.Count; i++)
            {
                int room = index.RoomOf(index.JinnSpawns[i]);
                Assert.GreaterOrEqual(index.StageOfRoom(room), 1);
                rooms.Add(room);
            }
            Assert.AreEqual(3, rooms.Count);
        }

        [Test]
        public void BlessedOil_LiesInTheShed()
        {
            HouseIndex index = Index();

            Assert.AreEqual(GrayboxMarkers.RoomIdOf("shed"), index.RoomOf(index.ToolShedItems[0].Position));
        }

        // ------------------------------------------------------------------------------------------------
        // gates
        // ------------------------------------------------------------------------------------------------

        [Test]
        public void Gates_AreTheFrontDoorAndTheStairGate()
        {
            HouseIndex index = Index();
            HouseGateEntry front = null;
            HouseGateEntry stairs = null;
            for (int i = 0; i < index.Gates.Count; i++)
            {
                if (index.Gates[i].StageIndex == 0) front = index.Gates[i];
                if (index.Gates[i].StageIndex == 1) stairs = index.Gates[i];
            }

            Assert.IsNotNull(front);
            Assert.IsNotNull(stairs);
            Assert.AreEqual("house_key", front.RequiredItemId);
            Assert.IsFalse(stairs.HasLock);

            GrayboxDoor frontDoor = FindDoor(GrayboxDoorKind.FrontDoor);
            GrayboxDoor stairDoor = FindDoor(GrayboxDoorKind.StageGate);
            Assert.AreEqual(frontDoor.Edge.CenterCellX * GrayboxLayout.CellSize, front.Position.x, 0.01f);
            Assert.AreEqual(frontDoor.Edge.CenterCellZ * GrayboxLayout.CellSize, front.Position.z, 0.01f);
            Assert.AreEqual(stairDoor.Edge.CenterCellX * GrayboxLayout.CellSize, stairs.Position.x, 0.01f);
            Assert.AreEqual(stairDoor.Edge.CenterCellZ * GrayboxLayout.CellSize, stairs.Position.z, 0.01f);
        }

        private static GrayboxDoor FindDoor(GrayboxDoorKind kind)
        {
            for (int i = 0; i < Layout.Doors.Count; i++)
            {
                if (Layout.Doors[i].Kind == kind)
                {
                    return Layout.Doors[i];
                }
            }
            return null;
        }

        [Test]
        public void GateSpecs_PointAtTheLayoutDoors()
        {
            List<GrayboxMarkerSpec> gates = SpecsOf(GrayboxMarkerKind.StageGate);

            Assert.AreEqual(2, gates.Count);
            for (int i = 0; i < gates.Count; i++)
            {
                GrayboxDoor door = Layout.FindDoor(gates[i].Edge);
                Assert.IsNotNull(door, gates[i].Path);
                Assert.IsTrue(door.Kind == GrayboxDoorKind.FrontDoor || door.Kind == GrayboxDoorKind.StageGate, gates[i].Path);
            }
        }

        // ------------------------------------------------------------------------------------------------
        // slots (what RoundSetupPlanner will need)
        // ------------------------------------------------------------------------------------------------

        [TestCase(1)]
        [TestCase(2)]
        public void EveryMvpPuzzleType_FitsAtLeastOneSlotOfEachPuzzleStage(int stage)
        {
            HouseIndex index = Index();
            PuzzleType[] types = { PuzzleType.Paintings, PuzzleType.Statue, PuzzleType.Wires, PuzzleType.Sigil };

            for (int t = 0; t < types.Length; t++)
            {
                bool fits = false;
                for (int i = 0; i < index.PuzzleSlots.Count; i++)
                {
                    fits |= index.PuzzleSlots[i].StageIndex == stage && index.PuzzleSlots[i].Allows(types[t]);
                }
                Assert.IsTrue(fits, types[t] + " fits no slot of stage " + stage);
            }
        }

        [TestCase(1)]
        [TestCase(2)]
        public void PuzzleSlotsOfAStage_MixSizesAndRooms(int stage)
        {
            HouseIndex index = Index();
            var sizes = new HashSet<PuzzleSlotSize>();
            var rooms = new HashSet<int>();

            for (int i = 0; i < index.PuzzleSlots.Count; i++)
            {
                if (index.PuzzleSlots[i].StageIndex == stage)
                {
                    sizes.Add(index.PuzzleSlots[i].Size);
                    rooms.Add(index.PuzzleSlots[i].RoomId);
                }
            }
            Assert.AreEqual(3, sizes.Count, "Wall, Floor and Table");
            Assert.GreaterOrEqual(rooms.Count, 3, "slots are in different rooms");
        }

        [Test]
        public void ReferenceSlots_OfferEverySizeOnBothFloors()
        {
            HouseIndex index = Index();
            var found = new HashSet<string>();

            for (int i = 0; i < index.ReferenceSlots.Count; i++)
            {
                HouseReferenceSlotEntry slot = index.ReferenceSlots[i];
                int floor = index.FindRoom(slot.RoomId).Floor;
                found.Add(slot.Size + "@" + floor);
                Assert.GreaterOrEqual(index.StageOfRoom(slot.RoomId), 1, slot.Path + " must be inside the house");
            }

            foreach (PuzzleSlotSize size in new[] { PuzzleSlotSize.Wall, PuzzleSlotSize.Floor, PuzzleSlotSize.Table })
            {
                Assert.IsTrue(found.Contains(size + "@0"), size + " reference on the ground floor");
                Assert.IsTrue(found.Contains(size + "@1"), size + " reference on the first floor");
            }
        }

        [Test]
        public void EveryPuzzleSlot_HasAMatchingReferenceSlotInAnotherRoom()
        {
            HouseIndex index = Index();

            for (int p = 0; p < index.PuzzleSlots.Count; p++)
            {
                HousePuzzleSlotEntry puzzle = index.PuzzleSlots[p];
                for (int t = 0; t < puzzle.AllowedTypes.Count; t++)
                {
                    bool found = false;
                    for (int r = 0; r < index.ReferenceSlots.Count; r++)
                    {
                        HouseReferenceSlotEntry reference = index.ReferenceSlots[r];
                        found |= reference.RoomId != puzzle.RoomId && reference.Size == puzzle.Size && reference.Allows(puzzle.AllowedTypes[t]);
                    }
                    Assert.IsTrue(found, puzzle.Path + " / " + puzzle.AllowedTypes[t] + " has no reference slot in another room");
                }
            }
        }

        // ------------------------------------------------------------------------------------------------
        // data errors
        // ------------------------------------------------------------------------------------------------

        [Test]
        public void ARoomMissingFromTheIdTable_FailsLoudly()
        {
            var sauna = new GrayboxRoom("sauna", "Sauna", 'X', GrayboxLayout.GroundFloor, 1, false, GrayboxFloorStyle.Wood, new GrayboxRect(0, 0, 1, 1));
            var layout = new GrayboxLayout(new[] { sauna }, new GrayboxDoor[0], new GrayboxRailing[0], new GrayboxStairs[0], new GrayboxProp[0]);

            var e = Assert.Throws<InvalidOperationException>(() => GrayboxMarkers.Create(layout));
            StringAssert.Contains("sauna", e.Message);
        }

        [Test]
        public void ForDefaultLayout_ReturnsTheSharedInstance()
        {
            Assert.AreSame(GrayboxMarkers.Default, GrayboxMarkers.For(GrayboxLayout.Default));
        }
    }
}
