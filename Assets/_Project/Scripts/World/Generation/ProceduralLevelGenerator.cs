using System.Collections.Generic;
using CinliMahzen.Core;
using UnityEngine;

namespace CinliMahzen.World
{
    /// <summary>
    /// Unity layer of the procedural generator: turns a LevelPlan (pure C#, see LevelPlanner) into GameObjects.
    /// Uses the KayKit prefabs from LevelPrefabSet (pivot convention: see LevelPrefabSet); falls back to primitive cubes when the set is missing.
    /// </summary>
    public sealed class ProceduralLevelGenerator : ILevelGenerator
    {
        private const float WallThickness = 0.3f;
        private const float DoorWidth = 2f;
        private const float DoorHeight = 2.4f;

        private static readonly Dictionary<int, Material> MaterialCache = new Dictionary<int, Material>();

        public LevelLayout Generate(int seed, LevelGenSettings settings, Transform root)
        {
            LevelPlan plan = null;
            int used = seed;
            for (int i = 0; i <= settings.MaxRetries && plan == null; i++)
            {
                used = seed + i;
                plan = LevelPlanner.Plan(used, settings);
                if (plan == null) CMLog.Error("Level", "Seed " + used + " failed validation; retrying with seed+1");
            }
            if (plan == null)
            {
                used = settings.FallbackSeed;
                CMLog.Error("Level", "Using FallbackSeed " + used);
                plan = LevelPlanner.Plan(used, settings);
            }
            return Build(plan, settings, root);
        }

        public static LevelLayout Build(LevelPlan plan, LevelGenSettings s, Transform root)
        {
            int layer = LayerMask.NameToLayer("Environment");
            if (layer < 0) layer = 0;
            float cell = plan.CellSize;
            float h = s.WallHeight;

            var prefabs = LevelPrefabSet.Load();
            if (prefabs != null && !prefabs.IsComplete) prefabs = null;

            var floors = Child(root, "Floors");
            var walls = Child(root, "Walls");
            var markers = Child(root, "Markers");

            for (int ri = 0; ri < plan.Rooms.Count; ri++)
            {
                var r = plan.Rooms[ri];
                for (int x = r.CellX; x < r.CellX + r.CellW; x++)
                    for (int z = r.CellZ; z < r.CellZ + r.CellH; z++)
                    {
                        if (prefabs != null)
                            Spawn(floors, "Floor_" + x + "_" + z, Pick(prefabs.Floors, x, z, 5), new Vector3((x + 0.5f) * cell, 0f, (z + 0.5f) * cell), 0f);
                        else
                            Box(floors, "Floor_" + x + "_" + z, new Vector3((x + 0.5f) * cell, -0.1f, (z + 0.5f) * cell), new Vector3(cell, 0.2f, cell), 0f, layer, 1);
                    }
            }

            for (int i = 0; i < plan.Walls.Count; i++)
            {
                var w = plan.Walls[i];
                float mx = w.Horizontal ? (w.X + 0.5f) * cell : w.X * cell;
                float mz = w.Horizontal ? w.Z * cell : (w.Z + 0.5f) * cell;
                float yaw = w.Horizontal ? 0f : 90f;
                string tag = w.X + "_" + w.Z + (w.Horizontal ? "h" : "v");
                float len = cell + WallThickness;
                if (prefabs != null)
                {
                    GameObject prefab = w.Kind == WallKind.Wall ? Pick(prefabs.Walls, w.X, w.Z, 6)
                        : w.Kind == WallKind.Gated ? prefabs.WallGated : prefabs.WallDoorway;
                    string prefix = w.Kind == WallKind.Wall ? "Wall_" : w.Kind == WallKind.Gated ? "WallGated_" : "WallDoorway_";
                    Spawn(walls, prefix + tag, prefab, new Vector3(mx, 0f, mz), yaw);
                    continue;
                }
                switch (w.Kind)
                {
                    case WallKind.Wall:
                        Box(walls, "Wall_" + tag, new Vector3(mx, h * 0.5f, mz), new Vector3(len, h, WallThickness), yaw, layer, 2);
                        break;
                    case WallKind.Gated:
                        Box(walls, "WallGated_" + tag, new Vector3(mx, h * 0.5f, mz), new Vector3(len, h, WallThickness), yaw, layer, 3);
                        break;
                    default:
                    {
                        float side = (cell - DoorWidth) * 0.5f;
                        float off = (DoorWidth + side) * 0.5f + WallThickness * 0.25f;
                        Vector3 along = w.Horizontal ? Vector3.right : Vector3.forward;
                        Vector3 mid = new Vector3(mx, 0f, mz);
                        Box(walls, "WallDoorL_" + tag, mid - along * off + Vector3.up * (h * 0.5f), new Vector3(side + WallThickness * 0.5f, h, WallThickness), yaw, layer, 2);
                        Box(walls, "WallDoorR_" + tag, mid + along * off + Vector3.up * (h * 0.5f), new Vector3(side + WallThickness * 0.5f, h, WallThickness), yaw, layer, 2);
                        Box(walls, "WallLintel_" + tag, mid + Vector3.up * (DoorHeight + (h - DoorHeight) * 0.5f), new Vector3(DoorWidth, h - DoorHeight, WallThickness), yaw, layer, 2);
                        break;
                    }
                }
            }

            if (prefabs != null)
            {
                // Pillars on every grid vertex that touches a wall: hide the corner gap between 1 m thick wall segments.
                var vertices = new SortedSet<long>();
                for (int i = 0; i < plan.Walls.Count; i++)
                {
                    var w = plan.Walls[i];
                    vertices.Add(VertexKey(w.X, w.Z));
                    vertices.Add(w.Horizontal ? VertexKey(w.X + 1, w.Z) : VertexKey(w.X, w.Z + 1));
                }
                foreach (long key in vertices)
                {
                    int vx = (int)(key / 1000L), vz = (int)(key % 1000L);
                    Spawn(walls, "Pillar_" + vx + "_" + vz, prefabs.Pillar, new Vector3(vx * cell, 0f, vz * cell), 0f);
                }
            }

            var layout = new LevelLayout
            {
                Seed = plan.Seed,
                Bounds = new Bounds(new Vector3(plan.GridSize * cell * 0.5f, h * 0.5f, plan.GridSize * cell * 0.5f), new Vector3(plan.GridSize * cell, h, plan.GridSize * cell)),
            };
            int n = plan.Rooms.Count;
            layout.RoomAdjacency = new int[n, n];
            for (int i = 0; i < n; i++)
            {
                var r = plan.Rooms[i];
                var b = new Bounds();
                b.SetMinMax(new Vector3(r.MinX, 0f, r.MinZ), new Vector3(r.MaxX, h, r.MaxZ));
                layout.Rooms.Add(new RoomInfo { RoomId = r.RoomId, Bounds = b, Tags = r.Tags, Neighbors = new List<int>(r.Neighbors) });
                for (int k = 0; k < r.Neighbors.Count; k++) layout.RoomAdjacency[i, r.Neighbors[k]] = 1;
            }

            for (int i = 0; i < plan.Markers.Count; i++)
                layout.Markers.Add(CreateMarker(plan.Markers[i], i, markers));

            var holder = root.GetComponent<LevelLayoutHolder>();
            if (holder == null) holder = root.gameObject.AddComponent<LevelLayoutHolder>();
            holder.Store(layout);
            return layout;
        }

