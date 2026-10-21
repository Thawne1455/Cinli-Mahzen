using CinliMahzen.Core;
using UnityEngine;

namespace CinliMahzen.Visibility
{
    /// <summary>
    /// Auto-bootstrap hook of the visibility module (TODO 3.9): creates the hidden persistent <see cref="VisibilityService"/> and registers the
    /// "visibility" Dump State section. Same shape as the Spirit hook in CM.Jinn.
    /// </summary>
    internal static class VisibilityBootstrap
    {
        private const string DebugStateKey = "visibility";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void AddHook()
        {
            GameBootstrap.AddHook(Boot);
        }

        private static void Boot()
        {
            var go = new GameObject("Visibility");
            Object.DontDestroyOnLoad(go);
            VisibilityService service = go.AddComponent<VisibilityService>();
            DebugState.Register(DebugStateKey, service.BuildDebugJson);
        }
    }
}
