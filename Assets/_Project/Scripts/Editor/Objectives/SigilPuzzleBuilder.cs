using System.Collections.Generic;
using CinliMahzen.Objectives;
using UnityEditor;
using UnityEngine;

namespace CinliMahzen.Editor
{
    /// <summary>
    /// TODO 3.3: <c>CinliMahzen/Setup/Build Sigil Puzzle Prefabs</c> — the graybox P4 "Mühür Taşları" (table slot, drawn flat; needs paint, which
    /// the puzzle rules check). A 3×3 dot grid with the four corner symbols Moon, Sun, Star, Bone; 12 lines between neighbouring dots.
    /// Part order: the 6 horizontal lines row by row (2 per row, west to east), then the 6 vertical lines column by column (2 per column, south
    /// to north). Value 0 = empty, 1 = painted.
    /// <list type="bullet">
    /// <item><c>P_Puzzle_Sigil</c>: every line is a part with <see cref="PuzzlePart.PartOp.Toggle"/> (one press paints / wipes it) and an
    /// <see cref="PuzzlePartDisplay.Mode.ActivateChild"/> display (thin grey / thick red). Lever standing at the east edge.</item>
    /// <item><c>P_PuzzleRef_Sigil</c> (SpiritOnly): the finished sigil with the same corner symbols under a rotation root whose local Z is world
    /// up, so the planner's <c>ReferenceRotation</c> turns the whole drawing (the corner symbols tell how it is turned).</item>
    /// </list>
    /// </summary>
    public static class SigilPuzzleBuilder
    {
        public const string PuzzlePrefabPath = PuzzlePrefabKit.PrefabFolder + "/P_Puzzle_Sigil.prefab";
        public const string ReferencePrefabPath = PuzzlePrefabKit.PrefabFolder + "/P_PuzzleRef_Sigil.prefab";

        private const int Dots = 3;
        private const float Spacing = 0.25f;
        private const float Height = 0.02f;

        private static readonly string[] CornerNames = { "Moon", "Sun", "Star", "Bone" };
        private static readonly Color[] CornerColors =
        {
            new Color(0.85f, 0.9f, 1f), new Color(1f, 0.55f, 0.1f), new Color(1f, 0.9f, 0.2f), new Color(0.9f, 0.85f, 0.75f),
        };

        [MenuItem("CinliMahzen/Setup/Build Sigil Puzzle Prefabs")]
        public static void Build()
        {
            if (!PuzzlePrefabKit.CheckNotPlaying("Build Sigil Puzzle Prefabs"))
                return;

            Material slab = PuzzlePrefabKit.Material("M_Puzzle_Stone", new Color(0.55f, 0.55f, 0.5f));
            Material dot = PuzzlePrefabKit.Material("M_Puzzle_Marker", new Color(0.05f, 0.05f, 0.05f));
            Material empty = PuzzlePrefabKit.Material("M_Puzzle_LineEmpty", new Color(0.4f, 0.4f, 0.4f));
            Material paint = PuzzlePrefabKit.Material("M_Puzzle_Paint", new Color(0.75f, 0.05f, 0.05f));
            Material wood = PuzzlePrefabKit.Material("M_Puzzle_Frame", new Color(0.25f, 0.15f, 0.08f));
            var corners = new Material[CornerNames.Length];
            for (int i = 0; i < CornerNames.Length; i++)
                corners[i] = PuzzlePrefabKit.Material("M_Puzzle_Corner_" + CornerNames[i], CornerColors[i]);

            // ---- interactive ----
            var root = new GameObject("P_Puzzle_Sigil");
            GameObject board = PuzzlePrefabKit.Empty("Board", root.transform, Vector3.zero);
            PuzzlePrefabKit.Cube("Slab", board.transform, new Vector3(0f, -0.01f, 0f), new Vector3(0.8f, 0.02f, 0.8f), slab);
            AddDotsAndCorners(board.transform, dot, corners);
            var parts = new List<PuzzlePart>(12);
            foreach (LineSpec line in Lines())
            {
                GameObject go = PuzzlePrefabKit.Interactable(line.Name, board.transform, line.Center, line.Horizontal
                    ? new Vector3(Spacing * 0.8f, 0.06f, 0.07f) : new Vector3(0.07f, 0.06f, Spacing * 0.8f));
                GameObject states = PuzzlePrefabKit.Empty("States", go.transform, Vector3.zero);
                PuzzlePrefabKit.Cube("Empty", states.transform, Vector3.zero, LineSize(line.Horizontal, 0.012f), empty);
                PuzzlePrefabKit.Cube("Painted", states.transform, Vector3.zero, LineSize(line.Horizontal, 0.035f), paint);
                PuzzlePartDisplay display = PuzzlePrefabKit.Display(go, states.transform, PuzzlePartDisplay.Mode.ActivateChild);
                parts.Add(PuzzlePrefabKit.Part(go, display, PuzzlePart.PartOp.Toggle, "interact.puzzle_paint"));
            }
            ConfirmLever lever = PuzzlePrefabKit.Lever(root.transform, new Vector3(0.5f, 0f, 0f), wood, dot);
            PuzzlePrefabKit.View(root, parts, lever, board.transform);

            // ---- reference ----
            var reference = new GameObject("P_PuzzleRef_Sigil");
            GameObject content = PuzzlePrefabKit.Empty("Content", reference.transform, Vector3.zero);
            GameObject rotationRoot = PuzzlePrefabKit.Empty("RotationRoot", content.transform, Vector3.zero);
            rotationRoot.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // local Z = world up
            GameObject flat = PuzzlePrefabKit.Empty("Flat", rotationRoot.transform, Vector3.zero);
            flat.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);           // back to the board's XZ plane
            AddDotsAndCorners(flat.transform, dot, corners);
            var displays = new List<PuzzlePartDisplay>(12);
            foreach (LineSpec line in Lines())
            {
                GameObject holder = PuzzlePrefabKit.Empty(line.Name, flat.transform, line.Center);
                GameObject states = PuzzlePrefabKit.Empty("States", holder.transform, Vector3.zero);
                PuzzlePrefabKit.Cube("Empty", states.transform, Vector3.zero, LineSize(line.Horizontal, 0.004f), empty);
                PuzzlePrefabKit.Cube("Painted", states.transform, Vector3.zero, LineSize(line.Horizontal, 0.035f), paint);
                displays.Add(PuzzlePrefabKit.Display(holder, states.transform, PuzzlePartDisplay.Mode.ActivateChild));
            }
            GameObject veil = PuzzlePrefabKit.Cube("Veil", reference.transform, new Vector3(0f, 0.05f, 0f), new Vector3(0.9f, 0.1f, 0.9f), PuzzlePrefabKit.Veil());
            PuzzlePrefabKit.Reference(reference, displays, content, veil, rotationRoot.transform);
            PuzzlePrefabKit.MakeSpiritOnly(reference);

