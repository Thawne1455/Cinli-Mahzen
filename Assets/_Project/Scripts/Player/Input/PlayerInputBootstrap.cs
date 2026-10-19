using CinliMahzen.Core;
using UnityEngine;

namespace CinliMahzen.Player
{
    /// <summary>Registers the LocalInputSource as a service (and as the ILocalInputProvider that pawns bind through) during auto-bootstrap.</summary>
    internal static class PlayerInputBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void AddHook()
        {
            GameBootstrap.AddHook(() =>
            {
                var input = new LocalInputSource();
                GameServices.Register(input);
                GameServices.Register<ILocalInputProvider>(input);
                Application.quitting += input.Dispose;
            });
        }
    }
}
