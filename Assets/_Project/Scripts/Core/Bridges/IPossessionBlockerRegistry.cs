using UnityEngine;

namespace CinliMahzen.Core
{
    /// <summary>Implemented by A. Consumed by B (PossessionArbiter).</summary>
    public interface IPossessionBlockerRegistry
    {
        void Add(IPossessionBlocker b);
        void Remove(IPossessionBlocker b);
        bool IsBlocked(Vector3 p);
    }
}
