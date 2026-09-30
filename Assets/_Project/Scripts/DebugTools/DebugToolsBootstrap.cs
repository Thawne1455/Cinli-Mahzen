using System.Text;
using CinliMahzen.Core;
using CinliMahzen.Core.Net;
using UnityEngine;

namespace CinliMahzen.DebugTools
{
    /// <summary>
    /// During auto-bootstrap (editor / development builds): creates the persistent DebugRoot (hotkeys + overlay)
    /// and registers the "core" Dump State section.
    /// </summary>
    internal static class DebugToolsBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void AddHook()
        {
            GameBootstrap.AddHook(Boot);
        }

        private static void Boot()
        {
            DebugState.Register("core", BuildCoreJson);
            if (!Debug.isDebugBuild)
                return;
            var root = new GameObject("DebugRoot");
            Object.DontDestroyOnLoad(root);
            root.AddComponent<DebugHotkeys>();
            root.AddComponent<DebugOverlay>();
        }

        private static string BuildCoreJson()
        {
            var sb = new StringBuilder(512);
            INetBridge net = GameServices.Net;
            sb.Append("{\"booted\":").Append(GameBootstrap.IsBooted ? "true" : "false");
            sb.Append(",\"scene\":").Append(DebugState.Quote(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name));
            sb.Append(",\"timeScale\":").Append(Time.timeScale.ToString(System.Globalization.CultureInfo.InvariantCulture));
            if (net != null)
            {
                sb.Append(",\"net\":{\"online\":").Append(net.IsOnline ? "true" : "false")
                  .Append(",\"authority\":").Append(net.IsAuthority ? "true" : "false")
                  .Append(",\"local\":").Append(DebugState.Quote(net.LocalPlayer.ToString()))
                  .Append(",\"time\":").Append(net.Time.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)).Append('}');
            }

            IMatchInfo m = GameServices.Match;
            if (m != null)
            {
                sb.Append(",\"match\":{\"state\":").Append(DebugState.Quote(m.State.ToString()))
                  .Append(",\"round\":").Append(m.RoundIndex)
                  .Append(",\"jinnsAwake\":").Append(m.JinnsAwake ? "true" : "false")
                  .Append(",\"stub\":").Append(m is StubMatchInfo ? "true" : "false").Append('}');
            }

            IObjectiveInfo o = GameServices.Get<IObjectiveInfo>();
            if (o != null)
            {
                sb.Append(",\"objective\":{\"phase\":").Append(DebugState.Quote(o.Phase.ToString()))
                  .Append(",\"fragments\":").Append(o.Fragments)
                  .Append(",\"vaultOpen\":").Append(o.VaultOpen ? "true" : "false")
                  .Append(",\"goldCarrier\":").Append(DebugState.Quote(o.GoldCarrier.ToString()))
                  .Append(",\"stub\":").Append(o is StubObjectiveInfo ? "true" : "false").Append('}');
            }

            IPlayerRegistry players = GameServices.Players;
            sb.Append(",\"players\":[");
            if (players != null)
            {
                for (int i = 0; i < players.Players.Count; i++)
                {
                    PlayerInfo p = players.Players[i];
                    if (i > 0)
                        sb.Append(',');
                    sb.Append("{\"id\":").Append(DebugState.Quote(p.Id.ToString()))
                      .Append(",\"nick\":").Append(DebugState.Quote(p.Nickname))
                      .Append(",\"role\":").Append(DebugState.Quote(p.Role.ToString()))
                      .Append(",\"score\":").Append(p.Score).Append('}');
                }
            }
            sb.Append(']');

            var ents = GameServices.Entities as EntityRegistry;
            sb.Append(",\"entities\":").Append(ents != null ? ents.Count : 0);
            sb.Append(",\"configLoaded\":").Append(GameServices.Config != null ? "true" : "false");
            sb.Append('}');
            return sb.ToString();
        }
    }
}
