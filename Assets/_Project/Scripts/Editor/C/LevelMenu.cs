using CinliMahzen.Core;
using CinliMahzen.World;
using UnityEditor;
using UnityEngine;

namespace CinliMahzen.Editor
{
    /// <summary>Owner: C. CinliMahzen/Level/* menu commands (MCP: execute_menu_item).</summary>
    public static class LevelMenu
    {
        private const string RootName = "LevelRoot";

        [MenuItem("CinliMahzen/Level/Generate Map (Random Seed)")]
        public static void GenerateRandom() => Generate(System.Environment.TickCount & 0x7FFFFFFF);

        [MenuItem("CinliMahzen/Level/Generate Map (Seed=12345)")]
        public static void GenerateFixed() => Generate(12345);

        [MenuItem("CinliMahzen/Level/Validate Current Level")]
        public static void ValidateCurrent()
        {
            var go = GameObject.Find(RootName);
            var holder = go != null ? go.GetComponent<LevelLayoutHolder>() : null;
            if (holder == null) { CMLog.Warn("Level", "No generated level in the scene."); return; }
            LevelValidator.ValidateAndLog(holder.Layout, new LevelGenSettings());
            CMLog.Info("Level", "LevelHash = " + LevelHash.Compute(go.transform).ToString("x16"));
        }

        [MenuItem("CinliMahzen/Level/Clear Level")]
        public static void Clear()
        {
            var go = GameObject.Find(RootName);
            if (go != null) Object.DestroyImmediate(go);
        }

        private static void Generate(int seed)
        {
            Clear();
            var root = new GameObject(RootName).transform;
            var settings = new LevelGenSettings();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var layout = new ProceduralLevelGenerator().Generate(seed, settings, root);
            sw.Stop();
            bool ok = LevelValidator.ValidateAndLog(layout, settings);
            CMLog.Info("Level", "Generated seed " + layout.Seed + " in " + sw.ElapsedMilliseconds + " ms, valid=" + ok + ", hash=" + LevelHash.Compute(root).ToString("x16"));
        }
    }
}
