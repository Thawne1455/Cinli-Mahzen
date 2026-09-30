using System;
using System.Collections.Generic;
using UnityEngine;

namespace CinliMahzen.World
{
    [Serializable]
    public class RoomInfo
    {
        public int RoomId;
        public Bounds Bounds;
        public RoomTag Tags;
        public List<int> Neighbors = new List<int>();
    }
}
