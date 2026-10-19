using System;
using System.Collections.Generic;
using System.Text;
using CinliMahzen.Core;

namespace CinliMahzen.World
{
    /// <summary>
    /// Computes the <see cref="RoundPlan"/> of a round from a seed (Teknik 7.5). Plain C#: reads only the <see cref="HouseIndex"/>
    /// (marker data, never the hierarchy), the <see cref="PuzzleDefinition"/> pool and <see cref="PlannerSettings"/>.
    ///
    /// Determinism: one <see cref="GameRandom"/>(seed) is consumed in a fixed order, and every collection is walked in index order
    /// (house lists are sorted by NetId, the pool is sorted by type and then pool index), so the same seed, house and pool give
    /// the same plan on every machine, whatever order the pool was given in.
    ///
    /// Steps:
    /// <list type="number">
    /// <item>Per active stage choose PuzzlesPerStage of its PuzzleSlots (combinations in random order, those whose puzzles stand
    /// in different rooms first) and give each a fitting definition (Size and allowedTypes, a type not used yet this round).</item>
    /// <item>Rewards: every item a chosen puzzle needs, plus the shovel, goes to exactly one other puzzle of an earlier stage or
    /// of the same stage, without a dependency cycle; the shovel to a puzzle of the last stage; at most one item per puzzle.
    /// Items no puzzle needs are not placed.</item>
    /// <item>References: one ReferenceSlot per puzzle, in another room, same size, allowing the type, never shared; first try
    /// with every reference on another floor, then with "another room" only.</item>
    /// <item>Answers: GameRandom.Range(0, K) per part; Sigil a reference rotation, Wires trap values not in its answer.</item>
    /// <item>The house key spot, the cologne spot and the real dig spot (none while the house has no cellar).</item>
    /// </list>
    /// Steps 1-3 are one depth-first search over randomly ordered candidates: when a choice cannot be completed (for example
    /// Wires and Sigil alone in the first stage, which would need each other's item) the search backtracks to the next
    /// candidate. The search is exhaustive, so it finds a valid plan whenever one exists, and it is bounded by
    /// <see cref="PlannerSettings.MaxSearchSteps"/>, so it always ends; failures come back as an explanatory error.
    /// </summary>
    public sealed class RoundSetupPlanner
    {
        /// <summary>Like <see cref="TryPlan"/> but throws <see cref="InvalidOperationException"/> with the error.</summary>
        public RoundPlan Plan(HouseIndex house, IReadOnlyList<PuzzleDefinition> pool, PlannerSettings settings, int seed)
        {
            RoundPlan plan;
            string error;
            if (!TryPlan(house, pool, settings, seed, out plan, out error))
            {
                throw new InvalidOperationException("RoundSetupPlanner (seed " + seed + "): " + error);
            }
            return plan;
        }

        /// <summary>
        /// Plans the round. False (and a message saying which rule cannot be met) when the inputs are invalid or the house and pool
        /// admit no valid plan; <paramref name="plan"/> is then null.
        /// </summary>
        public bool TryPlan(HouseIndex house, IReadOnlyList<PuzzleDefinition> pool, PlannerSettings settings, int seed,
            out RoundPlan plan, out string error)
        {
            plan = null;
            error = CheckInputs(house, pool, settings);
            if (error != null)
            {
                return false;
            }

            var search = new Search(house, pool, settings, new GameRandom(seed));
            error = search.Run();
            if (error != null)
            {
                return false;
            }
            return search.TryFinish(seed, out plan, out error);
        }

