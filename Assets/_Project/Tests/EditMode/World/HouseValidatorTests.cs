using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.World;
using NUnit.Framework;
using UnityEngine;

namespace CinliMahzen.Tests.EditMode.World
{
    /// <summary>
    /// TODO 2.2: <see cref="HouseValidator"/>. Every test starts from <see cref="HouseFixture.Valid"/> (a house that satisfies the
    /// whole contract except the two things that arrive later) and breaks exactly one rule.
    /// </summary>
    public class HouseValidatorTests
    {
        private static List<HouseIssue> Run(HouseFixture fixture)
        {
            return HouseValidator.Validate(fixture.Build());
        }

        private static bool Has(List<HouseIssue> issues, HouseIssueSeverity severity, string fragment)
        {
            for (int i = 0; i < issues.Count; i++)
            {
                if (issues[i].Severity == severity && issues[i].Message.Contains(fragment))
                {
                    return true;
                }
            }
            return false;
        }

        private static string Dump(List<HouseIssue> issues)
        {
            if (issues.Count == 0)
            {
                return "(no issues)";
            }
            var lines = new List<string>();
            for (int i = 0; i < issues.Count; i++)
            {
                lines.Add(issues[i].ToString());
            }
            return "validator said:\n  " + string.Join("\n  ", lines);
        }

        private static void AssertError(List<HouseIssue> issues, string fragment)
        {
            Assert.IsTrue(Has(issues, HouseIssueSeverity.Error, fragment), "expected an error containing \"" + fragment + "\"; " + Dump(issues));
        }

        private static void AssertWarning(List<HouseIssue> issues, string fragment)
        {
            Assert.IsTrue(Has(issues, HouseIssueSeverity.Warning, fragment), "expected a warning containing \"" + fragment + "\"; " + Dump(issues));
        }

        private static void AssertNoErrors(List<HouseIssue> issues)
        {
            Assert.AreEqual(0, HouseValidator.CountErrors(issues), Dump(issues));
        }

        private static HouseSpotEntry SpotWithRoom(HouseFixture f, string name, string type, HouseSpotKind kind, int roomId, Vector3 position, string itemId = "")
        {
            return new HouseSpotEntry(f.NextPath(name, type), kind, roomId, itemId, position);
        }

        // ------------------------------------------------------------------------------------------------
        // the valid house
        // ------------------------------------------------------------------------------------------------

        [Test]
        public void ValidHouse_HasNoErrors_AndOnlyTheTwoComingLaterWarnings()
        {
            List<HouseIssue> issues = Run(HouseFixture.Valid());

            AssertNoErrors(issues);
            Assert.AreEqual(2, HouseValidator.CountWarnings(issues), Dump(issues));
            AssertWarning(issues, "DigSpots");
            AssertWarning(issues, "no cellar hatch");
        }

        [Test]
        public void NullIndex_IsAnError()
        {
            AssertError(HouseValidator.Validate(null), "null");
        }

        [Test]
        public void ContractNumbers_MatchTheTechDocSection8()
        {
            Assert.AreEqual(3, HouseContract.MinPuzzleSlotsPerStage);
            Assert.AreEqual(8, HouseContract.MinReferenceSlots);
            Assert.AreEqual(5, HouseContract.MinKeyHideSpots);
            Assert.AreEqual(3, HouseContract.MinCologneSpawnSpots);
            Assert.AreEqual(3, HouseContract.MinDigSpots);
            Assert.AreEqual(1, HouseContract.HumanSpawnCount);
            Assert.AreEqual(3, HouseContract.JinnSpawnCount);
            Assert.AreEqual("house_key", HouseContract.FrontDoorItemId);
            Assert.AreEqual("blessed_oil", HouseContract.BlessedOilItemId);
        }

        // ------------------------------------------------------------------------------------------------
        // rooms and stages
        // ------------------------------------------------------------------------------------------------

        [Test]
        public void EmptyHouse_ReportsMissingRoomsAndStages()
        {
            List<HouseIssue> issues = HouseValidator.Validate(new HouseIndexBuilder().Build());

            AssertError(issues, "no RoomMarker");
            AssertError(issues, "no StageZone");
        }

