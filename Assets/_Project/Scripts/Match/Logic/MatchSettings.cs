using System;
using CinliMahzen.Core;

namespace CinliMahzen.Match
{
    /// <summary>
    /// Tuning consumed by <see cref="MatchStateMachine"/>. Built from <c>GameBalanceConfig</c> by the
    /// MonoBehaviour side (<see cref="FromConfig"/>) — no literals in the machine (Oyun §14, "Match" + FaintsToLose).
    /// </summary>
    public readonly struct MatchSettings
    {
        /// <summary><c>RoleRevealDuration</c> (s).</summary>
        public readonly float RoleRevealDuration;

        /// <summary><c>IntroCountdown</c> (s).</summary>
        public readonly float IntroCountdown;

        /// <summary><c>BaseRoundDuration</c> (s) — Playing length before any stage bonus.</summary>
        public readonly float BaseRoundDuration;

        /// <summary><c>StageTimeBonus</c> (s) — added to the Sunrise counter per opened gate.</summary>
        public readonly float StageTimeBonus;

        /// <summary><c>JinnWakeDelay</c> (s) — evil jinns sleep for this long after Playing starts.</summary>
        public readonly float JinnWakeDelay;

        /// <summary><c>RoundEndDuration</c> (s).</summary>
        public readonly float RoundEndDuration;

        /// <summary><c>RoundsPerMatch</c>.</summary>
        public readonly int RoundsPerMatch;

        /// <summary><c>FaintsToLose</c> — human faint count at which the jinns win the round.</summary>
        public readonly int FaintsToLose;

        public MatchSettings(
            float roleRevealDuration,
            float introCountdown,
            float baseRoundDuration,
            float stageTimeBonus,
            float jinnWakeDelay,
            float roundEndDuration,
            int roundsPerMatch,
            int faintsToLose)
        {
            RoleRevealDuration = roleRevealDuration;
            IntroCountdown = introCountdown;
            BaseRoundDuration = baseRoundDuration;
            StageTimeBonus = stageTimeBonus;
            JinnWakeDelay = jinnWakeDelay;
            RoundEndDuration = roundEndDuration;
            RoundsPerMatch = roundsPerMatch;
            FaintsToLose = faintsToLose;
        }

        public static MatchSettings FromConfig(GameBalanceConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return new MatchSettings(
                config.RoleRevealDuration,
                config.IntroCountdown,
                config.BaseRoundDuration,
                config.StageTimeBonus,
                config.JinnWakeDelay,
                config.RoundEndDuration,
                config.RoundsPerMatch,
                config.FaintsToLose);
        }
    }
}
