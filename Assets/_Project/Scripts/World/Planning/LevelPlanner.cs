using System;
using System.Collections.Generic;

namespace CinliMahzen.World
{
    /// <summary>
    /// ProceduralLevelGenerator v1 logic (Spec 7.2), Unity-free. Grid is partitioned into rectangular rooms
    /// (so every room touches its neighbours), doors are opened on an MST plus ~20% extra edges (loops),
    /// then roles, markers and sockets are assigned. Deterministic: only System.Random(seed), ordered lists.
    /// </summary>
    public static class LevelPlanner
    {
        private const int MaxAttempts = 200;

        private struct Rect { public int X, Z, W, H; }
        private struct Edge { public int A, B; public List<PlannedWall> Segments; public float Weight; }

        /// <summary>Returns null only if no attempt satisfied the hard constraints (caller retries with another seed).</summary>
        public static LevelPlan Plan(int seed, LevelGenSettings s)
        {
            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                var rng = new Random(unchecked(seed * 7919 + attempt * 104729 + 17));
                var plan = TryPlan(seed, attempt, rng, s);
                if (plan != null && LevelRules.Validate(plan.Rooms, plan.Markers, s).Count == 0) return plan;
            }
            return null;
        }

        private static LevelPlan TryPlan(int seed, int attempt, Random rng, LevelGenSettings s)
        {
            var rects = Partition(rng, s);
            if (rects == null) return null;

            int g = s.GridSize;
            var plan = new LevelPlan { Seed = seed, Attempt = attempt, GridSize = g, CellSize = s.CellSize };
            var owner = new int[g, g];
            for (int i = 0; i < rects.Count; i++)
            {
                var r = rects[i];
                var room = new PlannedRoom
                {
                    RoomId = i, CellX = r.X, CellZ = r.Z, CellW = r.W, CellH = r.H,
                    MinX = r.X * s.CellSize, MinZ = r.Z * s.CellSize,
                    MaxX = (r.X + r.W) * s.CellSize, MaxZ = (r.Z + r.H) * s.CellSize,
                };
                if (Math.Min(r.W, r.H) == 2 && Math.Max(r.W, r.H) >= 3) room.Tags |= RoomTag.Corridor;
                if (r.W * r.H >= 9) room.Tags |= RoomTag.Large;
                plan.Rooms.Add(room);
                for (int x = r.X; x < r.X + r.W; x++)
                    for (int z = r.Z; z < r.Z + r.H; z++) owner[x, z] = i;
            }

            // shared boundaries between rooms (scan order is deterministic)
            var edgeMap = new SortedDictionary<long, Edge>();
            var perimeter = new List<PlannedWall>();
            for (int x = 0; x <= g; x++)
                for (int z = 0; z < g; z++)
                {
                    var seg = new PlannedWall { X = x, Z = z, Horizontal = false, Kind = WallKind.Wall };
                    if (x == 0 || x == g) { perimeter.Add(seg); continue; }
                    AddBoundary(edgeMap, owner[x - 1, z], owner[x, z], seg);
                }
            for (int z = 0; z <= g; z++)
                for (int x = 0; x < g; x++)
                {
                    var seg = new PlannedWall { X = x, Z = z, Horizontal = true, Kind = WallKind.Wall };
                    if (z == 0 || z == g) { perimeter.Add(seg); continue; }
                    AddBoundary(edgeMap, owner[x, z - 1], owner[x, z], seg);
                }

            var edges = new List<Edge>(edgeMap.Values);
            for (int i = 0; i < edges.Count; i++)
            {
                var e = edges[i];
                e.Weight = (float)rng.NextDouble();
                edges[i] = e;
            }

            // Kruskal MST
            var order = new List<int>();
            for (int i = 0; i < edges.Count; i++) order.Add(i);
            order.Sort((p, q) =>
            {
                int c = edges[p].Weight.CompareTo(edges[q].Weight);
                return c != 0 ? c : p.CompareTo(q);
            });
            var uf = new int[rects.Count];
            for (int i = 0; i < uf.Length; i++) uf[i] = i;
            int Find(int v) { while (uf[v] != v) { uf[v] = uf[uf[v]]; v = uf[v]; } return v; }
            var mst = new List<int>();
            var rest = new List<int>();
            for (int k = 0; k < order.Count; k++)
            {
                var e = edges[order[k]];
                int ra = Find(e.A), rb = Find(e.B);
                if (ra != rb) { uf[ra] = rb; mst.Add(order[k]); } else rest.Add(order[k]);
            }
            if (mst.Count != rects.Count - 1) return null;

            // vault = an MST leaf (single entrance); extras never touch it
            var degree = new int[rects.Count];
            for (int k = 0; k < mst.Count; k++) { degree[edges[mst[k]].A]++; degree[edges[mst[k]].B]++; }
            var leaves = new List<int>();
            for (int i = 0; i < degree.Length; i++) if (degree[i] == 1) leaves.Add(i);
            if (leaves.Count == 0) return null;
            int vault = leaves[rng.Next(leaves.Count)];

            var chosen = new List<int>(mst);
            int extra = Math.Max(1, (int)Math.Round(s.ExtraEdgeChance * mst.Count));
            for (int k = 0; k < rest.Count && extra > 0; k++)
            {
                var e = edges[rest[k]];
                if (e.A == vault || e.B == vault) continue;
                chosen.Add(rest[k]);
                extra--;
            }
            if (chosen.Count < rects.Count) return null; // no loop possible

            // open doors
            var doorPos = new Dictionary<int, PlannedWall>();
            chosen.Sort();
            for (int k = 0; k < chosen.Count; k++)
            {
                var e = edges[chosen[k]];
                int idx = rng.Next(e.Segments.Count);
                bool gated = e.A == vault || e.B == vault;
                var d = e.Segments[idx];
                d.Kind = gated ? WallKind.Gated : WallKind.Doorway;
                e.Segments[idx] = d;
                plan.Rooms[e.A].Neighbors.Add(e.B);
                plan.Rooms[e.B].Neighbors.Add(e.A);
            }
            for (int i = 0; i < plan.Rooms.Count; i++) plan.Rooms[i].Neighbors.Sort();

            plan.Walls.AddRange(perimeter);
            for (int i = 0; i < edges.Count; i++) plan.Walls.AddRange(edges[i].Segments);

            if (!AssignRoles(plan, rng, s, vault)) return null;
            PlaceMarkers(plan, rng, s);
            return plan;
        }

