using System.Collections.Generic;
using UnityEngine;

namespace CinliMahzen.World
{
    /// <summary>Groups the rooms of one stage (Teknik 8): stage 0 = garden, 1.. = floors. Every room belongs to exactly one zone.</summary>
    public sealed class StageZone : HouseMarker
    {
        [SerializeField] private int stageIndex;
        [Tooltip("RoomMarker.RoomId of every room in this stage.")]
        [SerializeField] private int[] roomIds = new int[0];

        public int StageIndex
        {
            get { return stageIndex; }
        }

        public IReadOnlyList<int> RoomIds
        {
            get { return roomIds; }
        }

        /// <summary>Used by the graybox builder and tests. Copies <paramref name="newRoomIds"/>.</summary>
        public void Configure(int newStageIndex, IReadOnlyList<int> newRoomIds)
        {
            stageIndex = newStageIndex;
            roomIds = new int[newRoomIds.Count];
            for (int i = 0; i < roomIds.Length; i++)
            {
                roomIds[i] = newRoomIds[i];
            }
        }

        protected override Color GizmoColor
        {
            get { return new Color(0.6f, 0.6f, 0.6f); }
        }
    }
}
