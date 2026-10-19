using CinliMahzen.Possession;
using CinliMahzen.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CinliMahzen.Editor
{
    /// <summary>
    /// TODO 3.4: <c>CinliMahzen/Setup/Add Possessable Lamps To House In Open Scene</c> — under every LampMarker of the open scene's house a small
    /// graybox lamp ("PossessableLamp": box collider + "Visual" cube) with <see cref="Possessable"/> using <c>PD_Lamp</c>, so the evil jinn can
    /// enter it and Burst it (the light service maps the lamp to its marker by room and distance). Markers that already have one are skipped.
    /// Marks the scene dirty; save it afterwards.
    /// </summary>
    public static class PossessableLampsSetup
    {
        private const string LampName = "PossessableLamp";
        private const string DefinitionPath = "Assets/_Project/ScriptableObjects/Possessables/PD_Lamp.asset";
        private static readonly Vector3 LampSize = new Vector3(0.25f, 0.3f, 0.25f);

        [MenuItem("CinliMahzen/Setup/Add Possessable Lamps To House In Open Scene")]
        public static void Setup()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[SetupLamps] Stop play mode first");
                return;
            }
            var definition = AssetDatabase.LoadAssetAtPath<PossessableDefinition>(DefinitionPath);
            if (definition == null)
            {
                Debug.LogError("[SetupLamps] " + DefinitionPath + " not found");
                return;
            }
            Scene scene = SceneManager.GetActiveScene();
            int added = 0;
            int skipped = 0;
            foreach (LampMarker marker in Object.FindObjectsByType<LampMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (marker.gameObject.scene != scene)
                    continue;
                if (marker.GetComponentInChildren<Possessable>(true) != null)
                {
                    skipped++;
                    continue;
                }

                var root = new GameObject(LampName);
                Undo.RegisterCreatedObjectUndo(root, "Add possessable lamp");
                root.transform.SetParent(marker.transform, false);
                root.AddComponent<BoxCollider>().size = LampSize;
                GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visual.name = "Visual";
                Object.DestroyImmediate(visual.GetComponent<Collider>());
                visual.transform.SetParent(root.transform, false);
                visual.transform.localScale = LampSize;
                int layer = LayerMask.NameToLayer("Possessable");
                if (layer >= 0)
                {
                    root.layer = layer;
                    visual.layer = layer;
                }

                var possessable = root.AddComponent<Possessable>();
                var so = new SerializedObject(possessable);
                so.FindProperty("definition").objectReferenceValue = definition;
                so.FindProperty("visual").objectReferenceValue = visual.transform;
                so.ApplyModifiedPropertiesWithoutUndo();
                added++;
            }

            if (added > 0)
                EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[SetupLamps] " + added + " possessable lamps added, " + skipped + " markers already had one" + (added > 0 ? "; scene is dirty - save it" : string.Empty));
        }
    }
}
