using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CinliMahzen.Editor
{
    /// <summary>
    /// Owner: A. Creates Boot / MainMenu / Game / Sandbox_A scenes (only the missing ones — existing scenes are never
    /// overwritten), Resources/CM_BootSettings.asset and the build scene list (Boot, MainMenu, Game).
    /// MCP: execute_menu_item("CinliMahzen/Setup A/Build Core Scenes").
    /// </summary>
    public static class CoreScenesBuilder
    {
        private const string SceneFolder = "Assets/_Project/Scenes";
        private const string ConfigPath = "Assets/_Project/ScriptableObjects/Config/GameBalanceConfig.asset";
        private const string BootSettingsPath = "Assets/_Project/Resources/CM_BootSettings.asset";

        [MenuItem("CinliMahzen/Setup A/Build Core Scenes")]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[SetupA] Stop play mode first");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EnsureBootSettings();
            string boot = BuildIfMissing("Boot", BuildBoot);
            string menu = BuildIfMissing("MainMenu", BuildMainMenu);
            string game = BuildIfMissing("Game", BuildGame);
            BuildIfMissing("Sandbox_A", BuildSandboxA);

            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(boot, true),
                new EditorBuildSettingsScene(menu, true),
                new EditorBuildSettingsScene(game, true),
            };
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("[SetupA] Core scenes ready; build list = Boot, MainMenu, Game");
        }

        private static void EnsureBootSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<BootSettings>(BootSettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<BootSettings>();
                AssetDatabase.CreateAsset(settings, BootSettingsPath);
            }
            var so = new SerializedObject(settings);
            so.FindProperty("config").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameBalanceConfig>(ConfigPath);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
        }

        private static string BuildIfMissing(string name, System.Action fill)
        {
            string path = SceneFolder + "/" + name + ".unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
                return path;
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            fill();
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log("[SetupA] Created " + path);
            return path;
        }

        private static void BuildBoot()
        {
            AddCamera(Color.black);
            new GameObject("BootLoader").AddComponent<BootSceneLoader>();
        }

        private static void BuildMainMenu()
        {
            AddCamera(new Color(0.06f, 0.05f, 0.08f));
            new GameObject("MainMenu").AddComponent<MainMenuPlaceholder>();
        }

        private static void BuildGame()
        {
            AddCamera(Color.black);
            AddSun();
            new GameObject("MatchController");
            new GameObject("LevelRoot");
            new GameObject("UIRoot");
            new GameObject("AudioRoot");
        }

        private static void BuildSandboxA()
        {
            AddCamera(new Color(0.2f, 0.2f, 0.25f)).transform.SetPositionAndRotation(new Vector3(0f, 6f, -10f), Quaternion.Euler(30f, 0f, 0f));
            AddSun();
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(4f, 1f, 4f);
        }

        private static Camera AddCamera(Color bg)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = bg;
            go.AddComponent<AudioListener>();
            go.transform.position = new Vector3(0f, 2f, -5f);
            return cam;
        }

        private static void AddSun()
        {
            var go = new GameObject("Directional Light");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.6f;
            light.shadows = LightShadows.Soft;
            go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }
    }
}