        [Test]
        public void DuplicateRoomId_IsAnError()
        {
            HouseFixture f = HouseFixture.Valid();
            f.Rooms[2] = new HouseRoomEntry(HouseFixture.HallId, "kitchen", 0, false, f.Rooms[2].Boxes);

            AssertError(Run(f), "duplicate room id");
        }

        [Test]
        public void DuplicateRoomKey_IsAnError()
        {
            HouseFixture f = HouseFixture.Valid();
            f.Rooms[2] = new HouseRoomEntry(HouseFixture.KitchenId, "hall", 0, false, f.Rooms[2].Boxes);

            AssertError(Run(f), "duplicate room key");
        }

        [Test]
        public void NegativeRoomId_IsAnError()
        {
            HouseFixture f = HouseFixture.Valid();
            f.Rooms.Add(new HouseRoomEntry(-1, "outside", 0, false, new[] { HouseFixture.Box(50, 0, 50, 60, 4, 60) }));

            AssertError(Run(f), "negative id");
        }

        [Test]
        public void RoomWithoutBoxes_IsAnError()
        {
            HouseFixture f = HouseFixture.Valid();
            f.Rooms[1] = new HouseRoomEntry(HouseFixture.HallId, "hall", 0, false, new Bounds[0]);

            AssertError(Run(f), "no usable box");
        }

        [Test]
        public void RoomBoxWithoutVolume_IsAnError()
        {
            HouseFixture f = HouseFixture.Valid();
            f.Rooms[1] = new HouseRoomEntry(HouseFixture.HallId, "hall", 0, false, new[] { new Bounds(new Vector3(5f, 2f, 5f), Vector3.zero) });

            AssertError(Run(f), "no volume");
        }

        [Test]
        public void RoomFloorOutOfRange_IsAnError()
        {
            HouseFixture f = HouseFixture.Valid();
            f.Rooms[1] = new HouseRoomEntry(HouseFixture.HallId, "hall", 7, false, f.Rooms[1].Boxes);

            AssertError(Run(f), "floor 7");
        }

        [Test]
        public void OverlappingRooms_AreAWarning_TouchingRoomsAreNot()
        {
            HouseFixture f = HouseFixture.Valid();
            AssertNoWarningContaining(Run(f), "overlap");

            f.Rooms.Add(new HouseRoomEntry(9, "closet", 0, false, new[] { HouseFixture.Box(2, 0, 2, 4, 4, 4) }));
            f.Stages[1] = new HouseStageEntry(1, new[] { HouseFixture.HallId, HouseFixture.KitchenId, 9 });

            AssertWarning(Run(f), "overlap");
        }

        private static void AssertNoWarningContaining(List<HouseIssue> issues, string fragment)
        {
            Assert.IsFalse(Has(issues, HouseIssueSeverity.Warning, fragment), Dump(issues));
        }

        [Test]
        public void StageIndexGap_IsAnError()
        {
            HouseFixture f = HouseFixture.Valid();
            f.Stages[1] = new HouseStageEntry(3, new[] { HouseFixture.HallId, HouseFixture.KitchenId });

            AssertError(Run(f), "stage 1 is missing");
        }

        [Test]
        public void MissingStageZeroOrDuplicateStage_IsAnError()
        {
            HouseFixture f = HouseFixture.Valid();
            f.Stages[0] = new HouseStageEntry(1, new[] { HouseFixture.GardenId });

            List<HouseIssue> issues = Run(f);
            AssertError(issues, "duplicate StageZone index 1");
            AssertError(issues, "stage 0 is missing");
        }

        [Test]
        public void StageListingAnUnknownRoom_IsAnError()
        {
            HouseFixture f = HouseFixture.Valid();
            f.Stages[2] = new HouseStageEntry(2, new[] { HouseFixture.UpstairsId, HouseFixture.StudyId, 42 });

            AssertError(Run(f), "unknown room id 42");
        }

        [Test]
        public void RoomInTwoStages_IsAnError()
        {
            HouseFixture f = HouseFixture.Valid();
            f.Stages[2] = new HouseStageEntry(2, new[] { HouseFixture.UpstairsId, HouseFixture.StudyId, HouseFixture.HallId });

            AssertError(Run(f), "more than one StageZone");
        }

