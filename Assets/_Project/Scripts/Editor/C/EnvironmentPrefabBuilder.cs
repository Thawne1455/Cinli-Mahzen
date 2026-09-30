using System.Collections.Generic;
using System.IO;
using CinliMahzen.Core;
using CinliMahzen.World;
using UnityEditor;
using UnityEngine;

namespace CinliMahzen.Editor
{
    /// <summary>Owner: C. C0.2: wraps KayKit fbx files into collider'd prefabs (Environment layer) and builds the LevelPrefabSet asset.</summary>
    public static class EnvironmentPrefabBuilder
    {
        private const string Models = "Assets/_Project/Art/KayKit/Models/";
        private const string EnvDir = "Assets/_Project/Prefabs/Environment/";
        private const string PropDir = "Assets/_Project/Prefabs/Props/";
        private const string SetPath = "Assets/_Project/Resources/Level/LevelPrefabSet.asset";

        private static readonly string[][] Env =
        {
            new[] { "Env_Wall", "wall" }, new[] { "Env_WallCracked", "wall_cracked" },
            new[] { "Env_WallDoorway", "wall_doorway" }, new[] { "Env_WallDoorwayDoor", "wall_doorway" }, new[] { "Env_WallGated", "wall_gated" },
            new[] { "Env_WallHalf", "wall_half" }, new[] { "Env_WallCorner", "wall_corner" },
            new[] { "Env_Floor", "floor_tile_large" }, new[] { "Env_FloorRocks", "floor_tile_large_rocks" },
            new[] { "Env_FloorWood", "floor_wood_large" },
            new[] { "Env_Pillar", "pillar" }, new[] { "Env_Column", "column" },
            new[] { "Env_Stairs", "stairs" }, new[] { "Env_StairsWide", "stairs_wide" },
        };

        private static readonly string[] Props =
        {
            "shelf_large", "shelf_small", "barrel_large", "barrel_large_decorated", "barrel_small", "chair", "stool",
            "chest", "trunk_large_A", "trunk_medium_A", "trunk_small_A", "sword_shield", "sword_shield_broken", "torch_mounted", "torch_lit",
            "candle_triple", "candle_lit", "bottle_A_brown", "bottle_A_green", "bottle_B_brown", "keg", "keg_decorated",
            "table_medium", "table_long", "table_small", "plate_stack", "chest_gold", "coin_stack_large", "coin_stack_medium",
            "coin_stack_small", "key", "crates_stacked", "box_stacked", "rubble_large",
        };

        [MenuItem("CinliMahzen/Setup C/2 Build Environment + Prop Prefabs")]
        public static void Run()
        {
            Directory.CreateDirectory(EnvDir);
            Directory.CreateDirectory(PropDir);
            Directory.CreateDirectory(Path.GetDirectoryName(SetPath));
            int layer = LayerMask.NameToLayer("Environment");
            var made = new Dictionary<string, GameObject>();
            foreach (var e in Env) made[e[0]] = Build(EnvDir + e[0] + ".prefab", e[1], layer, false);
            foreach (var p in Props) made["Prop_" + p] = Build(PropDir + "Prop_" + p + ".prefab", p, layer, true);

            var set = AssetDatabase.LoadAssetAtPath<LevelPrefabSet>(SetPath);
            if (set == null) { set = ScriptableObject.CreateInstance<LevelPrefabSet>(); AssetDatabase.CreateAsset(set, SetPath); }
            var so = new SerializedObject(set);
            SetArray(so.FindProperty("floors"), made["Env_Floor"], made["Env_FloorRocks"]);
            SetArray(so.FindProperty("walls"), made["Env_Wall"], made["Env_WallCracked"]);
            so.FindProperty("wallDoorway").objectReferenceValue = made["Env_WallDoorway"];
            so.FindProperty("wallGated").objectReferenceValue = made["Env_WallGated"];
            so.FindProperty("pillar").objectReferenceValue = made["Env_Pillar"];
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
            CMLog.Info("KayKit", "Built " + made.Count + " prefabs + LevelPrefabSet (complete=" + set.IsComplete + ")");
        }

        private static void SetArray(SerializedProperty p, params GameObject[] items)
        {
            p.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }

        private static GameObject Build(string prefabPath, string model, int layer, bool prop)
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(Models + model + ".fbx");
            if (fbx == null) { CMLog.Error("KayKit", "Missing model " + model); return null; }
            var root = new GameObject(Path.GetFileNameWithoutExtension(prefabPath));
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(fbx, root.transform);
            inst.name = "Model";
            if (Path.GetFileNameWithoutExtension(prefabPath) == "Env_WallDoorway")
            {
                // Open passage: the fbx ships a closed door mesh as a child; hide it (Env_WallDoorwayDoor keeps it).
                var door = inst.transform.Find("wall_doorway_door");
                if (door != null) door.gameObject.SetActive(false);
            }
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
            if (prop)
            {
                var b = new Bounds();
                bool first = true;
                foreach (var r in inst.GetComponentsInChildren<Renderer>())
                {
                    if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds);
                }
                var box = root.AddComponent<BoxCollider>();
                box.center = b.center;
                box.size = b.size;
            }
            else
            {
                foreach (var mf in inst.GetComponentsInChildren<MeshFilter>())
                    mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
            }
            var saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return saved;
        }
    }
}
