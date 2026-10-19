using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.Match;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CinliMahzen.Editor
{
    /// <summary>
    /// Sets up the PawnSpawner (TODO 1.3) in the open scene (Game, Sandbox): a root object "PawnSpawner" with <see cref="PawnSpawner"/> whose
    /// prefab slots point at P_Human and P_Spirit. A missing P_Spirit is built first; P_Human must exist (Build Human Prefab).
    /// The spawner owns the pawns of its scene, so hand-placed P_Human / P_Spirit instances (the Sandbox human from "Place Human In Sandbox")
    /// are deleted from the scene — the spawner would otherwise remove a colliding one at runtime with a warning. Other pawn objects are
    /// left alone and only reported.
    /// Idempotent: an existing spawner is reused and its references refreshed. Marks the scene dirty but does not save it.
    /// MCP: open the scene, execute_menu_item("CinliMahzen/Setup/Add Pawn Spawner To Open Scene"), save the scene.
    /// </summary>
    public static class PawnSpawnerSetup
    {
        private const string SpawnerName = "PawnSpawner";

        [MenuItem("CinliMahzen/Setup/Add Pawn Spawner To Open Scene")]
        public static void Setup()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[SetupSpawn] Stop play mode first");
                return;
            }
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError("[SetupSpawn] No open scene");
                return;
            }

            GameObject humanAsset = AssetDatabase.LoadAssetAtPath<GameObject>(HumanPrefabBuilder.PrefabPath);
            if (humanAsset == null)
            {
                Debug.LogError("[SetupSpawn] " + HumanPrefabBuilder.PrefabPath + " missing — run CinliMahzen/Setup/Build Human Prefab first");
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(SpiritPrefabBuilder.PrefabPath) == null)
                SpiritPrefabBuilder.Build();
            GameObject spiritAsset = AssetDatabase.LoadAssetAtPath<GameObject>(SpiritPrefabBuilder.PrefabPath);
            if (spiritAsset == null)
            {
                Debug.LogError("[SetupSpawn] " + SpiritPrefabBuilder.PrefabPath + " missing — run CinliMahzen/Setup/Build Spirit Prefab first");
                return;
            }
            PawnBase humanPawn = humanAsset.GetComponent<PawnBase>();
            PawnBase spiritPawn = spiritAsset.GetComponent<PawnBase>();
            if (humanPawn == null || spiritPawn == null)
            {
                Debug.LogError("[SetupSpawn] P_Human / P_Spirit have no PawnBase component — rebuild the prefabs");
                return;
            }

            PawnSpawner spawner = FindSpawner(scene);
            bool created = spawner == null;
            if (created)
            {
                var go = new GameObject(SpawnerName);
                Undo.RegisterCreatedObjectUndo(go, "Create PawnSpawner");
                spawner = Undo.AddComponent<PawnSpawner>(go);
            }

            var so = new SerializedObject(spawner);
            so.FindProperty("humanPrefab").objectReferenceValue = humanPawn;
            so.FindProperty("spiritPrefab").objectReferenceValue = spiritPawn;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(spawner);

            int removed = RemoveHandPlacedPawns(scene, humanAsset, spiritAsset);

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[SetupSpawn] " + (created ? "Created PawnSpawner" : "PawnSpawner already there") + " in '" + scene.name + "' -> "
                + HumanPrefabBuilder.PrefabPath + ", " + SpiritPrefabBuilder.PrefabPath + "; removed " + removed
                + " hand-placed pawn(s); scene is dirty - save it");
        }

        private static PawnSpawner FindSpawner(Scene scene)
        {
            PawnSpawner found = null;
            foreach (PawnSpawner candidate in Object.FindObjectsByType<PawnSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.gameObject.scene != scene)
                    continue;
                if (found == null)
                    found = candidate;
                else
                    Debug.LogWarning("[SetupSpawn] More than one PawnSpawner in the scene ('" + candidate.name + "') — keep only one", candidate);
            }
            return found;
        }

        private static int RemoveHandPlacedPawns(Scene scene, GameObject humanAsset, GameObject spiritAsset)
        {
            var doomed = new List<GameObject>();
            foreach (PawnBase pawn in Object.FindObjectsByType<PawnBase>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (pawn.gameObject.scene != scene)
                    continue;
                GameObject root = PrefabUtility.GetNearestPrefabInstanceRoot(pawn.gameObject);
                GameObject source = root != null ? PrefabUtility.GetCorrespondingObjectFromSource(root) : null;
                if (source != null && (source == humanAsset || source == spiritAsset))
                {
                    if (!doomed.Contains(root))
                        doomed.Add(root);
                }
                else
                {
                    Debug.LogWarning("[SetupSpawn] Pawn '" + pawn.name + "' in the scene is not a P_Human / P_Spirit instance — left alone"
                        + " (the spawner removes it at runtime only if it holds a NetId the spawner plans)", pawn);
                }
            }
            foreach (GameObject go in doomed)
            {
                Debug.Log("[SetupSpawn] Removing hand-placed pawn '" + go.name + "' (the PawnSpawner creates pawns at runtime)");
                Undo.DestroyObjectImmediate(go);
            }
            return doomed.Count;
        }
    }
}
