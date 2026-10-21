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

                // TODO 3.8: lantern pulse and kick.
                if (GameServices.Net != null && GameServices.Players != null && GameServices.Config != null)
                {
                    var abilities = new HumanAbilityAuthority(GameServices.Net, GameServices.Players, GameServices.Entities, GameServices.Config);
                    abilities.Attach();
                }
            });
        }
    }
}
