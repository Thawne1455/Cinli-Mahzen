using CinliMahzen.Core;
using CinliMahzen.Objectives;
using UnityEditor;
using UnityEngine;

namespace CinliMahzen.Editor
{
    /// <summary>
    /// TODO 2.5: <c>CinliMahzen/Setup/Build Paintings Puzzle Prefabs</c> builds the graybox P1 Portraits puzzle from primitives and links
    /// it into <c>PZ_Paintings</c> (interactivePrefab / referencePrefab; run <c>Create Puzzle Definitions</c> first).
    /// <list type="bullet">
    /// <item><c>P_Puzzle_Paintings</c> (wall slot, +Z into the room): three frames (Mustached Grandpa, Goggle-eyed Granny, Upside-down Donkey)
    /// 0.8 m apart, each a <see cref="PuzzlePart"/> + <see cref="PuzzlePartDisplay"/> (RotateRoll 90°) on the Interactable layer with a dark
    /// "up" marker so the angle reads at a glance, and a <see cref="ConfirmLever"/> on the right. Root: <see cref="PuzzleView"/>.</item>
    /// <item><c>P_PuzzleRef_Paintings</c> (SpiritOnly layer): the same three portraits as <see cref="PuzzlePartDisplay"/>s under "Content"
    /// and a translucent "Veil" in front. Root: <see cref="PuzzleReference"/>.</item>
    /// </list>
    /// Rebuilding overwrites both prefabs (and the four M_Puzzle_* materials). Last log line: "Build Paintings Puzzle Prefabs: done".
    /// </summary>
    public static class PaintingsPuzzleBuilder
    {
        public const string PrefabFolder = "Assets/_Project/Prefabs/Objectives";
        public const string PuzzlePrefabPath = PrefabFolder + "/P_Puzzle_Paintings.prefab";
        public const string ReferencePrefabPath = PrefabFolder + "/P_PuzzleRef_Paintings.prefab";

        private const string MaterialFolder = "Assets/_Project/Art/Materials/Puzzles";
        private const string DefinitionPath = PuzzleDefinitionSetup.Folder + "/PZ_Paintings.asset";
        private const string InteractableLayer = "Interactable";
        private const string SpiritOnlyLayer = "SpiritOnly";

        private const float FrameSpacing = 0.8f;
        private const float FrameSize = 0.6f;
        private const float PortraitSize = 0.5f;
        private const float Depth = 0.06f;

        private static readonly string[] PortraitNames = { "MustachedGrandpa", "GoggleEyedGranny", "UpsideDownDonkey" };
        private static readonly Color[] PortraitColors =
        {
            new Color(0.55f, 0.35f, 0.2f), new Color(0.55f, 0.3f, 0.6f), new Color(0.55f, 0.55f, 0.55f),
        };

        [MenuItem("CinliMahzen/Setup/Build Paintings Puzzle Prefabs")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Setup] Build Paintings Puzzle Prefabs: stop play mode first");
                return;
            }

            EnsureFolder(PrefabFolder);
            EnsureFolder(MaterialFolder);
            var portraitMaterials = new Material[PortraitColors.Length];
            for (int i = 0; i < PortraitColors.Length; i++)
                portraitMaterials[i] = GetMaterial("M_Puzzle_" + PortraitNames[i], PortraitColors[i]);
            Material frameMat = GetMaterial("M_Puzzle_Frame", new Color(0.25f, 0.15f, 0.08f));
            Material markerMat = GetMaterial("M_Puzzle_Marker", new Color(0.05f, 0.05f, 0.05f));
            Material veilMat = GetMaterial("M_Puzzle_SpiritVeil", new Color(0.6f, 0.75f, 1f, 0.55f), true);

            GameObject puzzle = BuildPuzzle(portraitMaterials, frameMat, markerMat);
            GameObject reference = BuildReference(portraitMaterials, markerMat, veilMat);
            GameObject puzzleAsset = PrefabUtility.SaveAsPrefabAsset(puzzle, PuzzlePrefabPath);
            GameObject referenceAsset = PrefabUtility.SaveAsPrefabAsset(reference, ReferencePrefabPath);
            Object.DestroyImmediate(puzzle);
            Object.DestroyImmediate(reference);

