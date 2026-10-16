using System;

namespace CinliMahzen.World.Graybox
{
    /// <summary>
    /// The border between two neighbouring cells on one floor, stored in a canonical form so the same wall is never
    /// described twice. A wall is centred on this border (grid line) and is 4 m long, 1 m thick.
    /// </summary>
    public readonly struct GrayboxEdge : IEquatable<GrayboxEdge>
    {
        public readonly int Floor;

        /// <summary>The south/west cell of the pair.</summary>
        public readonly int X;
        public readonly int Z;

        /// <summary>
        /// True: the wall runs along world X and separates (X,Z) from (X,Z+1).
        /// False: the wall runs along world Z and separates (X,Z) from (X+1,Z).
        /// </summary>
        public readonly bool RunsAlongX;

        private GrayboxEdge(int floor, int x, int z, bool runsAlongX)
        {
            Floor = floor;
            X = x;
            Z = z;
            RunsAlongX = runsAlongX;
        }

        public GrayboxCell LowCell
        {
            get { return new GrayboxCell(X, Z); }
        }

        public GrayboxCell HighCell
        {
            get { return RunsAlongX ? new GrayboxCell(X, Z + 1) : new GrayboxCell(X + 1, Z); }
        }

        /// <summary>Wall centre in cell units (multiply by the cell size for metres).</summary>
        public float CenterCellX
        {
            get { return RunsAlongX ? X + 0.5f : X + 1f; }
        }

        public float CenterCellZ
        {
            get { return RunsAlongX ? Z + 1f : Z + 0.5f; }
        }

        /// <summary>The two grid-vertex indices (corner points) at the ends of this border.</summary>
        public void GetVertices(out int ax, out int az, out int bx, out int bz)
        {
            if (RunsAlongX)
            {
                ax = X;
                az = Z + 1;
                bx = X + 1;
                bz = Z + 1;
            }
            else
            {
                ax = X + 1;
                az = Z;
                bx = X + 1;
                bz = Z + 1;
            }
        }

        /// <summary>The border between two adjacent cells. Throws when the cells are not neighbours.</summary>
        public static GrayboxEdge Between(int floor, GrayboxCell a, GrayboxCell b)
        {
            if (a.Step(GrayboxDirection.East).Equals(b))
            {
                return new GrayboxEdge(floor, a.X, a.Z, false);
            }
            if (b.Step(GrayboxDirection.East).Equals(a))
            {
                return new GrayboxEdge(floor, b.X, b.Z, false);
            }
            if (a.Step(GrayboxDirection.North).Equals(b))
            {
                return new GrayboxEdge(floor, a.X, a.Z, true);
            }
            if (b.Step(GrayboxDirection.North).Equals(a))
            {
                return new GrayboxEdge(floor, b.X, b.Z, true);
            }
            throw new ArgumentException("Cells " + a + " and " + b + " are not adjacent.");
        }

        public bool Equals(GrayboxEdge other)
        {
            return Floor == other.Floor && X == other.X && Z == other.Z && RunsAlongX == other.RunsAlongX;
        }

        public override bool Equals(object obj)
        {
            return obj is GrayboxEdge other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Floor;
                hash = hash * 397 ^ X;
                hash = hash * 397 ^ Z;
                hash = hash * 397 ^ (RunsAlongX ? 1 : 0);
                return hash;
            }
        }

        public override string ToString()
        {
            return "f" + Floor + " " + LowCell + "|" + HighCell;
        }
    }
}
