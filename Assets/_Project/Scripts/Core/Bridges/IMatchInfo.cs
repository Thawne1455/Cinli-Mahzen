namespace CinliMahzen.Core
{
    /// <summary>Match state (Teknik §4.8). Implemented by Match.</summary>
    public interface IMatchInfo
    {
        MatchState State { get; }
        int RoundIndex { get; }
        /// <summary>Net.Time at which the current state ends. Grows by StageTimeBonus when a gate opens.</summary>
        double StateEndTime { get; }
        /// <summary>False until JinnWakeDelay has passed in Playing.</summary>
        bool JinnsAwake { get; }
        /// <summary>Human faints this round; FaintsToLose ends the round.</summary>
        int Faints { get; }
    }
}
