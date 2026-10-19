using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using CinliMahzen.Core;

namespace CinliMahzen.World
{
    /// <summary>
    /// Plans and validates a run of consecutive seeds and collects the statistics of CinliMahzen/House/Plan Batch Report
    /// (TODO 2.3 acceptance): valid count, first errors, type per stage, puzzle and reference rooms, other-floor references,
    /// reward givers, Sigil rotations, key and cologne spots, distinct plans and planner time. Plain C#; the EditMode tests run
    /// the same report, so the menu and the tests cannot disagree.
    /// </summary>
    public sealed class RoundPlanBatchReport
    {
        /// <summary>Failing seeds whose errors are kept for the report.</summary>
        public const int MaxListedErrors = 5;

        private readonly List<string> _firstErrors = new List<string>();
        private readonly SortedDictionary<int, SortedDictionary<PuzzleType, int>> _typesByStage = new SortedDictionary<int, SortedDictionary<PuzzleType, int>>();
        private readonly SortedDictionary<int, int> _puzzleRooms = new SortedDictionary<int, int>();
        private readonly SortedDictionary<int, int> _referenceRooms = new SortedDictionary<int, int>();
        private readonly SortedDictionary<string, SortedDictionary<PuzzleType, int>> _rewardGivers =
            new SortedDictionary<string, SortedDictionary<PuzzleType, int>>(StringComparer.Ordinal);
        private readonly SortedDictionary<int, int> _rotations = new SortedDictionary<int, int>();
        private readonly SortedDictionary<int, int> _keySpots = new SortedDictionary<int, int>();
        private readonly SortedDictionary<int, int> _cologneSpots = new SortedDictionary<int, int>();
        private readonly HashSet<ulong> _hashes = new HashSet<ulong>();

        private RoundPlanBatchReport(int firstSeed, int seedCount)
        {
            FirstSeed = firstSeed;
            SeedCount = seedCount;
        }

        public int FirstSeed { get; }
        public int SeedCount { get; }
        public int ValidCount { get; private set; }
        public int InvalidCount => SeedCount - ValidCount;

        /// <summary>"seed N: error" for the first <see cref="MaxListedErrors"/> failing seeds.</summary>
        public IReadOnlyList<string> FirstErrors => _firstErrors;

        /// <summary>Time spent inside the planner (validation excluded), all seeds together.</summary>
        public double TotalMilliseconds { get; private set; }

        public double AverageMilliseconds => SeedCount > 0 ? TotalMilliseconds / SeedCount : 0d;

        public int ReferenceCount { get; private set; }
        public int OtherFloorReferenceCount { get; private set; }

        /// <summary>Stages of valid plans, and how many of them have their puzzles in pairwise different rooms.</summary>
        public int StageCount { get; private set; }
        public int SplitStageCount { get; private set; }

        /// <summary>Different <see cref="RoundPlan.ComputeHash"/> values among the valid plans.</summary>
        public int DistinctPlanCount => _hashes.Count;

        public string SummaryLine => "Plan Batch Report: " + ValidCount + "/" + SeedCount + " valid";

        /// <summary>Plans seeds firstSeed .. firstSeed + seedCount - 1 and validates each plan with <see cref="RoundPlanValidator"/>.</summary>
        public static RoundPlanBatchReport Run(HouseIndex house, IReadOnlyList<PuzzleDefinition> pool, PlannerSettings settings,
            int firstSeed, int seedCount)
        {
            var report = new RoundPlanBatchReport(firstSeed, Math.Max(0, seedCount));
            var planner = new RoundSetupPlanner();
            var watch = new Stopwatch();
            for (int i = 0; i < report.SeedCount; i++)
            {
                int seed = firstSeed + i;
                RoundPlan plan;
                string error;
                watch.Start();
                bool planned = planner.TryPlan(house, pool, settings, seed, out plan, out error);
                watch.Stop();

                if (!planned)
                {
                    report.AddError(seed, error);
                    continue;
                }
                List<string> errors = RoundPlanValidator.Validate(plan, house, pool, settings);
                if (errors.Count > 0)
                {
                    report.AddError(seed, string.Join("; ", errors));
                    continue;
                }
                report.ValidCount++;
                report.Collect(plan, house);
            }
            report.TotalMilliseconds = watch.Elapsed.TotalMilliseconds;
            return report;
        }