        [Test]
        public void RoomInNoStage_IsAnError()
        {
            HouseFixture f = HouseFixture.Valid();
            f.Stages[2] = new HouseStageEntry(2, new[] { HouseFixture.UpstairsId });

            AssertError(Run(f), "not in any StageZone");
        }

        // ------------------------------------------------------------------------------------------------
        // puzzle and reference slots
        // ------------------------------------------------------------------------------------------------

        [Test]
        public void FewerThanThreePuzzleSlotsInAStage_IsAnError()
        {
            HouseFixture f = HouseFixture.Valid();
            f.PuzzleSlots.RemoveAt(0);

            List<HouseIssue> issues = Run(f);
            AssertError(issues, "stage 1 has 2 PuzzleSlots (minimum 3)");
            Assert.IsFalse(Has(issues, HouseIssueSeverity.Error, "stage 2 has"), Dump(issues));
        }

        [Test]
        public void TheGardenStageNeedsNoPuzzleSlots()
        {
            AssertNoErrors(Run(HouseFixture.Valid()));
            Assert.AreEqual(0, CountSlotsOfStage(HouseFixture.Valid(), 0));
        }

        private static int CountSlotsOfStage(HouseFixture f, int stage)
        {
            int count = 0;
            for (int i = 0; i < f.PuzzleSlots.Count; i++)
            {
                if (f.PuzzleSlots[i].StageIndex == stage)
                {
                    count++;
                }
            }
            return count;
        }

        [Test]
        public void PuzzleSlotsAllInOneRoom_AreAWarning()
        {
            HouseFixture f = HouseFixture.Valid();
            f.PuzzleSlots.Clear();
            f.AddPuzzleSlot(1, HouseFixture.HallId);
            f.AddPuzzleSlot(1, HouseFixture.HallId);
            f.AddPuzzleSlot(1, HouseFixture.HallId);
            f.AddPuzzleSlot(2, HouseFixture.UpstairsId);
            f.AddPuzzleSlot(2, HouseFixture.StudyId);
            f.AddPuzzleSlot(2, HouseFixture.UpstairsId);

            List<HouseIssue> issues = Run(f);

            AssertNoErrors(issues);
            AssertWarning(issues, "all PuzzleSlots of stage 1 are in one room");
        }

        [Test]
        public void PuzzleSlotInAnUnknownRoom_IsAnError()
        {
            HouseFixture f = HouseFixture.Valid();
            f.AddPuzzleSlot(1, 42);

            AssertError(Run(f), "unknown room id 42");
        }

        [Test]
        public void PuzzleSlotInARoomOfAnotherStage_IsAnError()
        {
            HouseFixture f = HouseFixture.Valid();
            f.PuzzleSlots[0] = new HousePuzzleSlotEntry(f.NextPath("PuzzleSlot", "PuzzleSlot"), 1, HouseFixture.UpstairsId, PuzzleSlotSize.Wall,
                HouseFixture.AnyWallTypes, HouseFixture.Inside(HouseFixture.UpstairsId, 2f, 1.5f, 5f), Vector3.zero);

            AssertError(Run(f), "is not a room of stage 1");
        }

        [Test]
        public void PuzzleSlotOfAStageWithoutZone_IsAnError()
        {
            HouseFixture f = HouseFixture.Valid();
            f.AddPuzzleSlot(9, HouseFixture.HallId);

            AssertError(Run(f), "stage 9, which has no StageZone");
        }

        [Test]
        public void PuzzleSlotWithoutAllowedTypes_IsAnError()
        {
            HouseFixture f = HouseFixture.Valid();
            f.PuzzleSlots[0] = new HousePuzzleSlotEntry(f.NextPath("PuzzleSlot", "PuzzleSlot"), 1, HouseFixture.HallId, PuzzleSlotSize.Wall,
                new PuzzleType[0], HouseFixture.Inside(HouseFixture.HallId, 2f, 1.5f, 5f), Vector3.zero);

            AssertError(Run(f), "no allowed puzzle types");
        }

