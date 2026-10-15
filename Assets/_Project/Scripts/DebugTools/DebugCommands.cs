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
            Todo("F" + (index + 1) + " Switch To Player " + (index + 1), "1.3 Hotseat");
        }

        public static void ToggleImmortalHuman() => Todo("F5 Immortal Human", "1.5 HumanHealth");
        public static void ToggleInfiniteEnergy() => Todo("F6 Infinite Energy", "1.7 Possession");
        public static void ToggleRevealAll() => Todo("F7 Reveal All", "3.9 Visibility");
        public static void RestartRound() => Todo("F8 Restart Round (New Seed)", "1.10 Match");
        public static void CompleteCurrentStage() => Todo("F11 Complete Current Stage (+StageTimeBonus)", "2.7 StageGate");
        public static void ToggleDummyHuman() => Todo("F12 Toggle Dummy Human", "3.12 Dummy bot");

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
