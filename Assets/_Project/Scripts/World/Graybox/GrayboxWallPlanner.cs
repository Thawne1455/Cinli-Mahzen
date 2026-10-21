using System;
using System.Collections.Generic;

namespace CinliMahzen.World.Graybox
{
    /// <summary>
    /// Turns a <see cref="GrayboxLayout"/> into the list of wall pieces and corner posts to build. Pure C# so the
    /// editor builder stays a thin prefab placer and the rules are covered by EditMode tests.
    ///
    /// Rules, per cell border:
    ///  - both cells in the same room: nothing.
    ///  - two different rooms: one wall (or door) shared by both, owned by the indoor room (else the room listed first).
    ///  - room next to nothing: a fence for outdoor rooms, a full exterior wall otherwise.
    ///  - room next to a stair shaft: railing if declared, nothing on the arrival border (the cellar hatch door if one is
    ///    declared there), else full wall.
    /// </summary>
    public static class GrayboxWallPlanner
    {
        public static List<GrayboxWallSegment> PlanWalls(GrayboxLayout layout)
        {
            var segments = new List<GrayboxWallSegment>();
            var cells = new List<GrayboxCell>();
            var seen = new HashSet<GrayboxEdge>();

            for (int floor = GrayboxLayout.MinFloor; floor <= GrayboxLayout.MaxFloor; floor++)
            {
                seen.Clear();
                for (int roomIndex = 0; roomIndex < layout.Rooms.Count; roomIndex++)
                {
                    GrayboxRoom room = layout.Rooms[roomIndex];
                    if (room.Floor != floor)
                    {
                        continue;
                    }

                    cells.Clear();
                    room.AppendCellsTo(cells);
                    for (int c = 0; c < cells.Count; c++)
                    {
                        for (int d = 0; d < GrayboxDirectionUtil.DirectionCount; d++)
                        {
                            GrayboxCell cell = cells[c];
                            GrayboxCell neighbor = cell.Step((GrayboxDirection)d);
                            GrayboxEdge edge = GrayboxEdge.Between(floor, cell, neighbor);
                            if (!seen.Add(edge))
                            {
                                continue;
                            }

                            GrayboxWallSegment segment = PlanEdge(layout, floor, room, roomIndex, neighbor, edge);
                            if (segment != null)
                            {
                                segments.Add(segment);
                            }
                        }
                    }
                }
            }
            return segments;
        }

        private static GrayboxWallSegment PlanEdge(GrayboxLayout layout, int floor, GrayboxRoom room, int roomIndex,
            GrayboxCell neighbor, GrayboxEdge edge)
        {
            int neighborIndex = layout.RoomIndexAt(floor, neighbor);
            if (neighborIndex == roomIndex)
            {
                return null;
            }

            if (layout.IsStairVoid(floor, neighbor))
            {
                if (IsArrivalEdge(layout, edge))
                {
                    // Open, except the cellar hatch that closes the top of the cellar stairs.
                    GrayboxDoor hatch = layout.FindDoor(edge);
                    return hatch != null ? new GrayboxWallSegment(edge, GrayboxWallKind.Door, hatch, room.Id) : null;
                }
                bool railing = layout.FindRailing(edge) != null;
                return new GrayboxWallSegment(edge, railing ? GrayboxWallKind.Railing : GrayboxWallKind.Wall, null, room.Id);
            }

            if (neighborIndex < 0)
            {
                GrayboxWallKind kind = room.IsOutdoor ? GrayboxWallKind.Fence : GrayboxWallKind.Wall;
                return new GrayboxWallSegment(edge, kind, null, room.Id);
            }

            GrayboxRoom other = layout.Rooms[neighborIndex];
            GrayboxRoom owner = PickOwner(room, roomIndex, other, neighborIndex);
            GrayboxDoor door = layout.FindDoor(edge);
            if (door != null)
            {
                return new GrayboxWallSegment(edge, GrayboxWallKind.Door, door, owner.Id);
            }
            return new GrayboxWallSegment(edge, GrayboxWallKind.Wall, null, owner.Id);
        }

        private static GrayboxRoom PickOwner(GrayboxRoom a, int indexA, GrayboxRoom b, int indexB)
        {
            if (a.IsOutdoor != b.IsOutdoor)
            {
                return a.IsOutdoor ? b : a;
            }
            return indexA <= indexB ? a : b;
        }

        private static bool IsArrivalEdge(GrayboxLayout layout, GrayboxEdge edge)
        {
            for (int i = 0; i < layout.Stairs.Count; i++)
            {
                if (layout.Stairs[i].ArrivalEdge.Equals(edge))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// One post per grid vertex touched by a wall piece: a pillar if any full wall or door ends there, otherwise
        /// a short fence post. Output order follows the segment order, so it is deterministic.
        /// </summary>
        public static List<GrayboxPost> PlanPosts(IReadOnlyList<GrayboxWallSegment> segments)
        {
            var order = new List<VertexKey>();
            var solid = new Dictionary<VertexKey, bool>();

            for (int i = 0; i < segments.Count; i++)
            {
                GrayboxWallSegment segment = segments[i];
                bool isSolid = segment.Kind == GrayboxWallKind.Wall || segment.Kind == GrayboxWallKind.Door;
                int ax, az, bx, bz;
                segment.Edge.GetVertices(out ax, out az, out bx, out bz);
                Touch(order, solid, new VertexKey(segment.Edge.Floor, ax, az), isSolid);
                Touch(order, solid, new VertexKey(segment.Edge.Floor, bx, bz), isSolid);
            }

            var posts = new List<GrayboxPost>(order.Count);
            for (int i = 0; i < order.Count; i++)
            {
                VertexKey key = order[i];
                GrayboxPostKind kind = solid[key] ? GrayboxPostKind.Pillar : GrayboxPostKind.FencePost;
                posts.Add(new GrayboxPost(key.Floor, key.X, key.Z, kind));
            }
            return posts;
        }

        private static void Touch(List<VertexKey> order, Dictionary<VertexKey, bool> solid, VertexKey key, bool isSolid)
        {
            bool existing;
            if (solid.TryGetValue(key, out existing))
            {
                solid[key] = existing || isSolid;
                return;
            }
            order.Add(key);
            solid.Add(key, isSolid);
        }

        private readonly struct VertexKey : IEquatable<VertexKey>
        {
            public readonly int Floor;
            public readonly int X;
            public readonly int Z;

            public VertexKey(int floor, int x, int z)
            {
                Floor = floor;
                X = x;
                Z = z;
            }

            public bool Equals(VertexKey other)
            {
                return Floor == other.Floor && X == other.X && Z == other.Z;
            }

            public override bool Equals(object obj)
            {
                return obj is VertexKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (Floor * 397 ^ X) * 397 ^ Z;
                }
            }
        }
    }
}
