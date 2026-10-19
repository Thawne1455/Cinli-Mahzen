using CinliMahzen.DebugTools;
using UnityEditor;

namespace CinliMahzen.Editor
{
    /// <summary>
    /// CinliMahzen/Debug/* menu entries of the spirit form (TODO 1.6) — MCP <c>execute_menu_item</c> equivalents of holding the focus button
    /// and flying into the good jinn. Play mode only. The logic is in <see cref="SpiritDebugCommands"/>.
    /// </summary>
    public static class SpiritDebugMenu
    {
        private const string Root = "CinliMahzen/Debug/";

        [MenuItem(Root + "Toggle Focus (local good jinn)")]
        public static void ToggleFocus() => SpiritDebugCommands.ToggleFocusLocalGoodJinn();

        [MenuItem(Root + "Teleport Local Spirit To Good Jinn")]
        public static void TeleportToGoodJinn() => SpiritDebugCommands.TeleportLocalSpiritToGoodJinn();

        [MenuItem(Root + "Request Spirit Bump")]
        public static void RequestSpiritBump() => SpiritDebugCommands.RequestSpiritBump();

        [MenuItem(Root + "Toggle Focus (local good jinn)", true)]
        [MenuItem(Root + "Teleport Local Spirit To Good Jinn", true)]
        [MenuItem(Root + "Request Spirit Bump", true)]
        private static bool PlayingOnly() => EditorApplication.isPlaying;
    }
}
