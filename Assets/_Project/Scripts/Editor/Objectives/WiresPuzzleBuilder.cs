using System.Collections.Generic;
using CinliMahzen.Objectives;
using UnityEditor;
using UnityEngine;

namespace CinliMahzen.Editor
{
    /// <summary>
    /// TODO 3.2: <c>CinliMahzen/Setup/Build Wires Puzzle Prefabs</c> — the graybox P3 "Tılsımlı Şalter" (wall slot; needs the fuse key, which the
    /// puzzle rules check). 6 village motifs: Snake, Eye, Key, Sickle, Hammer, Rooster (value 0..5, one colour each).
    /// <list type="bullet">
    /// <item><c>P_Puzzle_Wires</c>: a fuse box with 4 sockets; each socket is a part (Cycle) whose plug shows motif <c>value</c>
    /// (<see cref="PuzzlePartDisplay.Mode.ActivateChild"/>, 6 children). Lever on the right.</item>
    /// <item><c>P_PuzzleRef_Wires</c> (SpiritOnly): the spirit diagram — 6 pictures in a row: the 4 answer motifs in socket order at positions
    /// 0, 2, 3, 5 and the 2 trap motifs (crossed out with an X) at positions 1 and 4 (<c>trapDisplays</c>).</item>
    /// </list>
    /// </summary>
    public static class WiresPuzzleBuilder
    {
        public const string PuzzlePrefabPath = PuzzlePrefabKit.PrefabFolder + "/P_Puzzle_Wires.prefab";
        public const string ReferencePrefabPath = PuzzlePrefabKit.PrefabFolder + "/P_PuzzleRef_Wires.prefab";

        private const int Sockets = 4;
        private const float SocketSpacing = 0.3f;
        private const float DiagramSpacing = 0.28f;
        private static readonly int[] AnswerPositions = { 0, 2, 3, 5 };
        private static readonly int[] TrapPositions = { 1, 4 };

        private static readonly string[] MotifNames = { "Snake", "Eye", "Key", "Sickle", "Hammer", "Rooster" };
        private static readonly Color[] MotifColors =
        {
            new Color(0.2f, 0.65f, 0.25f), new Color(0.2f, 0.4f, 0.9f), new Color(0.95f, 0.8f, 0.2f),
            new Color(0.6f, 0.6f, 0.65f), new Color(0.5f, 0.32f, 0.15f), new Color(0.85f, 0.15f, 0.1f),
        };