        private static void AddBoundary(SortedDictionary<long, Edge> map, int a, int b, PlannedWall seg)
        {
            if (a == b) return;
            int lo = Math.Min(a, b), hi = Math.Max(a, b);
            long key = lo * 1000L + hi;
            if (!map.TryGetValue(key, out var e))
                e = new Edge { A = lo, B = hi, Segments = new List<PlannedWall>() };
            e.Segments.Add(seg);
            map[key] = e;
        }

        private static List<Rect> Partition(Random rng, LevelGenSettings s)
        {
            int g = s.GridSize;
            int target = rng.Next(s.MinRooms, s.MaxRooms + 1);
            var rects = new List<Rect> { new Rect { X = 0, Z = 0, W = g, H = g } };
            for (int guard = 0; guard < 200; guard++)
            {
                int idx = -1;
                for (int i = 0; i < rects.Count; i++)
                {
                    var r = rects[i];
                    if (r.W > 4 || r.H > 4 || r.W * r.H > 12) { idx = i; break; }
                }
                if (idx < 0)
                {
                    if (rects.Count >= target) break;
                    var cand = new List<int>();
                    for (int i = 0; i < rects.Count; i++) if (rects[i].W >= 4 || rects[i].H >= 4) cand.Add(i);
                    if (cand.Count == 0) return null;
                    idx = cand[rng.Next(cand.Count)];
                }
                var rc = rects[idx];
                bool splitW;
                if (rc.W >= 4 && rc.H >= 4) splitW = rc.W == rc.H ? rng.Next(2) == 0 : rc.W > rc.H;
                else splitW = rc.W >= 4;
                int len = splitW ? rc.W : rc.H;
                int k = rng.Next(2, len - 1); // both halves >= 2
                Rect a, b;
                if (splitW)
                {
                    a = new Rect { X = rc.X, Z = rc.Z, W = k, H = rc.H };
                    b = new Rect { X = rc.X + k, Z = rc.Z, W = rc.W - k, H = rc.H };
                }
                else
                {
                    a = new Rect { X = rc.X, Z = rc.Z, W = rc.W, H = k };
                    b = new Rect { X = rc.X, Z = rc.Z + k, W = rc.W, H = rc.H - k };
                }
                rects[idx] = a;
                rects.Add(b);
            }
            if (rects.Count < s.MinRooms || rects.Count > s.MaxRooms) return null;
            foreach (var r in rects)
                if (r.W > 4 || r.H > 4 || r.W * r.H > 12) return null;
            rects.Sort((p, q) => p.Z != q.Z ? p.Z.CompareTo(q.Z) : p.X.CompareTo(q.X));
            return rects;
        }

