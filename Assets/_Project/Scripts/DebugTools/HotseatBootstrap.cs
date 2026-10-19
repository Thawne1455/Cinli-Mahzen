using System.Globalization;
using System.Text;
using CinliMahzen.Core;
using CinliMahzen.Match;
using UnityEngine;

namespace CinliMahzen.DebugTools
{
    /// <summary>
    /// Hotseat start (TODO 1.3): during auto-bootstrap it assigns round 0 of the role rotation to the registry
    /// (P1 Human, P2 Good Jinn, P3-P4 Evil Jinn, Oyun §10) and registers the "players" Dump State section.
    /// CM.Core cannot see CM.Match, so the table comes from here rather than from GameBootstrap. Once the MatchController exists
    /// (TODO 1.10 integration) it assigns the roles of every round itself and this start-up assignment is only the
    /// "press Play in any scene" convenience; PawnSpawners in the scene react to the registry's RolesChanged either way.
    /// </summary>
    internal static class HotseatBootstrap
    {
        private const string DebugStateKey = "players";
        private const int FirstRound = 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void AddHook()
        {
            GameBootstrap.AddHook(Boot);
        }

        private static void Boot()
        {
            DebugState.Register(DebugStateKey, BuildPlayersJson);

            var registry = GameServices.Players as OfflinePlayerRegistry;
            if (registry == null)
                return;
            Role[] roles = RoleRotation.CreateRoles(FirstRound, registry.Players.Count);
            if (!registry.SetRoles(roles))
                CMLog.Warn("Hotseat", "Round " + FirstRound + " roles rejected by the registry");
        }

        /// <summary>[{"id":"P0","role":"Human","local":true,"pawn":"N1","pos":[x,y,z]}, ...]. Scans the scene — dump time only.</summary>
        private static string BuildPlayersJson()
        {
            IPlayerRegistry players = GameServices.Players;
            if (players == null)
                return "[]";

            CultureInfo ci = CultureInfo.InvariantCulture;
            PawnBase[] pawns = Object.FindObjectsByType<PawnBase>(FindObjectsSortMode.None);
            var sb = new StringBuilder(256);
            sb.Append('[');
            for (int i = 0; i < players.Players.Count; i++)
            {
                PlayerInfo info = players.Players[i];
                PawnBase pawn = FindPawnOf(pawns, info.Id);
                if (i > 0)
                    sb.Append(',');
                sb.Append("{\"id\":").Append(DebugState.Quote(info.Id.ToString()))
                  .Append(",\"role\":").Append(DebugState.Quote(info.Role.ToString()))
                  .Append(",\"local\":").Append(info.Id == players.LocalPlayer ? "true" : "false");
                if (pawn != null)
                {
                    Vector3 p = pawn.transform.position;
                    sb.Append(",\"pawn\":").Append(DebugState.Quote(pawn.NetId.ToString()))
                      .Append(",\"pos\":[").Append(p.x.ToString("0.00", ci)).Append(',').Append(p.y.ToString("0.00", ci))
                      .Append(',').Append(p.z.ToString("0.00", ci)).Append(']');
                }
                else
                {
                    sb.Append(",\"pawn\":null,\"pos\":null");
                }
                sb.Append('}');
            }
            sb.Append(']');
            return sb.ToString();
        }

        private static PawnBase FindPawnOf(PawnBase[] pawns, PlayerId owner)
        {
            for (int i = 0; i < pawns.Length; i++)
            {
                if (pawns[i].Owner == owner)
                    return pawns[i];
            }
            return null;
        }
    }
}
