using CinliMahzen.Possession;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CinliMahzen.Editor
{
    /// <summary>
    /// Scene setup of possession (TODO 1.7). Both commands only mark the open scene dirty; save it afterwards.
    /// <list type="bullet">
    /// <item><c>CinliMahzen/Setup/Add Possession System To Open Scene</c>: a root "PossessionSystem" object with <see cref="PossessionSystem"/>
    /// (idempotent).</item>
    /// <item><c>CinliMahzen/Setup/Add Test Possessable Chair To Open Scene</c>: a graybox chair (box collider + "Visual" child cube) with
    /// <see cref="Possessable"/> using <c>PD_Chair</c>, 3 m in front of the scene origin, for the 1.7 acceptance run in Sandbox.</item>
    /// </list>
    /// </summary>
    public static class PossessionSetup
    {
        private const string SystemName = "PossessionSystem";
        private const string ChairName = "Test_Possessable_Chair";
        private const string ChairDefinitionPath = "Assets/_Project/ScriptableObjects/Possessables/PD_Chair.asset";
        private static readonly Vector3 ChairPosition = new Vector3(0f, 0.5f, 3f);
        private static readonly Vector3 ChairSize = new Vector3(0.6f, 1f, 0.6f);

        [MenuItem("CinliMahzen/Setup/Add Possession System To Open Scene")]
        public static void AddSystem()
        {
            if (!TryGetEditableScene(out Scene scene))
                return;

            foreach (PossessionSystem existing in Object.FindObjectsByType<PossessionSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (existing.gameObject.scene == scene)
                {
                    Debug.Log("[SetupPossession] '" + scene.name + "' already has a PossessionSystem ('" + existing.name + "') - nothing to do", existing);
                    return;
                }
            }

            var host = new GameObject(SystemName);
            Undo.RegisterCreatedObjectUndo(host, "Create PossessionSystem");
            Undo.AddComponent<PossessionSystem>(host);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[SetupPossession] Created '" + SystemName + "' in '" + scene.name + "'; scene is dirty - save it");
        }

        [MenuItem("CinliMahzen/Setup/Add Test Possessable Chair To Open Scene")]
        public static void AddTestChair()
        {
            if (!TryGetEditableScene(out Scene scene))
                return;

            var definition = AssetDatabase.LoadAssetAtPath<PossessableDefinition>(ChairDefinitionPath);
            if (definition == null)
            {
                Debug.LogError("[SetupPossession] " + ChairDefinitionPath + " not found");
                return;
            }

            var root = new GameObject(ChairName);
            Undo.RegisterCreatedObjectUndo(root, "Create test chair");
            root.transform.position = ChairPosition;
            var box = root.AddComponent<BoxCollider>();
            box.size = ChairSize;

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Visual";
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = ChairSize;

            var possessable = root.AddComponent<Possessable>();
            var so = new SerializedObject(possessable);
            so.FindProperty("definition").objectReferenceValue = definition;
            so.FindProperty("visual").objectReferenceValue = visual.transform;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = root;
            Debug.Log("[SetupPossession] Added '" + ChairName + "' (PD_Chair) at " + ChairPosition + " in '" + scene.name + "'; scene is dirty - save it");
        }

        private static bool TryGetEditableScene(out Scene scene)
        {
            scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[SetupPossession] Stop play mode first");
                return false;
            }
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError("[SetupPossession] No open scene");
                return false;
            }
            return true;
        }
    }
}
