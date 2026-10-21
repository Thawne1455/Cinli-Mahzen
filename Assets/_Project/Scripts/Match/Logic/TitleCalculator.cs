using System.Collections.Generic;
using CinliMahzen.Core;

namespace CinliMahzen.Match
{
    /// <summary>
    /// Match-end titles (Oyun §10, TODO 4.3), plain C#: each title goes to whoever has the most of its <see cref="StatKind"/> over the match;
    /// nobody gets it at 0, and a tie for the most shares it.
    /// </summary>
    public static class TitleCalculator
    {
        /// <summary>The stat behind <paramref name="title"/>.</summary>
        public static StatKind StatOf(TitleKind title)
        {
            switch (title)
            {
                case TitleKind.Electrician: return StatKind.WrongConfirm;
                case TitleKind.Saboteur: return StatKind.Scramble;
                case TitleKind.BulbKiller: return StatKind.LampBurst;
                case TitleKind.Navigator: return StatKind.Ping;
                case TitleKind.Kicker: return StatKind.MissedKick;
                case TitleKind.SlipperSniper: return StatKind.SlipperHit;
                default: return StatKind.Rescue;
            }
        }

        /// <summary>Clears <paramref name="results"/> and fills it in title order (allocation-free when it has capacity; call once per match).</summary>
        public static void Compute(IReadOnlyList<PlayerInfo> players, IMatchStats stats, List<MatchTitle> results)
        {
            results.Clear();
            for (int t = 0; t <= (int)TitleKind.Cologne; t++)
            {
                var title = (TitleKind)t;
                StatKind stat = StatOf(title);
                int best = 0;
                for (int i = 0; i < players.Count; i++)
                {
                    int count = stats.MatchCount(players[i].Id, stat);
                    if (count > best)
                        best = count;
                }
                if (best <= 0)
                    continue;
                for (int i = 0; i < players.Count; i++)
                {
                    if (stats.MatchCount(players[i].Id, stat) == best)
                        results.Add(new MatchTitle(title, players[i].Id, best));
                }
            }
        }
    }
}