        private static string CheckInputs(HouseIndex house, IReadOnlyList<PuzzleDefinition> pool, PlannerSettings settings)
        {
            if (house == null)
            {
                return "the house index is null";
            }
            if (pool == null)
            {
                return "the puzzle pool is null";
            }
            if (settings == null)
            {
                return "the planner settings are null";
            }

            var problems = new List<string>();
            settings.CollectProblems(problems);
            if (problems.Count > 0)
            {
                return "invalid planner settings: " + string.Join("; ", problems);
            }

            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i] != null)
                {
                    pool[i].CollectProblems(problems);
                }
            }
            if (problems.Count > 0)
            {
                return "the puzzle pool has invalid definitions: " + string.Join("; ", problems);
            }
            return null;
        }

        // ------------------------------------------------------------------------------------------------

        private struct PoolEntry
        {
            public readonly PuzzleDefinition Definition;
            public readonly int Index;

            public PoolEntry(PuzzleDefinition definition, int index)
            {
                Definition = definition;
                Index = index;
            }
        }

        /// <summary>The state of one planning run. Puzzle p of the round is puzzle p % PuzzlesPerStage of active stage p / PuzzlesPerStage.</summary>
        private sealed class Search
        {
            private const int TypeTableSize = byte.MaxValue + 1;

            private readonly HouseIndex _house;
            private readonly IReadOnlyList<PuzzleDefinition> _input;
            private readonly PlannerSettings _settings;
            private readonly GameRandom _rng;
            private readonly int _stageCount;
            private readonly int _perStage;
            private readonly int _puzzleCount;

            private readonly List<PoolEntry> _pool = new List<PoolEntry>();
            private readonly List<HousePuzzleSlotEntry>[] _stageSlots;
            private readonly List<int>[][] _slotDefinitions;
            private readonly List<int[]>[] _stageCombinations;
            private List<HouseSpotEntry> _keyCandidates;
            private List<HouseSpotEntry> _cologneCandidates;
            private List<HouseSpotEntry> _digCandidates;

            // current choice per puzzle
            private readonly HousePuzzleSlotEntry[] _slot;
            private readonly int[] _definition;
            private readonly string[] _reward;
            private readonly HouseReferenceSlotEntry[] _reference;
            private readonly bool[] _typeUsed = new bool[TypeTableSize];
            private readonly List<string> _items = new List<string>();
            private bool[] _referenceUsed;

            private int _steps;
            private bool _exhausted;
            private int _rewardFailures;
            private int _rewardSuccesses;
            private string _lastRewardFailure = string.Empty;
            private string _lastReferenceFailure = string.Empty;

            public Search(HouseIndex house, IReadOnlyList<PuzzleDefinition> pool, PlannerSettings settings, GameRandom rng)
            {
                _house = house;
                _input = pool;
                _settings = settings;
                _rng = rng;
                _stageCount = settings.StagesActive;
                _perStage = settings.PuzzlesPerStage;
                _puzzleCount = settings.PuzzleCount;

                _stageSlots = new List<HousePuzzleSlotEntry>[_stageCount];
                _slotDefinitions = new List<int>[_stageCount][];
                _stageCombinations = new List<int[]>[_stageCount];
                _slot = new HousePuzzleSlotEntry[_puzzleCount];
                _definition = new int[_puzzleCount];
                _reward = new string[_puzzleCount];
                _reference = new HouseReferenceSlotEntry[_puzzleCount];
                _referenceUsed = new bool[house.ReferenceSlots.Count];
            }

            /// <summary>Null when a slot/type/reward/reference choice was found; otherwise why not.</summary>
            public string Run()
            {
                string error = Prepare();
                if (error != null)
                {
                    return error;
                }
                if (SearchStage(0))
                {
                    return null;
                }

                if (_exhausted)
                {
                    return "the search budget of " + _settings.MaxSearchSteps + " steps ran out before a valid plan was found ("
                        + _rewardSuccesses + " arrangement(s) passed the item rules)";
                }
                if (_rewardFailures == 0 && _rewardSuccesses == 0)
                {
                    return "no choice of PuzzleSlots gives each of the " + _puzzleCount
                        + " puzzles a fitting definition of a different type (" + DescribeFittingTypes() + ")";
                }
                if (_rewardSuccesses == 0)
                {
                    return "every slot/type arrangement breaks the item rules: a puzzle that needs an item must get it from a puzzle "
                        + "of an earlier stage or from another puzzle of its own stage, without a cycle"
                        + (_settings.HasShovel ? ", and the '" + _settings.ShovelItemId + "' must come from stage " + _settings.LastStage : string.Empty)
                        + "; at most one item per puzzle (last tried: " + _lastRewardFailure + ")";
                }
                return "no ReferenceSlot assignment for any of the " + _rewardSuccesses + " arrangement(s) that pass the item rules: "
                    + "every puzzle needs its own ReferenceSlot in another room, with its size and allowing its type (last tried: "
                    + _lastReferenceFailure + ")";
            }

            /// <summary>Draws the answers and the spots and builds the plan (after a successful <see cref="Run"/>).</summary>
            public bool TryFinish(int seed, out RoundPlan plan, out string error)
            {
                plan = null;
                error = null;

                var puzzles = new List<PlannedPuzzle>(_puzzleCount);
                for (int p = 0; p < _puzzleCount; p++)
                {
                    PuzzleDefinition definition = Definition(p);
                    var solution = new int[definition.PartCount];
                    for (int part = 0; part < solution.Length; part++)
                    {
                        solution[part] = _rng.Range(0, definition.ValueCounts[part]);
                    }

                    int rotation = 0;
                    if (RoundPlanRules.HasReferenceRotation(definition.Type))
                    {
                        rotation = _rng.Range(0, RoundPlanRules.RotationStepCount) * RoundPlanRules.RotationStepDegrees;
                    }

                    int[] traps;
                    error = PickTraps(definition, solution, out traps);
                    if (error != null)
                    {
                        return false;
                    }

                    HousePuzzleSlotEntry slot = _slot[p];
                    HouseReferenceSlotEntry reference = _reference[p];
                    puzzles.Add(new PlannedPuzzle(_settings.StageIndex(p / _perStage), slot.NetId, slot.RoomId, definition.Type,
                        _pool[_definition[p]].Index, reference.NetId, reference.RoomId, solution, rotation, traps,
                        _reward[p] ?? string.Empty));
                }

                NetId key = _rng.Pick(_keyCandidates).NetId;
                NetId cologne = _rng.Pick(_cologneCandidates).NetId;
                NetId dig = _digCandidates.Count > 0 ? _rng.Pick(_digCandidates).NetId : NetId.Invalid;

                var stages = new int[_stageCount];
                for (int s = 0; s < _stageCount; s++)
                {
                    stages[s] = _settings.StageIndex(s);
                }

                plan = new RoundPlan(seed, stages, puzzles, _items, key, cologne, dig);
                return true;
            }

            // ---- preparation ------------------------------------------------------------------------------

            private string Prepare()
            {
                for (int i = 0; i < _input.Count; i++)
                {
                    if (_input[i] != null)
                    {
                        _pool.Add(new PoolEntry(_input[i], i));
                    }
                }
                if (_pool.Count == 0)
                {
                    return "the puzzle pool is empty";
                }
                _pool.Sort(ComparePoolEntries);

                var fittingTypes = new bool[TypeTableSize];
                int fittingTypeCount = 0;
                for (int s = 0; s < _stageCount; s++)
                {
                    int stageIndex = _settings.StageIndex(s);
                    if (_house.FindStage(stageIndex) == null)
                    {
                        return "stage " + stageIndex + " has no StageZone in the house (StagesActive " + _stageCount + " needs stages "
                            + _settings.FirstStage + ".." + _settings.LastStage + ")";
                    }

                    var slots = new List<HousePuzzleSlotEntry>();
                    var definitions = new List<List<int>>();
                    int total = 0;
                    for (int i = 0; i < _house.PuzzleSlots.Count; i++)
                    {
                        HousePuzzleSlotEntry slot = _house.PuzzleSlots[i];
                        if (slot.StageIndex != stageIndex)
                        {
                            continue;
                        }
                        total++;
                        if (_house.FindRoom(slot.RoomId) == null)
                        {
                            continue;
                        }

                        var fits = new List<int>();
                        for (int d = 0; d < _pool.Count; d++)
                        {
                            if (RoundPlanRules.Fits(_pool[d].Definition, slot))
                            {
                                fits.Add(d);
                                int type = (int)_pool[d].Definition.Type;
                                if (!fittingTypes[type])
                                {
                                    fittingTypes[type] = true;
                                    fittingTypeCount++;
                                }
                            }
                        }
                        if (fits.Count > 0)
                        {
                            slots.Add(slot);
                            definitions.Add(fits);
                        }
                    }

                    if (slots.Count < _perStage)
                    {
                        return "stage " + stageIndex + " has " + slots.Count + " PuzzleSlot(s) that fit a pool definition (" + total
                            + " in total), but PuzzlesPerStage is " + _perStage;
                    }
                    if (Binomial(slots.Count, _perStage, _settings.MaxSearchSteps) > _settings.MaxSearchSteps)
                    {
                        return "stage " + stageIndex + " has more slot combinations (" + slots.Count + " choose " + _perStage
                            + ") than the search budget of " + _settings.MaxSearchSteps + " steps";
                    }
                    _stageSlots[s] = slots;
                    _slotDefinitions[s] = definitions.ToArray();
                }

                if (fittingTypeCount < _puzzleCount)
                {
                    return "only " + fittingTypeCount + " puzzle type(s) fit the PuzzleSlots of stages " + _settings.FirstStage + ".."
                        + _settings.LastStage + " (" + DescribeFittingTypes() + "), but a round of " + _puzzleCount
                        + " puzzles needs " + _puzzleCount + " different types";
                }

                _keyCandidates = RoundPlanRules.KeyHideCandidates(_house);
                if (_keyCandidates.Count == 0)
                {
                    return "the house has no KeyHideSpot in a garden room (the house key needs one)";
                }
                _cologneCandidates = RoundPlanRules.CologneCandidates(_house, _settings);
                if (_cologneCandidates.Count == 0)
                {
                    return "the house has no CologneSpawnSpot in a room of stages " + _settings.FirstStage + ".." + _settings.LastStage;
                }
                _digCandidates = RoundPlanRules.DigCandidates(_house);

                // Randomness, in a fixed order: per stage the definition order of each slot, then the order of the combinations.
                for (int s = 0; s < _stageCount; s++)
                {
                    for (int i = 0; i < _slotDefinitions[s].Length; i++)
                    {
                        _rng.Shuffle(_slotDefinitions[s][i]);
                    }
                    List<int[]> combinations = Combinations(_stageSlots[s].Count, _perStage);
                    _rng.Shuffle(combinations);
                    _stageCombinations[s] = DistinctRoomsFirst(combinations, _stageSlots[s]);
                }
                return null;
            }

            private static int ComparePoolEntries(PoolEntry a, PoolEntry b)
            {
                int byType = ((int)a.Definition.Type).CompareTo((int)b.Definition.Type);
                return byType != 0 ? byType : a.Index.CompareTo(b.Index);
            }

            /// <summary>All k-subsets of 0..n-1 as ascending index arrays, in lexicographic order (k &lt;= n).</summary>
            private static List<int[]> Combinations(int n, int k)
            {
                var result = new List<int[]>();
                var current = new int[k];
                for (int i = 0; i < k; i++)
                {
                    current[i] = i;
                }
                while (true)
                {
                    result.Add((int[])current.Clone());
                    int pos = k - 1;
                    while (pos >= 0 && current[pos] == n - k + pos)
                    {
                        pos--;
                    }
                    if (pos < 0)
                    {
                        return result;
                    }
                    current[pos]++;
                    for (int i = pos + 1; i < k; i++)
                    {
                        current[i] = current[i - 1] + 1;
                    }
                }
            }

            /// <summary>C(n, k), or cap + 1 as soon as it passes <paramref name="cap"/>.</summary>
            private static long Binomial(int n, int k, long cap)
            {
                long result = 1;
                for (int i = 1; i <= k; i++)
                {
                    result = result * (n - k + i) / i;
                    if (result > cap)
                    {
                        return cap + 1;
                    }
                }
                return result;
            }

            /// <summary>Stable reorder: combinations whose slots lie in more different rooms come first (the parallel puzzles of a stage should split the jinns).</summary>
            private static List<int[]> DistinctRoomsFirst(List<int[]> combinations, List<HousePuzzleSlotEntry> slots)
            {
                var result = new List<int[]>(combinations.Count);
                int k = combinations.Count > 0 ? combinations[0].Length : 0;
                for (int want = k; want >= 1; want--)
                {
                    for (int c = 0; c < combinations.Count; c++)
                    {
                        if (DistinctRooms(combinations[c], slots) == want)
                        {
                            result.Add(combinations[c]);
                        }
                    }
                }
                return result;
            }

            private static int DistinctRooms(int[] combination, List<HousePuzzleSlotEntry> slots)
            {
                int distinct = 0;
                for (int i = 0; i < combination.Length; i++)
                {
                    bool seen = false;
                    for (int j = 0; j < i; j++)
                    {
                        if (slots[combination[j]].RoomId == slots[combination[i]].RoomId)
                        {
                            seen = true;
                            break;
                        }
                    }
                    if (!seen)
                    {
                        distinct++;
                    }
                }
                return distinct;
            }

            // ---- search: slots and definitions --------------------------------------------------------------

            private bool Step()
            {
                if (_exhausted)
                {
                    return false;
                }
                _steps++;
                if (_steps > _settings.MaxSearchSteps)
                {
                    _exhausted = true;
                    return false;
                }
                return true;
            }

            private PuzzleDefinition Definition(int puzzle)
            {
                return _pool[_definition[puzzle]].Definition;
            }

            private bool SearchStage(int s)
            {
                if (s == _stageCount)
                {
                    return SolveArrangement();
                }

                List<int[]> combinations = _stageCombinations[s];
                for (int c = 0; c < combinations.Count; c++)
                {
                    if (!Step())
                    {
                        return false;
                    }
                    int[] combination = combinations[c];
                    for (int j = 0; j < _perStage; j++)
                    {
                        _slot[s * _perStage + j] = _stageSlots[s][combination[j]];
                    }
                    if (AssignDefinitions(s, combination, 0))
                    {
                        return true;
                    }
                    if (_exhausted)
                    {
                        return false;
                    }
                }
                return false;
            }

            private bool AssignDefinitions(int s, int[] combination, int j)
            {
                if (j == _perStage)
                {
                    // Items needed in stages 0..s can only come from stages 0..s: prune when they already fail there.
                    int limit = (s + 1) * _perStage;
                    if (s + 1 < _stageCount && !SolveRewards(limit, false, false))
                    {
                        if (!_exhausted)
                        {
                            _rewardFailures++;
                            _lastRewardFailure = DescribeArrangement(limit);
                        }
                        return false;
                    }
                    return SearchStage(s + 1);
                }

                int p = s * _perStage + j;
                List<int> candidates = _slotDefinitions[s][combination[j]];
                for (int c = 0; c < candidates.Count; c++)
                {
                    int d = candidates[c];
                    int type = (int)_pool[d].Definition.Type;
                    if (_typeUsed[type])
                    {
                        continue;
                    }
                    if (!Step())
                    {
                        return false;
                    }

                    _typeUsed[type] = true;
                    _definition[p] = d;
                    if (AssignDefinitions(s, combination, j + 1))
                    {
                        return true;
                    }
                    _typeUsed[type] = false;
                    if (_exhausted)
                    {
                        return false;
                    }
                }
                return false;
            }

            private bool SolveArrangement()
            {
                if (!SolveRewards(_puzzleCount, true, true))
                {
                    if (!_exhausted)
                    {
                        _rewardFailures++;
                        _lastRewardFailure = DescribeArrangement(_puzzleCount);
                    }
                    return false;
                }
                _rewardSuccesses++;

                if (SolveReferences(true) || (!_exhausted && SolveReferences(false)))
                {
                    return true;
                }
                if (!_exhausted)
                {
                    _lastReferenceFailure = DescribeArrangement(_puzzleCount);
                }
                return false;
            }

            // ---- search: rewards --------------------------------------------------------------------------

            /// <summary>
            /// Gives every item needed by puzzles 0..limit-1 (and the shovel when <paramref name="withShovel"/>) to one of those
            /// puzzles. <paramref name="randomize"/> false keeps the candidate order (used for pruning, so it draws nothing).
            /// </summary>
            private bool SolveRewards(int limit, bool withShovel, bool randomize)
            {
                _items.Clear();
                for (int p = 0; p < limit; p++)
                {
                    string needed = Definition(p).RequiredItemId;
                    if (needed.Length > 0 && !_items.Contains(needed))
                    {
                        _items.Add(needed);
                    }
                }
                if (withShovel && _settings.HasShovel && !_items.Contains(_settings.ShovelItemId))
                {
                    _items.Add(_settings.ShovelItemId);
                }

                for (int p = 0; p < _puzzleCount; p++)
                {
                    _reward[p] = null;
                }
                if (_items.Count > limit)
                {
                    return false;
                }
                return AssignItem(0, limit, randomize);
            }

            private bool AssignItem(int i, int limit, bool randomize)
            {
                if (i == _items.Count)
                {
                    return IsAcyclic(limit);
                }

                string item = _items[i];
                var candidates = new List<int>();
                for (int p = 0; p < limit; p++)
                {
                    if (_reward[p] == null && CanGive(p, item, limit))
                    {
                        candidates.Add(p);
                    }
                }
                if (randomize)
                {
                    _rng.Shuffle(candidates);
                }

                for (int c = 0; c < candidates.Count; c++)
                {
                    if (!Step())
                    {
                        return false;
                    }
                    int p = candidates[c];
                    _reward[p] = item;
                    if (AssignItem(i + 1, limit, randomize))
                    {
                        return true;
                    }
                    _reward[p] = null;
                    if (_exhausted)
                    {
                        return false;
                    }
                }
                return false;
            }

            /// <summary>Puzzle <paramref name="giver"/> may reward the item: not to itself, never to an earlier stage, the shovel only from the last stage.</summary>
            private bool CanGive(int giver, string item, int limit)
            {
                int giverStage = giver / _perStage;
                if (_settings.HasShovel && string.Equals(item, _settings.ShovelItemId, StringComparison.Ordinal)
                    && giverStage != _stageCount - 1)
                {
                    return false;
                }
                for (int q = 0; q < limit; q++)
                {
                    if (!string.Equals(Definition(q).RequiredItemId, item, StringComparison.Ordinal))
                    {
                        continue;
                    }
                    if (q == giver || giverStage > q / _perStage)
                    {
                        return false;
                    }
                }
                return true;
            }

            /// <summary>No puzzle waits (through a chain of "needs the item of") on itself.</summary>
            private bool IsAcyclic(int limit)
            {
                for (int start = 0; start < limit; start++)
                {
                    int current = start;
                    for (int hop = 0; hop < limit; hop++)
                    {
                        string needed = Definition(current).RequiredItemId;
                        if (needed.Length == 0)
                        {
                            break;
                        }
                        int giver = GiverOf(needed, limit);
                        if (giver < 0)
                        {
                            break;
                        }
                        if (giver == start)
                        {
                            return false;
                        }
                        current = giver;
                    }
                }
                return true;
            }

            private int GiverOf(string item, int limit)
            {
                for (int p = 0; p < limit; p++)
                {
                    if (string.Equals(_reward[p], item, StringComparison.Ordinal))
                    {
                        return p;
                    }
                }
                return -1;
            }

            // ---- search: references -----------------------------------------------------------------------

            /// <summary><paramref name="otherFloorOnly"/>: every reference on another floor than its puzzle; otherwise other floors are only preferred.</summary>
            private bool SolveReferences(bool otherFloorOnly)
            {
                Array.Clear(_referenceUsed, 0, _referenceUsed.Length);
                return AssignReference(0, otherFloorOnly);
            }

            private bool AssignReference(int p, bool otherFloorOnly)
            {
                if (p == _puzzleCount)
                {
                    return true;
                }

                HousePuzzleSlotEntry slot = _slot[p];
                PuzzleDefinition definition = Definition(p);
                var otherFloor = new List<int>();
                var sameFloor = new List<int>();
                for (int r = 0; r < _house.ReferenceSlots.Count; r++)
                {
                    HouseReferenceSlotEntry reference = _house.ReferenceSlots[r];
                    if (_referenceUsed[r] || reference.RoomId == slot.RoomId || _house.FindRoom(reference.RoomId) == null
                        || !RoundPlanRules.Fits(definition, reference))
                    {
                        continue;
                    }
                    if (RoundPlanRules.OnDifferentFloors(_house, slot.RoomId, reference.RoomId))
                    {
                        otherFloor.Add(r);
                    }
                    else if (!otherFloorOnly)
                    {
                        sameFloor.Add(r);
                    }
                }
                _rng.Shuffle(otherFloor);
                _rng.Shuffle(sameFloor);
                otherFloor.AddRange(sameFloor);

                for (int c = 0; c < otherFloor.Count; c++)
                {
                    if (!Step())
                    {
                        return false;
                    }
                    int r = otherFloor[c];
                    _referenceUsed[r] = true;
                    _reference[p] = _house.ReferenceSlots[r];
                    if (AssignReference(p + 1, otherFloorOnly))
                    {
                        return true;
                    }
                    _referenceUsed[r] = false;
                    if (_exhausted)
                    {
                        return false;
                    }
                }
                return false;
            }

            // ---- answers ----------------------------------------------------------------------------------

            /// <summary>Trap values for types that show them: distinct values of [0, max K) that are not in the answer, ascending.</summary>
            private string PickTraps(PuzzleDefinition definition, int[] solution, out int[] traps)
            {
                int count = RoundPlanRules.TrapCount(definition.Type);
                if (count == 0)
                {
                    traps = Array.Empty<int>();
                    return null;
                }

                int max = RoundPlanRules.MaxValueCount(definition);
                var free = new List<int>();
                for (int v = 0; v < max; v++)
                {
                    if (Array.IndexOf(solution, v) < 0)
                    {
                        free.Add(v);
                    }
                }
                if (free.Count < count)
                {
                    traps = null;
                    return definition.Type + " (" + definition.name + "): only " + free.Count + " value(s) are not in the answer, "
                        + count + " trap values are needed";
                }

                _rng.Shuffle(free);
                traps = free.GetRange(0, count).ToArray();
                Array.Sort(traps);
                return null;
            }

            // ---- messages ---------------------------------------------------------------------------------

            private string RoomName(int roomId)
            {
                HouseRoomEntry room = _house.FindRoom(roomId);
                return room != null ? room.Key : "#" + roomId;
            }

            private string DescribeArrangement(int limit)
            {
                var text = new StringBuilder();
                for (int p = 0; p < limit; p++)
                {
                    if (p % _perStage == 0)
                    {
                        if (p > 0)
                        {
                            text.Append("; ");
                        }
                        text.Append("stage ").Append(_settings.StageIndex(p / _perStage)).Append(": ");
                    }
                    else
                    {
                        text.Append(", ");
                    }
                    PuzzleDefinition definition = Definition(p);
                    text.Append(definition.Type).Append(" in ").Append(RoomName(_slot[p].RoomId));
                    if (definition.HasRequiredItem)
                    {
                        text.Append(" (needs ").Append(definition.RequiredItemId).Append(')');
                    }
                }
                return text.ToString();
            }

            private string DescribeFittingTypes()
            {
                var text = new StringBuilder();
                for (int s = 0; s < _stageCount; s++)
                {
                    if (s > 0)
                    {
                        text.Append("; ");
                    }
                    int stageIndex = _settings.StageIndex(s);
                    text.Append("stage ").Append(stageIndex).Append(':');
                    var seen = new bool[TypeTableSize];
                    List<int>[] definitions = _slotDefinitions[s];
                    for (int i = 0; definitions != null && i < definitions.Length; i++)
                    {
                        for (int c = 0; c < definitions[i].Count; c++)
                        {
                            seen[(int)_pool[definitions[i][c]].Definition.Type] = true;
                        }
                    }
                    bool any = false;
                    for (int type = 0; type < TypeTableSize; type++)
                    {
                        if (seen[type])
                        {
                            text.Append(any ? ", " : " ").Append((PuzzleType)type);
                            any = true;
                        }
                    }
                    if (!any)
                    {
                        text.Append(" none");
                    }
                }
                return text.ToString();
            }
        }
    }
}