        [Test]
        public void PuzzleSlotOutsideItsRoom_IsAWarning()
        {
            HouseFixture f = HouseFixture.Valid();
            f.PuzzleSlots[0] = new HousePuzzleSlotEntry(f.NextPath("PuzzleSlot", "PuzzleSlot"), 1, HouseFixture.HallId, PuzzleSlotSize.Wall,
                HouseFixture.AnyWallTypes, HouseFixture.Inside(HouseFixture.KitchenId, 2f, 1.5f, 5f), Vector3.zero);

            List<HouseIssue> issues = Run(f);

            AssertNoErrors(issues);
            AssertWarning(issues, "is not inside its room 'hall'");
        }

        [Test]
        public void FewerThanEightReferenceSlots_IsAnError()
        {
            HouseFixture f = HouseFixture.Valid();
            f.ReferenceSlots.RemoveAt(0);

            AssertError(Run(f), "7 ReferenceSlots (minimum 8)");
        }

        [Test]
        public void ReferenceSlotInAnUnknownRoom_IsAnError()
        {
            HouseFixture f = HouseFixture.Valid();
            f.AddReferenceSlot(42);

            AssertError(Run(f), "unknown room id 42");
        }

        [Test]
        public void ReferenceSlotWithoutAllowedTypes_IsAnError()
        {
            HouseFixture f = HouseFixture.Valid();
            f.ReferenceSlots[0] = new HouseReferenceSlotEntry(f.NextPath("ReferenceSlot", "ReferenceSlot"), HouseFixture.HallId, PuzzleSlotSize.Wall,
                new PuzzleType[0], HouseFixture.Inside(HouseFixture.HallId, 2f, 1.5f, 8f));

            AssertError(Run(f), "no allowed puzzle types");
        }

        // ------------------------------------------------------------------------------------------------
        // gates
        // ------------------------------------------------------------------------------------------------

        [Test]
        public void MissingGateOfAMiddleStage_IsAnError_TheLastStageNeedsNone()
        {
            HouseFixture f = HouseFixture.Valid();
            f.Gates.RemoveAt(1); // stage 1

            List<HouseIssue> issues = Run(f);

            AssertError(issues, "stage 1 has no StageGate");
            for (int i = 0; i < issues.Count; i++)
            {
                StringAssert.DoesNotContain("stage 2 has no StageGate", issues[i].Message, "the cellar follows the last stage");
            }
        }

        // ---------- cellar hatch (TODO 4.1) ----------

        private static HouseGateEntry Hatch(HouseFixture f, string item)
        {
            return new HouseGateEntry(f.NextPath("StageGate", "StageGate"), 1, item, Vector3.zero, default, true);
        }

        [Test]
        public void CellarHatch_WithTheCellarKey_RemovesTheNoHatchWarning()
        {
            HouseFixture f = HouseFixture.Valid();
            f.Gates.Add(Hatch(f, HouseContract.CellarKeyItemId));

            List<HouseIssue> issues = Run(f);

            AssertNoErrors(issues);
            for (int i = 0; i < issues.Count; i++)
            {
                StringAssert.DoesNotContain("no cellar hatch", issues[i].Message);
            }
        }

        [Test]
        public void CellarHatch_NeedsTheCellarKey_AndThereIsOnlyOne()
        {
            HouseFixture f = HouseFixture.Valid();
            f.Gates.Add(Hatch(f, "house_key"));
            f.Gates.Add(Hatch(f, HouseContract.CellarKeyItemId));

            List<HouseIssue> issues = Run(f);

            AssertError(issues, "must require 'cellar_key'");
            AssertError(issues, "more than one cellar hatch");
        }

        [Test]
        public void CellarRoom_NeedsNoStageZone_OtherRoomsDo()
        {
            HouseFixture f = HouseFixture.Valid();
            f.Rooms.Add(new HouseRoomEntry(9, "cellar", HouseContract.CellarFloor, false, new[] { HouseFixture.Box(0f, -4f, 0f, 20f, 0f, 10f) }));
            f.Rooms.Add(new HouseRoomEntry(10, "loft", 2, false, new[] { HouseFixture.Box(0f, 8f, 0f, 20f, 12f, 10f) }));

            List<HouseIssue> issues = Run(f);

            AssertError(issues, "loft");
            for (int i = 0; i < issues.Count; i++)
            {
                Assert.IsFalse(issues[i].Message.Contains("cellar") && issues[i].Message.Contains("StageZone"), issues[i].Message);
            }
        }