        private static bool AssignRoles(LevelPlan plan, Random rng, LevelGenSettings s, int vault)
        {
            var rooms = plan.Rooms;
            rooms[vault].Tags |= RoomTag.Vault;

            var fromVault = LevelGraph.PathDistances(rooms, vault);
            int exit = -1;
            for (int i = 0; i < rooms.Count; i++)
            {
                if (i == vault) continue;
                if (exit < 0 || fromVault[i] > fromVault[exit]) exit = i;
            }
            if (exit < 0 || fromVault[exit] < s.MinExitPathFromVault) return false;
            rooms[exit].Tags |= RoomTag.Exit;

            // start: the room with the most rooms >= jinn distance away (so three jinn spawns are possible)
            int start = -1, bestFar = -1;
            for (int i = 0; i < rooms.Count; i++)
            {
                if (i == vault || i == exit) continue;
                int far = 0;
                for (int j = 0; j < rooms.Count; j++)
                {
                    if (j == vault) continue;
                    FarCorner(rooms[j], rooms[i].CenterX, rooms[i].CenterZ, out float cx, out float cz);
                    float dx = cx - rooms[i].CenterX, dz = cz - rooms[i].CenterZ;
                    if (Math.Sqrt(dx * dx + dz * dz) >= s.MinJinnDistanceFromHuman + 2f) far++;
                }
                if (far > bestFar) { bestFar = far; start = i; }
            }
            if (start < 0 || bestFar < 1) return false;
            rooms[start].Tags |= RoomTag.Start;
            return true;
        }

        private static int RoomWith(IReadOnlyList<PlannedRoom> rooms, RoomTag tag)
        {
            for (int i = 0; i < rooms.Count; i++) if ((rooms[i].Tags & tag) != 0) return i;
            return -1;
        }

        private static void Add(LevelPlan p, MarkerKind kind, int room, float x, float y, float z, float yaw = 0f,
            SocketCategory cat = SocketCategory.None, PuzzleType puzzle = PuzzleType.RuneStones, bool wall = false)
        {
            p.Markers.Add(new PlannedMarker { Kind = kind, RoomId = room, X = x, Y = y, Z = z, YawDeg = yaw, Category = cat, Puzzle = puzzle, WallFacing = wall });
        }