            var definition = AssetDatabase.LoadAssetAtPath<PuzzleDefinition>(DefinitionPath);
            if (definition != null)
            {
                var so = new SerializedObject(definition);
                so.FindProperty("interactivePrefab").objectReferenceValue = puzzleAsset;
                so.FindProperty("referencePrefab").objectReferenceValue = referenceAsset;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(definition);
                Debug.Log("[Setup] Linked both prefabs into " + DefinitionPath);
            }
            else
            {
                Debug.LogWarning("[Setup] " + DefinitionPath + " not found - run CinliMahzen/Setup/Create Puzzle Definitions, then this again");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] Build Paintings Puzzle Prefabs: done (" + PuzzlePrefabPath + ", " + ReferencePrefabPath + ")");
        }

        private static GameObject BuildPuzzle(Material[] portraits, Material frameMat, Material markerMat)
        {
            var root = new GameObject("P_Puzzle_Paintings");
            var frames = new GameObject("Frames");
            frames.transform.SetParent(root.transform, false);
            int interactable = LayerMask.NameToLayer(InteractableLayer);

            var parts = new PuzzlePart[PortraitNames.Length];
            for (int i = 0; i < PortraitNames.Length; i++)
            {
                var frame = new GameObject("Frame_" + PortraitNames[i]);
                frame.transform.SetParent(frames.transform, false);
                frame.transform.localPosition = new Vector3((i - 1) * FrameSpacing, 0f, 0f);
                if (interactable >= 0)
                    frame.layer = interactable;
                var box = frame.AddComponent<BoxCollider>();
                box.size = new Vector3(FrameSize, FrameSize, Depth * 2f);

                Cube("Border", frame.transform, Vector3.zero, new Vector3(FrameSize, FrameSize, Depth), frameMat);
                Transform portrait = PortraitWithMarker(frame.transform, portraits[i], markerMat);

                var display = frame.AddComponent<PuzzlePartDisplay>();
                SetRef(display, "visual", portrait);
                var part = frame.AddComponent<PuzzlePart>();
                SetRef(part, "display", display);
                parts[i] = part;
            }

            // Lever: base on the wall, handle pivot at its top, tipping forward (+X rotation) when done.
            var lever = new GameObject("ConfirmLever");
            lever.transform.SetParent(root.transform, false);
            lever.transform.localPosition = new Vector3(1.5f * FrameSpacing + 0.3f, -0.35f, 0f);
            if (interactable >= 0)
                lever.layer = interactable;
            var leverBox = lever.AddComponent<BoxCollider>();
            leverBox.size = new Vector3(0.2f, 0.5f, 0.3f);
            leverBox.center = new Vector3(0f, 0.1f, 0.1f);
            Cube("Base", lever.transform, Vector3.zero, new Vector3(0.15f, 0.2f, 0.08f), frameMat);
            var handle = new GameObject("Handle");
            handle.transform.SetParent(lever.transform, false);
            handle.transform.localPosition = new Vector3(0f, 0f, 0.05f);
            Cube("Stick", handle.transform, new Vector3(0f, 0.18f, 0f), new Vector3(0.04f, 0.36f, 0.04f), markerMat);
            var confirm = lever.AddComponent<ConfirmLever>();
            SetRef(confirm, "handle", handle.transform);

            var view = root.AddComponent<PuzzleView>();
            var so = new SerializedObject(view);
            SerializedProperty partsProp = so.FindProperty("parts");
            partsProp.arraySize = parts.Length;
            for (int i = 0; i < parts.Length; i++)
                partsProp.GetArrayElementAtIndex(i).objectReferenceValue = parts[i];
            so.FindProperty("lever").objectReferenceValue = confirm;
            so.FindProperty("shakeRoot").objectReferenceValue = frames.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        private static GameObject BuildReference(Material[] portraits, Material markerMat, Material veilMat)
        {
            var root = new GameObject("P_PuzzleRef_Paintings");
            var content = new GameObject("Content");
            content.transform.SetParent(root.transform, false);

            var displays = new PuzzlePartDisplay[PortraitNames.Length];
            for (int i = 0; i < PortraitNames.Length; i++)
            {
                var holder = new GameObject("Ref_" + PortraitNames[i]);
                holder.transform.SetParent(content.transform, false);
                holder.transform.localPosition = new Vector3((i - 1) * FrameSpacing, 0f, 0f);
                Transform portrait = PortraitWithMarker(holder.transform, portraits[i], markerMat);
                var display = holder.AddComponent<PuzzlePartDisplay>();
                SetRef(display, "visual", portrait);
                displays[i] = display;
            }

            GameObject veil = Cube("Veil", root.transform, new Vector3(0f, 0f, 0.05f),
                new Vector3(FrameSpacing * PortraitNames.Length, FrameSize + 0.1f, 0.01f), veilMat);

            var reference = root.AddComponent<PuzzleReference>();
            var so = new SerializedObject(reference);
            SerializedProperty partsProp = so.FindProperty("parts");
            partsProp.arraySize = displays.Length;
            for (int i = 0; i < displays.Length; i++)
                partsProp.GetArrayElementAtIndex(i).objectReferenceValue = displays[i];
            so.FindProperty("content").objectReferenceValue = content;
            so.FindProperty("veil").objectReferenceValue = veil;
            so.ApplyModifiedPropertiesWithoutUndo();

            int spiritOnly = LayerMask.NameToLayer(SpiritOnlyLayer);
            if (spiritOnly >= 0)
                SetLayerRecursively(root, spiritOnly);
            else
                Debug.LogWarning("[Setup] Layer " + SpiritOnlyLayer + " missing - the reference would be visible to the human");
            return root;
        }

        /// <summary>A coloured portrait with a dark marker at its top edge (the "up" of the painting).</summary>
        private static Transform PortraitWithMarker(Transform parent, Material portraitMat, Material markerMat)
        {
            var pivot = new GameObject("Portrait");
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = new Vector3(0f, 0f, Depth * 0.5f + 0.005f);
            Cube("Canvas", pivot.transform, Vector3.zero, new Vector3(PortraitSize, PortraitSize, 0.01f), portraitMat);
            Cube("UpMarker", pivot.transform, new Vector3(0f, PortraitSize * 0.38f, 0.008f), new Vector3(PortraitSize * 0.25f, PortraitSize * 0.12f, 0.006f), markerMat);
            Cube("Eye", pivot.transform, new Vector3(-PortraitSize * 0.15f, PortraitSize * 0.12f, 0.008f), Vector3.one * 0.05f, markerMat);
            return pivot.transform;
        }

        private static GameObject Cube(string name, Transform parent, Vector3 localPos, Vector3 size, Material mat)
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

        private static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
                SetLayerRecursively(child.gameObject, layer);
        }

        private static Material GetMaterial(string name, Color color, bool transparent = false)
        {
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
                // URP Lit transparent setup.
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

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
