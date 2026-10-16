using System;
using System.Collections.Generic;

namespace CinliMahzen.World.Graybox
{
    /// <summary>
    /// Checks a <see cref="GrayboxLayout"/> against the house rules (GDD 9, 3.1). Returns one readable message per
    /// problem; an empty list means the layout is valid.
    ///
    /// Rules: rooms are well formed and do not overlap on a floor; every door joins two different rooms; exactly one
    /// front door (garden to house); every staircase sits in a ground-floor room, arrives in an upper-floor room,
    /// leaves its shaft open above and is entered through a stage gate; railings only line the shaft; every room is
    /// reachable from the front door (stairs included); every floor has at least one loop in its door graph; garden
    /// props stand where they belong.
    /// </summary>
    public static class GrayboxLayoutValidator
    {
        public static List<string> Validate(GrayboxLayout layout)
        {
            var errors = new List<string>();
            if (layout == null)
            {
                errors.Add("layout is null");
                return errors;
            }

            CheckRooms(layout, errors);
            CheckOverlaps(layout, errors);
            CheckDoors(layout, errors);
            CheckFrontDoor(layout, errors);
            CheckStairs(layout, errors);
            CheckRailings(layout, errors);
            CheckReachability(layout, errors);
            CheckLoops(layout, errors);
            CheckProps(layout, errors);
            return errors;
        }

        public static bool IsValid(GrayboxLayout layout)
        {
            return Validate(layout).Count == 0;
        }

        // ---- rooms ----------------------------------------------------------------------------------

        private static void CheckRooms(GrayboxLayout layout, List<string> errors)
        {
            if (layout.Rooms.Count == 0)
            {
                errors.Add("layout has no rooms");
            }

            var ids = new HashSet<string>();
            for (int i = 0; i < layout.Rooms.Count; i++)
            {
                GrayboxRoom room = layout.Rooms[i];
                if (string.IsNullOrEmpty(room.Id))
                {
                    errors.Add("room #" + i + " has an empty id");
                }
                else if (!ids.Add(room.Id))
                {
                    errors.Add("duplicate room id '" + room.Id + "'");
                }

                if (room.Floor < 0 || room.Floor >= GrayboxLayout.FloorCount)
                {
                    errors.Add("room '" + room.Id + "' has floor " + room.Floor + " (allowed 0.." + (GrayboxLayout.FloorCount - 1) + ")");
                }
                if (room.IsOutdoor && room.Floor != GrayboxLayout.GroundFloor)
                {
                    errors.Add("outdoor room '" + room.Id + "' must be on the ground floor");
                }
                if (room.Rects.Count == 0)
                {
                    errors.Add("room '" + room.Id + "' has no cell rectangle");
                }
                for (int r = 0; r < room.Rects.Count; r++)
                {
                    if (room.Rects[r].Width <= 0 || room.Rects[r].Depth <= 0)
                    {
                        errors.Add("room '" + room.Id + "' has an empty rectangle " + room.Rects[r]);
                    }
                }
            }
        }

        private static void CheckOverlaps(GrayboxLayout layout, List<string> errors)
        {
            var reported = new HashSet<string>();
            var cells = new List<GrayboxCell>();
            for (int floor = 0; floor < GrayboxLayout.FloorCount; floor++)
            {
                var owner = new Dictionary<GrayboxCell, int>();
                for (int i = 0; i < layout.Rooms.Count; i++)
                {
                    GrayboxRoom room = layout.Rooms[i];
                    if (room.Floor != floor)
                    {
                        continue;
                    }
                    cells.Clear();
                    room.AppendCellsTo(cells);
                    for (int c = 0; c < cells.Count; c++)
                    {
                        int other;
                        if (!owner.TryGetValue(cells[c], out other))
                        {
                            owner.Add(cells[c], i);
                            continue;
                        }

                        string a = layout.Rooms[other].Id;
                        string b = room.Id;
                        string key = floor + ":" + a + ":" + b;
                        if (reported.Add(key))
                        {
                            string who = other == i ? "room '" + b + "' covers cell " + cells[c] + " twice"
                                : "rooms '" + a + "' and '" + b + "' overlap at cell " + cells[c];
                            errors.Add("floor " + floor + ": " + who);
                        }
                    }
                }
            }
        }

