using UnityEditor;
using UnityEngine;

namespace CinliMahzen.Editor
{
    /// <summary>
    /// TODO 3.1–3.3: <c>CinliMahzen/Setup/Build All Puzzle Prefabs</c> — the four MVP puzzles (Portraits, Statue, Wires, Sigil) in one go. Run
    /// <c>Create Puzzle Definitions</c> first so each pair is linked into its PZ_* asset.
    /// </summary>
    public static class AllPuzzlePrefabsBuilder
    {
        [MenuItem("CinliMahzen/Setup/Build All Puzzle Prefabs")]
        public static void BuildAll()
        {
            PaintingsPuzzleBuilder.Build();
            StatuePuzzleBuilder.Build();
            WiresPuzzleBuilder.Build();
            SigilPuzzleBuilder.Build();
            Debug.Log("[Setup] Build All Puzzle Prefabs: done");
        }
    }
}
