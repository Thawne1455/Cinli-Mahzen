using UnityEngine;

namespace CinliMahzen.World
{
    /// <summary>A candidate spot for the lemon cologne inside the house (Teknik 8, Oyun 6); the seed puts PD_Cologne on one of them. The bathroom is one of the candidates.</summary>
    public sealed class CologneSpawnSpot : NetHouseMarker
    {
        [Tooltip("RoomMarker.RoomId of the room the spot is in; must be a room inside the house.")]
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
            get { return new Color(0.7f, 0.4f, 1f); }
        }
    }
}