        [Test]
        public void FrontDoorWithoutTheHouseKeyLock_IsAnError()
        {
            HouseFixture f = HouseFixture.Valid();
            f.Gates[0] = new HouseGateEntry(f.NextPath("StageGate", "StageGate"), 0, string.Empty, Vector3.zero);

            AssertError(Run(f), "must require 'house_key'");
        }

        [Test]
        public void FrontDoorWithTheWrongKey_IsAnError()
        {
            HouseFixture f = HouseFixture.Valid();
            f.Gates[0] = new HouseGateEntry(f.NextPath("StageGate", "StageGate"), 0, "cellar_key", Vector3.zero);

            AssertError(Run(f), "requires 'cellar_key'");
        }

        [Test]
        public void TwoGatesForOneStage_IsAnError()
        {
            HouseFixture f = HouseFixture.Valid();
            f.Gates.Add(new HouseGateEntry(f.NextPath("StageGate", "StageGate"), 1, string.Empty, Vector3.zero));

            AssertError(Run(f), "stage 1 has more than one StageGate");
        }

        [Test]
        public void GateOfAStageWithoutZone_IsAnError()
        {
            HouseFixture f = HouseFixture.Valid();
            f.Gates.Add(new HouseGateEntry(f.NextPath("StageGate", "StageGate"), 7, string.Empty, Vector3.zero));

            AssertError(Run(f), "stage 7, which has no StageZone");
        }

        [Test]
        public void GateLockWithAnUnknownItem_IsAnErrorWhenItemIdsAreGiven()
        {
            HouseFixture f = HouseFixture.Valid();
            f.Gates[1] = new HouseGateEntry(f.NextPath("StageGate", "StageGate"), 1, "banana", Vector3.zero);
            HouseIndex index = f.Build();

            Assert.IsFalse(Has(HouseValidator.Validate(index), HouseIssueSeverity.Error, "banana"), "no item list: the check is skipped");
            AssertError(HouseValidator.Validate(index, new HashSet<string> { "house_key", "blessed_oil" }), "unknown item 'banana'");
        }

        // ------------------------------------------------------------------------------------------------
        // lamps and switches
        // ------------------------------------------------------------------------------------------------

        [Test]
        public void IndoorRoomWithoutLamp_IsAnError()
        {
            HouseFixture f = HouseFixture.Valid();
            f.Lamps.RemoveAll(l => l.RoomId == HouseFixture.KitchenId);

            AssertError(Run(f), "kitchen (#2, floor 0) has no LampMarker");
        }

        [Test]
        public void IndoorRoomWithoutSwitch_IsAnError()
        {
            HouseFixture f = HouseFixture.Valid();
            f.Switches.RemoveAll(s => s.RoomId == HouseFixture.StudyId);

            AssertError(Run(f), "study (#4, floor 1) has no LightSwitch");
        }

        [Test]
        public void OutdoorRoom_NeedsNeitherLampNorSwitch()
        {
            HouseFixture f = HouseFixture.Valid();
            Assert.IsTrue(f.Rooms[0].IsOutdoor);
            Assert.AreEqual(0, f.Lamps.FindAll(l => l.RoomId == HouseFixture.GardenId).Count);

            AssertNoErrors(Run(f));
        }

        [Test]
        public void LampAndSwitchInAnUnknownRoom_AreErrors()
        {
            HouseFixture f = HouseFixture.Valid();
            f.AddLamp(42);
            f.AddSwitch(43);

            List<HouseIssue> issues = Run(f);
            AssertError(issues, "LampMarker");
            AssertError(issues, "unknown room id 42");
            AssertError(issues, "unknown room id 43");
        }

