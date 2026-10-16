using CinliMahzen.DebugTools;
using UnityEditor;

namespace CinliMahzen.Editor
{
    /// <summary>CinliMahzen/Debug/* menu commands (Teknik §12.2) — MCP execute_menu_item equivalents of F1-F12.</summary>
    public static class DebugMenuA
    {
        private const string Root = "CinliMahzen/Debug/";

        [MenuItem(Root + "Dump State To Console")]
        public static void DumpState() => DebugCommands.DumpState();

        [MenuItem(Root + "Toggle Debug Overlay (F9)")]
        public static void ToggleOverlay() => DebugCommands.ToggleOverlay();

        [MenuItem(Root + "Toggle Time Scale x2 (F10)")]
        public static void ToggleTimeScale() => DebugCommands.ToggleTimeScale();

        [MenuItem(Root + "Switch To Player 1")] public static void P1() => DebugCommands.SwitchToPlayer(0);
        [MenuItem(Root + "Switch To Player 2")] public static void P2() => DebugCommands.SwitchToPlayer(1);
        [MenuItem(Root + "Switch To Player 3")] public static void P3() => DebugCommands.SwitchToPlayer(2);
        [MenuItem(Root + "Switch To Player 4")] public static void P4() => DebugCommands.SwitchToPlayer(3);

        [MenuItem(Root + "Restart Round (New Seed)")]
        public static void RestartRound() => DebugCommands.RestartRound();

        [MenuItem(Root + "Complete Current Stage")]
        public static void CompleteCurrentStage() => DebugCommands.CompleteCurrentStage();

        [MenuItem(Root + "Spin Human 180")]
        public static void SpinHuman180() => DebugCommands.SpinHuman180();

        [MenuItem(Root + "Dump State To Console", true)]
        [MenuItem(Root + "Toggle Debug Overlay (F9)", true)]
        [MenuItem(Root + "Toggle Time Scale x2 (F10)", true)]
        [MenuItem(Root + "Switch To Player 1", true)]
        [MenuItem(Root + "Switch To Player 2", true)]
        [MenuItem(Root + "Switch To Player 3", true)]
        [MenuItem(Root + "Switch To Player 4", true)]
        [MenuItem(Root + "Restart Round (New Seed)", true)]
        [MenuItem(Root + "Complete Current Stage", true)]
        [MenuItem(Root + "Spin Human 180", true)]
        private static bool PlayingOnly() => EditorApplication.isPlaying;
    }
}
