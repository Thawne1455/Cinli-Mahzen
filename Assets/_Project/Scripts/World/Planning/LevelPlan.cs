using System.Collections.Generic;

namespace CinliMahzen.World
{
    /// <summary>Complete, Unity-free description of a level: what the Unity layer instantiates.</summary>
    public sealed class LevelPlan
    {
        public int Seed;
        public int Attempt;
        public int GridSize;
        public float CellSize;
        public List<PlannedRoom> Rooms = new List<PlannedRoom>();
        public List<PlannedWall> Walls = new List<PlannedWall>();
        public List<PlannedMarker> Markers = new List<PlannedMarker>();

        /// <summary>FNV-1a over every integer-rounded value, in list order (all lists are built in deterministic order).</summary>
        public ulong ComputeHash()
        {
            ulong h = 14695981039346656037UL;
            void Mix(long v) { unchecked { h ^= (ulong)v; h *= 1099511628211UL; } }
            Mix(GridSize);
            Mix((long)(CellSize * 100f));
            for (int i = 0; i < Rooms.Count; i++)
            {
                var r = Rooms[i];
                Mix(r.RoomId); Mix(r.CellX); Mix(r.CellZ); Mix(r.CellW); Mix(r.CellH); Mix((int)r.Tags);
                for (int k = 0; k < r.Neighbors.Count; k++) Mix(r.Neighbors[k]);
            }
            for (int i = 0; i < Walls.Count; i++)
            {
                var w = Walls[i];
                Mix(w.X); Mix(w.Z); Mix(w.Horizontal ? 1 : 0); Mix((int)w.Kind);
            }
            for (int i = 0; i < Markers.Count; i++)
            {
                var m = Markers[i];
                Mix((int)m.Kind); Mix(m.RoomId);
                Mix((long)System.Math.Round(m.X * 100f)); Mix((long)System.Math.Round(m.Y * 100f)); Mix((long)System.Math.Round(m.Z * 100f));
                Mix((long)System.Math.Round(m.YawDeg)); Mix((int)m.Category); Mix((int)m.Puzzle); Mix(m.WallFacing ? 1 : 0);
            }
            return h;
        }
    }
}
