using UnityEditor;
using UnityEngine;

namespace CinliMahzen.Editor
{
    /// <summary>
    /// Adds an invisible "RampCollider" (an inclined BoxCollider) to Env_Stairs and Env_StairsWide (TODO 1.1: stairs = ramp
    /// collider, Teknik section 5.2) so the CharacterController glides up and down instead of snagging on the 0.5 m steps.
    /// The original step MeshCollider stays (side walls, stringers); the ramp's top surface lies on the line through the
    /// tread nosings, so every tread is at or below it and the controller only ever touches the ramp.
    /// Idempotent: an existing RampCollider child is rebuilt. MCP: execute_menu_item("CinliMahzen/Setup/Add Ramp Colliders To Stairs").
    /// </summary>
    public static class StairsRampBuilder
    {
        private const string EnvironmentFolder = "Assets/_Project/Prefabs/Environment/";
        private const string RampName = "RampCollider";
        private const string EnvironmentLayer = "Environment";

        // Geometry measured from stairs.fbx / stairs_wide.fbx (tread polygons, TODO 1.1): the walkway rises 8 steps of 0.5 m
        // over 4 m of depth; each tread is 0.4 m deep with a 0.1 m riser, so the nosing line is a 45 degree incline
        // (height = distance from the bottom edge + 0.4). The 0.75 m on each side of the walkway is the side wall.
        private const float TopTreadHeight = 4f;
        private const float TopTreadDepth = 0.4f;
        private const float NosingSlope = 1f;
        private const float SideWallThickness = 0.75f;

        // Ramp slab.
        private const float RampThickness = 0.3f;
        // The slab continues this far (m, along the slope) below the floor so its lower end is buried in the floor.
        private const float RampBuriedOverrun = 0.4f;

        // The KayKit stairs rise toward -Z in mesh space: measured 2026-10-16 by raycasting the step MeshCollider of the
        // Sandbox instance (tread y = 4.0 at the min-Z end, 0.5 at the max-Z end). The mesh is not CPU-readable, so this
        // cannot be detected at build time; re-measure if the FBX import settings change.
        private const int AscentSign = -1;

        [MenuItem("CinliMahzen/Setup/Add Ramp Colliders To Stairs")]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[SetupPlayer] Stop play mode first");
                return;
            }
            int done = 0;
            done += BuildOne(EnvironmentFolder + "Env_Stairs.prefab") ? 1 : 0;
            done += BuildOne(EnvironmentFolder + "Env_StairsWide.prefab") ? 1 : 0;
            AssetDatabase.SaveAssets();
            Debug.Log("[SetupPlayer] Ramp colliders ready on " + done + "/2 stair prefabs");
        }

        private static bool BuildOne(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                Debug.LogError("[SetupPlayer] Missing prefab " + path);
                return false;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Transform old = root.transform.Find(RampName);
                if (old != null)
                    Object.DestroyImmediate(old.gameObject);

                MeshCollider steps = root.GetComponentInChildren<MeshCollider>(true);
                if (steps == null || steps.sharedMesh == null)
                {
                    Debug.LogError("[SetupPlayer] " + path + ": no step MeshCollider with a mesh found");
                    return false;
                }

                Mesh mesh = steps.sharedMesh;
                Bounds bounds = mesh.bounds;
                int sign = AscentSign;

                // Model-local ramp line: s = distance from the bottom edge along the ascent direction.
                float bottomZ = sign > 0 ? bounds.min.z : bounds.max.z;
                float baseY = bounds.min.y;
                float topS = bounds.size.z - TopTreadDepth;
                float groundS = topS - TopTreadHeight / NosingSlope;
                Vector3 topModel = new Vector3(bounds.center.x, baseY + TopTreadHeight, bottomZ + sign * topS);
                Vector3 groundModel = new Vector3(bounds.center.x, baseY, bottomZ + sign * groundS);

                Transform model = steps.transform;
                Vector3 top = root.transform.InverseTransformPoint(model.TransformPoint(topModel));
                Vector3 ground = root.transform.InverseTransformPoint(model.TransformPoint(groundModel));

                Vector3 slopeDir = (top - ground).normalized;
                Vector3 lower = ground - slopeDir * RampBuriedOverrun;
                Vector3 lateral = Vector3.Cross(Vector3.up, slopeDir).normalized;
                Vector3 normal = Vector3.Cross(slopeDir, lateral).normalized;

                float width = bounds.size.x - 2f * SideWallThickness;
                float length = Vector3.Distance(lower, top);

                var ramp = new GameObject(RampName);
                ramp.transform.SetParent(root.transform, false);
                ramp.transform.localPosition = (lower + top) * 0.5f - normal * (RampThickness * 0.5f);
                ramp.transform.localRotation = Quaternion.LookRotation(slopeDir, normal);
                ramp.layer = LayerOrFallback(root.layer);

                var box = ramp.AddComponent<BoxCollider>();
                box.center = Vector3.zero;
                box.size = new Vector3(width, RampThickness, length);

                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log("[SetupPlayer] " + root.name + ": RampCollider width " + width.ToString("0.00") + " m, length "
                    + length.ToString("0.00") + " m, slope " + (Mathf.Asin(Mathf.Clamp(slopeDir.y, -1f, 1f)) * Mathf.Rad2Deg).ToString("0.0")
                    + " deg, ascends toward " + (sign > 0 ? "+Z" : "-Z") + " (mesh space)");
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static int LayerOrFallback(int fallback)
        {
            int layer = LayerMask.NameToLayer(EnvironmentLayer);
            return layer >= 0 ? layer : fallback;
        }
    }
}