        [Test]
        public void LampOutsideItsRoom_IsAWarning()
        {
            HouseFixture f = HouseFixture.Valid();
            f.Lamps[0] = SpotWithRoom(f, "Lamp", "LampMarker", HouseSpotKind.Lamp, f.Lamps[0].RoomId, new Vector3(500f, 1f, 500f));

            List<HouseIssue> issues = Run(f);

            AssertNoErrors(issues);
            AssertWarning(issues, "LampMarker");
        }

        // ------------------------------------------------------------------------------------------------
        // key hide spots, cologne, dig spots
        // ------------------------------------------------------------------------------------------------

        [Test]
        public void FewerThanFiveKeyHideSpotsInTheGarden_IsAnError()
        {
            HouseFixture f = HouseFixture.Valid();
            f.KeyHides.RemoveAt(0);

            AssertError(Run(f), "garden has 4 KeyHideSpots (minimum 5)");
        }

        [Test]
        public void KeyHideSpotInsideTheHouse_IsAWarning_AndDoesNotCount()
        {
            HouseFixture f = HouseFixture.Valid();
            f.KeyHides[0] = SpotWithRoom(f, "KeyHide", "KeyHideSpot", HouseSpotKind.KeyHide, HouseIndex.NoRoom,
                HouseFixture.Inside(HouseFixture.HallId, 5f, 1f, 5f));

            List<HouseIssue> issues = Run(f);

            AssertWarning(issues, "is not in a garden room");
            AssertError(issues, "garden has 4 KeyHideSpots");
        }

        [Test]
        public void FewerThanThreeCologneSpots_IsAnError()
        {
            HouseFixture f = HouseFixture.Valid();
            f.Colognes.RemoveAt(0);

            AssertError(Run(f), "house has 2 CologneSpawnSpots (minimum 3)");
        }

        [Test]
        public void CologneSpotInTheGarden_IsAnError()
        {
            HouseFixture f = HouseFixture.Valid();
            f.Colognes[0] = SpotWithRoom(f, "Cologne", "CologneSpawnSpot", HouseSpotKind.Cologne, HouseFixture.GardenId,
                HouseFixture.Inside(HouseFixture.GardenId, 5f, 1f, 5f));

            List<HouseIssue> issues = Run(f);

            AssertError(issues, "is not inside the house");
            AssertError(issues, "house has 2 CologneSpawnSpots");
        }

        [Test]
        public void CologneSpotInAnUnknownRoom_IsAnError()
        {
            HouseFixture f = HouseFixture.Valid();
            f.Colognes[0] = SpotWithRoom(f, "Cologne", "CologneSpawnSpot", HouseSpotKind.Cologne, 42, Vector3.zero);

            AssertError(Run(f), "unknown room id 42");
        }

        [Test]
        public void DigSpotsInTheCellar_RemoveTheWarning()
        {
            HouseFixture f = HouseFixture.Valid();
            f.Rooms.Add(new HouseRoomEntry(5, "cellar", -1, false, new[] { HouseFixture.Box(0, -4, 0, 10, 0, 10) }));
            f.Stages[2] = new HouseStageEntry(2, new[] { HouseFixture.UpstairsId, HouseFixture.StudyId, 5 });
            f.AddLamp(5);
            f.AddSwitch(5);
            for (int i = 0; i < HouseContract.MinDigSpots; i++)
            {
                f.Digs.Add(SpotWithRoom(f, "Dig", "DigSpot", HouseSpotKind.Dig, HouseIndex.NoRoom, new Vector3(2f + i, -3f, 5f)));
            }

            List<HouseIssue> issues = Run(f);

            AssertNoErrors(issues);
            Assert.IsFalse(Has(issues, HouseIssueSeverity.Warning, "DigSpots"), Dump(issues));
        }

        [Test]
        public void TooFewDigSpotsInTheCellar_IsOnlyAWarning()
        {
            HouseFixture f = HouseFixture.Valid();
            f.Rooms.Add(new HouseRoomEntry(5, "cellar", -1, false, new[] { HouseFixture.Box(0, -4, 0, 10, 0, 10) }));
            f.Stages[2] = new HouseStageEntry(2, new[] { HouseFixture.UpstairsId, HouseFixture.StudyId, 5 });
            f.AddLamp(5);
            f.AddSwitch(5);
            f.Digs.Add(SpotWithRoom(f, "Dig", "DigSpot", HouseSpotKind.Dig, HouseIndex.NoRoom, new Vector3(2f, -3f, 5f)));

            List<HouseIssue> issues = Run(f);

            AssertNoErrors(issues);
            AssertWarning(issues, "cellar has 1 DigSpots (minimum 3)");
        }

