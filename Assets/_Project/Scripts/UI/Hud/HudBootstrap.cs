using CinliMahzen.Core;
using UnityEngine;

namespace CinliMahzen.UI
{
    /// <summary>
    /// Auto-bootstrap hook of the UI module (TODO 3.11): creates the hidden persistent <see cref="HudView"/>. It draws only while a match runs
    /// (not in Lobby), so it is harmless in the main menu. Same shape as the Spirit hook in CM.Jinn.
    /// </summary>
    internal static class HudBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void AddHook()
        {
            GameBootstrap.AddHook(Boot);
        }

        private static void Boot()
        {
            var go = new GameObject("Hud");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<HudView>();
        }
    }
}