        /// <summary>How often a valid plan put <paramref name="type"/> into <paramref name="stage"/>.</summary>
        public int CountType(int stage, PuzzleType type)
        {
            SortedDictionary<PuzzleType, int> types;
            int count;
            return _typesByStage.TryGetValue(stage, out types) && types.TryGetValue(type, out count) ? count : 0;
        }

        /// <summary>How often a puzzle of <paramref name="type"/> rewarded <paramref name="itemId"/>.</summary>
        public int CountRewardGiver(string itemId, PuzzleType type)
        {
            SortedDictionary<PuzzleType, int> types;
            int count;
            return _rewardGivers.TryGetValue(itemId, out types) && types.TryGetValue(type, out count) ? count : 0;
        }

        /// <summary>The report, one line per topic; the last line is <see cref="SummaryLine"/>.</summary>
        public List<string> ToLines(HouseIndex house)
        {
            var lines = new List<string>();
            lines.Add("Plan Batch Report: seeds " + FirstSeed + ".." + (FirstSeed + SeedCount - 1) + ", " + ValidCount + " valid, "
                + InvalidCount + " invalid");

            var text = new StringBuilder("Types per stage:");
            foreach (KeyValuePair<int, SortedDictionary<PuzzleType, int>> stage in _typesByStage)
            {
                text.Append(" stage ").Append(stage.Key).Append(" {");
                AppendCounts(text, stage.Value);
                text.Append('}');
            }
            lines.Add(text.ToString());

            lines.Add("Puzzle rooms: " + RoomCounts(house, _puzzleRooms));
            lines.Add("Reference rooms: " + RoomCounts(house, _referenceRooms));
            lines.Add("References on another floor: " + OtherFloorReferenceCount + "/" + ReferenceCount + " (" + Percent(OtherFloorReferenceCount, ReferenceCount) + ")");
            lines.Add("Stages with their puzzles in different rooms: " + SplitStageCount + "/" + StageCount + " (" + Percent(SplitStageCount, StageCount) + ")");

            text = new StringBuilder("Reward givers:");
            foreach (KeyValuePair<string, SortedDictionary<PuzzleType, int>> item in _rewardGivers)
            {
                text.Append(' ').Append(item.Key).Append(" <- {");
                AppendCounts(text, item.Value);
                text.Append('}');
            }
            lines.Add(text.ToString());

            text = new StringBuilder("Sigil reference rotation:");
            foreach (KeyValuePair<int, int> rotation in _rotations)
            {
                text.Append(' ').Append(rotation.Key).Append(": ").Append(rotation.Value);
            }
            lines.Add(text.ToString());

            lines.Add("House key spots: " + SpotCounts(_keySpots));
            lines.Add("Cologne spots: " + SpotCounts(_cologneSpots));
            lines.Add("Distinct plans: " + DistinctPlanCount + "/" + ValidCount);
            lines.Add("Planner time: total " + TotalMilliseconds.ToString("0.0", CultureInfo.InvariantCulture) + " ms, average "
                + AverageMilliseconds.ToString("0.000", CultureInfo.InvariantCulture) + " ms per seed");

            for (int i = 0; i < _firstErrors.Count; i++)
            {
                lines.Add("Invalid " + _firstErrors[i]);
            }
            lines.Add(SummaryLine);
            return lines;
        }

        // ------------------------------------------------------------------------------------------------

        private void AddError(int seed, string error)
        {
            if (_firstErrors.Count < MaxListedErrors)
            {
                _firstErrors.Add("seed " + seed + ": " + error);
            }
        }

