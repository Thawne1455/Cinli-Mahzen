namespace CinliMahzen.Possession
{
    /// <summary>
    /// Tuning consumed by <see cref="PossessionArbiterCore"/>. Built from <c>GameBalanceConfig</c>
    /// (PossessRange, PossessTime, ReenterCooldown) by the MonoBehaviour side — no literals here.
    /// </summary>
    public readonly struct PossessionRules
    {
        /// <summary><c>PossessRange</c> (m).</summary>
        public readonly float PossessRange;

        /// <summary>Server-side leniency added to <see cref="PossessRange"/> (Tech §6.2: "+ 0.5").</summary>
        public readonly float RangeTolerance;

        /// <summary><c>PossessTime</c> (s) — Entering duration.</summary>
        public readonly float PossessTime;

        /// <summary><c>ReenterCooldown</c> (s) — same player, same object.</summary>
        public readonly float ReenterCooldown;

        public PossessionRules(float possessRange, float rangeTolerance, float possessTime, float reenterCooldown)
        {
            PossessRange = possessRange;
            RangeTolerance = rangeTolerance;
            PossessTime = possessTime;
            ReenterCooldown = reenterCooldown;
        }

        public float MaxDistance => PossessRange + RangeTolerance;
    }
}