        [Test]
        public void DigSpotOutsideTheCellar_IsAnError()
        {
            HouseFixture f = HouseFixture.Valid();
            f.Digs.Add(SpotWithRoom(f, "Dig", "DigSpot", HouseSpotKind.Dig, HouseIndex.NoRoom, HouseFixture.Inside(HouseFixture.GardenId, 5f, 0f, 5f)));

            AssertError(Run(f), "not inside a cellar room");
        }

        // ------------------------------------------------------------------------------------------------
        // spawns and tool shed
        // ------------------------------------------------------------------------------------------------

        [Test]
        public void NoOrTwoHumanSpawns_IsAnError()
        {
            HouseFixture none = HouseFixture.Valid();
            none.HumanSpawns.Clear();
            AssertError(Run(none), "exactly 1 HumanSpawnMarker, found 0");

            HouseFixture two = HouseFixture.Valid();
            two.HumanSpawns.Add(HouseFixture.Inside(HouseFixture.GardenId, 4f, 0.1f, 4f));
            AssertError(Run(two), "exactly 1 HumanSpawnMarker, found 2");
        }

        [Test]
        public void HumanSpawnInsideTheHouse_IsAnError()
        {
            HouseFixture f = HouseFixture.Valid();
            f.HumanSpawns[0] = HouseFixture.Inside(HouseFixture.HallId, 5f, 0.1f, 5f);

            AssertError(Run(f), "HumanSpawnMarker");
        }

        [Test]
        public void WrongNumberOfJinnSpawns_IsAnError()
        {
            HouseFixture f = HouseFixture.Valid();
            f.JinnSpawns.RemoveAt(0);

            AssertError(Run(f), "exactly 3 JinnSpawnMarkers, found 2");
        }

        [Test]
        public void JinnSpawnInTheGardenOrOutside_IsAnError()
        {
            HouseFixture f = HouseFixture.Valid();
            f.JinnSpawns[0] = HouseFixture.Inside(HouseFixture.GardenId, 5f, 1.6f, 5f);
            f.JinnSpawns[1] = new Vector3(500f, 1f, 500f);

            List<HouseIssue> issues = Run(f);

            int errors = 0;
            for (int i = 0; i < issues.Count; i++)
            {
                if (issues[i].Severity == HouseIssueSeverity.Error && issues[i].Message.Contains("JinnSpawnMarker at"))
                {
                    errors++;
                }
            }
            Assert.AreEqual(2, errors, Dump(issues));
        }

        [Test]
        public void JinnSpawnsSharingARoom_AreAWarning()
        {
            HouseFixture f = HouseFixture.Valid();
            f.JinnSpawns[1] = HouseFixture.Inside(HouseFixture.HallId, 6f, 1.6f, 6f);

            List<HouseIssue> issues = Run(f);

            AssertNoErrors(issues);
            AssertWarning(issues, "share a room");
        }

        [Test]
        public void ToolShedWithoutTheBlessedOil_IsAnError()
        {
            HouseFixture f = HouseFixture.Valid();
            f.ToolShed.Clear();

            AssertError(Run(f), "exactly 1 ToolShedItemPoint with 'blessed_oil', found 0");
        }

        [Test]
        public void TwoBlessedOilPoints_IsAnError()
        {
            HouseFixture f = HouseFixture.Valid();
            f.ToolShed.Add(SpotWithRoom(f, "ToolShedItem", "ToolShedItemPoint", HouseSpotKind.ToolShedItem, HouseIndex.NoRoom,
                HouseFixture.Inside(HouseFixture.GardenId, 16f, 1f, 15f), HouseContract.BlessedOilItemId));

            AssertError(Run(f), "found 2");
        }

