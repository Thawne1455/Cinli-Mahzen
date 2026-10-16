using System.Collections.Generic;

namespace CinliMahzen.World.Graybox
{
    /// <summary>Axis-aligned block of cells: Width cells along +X, Depth cells along +Z, starting at (X, Z).</summary>
    public readonly struct GrayboxRect
    {
        public readonly int X;
        public readonly int Z;
        public readonly int Width;
        public readonly int Depth;

        public GrayboxRect(int x, int z, int width, int depth)
        {
            X = x;
            Z = z;
            Width = width;
            Depth = depth;
        }

        public int CellCount
        {
            get { return Width > 0 && Depth > 0 ? Width * Depth : 0; }
        }

        public bool Contains(GrayboxCell cell)
        {
            return cell.X >= X && cell.X < X + Width && cell.Z >= Z && cell.Z < Z + Depth;
        }

        /// <summary>Appends every cell, row by row (Z ascending, then X ascending).</summary>
        public void AppendCellsTo(List<GrayboxCell> cells)
        {
            for (int z = Z; z < Z + Depth; z++)
            {
                for (int x = X; x < X + Width; x++)
                {
                    cells.Add(new GrayboxCell(x, z));
                }
            }
        }

        public override string ToString()
        {
            return "[" + X + "," + Z + " " + Width + "x" + Depth + "]";
        }
    }
}