        // ---- doors ----------------------------------------------------------------------------------

        private static void CheckDoors(GrayboxLayout layout, List<string> errors)
        {
            var edges = new HashSet<GrayboxEdge>();
            for (int i = 0; i < layout.Doors.Count; i++)
            {
                GrayboxDoor door = layout.Doors[i];
                if (door.Floor < 0 || door.Floor >= GrayboxLayout.FloorCount)
                {
                    errors.Add(door + ": floor " + door.Floor + " does not exist");
                    continue;
                }
                if (!edges.Add(door.Edge))
                {
                    errors.Add(door + ": two doors on the same border");
                }

                GrayboxRoom a = layout.RoomAt(door.Floor, door.CellA);
                GrayboxRoom b = layout.RoomAt(door.Floor, door.CellB);
                if (a == null || b == null)
                {
                    errors.Add(door + ": does not connect two rooms (cell " + (a == null ? door.CellA : door.CellB) + " has no room)");
                }
                else if (ReferenceEquals(a, b))
                {
                    errors.Add(door + ": both cells belong to room '" + a.Id + "' (a door needs a wall between two rooms)");
                }
            }
        }

        private static void CheckFrontDoor(GrayboxLayout layout, List<string> errors)
        {
            var front = new List<GrayboxDoor>();
            for (int i = 0; i < layout.Doors.Count; i++)
            {
                if (layout.Doors[i].Kind == GrayboxDoorKind.FrontDoor)
                {
                    front.Add(layout.Doors[i]);
                }
            }

            if (front.Count != 1)
            {
                errors.Add("expected exactly one FrontDoor, found " + front.Count);
                return;
            }

            GrayboxDoor door = front[0];
            GrayboxRoom a = layout.RoomAt(door.Floor, door.CellA);
            GrayboxRoom b = layout.RoomAt(door.Floor, door.CellB);
            if (door.Floor != GrayboxLayout.GroundFloor || a == null || b == null || a.IsOutdoor == b.IsOutdoor)
            {
                errors.Add("the FrontDoor must connect an outdoor room and an indoor room on the ground floor");
            }
        }

        // ---- stairs ---------------------------------------------------------------------------------

        private static void CheckStairs(GrayboxLayout layout, List<string> errors)
        {
            bool hasUpperRooms = false;
            for (int i = 0; i < layout.Rooms.Count; i++)
            {
                hasUpperRooms |= layout.Rooms[i].Floor == GrayboxLayout.UpperFloor;
            }
            if (hasUpperRooms && layout.Stairs.Count == 0)
            {
                errors.Add("the layout has upper-floor rooms but no staircase");
            }

            for (int s = 0; s < layout.Stairs.Count; s++)
            {
                GrayboxStairs stairs = layout.Stairs[s];
                string label = stairs.ToString();

                GrayboxRoom stairRoom = null;
                bool lowerOk = true;
                for (int c = 0; c < stairs.Cells.Count; c++)
                {
                    GrayboxCell cell = stairs.Cells[c];
                    GrayboxRoom lower = layout.RoomAt(GrayboxLayout.GroundFloor, cell);
                    if (lower == null)
                    {
                        errors.Add(label + ": cell " + cell + " is not inside a ground-floor room");
                        lowerOk = false;
                    }
                    else if (stairRoom == null)
                    {
                        stairRoom = lower;
                    }
                    else if (!ReferenceEquals(stairRoom, lower))
                    {
                        errors.Add(label + ": stair cells span rooms '" + stairRoom.Id + "' and '" + lower.Id + "'");
                        lowerOk = false;
                    }

                    GrayboxRoom upper = layout.RoomAt(GrayboxLayout.UpperFloor, cell);
                    if (upper != null)
                    {
                        errors.Add(label + ": shaft cell " + cell + " is covered by upper-floor room '" + upper.Id + "'");
                    }
                }

                if (lowerOk && stairRoom != null && stairRoom.IsOutdoor)
                {
                    errors.Add(label + ": the stairs stand in outdoor room '" + stairRoom.Id + "'");
                }

                GrayboxRoom arrival = layout.RoomAt(GrayboxLayout.UpperFloor, stairs.Arrival);
                if (arrival == null)
                {
                    errors.Add(label + ": arrival cell " + stairs.Arrival + " is not inside an upper-floor room");
                }

                GrayboxRoom entrance = layout.RoomAt(GrayboxLayout.GroundFloor, stairs.Entrance);
                if (entrance == null)
                {
                    errors.Add(label + ": entrance cell " + stairs.Entrance + " is not inside a ground-floor room");
                }
                else if (stairRoom != null && ReferenceEquals(entrance, stairRoom))
                {
                    errors.Add(label + ": entrance cell " + stairs.Entrance + " is in the stairwell room itself");
                }

                GrayboxDoor gate = layout.FindDoor(stairs.EntranceEdge);
                if (gate == null || gate.Kind != GrayboxDoorKind.StageGate)
                {
                    errors.Add(label + ": the entrance border " + stairs.EntranceEdge + " needs a StageGate door");
                }
            }
        }

