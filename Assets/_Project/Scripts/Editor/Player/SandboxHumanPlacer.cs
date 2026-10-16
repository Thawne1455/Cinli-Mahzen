using CinliMahzen.Core;
using CinliMahzen.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CinliMahzen.Editor
{
    /// <summary>
    /// Puts P_Human into Assets/_Project/Scenes/Sandbox.unity at (0, 0.1, -6) as pawn N1, owner P0, role Human, and moves the
    /// Sandbox "Ground" plane to the Environment layer (it is created on Default, which HumanPawn does not collide with).
    /// TEMPORARY: delete this menu when the PawnSpawner (TODO 1.3) spawns the pawns at runtime.
    /// Idempotent: an existing HumanController in the scene is repositioned and reconfigured instead of duplicated.
    /// MCP: execute_menu_item("CinliMahzen/Setup/Place Human In Sandbox") after "Build Human Prefab".
    /// </summary>
    public static class SandboxHumanPlacer
    {
        private const string SandboxScenePath = "Assets/_Project/Scenes/Sandbox.unity";
        private const string GroundName = "Ground";
        private const string EnvironmentLayer = "Environment";

        private static readonly Vector3 SpawnPosition = new Vector3(0f, 0.1f, -6f);
        private static readonly PlayerId StartOwner = new PlayerId(0);
        private const Role StartRole = Role.Human;

        [MenuItem("CinliMahzen/Setup/Place Human In Sandbox")]
        public static void Place()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[SetupPlayer] Stop play mode first");
                return;
            }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HumanPrefabBuilder.PrefabPath);
            if (prefab == null)
            {
                Debug.LogError("[SetupPlayer] " + HumanPrefabBuilder.PrefabPath + " missing — run CinliMahzen/Setup/Build Human Prefab first");
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SandboxScenePath) == null)
            {
                Debug.LogError("[SetupPlayer] " + SandboxScenePath + " missing");
                return;
            }

            if (SceneManager.GetActiveScene().path != SandboxScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    return;
                EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single);
            }
            Scene scene = SceneManager.GetActiveScene();

            var human = Object.FindFirstObjectByType<HumanController>();
            GameObject instance;
            if (human != null)
            {
                instance = human.gameObject;
            }
            else
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                human = instance.GetComponent<HumanController>();
            }
            instance.transform.SetPositionAndRotation(SpawnPosition, Quaternion.identity);

            // PawnBase's serialized fields are the initial state of a hand-placed pawn.
            var so = new SerializedObject(human);
            so.FindProperty("netId").intValue = NetId.PawnMin + StartOwner.Value;
            so.FindProperty("ownerId").intValue = StartOwner.Value;
            so.FindProperty("role").enumValueIndex = RoleEnumIndex(so.FindProperty("role"), StartRole);
            so.ApplyModifiedProperties();
            PrefabUtility.RecordPrefabInstancePropertyModifications(human);

            FixGroundLayer(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[SetupPlayer] Human placed in Sandbox at " + SpawnPosition + " (N" + (NetId.PawnMin + StartOwner.Value)
                + ", owner " + StartOwner + ", role " + StartRole + "); scene saved");
        }

        private static void FixGroundLayer(Scene scene)
        {
            int environment = LayerMask.NameToLayer(EnvironmentLayer);
            if (environment < 0)
            {
                Debug.LogWarning("[SetupPlayer] Layer '" + EnvironmentLayer + "' missing — Ground layer not changed");
                return;
            }
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                if (rootObject.name == GroundName && rootObject.layer != environment)
                {
                    rootObject.layer = environment;
                    Debug.Log("[SetupPlayer] Sandbox '" + GroundName + "' moved to layer " + EnvironmentLayer);
                }
            }
        }

        private static int RoleEnumIndex(SerializedProperty property, Role role)
        {
            string[] names = property.enumNames;
            for (int i = 0; i < names.Length; i++)
            {
                if (names[i] == role.ToString())
                    return i;
            }
            return 0;
        }
    }
}
