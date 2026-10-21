using System;
using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.Objectives;
using CinliMahzen.World;
using CinliMahzen.World.Graybox;
using NUnit.Framework;

namespace CinliMahzen.Tests.EditMode.World
{
    /// <summary>
    /// TODO 2.3: <see cref="RoundSetupPlanner"/> on the real graybox house (<see cref="GrayboxHouseIndexFactory"/>) with the MVP pool
    /// (<see cref="MvpPuzzleCatalog"/>) and the default config (2 stages x 2 puzzles). Seeds 1..500 are planned once; every test
    /// checks one rule (Teknik 7.5, Oyun 3.1, 4, 6, 9) over all of them.
    /// </summary>
    public class RoundSetupPlannerTests
    {
        private const int FirstSeed = 1;
        private const int SeedCount = 500;
        private const int DeterminismSeeds = 50;

        private readonly PuzzlePoolFixture _fixture = new PuzzlePoolFixture();
        private HouseIndex _house;
        private List<PuzzleDefinition> _pool;
        private PlannerSettings _settings;
        private List<RoundPlan> _plans;
        private List<string> _planErrors;

        [OneTimeSetUp]
        public void PlanAllSeeds()
        {
            _house = GrayboxHouseIndexFactory.BuildDefault();
            _pool = _fixture.Mvp();
            _settings = PlannerSettings.FromConfig(_fixture.DefaultConfig());
            _plans = new List<RoundPlan>();
            _planErrors = new List<string>();

            var planner = new RoundSetupPlanner();
            for (int seed = FirstSeed; seed < FirstSeed + SeedCount; seed++)
            {
                RoundPlan plan;
                string error;
                if (planner.TryPlan(_house, _pool, _settings, seed, out plan, out error))
                {
                    _plans.Add(plan);
                }
                else
                {
                    _planErrors.Add("seed " + seed + ": " + error);
                }
            }
        }

        [OneTimeTearDown]
        public void DestroyDefinitions()
        {
            _fixture.DestroyAll();
        }

        private PuzzleDefinition DefinitionOf(PlannedPuzzle puzzle)
        {
            return _pool[puzzle.DefinitionIndex];
        }

        private string Describe(RoundPlan plan)
        {
            return RoundPlanFormatter.Format(plan, _house, _pool);
        }

        // ---------- validity ----------

        [Test]
        public void DefaultConfig_IsTwoStagesOfTwoPuzzles()
        {
            Assert.AreEqual(2, _settings.StagesActive);
            Assert.AreEqual(2, _settings.PuzzlesPerStage);
            Assert.AreEqual(PlannerSettings.DefaultShovelItemId, _settings.ShovelItemId);
        }

        [Test]
        public void EverySeed_IsPlanned_AndValidatorClean()
        {
            Assert.IsEmpty(_planErrors, string.Join("\n", _planErrors));
            Assert.AreEqual(SeedCount, _plans.Count);
            foreach (RoundPlan plan in _plans)
            {
                List<string> errors = RoundPlanValidator.Validate(plan, _house, _pool, _settings);
                Assert.IsEmpty(errors, "seed " + plan.Seed + ":\n" + string.Join("\n", errors) + "\n" + Describe(plan));
            }
        }

        [Test]
        public void EveryPlan_UsesAllFourMvpTypes_TwoPerStage()
        {
            foreach (RoundPlan plan in _plans)
            {
                CollectionAssert.AreEqual(new[] { 1, 2 }, plan.StageIndices, "seed " + plan.Seed);
                Assert.AreEqual(2, plan.CountInStage(1), "seed " + plan.Seed);
                Assert.AreEqual(2, plan.CountInStage(2), "seed " + plan.Seed);
                var types = new HashSet<PuzzleType>();
                foreach (PlannedPuzzle p in plan.Puzzles)
                {
                    types.Add(p.Type);
                }
                Assert.AreEqual(4, types.Count, Describe(plan));
            }
        }

        [Test]
        public void BatchReport_500Seeds_IsFullyValid_AndEndsWithTheSummaryLine()
        {
            RoundPlanBatchReport report = RoundPlanBatchReport.Run(_house, _pool, _settings, FirstSeed, SeedCount);
            Assert.AreEqual(SeedCount, report.ValidCount, string.Join("\n", report.FirstErrors));
            Assert.AreEqual(0, report.InvalidCount);
            List<string> lines = report.ToLines(_house);
            Assert.AreEqual("Plan Batch Report: 500/500 valid", lines[lines.Count - 1]);
            Assert.AreEqual(report.ReferenceCount, report.OtherFloorReferenceCount);
        }

