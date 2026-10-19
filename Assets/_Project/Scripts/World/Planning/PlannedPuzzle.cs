using System;
using System.Collections.Generic;
using CinliMahzen.Core;

namespace CinliMahzen.World
{
    /// <summary>
    /// One puzzle of a <see cref="RoundPlan"/> (Teknik 7.5): where it stands, which definition it uses, where its reference hangs,
    /// its answer and the item it rewards. Immutable; the arrays are copied in and only exposed read-only.
    /// </summary>
    public sealed class PlannedPuzzle
    {
        private readonly int[] _solution;
        private readonly int[] _trapValues;

        /// <summary>House stage index (1 = ground floor, ...).</summary>
        public int StageIndex { get; }

        /// <summary>NetId of the PuzzleSlot marker the interactive puzzle is placed on.</summary>
        public NetId PuzzleSlotId { get; }

        /// <summary>Room of the puzzle slot.</summary>
        public int RoomId { get; }

        public PuzzleType Type { get; }

        /// <summary>
        /// Index of the definition in the pool given to the planner. A local lookup aid only: it depends on the pool's order, so it
        /// is not part of <see cref="RoundPlan.ComputeHash"/>; <see cref="Type"/> is.
        /// </summary>
        public int DefinitionIndex { get; }

        /// <summary>NetId of the ReferenceSlot marker the answer view is placed on.</summary>
        public NetId ReferenceSlotId { get; }

        /// <summary>Room of the reference slot (never <see cref="RoomId"/>).</summary>
        public int ReferenceRoomId { get; }

        /// <summary>The answer: one value in [0, K_i) per part. The interactive puzzle starts in this state (Oyun 4.2).</summary>
        public IReadOnlyList<int> Solution { get; }

        /// <summary>Degrees the reference is drawn rotated by (0, 90, 180, 270); 0 for types without a rotated reference.</summary>
        public int ReferenceRotation { get; }

        /// <summary>Values crossed out on the reference (Wires); empty for other types.</summary>
        public IReadOnlyList<int> TrapValues { get; }

        /// <summary>ItemDefinition.Id the puzzle rewards when completed; empty = no item.</summary>
        public string RewardItemId { get; }

        public PlannedPuzzle(int stageIndex, NetId puzzleSlotId, int roomId, PuzzleType type, int definitionIndex,
            NetId referenceSlotId, int referenceRoomId, IList<int> solution, int referenceRotation, IList<int> trapValues,
            string rewardItemId)
        {
            StageIndex = stageIndex;
            PuzzleSlotId = puzzleSlotId;
            RoomId = roomId;
            Type = type;
            DefinitionIndex = definitionIndex;
            ReferenceSlotId = referenceSlotId;
            ReferenceRoomId = referenceRoomId;
            ReferenceRotation = referenceRotation;
            RewardItemId = rewardItemId ?? string.Empty;

            _solution = Copy(solution);
            _trapValues = Copy(trapValues);
            Solution = Array.AsReadOnly(_solution);
            TrapValues = Array.AsReadOnly(_trapValues);
        }

        public bool HasReward => RewardItemId.Length > 0;

        /// <summary>A new array with the answer (PuzzleState takes ownership of a copy anyway).</summary>
        public int[] CopySolution()
        {
            return Copy(_solution);
        }

        /// <summary>A new array with the trap values (empty when there are none).</summary>
        public int[] CopyTrapValues()
        {
            return Copy(_trapValues);
        }

        public override string ToString()
        {
            return Type + " @" + PuzzleSlotId + " (stage " + StageIndex + ", room " + RoomId + ") ref @" + ReferenceSlotId
                + " (room " + ReferenceRoomId + ")";
        }

        private static int[] Copy(IList<int> source)
        {
            if (source == null)
            {
                return Array.Empty<int>();
            }
            var copy = new int[source.Count];
            source.CopyTo(copy, 0);
            return copy;
        }
    }
}
