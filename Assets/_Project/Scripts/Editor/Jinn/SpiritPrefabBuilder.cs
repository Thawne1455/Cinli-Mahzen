using System.IO;
using CinliMahzen.Jinn;
using UnityEditor;
using UnityEngine;

namespace CinliMahzen.Editor
{
    /// <summary>
    /// Builds Assets/_Project/Prefabs/Jinn/P_Spirit.prefab (TODO 1.3, 1.6): root on layer SpiritPawn with <see cref="SpiritPawn"/> and
    /// <see cref="SpiritController"/> (flight, reference focus, Spirit Bump; no collider, no Rigidbody — spirits fly kinematically and
    /// collide with nothing, Teknik §2), an "Eyes" child for the first-person camera, and the
    /// two placeholder looks as nested prefabs, "Visual_Evil" (P_EvilJinnVisual, layer EvilJinnVisual) and "Visual_Good"
    /// (P_GoodJinnVisual, layer GoodJinnVisual), both inactive: SpiritPawn switches on the one matching its role. The look prefabs come from
    /// SandboxBBuilder (B0.2) and are built first when missing; they keep their own layers so camera culling treats them like before.
    /// Idempotent: an existing prefab is loaded and brought up to date in place; wrong or missing look children are replaced.
    /// MCP: execute_menu_item("CinliMahzen/Setup/Build Spirit Prefab").
    /// </summary>
    public static class SpiritPrefabBuilder
    {
        public const string PrefabPath = "Assets/_Project/Prefabs/Jinn/P_Spirit.prefab";

        private const string PrefabFolder = "Assets/_Project/Prefabs/Jinn";
        private const string SpiritPawnLayer = "SpiritPawn";
        private const string EyesName = "Eyes";
        private const string EvilVisualName = "Visual_Evil";
        private const string GoodVisualName = "Visual_Good";

        [MenuItem("CinliMahzen/Setup/Build Spirit Prefab")]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[SetupJinn] Stop play mode first");
                return;
            }
            int layer = LayerMask.NameToLayer(SpiritPawnLayer);
            if (layer < 0)
            {
                Debug.LogError("[SetupJinn] Layer '" + SpiritPawnLayer + "' is missing (Teknik section 2, layer 10)");
                return;
            }

            EnsureFolder(PrefabFolder);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(SandboxBBuilder.EvilPrefabPath) == null
                || AssetDatabase.LoadAssetAtPath<GameObject>(SandboxBBuilder.GoodPrefabPath) == null)
            {
                SandboxBBuilder.BuildVisuals();
                AssetDatabase.Refresh();
            }
            var evilSource = AssetDatabase.LoadAssetAtPath<GameObject>(SandboxBBuilder.EvilPrefabPath);
            var goodSource = AssetDatabase.LoadAssetAtPath<GameObject>(SandboxBBuilder.GoodPrefabPath);
            if (evilSource == null || goodSource == null)
            {
                Debug.LogError("[SetupJinn] Placeholder look prefabs missing — run CinliMahzen/Sandbox B/Build Jinn Placeholder Visuals");
                return;
            }

            bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null;
            GameObject root = exists ? PrefabUtility.LoadPrefabContents(PrefabPath) : new GameObject("P_Spirit");
            try
            {
                Configure(root, layer, evilSource, goodSource);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                if (exists)
                    PrefabUtility.UnloadPrefabContents(root);
                else
                    Object.DestroyImmediate(root);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[SetupJinn] " + (exists ? "Updated " : "Created ") + PrefabPath);
        }

        private static void Configure(GameObject root, int layer, GameObject evilSource, GameObject goodSource)
        {
            int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);
            if (removed > 0)
                Debug.Log("[SetupJinn] Removed " + removed + " component(s) with a missing script from " + root.name);

            // Spirits are kinematic and collide with nothing: drop any collider/rigidbody a hand edit might have added to the root.
            foreach (Collider c in root.GetComponents<Collider>())
                Object.DestroyImmediate(c);
            foreach (Rigidbody rb in root.GetComponents<Rigidbody>())
                Object.DestroyImmediate(rb);

            var pawn = GetOrAdd<SpiritPawn>(root);
            // Sibling of the pawn (RequireComponent): flight, reference focus and Spirit Bump. It has no references to wire — it finds the
            // pawn itself — and keeps its serialized feel values (look sensitivity, pitch limit) when the prefab is updated.
            GetOrAdd<SpiritController>(root);
            // TODO 1.7: follows the possessed object (hidden body, orbit camera) and sends the possess / unpossess requests.
            GetOrAdd<PossessionCameraBinder>(root);
            GetOrAdd<JinnPossessInput>(root);
            // TODO 2.6: hold E on a puzzle (mass sabotage) / later a lamp.
            GetOrAdd<SpiritInteractor>(root);

            Transform eyes = root.transform.Find(EyesName);
            if (eyes == null)
            {
                var eyesGo = new GameObject(EyesName);
                eyesGo.transform.SetParent(root.transform, false);
                eyes = eyesGo.transform;
            }
            eyes.localPosition = Vector3.zero;
            eyes.localRotation = Quaternion.identity;
            eyes.localScale = Vector3.one;

            GameObject evil = EnsureVisual(root, EvilVisualName, evilSource);
            GameObject good = EnsureVisual(root, GoodVisualName, goodSource);

            var so = new SerializedObject(pawn);
            so.FindProperty("eyes").objectReferenceValue = eyes;
            so.FindProperty("evilVisual").objectReferenceValue = evil;
            so.FindProperty("goodVisual").objectReferenceValue = good;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Only the pawn itself sits on SpiritPawn; the look children keep EvilJinnVisual / GoodJinnVisual.
            root.layer = layer;
            eyes.gameObject.layer = layer;
        }

        private static GameObject EnsureVisual(GameObject root, string childName, GameObject source)
        {
            Transform existing = root.transform.Find(childName);
            if (existing != null)
            {
                if (PrefabUtility.GetCorrespondingObjectFromSource(existing.gameObject) == source)
                    return ResetLook(existing.gameObject);
                Object.DestroyImmediate(existing.gameObject);
            }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source, root.transform);
            instance.name = childName;
            return ResetLook(instance);
        }

        private static GameObject ResetLook(GameObject look)
        {
            look.transform.localPosition = Vector3.zero;
            look.transform.localRotation = Quaternion.identity;
            look.SetActive(false);
            return look;
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T existing = go.GetComponent<T>();
            return existing != null ? existing : go.AddComponent<T>();
        }

        private static void EnsureFolder(string assetFolder)
        {
            if (Directory.Exists(assetFolder))
                return;
            Directory.CreateDirectory(assetFolder);
            AssetDatabase.Refresh();
        }
    }
}
