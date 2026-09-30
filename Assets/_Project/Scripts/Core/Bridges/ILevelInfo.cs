using System.Collections.Generic;
using UnityEngine;

namespace CinliMahzen.Core
{
    /// <summary>Active level data. Implemented by C (World).</summary>
    public interface ILevelInfo
    {
        Bounds Bounds { get; }
        int RoomCount { get; }
        int RoomOf(Vector3 p);
        Vector3 HumanSpawn { get; }
        IReadOnlyList<Vector3> JinnSpawns { get; }
    }
}
