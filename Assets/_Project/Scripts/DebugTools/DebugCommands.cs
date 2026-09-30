using CinliMahzen.Core;
using UnityEngine;

namespace CinliMahzen.DebugTools
{
    /// <summary>
    /// Single entry point for debug actions — used by F-key hotkeys and the CinliMahzen/Debug menu (MCP).
    /// Features whose systems do not exist yet log a TODO.
    /// </summary>
    public static class DebugCommands
    {
        public static void SwitchToPlayer(int index)
        {
            Todo("F" + (index + 1) + " Switch To Player " + (index + 1), "A1.5 PlayerRegistry");
        }

        public static void ToggleImmortalHuman() => Todo("F5 Immortal Human", "A1.4 HumanHealth");
        public static void ToggleInfiniteEnergy() => Todo("F6 Infinite Energy", "B JinnEnergy");
        public static void ToggleRevealAll() => Todo("F7 Reveal All", "B Visibility");
        public static void RestartRound() => Todo("F8 Restart Round (New Seed)", "A1.6 Match");
        public static void GiveAllFragments() => Todo("F11 Give All Fragments", "C Objectives");
        public static void ToggleDummyHuman() => Todo("F12 Toggle Dummy Human", "B BotInput");

        public static void ToggleOverlay()
        {
            DebugOverlay.Visible = !DebugOverlay.Visible;
            CMLog.Info("Debug", "Overlay " + (DebugOverlay.Visible ? "on" : "off"));
        }

        public static void ToggleTimeScale()
        {
            Time.timeScale = Time.timeScale > 1.5f ? 1f : 2f;
            CMLog.Info("Debug", "Time scale x" + Time.timeScale);
        }

        public static void DumpState()
        {
            CMLog.Info("Dump", DebugState.BuildJson());
        }

        private static void Todo(string what, string waitingFor)
        {
            CMLog.Info("Debug", what + ": TODO (" + waitingFor + ")");
        }
    }
}