        [Test]
        public void ToolShedPointWithoutItemOrOutsideTheGarden_IsAnError()
        {
            HouseFixture f = HouseFixture.Valid();
            f.ToolShed.Add(SpotWithRoom(f, "ToolShedItem", "ToolShedItemPoint", HouseSpotKind.ToolShedItem, HouseIndex.NoRoom,
                HouseFixture.Inside(HouseFixture.HallId, 5f, 1f, 5f), "paint"));
            f.ToolShed.Add(SpotWithRoom(f, "ToolShedItem", "ToolShedItemPoint", HouseSpotKind.ToolShedItem, HouseIndex.NoRoom,
                HouseFixture.Inside(HouseFixture.GardenId, 17f, 1f, 15f), ""));

            List<HouseIssue> issues = Run(f);

            AssertError(issues, "is not in a stage 0 room");
            AssertError(issues, "has no itemId");
        }

        [Test]
        public void ToolShedItemUnknownToTheItemList_IsAnError()
        {
            HouseFixture f = HouseFixture.Valid();
            HouseIndex index = f.Build();

            AssertError(HouseValidator.Validate(index, new HashSet<string> { "house_key" }), "unknown item 'blessed_oil'");
            AssertNoErrors(HouseValidator.Validate(index, new HashSet<string> { "house_key", "blessed_oil" }));
        }

        // ------------------------------------------------------------------------------------------------
        // NetIds
        // ------------------------------------------------------------------------------------------------

        [Test]
        public void TwoEntriesWithTheSameNetId_AreAnError()
        {
            HouseFixture f = HouseFixture.Valid();
            f.Lamps[0] = new HouseSpotEntry(f.NextPath("Lamp", "LampMarker"), HouseSpotKind.Lamp, f.Lamps[0].RoomId, "", f.Lamps[0].Position, new NetId(5000));
            f.Switches[0] = new HouseSpotEntry(f.NextPath("LightSwitch", "LightSwitch"), HouseSpotKind.LightSwitch, f.Switches[0].RoomId, "",
                f.Switches[0].Position, new NetId(5000));

            AssertError(Run(f), "NetId 5000 is used by both");
        }

        [Test]
        public void NetIdOutsideTheLevelRange_IsAnError()
        {
            HouseFixture f = HouseFixture.Valid();
            f.Lamps[0] = new HouseSpotEntry(f.NextPath("Lamp", "LampMarker"), HouseSpotKind.Lamp, f.Lamps[0].RoomId, "", f.Lamps[0].Position, new NetId(5));
            f.Lamps[1] = new HouseSpotEntry(f.NextPath("Lamp", "LampMarker"), HouseSpotKind.Lamp, f.Lamps[1].RoomId, "", f.Lamps[1].Position, new NetId(60000));

            List<HouseIssue> issues = Run(f);

            AssertError(issues, "NetId 5 outside the level range 1000..59999");
            AssertError(issues, "NetId 60000 outside the level range");
        }

        [Test]
        public void BuilderAssignedNetIds_NeverCollide()
        {
            HouseIndex index = HouseFixture.Valid().Build();
            var all = new List<HouseNetEntry>();
            index.CollectNetEntries(all);

            var seen = new HashSet<int>();
            for (int i = 0; i < all.Count; i++)
            {
                Assert.IsTrue(all[i].NetId.Value >= NetId.LevelMin && all[i].NetId.Value < NetId.RuntimeMin, all[i].Path);
                Assert.IsTrue(seen.Add(all[i].NetId.Value), "duplicate " + all[i].NetId);
            }
            Assert.Greater(all.Count, 30);
        }

        // ------------------------------------------------------------------------------------------------
        // helpers of the validator itself
        // ------------------------------------------------------------------------------------------------

        [Test]
        public void IssueCounts_SplitBySeverity()
        {
            var issues = new List<HouseIssue>
            {
                new HouseIssue(HouseIssueSeverity.Error, "a"),
                new HouseIssue(HouseIssueSeverity.Warning, "b"),
                new HouseIssue(HouseIssueSeverity.Error, "c")
            };

            Assert.AreEqual(2, HouseValidator.CountErrors(issues));
            Assert.AreEqual(1, HouseValidator.CountWarnings(issues));
            Assert.AreEqual("[Warning] b", issues[1].ToString());
        }
    }
}
