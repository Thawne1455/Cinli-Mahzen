namespace CinliMahzen.Possession
{
    /// <summary>The good jinn's exorcism tuning (TODO 3.5), from GameBalanceConfig (ExorciseRange + NetRangeTolerance, ExorciseCooldown, ExorciseStun, ExorcisePossessLock).</summary>
    public readonly struct ExorciseSettings
    {
        /// <summary>Largest distance (m) from the good jinn to the object, tolerance included.</summary>
        public readonly float Range;
        public readonly float Cooldown;
        public readonly float Stun;
        public readonly float PossessLock;

        public ExorciseSettings(float range, float cooldown, float stun, float possessLock)
        {
            Range = range;
            Cooldown = cooldown;
            Stun = stun;
            PossessLock = possessLock;
        }
    }
}
