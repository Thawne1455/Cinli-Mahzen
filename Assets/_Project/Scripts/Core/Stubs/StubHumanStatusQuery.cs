using UnityEngine;

namespace CinliMahzen.Core
{
    /// <summary>Placeholder until Player's HumanHealth/HumanLantern: standing human, weak lantern. Settable for tests/sandboxes.</summary>
    public sealed class StubHumanStatusQuery : IHumanStatusQuery
    {
        public Vector3 Position { get; set; } = Vector3.zero;
        public bool IsFainted { get; set; }
        public bool IsBlessedLanternActive { get; set; }
        /// <summary>Must be set explicitly (e.g. from GameBalanceConfig.LanternSafeZoneRadius): the default 0 means IsInLanternSafeZone is always false, even with the blessed lantern.</summary>
        public float SafeZoneRadius { get; set; }

        public bool IsInLanternSafeZone(Vector3 worldPos)
        {
            return IsBlessedLanternActive && (worldPos - Position).sqrMagnitude <= SafeZoneRadius * SafeZoneRadius;
        }
    }
}
