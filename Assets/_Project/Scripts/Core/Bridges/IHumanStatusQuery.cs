using UnityEngine;

namespace CinliMahzen.Core
{
    /// <summary>Human state other modules need (Teknik §4.8): lantern protection, fainting. Implemented by Player.</summary>
    public interface IHumanStatusQuery
    {
        Vector3 Position { get; }
        bool IsFainted { get; }
        /// <summary>TODO 3.11 (HUD): mirrored HP, its maximum and this round's faint count.</summary>
        int Hp { get; }
        int MaxHp { get; }
        int Faints { get; }
        /// <summary>TODO 3.11 (HUD): sprint stamina 0..1. Simulated by the owning machine only; elsewhere it reads full.</summary>
        float Stamina01 { get; }
        /// <summary>Lantern is in strong mode (Blessed Oil collected).</summary>
        bool IsBlessedLanternActive { get; }
        /// <summary>True when the lantern is strong and <paramref name="worldPos"/> is within LanternSafeZoneRadius of the human: no scrambling there.</summary>
        bool IsInLanternSafeZone(Vector3 worldPos);
    }
}
