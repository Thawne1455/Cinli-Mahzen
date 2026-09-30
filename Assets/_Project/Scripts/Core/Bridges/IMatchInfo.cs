namespace CinliMahzen.Core
{
    /// <summary>Implemented by A (Match).</summary>
    public interface IMatchInfo
    {
        MatchState State { get; }
        int RoundIndex { get; }
        /// <summary>Net.Time at which the current state ends.</summary>
        double StateEndTime { get; }
        /// <summary>False until JinnWakeDelay has passed in Playing.</summary>
        bool JinnsAwake { get; }
    }
}
