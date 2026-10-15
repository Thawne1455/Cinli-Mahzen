using UnityEngine;

namespace CinliMahzen.Core
{
    /// <summary>Human state other modules need (Teknik §4.8): lantern protection, fainting. Implemented by Player.</summary>
    public interface IHumanStatusQuery
    {
        Vector3 Position { get; }
        bool IsFainted { get; }
        /// <summary>Lantern is in strong mode (Blessed Oil collected).</summary>
        bool IsBlessedLanternActive { get; }
        /// <summary>True when the lantern is strong and <paramref name="worldPos"/> is within LanternSafeZoneRadius of the human: no scrambling there.</summary>
        bool IsInLanternSafeZone(Vector3 worldPos);
    }
}
