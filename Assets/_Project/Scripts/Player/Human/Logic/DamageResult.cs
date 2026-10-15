namespace CinliMahzen.Player
{
    /// <summary>
    /// Result of <see cref="HumanHealthCore.ApplyDamage"/>. The knockdown fields are only forwarded
    /// (the health core does not run the knockdown timer); the MonoBehaviour side hands them to the motor.
    /// </summary>
    public readonly struct DamageResult
    {
        public readonly DamageOutcome Outcome;

        /// <summary>HP after the call (unchanged current HP when <see cref="DamageOutcome.Ignored"/>).</summary>
        public readonly int HpAfter;

        /// <summary>Faint count after the call (unchanged current count when <see cref="DamageOutcome.Ignored"/>).</summary>
        public readonly int FaintCount;

        /// <summary>The applied hit carried <c>DamageFlags.Knockdown</c>. Always false when ignored.
        /// May be true together with Fainted/Lost — the caller decides that fainting wins.</summary>
        public readonly bool Knockdown;

        /// <summary><c>DamageInfo.KnockdownTime</c> (s) when <see cref="Knockdown"/> is true, otherwise 0.</summary>
        public readonly float KnockdownTime;

        public DamageResult(DamageOutcome outcome, int hpAfter, int faintCount, bool knockdown, float knockdownTime)
        {
            Outcome = outcome;
            HpAfter = hpAfter;
            FaintCount = faintCount;
            Knockdown = knockdown;
            KnockdownTime = knockdownTime;
        }

        /// <summary>True when the hit changed state (anything but <see cref="DamageOutcome.Ignored"/>).</summary>
        public bool IsApplied => Outcome != DamageOutcome.Ignored;
    }
}
