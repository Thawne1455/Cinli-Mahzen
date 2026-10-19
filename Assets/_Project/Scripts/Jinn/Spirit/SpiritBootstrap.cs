using CinliMahzen.Core;
using CinliMahzen.Core.Net;
using UnityEngine;

namespace CinliMahzen.Jinn
{
    /// <summary>
    /// Auto-bootstrap hook of the spirit module (TODO 1.6): attaches the <see cref="SpiritAuthority"/> to the network bridge and registers
    /// the "spirits" Dump State section; since TODO 1.8 it also creates the persistent <see cref="JinnEnergyRunner"/>. Same shape as the Interaction hook in CM.Player.
    /// </summary>
    internal static class SpiritBootstrap
    {
        private const string DebugStateKey = "spirits";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void AddHook()
        {
            GameBootstrap.AddHook(Boot);
        }

        private static void Boot()
        {
            DebugState.Register(DebugStateKey, SpiritController.BuildDebugJson);

            INetBridge net = GameServices.Net;
            IPlayerRegistry players = GameServices.Players;
            GameBalanceConfig config = GameServices.Config;
            if (net == null || players == null || config == null)
            {
                CMLog.Warn("Spirit", "GameServices.Net / Players / Config missing — the reference focus and the Spirit Bump have no authority");
                return;
            }

            var authority = new SpiritAuthority(net, players, SpiritBumpSettings.FromConfig(config), SpiritController.FindPosition);
            authority.Attach();

            // TODO 1.8: the real jinn energy (regen needs a frame tick, so it lives on a hidden persistent object).
            var energy = new GameObject("JinnEnergy");
            Object.DontDestroyOnLoad(energy);
            energy.AddComponent<JinnEnergyRunner>();
        }
    }
}
