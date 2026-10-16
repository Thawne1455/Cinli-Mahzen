using System.Collections.Generic;
using CinliMahzen.World.Graybox;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CinliMahzen.Editor
{
    /// <summary>
    /// TODO 1.9: turns the fixed layout in <see cref="GrayboxLayout.Default"/> into the <c>P_House</c> prefab (garden,
    /// ground floor, first floor, stairs, props) from the KayKit <c>Env_*</c> prefabs. There is no generator here
    /// (CLAUDE.md section 5): all positions come from the hand-authored data; this class only places prefabs.
    /// Walls come from <see cref="GrayboxWallPlanner"/>, so a shared wall is built once.
    ///
    /// Door leaves of stage gates and the front door are drawn but non-blocking in v0 (see DoorLeafNameSuffix).
    ///
    /// Idempotent: Build Graybox overwrites <c>P_House.prefab</c> (the asset keeps its GUID, so scene instances stay
    /// linked). No markers yet (TODO 2.2); the house is recognised by hierarchy names only for now.
    ///
    /// Hierarchy: P_House / Garden / Room_X / {Floor, Walls}, Garden/Posts, Garden/Ground;
    /// P_House / Floor0 | Floor1 / Room_X / {Floor, Walls}, Floor0 | Floor1 / Posts; P_House / Stairs; P_House / Props.
    /// </summary>
    public static class HouseGrayboxBuilder
    {
        // ---- paths and names ---------------------------------------------------------------------------
        private const string MenuRoot = "CinliMahzen/House/";
        private const string EnvironmentFolder = "Assets/_Project/Prefabs/Environment";
        private const string WorldFolder = "Assets/_Project/Prefabs/World";
        private const string HousePrefabPath = WorldFolder + "/P_House.prefab";
        private const string MaterialFolder = "Assets/_Project/Art/Materials";
        private const string LitShaderName = "Universal Render Pipeline/Lit";
        private const string BaseColorProperty = "_BaseColor";
        private const string EnvironmentLayerName = "Environment";
        private const int EnvironmentLayerFallback = 6;

        private const string HouseRootName = "P_House";
        private const string GardenGroupName = "Garden";
        private const string FloorGroupPrefix = "Floor";
        private const string StairsGroupName = "Stairs";
        private const string PropsGroupName = "Props";
        private const string RoomPrefix = "Room_";
        private const string RoomFloorGroupName = "Floor";
        private const string RoomWallsGroupName = "Walls";
        private const string PostsGroupName = "Posts";
        private const string GroundName = "Ground";

        // ---- measurements (docs/04_Asset_Eslestirme.md sections 1-2) --------------------------------------
        private const float CellSize = GrayboxLayout.CellSize;
        private const float FloorHeight = GrayboxLayout.FloorHeight;

        /// <summary>floor_tile_large is a 4 x 0.15 x 4 slab centred on its pivot; its top face is 0.05 above the pivot.</summary>
        private const float FloorTopOffset = 0.05f;

        /// <summary>Env_Wall: 4 m long (local X), 4 m high, 1 m thick (local Z), pivot at the segment centre on the ground.</summary>
        private const float WallHeight = 4f;
        private const float WallThickness = 1f;

        /// <summary>Fences and railings are Env_Wall squashed in height and thickness (Env_WallHalf is only half as long, not half as high).</summary>
        private const float FenceHeight = 1.2f;
        private const float FenceThickness = 0.25f;
        private const float RailingHeight = 1f;
        private const float RailingThickness = 0.2f;

        /// <summary>A wall that runs along world Z is the X-running prefab turned by this yaw.</summary>
        private const float WallYawAlongZ = 90f;

        /// <summary>Only the garden stage (stage 0) is grouped under "Garden"; the other rooms go under their floor.</summary>
        private const int GardenStage = 0;

        /// <summary>
        /// In Env_WallDoorwayDoor the door leaf is the child mesh "wall_doorway_door" and it comes with its own closed
        /// MeshCollider. Graybox v0 has no way to open doors yet (locks arrive with TODO 2.7), so the leaf is drawn but
        /// its collider is switched off; otherwise nobody could enter the house or reach the stairs.
        /// </summary>
        private const string DoorLeafNameSuffix = "_door";

        /// <summary>The stairs model may sit this far (m) from the stair cell centre before the builder warns.</summary>
        private const float StairsCenterTolerance = 0.25f;

        // ---- ground slab under the garden and the tree line ------------------------------------------------
        private const int GroundMarginCells = 2;
        private const float GroundThickness = 0.5f;

        // ---- primitive props (graybox dressing; real models come in the polish phase) ------------------------
        private const float TreeTrunkHeight = 3.2f;
        private const float TreeTrunkDiameter = 0.6f;
        private const float TreeCrownDiameter = 3.4f;
        private const float TreeCrownCenterY = 4.1f;

        private const float WellRingDiameter = 1.8f;
        private const float WellRingHeight = 0.9f;
        private const float WellWaterDiameter = 1.2f;
        private const float WellWaterThickness = 0.04f;
        private const float WellPostHeight = 2.2f;
        private const float WellPostThickness = 0.15f;
        private const float WellPostOffsetX = 0.85f;
        private const float WellBeamLength = 1.9f;

        private static readonly Vector3 WoodpileBaseSize = new Vector3(1.8f, 0.9f, 1.1f);
        private static readonly Vector3 WoodpileTopSize = new Vector3(1.5f, 0.45f, 0.9f);

        private const float PotDiameter = 0.5f;
        private const float PotHeight = 0.5f;

        /// <summary>Unity's built-in Cylinder is 2 m tall and 1 m wide at scale 1.</summary>
        private const float CylinderBaseHeight = 2f;

        private static readonly Color WoodColor = new Color(0.45f, 0.3f, 0.16f);
        private static readonly Color LeafColor = new Color(0.2f, 0.45f, 0.2f);
        private static readonly Color StoneColor = new Color(0.52f, 0.52f, 0.55f);
        private static readonly Color ClayColor = new Color(0.7f, 0.4f, 0.25f);
        private static readonly Color WaterColor = new Color(0.05f, 0.1f, 0.15f);
        private static readonly Color GroundColor = new Color(0.18f, 0.22f, 0.12f);

        // ---- the prefab table: one place that maps layout pieces to prefabs ------------------------------
        private readonly struct PieceSpec
        {
            public readonly string Prefab;
            public readonly Vector3 Scale;

            /// <summary>The prefab carries a closed door leaf whose collider is switched off in graybox v0.</summary>
            public readonly bool HasDoorLeaf;

            public PieceSpec(string prefab, Vector3 scale, bool hasDoorLeaf = false)
            {
                Prefab = prefab;
                Scale = scale;
                HasDoorLeaf = hasDoorLeaf;
            }
        }

        private static readonly PieceSpec FloorStoneSpec = new PieceSpec("Env_Floor", Vector3.one);
        private static readonly PieceSpec FloorWoodSpec = new PieceSpec("Env_FloorWood", Vector3.one);
        private static readonly PieceSpec WallSpec = new PieceSpec("Env_Wall", Vector3.one);
        private static readonly PieceSpec FenceSpec = new PieceSpec("Env_Wall",
            new Vector3(1f, FenceHeight / WallHeight, FenceThickness / WallThickness));
        private static readonly PieceSpec RailingSpec = new PieceSpec("Env_Wall",
            new Vector3(1f, RailingHeight / WallHeight, RailingThickness / WallThickness));
        private static readonly PieceSpec DoorwaySpec = new PieceSpec("Env_WallDoorway", Vector3.one);
        private static readonly PieceSpec DoorwayWithDoorSpec = new PieceSpec("Env_WallDoorwayDoor", Vector3.one, true);
        private static readonly PieceSpec PillarSpec = new PieceSpec("Env_Pillar", Vector3.one);
        private static readonly PieceSpec FencePostSpec = new PieceSpec("Env_Column", Vector3.one);
        private static readonly PieceSpec StairsSpec = new PieceSpec("Env_Stairs", Vector3.one);

        private static PieceSpec FloorSpec(GrayboxFloorStyle style)
        {
            return style == GrayboxFloorStyle.Wood ? FloorWoodSpec : FloorStoneSpec;
        }

        private static PieceSpec WallPieceSpec(GrayboxWallSegment segment)
        {
            switch (segment.Kind)
            {
                case GrayboxWallKind.Fence:
                    return FenceSpec;
                case GrayboxWallKind.Railing:
                    return RailingSpec;
                case GrayboxWallKind.Door:
                    // Open and plain doors are just the frame; gates and the front door get the door leaf.
                    return segment.Door.Kind == GrayboxDoorKind.StageGate || segment.Door.Kind == GrayboxDoorKind.FrontDoor
                        ? DoorwayWithDoorSpec
                        : DoorwaySpec;
                default:
                    return WallSpec;
            }
        }

        private static PieceSpec PostSpec(GrayboxPostKind kind)
        {
            return kind == GrayboxPostKind.Pillar ? PillarSpec : FencePostSpec;
        }

        // ------------------------------------------------------------------------------------------------
        // menu commands
        // ------------------------------------------------------------------------------------------------

        [MenuItem(MenuRoot + "Build Graybox")]
        public static void BuildGraybox()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[House] Stop Play mode before building the graybox house.");
                return;
            }

            GrayboxLayout layout = GrayboxLayout.Default;
            List<string> errors = GrayboxLayoutValidator.Validate(layout);
            if (errors.Count > 0)
            {
                Debug.LogError("[House] GrayboxLayout.Default is invalid, nothing was built:\n  " + string.Join("\n  ", errors));
                return;
            }

            EnsureFolder(WorldFolder);
            EnsureFolder(MaterialFolder);

            // Build in a throw-away scene so the open scene is never touched or dirtied.
            Scene previous = SceneManager.GetActiveScene();
            Scene temp = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(temp);
            GameObject root = null;
            try
            {
                var ctx = new BuildContext(ResolveEnvironmentLayer());
                root = BuildHouse(ctx, layout);
                if (ctx.Failed)
                {
                    Debug.LogError("[House] Build aborted (missing prefabs, see above). P_House.prefab was not touched.");
                    return;
                }

                bool saved;
                PrefabUtility.SaveAsPrefabAsset(root, HousePrefabPath, out saved);
                if (!saved)
                {
                    Debug.LogError("[House] Could not save " + HousePrefabPath);
                    return;
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log(ctx.Summary(HousePrefabPath));
            }
            finally
            {
                if (root != null)
                {
                    Object.DestroyImmediate(root);
                }
                if (previous.IsValid() && previous.isLoaded)
                {
                    SceneManager.SetActiveScene(previous);
                }
                EditorSceneManager.CloseScene(temp, true);
            }
        }

        [MenuItem(MenuRoot + "Place House In Open Scene")]
        public static void PlaceHouseInOpenScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[House] Stop Play mode before placing the house.");
                return;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HousePrefabPath);
            if (prefab == null)
            {
                Debug.LogError("[House] " + HousePrefabPath + " does not exist yet. Run CinliMahzen/House/Build Graybox first.");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError("[House] There is no open scene to place the house in.");
                return;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (PrefabUtility.IsAnyPrefabInstanceRoot(roots[i])
                    && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(roots[i]) == HousePrefabPath)
                {
                    Selection.activeGameObject = roots[i];
                    Debug.Log("[House] Scene '" + scene.name + "' already contains " + HouseRootName + "; nothing added.");
                    return;
                }
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.transform.position = Vector3.zero;
            instance.transform.rotation = Quaternion.identity;
            Undo.RegisterCreatedObjectUndo(instance, "Place House");
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = instance;
            Debug.Log("[House] Placed " + HouseRootName + " at (0,0,0) in scene '" + scene.name + "'. Save the scene.");
        }

        [MenuItem(MenuRoot + "Print Graybox ASCII Map")]
        public static void PrintGrayboxAsciiMap()
        {
            GrayboxLayout layout = GrayboxLayout.Default;
            for (int floor = 0; floor < GrayboxLayout.FloorCount; floor++)
            {
                Debug.Log("[House] Graybox floor " + floor + " (north up)\n" + GrayboxAsciiMap.Render(layout, floor));
            }
            Debug.Log("[House] Graybox legend\n" + GrayboxAsciiMap.Legend(layout));
        }

        // ------------------------------------------------------------------------------------------------
        // building
        // ------------------------------------------------------------------------------------------------

        private static GameObject BuildHouse(BuildContext ctx, GrayboxLayout layout)
        {
            Transform root = Group(ctx, null, HouseRootName);
            Transform garden = Group(ctx, root, GardenGroupName);
            var floorGroups = new Transform[GrayboxLayout.FloorCount];
            for (int floor = 0; floor < floorGroups.Length; floor++)
            {
                floorGroups[floor] = Group(ctx, root, FloorGroupPrefix + floor);
            }
            Transform stairsGroup = Group(ctx, root, StairsGroupName);
            Transform propsGroup = Group(ctx, root, PropsGroupName);

            // Rooms: floor tiles now, walls are filled in below from the planner (a shared wall belongs to one room).
            var wallGroups = new Dictionary<string, Transform>();
            for (int r = 0; r < layout.Rooms.Count; r++)
            {
                GrayboxRoom room = layout.Rooms[r];
                Transform parent = room.Stage == GardenStage ? garden : floorGroups[room.Floor];
                Transform roomGroup = Group(ctx, parent, RoomPrefix + room.Name);
                Transform floorGroup = Group(ctx, roomGroup, RoomFloorGroupName);
                wallGroups.Add(room.Id, Group(ctx, roomGroup, RoomWallsGroupName));
                ctx.Rooms++;

                ctx.Cells.Clear();
                room.AppendCellsTo(ctx.Cells);
                for (int c = 0; c < ctx.Cells.Count; c++)
                {
                    GrayboxCell cell = ctx.Cells[c];
                    Vector3 position = new Vector3((cell.X + 0.5f) * CellSize, room.Floor * FloorHeight, (cell.Z + 0.5f) * CellSize);
                    if (Place(ctx, FloorSpec(room.FloorStyle), "Floor_" + cell.X + "_" + cell.Z, floorGroup, position, 0f) != null)
                    {
                        ctx.FloorTiles++;
                    }
                }
            }

            // Walls, doors, fences, railings.
            List<GrayboxWallSegment> segments = GrayboxWallPlanner.PlanWalls(layout);
            for (int s = 0; s < segments.Count; s++)
            {
                GrayboxWallSegment segment = segments[s];
                GrayboxEdge edge = segment.Edge;
                Vector3 position = new Vector3(edge.CenterCellX * CellSize, edge.Floor * FloorHeight, edge.CenterCellZ * CellSize);
                float yaw = edge.RunsAlongX ? 0f : WallYawAlongZ;
                PieceSpec spec = WallPieceSpec(segment);
                GameObject piece = Place(ctx, spec, PieceName(segment), wallGroups[segment.OwnerRoomId], position, yaw);
                if (piece != null)
                {
                    ctx.Pieces[(int)segment.Kind]++;
                    if (spec.HasDoorLeaf)
                    {
                        DisableDoorLeafColliders(piece);
                    }
                }
            }

            // Corner posts. Fence posts on the ground level belong to the garden, everything else to its floor.
            var postGroups = new Transform[GrayboxLayout.FloorCount];
            for (int floor = 0; floor < postGroups.Length; floor++)
            {
                postGroups[floor] = Group(ctx, floorGroups[floor], PostsGroupName);
            }
            Transform gardenPosts = Group(ctx, garden, PostsGroupName);
            List<GrayboxPost> posts = GrayboxWallPlanner.PlanPosts(segments);
            for (int p = 0; p < posts.Count; p++)
            {
                GrayboxPost post = posts[p];
                Transform parent = post.Kind == GrayboxPostKind.FencePost && post.Floor == GrayboxLayout.GroundFloor
                    ? gardenPosts
                    : postGroups[post.Floor];
                Vector3 position = new Vector3(post.VertexX * CellSize, post.Floor * FloorHeight, post.VertexZ * CellSize);
                if (Place(ctx, PostSpec(post.Kind), post.Kind + "_" + post.VertexX + "_" + post.VertexZ, parent, position, 0f) != null)
                {
                    ctx.Posts++;
                }
            }

            // Stairs: unscaled Env_Stairs, pivot (= top step) on the arrival border, climbing towards Direction.
            for (int i = 0; i < layout.Stairs.Count; i++)
            {
                PlaceStairs(ctx, layout.Stairs[i], stairsGroup);
            }

            // Props and the ground slab.
            BuildGround(ctx, layout, garden);
            for (int i = 0; i < layout.Props.Count; i++)
            {
                BuildProp(ctx, layout.Props[i], i, propsGroup);
            }

            return root.gameObject;
        }

        private static void DisableDoorLeafColliders(GameObject piece)
        {
            int disabled = 0;
            Collider[] colliders = piece.GetComponentsInChildren<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].gameObject.name.EndsWith(DoorLeafNameSuffix, System.StringComparison.Ordinal))
                {
                    colliders[i].enabled = false;
                    disabled++;
                }
            }
            if (disabled == 0)
            {
                Debug.LogWarning("[House] No door leaf collider (child named '*" + DoorLeafNameSuffix + "') found on " + piece.name
                    + "; this gate may block the doorway.");
            }
        }

        private static string PieceName(GrayboxWallSegment segment)
        {
            GrayboxEdge edge = segment.Edge;
            string label = segment.Kind == GrayboxWallKind.Door ? segment.Door.Kind.ToString() : segment.Kind.ToString();
            return label + "_" + edge.X + "_" + edge.Z + (edge.RunsAlongX ? "_N" : "_E");
        }

        private static void PlaceStairs(BuildContext ctx, GrayboxStairs stairs, Transform parent)
        {
            GrayboxEdge top = stairs.ArrivalEdge;
            Vector3 position = new Vector3(top.CenterCellX * CellSize, FloorTopOffset, top.CenterCellZ * CellSize);
            GameObject instance = Place(ctx, StairsSpec, "Stairs_" + stairs.Start.X + "_" + stairs.Start.Z + "_" + stairs.Direction,
                parent, position, stairs.ModelYawDegrees);
            if (instance == null)
            {
                return;
            }
            ctx.Stairs++;

            if (instance.GetComponentInChildren<BoxCollider>() == null)
            {
                Debug.LogWarning("[House] Env_Stairs has no ramp BoxCollider (RampCollider). Without it the human has to climb the 0.5 m "
                    + "steps; merge the Env_Stairs.prefab change that adds the 45 degree ramp.");
            }

            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return;
            }
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            GrayboxCell last = stairs.LastCell;
            float expectedX = ((stairs.Start.X + last.X) * 0.5f + 0.5f) * CellSize;
            float expectedZ = ((stairs.Start.Z + last.Z) * 0.5f + 0.5f) * CellSize;
            if (Mathf.Abs(bounds.center.x - expectedX) > StairsCenterTolerance || Mathf.Abs(bounds.center.z - expectedZ) > StairsCenterTolerance)
            {
                Debug.LogWarning("[House] Env_Stairs bounds centre (" + bounds.center.x.ToString("0.00") + ", " + bounds.center.z.ToString("0.00")
                    + ") is not the stair cell centre (" + expectedX.ToString("0.00") + ", " + expectedZ.ToString("0.00")
                    + "). The model is probably turned the wrong way: check GrayboxStairs.ModelYawOffsetDegrees.");
            }
        }

        // ------------------------------------------------------------------------------------------------
        // ground and props
        // ------------------------------------------------------------------------------------------------

        private static void BuildGround(BuildContext ctx, GrayboxLayout layout, Transform garden)
        {
            int minX, minZ, maxX, maxZ;
            layout.GetCellBounds(out minX, out minZ, out maxX, out maxZ);
            for (int i = 0; i < layout.Props.Count; i++)
            {
                GrayboxCell cell = layout.Props[i].Cell;
                minX = Mathf.Min(minX, cell.X);
                minZ = Mathf.Min(minZ, cell.Z);
                maxX = Mathf.Max(maxX, cell.X);
                maxZ = Mathf.Max(maxZ, cell.Z);
            }
            minX -= GroundMarginCells;
            minZ -= GroundMarginCells;
            maxX += GroundMarginCells;
            maxZ += GroundMarginCells;

            float sizeX = (maxX - minX + 1) * CellSize;
            float sizeZ = (maxZ - minZ + 1) * CellSize;
            Vector3 center = new Vector3((minX + maxX + 1) * 0.5f * CellSize, -GroundThickness * 0.5f, (minZ + maxZ + 1) * 0.5f * CellSize);
            Primitive(ctx, PrimitiveType.Cube, GroundName, garden, center, new Vector3(sizeX, GroundThickness, sizeZ),
                ctx.LitMaterial("M_Graybox_Ground", GroundColor), ColliderMode.Primitive);
        }

        private static void BuildProp(BuildContext ctx, GrayboxProp prop, int index, Transform parent)
        {
            // Trees stand outside the garden on the ground slab, everything else on the garden floor tiles.
            float baseY = prop.Kind == GrayboxPropKind.Tree ? 0f : FloorTopOffset;
            Transform root = Group(ctx, parent, "Prop_" + prop.Kind + "_" + index.ToString("00"));
            root.localPosition = new Vector3(prop.CellX * CellSize, baseY, prop.CellZ * CellSize);
            root.localRotation = Quaternion.Euler(0f, prop.YawDegrees, 0f);

            Material wood = ctx.LitMaterial("M_Graybox_Wood", WoodColor);
            Material stone = ctx.LitMaterial("M_Graybox_Stone", StoneColor);
            switch (prop.Kind)
            {
                case GrayboxPropKind.Tree:
                    Primitive(ctx, PrimitiveType.Cylinder, "Trunk", root, new Vector3(0f, TreeTrunkHeight * 0.5f, 0f),
                        CylinderScale(TreeTrunkDiameter, TreeTrunkHeight), wood, ColliderMode.Primitive);
                    Primitive(ctx, PrimitiveType.Sphere, "Crown", root, new Vector3(0f, TreeCrownCenterY, 0f),
                        Vector3.one * TreeCrownDiameter, ctx.LitMaterial("M_Graybox_Leaf", LeafColor), ColliderMode.None);
                    break;

                case GrayboxPropKind.Well:
                    Primitive(ctx, PrimitiveType.Cylinder, "Ring", root, new Vector3(0f, WellRingHeight * 0.5f, 0f),
                        CylinderScale(WellRingDiameter, WellRingHeight), stone, ColliderMode.Mesh);
                    Primitive(ctx, PrimitiveType.Cylinder, "Water", root, new Vector3(0f, WellRingHeight + WellWaterThickness * 0.5f, 0f),
                        CylinderScale(WellWaterDiameter, WellWaterThickness), ctx.LitMaterial("M_Graybox_Water", WaterColor), ColliderMode.None);
                    Vector3 postSize = new Vector3(WellPostThickness, WellPostHeight, WellPostThickness);
                    Primitive(ctx, PrimitiveType.Cube, "PostL", root, new Vector3(-WellPostOffsetX, WellPostHeight * 0.5f, 0f), postSize, wood, ColliderMode.None);
                    Primitive(ctx, PrimitiveType.Cube, "PostR", root, new Vector3(WellPostOffsetX, WellPostHeight * 0.5f, 0f), postSize, wood, ColliderMode.None);
                    Primitive(ctx, PrimitiveType.Cube, "Beam", root, new Vector3(0f, WellPostHeight + WellPostThickness * 0.5f, 0f),
                        new Vector3(WellBeamLength, WellPostThickness, WellPostThickness), wood, ColliderMode.None);
                    break;

                case GrayboxPropKind.Woodpile:
                    Primitive(ctx, PrimitiveType.Cube, "Base", root, new Vector3(0f, WoodpileBaseSize.y * 0.5f, 0f), WoodpileBaseSize, wood, ColliderMode.Primitive);
                    Primitive(ctx, PrimitiveType.Cube, "Top", root, new Vector3(0f, WoodpileBaseSize.y + WoodpileTopSize.y * 0.5f, 0f), WoodpileTopSize, wood, ColliderMode.Primitive);
                    break;

                default: // Pot
                    Primitive(ctx, PrimitiveType.Cylinder, "Pot", root, new Vector3(0f, PotHeight * 0.5f, 0f),
                        CylinderScale(PotDiameter, PotHeight), ctx.LitMaterial("M_Graybox_Clay", ClayColor), ColliderMode.Mesh);
                    break;
            }
            ctx.Props++;
        }

        private static Vector3 CylinderScale(float diameter, float height)
        {
            return new Vector3(diameter, height / CylinderBaseHeight, diameter);
        }

        private enum ColliderMode
        {
            None,

            /// <summary>The primitive's own collider (box, capsule, sphere).</summary>
            Primitive,

            /// <summary>A mesh collider: use for flat, wide cylinders where a capsule would balloon.</summary>
            Mesh
        }

        private static GameObject Primitive(BuildContext ctx, PrimitiveType type, string name, Transform parent, Vector3 localPosition,
            Vector3 localScale, Material material, ColliderMode colliderMode)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.layer = ctx.Layer;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            if (material != null)
            {
                go.GetComponent<MeshRenderer>().sharedMaterial = material;
            }

            if (colliderMode != ColliderMode.Primitive)
            {
                Object.DestroyImmediate(go.GetComponent<Collider>());
            }
            if (colliderMode == ColliderMode.Mesh)
            {
                var meshCollider = go.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
            }
            return go;
        }

        // ------------------------------------------------------------------------------------------------
        // helpers
        // ------------------------------------------------------------------------------------------------

        private static GameObject Place(BuildContext ctx, PieceSpec spec, string name, Transform parent, Vector3 localPosition, float yaw)
        {
            GameObject prefab = ctx.Prefab(spec.Prefab);
            if (prefab == null)
            {
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = name;
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            instance.transform.localScale = spec.Scale;
            if (instance.layer != ctx.Layer)
            {
                instance.layer = ctx.Layer;
            }
            return instance;
        }

        private static Transform Group(BuildContext ctx, Transform parent, string name)
        {
            var go = new GameObject(name);
            go.layer = ctx.Layer;
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }
            return go.transform;
        }

        private static int ResolveEnvironmentLayer()
        {
            int layer = LayerMask.NameToLayer(EnvironmentLayerName);
            if (layer < 0)
            {
                Debug.LogWarning("[House] Layer '" + EnvironmentLayerName + "' is missing (Tech section 2); using layer " + EnvironmentLayerFallback + ".");
                return EnvironmentLayerFallback;
            }
            return layer;
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

        /// <summary>Per-build state: layer, prefab and material caches, counters for the summary log.</summary>
        private sealed class BuildContext
        {
            public readonly int Layer;
            public readonly List<GrayboxCell> Cells = new List<GrayboxCell>();
            public bool Failed;

            public int Rooms;
            public int FloorTiles;
            public int Posts;
            public int Stairs;
            public int Props;
            public readonly int[] Pieces = new int[4]; // indexed by GrayboxWallKind

            private readonly Dictionary<string, GameObject> _prefabs = new Dictionary<string, GameObject>();
            private readonly Dictionary<string, Material> _materials = new Dictionary<string, Material>();

            public BuildContext(int layer)
            {
                Layer = layer;
            }

            public GameObject Prefab(string name)
            {
                GameObject prefab;
                if (_prefabs.TryGetValue(name, out prefab))
                {
                    return prefab;
                }

                string path = EnvironmentFolder + "/" + name + ".prefab";
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    Debug.LogError("[House] Missing prefab " + path);
                    Failed = true;
                }
                _prefabs.Add(name, prefab);
                return prefab;
            }

            /// <summary>An opaque URP Lit material asset; null (default material) when the shader is not found.</summary>
            public Material LitMaterial(string name, Color color)
            {
                Material material;
                if (_materials.TryGetValue(name, out material))
                {
                    return material;
                }

                Shader shader = Shader.Find(LitShaderName);
                if (shader == null)
                {
                    Debug.LogWarning("[House] Shader '" + LitShaderName + "' not found; graybox props keep the default material.");
                }
                else
                {
                    string path = MaterialFolder + "/" + name + ".mat";
                    material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (material == null)
                    {
                        material = new Material(shader);
                        AssetDatabase.CreateAsset(material, path);
                    }
                    material.shader = shader;
                    material.SetColor(BaseColorProperty, color);
                    EditorUtility.SetDirty(material);
                }
                _materials.Add(name, material);
                return material;
            }

            public string Summary(string path)
            {
                int doors = Pieces[(int)GrayboxWallKind.Door];
                int walls = Pieces[(int)GrayboxWallKind.Wall];
                int fences = Pieces[(int)GrayboxWallKind.Fence];
                int railings = Pieces[(int)GrayboxWallKind.Railing];
                return "[House] Graybox built -> " + path + ": " + Rooms + " rooms, " + FloorTiles + " floor tiles, "
                    + (doors + walls + fences + railings) + " wall pieces (" + walls + " walls, " + doors + " doors, "
                    + fences + " fences, " + railings + " railings), " + Posts + " posts, " + Stairs + " stairs, " + Props + " props.";
            }
        }
    }
}