        private void Collect(RoundPlan plan, HouseIndex house)
        {
            _hashes.Add(plan.ComputeHash());
            for (int i = 0; i < plan.Puzzles.Count; i++)
            {
                PlannedPuzzle p = plan.Puzzles[i];
                SortedDictionary<PuzzleType, int> types;
                if (!_typesByStage.TryGetValue(p.StageIndex, out types))
                {
                    types = new SortedDictionary<PuzzleType, int>();
                    _typesByStage.Add(p.StageIndex, types);
                }
                Increment(types, p.Type);
                Increment(_puzzleRooms, p.RoomId);
                Increment(_referenceRooms, p.ReferenceRoomId);

                ReferenceCount++;
                if (RoundPlanRules.OnDifferentFloors(house, p.RoomId, p.ReferenceRoomId))
                {
                    OtherFloorReferenceCount++;
                }
                if (RoundPlanRules.HasReferenceRotation(p.Type))
                {
                    Increment(_rotations, p.ReferenceRotation);
                }
                if (p.HasReward)
                {
                    SortedDictionary<PuzzleType, int> givers;
                    if (!_rewardGivers.TryGetValue(p.RewardItemId, out givers))
                    {
                        givers = new SortedDictionary<PuzzleType, int>();
                        _rewardGivers.Add(p.RewardItemId, givers);
                    }
                    Increment(givers, p.Type);
                }
            }

            for (int s = 0; s < plan.StageIndices.Count; s++)
            {
                StageCount++;
                if (PuzzlesInDifferentRooms(plan, plan.StageIndices[s]))
                {
                    SplitStageCount++;
                }
            }
            Increment(_keySpots, plan.HouseKeySpotId.Value);
            Increment(_cologneSpots, plan.CologneSpotId.Value);
        }

        private static bool PuzzlesInDifferentRooms(RoundPlan plan, int stage)
        {
            for (int a = 0; a < plan.Puzzles.Count; a++)
            {
                for (int b = a + 1; b < plan.Puzzles.Count; b++)
                {
                    if (plan.Puzzles[a].StageIndex == stage && plan.Puzzles[b].StageIndex == stage
                        && plan.Puzzles[a].RoomId == plan.Puzzles[b].RoomId)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private static void Increment<TKey>(SortedDictionary<TKey, int> counts, TKey key)
        {
            int count;
            counts.TryGetValue(key, out count);
            counts[key] = count + 1;
        }

        private static void AppendCounts(StringBuilder text, SortedDictionary<PuzzleType, int> counts)
        {
            bool first = true;
            foreach (KeyValuePair<PuzzleType, int> entry in counts)
            {
                text.Append(first ? string.Empty : ", ").Append(entry.Key).Append(' ').Append(entry.Value);
                first = false;
            }
        }

        private static string RoomCounts(HouseIndex house, SortedDictionary<int, int> counts)
        {
            var text = new StringBuilder();
            foreach (KeyValuePair<int, int> entry in counts)
            {
                HouseRoomEntry room = house != null ? house.FindRoom(entry.Key) : null;
                text.Append(text.Length > 0 ? ", " : string.Empty).Append(room != null ? room.Key : "#" + entry.Key).Append(' ').Append(entry.Value);
            }
            return text.Length > 0 ? text.ToString() : "-";
        }

        private static string SpotCounts(SortedDictionary<int, int> counts)
        {
            var text = new StringBuilder();
            foreach (KeyValuePair<int, int> entry in counts)
            {
                text.Append(text.Length > 0 ? ", " : string.Empty).Append(new NetId(entry.Key)).Append(' ').Append(entry.Value);
            }
            return text.Length > 0 ? text.ToString() : "-";
        }

        private static string Percent(int part, int total)
        {
            return total > 0 ? (100.0 * part / total).ToString("0.0", CultureInfo.InvariantCulture) + "%" : "-";
        }
    }
}
