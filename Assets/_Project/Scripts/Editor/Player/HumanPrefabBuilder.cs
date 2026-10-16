using System.IO;
using CinliMahzen.Player;
using UnityEditor;
using UnityEngine;

namespace CinliMahzen.Editor
{
    /// <summary>
    /// Builds Assets/_Project/Prefabs/Player/P_Human.prefab (TODO 1.1): root on layer HumanPawn with CharacterController,
    /// HumanController and Interactor (TODO 1.4), a capsule "Visual" (collider removed) and a "Head" child. The camera is not
    /// part of the prefab: the scene's CameraRig (TODO 1.2) follows the Head once the pawn is local.
    /// Idempotent: an existing prefab is loaded and brought up to date in place (component ids stay stable, so scene
    /// instances keep their overrides); a missing one is created. Components whose script no longer exists
    /// (the retired TempFpsCameraAttach) are removed.
    /// MCP: execute_menu_item("CinliMahzen/Setup/Build Human Prefab").
    /// </summary>
    public static class HumanPrefabBuilder
    {
        public const string PrefabPath = "Assets/_Project/Prefabs/Player/P_Human.prefab";

        private const string PrefabFolder = "Assets/_Project/Prefabs/Player";
        private const string HumanPawnLayer = "HumanPawn";

        // Prefab data: body dimensions (m). Unity's capsule primitive is 2 m high with 0.5 m radius.
        private const float BodyHeight = 1.8f;
        private const float BodyRadius = 0.35f;
        private const float HeadHeight = 1.65f;
        private const float CapsulePrimitiveHeight = 2f;
        private const float CapsulePrimitiveRadius = 0.5f;

        [MenuItem("CinliMahzen/Setup/Build Human Prefab")]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[SetupPlayer] Stop play mode first");
                return;
            }
            int layer = LayerMask.NameToLayer(HumanPawnLayer);
            if (layer < 0)
            {
                Debug.LogError("[SetupPlayer] Layer '" + HumanPawnLayer + "' is missing (Teknik section 2, layer 8)");
                return;
            }

            EnsureFolder(PrefabFolder);

            bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null;
            GameObject root = exists ? PrefabUtility.LoadPrefabContents(PrefabPath) : new GameObject("P_Human");
            try
            {
                Configure(root, layer);
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
            Debug.Log("[SetupPlayer] " + (exists ? "Updated " : "Created ") + PrefabPath);
        }

        private static void Configure(GameObject root, int layer)
        {
            var controller = GetOrAdd<CharacterController>(root);
            controller.height = BodyHeight;
            controller.radius = BodyRadius;
            controller.center = new Vector3(0f, BodyHeight * 0.5f, 0f);

            int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);
            if (removed > 0)
                Debug.Log("[SetupPlayer] Removed " + removed + " component(s) with a missing script from " + root.name);

            var human = GetOrAdd<HumanController>(root);
            GetOrAdd<Interactor>(root);

            Transform visual = root.transform.Find("Visual");
            if (visual == null)
            {
                GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                capsule.name = "Visual";
                capsule.transform.SetParent(root.transform, false);
                visual = capsule.transform;
            }
            // The CharacterController is the only collider of the pawn.
            foreach (Collider c in visual.GetComponents<Collider>())
                Object.DestroyImmediate(c);
            visual.localPosition = new Vector3(0f, BodyHeight * 0.5f, 0f);
            visual.localRotation = Quaternion.identity;
            visual.localScale = new Vector3(
                BodyRadius / CapsulePrimitiveRadius,
                BodyHeight / CapsulePrimitiveHeight,
                BodyRadius / CapsulePrimitiveRadius);

            Transform head = root.transform.Find("Head");
            if (head == null)
            {
                var headGo = new GameObject("Head");
                headGo.transform.SetParent(root.transform, false);
                head = headGo.transform;
            }
            head.localPosition = new Vector3(0f, HeadHeight, 0f);
            head.localRotation = Quaternion.identity;
            head.localScale = Vector3.one;

            var so = new SerializedObject(human);
            so.FindProperty("head").objectReferenceValue = head;
            so.ApplyModifiedPropertiesWithoutUndo();

            SetLayerRecursively(root, layer);
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T existing = go.GetComponent<T>();
            return existing != null ? existing : go.AddComponent<T>();
        }

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
                SetLayerRecursively(child.gameObject, layer);
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