        // ---------- determinism ----------

        [Test]
        public void SameSeed_GivesTheSamePlan_EvenFromARebuiltHouseAndANewPlanner()
        {
            HouseIndex rebuilt = GrayboxHouseIndexFactory.Build(GrayboxMarkers.Create(GrayboxLayout.Default));
            for (int i = 0; i < DeterminismSeeds; i++)
            {
                RoundPlan first = _plans[i];
                RoundPlan second = new RoundSetupPlanner().Plan(rebuilt, _pool, _settings, first.Seed);
                Assert.AreEqual(first.ComputeHash(), second.ComputeHash(), "seed " + first.Seed);
                Assert.AreEqual(Describe(first), RoundPlanFormatter.Format(second, rebuilt, _pool));
            }
        }

        [Test]
        public void PoolOrder_DoesNotChangeThePlan()
        {
            var reversed = new List<PuzzleDefinition>(_pool);
            reversed.Reverse();
            for (int i = 0; i < DeterminismSeeds; i++)
            {
                RoundPlan original = _plans[i];
                RoundPlan fromReversed = new RoundSetupPlanner().Plan(_house, reversed, _settings, original.Seed);
                Assert.AreEqual(original.ComputeHash(), fromReversed.ComputeHash(), "seed " + original.Seed);
                for (int p = 0; p < fromReversed.Puzzles.Count; p++)
                {
                    Assert.AreEqual(fromReversed.Puzzles[p].Type, reversed[fromReversed.Puzzles[p].DefinitionIndex].Type);
                }
            }
        }

        [Test]
        public void DifferentSeeds_GiveDifferentPlans_AtLeast90Percent()
        {
            var hashes = new HashSet<ulong>();
            foreach (RoundPlan plan in _plans)
            {
                hashes.Add(plan.ComputeHash());
            }
            Assert.GreaterOrEqual(hashes.Count, SeedCount * 9 / 10, "distinct plans: " + hashes.Count + "/" + SeedCount);
        }

        [Test]
        public void Layouts_Vary_AcrossSeeds()
        {
            var stageOneTypeSets = new HashSet<string>();
            foreach (RoundPlan plan in _plans)
            {
                var types = new List<string>();
                foreach (PlannedPuzzle p in plan.Puzzles)
                {
                    if (p.StageIndex == 1)
                    {
                        types.Add(p.Type.ToString());
                    }
                }
                types.Sort(StringComparer.Ordinal);
                stageOneTypeSets.Add(string.Join("+", types));
            }
            // Six pairs of four types, minus Wires+Sigil (they would need each other's item in the first stage).
            Assert.AreEqual(5, stageOneTypeSets.Count, string.Join(", ", stageOneTypeSets));
            Assert.IsFalse(stageOneTypeSets.Contains("Sigil+Wires"));
        }

        // ---------- items ----------

        [Test]
        public void ItemNeedingPuzzles_GetTheirItemFromAnEarlierStage_OrFromTheOtherPuzzleOfTheirStage()
        {
            int fromEarlierStage = 0;
            int fromSameStage = 0;
            foreach (RoundPlan plan in _plans)
            {
                for (int i = 0; i < plan.Puzzles.Count; i++)
                {
                    PlannedPuzzle p = plan.Puzzles[i];
                    PuzzleDefinition definition = DefinitionOf(p);
                    if (!definition.HasRequiredItem)
                    {
                        continue;
                    }
                    int giver = plan.IndexOfRewardGiver(definition.RequiredItemId);
                    Assert.GreaterOrEqual(giver, 0, "nobody rewards " + definition.RequiredItemId + "\n" + Describe(plan));
                    Assert.AreNotEqual(i, giver, Describe(plan));
                    Assert.LessOrEqual(plan.Puzzles[giver].StageIndex, p.StageIndex, Describe(plan));
                    if (plan.Puzzles[giver].StageIndex < p.StageIndex)
                    {
                        fromEarlierStage++;
                    }
                    else
                    {
                        fromSameStage++;
                    }
                }
            }
            Assert.Greater(fromEarlierStage, 0, "no plan used an earlier stage as the item source");
            Assert.Greater(fromSameStage, 0, "no plan used the parallel puzzle as the item source");
        }

