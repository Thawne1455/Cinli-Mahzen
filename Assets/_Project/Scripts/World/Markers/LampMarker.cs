using UnityEngine;

namespace CinliMahzen.World
{
    /// <summary>
    /// Where a lamp hangs (Teknik 8). Every indoor room has at least one; the lamp itself (PD_Lamp, light, break/repair)
    /// is built by the light system (TODO 3.4). The room is lit while any of its lamps is on.
    /// </summary>
    public sealed class LampMarker : NetHouseMarker
    {
        [Tooltip("RoomMarker.RoomId of the room this lamp lights.")]
        [SerializeField] private int roomId;

        public int RoomId
        {
            get { return roomId; }
        }

        /// <summary>Used by the graybox builder and tests.</summary>
        public void Configure(int newRoomId)
        {
            roomId = newRoomId;
        }

        protected override Color GizmoColor
        {
            get { return new Color(1f, 1f, 0.6f); }
        }
    }
}