        private static long VertexKey(int x, int z) => x * 1000L + z;

        /// <summary>Deterministic variant pick from grid coordinates (never UnityEngine.Random): ~1/rarity cells use a non-default variant.</summary>
        private static GameObject Pick(GameObject[] variants, int x, int z, int rarity)
        {
            if (variants.Length == 1 || variants[1] == null) return variants[0];
            unchecked
            {
                uint h = (uint)(x * 73856093) ^ (uint)(z * 19349663);
                h ^= h >> 13; h *= 1274126177u; h ^= h >> 16;
                return h % (uint)rarity == 0 ? variants[1 + (int)((h / (uint)rarity) % (uint)(variants.Length - 1))] : variants[0];
            }
        }

        private static void Spawn(Transform parent, string name, GameObject prefab, Vector3 pos, float yaw)
        {
            var go = Object.Instantiate(prefab, parent, false);
            go.name = name;
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            SetStatic(go);
        }

        private static void SetStatic(GameObject go)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.isStatic = true;
        }

        private static LevelMarker CreateMarker(PlannedMarker pm, int index, Transform parent)
        {
            var go = new GameObject(pm.Kind + "_" + index);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(pm.X, pm.Y, pm.Z);
            go.transform.rotation = Quaternion.Euler(0f, pm.YawDeg, 0f);
            LevelMarker marker;
            switch (pm.Kind)
            {
                case MarkerKind.HumanSpawn: marker = go.AddComponent<HumanSpawnMarker>(); break;
                case MarkerKind.JinnSpawn: marker = go.AddComponent<JinnSpawnMarker>(); break;
                case MarkerKind.Exit: marker = go.AddComponent<ExitMarker>(); break;
                case MarkerKind.GoldSpawn: marker = go.AddComponent<GoldSpawnMarker>(); break;
                case MarkerKind.ContainerSocket: marker = go.AddComponent<ContainerSocket>(); break;
                case MarkerKind.DecorSocket: marker = go.AddComponent<DecorSocket>(); break;
                case MarkerKind.Vault:
                    var v = go.AddComponent<VaultMarker>();
                    v.SetDoor(go.transform);
                    marker = v;
                    break;
                case MarkerKind.PossessableSocket:
                    var ps = go.AddComponent<PossessableSocket>();
                    ps.Setup(pm.Category, pm.WallFacing);
                    marker = ps;
                    break;
                case MarkerKind.PuzzleSocket:
                    var pz = go.AddComponent<PuzzleSocket>();
                    pz.Setup(pm.Puzzle);
                    marker = pz;
                    break;
                default:
                    var ls = go.AddComponent<LightSocket>();
                    ls.Setup(pm.WallFacing);
                    marker = ls;
                    break;
            }
            marker.Init(pm.RoomId);
            return marker;
        }

        private static Transform Child(Transform root, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(root, false);
            return t;
        }

        private static void Box(Transform parent, string name, Vector3 pos, Vector3 scale, float yaw, int layer, int style)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.layer = layer;
            go.isStatic = true;
            var t = go.transform;
            t.SetParent(parent, false);
            t.position = pos;
            t.rotation = Quaternion.Euler(0f, yaw, 0f);
            t.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = GetMaterial(style);
        }

        private static Material GetMaterial(int style)
        {
            if (MaterialCache.TryGetValue(style, out var cached) && cached != null) return cached;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Color c = style == 1 ? new Color(0.25f, 0.23f, 0.22f) : style == 2 ? new Color(0.45f, 0.4f, 0.35f) : new Color(0.55f, 0.15f, 0.12f);
            var m = new Material(shader) { name = "M_LevelPlaceholder_" + style, color = c };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            MaterialCache[style] = m;
            return m;
        }
    }
}
