using System;
using System.Collections.Generic;

namespace CinliMahzen.World.Graybox
{
    /// <summary>
    /// Checks a <see cref="GrayboxLayout"/> against the house rules (GDD 9, 3.1). Returns one readable message per
    /// problem; an empty list means the layout is valid.
    ///
    /// Rules: rooms are well formed and do not overlap on a floor; every door joins two different rooms; exactly one
    /// front door (garden to house); every staircase sits in a room of its base floor, arrives in a room of the floor above,
    /// leaves its shaft open above and is entered through a stage gate (the cellar stairs instead end at the single cellar
    /// hatch); every level is linked by stairs; railings only line a shaft; every room is reachable from the front door
    /// (stairs included); every floor with several rooms has at least one loop in its door graph; garden props stand where
    /// they belong.
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

                if (room.Floor < GrayboxLayout.MinFloor || room.Floor > GrayboxLayout.MaxFloor)
                {
                    errors.Add("room '" + room.Id + "' has floor " + room.Floor + " (allowed " + GrayboxLayout.MinFloor + ".." + GrayboxLayout.MaxFloor + ")");
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
            for (int floor = GrayboxLayout.MinFloor; floor <= GrayboxLayout.MaxFloor; floor++)
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
                if (door.Floor < GrayboxLayout.MinFloor || door.Floor > GrayboxLayout.MaxFloor)
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
                if (door.Kind == GrayboxDoorKind.CellarHatch)
                {
                    // The hatch joins a room and the top of the cellar stairs (checked in CheckStairs).
                    if (!IsCellarArrival(layout, door.Edge) || (a == null) == (b == null))
                    {
                        errors.Add(door + ": a CellarHatch must sit on the arrival border of the cellar stairs, between the shaft and a room");
                    }
                    continue;
                }
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
            // Every level other than the ground floor is reached by a staircase that arrives on it (or leaves it, for the cellar).
            for (int floor = GrayboxLayout.MinFloor; floor <= GrayboxLayout.MaxFloor; floor++)
            {
                if (floor == GrayboxLayout.GroundFloor || !HasRoomsOn(layout, floor))
                {
                    continue;
                }
                bool linked = false;
                for (int s = 0; s < layout.Stairs.Count; s++)
                {
                    linked |= floor > GrayboxLayout.GroundFloor ? layout.Stairs[s].TopFloor == floor : layout.Stairs[s].BaseFloor == floor;
                }
                if (!linked)
                {
                    errors.Add("the layout has rooms on floor " + floor + " but no staircase to them");
                }
            }

            for (int s = 0; s < layout.Stairs.Count; s++)
            {
                GrayboxStairs stairs = layout.Stairs[s];
                string label = stairs.ToString();
                if (stairs.BaseFloor < GrayboxLayout.MinFloor || stairs.TopFloor > GrayboxLayout.MaxFloor)
                {
                    errors.Add(label + ": climbs from floor " + stairs.BaseFloor + ", outside the layout's floors");
                    continue;
                }

                GrayboxRoom stairRoom = null;
                bool lowerOk = true;
                for (int c = 0; c < stairs.Cells.Count; c++)
                {
                    GrayboxCell cell = stairs.Cells[c];
                    GrayboxRoom lower = layout.RoomAt(stairs.BaseFloor, cell);
                    if (lower == null)
                    {
                        errors.Add(label + ": cell " + cell + " is not inside a room of floor " + stairs.BaseFloor);
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

                    GrayboxRoom upper = layout.RoomAt(stairs.TopFloor, cell);
                    if (upper != null)
                    {
                        errors.Add(label + ": shaft cell " + cell + " is covered by floor " + stairs.TopFloor + " room '" + upper.Id + "'");
                    }
                }

                if (lowerOk && stairRoom != null && stairRoom.IsOutdoor)
                {
                    errors.Add(label + ": the stairs stand in outdoor room '" + stairRoom.Id + "'");
                }

                GrayboxRoom arrival = layout.RoomAt(stairs.TopFloor, stairs.Arrival);
                if (arrival == null)
                {
                    errors.Add(label + ": arrival cell " + stairs.Arrival + " is not inside a room of floor " + stairs.TopFloor);
                }

                if (stairs.IsCellarStairs)
                {
                    // Down to the cellar: the hatch closes the top, the bottom opens straight into the cellar.
                    GrayboxDoor hatch = layout.FindDoor(stairs.ArrivalEdge);
                    if (hatch == null || hatch.Kind != GrayboxDoorKind.CellarHatch)
                    {
                        errors.Add(label + ": the arrival border " + stairs.ArrivalEdge + " needs a CellarHatch door");
                    }
                    continue;
                }

                GrayboxRoom entrance = layout.RoomAt(stairs.BaseFloor, stairs.Entrance);
                if (entrance == null)
                {
                    errors.Add(label + ": entrance cell " + stairs.Entrance + " is not inside a room of floor " + stairs.BaseFloor);
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

            int hatches = 0;
            for (int i = 0; i < layout.Doors.Count; i++)
            {
                hatches += layout.Doors[i].Kind == GrayboxDoorKind.CellarHatch ? 1 : 0;
            }
            if (hatches > 1)
            {
                errors.Add("expected at most one CellarHatch, found " + hatches);
            }
        }

        private static bool HasRoomsOn(GrayboxLayout layout, int floor)
        {
            for (int i = 0; i < layout.Rooms.Count; i++)
            {
                if (layout.Rooms[i].Floor == floor)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsCellarArrival(GrayboxLayout layout, GrayboxEdge edge)
        {
            for (int i = 0; i < layout.Stairs.Count; i++)
            {
                if (layout.Stairs[i].IsCellarStairs && layout.Stairs[i].ArrivalEdge.Equals(edge))
                {
                    return true;
                }
            }
            return false;
        }

        private static void CheckRailings(GrayboxLayout layout, List<string> errors)
        {
            for (int i = 0; i < layout.Railings.Count; i++)
            {
                GrayboxRailing railing = layout.Railings[i];
                if (railing.Floor <= GrayboxLayout.GroundFloor || railing.Floor > GrayboxLayout.MaxFloor)
                {
                    errors.Add(railing + ": railings belong on the upper floors");
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
                int low = layout.RoomIndexAt(layout.Stairs[i].BaseFloor, layout.Stairs[i].Start);
                int high = layout.RoomIndexAt(layout.Stairs[i].TopFloor, layout.Stairs[i].Arrival);
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
        /// more doors than a spanning forest needs, i.e. doors - rooms + components >= 1. A floor that is a single open room
        /// (the cellar) has nothing to loop around and is exempt.
        /// </summary>
        private static void CheckLoops(GrayboxLayout layout, List<string> errors)
        {
            for (int floor = GrayboxLayout.MinFloor; floor <= GrayboxLayout.MaxFloor; floor++)
            {
                var parent = new Dictionary<int, int>();
                for (int i = 0; i < layout.Rooms.Count; i++)
                {
                    if (layout.Rooms[i].Floor == floor && !layout.Rooms[i].IsOutdoor)
                    {
                        parent.Add(i, i);
                    }
                }
                if (parent.Count <= 1)
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
