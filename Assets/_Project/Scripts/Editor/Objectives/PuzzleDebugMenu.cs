using System;
using CinliMahzen.Core;
using CinliMahzen.Core.Net;
using CinliMahzen.Objectives;
using UnityEditor;
using UnityEngine;

namespace CinliMahzen.Editor
{
    /// <summary>
    /// CinliMahzen/Debug/Puzzles/* (TODO 2.4, Play mode only) - MCP <c>execute_menu_item</c> tools for the PuzzleSystem before the real
    /// puzzles exist (2.5-2.7). One test puzzle in slot 0 (Paintings 3x4, starts solved), placed 1 m in front of the human; the other
    /// items act on it AS a player of the right role, straight on the authority entry points of <see cref="PuzzleSystemCore"/>
    /// (the same code a ReqPuzzle* message reaches). They skip only the range rule (DebugIgnoreRange) - role, darkness, lantern, cooldown,
    /// energy, lock and match-wake rules all apply, and a refusal is logged with its reason. "puzzles" in Dump State shows the same data.
    /// </summary>
    public static class PuzzleDebugMenu
    {
        private const string Root = "CinliMahzen/Debug/Puzzles/";
        private const string Category = "PuzzleDebug";
        private const int TestSlot = 0;
        private const int TestStage = 1;
        private const float PlacementDistance = 1f;

        private static readonly int[] PaintingsValueCounts = { 4, 4, 4 };
        private static readonly int[] PaintingsSolution = { 1, 3, 0 };

        [MenuItem(Root + "Register Test Puzzle (Paintings 3x4)")]
        public static void RegisterTestPuzzle()
        {
            PuzzleSystem system = FindSystem();
            if (system == null)
            {
                system = new GameObject("PuzzleSystem").AddComponent<PuzzleSystem>();
                CMLog.Info(Category, "No PuzzleSystem in the scene - created a temporary one (not saved)");
            }
            if (system.Core == null)
            {
                CMLog.Warn(Category, "PuzzleSystem is inert (GameServices missing) - nothing registered");
                return;
            }

            Vector3 position = FindPlacement();
            var state = new PuzzleState(PaintingsValueCounts, PaintingsSolution, PuzzleOpKind.Cycle);
            int room = GameServices.Get<IRoomService>().RoomOf(position);
            system.RegisterSlot(TestSlot, new PuzzleSlotRuntime(state, string.Empty, position, room, TestStage));
            CMLog.Info(Category, "Test puzzle in slot " + TestSlot + " at " + position + " (room " + room + "), solved. " + system.Core.BuildDebugJson());
        }

        [MenuItem(Root + "Scramble Test Puzzle As Evil")]
        public static void ScrambleAsEvil()
        {
            Act("Scramble", Role.EvilJinn, (core, who) => core.ProcessScrambleAuthority(who, TestSlot));
        }

        [MenuItem(Root + "Cycle Part 0 As Human")]
        public static void CyclePartZeroAsHuman()
        {
            Act("Cycle part 0", Role.Human, (core, who) => core.ProcessOpAuthority(who, TestSlot, PuzzleOp.Cycle(0, 1)));
        }

        [MenuItem(Root + "Confirm Test Puzzle")]
        public static void ConfirmAsHuman()
        {
            Act("Confirm", Role.Human, (core, who) => core.ProcessConfirmAuthority(who, TestSlot));
        }

        [MenuItem(Root + "Dump Puzzles")]
        public static void DumpPuzzles()
        {
            PuzzleSystem system = FindSystem();
            if (system == null || system.Core == null)
            {
                CMLog.Warn(Category, "No working PuzzleSystem in the scene (run Register Test Puzzle first)");
                return;
            }
            CMLog.Info(Category, "puzzles " + system.Core.BuildDebugJson());
        }

        [MenuItem(Root + "Register Test Puzzle (Paintings 3x4)", true)]
        [MenuItem(Root + "Scramble Test Puzzle As Evil", true)]
        [MenuItem(Root + "Cycle Part 0 As Human", true)]
        [MenuItem(Root + "Confirm Test Puzzle", true)]
        [MenuItem(Root + "Dump Puzzles", true)]
        private static bool PlayingOnly()
        {
            return EditorApplication.isPlaying;
        }

        private static void Act(string what, Role role, Func<PuzzleSystemCore, PlayerId, PuzzleDenyReason> action)
        {
            PuzzleSystem system = FindSystem();
            if (system == null || system.Core == null)
            {
                CMLog.Warn(Category, what + ": no working PuzzleSystem in the scene (run Register Test Puzzle first)");
                return;
            }

            PuzzleSystemCore core = system.Core;
            INetBridge net = GameServices.Net;
            if (net == null || !net.IsAuthority)
            {
                CMLog.Warn(Category, what + ": this machine is not the authority");
                return;
            }
            if (!core.TryGetSlot(TestSlot, out PuzzleSlotRuntime _))
            {
                CMLog.Warn(Category, what + ": slot " + TestSlot + " is empty (run Register Test Puzzle first)");
                return;
            }
            if (!TryFindPlayer(role, out PlayerId who))
            {
                CMLog.Warn(Category, what + ": no player has the role " + role + " (roles are assigned when the round starts)");
                return;
            }

            bool before = core.DebugIgnoreRange;
            core.DebugIgnoreRange = true;
            PuzzleDenyReason reason;
            try
            {
                reason = action(core, who);
            }
            finally
            {
                core.DebugIgnoreRange = before;
            }
            CMLog.Info(Category, what + " as " + who + " (" + role + ") -> " + reason + ". " + core.BuildDebugJson());
        }

        private static PuzzleSystem FindSystem()
        {
            return UnityEngine.Object.FindFirstObjectByType<PuzzleSystem>();
        }

        /// <summary>The first player holding <paramref name="role"/> this round.</summary>
        private static bool TryFindPlayer(Role role, out PlayerId player)
        {
            player = PlayerId.None;
            IPlayerRegistry players = GameServices.Players;
            if (players == null)
            {
                return false;
            }
            for (int i = 0; i < players.Players.Count; i++)
            {
                if (players.Players[i].Role == role)
                {
                    player = players.Players[i].Id;
                    return true;
                }
            }
            return false;
        }

        /// <summary>1 m in front of the human's pawn; the human status bridge's position when no human pawn exists.</summary>
        private static Vector3 FindPlacement()
        {
            PawnBase[] pawns = UnityEngine.Object.FindObjectsByType<PawnBase>(FindObjectsSortMode.None);
            for (int i = 0; i < pawns.Length; i++)
            {
                if (pawns[i].Role == Role.Human)
                {
                    return pawns[i].transform.position + pawns[i].transform.forward * PlacementDistance;
                }
            }
            CMLog.Warn(Category, "No human pawn in the scene - using the IHumanStatusQuery position");
            return GameServices.Get<IHumanStatusQuery>().Position;
        }
    }
}