        [MenuItem("CinliMahzen/Setup/Build Wires Puzzle Prefabs")]
        public static void Build()
        {
            if (!PuzzlePrefabKit.CheckNotPlaying("Build Wires Puzzle Prefabs"))
                return;

            Material box = PuzzlePrefabKit.Material("M_Puzzle_FuseBox", new Color(0.3f, 0.32f, 0.3f));
            Material marker = PuzzlePrefabKit.Material("M_Puzzle_Marker", new Color(0.05f, 0.05f, 0.05f));
            Material cross = PuzzlePrefabKit.Material("M_Puzzle_TrapCross", new Color(0.6f, 0f, 0f));
            var motifs = new Material[MotifNames.Length];
            for (int i = 0; i < MotifNames.Length; i++)
                motifs[i] = PuzzlePrefabKit.Material("M_Puzzle_Motif_" + MotifNames[i], MotifColors[i]);

            // ---- interactive ----
            var root = new GameObject("P_Puzzle_Wires");
            GameObject panel = PuzzlePrefabKit.Empty("Panel", root.transform, Vector3.zero);
            PuzzlePrefabKit.Cube("Box", panel.transform, Vector3.zero, new Vector3(1.4f, 0.6f, 0.12f), box);
            var parts = new PuzzlePart[Sockets];
            for (int s = 0; s < Sockets; s++)
            {
                float x = (s - (Sockets - 1) * 0.5f) * SocketSpacing;
                GameObject socket = PuzzlePrefabKit.Interactable("Socket_" + s, panel.transform, new Vector3(x, 0f, 0.08f), new Vector3(0.22f, 0.3f, 0.1f));
                PuzzlePrefabKit.Cube("Hole", socket.transform, new Vector3(0f, 0f, -0.01f), new Vector3(0.18f, 0.25f, 0.02f), marker);
                Transform plug = MotifSet(socket.transform, motifs, new Vector3(0f, 0f, 0.03f));
                parts[s] = PuzzlePrefabKit.Part(socket, PuzzlePrefabKit.Display(socket, plug, PuzzlePartDisplay.Mode.ActivateChild));
            }
            ConfirmLever lever = PuzzlePrefabKit.Lever(root.transform, new Vector3(0.95f, -0.15f, 0f), box, marker);
            PuzzlePrefabKit.View(root, parts, lever, panel.transform);

            // ---- reference ----
            var reference = new GameObject("P_PuzzleRef_Wires");
            GameObject content = PuzzlePrefabKit.Empty("Content", reference.transform, Vector3.zero);
            var answers = new List<PuzzlePartDisplay>(Sockets);
            for (int i = 0; i < AnswerPositions.Length; i++)
                answers.Add(DiagramPicture(content.transform, "Answer_" + i, AnswerPositions[i], motifs, null));
            var traps = new List<PuzzlePartDisplay>(TrapPositions.Length);
            for (int i = 0; i < TrapPositions.Length; i++)
                traps.Add(DiagramPicture(content.transform, "Trap_" + i, TrapPositions[i], motifs, cross));
            GameObject veil = PuzzlePrefabKit.Cube("Veil", reference.transform, new Vector3(0f, 0f, 0.05f),
                new Vector3(DiagramSpacing * 6.5f, 0.45f, 0.01f), PuzzlePrefabKit.Veil());
            PuzzleReference pr = PuzzlePrefabKit.Reference(reference, answers, content, veil);
            PuzzlePrefabKit.SetArray(pr, "trapDisplays", traps);
            PuzzlePrefabKit.MakeSpiritOnly(reference);

            PuzzlePrefabKit.SaveAndLink(root, PuzzlePrefabPath, reference, ReferencePrefabPath, "PZ_Wires");
            Debug.Log("[Setup] Build Wires Puzzle Prefabs: done");
        }

        /// <summary>A container whose child i is motif i (only one is shown at a time by ActivateChild).</summary>
        private static Transform MotifSet(Transform parent, Material[] motifs, Vector3 localPos)
        {
            GameObject set = PuzzlePrefabKit.Empty("Motifs", parent, localPos);
            for (int m = 0; m < motifs.Length; m++)
                PuzzlePrefabKit.Cube(MotifNames[m], set.transform, Vector3.zero, new Vector3(0.14f, 0.2f, 0.04f), motifs[m]);
            return set.transform;
        }

        private static PuzzlePartDisplay DiagramPicture(Transform parent, string name, int position, Material[] motifs, Material crossMat)
        {
            float x = (position - 2.5f) * DiagramSpacing;
            GameObject holder = PuzzlePrefabKit.Empty(name, parent, new Vector3(x, 0f, 0f));
            Transform set = MotifSet(holder.transform, motifs, Vector3.zero);
            if (crossMat != null)
            {
                // The X over a decoy motif.
                GameObject a = PuzzlePrefabKit.Cube("CrossA", holder.transform, new Vector3(0f, 0f, 0.03f), new Vector3(0.03f, 0.28f, 0.01f), crossMat);
                a.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                GameObject b = PuzzlePrefabKit.Cube("CrossB", holder.transform, new Vector3(0f, 0f, 0.03f), new Vector3(0.03f, 0.28f, 0.01f), crossMat);
                b.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);
            }
            return PuzzlePrefabKit.Display(holder, set, PuzzlePartDisplay.Mode.ActivateChild);
        }
    }
}