        private static void CheckRailings(GrayboxLayout layout, List<string> errors)
        {
            for (int i = 0; i < layout.Railings.Count; i++)
            {
                GrayboxRailing railing = layout.Railings[i];
                if (railing.Floor != GrayboxLayout.UpperFloor)
                {
                    errors.Add(railing + ": railings belong on the upper floor");
                    continue;
                }

                bool voidA = layout.IsStairVoid(railing.Floor, railing.CellA);
                bool voidB = layout.IsStairVoid(railing.Floor, railing.CellB);
                GrayboxCell roomCell = voidA ? railing.CellB : railing.CellA;
                if (voidA == voidB)
                {
                    errors.Add(railing + ": must lie between the stair shaft and a room cell");
                }
                else if (layout.RoomAt(railing.Floor, roomCell) == null)
                {
                    errors.Add(railing + ": cell " + roomCell + " has no room");
                }

                for (int s = 0; s < layout.Stairs.Count; s++)
                {
                    if (layout.Stairs[s].ArrivalEdge.Equals(railing.Edge))
                    {
                        errors.Add(railing + ": blocks the stairs arrival border");
                    }
                }
                if (layout.FindDoor(railing.Edge) != null)
                {
                    errors.Add(railing + ": shares its border with a door");
                }
            }
        }

        // ---- graph rules ----------------------------------------------------------------------------

        private static void CheckReachability(GrayboxLayout layout, List<string> errors)
        {
            GrayboxDoor front = null;
            for (int i = 0; i < layout.Doors.Count; i++)
            {
                if (layout.Doors[i].Kind == GrayboxDoorKind.FrontDoor)
                {
                    front = layout.Doors[i];
                    break;
                }
            }
            if (front == null)
            {
                return; // already reported by CheckFrontDoor
            }

            int count = layout.Rooms.Count;
            var adjacency = new List<int>[count];
            for (int i = 0; i < count; i++)
            {
                adjacency[i] = new List<int>();
            }

            for (int i = 0; i < layout.Doors.Count; i++)
            {
                GrayboxDoor door = layout.Doors[i];
                int a = layout.RoomIndexAt(door.Floor, door.CellA);
                int b = layout.RoomIndexAt(door.Floor, door.CellB);
                if (a >= 0 && b >= 0 && a != b)
                {
                    adjacency[a].Add(b);
                    adjacency[b].Add(a);
                }
            }
            for (int i = 0; i < layout.Stairs.Count; i++)
            {
                int low = layout.RoomIndexAt(GrayboxLayout.GroundFloor, layout.Stairs[i].Start);
                int high = layout.RoomIndexAt(GrayboxLayout.UpperFloor, layout.Stairs[i].Arrival);
                if (low >= 0 && high >= 0)
                {
                    adjacency[low].Add(high);
                    adjacency[high].Add(low);
                }
            }

            int start = layout.RoomIndexAt(front.Floor, front.CellA);
            int other = layout.RoomIndexAt(front.Floor, front.CellB);
            if (start >= 0 && other >= 0 && !layout.Rooms[start].IsOutdoor && layout.Rooms[other].IsOutdoor)
            {
                start = other;
            }
            if (start < 0)
            {
                start = other;
            }
            if (start < 0)
            {
                return;
            }

            var visited = new bool[count];
            var queue = new Queue<int>();
            visited[start] = true;
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                int node = queue.Dequeue();
                for (int n = 0; n < adjacency[node].Count; n++)
                {
                    int next = adjacency[node][n];
                    if (!visited[next])
                    {
                        visited[next] = true;
                        queue.Enqueue(next);
                    }
                }
            }

