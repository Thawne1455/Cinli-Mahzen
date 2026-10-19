namespace CinliMahzen.Core
{
    /// <summary>Why a player's score changed (Oyun §10 tiered scoring, TODO 3.10). Wire value — append only.</summary>
    public enum ScoreReason : byte
    {
        /// <summary>New match: back to 0.</summary>
        Reset = 0,
        /// <summary>Seekers: a puzzle solved (ScorePerPuzzle).</summary>
        Puzzle = 1,
        /// <summary>Seekers: the cellar opened (ScoreBasementOpen).</summary>
        BasementOpened = 2,
        /// <summary>Seekers: treasure dug (ScoreTreasureWin + seconds left).</summary>
        TreasureWin = 3,
        /// <summary>Evil jinns: the human fainted (ScorePerFaint).</summary>
        Faint = 4,
        /// <summary>Evil jinns: round won (JinnWinPoints + unfinished puzzles × ScorePerUnfinishedPuzzle).</summary>
        JinnWin = 5,
    }
}
