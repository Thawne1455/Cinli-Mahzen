using System.Collections.Generic;
using CinliMahzen.Core;
using UnityEngine;

namespace CinliMahzen.World
{
    /// <summary>Plain-data copy of one <see cref="PuzzleSlot"/>, for RoundSetupPlanner and the validator.</summary>
    public sealed class HousePuzzleSlotEntry : HouseNetEntry
    {
        public int StageIndex { get; }
        public int RoomId { get; }
        public PuzzleSlotSize Size { get; }
        public IReadOnlyList<PuzzleType> AllowedTypes { get; }
        public Vector3 Position { get; }
        public Vector3 RewardPosition { get; }

        public HousePuzzleSlotEntry(string path, int stageIndex, int roomId, PuzzleSlotSize size,
            IEnumerable<PuzzleType> allowedTypes, Vector3 position, Vector3 rewardPosition, NetId netId = default)
            : base(path, netId)
        {
            StageIndex = stageIndex;
            RoomId = roomId;
            Size = size;
            var copy = new List<PuzzleType>();
            if (allowedTypes != null)
            {
                copy.AddRange(allowedTypes);
            }
            AllowedTypes = copy;
            Position = position;
            RewardPosition = rewardPosition;
        }

        public bool Allows(PuzzleType type)
        {
            for (int i = 0; i < AllowedTypes.Count; i++)
            {
                if (AllowedTypes[i] == type)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
