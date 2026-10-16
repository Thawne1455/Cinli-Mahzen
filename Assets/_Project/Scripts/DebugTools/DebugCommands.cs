using CinliMahzen.Core;
using CinliMahzen.Core.Net;
using CinliMahzen.Player;
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

        /// <summary>
        /// Menu "Spin Human 180": the authority applies CameraSpin180 to the scene's human. The duration is the human's
        /// serialized debugSpinDuration (PD_Slipper's status duration), so no number lives here.
        /// </summary>
        public static void SpinHuman180()
        {
            INetBridge net = GameServices.Net;
            if (net == null || !net.IsAuthority)
            {
                CMLog.Warn("Debug", "Spin Human 180: this machine is not the authority");
                return;
            }
            HumanController human = Object.FindFirstObjectByType<HumanController>();
            if (human == null)
            {
                CMLog.Warn("Debug", "Spin Human 180: no HumanController in the scene");
                return;
            }
            human.ApplyStatusAuthority(StatusType.CameraSpin180, human.DebugSpinDuration, PlayerId.None);
        }

        private static void Todo(string what, string waitingFor)
        {
            CMLog.Info("Debug", what + ": TODO (" + waitingFor + ")");
        }
    }
}