        [Test]
        public void PaintAndFuseKey_AreNeverRewardedAfterTheStageThatNeedsThem()
        {
            foreach (RoundPlan plan in _plans)
            {
                AssertItemNotLater(plan, PuzzleType.Sigil, MvpPuzzleCatalog.PaintItemId);
                AssertItemNotLater(plan, PuzzleType.Wires, MvpPuzzleCatalog.FuseKeyItemId);
            }
        }

        private void AssertItemNotLater(RoundPlan plan, PuzzleType needer, string item)
        {
            int giver = plan.IndexOfRewardGiver(item);
            Assert.GreaterOrEqual(giver, 0, Describe(plan));
            foreach (PlannedPuzzle p in plan.Puzzles)
            {
                if (p.Type == needer)
                {
                    Assert.LessOrEqual(plan.Puzzles[giver].StageIndex, p.StageIndex, Describe(plan));
                    Assert.AreNotEqual(needer, plan.Puzzles[giver].Type, Describe(plan));
                }
            }
        }

        [Test]
        public void OnlyNeededItemsAndTheShovel_AreRewarded_EachExactlyOnce()
        {
            var expected = new[] { MvpPuzzleCatalog.FuseKeyItemId, MvpPuzzleCatalog.PaintItemId, PlannerSettings.DefaultShovelItemId };
            foreach (RoundPlan plan in _plans)
            {
                CollectionAssert.AreEquivalent(expected, plan.RequiredItems, Describe(plan));
                int rewards = 0;
                foreach (PlannedPuzzle p in plan.Puzzles)
                {
                    if (p.HasReward)
                    {
                        rewards++;
                        CollectionAssert.Contains(expected, p.RewardItemId, Describe(plan));
                    }
                }
                Assert.AreEqual(expected.Length, rewards, Describe(plan));
            }
        }

        [Test]
        public void Shovel_IsAlwaysTheRewardOfALastStagePuzzle()
        {
            foreach (RoundPlan plan in _plans)
            {
                int giver = plan.IndexOfRewardGiver(PlannerSettings.DefaultShovelItemId);
                Assert.GreaterOrEqual(giver, 0, Describe(plan));
                Assert.AreEqual(_settings.LastStage, plan.Puzzles[giver].StageIndex, Describe(plan));
            }
        }

        // ---------- references ----------

        [Test]
        public void References_AreNeverInThePuzzleRoom_AndOnTheGrayboxAlwaysOnAnotherFloor()
        {
            int total = 0;
            int otherFloor = 0;
            foreach (RoundPlan plan in _plans)
            {
                foreach (PlannedPuzzle p in plan.Puzzles)
                {
                    total++;
                    Assert.AreNotEqual(p.RoomId, p.ReferenceRoomId, Describe(plan));
                    if (RoundPlanRules.OnDifferentFloors(_house, p.RoomId, p.ReferenceRoomId))
                    {
                        otherFloor++;
                    }
                }
            }
            // The graybox has a fitting reference slot on the other floor for every arrangement, so the preference always wins.
            Assert.AreEqual(total, otherFloor, "references on another floor: " + otherFloor + "/" + total);
        }

        [Test]
        public void EveryReferenceSlotOfTheGraybox_IsUsedByAtLeastOneSeed()
        {
            var used = new HashSet<int>();
            foreach (RoundPlan plan in _plans)
            {
                foreach (PlannedPuzzle p in plan.Puzzles)
                {
                    used.Add(p.ReferenceSlotId.Value);
                }
            }
            foreach (HouseReferenceSlotEntry slot in _house.ReferenceSlots)
            {
                Assert.IsTrue(used.Contains(slot.NetId.Value), "ReferenceSlot " + slot.Path + " is never used");
            }
        }

        // ---------- types and slots ----------

        [Test]
        public void EveryType_AppearsInEveryStage_AndEveryPuzzleSlotIsUsed()
        {
            var counts = new Dictionary<string, int>();
            var usedSlots = new HashSet<int>();
            foreach (RoundPlan plan in _plans)
            {
                foreach (PlannedPuzzle p in plan.Puzzles)
                {
                    string key = p.StageIndex + ":" + p.Type;
                    int count;
                    counts.TryGetValue(key, out count);
                    counts[key] = count + 1;
                    usedSlots.Add(p.PuzzleSlotId.Value);
                }
            }
            foreach (int stage in new[] { 1, 2 })
            {
                foreach (PuzzleSpecType type in AllMvpTypes())
                {
                    Assert.IsTrue(counts.ContainsKey(stage + ":" + type.Type), type.Type + " never lands in stage " + stage);
                }
            }
            foreach (HousePuzzleSlotEntry slot in _house.PuzzleSlots)
            {
                if (!_settings.IsActiveStage(slot.StageIndex))
                {
                    continue; // the attic slots wait for StagesActive = 3 (TODO 4.7)
                }
                Assert.IsTrue(usedSlots.Contains(slot.NetId.Value), "PuzzleSlot " + slot.Path + " is never used");
            }
        }

