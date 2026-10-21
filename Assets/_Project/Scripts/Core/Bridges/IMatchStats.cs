using System.Collections.Generic;

namespace CinliMahzen.Core
{
    /// <summary>
    /// Match statistics and titles (Oyun §10, TODO 4.3). Implemented by Match (StatsService, built from events every machine sees, so every
    /// machine agrees); consumed by the UI (round report, match end). Registered by the MatchController; null until then.
    /// </summary>
    public interface IMatchStats
    {
        /// <summary>This round's count of <paramref name="kind"/> for <paramref name="player"/>.</summary>
        int RoundCount(PlayerId player, StatKind kind);

        /// <summary>The whole match's count.</summary>
        int MatchCount(PlayerId player, StatKind kind);

        /// <summary>Titles of the finished match (empty before MatchEnd). Ties share a title.</summary>
        IReadOnlyList<MatchTitle> Titles { get; }
    }
}
