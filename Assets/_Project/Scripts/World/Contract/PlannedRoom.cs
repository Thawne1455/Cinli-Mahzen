using System.Collections.Generic;

namespace CinliMahzen.World
{
    /// <summary>Unity-free room description. Bounds are level-local metres (XZ); Cell* are grid cells.</summary>
    public sealed class PlannedRoom
    {
        public int RoomId;
        public int CellX, CellZ, CellW, CellH;
        public float MinX, MinZ, MaxX, MaxZ;
        public RoomTag Tags;
        public List<int> Neighbors = new List<int>();

        public float CenterX => (MinX + MaxX) * 0.5f;
        public float CenterZ => (MinZ + MaxZ) * 0.5f;
    }
}
