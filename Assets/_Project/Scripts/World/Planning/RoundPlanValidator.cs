using System;
using System.Collections.Generic;
using CinliMahzen.Core;

namespace CinliMahzen.World
{
    /// <summary>
    /// Checks a <see cref="RoundPlan"/> against the house, the pool and every rule of Teknik 7.5 / Oyun 3.1, 4, 6, 9 independently of
    /// how it was made (the planner's own output in the 500-seed tests, hand-broken plans in the negative tests). Plain C#.
    ///
    /// Errors: stage list and puzzle counts; every puzzle slot exists, lies in the puzzle's stage and room and is used once; the
    /// definition exists, has the puzzle's type and fits the slot (size + allowed types); types are unique; every reference slot
    /// exists, matches the reference room, is in ANOTHER room than its puzzle, fits the definition and is used once; answers have
    /// one in-range value per part; rotations are 0/90/180/270 (only for rotated types); trap values have the right count, are in
    /// range, distinct and not part of the answer; the round items are exactly the needed items plus the shovel, each rewarded by
    /// exactly one puzzle, never by the puzzle that needs it, never later than it is needed, without dependency cycles, the
    /// shovel in the last stage; the key, cologne and dig spots exist in the house and lie where they may.
    /// "Reference on another floor" is a preference of the planner, not a rule, so it is not checked here.
    /// </summary>
    public static class RoundPlanValidator
    {
        /// <summary>All rule violations of <paramref name="plan"/>; empty when it is valid.</summary>
        public static List<string> Validate(RoundPlan plan, HouseIndex house, IReadOnlyList<PuzzleDefinition> pool, PlannerSettings settings)
        {
            var errors = new List<string>();
            if (plan == null)
            {
                errors.Add("the plan is null");
            }
            if (house == null)
            {
                errors.Add("the house index is null");
            }
            if (pool == null)
            {
                errors.Add("the puzzle pool is null");
            }
            if (settings == null)
            {
                errors.Add("the planner settings are null");
            }
            if (errors.Count > 0)
            {
                return errors;
            }

            CheckStages(plan, settings, errors);
            var definitions = new PuzzleDefinition[plan.Puzzles.Count];
            CheckPuzzles(plan, house, pool, definitions, errors);
            CheckItems(plan, settings, definitions, errors);
            CheckSpots(plan, house, settings, errors);
            return errors;
        }

        // ------------------------------------------------------------------------------------------------

        private static void CheckStages(RoundPlan plan, PlannerSettings settings, List<string> errors)
        {
            bool stagesMatch = plan.StageIndices.Count == settings.StagesActive;
            for (int i = 0; stagesMatch && i < plan.StageIndices.Count; i++)
            {
                stagesMatch = plan.StageIndices[i] == settings.StageIndex(i);
            }
            if (!stagesMatch)
            {
                errors.Add("the plan's stages are [" + string.Join(", ", plan.StageIndices) + "], the settings need stages "
                    + settings.FirstStage + ".." + settings.LastStage);
            }

            if (plan.Puzzles.Count != settings.PuzzleCount)
            {
                errors.Add("the plan has " + plan.Puzzles.Count + " puzzles, the settings need " + settings.PuzzleCount);
            }
            for (int s = 0; s < settings.StagesActive; s++)
            {
                int stage = settings.StageIndex(s);
                int count = plan.CountInStage(stage);
                if (count != settings.PuzzlesPerStage)
                {
                    errors.Add("stage " + stage + " has " + count + " puzzles (PuzzlesPerStage is " + settings.PuzzlesPerStage + ")");
                }
            }
            for (int i = 0; i < plan.Puzzles.Count; i++)
            {
                if (!settings.IsActiveStage(plan.Puzzles[i].StageIndex))
                {
                    errors.Add(Label(plan, i) + " is in stage " + plan.Puzzles[i].StageIndex + ", which is not an active stage");
                }
            }
        }