        private static IEnumerable<PuzzleSpecType> AllMvpTypes()
        {
            foreach (PuzzleDefinitionSpec spec in MvpPuzzleCatalog.Specs)
            {
                yield return new PuzzleSpecType(spec.Type);
            }
        }

        private struct PuzzleSpecType
        {
            public readonly PuzzleType Type;

            public PuzzleSpecType(PuzzleType type)
            {
                Type = type;
            }
        }

        // ---------- answers ----------

        [Test]
        public void Answers_BuildSolvedPuzzleStates()
        {
            foreach (RoundPlan plan in _plans)
            {
                foreach (PlannedPuzzle p in plan.Puzzles)
                {
                    PuzzleDefinition definition = DefinitionOf(p);
                    PuzzleState state = null;
                    Assert.DoesNotThrow(() => state = new PuzzleState(definition.CopyValueCounts(), p.CopySolution(), definition.OpKind,
                        p.ReferenceRotation, p.CopyTrapValues()), Describe(plan));
                    Assert.IsTrue(state.IsSolved);
                    Assert.AreEqual(p.ReferenceRotation, state.ReferenceRotation);
                    CollectionAssert.AreEqual(p.TrapValues, state.TrapValues);
                }
            }
        }

        [Test]
        public void SigilRotations_CoverAllFourAngles_OtherTypesAreNotRotated()
        {
            var angles = new HashSet<int>();
            foreach (RoundPlan plan in _plans)
            {
                foreach (PlannedPuzzle p in plan.Puzzles)
                {
                    if (p.Type == PuzzleType.Sigil)
                    {
                        angles.Add(p.ReferenceRotation);
                    }
                    else
                    {
                        Assert.AreEqual(0, p.ReferenceRotation, Describe(plan));
                    }
                }
            }
            CollectionAssert.AreEquivalent(new[] { 0, 90, 180, 270 }, angles);
        }

        [Test]
        public void WiresTraps_AreTwoDistinctMotifs_NotInTheAnswer()
        {
            foreach (RoundPlan plan in _plans)
            {
                foreach (PlannedPuzzle p in plan.Puzzles)
                {
                    if (p.Type != PuzzleType.Wires)
                    {
                        CollectionAssert.IsEmpty(p.TrapValues, Describe(plan));
                        continue;
                    }
                    Assert.AreEqual(RoundPlanRules.WiresTrapCount, p.TrapValues.Count, Describe(plan));
                    Assert.AreNotEqual(p.TrapValues[0], p.TrapValues[1], Describe(plan));
                    foreach (int trap in p.TrapValues)
                    {
                        Assert.That(trap, Is.InRange(0, MvpPuzzleCatalog.WiresMotifs - 1));
                        CollectionAssert.DoesNotContain(p.Solution, trap, Describe(plan));
                    }
                }
            }
        }

        [Test]
        public void AnswerValues_CoverTheWholeRangeOfEveryPart()
        {
            // Range(0, K) per part: over 500 seeds every value of every part should appear (e.g. all 4 portrait angles).
            var seen = new Dictionary<string, HashSet<int>>();
            foreach (RoundPlan plan in _plans)
            {
                foreach (PlannedPuzzle p in plan.Puzzles)
                {
                    for (int part = 0; part < p.Solution.Count; part++)
                    {
                        string key = p.Type + "#" + part;
                        HashSet<int> values;
                        if (!seen.TryGetValue(key, out values))
                        {
                            values = new HashSet<int>();
                            seen.Add(key, values);
                        }
                        values.Add(p.Solution[part]);
                    }
                }
            }
            foreach (PuzzleDefinition definition in _pool)
            {
                for (int part = 0; part < definition.PartCount; part++)
                {
                    HashSet<int> values = seen[definition.Type + "#" + part];
                    Assert.AreEqual(definition.ValueCounts[part], values.Count, definition.Type + " part " + part);
                }
            }
        }

