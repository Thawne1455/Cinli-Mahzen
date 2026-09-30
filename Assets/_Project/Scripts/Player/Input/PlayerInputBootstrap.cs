using CinliMahzen.Core;
using UnityEngine;

namespace CinliMahzen.Player
{
    /// <summary>Registers the LocalInputSource as a service during auto-bootstrap.</summary>
    internal static class PlayerInputBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void AddHook()
        {
            GameBootstrap.AddHook(() =>
            {
                var input = new LocalInputSource();
                GameServices.Register(input);
                Application.quitting += input.Dispose;
            });
        }
    }
}
