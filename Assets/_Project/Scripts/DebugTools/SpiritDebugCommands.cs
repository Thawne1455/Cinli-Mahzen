using CinliMahzen.Core;
using CinliMahzen.Jinn;

namespace CinliMahzen.DebugTools
{
    /// <summary>
    /// Debug actions of the spirit form (TODO 1.6) behind the <c>CinliMahzen/Debug</c> menu entries (MCP <c>execute_menu_item</c>).
    /// Kept in its own class, next to <see cref="DebugCommands"/>, so the spirit work does not touch the shared command list.
    /// All three act on the locally played player's spirit pawn (hotseat: pick one with Switch To Player N first).
    /// </summary>
    public static class SpiritDebugCommands
    {
        private const string Category = "Debug";

        /// <summary>
        /// Menu "Toggle Focus (local good jinn)": the local good jinn asks the authority to start / stop focusing a reference, without
        /// holding the right mouse button. The flag outlives a switch to another player, so the evil jinn can then fly into it.
        /// </summary>
        public static void ToggleFocusLocalGoodJinn()
        {
            const string command = "Toggle Focus (local good jinn)";
            if (!TryGetLocalSpirit(command, out SpiritController local))
                return;
            if (local.Role != Role.GoodJinn)
            {
                CMLog.Warn(Category, command + ": the local player is " + local.Role + ", not the good jinn (Switch To Player 2)");
                return;
            }

            bool wanted = !local.IsFocusingReference;
            local.RequestFocusReference(wanted);
            CMLog.Info(Category, command + ": requested focus " + (wanted ? "on" : "off") + " for " + local.Owner
                + " - focusing now: " + local.IsFocusingReference);
        }

        /// <summary>
        /// Menu "Teleport Local Spirit To Good Jinn": moves the local evil jinn next to the good jinn (SpiritController's
        /// debugTeleportDistance), facing it.
        /// </summary>
        public static void TeleportLocalSpiritToGoodJinn()
        {
            const string command = "Teleport Local Spirit To Good Jinn";
            if (!TryGetLocalSpirit(command, out SpiritController local))
                return;
            if (!SpiritController.TryFindGoodJinn(local, out SpiritController good))
            {
                CMLog.Warn(Category, command + ": there is no good jinn pawn to go to (the local player is " + local.Role + ")");
                return;
            }
            if (!local.DebugTeleportBeside(good))
            {
                CMLog.Warn(Category, command + ": could not teleport");
                return;
            }
            CMLog.Info(Category, command + ": " + local.Owner + " (" + local.Role + ") is now beside " + good.Owner + " at "
                + local.transform.position.ToString("0.00"));
        }

        /// <summary>
        /// Menu "Request Spirit Bump": the local evil jinn sends ReqSpiritBump for the good jinn without holding Shift. The authority still
        /// checks roles, the focus flag, the range (SpiritBumpRange + NetRangeTolerance) and the cooldown; its verdict is in the [Spirit] log.
        /// </summary>
        public static void RequestSpiritBump()
        {
            const string command = "Request Spirit Bump";
            if (!TryGetLocalSpirit(command, out SpiritController local))
                return;
            if (local.Role != Role.EvilJinn)
            {
                CMLog.Warn(Category, command + ": the local player is " + local.Role + ", not an evil jinn (Switch To Player 3)");
                return;
            }
            if (!local.DebugRequestSpiritBump())
            {
                CMLog.Warn(Category, command + ": there is no good jinn pawn in the scene");
                return;
            }
            CMLog.Info(Category, command + ": request sent by " + local.Owner + " - see the [Spirit] lines and Dump State (spirits)");
        }

        private static bool TryGetLocalSpirit(string command, out SpiritController local)
        {
            if (SpiritController.TryFindLocal(out local))
                return true;
            CMLog.Warn(Category, command + ": the local player has no spirit pawn (switch to a jinn: Switch To Player 2 / 3 / 4)");
            return false;
        }
    }
}
