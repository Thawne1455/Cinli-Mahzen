using System.Collections.Generic;
using CinliMahzen.Core;

namespace CinliMahzen.World
{
    /// <summary>
    /// The four MVP puzzle definitions (Oyun 4.3, Teknik 7.1) as plain data: the single source of the PZ_*.asset files
    /// (CinliMahzen/Setup/Create Puzzle Definitions) and of the in-memory pool the planner tests use.
    ///
    /// Sizes follow the graybox slots (GrayboxMarkers: Wall slots allow Paintings/Wires/Sigil, Floor slots Statue, Table slots
    /// Statue/Sigil): Paintings and Wires hang on a wall, the statue stands on the floor, the seal stones lie on a table. With
    /// Sigil = Table every graybox PuzzleSlot and ReferenceSlot can be used (with Sigil = Wall the table slots would stay empty).
    /// </summary>
    public static class MvpPuzzleCatalog
    {
        /// <summary>ItemDefinition.Id the Wires box needs (ID_FuseKey).</summary>
        public const string FuseKeyItemId = "fuse_key";

        /// <summary>ItemDefinition.Id the seal stones need (ID_Paint).</summary>
        public const string PaintItemId = "paint";

        // P1 Portraits: 3 portraits x 4 rotations of 90 degrees.
        public const int PaintingsPortraits = 3;
        public const int PaintingsAngles = 4;

        // P2 The Statue's Charm: one statue, 6 surrounding objects to face + 3 staff tilts.
        public const int StatueObjects = 6;
        public const int StatueStaffTilts = 3;

        // P3 Charmed Switch: 4 sockets x 6 village motifs.
        public const int WiresSockets = 4;
        public const int WiresMotifs = 6;

        // P4 Seal Stones: 12 lines of a 3x3 dot grid, painted or empty.
        public const int SigilLines = 12;
        public const int SigilLineStates = 2;

        private static readonly PuzzleDefinitionSpec[] All =
        {
            new PuzzleDefinitionSpec("PZ_Paintings", PuzzleType.Paintings, "puzzle.paintings",
                Repeat(PaintingsAngles, PaintingsPortraits), PuzzleOpKind.Cycle, string.Empty, PuzzleSlotSize.Wall),
            new PuzzleDefinitionSpec("PZ_Statue", PuzzleType.Statue, "puzzle.statue",
                new[] { StatueObjects, StatueStaffTilts }, PuzzleOpKind.Cycle, string.Empty, PuzzleSlotSize.Floor),
            new PuzzleDefinitionSpec("PZ_Wires", PuzzleType.Wires, "puzzle.wires",
                Repeat(WiresMotifs, WiresSockets), PuzzleOpKind.Cycle, FuseKeyItemId, PuzzleSlotSize.Wall),
            new PuzzleDefinitionSpec("PZ_Sigil", PuzzleType.Sigil, "puzzle.sigil",
                Repeat(SigilLineStates, SigilLines), PuzzleOpKind.Set, PaintItemId, PuzzleSlotSize.Table),
        };

        private static readonly IReadOnlyList<PuzzleDefinitionSpec> ReadOnlyAll = System.Array.AsReadOnly(All);

        /// <summary>Paintings, Statue, Wires, Sigil (ascending type).</summary>
        public static IReadOnlyList<PuzzleDefinitionSpec> Specs => ReadOnlyAll;

        /// <summary>The spec of the type, or null.</summary>
        public static PuzzleDefinitionSpec Find(PuzzleType type)
        {
            for (int i = 0; i < All.Length; i++)
            {
                if (All[i].Type == type)
                {
                    return All[i];
                }
            }
            return null;
        }

        private static int[] Repeat(int value, int count)
        {
            var array = new int[count];
            for (int i = 0; i < count; i++)
            {
                array[i] = value;
            }
            return array;
        }
    }
}
