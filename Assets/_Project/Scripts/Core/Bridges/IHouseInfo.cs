using System.Collections.Generic;
using UnityEngine;

namespace CinliMahzen.Core
{
    /// <summary>Static data of the hand-built house (Teknik §4.8). Implemented by World (HouseService).</summary>
    public interface IHouseInfo
    {
        /// <summary>Spirits are clamped inside these bounds.</summary>
        Bounds Bounds { get; }
        Vector3 HumanSpawn { get; }
        IReadOnlyList<Vector3> JinnSpawns { get; }
    }
}