            for (int i = 0; i < count; i++)
            {
                if (!visited[i])
                {
                    errors.Add("room '" + layout.Rooms[i].Id + "' is unreachable from the front door");
                }
            }
        }

        /// <summary>
        /// Every floor needs at least one loop (escape route, GDD 9): the door graph of its indoor rooms must have
        /// more doors than a spanning forest needs, i.e. doors - rooms + components >= 1.
        /// </summary>
        private static void CheckLoops(GrayboxLayout layout, List<string> errors)
        {
            for (int floor = 0; floor < GrayboxLayout.FloorCount; floor++)
            {
                var parent = new Dictionary<int, int>();
                for (int i = 0; i < layout.Rooms.Count; i++)
                {
                    if (layout.Rooms[i].Floor == floor && !layout.Rooms[i].IsOutdoor)
                    {
                        parent.Add(i, i);
                    }
                }
                if (parent.Count == 0)
                {
                    continue;
                }

                int nodes = parent.Count;
                int components = nodes;
                int edges = 0;
                for (int i = 0; i < layout.Doors.Count; i++)
                {
                    GrayboxDoor door = layout.Doors[i];
                    if (door.Floor != floor)
                    {
                        continue;
                    }
                    int a = layout.RoomIndexAt(floor, door.CellA);
                    int b = layout.RoomIndexAt(floor, door.CellB);
                    if (a < 0 || b < 0 || a == b || !parent.ContainsKey(a) || !parent.ContainsKey(b))
                    {
                        continue;
                    }

                    edges++;
                    int rootA = Find(parent, a);
                    int rootB = Find(parent, b);
                    if (rootA != rootB)
                    {
                        parent[rootA] = rootB;
                        components--;
                    }
                }

                int loops = edges - nodes + components;
                if (loops < 1)
                {
                    errors.Add("floor " + floor + " has no loop in its door graph (" + edges + " doors, "
                        + nodes + " rooms, " + components + " groups)");
                }
            }
        }

        private static int Find(Dictionary<int, int> parent, int node)
        {
            while (parent[node] != node)
            {
                parent[node] = parent[parent[node]];
                node = parent[node];
            }
            return node;
        }

        // ---- props ----------------------------------------------------------------------------------

        private static void CheckProps(GrayboxLayout layout, List<string> errors)
        {
            for (int i = 0; i < layout.Props.Count; i++)
            {
                GrayboxProp prop = layout.Props[i];
                GrayboxRoom room = layout.RoomAt(GrayboxLayout.GroundFloor, prop.Cell);
                if (prop.Kind == GrayboxPropKind.Tree)
                {
                    if (room != null)
                    {
                        errors.Add(prop + ": trees stand outside every room (found in '" + room.Id + "')");
                    }
                }
                else if (room == null || !room.IsOutdoor)
                {
                    errors.Add(prop + ": must stand inside the garden");
                }
            }
        }
    }
}
