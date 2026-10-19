using System;
using System.Collections.Generic;
using CinliMahzen.Core;

namespace CinliMahzen.World
{
    /// <summary>
    /// The layout of one round, computed by <see cref="RoundSetupPlanner"/> from a seed (Teknik 7.5, Oyun 9): which puzzle stands on
    /// which slot, where each reference hangs, the answers, which puzzle rewards which item, where the house key hides, where the
    /// cologne spawns and which dig spot is real. Every machine computes the same plan from the same seed and house, so only the
    /// seed travels over the network (RoundSetup message). Immutable plain data; the applier (TODO 2.7) turns it into scene state.
    /// </summary>
    public sealed class RoundPlan
    {
        private const ulong FnvOffsetBasis = 14695981039346656037UL;
        private const ulong FnvPrime = 1099511628211UL;
        private const int BitsPerByte = 8;
        private const int BitsPerInt = 32;

        public int Seed { get; }

        /// <summary>The active house stages, ascending (MVP: 1, 2).</summary>
        public IReadOnlyList<int> StageIndices { get; }

        /// <summary>All puzzles: by stage, inside a stage by puzzle slot NetId.</summary>
        public IReadOnlyList<PlannedPuzzle> Puzzles { get; }

        /// <summary>
        /// The quest items the puzzles reward this round (the items a chosen puzzle needs, then the shovel), each exactly once.
        /// Items no puzzle needs are not in the round (Oyun 6).
        /// </summary>
        public IReadOnlyList<string> RequiredItems { get; }

        /// <summary>KeyHideSpot that holds the house key.</summary>
        public NetId HouseKeySpotId { get; }

        /// <summary>CologneSpawnSpot that gets the PD_Cologne object.</summary>
        public NetId CologneSpotId { get; }

        /// <summary>The real DigSpot; <see cref="NetId.Invalid"/> while the house has no cellar (TODO 3.7).</summary>
        public NetId DigSpotId { get; }

        public RoundPlan(int seed, IEnumerable<int> stageIndices, IEnumerable<PlannedPuzzle> puzzles, IEnumerable<string> requiredItems,
            NetId houseKeySpotId, NetId cologneSpotId, NetId digSpotId)
        {
            Seed = seed;
            StageIndices = new List<int>(stageIndices ?? Array.Empty<int>()).AsReadOnly();
            Puzzles = new List<PlannedPuzzle>(puzzles ?? Array.Empty<PlannedPuzzle>()).AsReadOnly();
            var items = new List<string>();
            if (requiredItems != null)
            {
                foreach (string item in requiredItems)
                {
                    items.Add(item ?? string.Empty);
                }
            }
            RequiredItems = items.AsReadOnly();
            HouseKeySpotId = houseKeySpotId;
            CologneSpotId = cologneSpotId;
            DigSpotId = digSpotId;
        }

        public bool HasDigSpot => DigSpotId.IsValid;

        /// <summary>Number of puzzles in the given house stage.</summary>
        public int CountInStage(int stageIndex)
        {
            int count = 0;
            for (int i = 0; i < Puzzles.Count; i++)
            {
                if (Puzzles[i].StageIndex == stageIndex)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>Index in <see cref="Puzzles"/> of the puzzle that rewards the item, or -1.</summary>
        public int IndexOfRewardGiver(string itemId)
        {
            for (int i = 0; i < Puzzles.Count; i++)
            {
                if (Puzzles[i].HasReward && string.Equals(Puzzles[i].RewardItemId, itemId, StringComparison.Ordinal))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>Index in <see cref="Puzzles"/> of the puzzle on the slot, or -1.</summary>
        public int IndexOfSlot(NetId puzzleSlotId)
        {
            for (int i = 0; i < Puzzles.Count; i++)
            {
                if (Puzzles[i].PuzzleSlotId == puzzleSlotId)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// 64-bit FNV-1a over everything that shapes the round: stages, and per puzzle stage, slot, room, type, reference slot and
        /// room, answer, rotation, traps and reward; then the round items and the three spots. Integers are fed byte by byte
        /// (little-endian), strings as their length plus UTF-16 code units, so the value does not depend on culture, platform or
        /// pool order. The seed is NOT included (two seeds that give the same layout give the same hash), and neither is
        /// <see cref="PlannedPuzzle.DefinitionIndex"/>.
        /// </summary>
        public ulong ComputeHash()
        {
            ulong h = FnvOffsetBasis;
            h = Mix(h, StageIndices.Count);
            for (int i = 0; i < StageIndices.Count; i++)
            {
                h = Mix(h, StageIndices[i]);
            }

            h = Mix(h, Puzzles.Count);
            for (int i = 0; i < Puzzles.Count; i++)
            {
                PlannedPuzzle p = Puzzles[i];
                h = Mix(h, p.StageIndex);
                h = Mix(h, p.PuzzleSlotId.Value);
                h = Mix(h, p.RoomId);
                h = Mix(h, (int)p.Type);
                h = Mix(h, p.ReferenceSlotId.Value);
                h = Mix(h, p.ReferenceRoomId);
                h = Mix(h, p.Solution);
                h = Mix(h, p.ReferenceRotation);
                h = Mix(h, p.TrapValues);
                h = Mix(h, p.RewardItemId);
            }

            h = Mix(h, RequiredItems.Count);
            for (int i = 0; i < RequiredItems.Count; i++)
            {
                h = Mix(h, RequiredItems[i]);
            }

            h = Mix(h, HouseKeySpotId.Value);
            h = Mix(h, CologneSpotId.Value);
            h = Mix(h, DigSpotId.Value);
            return h;
        }

        public override string ToString()
        {
            return "RoundPlan(seed " + Seed + ", " + Puzzles.Count + " puzzles, hash 0x" + ComputeHash().ToString("X16") + ")";
        }

        private static ulong Mix(ulong h, int value)
        {
            unchecked
            {
                for (int shift = 0; shift < BitsPerInt; shift += BitsPerByte)
                {
                    h ^= (byte)(value >> shift);
                    h *= FnvPrime;
                }
            }
            return h;
        }

        private static ulong Mix(ulong h, IReadOnlyList<int> values)
        {
            h = Mix(h, values.Count);
            for (int i = 0; i < values.Count; i++)
            {
                h = Mix(h, values[i]);
            }
            return h;
        }

        private static ulong Mix(ulong h, string text)
        {
            h = Mix(h, text.Length);
            for (int i = 0; i < text.Length; i++)
            {
                h = Mix(h, text[i]);
            }
            return h;
        }
    }
}
