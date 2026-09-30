using System.Collections.Generic;
using UnityEngine;

namespace CinliMahzen.World
{
    public class LevelLayout
    {
        /// <summary>Flight bounds for the jinn (world space).</summary>
        public Bounds Bounds;
        public int Seed;
        public List<RoomInfo> Rooms = new List<RoomInfo>();
        public List<LevelMarker> Markers = new List<LevelMarker>();
        /// <summary>RoomAdjacency[a,b] = number of doors between rooms a and b.</summary>
        public int[,] RoomAdjacency;
    }
}