        private static void CheckPuzzles(RoundPlan plan, HouseIndex house, IReadOnlyList<PuzzleDefinition> pool,
            PuzzleDefinition[] definitions, List<string> errors)
        {
            var usedSlots = new HashSet<int>();
            var usedReferences = new HashSet<int>();
            var usedTypes = new HashSet<PuzzleType>();

            for (int i = 0; i < plan.Puzzles.Count; i++)
            {
                PlannedPuzzle p = plan.Puzzles[i];
                string label = Label(plan, i);

                if (!usedTypes.Add(p.Type))
                {
                    errors.Add("type " + p.Type + " is used more than once in the round (" + label + ")");
                }

                // definition
                PuzzleDefinition definition = null;
                if (p.DefinitionIndex < 0 || p.DefinitionIndex >= pool.Count)
                {
                    errors.Add(label + ": DefinitionIndex " + p.DefinitionIndex + " is outside the pool (" + pool.Count + " definitions)");
                }
                else if (pool[p.DefinitionIndex] == null)
                {
                    errors.Add(label + ": pool entry " + p.DefinitionIndex + " is null");
                }
                else if (pool[p.DefinitionIndex].Type != p.Type)
                {
                    errors.Add(label + ": DefinitionIndex " + p.DefinitionIndex + " is a " + pool[p.DefinitionIndex].Type + " definition");
                }
                else
                {
                    definition = pool[p.DefinitionIndex];
                }
                definitions[i] = definition;

                // puzzle slot
                HousePuzzleSlotEntry slot = FindPuzzleSlot(house, p.PuzzleSlotId);
                if (slot == null)
                {
                    errors.Add(label + ": PuzzleSlot " + p.PuzzleSlotId + " is not in the house");
                }
                else
                {
                    if (!usedSlots.Add(slot.NetId.Value))
                    {
                        errors.Add(label + ": PuzzleSlot " + slot.NetId + " is used by more than one puzzle");
                    }
                    if (slot.StageIndex != p.StageIndex)
                    {
                        errors.Add(label + ": PuzzleSlot " + slot.NetId + " belongs to stage " + slot.StageIndex + ", not " + p.StageIndex);
                    }
                    if (slot.RoomId != p.RoomId)
                    {
                        errors.Add(label + ": RoomId " + p.RoomId + " differs from the room of PuzzleSlot " + slot.NetId + " (" + slot.RoomId + ")");
                    }
                    if (definition != null && !RoundPlanRules.Fits(definition, slot))
                    {
                        errors.Add(label + ": the definition (size " + definition.Size + ") does not fit PuzzleSlot " + slot.NetId
                            + " (size " + slot.Size + ", allows " + string.Join("/", slot.AllowedTypes) + ")");
                    }
                }

                // reference slot
                if (p.ReferenceRoomId == p.RoomId)
                {
                    errors.Add(label + ": the reference is in the same room as the puzzle (room " + p.RoomId + ")");
                }
                HouseReferenceSlotEntry reference = FindReferenceSlot(house, p.ReferenceSlotId);
                if (reference == null)
                {
                    errors.Add(label + ": ReferenceSlot " + p.ReferenceSlotId + " is not in the house");
                }
                else
                {
                    if (!usedReferences.Add(reference.NetId.Value))
                    {
                        errors.Add(label + ": ReferenceSlot " + reference.NetId + " is used by more than one puzzle");
                    }
                    if (reference.RoomId != p.ReferenceRoomId)
                    {
                        errors.Add(label + ": ReferenceRoomId " + p.ReferenceRoomId + " differs from the room of ReferenceSlot "
                            + reference.NetId + " (" + reference.RoomId + ")");
                    }
                    if (slot != null && reference.RoomId == slot.RoomId && p.ReferenceRoomId != p.RoomId)
                    {
                        errors.Add(label + ": ReferenceSlot " + reference.NetId + " is in the same room as the puzzle (room " + slot.RoomId + ")");
                    }
                    if (definition != null && !RoundPlanRules.Fits(definition, reference))
                    {
                        errors.Add(label + ": the definition (size " + definition.Size + ") does not fit ReferenceSlot " + reference.NetId
                            + " (size " + reference.Size + ", allows " + string.Join("/", reference.AllowedTypes) + ")");
                    }
                }

                CheckAnswer(p, label, definition, errors);
            }
        }