        private static void PlaceMarkers(LevelPlan plan, Random rng, LevelGenSettings s)
        {
            var rooms = plan.Rooms;
            float cell = plan.CellSize;
            int start = RoomWith(rooms, RoomTag.Start), vault = RoomWith(rooms, RoomTag.Vault), exit = RoomWith(rooms, RoomTag.Exit);

            Add(plan, MarkerKind.HumanSpawn, start, rooms[start].CenterX, 0f, rooms[start].CenterZ);
            Add(plan, MarkerKind.Exit, exit, rooms[exit].CenterX, 0f, rooms[exit].CenterZ);

            // vault door = the gated wall segment
            for (int i = 0; i < plan.Walls.Count; i++)
            {
                var w = plan.Walls[i];
                if (w.Kind != WallKind.Gated) continue;
                float mx = w.Horizontal ? (w.X + 0.5f) * cell : w.X * cell;
                float mz = w.Horizontal ? w.Z * cell : (w.Z + 0.5f) * cell;
                Add(plan, MarkerKind.Vault, vault, mx, 0f, mz, w.Horizontal ? 0f : 90f);
                break;
            }
            Add(plan, MarkerKind.GoldSpawn, vault, rooms[vault].CenterX, 0f, rooms[vault].CenterZ);

            // jinn: three spots >= min distance from human, never in the vault
            var human = plan.Markers[0];
            var jinnRooms = new List<int>();
            for (int i = 0; i < rooms.Count; i++)
            {
                if (i == vault) continue;
                FarCorner(rooms[i], human.X, human.Z, out float cx, out float cz);
                if (Math.Sqrt((cx - human.X) * (cx - human.X) + (cz - human.Z) * (cz - human.Z)) >= s.MinJinnDistanceFromHuman + 0.5f) jinnRooms.Add(i);
            }
            for (int j = 0; j < 3 && jinnRooms.Count > 0; j++)
            {
                int room = jinnRooms[rng.Next(jinnRooms.Count)];
                var r = rooms[room];
                FarCorner(r, human.X, human.Z, out float fx, out float fz);
                float sx = fx > r.CenterX ? -1f : 1f, sz = fz > r.CenterZ ? -1f : 1f;
                float bx = fx, bz = fz;
                for (int t = 0; t < 8; t++)
                {
                    float x = fx + sx * (float)rng.NextDouble() * 1.2f;
                    float z = fz + sz * (float)rng.NextDouble() * 1.2f;
                    if (Math.Sqrt((x - human.X) * (x - human.X) + (z - human.Z) * (z - human.Z)) >= s.MinJinnDistanceFromHuman + 0.5f) { bx = x; bz = z; }
                }
                Add(plan, MarkerKind.JinnSpawn, room, bx, 1.5f, bz);
            }

            // puzzles
            var nonVault = new List<int>();
            for (int i = 0; i < rooms.Count; i++) if (i != vault) nonVault.Add(i);
            int stonesRoom = nonVault[rng.Next(nonVault.Count)];
            var hops = LevelGraph.Hops(rooms, stonesRoom);
            var hintCand = new List<int>();
            for (int i = 0; i < nonVault.Count; i++) if (hops[nonVault[i]] >= s.MinRuneRoomHops) hintCand.Add(nonVault[i]);
            int hintRoom = hintCand.Count > 0 ? hintCand[rng.Next(hintCand.Count)] : stonesRoom;
            int trailRoom = nonVault[rng.Next(nonVault.Count)];
            var hops2 = LevelGraph.Hops(rooms, trailRoom);
            var digCand = new List<int>();
            for (int i = 0; i < nonVault.Count; i++) if (hops2[nonVault[i]] >= 2) digCand.Add(nonVault[i]);
            int digRoom = digCand.Count > 0 ? digCand[rng.Next(digCand.Count)] : trailRoom;
            rooms[stonesRoom].Tags |= RoomTag.PuzzleCandidate;
            rooms[hintRoom].Tags |= RoomTag.PuzzleCandidate;
            rooms[trailRoom].Tags |= RoomTag.PuzzleCandidate;
            rooms[digRoom].Tags |= RoomTag.PuzzleCandidate;

            var walls = RoomWalls(plan);
            FloorPoint(rooms[stonesRoom], rng, 1.2f, out float px, out float pz);
            Add(plan, MarkerKind.PuzzleSocket, stonesRoom, px, 0f, pz, 0f, SocketCategory.None, PuzzleType.RuneStones);
            WallPoint(plan, walls[hintRoom], rng, 0.15f, out px, out pz, out float yaw);
            Add(plan, MarkerKind.PuzzleSocket, hintRoom, px, 1.6f, pz, yaw, SocketCategory.None, PuzzleType.RuneHint, true);
            FloorPoint(rooms[trailRoom], rng, 1.2f, out px, out pz);
            Add(plan, MarkerKind.PuzzleSocket, trailRoom, px, 0f, pz, 0f, SocketCategory.None, PuzzleType.FootprintStart);
            FloorPoint(rooms[digRoom], rng, 1.2f, out px, out pz);
            Add(plan, MarkerKind.PuzzleSocket, digRoom, px, 0f, pz, 0f, SocketCategory.None, PuzzleType.DigSpot);

            // lights: one per room on a plain wall, a second in large rooms
            for (int i = 0; i < rooms.Count; i++)
            {
                int n = (rooms[i].Tags & RoomTag.Large) != 0 ? 2 : 1;
                for (int k = 0; k < n; k++)
                {
                    if (walls[i].Count > 0)
                    {
                        WallPoint(plan, walls[i], rng, 0.1f, out px, out pz, out yaw);
                        Add(plan, MarkerKind.LightSocket, i, px, 2.2f, pz, yaw, SocketCategory.None, PuzzleType.RuneStones, true);
                    }
                    else Add(plan, MarkerKind.LightSocket, i, rooms[i].CenterX, 2.2f, rooms[i].CenterZ);
                }
            }

            // containers, weighted by room area, not in the vault
            int contCount = rng.Next(s.MinContainerSockets, s.MaxContainerSockets + 1);
            for (int n = 0; n < contCount; n++)
            {
                int room = WeightedRoom(rooms, rng, vault);
                if (walls[room].Count > 0)
                {
                    WallPoint(plan, walls[room], rng, 0.5f, out px, out pz, out yaw);
                    Add(plan, MarkerKind.ContainerSocket, room, px, 0f, pz, yaw, SocketCategory.None, PuzzleType.RuneStones, true);
                }
                else
                {
                    FloorPoint(rooms[room], rng, 1.2f, out px, out pz);
                    Add(plan, MarkerKind.ContainerSocket, room, px, 0f, pz);
                }
            }

            // possessable sockets
            int cells = 0;
            for (int i = 0; i < rooms.Count; i++) cells += rooms[i].CellW * rooms[i].CellH;
            int possCount = Math.Max(s.MinPossessableSockets, Math.Min(s.MaxPossessableSockets, (int)Math.Round(cells * 0.85)));
            for (int n = 0; n < possCount; n++)
            {
                int room = WeightedRoom(rooms, rng, -1);
                double roll = rng.NextDouble();
                SocketCategory cat = roll < 0.20 ? SocketCategory.WallLarge : roll < 0.50 ? SocketCategory.Floor
                    : roll < 0.65 ? SocketCategory.Table : roll < 0.80 ? SocketCategory.WallMount : SocketCategory.Corner;
                if ((cat == SocketCategory.WallLarge || cat == SocketCategory.WallMount) && walls[room].Count == 0) cat = SocketCategory.Floor;
                switch (cat)
                {
                    case SocketCategory.WallLarge:
                        WallPoint(plan, walls[room], rng, 0.5f, out px, out pz, out yaw);
                        Add(plan, MarkerKind.PossessableSocket, room, px, 0f, pz, yaw, cat, PuzzleType.RuneStones, true);
                        break;
                    case SocketCategory.WallMount:
                        WallPoint(plan, walls[room], rng, 0.1f, out px, out pz, out yaw);
                        Add(plan, MarkerKind.PossessableSocket, room, px, 1.6f, pz, yaw, cat, PuzzleType.RuneStones, true);
                        break;
                    case SocketCategory.Corner:
                        CornerPoint(rooms[room], rng, out px, out pz, out yaw);
                        Add(plan, MarkerKind.PossessableSocket, room, px, 0f, pz, yaw, cat);
                        break;
                    default:
                        FloorPoint(rooms[room], rng, 1.6f, out px, out pz);
                        Add(plan, MarkerKind.PossessableSocket, room, px, cat == SocketCategory.Table ? 0.9f : 0f, pz, (float)rng.Next(4) * 90f, cat);
                        break;
                }
            }
        }

