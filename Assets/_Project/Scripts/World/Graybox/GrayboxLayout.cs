using System.Collections.Generic;

namespace CinliMahzen.World.Graybox
{
    /// <summary>
    /// The hand-authored graybox house (TODO 1.9, v1 TODO 4.1): rooms, doors, stairs, railings and garden props as plain data.
    /// There is no generator here (CLAUDE.md section 5): the layout is fixed, the code only turns it into prefabs.
    /// Edit <see cref="CreateDefault"/> to move walls and doors, then run CinliMahzen/House/Build Graybox.
    ///
    /// Grid: cell (0,0) is the south-west corner of the house, X grows east, Z grows north, the front door faces south (-Z).
    /// The house is 5 x 4 cells (20 x 16 m) on four levels: cellar (-1), ground floor (0), first floor (1) and attic (2); the garden is
    /// 9 x 8 cells (36 x 32 m) around it.
    /// </summary>
    public sealed class GrayboxLayout
    {
        /// <summary>Grid cell edge length in metres (04_Asset_Eslestirme.md section 1).</summary>
        public const float CellSize = 4f;

        /// <summary>Floor to floor height in metres; also the wall height.</summary>
        public const float FloorHeight = 4f;

        public const int CellarFloor = -1;
        public const int GroundFloor = 0;
        public const int UpperFloor = 1;
        public const int AtticFloor = 2;

        /// <summary>Lowest and highest floor of the layout (HouseContract.MinFloor / MaxFloor).</summary>
        public const int MinFloor = CellarFloor;
        public const int MaxFloor = AtticFloor;
        public const int FloorCount = MaxFloor - MinFloor + 1;

        private static GrayboxLayout _default;

        /// <summary>The shipped layout. Immutable, safe to share.</summary>
        public static GrayboxLayout Default
        {
            get { return _default ?? (_default = CreateDefault()); }
        }

        public IReadOnlyList<GrayboxRoom> Rooms { get; }
        public IReadOnlyList<GrayboxDoor> Doors { get; }
        public IReadOnlyList<GrayboxRailing> Railings { get; }
        public IReadOnlyList<GrayboxStairs> Stairs { get; }
        public IReadOnlyList<GrayboxProp> Props { get; }

        public GrayboxLayout(IEnumerable<GrayboxRoom> rooms, IEnumerable<GrayboxDoor> doors,
            IEnumerable<GrayboxRailing> railings, IEnumerable<GrayboxStairs> stairs, IEnumerable<GrayboxProp> props)
        {
            Rooms = new List<GrayboxRoom>(rooms);
            Doors = new List<GrayboxDoor>(doors);
            Railings = new List<GrayboxRailing>(railings);
            Stairs = new List<GrayboxStairs>(stairs);
            Props = new List<GrayboxProp>(props);
        }

