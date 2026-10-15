using UnityEngine;

namespace CinliMahzen.Core
{
    /// <summary>Placeholder until Player's HumanHealth/HumanLantern: standing human, weak lantern. Settable for tests/sandboxes.</summary>
    public sealed class StubHumanStatusQuery : IHumanStatusQuery
    {
        public Vector3 Position { get; set; } = Vector3.zero;
        public bool IsFainted { get; set; }
        public bool IsBlessedLanternActive { get; set; }
        /// <summary>Used by <see cref="IsInLanternSafeZone"/>. Tests set it from GameBalanceConfig.LanternSafeZoneRadius; 0 = no zone.</summary>
        public float SafeZoneRadius { get; set; }

        public bool IsInLanternSafeZone(Vector3 worldPos)
        {
            return IsBlessedLanternActive && (worldPos - Position).sqrMagnitude <= SafeZoneRadius * SafeZoneRadius;
        }
    }
}
