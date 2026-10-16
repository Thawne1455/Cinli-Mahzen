using CinliMahzen.Core;
using UnityEngine;

namespace CinliMahzen.Player
{
    /// <summary>Attaches the InteractionAuthority to the network bridge during auto-bootstrap.</summary>
    internal static class InteractionBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void AddHook()
        {
            GameBootstrap.AddHook(() =>
            {
                var authority = new InteractionAuthority(GameServices.Net, GameServices.Entities, GameServices.Config);
                authority.Attach();
            });
        }
    }
}
