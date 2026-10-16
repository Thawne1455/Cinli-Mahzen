using CinliMahzen.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CinliMahzen.Editor
{
    /// <summary>
    /// Puts the test interactable (DebugInteractableCube: 0.6 m cube on layer Interactable, NetId 1001) at (0, 0.5, -3)
    /// in the open scene (TODO 1.4). Idempotent: an existing DebugInteractableCube is moved and reconfigured instead of
    /// duplicated. The scene is saved when it has a path.
    /// MCP: execute_menu_item("CinliMahzen/Setup/Place Test Interactable In Open Scene").
    /// </summary>
    public static class InteractableTestSetup
    {
        private const string InteractableLayer = "Interactable";
        private const string CubeName = "DebugInteractableCube";
        private const int TestNetId = 1001;
        private const float CubeSize = 0.6f;

        private static readonly Vector3 CubePosition = new Vector3(0f, 0.5f, -3f);

        [MenuItem("CinliMahzen/Setup/Place Test Interactable In Open Scene")]
        public static void Place()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[SetupInteract] Stop play mode first");
                return;
            }
            int layer = LayerMask.NameToLayer(InteractableLayer);
            if (layer < 0)
            {
                Debug.LogError("[SetupInteract] Layer '" + InteractableLayer + "' is missing (Teknik section 2, layer 15)");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            var cube = Object.FindFirstObjectByType<DebugInteractableCube>();
            GameObject instance;
            if (cube != null)
            {
                instance = cube.gameObject;
            }
            else
            {
                instance = GameObject.CreatePrimitive(PrimitiveType.Cube);
                instance.name = CubeName;
                cube = instance.AddComponent<DebugInteractableCube>();
            }

            instance.transform.position = CubePosition;
            instance.transform.rotation = Quaternion.identity;
            instance.transform.localScale = new Vector3(CubeSize, CubeSize, CubeSize);
            instance.layer = layer;

            var so = new SerializedObject(cube);
            so.FindProperty("netId").intValue = TestNetId;
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!string.IsNullOrEmpty(scene.path))
                EditorSceneManager.SaveScene(scene);
            Debug.Log("[SetupInteract] Test interactable placed at " + CubePosition + " (N" + TestNetId + ", layer " + InteractableLayer + ")");
        }
    }
}
