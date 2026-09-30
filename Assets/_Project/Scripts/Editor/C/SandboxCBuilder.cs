using CinliMahzen.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CinliMahzen.Editor
{
    /// <summary>Owner: C. Builds Sandbox_C.unity: a hand-assembled 3x3-cell test room from the KayKit prefabs (C0.2 acceptance).</summary>
    public static class SandboxCBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Sandbox_C.unity";
        private const float Cell = 4f;

        [MenuItem("CinliMahzen/Setup C/3 Build Sandbox_C Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var root = new GameObject("HandBuiltRoom").transform;

            for (int x = 0; x < 3; x++)
                for (int z = 0; z < 3; z++)
                    Place("Env_Floor", root, new Vector3((x + 0.5f) * Cell, 0f, (z + 0.5f) * Cell), 0f);

            for (int i = 0; i < 3; i++)
            {
                float m = (i + 0.5f) * Cell;
                Place(i == 1 ? "Env_WallDoorway" : "Env_Wall", root, new Vector3(m, 0f, 0f), 0f);             // south (door in the middle)
                Place("Env_Wall", root, new Vector3(m, 0f, 3 * Cell), 0f);                                      // north
                Place("Env_Wall", root, new Vector3(0f, 0f, m), 90f);                                           // west
                Place(i == 1 ? "Env_WallGated" : "Env_WallCracked", root, new Vector3(3 * Cell, 0f, m), 90f);  // east (gated vault door)
            }
            for (int x = 0; x <= 3; x++)
                for (int z = 0; z <= 3; z++)
                    if (x == 0 || z == 0 || x == 3 || z == 3)
                        Place("Env_Pillar", root, new Vector3(x * Cell, 0f, z * Cell), 0f);

            var props = new GameObject("Props").transform;
            props.SetParent(root, false);
            Prop("barrel_large", props, 1.4f, 1.4f, 0f);
            Prop("shelf_large", props, 4f, 0.6f, 0f);
            Prop("chair", props, 6f, 6f, 200f);
            Prop("table_medium", props, 6f, 7.5f, 0f);
            Prop("chest", props, 10.4f, 2f, 270f);
            Prop("stool", props, 2.5f, 9.5f, 40f);
            Prop("torch_mounted", props, 0.6f, 6f, 90f, 2.2f);
            Prop("chest_gold", props, 10.2f, 9.8f, 230f);

            var cam = Camera.main;
            cam.transform.SetPositionAndRotation(new Vector3(6f, 13f, -6f), Quaternion.Euler(58f, 0f, 0f));
            EditorSceneManager.SaveScene(scene, ScenePath);
            CMLog.Info("KayKit", "Sandbox_C saved: " + ScenePath);
        }

        private static void Place(string prefab, Transform parent, Vector3 pos, float yaw)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Environment/" + prefab + ".prefab");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
        }

        private static void Prop(string model, Transform parent, float x, float z, float yaw, float y = 0f)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Props/Prop_" + model + ".prefab");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            go.transform.SetPositionAndRotation(new Vector3(x, y, z), Quaternion.Euler(0f, yaw, 0f));
        }
    }
}
