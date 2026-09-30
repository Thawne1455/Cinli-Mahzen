namespace CinliMahzen.Jinn
{
    /// <summary>
    /// Tuning for <see cref="JinnEnergyCore"/>. Energy values come from <c>GameBalanceConfig</c>
    /// (EnergyMax, EnergyStart, EnergyRegen, RageRegenMult); sync throttle values from the
    /// JinnEnergy component (Tech §6.5: broadcast on change ≥ 1, at most 4 Hz).
    /// </summary>
    public readonly struct JinnEnergySettings
    {
        public readonly float Max;
        public readonly float Start;
        /// <summary>Energy per second.</summary>
        public readonly float Regen;
        /// <summary>Regen multiplier while Öfke is active.</summary>
        public readonly float RageRegenMult;
        /// <summary>Minimum accumulated change before a regen broadcast.</summary>
        public readonly float SyncMinDelta;
        /// <summary>Minimum seconds between two regen broadcasts.</summary>
        public readonly float SyncMinInterval;

        public JinnEnergySettings(float max, float start, float regen, float rageRegenMult,
            float syncMinDelta, float syncMinInterval)
        {
            Max = max;
            Start = start;
            Regen = regen;
            RageRegenMult = rageRegenMult;
            SyncMinDelta = syncMinDelta;
            SyncMinInterval = syncMinInterval;
        }
    }
}
