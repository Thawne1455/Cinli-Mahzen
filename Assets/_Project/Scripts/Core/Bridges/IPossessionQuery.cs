using System.Collections.Generic;
using UnityEngine;

namespace CinliMahzen.Core
{
    /// <summary>Implemented by B (Possession). Consumed by A (lantern, HUD).</summary>
    public interface IPossessionQuery
    {
        bool IsPossessed(NetId obj);
        PlayerId PossessorOf(NetId obj);
        /// <summary>Clears <paramref name="results"/> and fills it with possessed objects inside the cone.</summary>
        void GetPossessedInCone(Vector3 origin, Vector3 dir, float range, float angleDeg, List<NetId> results);
    }
}