            PuzzlePrefabKit.SaveAndLink(root, PuzzlePrefabPath, reference, ReferencePrefabPath, "PZ_Sigil");
            Debug.Log("[Setup] Build Sigil Puzzle Prefabs: done");
        }

        private readonly struct LineSpec
        {
            public readonly string Name;
            public readonly Vector3 Center;
            public readonly bool Horizontal;

            public LineSpec(string name, Vector3 center, bool horizontal)
            {
                Name = name;
                Center = center;
                Horizontal = horizontal;
            }
        }

        /// <summary>The 12 lines in part order (horizontal row-major, then vertical column-major).</summary>
        private static IEnumerable<LineSpec> Lines()
        {
            for (int row = 0; row < Dots; row++)
            {
                for (int col = 0; col < Dots - 1; col++)
                    yield return new LineSpec("H_r" + row + "_c" + col, DotPosition(col + 0.5f, row), true);
            }
            for (int col = 0; col < Dots; col++)
            {
                for (int row = 0; row < Dots - 1; row++)
                    yield return new LineSpec("V_c" + col + "_r" + row, DotPosition(col, row + 0.5f), false);
            }
        }

        private static Vector3 DotPosition(float col, float row)
        {
            return new Vector3((col - 1f) * Spacing, Height, (row - 1f) * Spacing);
        }

        private static Vector3 LineSize(bool horizontal, float thickness)
        {
            return horizontal ? new Vector3(Spacing * 0.8f, 0.01f, thickness) : new Vector3(thickness, 0.01f, Spacing * 0.8f);
        }

        /// <summary>The 9 dots and, just outside the grid, the corner symbols: Moon south-west, Sun south-east, Star north-east, Bone north-west.</summary>
        private static void AddDotsAndCorners(Transform parent, Material dot, Material[] corners)
        {
            for (int row = 0; row < Dots; row++)
            {
                for (int col = 0; col < Dots; col++)
                    PuzzlePrefabKit.Cube("Dot_" + col + "_" + row, parent, DotPosition(col, row), new Vector3(0.04f, 0.02f, 0.04f), dot);
            }
            Vector2[] at = { new Vector2(-1.4f, -1.4f), new Vector2(1.4f, -1.4f), new Vector2(1.4f, 1.4f), new Vector2(-1.4f, 1.4f) };
            for (int i = 0; i < CornerNames.Length; i++)
            {
                PuzzlePrefabKit.Cube("Corner_" + CornerNames[i], parent, new Vector3(at[i].x * Spacing, Height, at[i].y * Spacing),
                    new Vector3(0.07f, 0.03f, 0.07f), corners[i]);
            }
        }
    }
}