        /// <summary>Room corner (inset 1 m) that is farthest from the given point.</summary>
        private static void FarCorner(PlannedRoom r, float px, float pz, out float x, out float z)
        {
            x = Math.Abs(r.MinX + 1f - px) > Math.Abs(r.MaxX - 1f - px) ? r.MinX + 1f : r.MaxX - 1f;
            z = Math.Abs(r.MinZ + 1f - pz) > Math.Abs(r.MaxZ - 1f - pz) ? r.MinZ + 1f : r.MaxZ - 1f;
        }

        private static int WeightedRoom(List<PlannedRoom> rooms, Random rng, int exclude)
        {
            int total = 0;
            for (int i = 0; i < rooms.Count; i++) if (i != exclude) total += rooms[i].CellW * rooms[i].CellH;
            int pick = rng.Next(total);
            for (int i = 0; i < rooms.Count; i++)
            {
                if (i == exclude) continue;
                pick -= rooms[i].CellW * rooms[i].CellH;
                if (pick < 0) return i;
            }
            return 0;
        }

        private static void FloorPoint(PlannedRoom r, Random rng, float margin, out float x, out float z)
        {
            x = r.MinX + margin + (float)rng.NextDouble() * (r.MaxX - r.MinX - 2f * margin);
            z = r.MinZ + margin + (float)rng.NextDouble() * (r.MaxZ - r.MinZ - 2f * margin);
        }

