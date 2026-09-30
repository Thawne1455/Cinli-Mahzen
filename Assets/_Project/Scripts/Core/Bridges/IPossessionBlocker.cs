using UnityEngine;

namespace CinliMahzen.Core
{
    /// <summary>Implemented by A (salt zones). Consumed by B (PossessionArbiter).</summary>
    public interface IPossessionBlocker
    {
        bool Blocks(Vector3 worldPos);
    }
}
