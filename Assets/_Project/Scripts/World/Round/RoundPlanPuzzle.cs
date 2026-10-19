using System.Collections.Generic;
using CinliMahzen.Core;

namespace CinliMahzen.World
{
    /// <summary>
    /// One puzzle of a <see cref="RoundPlan"/> (Teknik 7.5): where it stands, which definition it uses, where its reference
    /// hangs, the seeded answer (+ reference rotation and trap values) and the quest item it rewards. Immutable; the arrays
    /// are copied in and only exposed as read-only views.
    /// </summary>
    public sealed class RoundPlanPuzzle
    {
        private static readonly int[] NoTraps = new int[0];

        private readonly int[] _solution;
        private readonly int[] _trapValues;

        /// <summary>Stage of the puzzle slot (1 = ground floor, 2 = first floor, ...).</summary>
        public int StageIndex => Slot.StageIndex;

        public HousePuzzleSlotEntry Slot { get; }
        public PuzzleDefinition Definition { get; }
        public HouseReferenceSlotEntry Reference { get; }

        public PuzzleType Type => Definition.Type;

        /// <summary>The answer = the state the interactive puzzle starts in (Oyun 4.2). One value per part, in [0, K).</summary>
        public IReadOnlyList<int> Solution => _solution;

        /// <summary>0, 90, 180 or 270; always 0 unless <see cref="PuzzleDefinition.ReferenceRotates"/>.</summary>
        public int ReferenceRotation { get; }

        /// <summary>Crossed-out decoy values on the reference (<see cref="PuzzleDefinition.TrapCount"/> of them); empty for most puzzles.</summary>
        public IReadOnlyList<int> TrapValues => _trapValues;

        /// <summary>ItemDefinition.Id that appears at the slot's reward point when the puzzle is completed; empty = no item.</summary>
        public string RewardItemId { get; }

        public bool HasReward => RewardItemId.Length > 0;

        public RoundPlanPuzzle(HousePuzzleSlotEntry slot, PuzzleDefinition definition, HouseReferenceSlotEntry reference,
            int[] solution, int referenceRotation, int[] trapValues, string rewardItemId)
        {
            Slot = slot;
            Definition = definition;
            Reference = reference;
            _solution = solution != null ? (int[])solution.Clone() : NoTraps;
            ReferenceRotation = referenceRotation;
            _trapValues = trapValues != null && trapValues.Length > 0 ? (int[])trapValues.Clone() : NoTraps;
            RewardItemId = rewardItemId ?? string.Empty;
        }

        /// <summary>Independent copy of the answer (PuzzleState copies its input again).</summary>
        public int[] CopySolution()
        {
            return (int[])_solution.Clone();
        }

        /// <summary>Independent copy of the trap values, or null when there are none (the PuzzleState default).</summary>
        public int[] CopyTrapValuesOrNull()
        {
            return _trapValues.Length > 0 ? (int[])_trapValues.Clone() : null;
        }
    }
}