        private static void CornerPoint(PlannedRoom r, Random rng, out float x, out float z, out float yaw)
        {
            int c = rng.Next(4);
            const float inset = 0.7f;
            x = (c & 1) == 0 ? r.MinX + inset : r.MaxX - inset;
            z = (c & 2) == 0 ? r.MinZ + inset : r.MaxZ - inset;
            yaw = 45f + 90f * c;
        }

        /// <summary>Plain (non-door) wall segments bordering each room, in plan order.</summary>
        private static List<RoomWall>[] RoomWalls(LevelPlan plan)
        {
            int g = plan.GridSize;
            var owner = new int[g, g];
            for (int i = 0; i < plan.Rooms.Count; i++)
            {
                var r = plan.Rooms[i];
                for (int x = r.CellX; x < r.CellX + r.CellW; x++)
                    for (int z = r.CellZ; z < r.CellZ + r.CellH; z++) owner[x, z] = i;
            }
            var result = new List<RoomWall>[plan.Rooms.Count];
            for (int i = 0; i < result.Length; i++) result[i] = new List<RoomWall>();
            for (int i = 0; i < plan.Walls.Count; i++)
            {
                var w = plan.Walls[i];
                if (w.Kind != WallKind.Wall) continue;
                if (w.Horizontal)
                {
                    if (w.Z > 0) result[owner[w.X, w.Z - 1]].Add(new RoomWall { Wall = w, Positive = false });
                    if (w.Z < g) result[owner[w.X, w.Z]].Add(new RoomWall { Wall = w, Positive = true });
                }
                else
                {
                    if (w.X > 0) result[owner[w.X - 1, w.Z]].Add(new RoomWall { Wall = w, Positive = false });
                    if (w.X < g) result[owner[w.X, w.Z]].Add(new RoomWall { Wall = w, Positive = true });
                }
            }
            return result;
        }

        /// <summary>Point next to a wall segment, inset into the room that owns it.</summary>
        private static void WallPoint(LevelPlan plan, List<RoomWall> segs, Random rng, float inset, out float x, out float z, out float yaw)
        {
            var rw = segs[rng.Next(segs.Count)];
            var w = rw.Wall;
            float cell = plan.CellSize;
            float t = 0.25f + (float)rng.NextDouble() * 0.5f;
            if (w.Horizontal)
            {
                x = (w.X + t) * cell;
                z = w.Z * cell + (rw.Positive ? inset : -inset);
                yaw = rw.Positive ? 0f : 180f;
            }
            else
            {
                z = (w.Z + t) * cell;
                x = w.X * cell + (rw.Positive ? inset : -inset);
                yaw = rw.Positive ? 90f : 270f;
            }
        }

        private struct RoomWall { public PlannedWall Wall; public bool Positive; }
    }
}