        [Test]
        public void RotationStep_IsTheOneOfPuzzleState()
        {
            Assert.AreEqual(PuzzleState.RotationStep, RoundPlanRules.RotationStepDegrees);
            for (int step = 0; step < RoundPlanRules.RotationStepCount; step++)
            {
                Assert.IsTrue(RoundPlanRules.IsValidRotation(step * RoundPlanRules.RotationStepDegrees));
            }
            Assert.IsFalse(RoundPlanRules.IsValidRotation(45));
            Assert.IsFalse(RoundPlanRules.IsValidRotation(360));
            Assert.IsFalse(RoundPlanRules.IsValidRotation(-90));
        }

        // ---------- spots ----------

        [Test]
        public void HouseKey_HidesInAGardenKeyHideSpot_AndEveryCandidateIsUsed()
        {
            List<HouseSpotEntry> candidates = RoundPlanRules.KeyHideCandidates(_house);
            Assert.GreaterOrEqual(candidates.Count, HouseContract.MinKeyHideSpots);
            var used = new HashSet<int>();
            foreach (RoundPlan plan in _plans)
            {
                HouseSpotEntry spot = FindSpot(_house.KeyHideSpots, plan.HouseKeySpotId);
                Assert.IsNotNull(spot, Describe(plan));
                Assert.AreEqual(HouseContract.GardenStage, _house.StageOfRoom(_house.ResolveRoom(spot)));
                used.Add(spot.NetId.Value);
            }
            Assert.AreEqual(candidates.Count, used.Count, "not every key hiding place was picked in " + SeedCount + " seeds");
        }

        [Test]
        public void Cologne_SpawnsInAnActiveStageRoom_AndEveryCandidateIsUsed()
        {
            List<HouseSpotEntry> candidates = RoundPlanRules.CologneCandidates(_house, _settings);
            Assert.AreEqual(_house.CologneSpots.Count, candidates.Count, "every graybox cologne spot is in a stage of the MVP (no attic spot)");
            var used = new HashSet<int>();
            foreach (RoundPlan plan in _plans)
            {
                HouseSpotEntry spot = FindSpot(_house.CologneSpots, plan.CologneSpotId);
                Assert.IsNotNull(spot, Describe(plan));
                int stage = _house.StageOfRoom(_house.ResolveRoom(spot));
                Assert.IsTrue(_settings.IsActiveStage(stage), "cologne in stage " + stage);
                used.Add(spot.NetId.Value);
            }
            Assert.AreEqual(candidates.Count, used.Count, "not every cologne spot was picked in " + SeedCount + " seeds");
        }

        [Test]
        public void DigSpot_IsAlwaysInTheCellar_AndEveryOneIsUsed()
        {
            Assert.GreaterOrEqual(_house.DigSpots.Count, HouseContract.MinDigSpots);
            var used = new HashSet<int>();
            foreach (RoundPlan plan in _plans)
            {
                Assert.IsTrue(plan.HasDigSpot, Describe(plan));
                HouseSpotEntry spot = FindSpot(_house.DigSpots, plan.DigSpotId);
                Assert.IsNotNull(spot, Describe(plan));
                Assert.AreEqual(HouseContract.CellarFloor, _house.FindRoom(_house.ResolveRoom(spot)).Floor);
                used.Add(spot.NetId.Value);
            }
            Assert.AreEqual(_house.DigSpots.Count, used.Count, "not every dig spot was picked in " + SeedCount + " seeds");
        }

        private static HouseSpotEntry FindSpot(IReadOnlyList<HouseSpotEntry> spots, NetId id)
        {
            foreach (HouseSpotEntry spot in spots)
            {
                if (spot.NetId == id)
                {
                    return spot;
                }
            }
            return null;
        }

        // ---------- API ----------

        [Test]
        public void Plan_Throws_WithThePlannerError_WhenTryPlanFails()
        {
            var tooMany = new PlannerSettings(5, 2);
            var e = Assert.Throws<InvalidOperationException>(() => new RoundSetupPlanner().Plan(_house, _pool, tooMany, 7));
            StringAssert.Contains("seed 7", e.Message);
            StringAssert.Contains("stage", e.Message);
        }

        [Test]
        public void SingleStageRound_PutsTheShovelIntoThatStage()
        {
            var oneStage = new PlannerSettings(1, 2);
            for (int seed = 1; seed <= DeterminismSeeds; seed++)
            {
                RoundPlan plan = new RoundSetupPlanner().Plan(_house, _pool, oneStage, seed);
                Assert.IsEmpty(RoundPlanValidator.Validate(plan, _house, _pool, oneStage), Describe(plan));
                Assert.AreEqual(1, plan.Puzzles[plan.IndexOfRewardGiver(PlannerSettings.DefaultShovelItemId)].StageIndex);
            }
        }
    }
}
