using System.Collections.Generic;
using UnityEngine;

namespace CinliMahzen.Core
{
    /// <summary>Placeholder until B's PossessionArbiter: nothing is possessed.</summary>
    public sealed class StubPossessionQuery : IPossessionQuery
    {
        public bool IsPossessed(NetId obj) => false;

        public PlayerId PossessorOf(NetId obj) => PlayerId.None;

        public void GetPossessedInCone(Vector3 origin, Vector3 dir, float range, float angleDeg, List<NetId> results)
        {
            results.Clear();
        }
    }
}
