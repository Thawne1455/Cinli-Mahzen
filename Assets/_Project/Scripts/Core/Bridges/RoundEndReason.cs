namespace CinliMahzen.Core
{
    /// <summary>Why a round ended (Teknik §9). Wire value — do not reorder.</summary>
    public enum RoundEndReason : byte
    {
        None = 0,
        /// <summary>Human fainted FaintsToLose times → JinnsWin.</summary>
        Faint = 1,
        /// <summary>Treasure dug in the cellar → SeekersWin.</summary>
        TreasureDug = 2,
        /// <summary>Sunrise counter ran out → JinnsWin.</summary>
        Timeout = 3,
    }
}
