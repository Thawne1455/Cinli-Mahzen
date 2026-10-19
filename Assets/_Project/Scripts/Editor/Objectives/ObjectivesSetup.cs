using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.Objectives;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CinliMahzen.Editor
{
    /// <summary>
    /// TODO 2.7: <c>CinliMahzen/Setup/Add Objectives To Open Scene</c> — a root "Objectives" object with <see cref="PuzzleSystem"/> and
    /// <see cref="RoundSetupApplier"/>, its pool filled with every PZ_* asset and its item list with every ID_* asset. Idempotent: an existing
    /// applier only gets its two lists refreshed (run it again after adding a puzzle definition). Marks the scene dirty; save it afterwards.
    /// </summary>
    public static class ObjectivesSetup
    {
        private const string ObjectName = "Objectives";
        private const string ItemFolder = "Assets/_Project/ScriptableObjects/Items";

        [MenuItem("CinliMahzen/Setup/Add Objectives To Open Scene")]
        public static void Setup()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[SetupObjectives] Stop play mode first");
                return;
            }
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError("[SetupObjectives] No open scene");
                return;
            }

            RoundSetupApplier applier = null;
            foreach (RoundSetupApplier candidate in Object.FindObjectsByType<RoundSetupApplier>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.gameObject.scene == scene)
                {
                    applier = candidate;
                    break;
                }
            }

            bool created = applier == null;
            if (created)
            {
                PuzzleSystem existingSystem = null;
                foreach (PuzzleSystem candidate in Object.FindObjectsByType<PuzzleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (candidate.gameObject.scene == scene)
                    {
                        existingSystem = candidate;
                        break;
                    }
                }
                GameObject host = existingSystem != null ? existingSystem.gameObject : new GameObject(ObjectName);
                if (existingSystem == null)
                {
                    Undo.RegisterCreatedObjectUndo(host, "Create Objectives");
                    Undo.AddComponent<PuzzleSystem>(host);
                }
                applier = Undo.AddComponent<RoundSetupApplier>(host);
            }

            List<PuzzleDefinition> pool = LoadAll<PuzzleDefinition>("t:PuzzleDefinition", PuzzleDefinitionSetup.Folder);
            List<ItemDefinition> items = LoadAll<ItemDefinition>("t:ItemDefinition", ItemFolder);
            var so = new SerializedObject(applier);
            Fill(so.FindProperty("pool"), pool);
            Fill(so.FindProperty("items"), items);
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[SetupObjectives] " + (created ? "Created" : "Refreshed") + " '" + applier.name + "' in '" + scene.name + "': "
                + pool.Count + " puzzle definitions, " + items.Count + " items; scene is dirty - save it");
            if (pool.Count == 0)
                Debug.LogWarning("[SetupObjectives] No PZ_* assets - run CinliMahzen/Setup/Create Puzzle Definitions first");
        }

        private static List<T> LoadAll<T>(string filter, string folder) where T : Object
        {
            var result = new List<T>();
            if (!AssetDatabase.IsValidFolder(folder))
                return result;
            foreach (string guid in AssetDatabase.FindAssets(filter, new[] { folder }))
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null)
                    result.Add(asset);
            }
            result.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return result;
        }

        private static void Fill<T>(SerializedProperty array, List<T> values) where T : Object
        {
            array.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