        /// <summary>Index into <see cref="Rooms"/> of the room that owns the cell on that floor, or -1.</summary>
        public int RoomIndexAt(int floor, GrayboxCell cell)
        {
            for (int i = 0; i < Rooms.Count; i++)
            {
                if (Rooms[i].Floor == floor && Rooms[i].Contains(cell))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>The room that owns the cell on that floor, or null (outside the layout or inside a stair shaft).</summary>
        public GrayboxRoom RoomAt(int floor, GrayboxCell cell)
        {
            int index = RoomIndexAt(floor, cell);
            return index < 0 ? null : Rooms[index];
        }

        public GrayboxRoom FindRoom(string id)
        {
            for (int i = 0; i < Rooms.Count; i++)
            {
                if (Rooms[i].Id == id)
                {
                    return Rooms[i];
                }
            }
            return null;
        }

        public GrayboxDoor FindDoor(GrayboxEdge edge)
        {
            for (int i = 0; i < Doors.Count; i++)
            {
                if (Doors[i].Edge.Equals(edge))
                {
                    return Doors[i];
                }
            }
            return null;
        }

        public GrayboxRailing FindRailing(GrayboxEdge edge)
        {
            for (int i = 0; i < Railings.Count; i++)
            {
                if (Railings[i].Edge.Equals(edge))
                {
                    return Railings[i];
                }
            }
            return null;
        }

        /// <summary>True when the cell is part of an open stair shaft on that floor (the floor the stairs arrive on).</summary>
        public bool IsStairVoid(int floor, GrayboxCell cell)
        {
            for (int i = 0; i < Stairs.Count; i++)
            {
                if (Stairs[i].TopFloor == floor && Stairs[i].Contains(cell))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Inclusive cell bounds over every room rectangle.</summary>
        public void GetCellBounds(out int minX, out int minZ, out int maxX, out int maxZ)
        {
            minX = int.MaxValue;
            minZ = int.MaxValue;
            maxX = int.MinValue;
            maxZ = int.MinValue;
            for (int i = 0; i < Rooms.Count; i++)
            {
                for (int r = 0; r < Rooms[i].Rects.Count; r++)
                {
                    GrayboxRect rect = Rooms[i].Rects[r];
                    if (rect.CellCount == 0)
                    {
                        continue;
                    }
                    minX = System.Math.Min(minX, rect.X);
                    minZ = System.Math.Min(minZ, rect.Z);
                    maxX = System.Math.Max(maxX, rect.X + rect.Width - 1);
                    maxZ = System.Math.Max(maxZ, rect.Z + rect.Depth - 1);
                }
            }
            if (minX == int.MaxValue)
            {
                minX = minZ = maxX = maxZ = 0;
            }
        }

        // ------------------------------------------------------------------------------------------------
        // The hand-made house. Draw it with CinliMahzen/House/Print Graybox ASCII Map (GrayboxAsciiMap).
        // ------------------------------------------------------------------------------------------------
        private static GrayboxLayout CreateDefault()
        {
            const int C = CellarFloor;
            const int G = GroundFloor;
            const int U = UpperFloor;
            const int A = AtticFloor;
            const GrayboxFloorStyle Wood = GrayboxFloorStyle.Wood;
            const GrayboxFloorStyle Stone = GrayboxFloorStyle.Stone;

            var rooms = new List<GrayboxRoom>
            {
                // Stage 0: garden (front yard + west strip + east strip + back strip) and the tool shed in its north-east corner.
                new GrayboxRoom("garden", "Garden", '.', G, 0, true, Stone,
                    new GrayboxRect(-2, -3, 9, 3), new GrayboxRect(-2, 0, 2, 5),
                    new GrayboxRect(5, 0, 2, 4), new GrayboxRect(0, 4, 6, 1)),
                new GrayboxRoom("shed", "Shed", 'A', G, 0, false, Wood, new GrayboxRect(6, 4, 1, 1)),

                // Stage 1: ground floor. Hall in front, kitchen west, living room east, the stairs (one cell, 4 m run =
                // one floor) straight ahead of the hall behind the stage gate, pantry at the back linking kitchen and living
                // room (this is the floor's loop). The cell between stairwell and pantry is the hole of the cellar stairs:
                // the cellar hatch opens from the pantry.
                new GrayboxRoom("hall", "Hall", 'H', G, 1, false, Wood, new GrayboxRect(1, 0, 3, 1)),
                new GrayboxRoom("kitchen", "Kitchen", 'M', G, 1, false, Wood,
                    new GrayboxRect(0, 0, 1, 4), new GrayboxRect(1, 1, 1, 3)),
                new GrayboxRoom("living", "Living", 'O', G, 1, false, Wood,
                    new GrayboxRect(4, 0, 1, 4), new GrayboxRect(3, 1, 1, 3)),
                new GrayboxRoom("pantry", "Pantry", 'K', G, 1, false, Stone, new GrayboxRect(2, 3, 1, 1)),
                new GrayboxRoom("stairwell", "Stairwell", 'S', G, 1, false, Wood, new GrayboxRect(2, 1, 1, 1)),

                // Stage 2: first floor. The stair shaft (2,1) is open; the corridor and the attic stairs look into it over
                // railings, the landing (2,2)-(2,3) sits at the top of the stairs. The attic stairs (1,1) are entered from the
                // corridor through the stage 2 gate. Loop: landing - bedroom1 - bathroom - corridor - bedroom2 - landing.
                new GrayboxRoom("landing", "Landing", 'L', U, 2, false, Wood, new GrayboxRect(2, 2, 1, 2)),
                new GrayboxRoom("bedroom1", "Bedroom1", '1', U, 2, false, Wood, new GrayboxRect(0, 2, 2, 2)),
                new GrayboxRoom("bedroom2", "Bedroom2", '2', U, 2, false, Wood, new GrayboxRect(3, 2, 2, 2)),
                new GrayboxRoom("corridor", "Corridor", 'C', U, 2, false, Wood,
                    new GrayboxRect(1, 0, 3, 1), new GrayboxRect(3, 1, 1, 1)),
                new GrayboxRoom("bathroom", "Bathroom", 'B', U, 2, false, Stone, new GrayboxRect(0, 0, 1, 2)),
                new GrayboxRoom("study", "Study", 'W', U, 2, false, Wood, new GrayboxRect(4, 0, 1, 2)),
                new GrayboxRoom("atticstairs", "AtticStairs", 'T', U, 2, false, Wood, new GrayboxRect(1, 1, 1, 1)),

                // Stage 3: attic (TODO 4.7 turns it on with StagesActive = 3). The loft wraps the attic stair shaft (1,1)
                // behind railings; the ritual room fills the east side, two doors make the loop.
                new GrayboxRoom("attic", "Attic", 'P', A, 3, false, Wood,
                    new GrayboxRect(0, 0, 1, 4), new GrayboxRect(1, 0, 1, 1), new GrayboxRect(1, 2, 1, 2), new GrayboxRect(2, 0, 1, 4)),
                new GrayboxRoom("ritual", "Ritual", 'R', A, 3, false, Wood, new GrayboxRect(3, 0, 2, 4)),

                // The cellar under the front part of the house. It has no stage zone: it opens after the last active stage,
                // through the hatch. The stair cell (2,2) is its own rectangle so its room box reaches up through the hatch hole.
                new GrayboxRoom("cellar", "Cellar", 'Z', C, 0, false, Stone,
                    new GrayboxRect(0, 0, 2, 3), new GrayboxRect(2, 0, 1, 2), new GrayboxRect(2, 2, 1, 1), new GrayboxRect(3, 0, 2, 3))
            };

            var doors = new List<GrayboxDoor>
            {
                // Ground level.
                Door(G, 2, 0, 2, -1, GrayboxDoorKind.FrontDoor),
                Door(G, 6, 4, 6, 3, GrayboxDoorKind.Door),          // shed <-> garden
                Door(G, 1, 0, 0, 0, GrayboxDoorKind.Open),          // hall <-> kitchen
                Door(G, 3, 0, 4, 0, GrayboxDoorKind.Open),          // hall <-> living
                Door(G, 2, 0, 2, 1, GrayboxDoorKind.StageGate),     // hall <-> stairwell (stage 1 gate)
                Door(G, 1, 3, 2, 3, GrayboxDoorKind.Door),          // kitchen <-> pantry
                Door(G, 3, 3, 2, 3, GrayboxDoorKind.Door),          // living <-> pantry
                Door(G, 2, 3, 2, 2, GrayboxDoorKind.CellarHatch),   // pantry <-> cellar stairs (the cellar hatch)

                // First floor.
                Door(U, 1, 3, 2, 3, GrayboxDoorKind.Door),          // bedroom1 <-> landing
                Door(U, 3, 3, 2, 3, GrayboxDoorKind.Door),          // bedroom2 <-> landing
                Door(U, 3, 2, 3, 1, GrayboxDoorKind.Door),          // bedroom2 <-> corridor
                Door(U, 0, 0, 1, 0, GrayboxDoorKind.Door),          // bathroom <-> corridor
                Door(U, 0, 1, 0, 2, GrayboxDoorKind.Door),          // bathroom <-> bedroom1
                Door(U, 4, 0, 3, 0, GrayboxDoorKind.Door),          // study <-> corridor
                Door(U, 4, 1, 4, 2, GrayboxDoorKind.Door),          // study <-> bedroom2
                Door(U, 1, 0, 1, 1, GrayboxDoorKind.StageGate),     // corridor <-> attic stairs (stage 2 gate)

                // Attic.
                Door(A, 2, 0, 3, 0, GrayboxDoorKind.Door),          // attic <-> ritual (south)
                Door(A, 2, 3, 3, 3, GrayboxDoorKind.Door)           // attic <-> ritual (north)
            };

            var railings = new List<GrayboxRailing>
            {
                // First floor: the corridor and the attic stairs look down into the main shaft; the bedrooms keep full walls.
                new GrayboxRailing(U, new GrayboxCell(1, 1), new GrayboxCell(2, 1)),
                new GrayboxRailing(U, new GrayboxCell(3, 1), new GrayboxCell(2, 1)),
                new GrayboxRailing(U, new GrayboxCell(2, 0), new GrayboxCell(2, 1)),

                // Attic: the loft wraps the attic stair shaft.
                new GrayboxRailing(A, new GrayboxCell(0, 1), new GrayboxCell(1, 1)),
                new GrayboxRailing(A, new GrayboxCell(2, 1), new GrayboxCell(1, 1)),
                new GrayboxRailing(A, new GrayboxCell(1, 0), new GrayboxCell(1, 1))
            };

            var stairs = new List<GrayboxStairs>
            {
                new GrayboxStairs(new GrayboxCell(2, 1), GrayboxDirection.North),
                new GrayboxStairs(new GrayboxCell(1, 1), GrayboxDirection.North, U),
                new GrayboxStairs(new GrayboxCell(2, 2), GrayboxDirection.North, C)
            };

            var props = new List<GrayboxProp>();

            // Garden: well in the front-left yard, woodpile beside the east wall, pots flanking the front door.
            props.Add(new GrayboxProp(GrayboxPropKind.Well, -0.5f, -1.5f, 0f));
            props.Add(new GrayboxProp(GrayboxPropKind.Woodpile, 5.5f, 2.5f, 90f));
            props.Add(new GrayboxProp(GrayboxPropKind.Pot, 1.6f, -0.3f, 0f));
            props.Add(new GrayboxProp(GrayboxPropKind.Pot, 3.4f, -0.3f, 0f));
            props.Add(new GrayboxProp(GrayboxPropKind.Pot, -0.4f, -0.4f, 0f));
            props.Add(new GrayboxProp(GrayboxPropKind.Pot, 5.5f, -0.3f, 0f));

            // Trees stand outside the fence (the forest edge): south, north, west and east rows.
            float[] rowX = { -1.5f, 0.5f, 2.5f, 4.5f, 6.5f };
            for (int i = 0; i < rowX.Length; i++)
            {
                props.Add(new GrayboxProp(GrayboxPropKind.Tree, rowX[i], -4.5f, 0f));
                props.Add(new GrayboxProp(GrayboxPropKind.Tree, rowX[i], 6.5f, 0f));
            }
            float[] columnZ = { -2.5f, -0.5f, 1.5f, 3.5f };
            for (int i = 0; i < columnZ.Length; i++)
            {
                props.Add(new GrayboxProp(GrayboxPropKind.Tree, -3.5f, columnZ[i], 0f));
                props.Add(new GrayboxProp(GrayboxPropKind.Tree, 8.5f, columnZ[i], 0f));
            }

            return new GrayboxLayout(rooms, doors, railings, stairs, props);
        }

        private static GrayboxDoor Door(int floor, int ax, int az, int bx, int bz, GrayboxDoorKind kind)
        {
            return new GrayboxDoor(floor, new GrayboxCell(ax, az), new GrayboxCell(bx, bz), kind);
        }
    }
}
