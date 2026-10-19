using CinliMahzen.Core;

namespace CinliMahzen.Match
{
    /// <summary>
    /// Seeds of a match (Teknik §9; CLAUDE.md: randomness that affects game state is a seeded <see cref="GameRandom"/>).
    /// <list type="bullet">
    /// <item>The <b>match seed</b> is chosen once by the authority when a match starts (<see cref="CreateMatchSeed"/>) and never leaves it.</item>
    /// <item>The <b>round seed</b> (<see cref="ForRound"/>) is what travels in <c>RoundSetup</c> and feeds the <c>RoundSetupPlanner</c>.
    /// It depends only on the match seed and the round index, so replaying a match seed replays every round's layout.</item>
    /// </list>
    /// Pure functions; the only non-deterministic input is the <c>entropy</c> the caller hands to <see cref="CreateMatchSeed"/>.
    /// Both results are positive, so 0 can keep meaning "no seed yet" (Lobby).
    /// </summary>
    public static class RoundSeeds
    {
        /// <summary>
        /// Match seed from caller-supplied <paramref name="entropy"/> (the authority passes <c>Environment.TickCount</c>; the raw value is
        /// scrambled through <see cref="GameRandom"/> so consecutive ticks do not give neighbouring seeds). Always &gt;= 1 and never
        /// equal to <paramref name="avoid"/> (pass the previous match seed so a restart really changes the layout).
        /// </summary>
        public static int CreateMatchSeed(int entropy, int avoid = 0)
        {
            int seed = new GameRandom(entropy).Range(1, int.MaxValue);
            return seed == avoid ? seed + 1 : seed;
        }

        /// <summary>
        /// Seed of round <paramref name="roundIndex"/> (0-based) of the match <paramref name="matchSeed"/>: the (roundIndex + 1)-th draw of
        /// <c>new GameRandom(matchSeed)</c>, in [1, int.MaxValue). A negative round index counts as round 0.
        /// Allocates a <see cref="GameRandom"/>; call once per round, never per frame.
        /// </summary>
        public static int ForRound(int matchSeed, int roundIndex)
        {
            var rng = new GameRandom(matchSeed);
            int seed = rng.Range(1, int.MaxValue);
            for (int round = 0; round < roundIndex; round++)
            {
                seed = rng.Range(1, int.MaxValue);
            }

            return seed;
        }
    }
}