        private static void CheckAnswer(PlannedPuzzle p, string label, PuzzleDefinition definition, List<string> errors)
        {
            if (RoundPlanRules.HasReferenceRotation(p.Type))
            {
                if (!RoundPlanRules.IsValidRotation(p.ReferenceRotation))
                {
                    errors.Add(label + ": reference rotation " + p.ReferenceRotation + " is not 0, 90, 180 or 270");
                }
            }
            else if (p.ReferenceRotation != 0)
            {
                errors.Add(label + ": reference rotation " + p.ReferenceRotation + " on a type without a rotated reference");
            }

            int expectedTraps = RoundPlanRules.TrapCount(p.Type);
            if (p.TrapValues.Count != expectedTraps)
            {
                errors.Add(label + ": " + p.TrapValues.Count + " trap values, the type needs " + expectedTraps);
            }

            if (definition == null)
            {
                return;
            }

            IReadOnlyList<int> counts = definition.ValueCounts;
            if (p.Solution.Count != counts.Count)
            {
                errors.Add(label + ": the answer has " + p.Solution.Count + " values, the definition has " + counts.Count + " parts");
            }
            for (int part = 0; part < p.Solution.Count && part < counts.Count; part++)
            {
                int value = p.Solution[part];
                if (value < 0 || value >= counts[part])
                {
                    errors.Add(label + ": answer value " + value + " of part " + part + " is out of range [0, " + counts[part] + ")");
                }
            }

            int max = RoundPlanRules.MaxValueCount(definition);
            for (int t = 0; t < p.TrapValues.Count; t++)
            {
                int trap = p.TrapValues[t];
                if (trap < 0 || trap >= max)
                {
                    errors.Add(label + ": trap value " + trap + " is out of range [0, " + max + ")");
                }
                if (Contains(p.Solution, trap))
                {
                    errors.Add(label + ": trap value " + trap + " is part of the answer");
                }
                for (int u = 0; u < t; u++)
                {
                    if (p.TrapValues[u] == trap)
                    {
                        errors.Add(label + ": trap value " + trap + " appears twice");
                        break;
                    }
                }
            }
        }

        private static void CheckItems(RoundPlan plan, PlannerSettings settings, PuzzleDefinition[] definitions, List<string> errors)
        {
            var needed = new List<string>();
            for (int i = 0; i < definitions.Length; i++)
            {
                if (definitions[i] != null && definitions[i].HasRequiredItem && !needed.Contains(definitions[i].RequiredItemId))
                {
                    needed.Add(definitions[i].RequiredItemId);
                }
            }
            if (settings.HasShovel && !needed.Contains(settings.ShovelItemId))
            {
                needed.Add(settings.ShovelItemId);
            }

            var listed = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < plan.RequiredItems.Count; i++)
            {
                string item = plan.RequiredItems[i];
                if (item.Length == 0)
                {
                    errors.Add("RequiredItems has an empty item id");
                    continue;
                }
                if (!listed.Add(item))
                {
                    errors.Add("item '" + item + "' is listed twice in RequiredItems");
                }
                if (!needed.Contains(item))
                {
                    errors.Add("item '" + item + "' is not needed this round (no chosen puzzle needs it), so it must not be placed");
                }
            }
            for (int i = 0; i < needed.Count; i++)
            {
                if (!listed.Contains(needed[i]))
                {
                    errors.Add("item '" + needed[i] + "' is needed this round but missing from RequiredItems");
                }
            }

            // givers
            for (int i = 0; i < plan.Puzzles.Count; i++)
            {
                PlannedPuzzle p = plan.Puzzles[i];
                if (p.HasReward && !listed.Contains(p.RewardItemId))
                {
                    errors.Add(Label(plan, i) + " rewards '" + p.RewardItemId + "', which is not a round item");
                }
            }
            foreach (string item in needed)
            {
                int givers = 0;
                for (int i = 0; i < plan.Puzzles.Count; i++)
                {
                    if (string.Equals(plan.Puzzles[i].RewardItemId, item, StringComparison.Ordinal))
                    {
                        givers++;
                    }
                }
                if (givers == 0)
                {
                    errors.Add("item '" + item + "' is not rewarded by any puzzle");
                }
                else if (givers > 1)
                {
                    errors.Add("item '" + item + "' is rewarded by " + givers + " puzzles");
                }
            }

            // order: needed items come from an earlier stage or from another puzzle of the same stage
            for (int i = 0; i < plan.Puzzles.Count; i++)
            {
                if (definitions[i] == null || !definitions[i].HasRequiredItem)
                {
                    continue;
                }
                string item = definitions[i].RequiredItemId;
                int giver = plan.IndexOfRewardGiver(item);
                if (giver == i)
                {
                    errors.Add(Label(plan, i) + " rewards the '" + item + "' it needs itself");
                }
                else if (giver >= 0 && plan.Puzzles[giver].StageIndex > plan.Puzzles[i].StageIndex)
                {
                    errors.Add(Label(plan, i) + " needs '" + item + "' in stage " + plan.Puzzles[i].StageIndex
                        + ", but it is only rewarded in the later stage " + plan.Puzzles[giver].StageIndex);
                }
            }

