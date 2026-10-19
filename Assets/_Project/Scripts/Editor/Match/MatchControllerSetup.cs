using CinliMahzen.Match;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CinliMahzen.Editor
{
    /// <summary>
    /// Sets up the MatchController (TODO 1.10) in the open scene (Game): a root object "MatchController" with the
    /// <see cref="MatchController"/> component. The empty "MatchController" placeholder object that Build Core Scenes puts into Game.unity
    /// is reused; otherwise a new object is created.
    /// Idempotent: a scene that already has a MatchController is left as it is. Marks the scene dirty but does not save it.
    /// MCP: open Game.unity, execute_menu_item("CinliMahzen/Setup/Add Match Controller To Open Scene"), save the scene.
    /// </summary>
    public static class MatchControllerSetup
    {
        private const string ControllerName = "MatchController";

        [MenuItem("CinliMahzen/Setup/Add Match Controller To Open Scene")]
        public static void Setup()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[SetupMatch] Stop play mode first");
                return;
            }
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError("[SetupMatch] No open scene");
                return;
            }

            MatchController existing = FindController(scene);
            if (existing != null)
            {
                Debug.Log("[SetupMatch] '" + scene.name + "' already has a MatchController ('" + existing.name + "') - nothing to do", existing);
                return;
            }

            GameObject host = FindPlaceholder(scene);
            bool reused = host != null;
            if (!reused)
            {
                host = new GameObject(ControllerName);
                Undo.RegisterCreatedObjectUndo(host, "Create MatchController");
            }
            Undo.AddComponent<MatchController>(host);

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[SetupMatch] " + (reused ? "Added MatchController to the existing '" : "Created '") + ControllerName + "' in '" + scene.name
                + "'; scene is dirty - save it");
        }

        private static MatchController FindController(Scene scene)
        {
            MatchController found = null;
            foreach (MatchController candidate in Object.FindObjectsByType<MatchController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.gameObject.scene != scene)
                    continue;
                if (found == null)
                    found = candidate;
                else
                    Debug.LogWarning("[SetupMatch] More than one MatchController in the scene ('" + candidate.name + "') - keep only one", candidate);
            }
            return found;
        }

        /// <summary>The empty root object named "MatchController" that Build Core Scenes creates, if the scene still has it.</summary>
        private static GameObject FindPlaceholder(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == ControllerName)
                    return root;
            }
            return null;
        }
    }
}
