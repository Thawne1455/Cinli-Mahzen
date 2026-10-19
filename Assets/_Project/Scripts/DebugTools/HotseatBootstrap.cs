using System.Globalization;
using System.Text;
using CinliMahzen.Core;
using CinliMahzen.Match;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CinliMahzen.DebugTools
{
    /// <summary>
    /// Hotseat start (TODO 1.3): once the first scene has loaded it assigns round 0 of the role rotation to the registry
    /// (P1 Human, P2 Good Jinn, P3-P4 Evil Jinn, Oyun §10) - unless that scene has a <see cref="MatchController"/>, which then assigns
    /// the roles of every round itself (TODO 1.10) - and registers the "players" Dump State section.
    /// CM.Core cannot see CM.Match, so the table comes from here rather than from GameBootstrap. The assignment is only the
    /// "press Play in any scene" convenience (Sandbox has no MatchController). It waits for <c>sceneLoaded</c> because the scene's
    /// objects do not exist yet during the BeforeSceneLoad bootstrap, so a MatchController could not be asked for before.
    /// PawnSpawners in the scene react to the registry's RolesChanged either way.
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

            Scene active = SceneManager.GetActiveScene();
            if (active.IsValid() && active.isLoaded)
            {
                AssignRoundZeroUnlessMatchOwnsRoles();
                return;
            }
            SceneManager.sceneLoaded -= OnFirstSceneLoaded; // domain reload may be off: never subscribe twice
            SceneManager.sceneLoaded += OnFirstSceneLoaded;
        }

        private static void OnFirstSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnFirstSceneLoaded;
            AssignRoundZeroUnlessMatchOwnsRoles();
        }

        private static void AssignRoundZeroUnlessMatchOwnsRoles()
        {
            var registry = GameServices.Players as OfflinePlayerRegistry;
            if (registry == null)
                return;
            if (Object.FindFirstObjectByType<MatchController>() != null)
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
