using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.Objectives;
using UnityEditor;
using UnityEngine;

namespace CinliMahzen.Editor
{
    /// <summary>
    /// Shared helpers of the graybox puzzle prefab builders (TODO 2.5, 3.1–3.3): primitive cubes without colliders, colour materials saved
    /// under <see cref="MaterialFolder"/>, serialized-field wiring, layers, and saving both prefabs + linking them into the PZ_* definition.
    /// </summary>
    internal static class PuzzlePrefabKit
    {
        public const string PrefabFolder = "Assets/_Project/Prefabs/Objectives";
        public const string MaterialFolder = "Assets/_Project/Art/Materials/Puzzles";
        public const string InteractableLayer = "Interactable";
        public const string SpiritOnlyLayer = "SpiritOnly";

        public static bool CheckNotPlaying(string what)
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                return true;
            Debug.LogError("[Setup] " + what + ": stop play mode first");
            return false;
        }

        public static GameObject Cube(string name, Transform parent, Vector3 localPos, Vector3 size, Material mat)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            Object.DestroyImmediate(cube.GetComponent<Collider>());
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPos;
            cube.transform.localScale = size;
            cube.GetComponent<MeshRenderer>().sharedMaterial = mat;
            cube.layer = parent.gameObject.layer;
            return cube;
        }

        public static GameObject Empty(string name, Transform parent, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.layer = parent.gameObject.layer;
            return go;
        }

        /// <summary>A GameObject on the Interactable layer with a box collider of <paramref name="colliderSize"/>.</summary>
        public static GameObject Interactable(string name, Transform parent, Vector3 localPos, Vector3 colliderSize)
        {
            GameObject go = Empty(name, parent, localPos);
            int layer = LayerMask.NameToLayer(InteractableLayer);
            if (layer >= 0)
                go.layer = layer;
            go.AddComponent<BoxCollider>().size = colliderSize;
            return go;
        }

        public static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetEnum(Object target, string field, int value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).enumValueIndex = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetFloat(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetString(Object target, string field, string value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetArray<T>(Object target, string field, IList<T> values) where T : Object
        {
            var so = new SerializedObject(target);
            SerializedProperty array = so.FindProperty(field);
            array.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>A display on <paramref name="host"/> that changes <paramref name="visual"/>.</summary>
        public static PuzzlePartDisplay Display(GameObject host, Transform visual, PuzzlePartDisplay.Mode mode, float degreesPerValue = 90f, float baseAngle = 0f)
        {
            var display = host.AddComponent<PuzzlePartDisplay>();
            var so = new SerializedObject(display);
            so.FindProperty("mode").enumValueIndex = (int)mode;
            so.FindProperty("visual").objectReferenceValue = visual;
            so.FindProperty("degreesPerValue").floatValue = degreesPerValue;
            so.FindProperty("baseAngle").floatValue = baseAngle;
            so.ApplyModifiedPropertiesWithoutUndo();
            return display;
        }

        /// <summary>A part (Cycle or Toggle) driving <paramref name="display"/>.</summary>
        public static PuzzlePart Part(GameObject host, PuzzlePartDisplay display, PuzzlePart.PartOp op = PuzzlePart.PartOp.Cycle, string promptKey = "interact.puzzle_turn")
        {
            var part = host.AddComponent<PuzzlePart>();
            var so = new SerializedObject(part);
            so.FindProperty("display").objectReferenceValue = display;
            so.FindProperty("op").enumValueIndex = (int)op;
            so.FindProperty("promptKey").stringValue = promptKey;
            so.ApplyModifiedPropertiesWithoutUndo();
            return part;
        }

        /// <summary>A pull lever (base + tipping handle) with <see cref="ConfirmLever"/>.</summary>
        public static ConfirmLever Lever(Transform parent, Vector3 localPos, Material baseMat, Material stickMat)
        {
            GameObject lever = Interactable("ConfirmLever", parent, localPos, new Vector3(0.2f, 0.5f, 0.3f));
            lever.GetComponent<BoxCollider>().center = new Vector3(0f, 0.1f, 0.1f);
            Cube("Base", lever.transform, Vector3.zero, new Vector3(0.15f, 0.2f, 0.08f), baseMat);
            GameObject handle = Empty("Handle", lever.transform, new Vector3(0f, 0f, 0.05f));
            Cube("Stick", handle.transform, new Vector3(0f, 0.18f, 0f), new Vector3(0.04f, 0.36f, 0.04f), stickMat);
            var confirm = lever.AddComponent<ConfirmLever>();
            SetRef(confirm, "handle", handle.transform);
            return confirm;
        }

        /// <summary>PuzzleView (+ PuzzleScrambleTarget) on <paramref name="root"/>.</summary>
        public static PuzzleView View(GameObject root, IList<PuzzlePart> parts, ConfirmLever lever, Transform shakeRoot)
        {
            var view = root.AddComponent<PuzzleView>();
            root.AddComponent<PuzzleScrambleTarget>();
            SetArray(view, "parts", parts);
            SetRef(view, "lever", lever);
            SetRef(view, "shakeRoot", shakeRoot);
            return view;
        }

        public static PuzzleReference Reference(GameObject root, IList<PuzzlePartDisplay> parts, GameObject content, GameObject veil, Transform rotationRoot = null)
        {
            var reference = root.AddComponent<PuzzleReference>();
            SetArray(reference, "parts", parts);
            SetRef(reference, "content", content);
            SetRef(reference, "veil", veil);
            if (rotationRoot != null)
                SetRef(reference, "rotationRoot", rotationRoot);
            return reference;
        }

        public static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
                SetLayerRecursively(child.gameObject, layer);
        }

        public static void MakeSpiritOnly(GameObject root)
        {
            int spiritOnly = LayerMask.NameToLayer(SpiritOnlyLayer);
            if (spiritOnly >= 0)
                SetLayerRecursively(root, spiritOnly);
            else
                Debug.LogWarning("[Setup] Layer " + SpiritOnlyLayer + " missing - the reference would be visible to the human");
        }

        public static Material Material(string name, Color color, bool transparent = false)
        {
            EnsureFolder(MaterialFolder);
            string path = MaterialFolder + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.color = color;
            if (transparent && mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend", 0f);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        public static Material Veil()
        {
            return Material("M_Puzzle_SpiritVeil", new Color(0.6f, 0.75f, 1f, 0.55f), true);
        }

        /// <summary>Saves both prefabs (overwriting), destroys the scene copies and links them into the PZ_* asset. Returns false when the definition is missing.</summary>
        public static bool SaveAndLink(GameObject puzzle, string puzzlePath, GameObject reference, string referencePath, string definitionAssetName)
        {
            EnsureFolder(PrefabFolder);
            GameObject puzzleAsset = PrefabUtility.SaveAsPrefabAsset(puzzle, puzzlePath);
            GameObject referenceAsset = PrefabUtility.SaveAsPrefabAsset(reference, referencePath);
            Object.DestroyImmediate(puzzle);
            Object.DestroyImmediate(reference);

            string definitionPath = PuzzleDefinitionSetup.Folder + "/" + definitionAssetName + ".asset";
            var definition = AssetDatabase.LoadAssetAtPath<PuzzleDefinition>(definitionPath);
            bool linked = definition != null;
            if (linked)
            {
                var so = new SerializedObject(definition);
                so.FindProperty("interactivePrefab").objectReferenceValue = puzzleAsset;
                so.FindProperty("referencePrefab").objectReferenceValue = referenceAsset;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(definition);
                Debug.Log("[Setup] Linked " + puzzlePath + " and " + referencePath + " into " + definitionPath);
            }
            else
            {
                Debug.LogWarning("[Setup] " + definitionPath + " not found - run CinliMahzen/Setup/Create Puzzle Definitions, then build again");
            }
            AssetDatabase.SaveAssets();
            return linked;
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
