using System;
using CinliMahzen.Core;

namespace CinliMahzen.Jinn
{
    /// <summary>
    /// Tuning of the authority's Spirit Bump check, taken from <c>GameBalanceConfig</c> by the MonoBehaviour side — no literals here.
    /// </summary>
    public readonly struct SpiritBumpSettings
    {
        /// <summary><c>SpiritBumpRange</c> (m).</summary>
        public readonly float Range;

        /// <summary><c>NetRangeTolerance</c> (m) — slack the authority grants on the range check.</summary>
        public readonly float Tolerance;

        /// <summary><c>SpiritBumpCooldown</c> (s) — per requesting evil jinn.</summary>
        public readonly float Cooldown;

        public SpiritBumpSettings(float range, float tolerance, float cooldown)
        {
            Range = range;
            Tolerance = tolerance;
            Cooldown = cooldown;
        }

        public static SpiritBumpSettings FromConfig(GameBalanceConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            return new SpiritBumpSettings(config.SpiritBumpRange, config.NetRangeTolerance, config.SpiritBumpCooldown);
        }
    }
}
