using CinliMahzen.Jinn;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace CinliMahzen.Editor
{
    /// <summary>
    /// Owner: B. Builds the B0.2 placeholder jinn visuals and the Sandbox_B test scene through the
    /// Unity API (so the serialized assets are always valid). Idempotent: re-running updates the
    /// materials, overwrites the prefabs and rebuilds the scene.
    /// MCP: execute_menu_item("CinliMahzen/Sandbox B/Build Sandbox_B Scene").
    /// </summary>
    public static class SandboxBBuilder
    {
        private const string VfxFolder = "Assets/_Project/Art/VFX";
        private const string JinnPrefabFolder = "Assets/_Project/Prefabs/Jinn";
        private const string ScenePath = "Assets/_Project/Scenes/Sandbox_B.unity";

        private const string EvilPrefabPath = JinnPrefabFolder + "/P_EvilJinnVisual.prefab";
        private const string GoodPrefabPath = JinnPrefabFolder + "/P_GoodJinnVisual.prefab";

        private const string UnlitShader = "Universal Render Pipeline/Unlit";
        private const string ParticleShader = "Universal Render Pipeline/Particles/Unlit";

        // Tech §6.1: evil = purple/red, good = turquoise. Semi-transparent bodies, black eyes.
        private static readonly Color EvilBody = new Color(0.62f, 0.18f, 0.95f, 0.55f);
        private static readonly Color EvilTrail = new Color(0.75f, 0.2f, 1f, 0.6f);
        private static readonly Color GoodBody = new Color(0.1f, 0.9f, 0.85f, 0.55f);
        private static readonly Color GoodTrail = new Color(0.3f, 1f, 0.9f, 0.6f);
        private static readonly Color Eye = new Color(0.02f, 0.02f, 0.03f, 1f);

        // Placeholder geometry (local to the prefab root).
        private const float BodyScale = 0.6f;
        private static readonly Vector3 EyeLocalLeft = new Vector3(-0.18f, 0.1f, 0.42f);
        private static readonly Vector3 EyeLocalRight = new Vector3(0.18f, 0.1f, 0.42f);
        private const float EyeScale = 0.18f;

        [MenuItem("CinliMahzen/Sandbox B/Build Jinn Placeholder Visuals")]
        public static void BuildVisuals()
        {
            EnsureFolder(VfxFolder);
            EnsureFolder(JinnPrefabFolder);

            Material eye = CreateOrUpdateMaterial(VfxFolder + "/M_JinnEye.mat", UnlitShader, Eye, false);
            Material evilBody = CreateOrUpdateMaterial(VfxFolder + "/M_EvilJinnBody.mat", UnlitShader, EvilBody, true);
            Material goodBody = CreateOrUpdateMaterial(VfxFolder + "/M_GoodJinnBody.mat", UnlitShader, GoodBody, true);
            Material evilTrail = CreateOrUpdateMaterial(VfxFolder + "/M_EvilJinnTrail.mat", ParticleShader, EvilTrail, true);
            Material goodTrail = CreateOrUpdateMaterial(VfxFolder + "/M_GoodJinnTrail.mat", ParticleShader, GoodTrail, true);

            BuildVisualPrefab(EvilPrefabPath, "P_EvilJinnVisual", Layer("EvilJinnVisual"), evilBody, eye, evilTrail, EvilTrail);
            BuildVisualPrefab(GoodPrefabPath, "P_GoodJinnVisual", Layer("GoodJinnVisual"), goodBody, eye, goodTrail, GoodTrail);

            AssetDatabase.SaveAssets();
            Debug.Log("[SandboxB] Jinn placeholder visuals built: " + EvilPrefabPath + ", " + GoodPrefabPath);
        }

        [MenuItem("CinliMahzen/Sandbox B/Build Sandbox_B Scene")]
        public static void BuildScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }
            BuildVisuals();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.08f, 0.09f, 0.14f);

            var lightGo = new GameObject("Moon Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.75f, 0.8f, 1f);
            light.intensity = 0.6f;
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.layer = Layer("Environment");
            floor.transform.localScale = new Vector3(3f, 1f, 3f);
            floor.isStatic = true;

            int possessable = Layer("Possessable");
            CreateProp("Shelf (placeholder)", PrimitiveType.Cube, new Vector3(-3f, 1f, 3f), new Vector3(2f, 2f, 0.5f), possessable);
            CreateProp("Barrel (placeholder)", PrimitiveType.Cylinder, new Vector3(3f, 0.5f, 3f), new Vector3(0.8f, 0.5f, 0.8f), possessable);
            CreateProp("Chair (placeholder)", PrimitiveType.Cube, new Vector3(2.5f, 0.45f, -1.5f), new Vector3(0.5f, 0.9f, 0.5f), possessable);

            var jinns = new GameObject("Jinns");
            InstantiateVisual(EvilPrefabPath, "EvilJinn P3", new Vector3(-1.2f, 1.4f, 0f), jinns.transform);
            InstantiateVisual(EvilPrefabPath, "EvilJinn P4", new Vector3(-3f, 1.7f, -1.5f), jinns.transform);
            InstantiateVisual(GoodPrefabPath, "GoodJinn P2", new Vector3(1.2f, 1.4f, 0f), jinns.transform);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.03f, 0.05f);
            cam.fieldOfView = 60f;
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(0f, 1.8f, -4.5f);
            camGo.transform.LookAt(new Vector3(-0.3f, 1.3f, 0f));

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[SandboxB] Scene saved: " + ScenePath);
        }

        private static void BuildVisualPrefab(string path, string name, int layer, Material bodyMat, Material eyeMat,
            Material trailMat, Color trailColor)
        {
            var root = new GameObject(name);

            GameObject body = CreateVisualPrimitive("Body", root.transform, Vector3.zero, BodyScale, bodyMat);
            CreateVisualPrimitive("EyeL", body.transform, EyeLocalLeft, EyeScale, eyeMat);
            CreateVisualPrimitive("EyeR", body.transform, EyeLocalRight, EyeScale, eyeMat);

            var visual = root.AddComponent<SpiritPlaceholderVisual>();
            var so = new SerializedObject(visual);
            so.FindProperty("body").objectReferenceValue = body.transform;
            so.FindProperty("trailMaterial").objectReferenceValue = trailMat;
            so.FindProperty("trailColor").colorValue = trailColor;
            so.ApplyModifiedPropertiesWithoutUndo();

            SetLayerRecursively(root, layer);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static GameObject CreateVisualPrimitive(string name, Transform parent, Vector3 localPos, float scale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = Vector3.one * scale;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go;
        }

        private static void CreateProp(string name, PrimitiveType type, Vector3 pos, Vector3 scale, int layer)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.layer = layer;
            go.transform.position = pos;
            go.transform.localScale = scale;
        }

        private static void InstantiateVisual(string prefabPath, string name, Vector3 pos, Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        }

        private static Material CreateOrUpdateMaterial(string path, string shaderName, Color color, bool transparent)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError("[SandboxB] Shader not found: " + shaderName);
                return null;
            }
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            mat.SetColor("_BaseColor", color);
            if (transparent)
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend", 0f);
                mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                mat.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_ZWrite", 0f);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.renderQueue = (int)RenderQueue.Transparent;
            }
            else
            {
                mat.SetFloat("_Surface", 0f);
                mat.SetFloat("_SrcBlend", (float)BlendMode.One);
                mat.SetFloat("_DstBlend", (float)BlendMode.Zero);
                mat.SetFloat("_ZWrite", 1f);
                mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.SetOverrideTag("RenderType", "Opaque");
                mat.renderQueue = (int)RenderQueue.Geometry;
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static int Layer(string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer < 0)
            {
                Debug.LogError("[SandboxB] Missing layer '" + layerName + "' (Tech §2 / A0.4)");
                return 0;
            }
            return layer;
        }

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }
            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
        }
    }
}
