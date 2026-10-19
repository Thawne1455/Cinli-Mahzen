using System.Collections.Generic;
using CinliMahzen.Core;
using UnityEngine;

namespace CinliMahzen.World
{
    /// <summary>
    /// A place where RoundSetupPlanner may put an interactive puzzle (Teknik 8). Facing: the marker's forward (+Z) points from
    /// the wall into the room. The puzzle must match <see cref="Size"/> and its type must be in <see cref="AllowedTypes"/>.
    /// </summary>
    public sealed class PuzzleSlot : NetHouseMarker
    {
        [Tooltip("Stage whose gate this puzzle helps to open (1 = ground floor, 2 = first floor).")]
        [SerializeField] private int stageIndex;
        [Tooltip("RoomMarker.RoomId of the room the slot is in; must be a room of the stage.")]
        [SerializeField] private int roomId;
        [SerializeField] private PuzzleSlotSize slotSize;
        [SerializeField] private PuzzleType[] allowedTypes = new PuzzleType[0];
        [Tooltip("Where the reward item appears when the puzzle is solved. Empty = this transform.")]
        [SerializeField] private Transform rewardPoint;

        public int StageIndex
        {
            get { return stageIndex; }
        }

        public int RoomId
        {
            get { return roomId; }
        }

        public PuzzleSlotSize Size
        {
            get { return slotSize; }
        }

        public IReadOnlyList<PuzzleType> AllowedTypes
        {
            get { return allowedTypes; }
        }

        public Transform RewardPoint
        {
            get { return rewardPoint != null ? rewardPoint : transform; }
        }

        /// <summary>Used by the graybox builder and tests. Copies <paramref name="newAllowedTypes"/>.</summary>
        public void Configure(int newStageIndex, int newRoomId, PuzzleSlotSize newSize,
            IReadOnlyList<PuzzleType> newAllowedTypes, Transform newRewardPoint)
        {
            stageIndex = newStageIndex;
            roomId = newRoomId;
            slotSize = newSize;
            allowedTypes = new PuzzleType[newAllowedTypes.Count];
            for (int i = 0; i < allowedTypes.Length; i++)
            {
                allowedTypes[i] = newAllowedTypes[i];
            }
            rewardPoint = newRewardPoint;
        }

        protected override Color GizmoColor
        {
            get { return new Color(1f, 0.9f, 0.1f); }
        }
    }
}
