using UnityEngine;

namespace CinliMahzen.World
{
    /// <summary>The wall switch of a room (Teknik 8): the human walks to it to toggle the room's lamps. Every indoor room has at least one, next to a door.</summary>
    public sealed class LightSwitch : NetHouseMarker
    {
        [Tooltip("RoomMarker.RoomId of the room this switch controls.")]
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
            get { return new Color(0.3f, 1f, 0.3f); }
        }
    }
}
