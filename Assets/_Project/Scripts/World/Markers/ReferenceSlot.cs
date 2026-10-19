using System.Collections.Generic;
using CinliMahzen.Core;
using UnityEngine;

namespace CinliMahzen.World
{
    /// <summary>
    /// A place where the spirit-only reference (the answer) of a puzzle may hang (Teknik 8). The planner never puts a
    /// reference in the room of its puzzle. Facing: forward (+Z) points from the wall into the room.
    /// </summary>
    public sealed class ReferenceSlot : NetHouseMarker
    {
        [Tooltip("RoomMarker.RoomId of the room the slot is in.")]
        [SerializeField] private int roomId;
        [SerializeField] private PuzzleSlotSize slotSize;
        [SerializeField] private PuzzleType[] allowedTypes = new PuzzleType[0];

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

        /// <summary>Used by the graybox builder and tests. Copies <paramref name="newAllowedTypes"/>.</summary>
        public void Configure(int newRoomId, PuzzleSlotSize newSize, IReadOnlyList<PuzzleType> newAllowedTypes)
        {
            roomId = newRoomId;
            slotSize = newSize;
            allowedTypes = new PuzzleType[newAllowedTypes.Count];
            for (int i = 0; i < allowedTypes.Length; i++)
            {
                allowedTypes[i] = newAllowedTypes[i];
            }
        }

        protected override Color GizmoColor
        {
            get { return new Color(0.9f, 0.3f, 1f); }
        }
    }
}