            if (settings.HasShovel)
            {
                int giver = plan.IndexOfRewardGiver(settings.ShovelItemId);
                if (giver >= 0 && plan.Puzzles[giver].StageIndex != settings.LastStage)
                {
                    errors.Add("the '" + settings.ShovelItemId + "' must be rewarded in the last stage " + settings.LastStage
                        + ", not in stage " + plan.Puzzles[giver].StageIndex);
                }
            }

            CheckCycles(plan, definitions, errors);
        }

        private static void CheckCycles(RoundPlan plan, PuzzleDefinition[] definitions, List<string> errors)
        {
            int count = plan.Puzzles.Count;
            for (int start = 0; start < count; start++)
            {
                int current = start;
                for (int hop = 0; hop < count; hop++)
                {
                    if (definitions[current] == null || !definitions[current].HasRequiredItem)
                    {
                        break;
                    }
                    int giver = plan.IndexOfRewardGiver(definitions[current].RequiredItemId);
                    if (giver < 0)
                    {
                        break;
                    }
                    if (giver == start)
                    {
                        errors.Add("item dependency cycle: " + Label(plan, start) + " waits (through the items it needs) on its own reward");
                        return;
                    }
                    current = giver;
                }
            }
        }

        private static void CheckSpots(RoundPlan plan, HouseIndex house, PlannerSettings settings, List<string> errors)
        {
            HouseSpotEntry key = FindSpot(house.KeyHideSpots, plan.HouseKeySpotId);
            if (key == null)
            {
                errors.Add("HouseKeySpot " + plan.HouseKeySpotId + " is not a KeyHideSpot of the house");
            }
            else if (!RoundPlanRules.IsKeyHideCandidate(house, key))
            {
                errors.Add("HouseKeySpot " + plan.HouseKeySpotId + " is not in a garden room");
            }

            HouseSpotEntry cologne = FindSpot(house.CologneSpots, plan.CologneSpotId);
            if (cologne == null)
            {
                errors.Add("CologneSpot " + plan.CologneSpotId + " is not a CologneSpawnSpot of the house");
            }
            else if (!RoundPlanRules.IsCologneCandidate(house, cologne, settings))
            {
                errors.Add("CologneSpot " + plan.CologneSpotId + " is not in a room of the active stages " + settings.FirstStage + ".."
                    + settings.LastStage);
            }

            bool cellarExists = RoundPlanRules.DigCandidates(house).Count > 0;
            if (!plan.DigSpotId.IsValid)
            {
                if (cellarExists)
                {
                    errors.Add("the house has DigSpots in the cellar but the plan picks none");
                }
                return;
            }
            HouseSpotEntry dig = FindSpot(house.DigSpots, plan.DigSpotId);
            if (dig == null)
            {
                errors.Add("DigSpot " + plan.DigSpotId + " is not a DigSpot of the house");
            }
            else if (!RoundPlanRules.IsDigCandidate(house, dig))
            {
                errors.Add("DigSpot " + plan.DigSpotId + " is not in a cellar room");
            }
        }

        // ------------------------------------------------------------------------------------------------

        private static string Label(RoundPlan plan, int index)
        {
            return "puzzle " + index + " (" + plan.Puzzles[index].Type + ")";
        }

        private static bool Contains(IReadOnlyList<int> values, int value)
        {
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] == value)
                {
                    return true;
                }
            }
            return false;
        }

        private static HousePuzzleSlotEntry FindPuzzleSlot(HouseIndex house, NetId id)
        {
            for (int i = 0; i < house.PuzzleSlots.Count; i++)
            {
                if (house.PuzzleSlots[i].NetId == id)
                {
                    return house.PuzzleSlots[i];
                }
            }
            return null;
        }

        private static HouseReferenceSlotEntry FindReferenceSlot(HouseIndex house, NetId id)
        {
            for (int i = 0; i < house.ReferenceSlots.Count; i++)
            {
                if (house.ReferenceSlots[i].NetId == id)
                {
                    return house.ReferenceSlots[i];
                }
            }
            return null;
        }

        private static HouseSpotEntry FindSpot(IReadOnlyList<HouseSpotEntry> spots, NetId id)
        {
            if (!id.IsValid)
            {
                return null;
            }
            for (int i = 0; i < spots.Count; i++)
            {
                if (spots[i].NetId == id)
                {
                    return spots[i];
                }
            }
            return null;
        }
    }
}
