using System;
using System.Collections.Generic;
using CinliMahzen.Core.Net;
using UnityEngine;

namespace CinliMahzen.Core
{
    /// <summary>
    /// Auto-bootstrap (Teknik §3): before the first scene loads, GameServices is filled for offline hotseat
    /// (OfflinePlayerRegistry, OfflineNetBridge, EntityRegistry, GameBalanceConfig), then module hooks run (input, debug tools, ...).
    /// Works for every scene — Boot, Game, Sandbox — so Play can be pressed anywhere.
    /// Modules add hooks from [RuntimeInitializeOnLoadMethod(AfterAssembliesLoaded)] via <see cref="AddHook"/>.
    /// </summary>
    public static class GameBootstrap
    {
        public const string BootSettingsResource = "CM_BootSettings";

        private static readonly List<Action> Hooks = new List<Action>();

        public static bool IsBooted { get; private set; }

        public static void AddHook(Action hook)
        {
            if (hook == null)
                return;
            if (IsBooted)
                hook();
            else
                Hooks.Add(hook);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Hooks.Clear();
            IsBooted = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoBootstrap()
        {
            BootSettings settings = Resources.Load<BootSettings>(BootSettingsResource);
            if (settings != null && !settings.AutoBootstrap)
                return;
            BootOffline(settings);
        }

        /// <summary>Offline hotseat boot. Idempotent.</summary>
        public static void BootOffline(BootSettings settings)
        {
            if (IsBooted)
                return;

            // Hotseat: the local player of the network bridge is the player currently being played, i.e. the registry's.
            // Roles are empty here; HotseatBootstrap (DebugTools) assigns round 0, the MatchController takes over with TODO 1.10.
            var players = new OfflinePlayerRegistry();
            GameServices.Register<IPlayerRegistry>(players);
            GameServices.Register<INetBridge>(new OfflineNetBridge(localPlayer: () => players.LocalPlayer));
            GameServices.Register<IEntityRegistry>(new EntityRegistry());
            // TODO 4.2: player settings (PlayerPrefs) and the menu input focus.
            var gameSettings = new GameSettings(new PlayerPrefsSettingsStore());
            gameSettings.Apply();
            GameServices.Register(gameSettings);
            GameServices.Register(new MenuFocus());
            if (settings != null && settings.Config != null)
                GameServices.Register(settings.Config);
            else
                CMLog.Error("Boot", "Resources/" + BootSettingsResource + " or its GameBalanceConfig is missing — run CinliMahzen/Setup A/Build Core Scenes");

            IsBooted = true;
            for (int i = 0; i < Hooks.Count; i++)
            {
                try
                {
                    Hooks[i]();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            Hooks.Clear();

            CMLog.Info("Boot", "Bootstrap OK (offline hotseat)");
        }
    }
}
