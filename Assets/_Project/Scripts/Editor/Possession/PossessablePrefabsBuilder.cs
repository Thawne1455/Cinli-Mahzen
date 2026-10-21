using System.Collections.Generic;
using CinliMahzen.Possession;
using UnityEditor;
using UnityEngine;

namespace CinliMahzen.Editor
{
    /// <summary>
    /// TODO 1.8 / 4.1: <c>CinliMahzen/Setup/Build Possessable Prefabs</c> — one <c>P_Poss_&lt;Name&gt;</c> per <c>PD_*</c> asset (lamps and quest items
    /// excepted: lamps sit under LampMarkers, quest items are spawned by Objectives). Root on the Possessable layer with a box collider fitted to the
    /// visual and <see cref="Possessable"/>; child "Visual" is the matching KayKit prop (colliders stripped) or a coloured graybox primitive.
    /// Rebuilding overwrites the prefabs.
    /// </summary>
    public static class PossessablePrefabsBuilder
    {
        public const string PrefabFolder = "Assets/_Project/Prefabs/Possessables";
        private const string DefinitionFolder = "Assets/_Project/ScriptableObjects/Possessables";
        private const string PropFolder = "Assets/_Project/Prefabs/Props";
        private const string MaterialFolder = "Assets/_Project/Art/Materials/Possessables";
        private const string LayerName = "Possessable";

        private readonly struct Spec
        {
            public readonly string Definition;
            public readonly string Prop;
            public readonly Vector3 PrimitiveSize;
            public readonly Color Color;

            public Spec(string definition, string prop, Vector3 primitiveSize, Color color)
            {
                Definition = definition;
                Prop = prop;
                PrimitiveSize = primitiveSize;
                Color = color;
            }
        }

        private static readonly Spec[] Specs =
        {
            new Spec("Chair", "Prop_chair", Vector3.zero, Color.white),
            new Spec("Stool", "Prop_stool", Vector3.zero, Color.white),
            new Spec("Barrel", "Prop_barrel_small", Vector3.zero, Color.white),
            new Spec("Chest", "Prop_chest", Vector3.zero, Color.white),
            new Spec("Table", "Prop_table_small", Vector3.zero, Color.white),
            new Spec("Shelf", "Prop_shelf_small", Vector3.zero, Color.white),
            new Spec("Bottle", "Prop_bottle_A_green", Vector3.zero, Color.white),
            new Spec("Candle", "Prop_candle_lit", Vector3.zero, Color.white),
            new Spec("Plate", "Prop_plate_stack", Vector3.zero, Color.white),
            new Spec("Slipper", null, new Vector3(0.12f, 0.06f, 0.28f), new Color(0.75f, 0.2f, 0.25f)),
            new Spec("Radio", null, new Vector3(0.4f, 0.3f, 0.22f), new Color(0.45f, 0.3f, 0.15f)),
            new Spec("Cologne", null, new Vector3(0.1f, 0.22f, 0.1f), new Color(0.95f, 0.9f, 0.3f)),
        };

        [MenuItem("CinliMahzen/Setup/Build Possessable Prefabs")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[SetupPossessables] Stop play mode first");
                return;
            }
            EnsureFolder(PrefabFolder);
            EnsureFolder(MaterialFolder);
            int layer = LayerMask.NameToLayer(LayerName);
            int built = 0;
            var problems = new List<string>();
            foreach (Spec spec in Specs)
            {
                var definition = AssetDatabase.LoadAssetAtPath<PossessableDefinition>(DefinitionFolder + "/PD_" + spec.Definition + ".asset");
                if (definition == null)
                {
                    problems.Add("PD_" + spec.Definition + " missing");
                    continue;
                }
                GameObject root = BuildOne(spec, definition, layer, problems);
                if (root == null)
                    continue;
                PrefabUtility.SaveAsPrefabAsset(root, PathOf(spec.Definition));
                Object.DestroyImmediate(root);
                built++;
            }
            AssetDatabase.SaveAssets();
            foreach (string problem in problems)
                Debug.LogWarning("[SetupPossessables] " + problem);
            Debug.Log("[SetupPossessables] Build Possessable Prefabs: " + built + " prefabs in " + PrefabFolder);
        }

        /// <summary>Prefab path of <c>P_Poss_&lt;name&gt;</c> (name = the PD_ asset without its prefix).</summary>
        public static string PathOf(string name)
        {
            return PrefabFolder + "/P_Poss_" + name + ".prefab";
        }

        private static GameObject BuildOne(Spec spec, PossessableDefinition definition, int layer, List<string> problems)
        {
            var root = new GameObject("P_Poss_" + spec.Definition);
            GameObject visual = null;
            if (!string.IsNullOrEmpty(spec.Prop))
            {
                var prop = AssetDatabase.LoadAssetAtPath<GameObject>(PropFolder + "/" + spec.Prop + ".prefab");
                if (prop != null)
                {
                    visual = (GameObject)PrefabUtility.InstantiatePrefab(prop);
                    foreach (Collider c in visual.GetComponentsInChildren<Collider>(true))
                        Object.DestroyImmediate(c);
                }
                else
                {
                    problems.Add(spec.Prop + " missing - graybox cube for " + spec.Definition);
                }
            }
            if (visual == null)
            {
                visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.DestroyImmediate(visual.GetComponent<Collider>());
                Vector3 size = spec.PrimitiveSize == Vector3.zero ? new Vector3(0.5f, 0.5f, 0.5f) : spec.PrimitiveSize;
                visual.transform.localScale = size;
                visual.transform.localPosition = new Vector3(0f, size.y * 0.5f, 0f);
                visual.GetComponent<Renderer>().sharedMaterial = MaterialOf(spec);
            }
            visual.name = "Visual";
            visual.transform.SetParent(root.transform, false);

            Bounds bounds = LocalBounds(root.transform);
            var box = root.AddComponent<BoxCollider>();
            box.center = bounds.center;
            box.size = Vector3.Max(bounds.size, new Vector3(0.05f, 0.05f, 0.05f));

            if (layer >= 0)
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                    t.gameObject.layer = layer;
            }

            var possessable = root.AddComponent<Possessable>();
            var so = new SerializedObject(possessable);
            so.FindProperty("definition").objectReferenceValue = definition;
            so.FindProperty("visual").objectReferenceValue = visual.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        /// <summary>Renderer bounds of everything under <paramref name="root"/>, in root space (root sits at the origin while building).</summary>
        private static Bounds LocalBounds(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return new Bounds(Vector3.up * 0.25f, Vector3.one * 0.5f);
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static Material MaterialOf(Spec spec)
        {
            string path = MaterialFolder + "/M_Poss_" + spec.Definition + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
                return material;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            material = new Material(shader != null ? shader : Shader.Find("Standard"));
            material.color = spec.Color;
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
        }
    }
}
