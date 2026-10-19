using CinliMahzen.Core;

namespace CinliMahzen.Match
{
    /// <summary>The tiered scoring values of Oyun §10 / Teknik §9 (TODO 3.10), from <see cref="GameBalanceConfig"/>. Plain data.</summary>
    public readonly struct ScoreRules
    {
        public readonly int PerPuzzle;
        public readonly int BasementOpen;
        public readonly int TreasureWin;
        public readonly int PerFaint;
        public readonly int JinnWin;
        public readonly int PerUnfinishedPuzzle;

        public ScoreRules(int perPuzzle, int basementOpen, int treasureWin, int perFaint, int jinnWin, int perUnfinishedPuzzle)
        {
            PerPuzzle = perPuzzle;
            BasementOpen = basementOpen;
            TreasureWin = treasureWin;
            PerFaint = perFaint;
            JinnWin = jinnWin;
            PerUnfinishedPuzzle = perUnfinishedPuzzle;
        }

        public static ScoreRules FromConfig(GameBalanceConfig config)
        {
            return new ScoreRules(config.ScorePerPuzzle, config.ScoreBasementOpen, config.ScoreTreasureWin, config.ScorePerFaint,
                config.JinnWinPoints, config.ScorePerUnfinishedPuzzle);
        }

        /// <summary>Seekers' treasure award: TreasureWin + whole seconds left on the sunrise counter (never negative).</summary>
        public int TreasureAward(double secondsLeft)
        {
            int left = secondsLeft > 0d ? (int)secondsLeft : 0;
            return TreasureWin + left;
        }

        /// <summary>Evil jinns' round-win award: JinnWin + unfinished puzzles × PerUnfinishedPuzzle.</summary>
        public int JinnWinAward(int unfinishedPuzzles)
        {
            return JinnWin + (unfinishedPuzzles > 0 ? unfinishedPuzzles : 0) * PerUnfinishedPuzzle;
        }

        public static bool IsSeeker(Role role)
        {
            return role == Role.Human || role == Role.GoodJinn;
        }
    }
}
