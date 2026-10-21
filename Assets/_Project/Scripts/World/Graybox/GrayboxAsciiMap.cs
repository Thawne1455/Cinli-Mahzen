using System.Text;

namespace CinliMahzen.World.Graybox
{
    /// <summary>
    /// Text picture of one floor of a layout, north at the top. Each cell is 3 characters wide and shows the room
    /// symbol; borders show what the planner builds: walls | and ---, fence ~, railing =, doors d (open), D (door),
    /// G (stage gate), F (front door), V (cellar hatch). Stair shaft cells show an arrow in the climbing direction.
    /// </summary>
    public static class GrayboxAsciiMap
    {
        private const int CellChars = 3;
        private const int ColumnsPerCell = CellChars + 1;

        public static string Render(GrayboxLayout layout, int floor)
        {
            int minX, minZ, maxX, maxZ;
            layout.GetCellBounds(out minX, out minZ, out maxX, out maxZ);
            int width = maxX - minX + 1;
            int depth = maxZ - minZ + 1;
            int cols = width * ColumnsPerCell + 1;
            int rows = depth * 2 + 1;

            var grid = new char[rows, cols];
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    grid[r, c] = ' ';
                }
            }

            for (int r = 0; r < rows; r += 2)
            {
                for (int c = 0; c < cols; c += ColumnsPerCell)
                {
                    grid[r, c] = '+';
                }
            }

            for (int z = minZ; z <= maxZ; z++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    var cell = new GrayboxCell(x, z);
                    GrayboxRoom room = layout.RoomAt(floor, cell);
                    char symbol = ' ';
                    if (room != null)
                    {
                        symbol = room.Symbol;
                    }
                    else if (layout.IsStairVoid(floor, cell))
                    {
                        symbol = StairArrow(layout, cell);
                    }
                    grid[(z - minZ) * 2 + 1, (x - minX) * ColumnsPerCell + 2] = symbol;
                }
            }

            var segments = GrayboxWallPlanner.PlanWalls(layout);
            for (int i = 0; i < segments.Count; i++)
            {
                GrayboxWallSegment segment = segments[i];
                if (segment.Edge.Floor != floor)
                {
                    continue;
                }
                if (segment.Edge.RunsAlongX)
                {
                    int row = (segment.Edge.Z + 1 - minZ) * 2;
                    int col = (segment.Edge.X - minX) * ColumnsPerCell + 1;
                    DrawHorizontal(grid, row, col, segment);
                }
                else
                {
                    int row = (segment.Edge.Z - minZ) * 2 + 1;
                    int col = (segment.Edge.X + 1 - minX) * ColumnsPerCell;
                    grid[row, col] = SegmentChar(segment, '|');
                }
            }

            var sb = new StringBuilder();
            for (int r = rows - 1; r >= 0; r--)
            {
                for (int c = 0; c < cols; c++)
                {
                    sb.Append(grid[r, c]);
                }
                sb.Append('\n');
            }
            return sb.ToString();
        }

        /// <summary>One line per room: "H = hall (floor 0, stage 1)".</summary>
        public static string Legend(GrayboxLayout layout)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < layout.Rooms.Count; i++)
            {
                GrayboxRoom room = layout.Rooms[i];
                sb.Append(room.Symbol).Append(" = ").Append(room.Id)
                    .Append(" (floor ").Append(room.Floor).Append(", stage ").Append(room.Stage).Append(")\n");
            }
            return sb.ToString();
        }

        private static void DrawHorizontal(char[,] grid, int row, int col, GrayboxWallSegment segment)
        {
            char fill = SegmentChar(segment, '-');
            if (segment.Kind == GrayboxWallKind.Door)
            {
                grid[row, col] = '-';
                grid[row, col + 1] = fill;
                grid[row, col + 2] = '-';
                return;
            }
            for (int i = 0; i < CellChars; i++)
            {
                grid[row, col + i] = fill;
            }
        }

        private static char SegmentChar(GrayboxWallSegment segment, char wallChar)
        {
            switch (segment.Kind)
            {
                case GrayboxWallKind.Fence: return '~';
                case GrayboxWallKind.Railing: return '=';
                case GrayboxWallKind.Door: return DoorChar(segment.Door.Kind);
                default: return wallChar;
            }
        }

        private static char DoorChar(GrayboxDoorKind kind)
        {
            switch (kind)
            {
                case GrayboxDoorKind.Open: return 'd';
                case GrayboxDoorKind.StageGate: return 'G';
                case GrayboxDoorKind.FrontDoor: return 'F';
                case GrayboxDoorKind.CellarHatch: return 'V';
                default: return 'D';
            }
        }

        private static char StairArrow(GrayboxLayout layout, GrayboxCell cell)
        {
            for (int i = 0; i < layout.Stairs.Count; i++)
            {
                if (layout.Stairs[i].Contains(cell))
                {
                    return layout.Stairs[i].Direction.Arrow();
                }
            }
            return ' ';
        }
    }
}
