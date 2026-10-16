using System;

namespace CinliMahzen.World.Graybox
{
    /// <summary>
    /// One 4 m grid cell. Cell (0,0) covers world X 0..4, Z 0..4 (house local space). X grows east, Z grows north.
    /// </summary>
    public readonly struct GrayboxCell : IEquatable<GrayboxCell>
    {
        public readonly int X;
        public readonly int Z;

        public GrayboxCell(int x, int z)
        {
            X = x;
            Z = z;
        }

        public GrayboxCell Step(GrayboxDirection dir)
        {
            return new GrayboxCell(X + dir.DeltaX(), Z + dir.DeltaZ());
        }

        public GrayboxCell Step(GrayboxDirection dir, int count)
        {
            return new GrayboxCell(X + dir.DeltaX() * count, Z + dir.DeltaZ() * count);
        }

        public bool IsAdjacentTo(GrayboxCell other)
        {
            return Math.Abs(X - other.X) + Math.Abs(Z - other.Z) == 1;
        }

        public bool Equals(GrayboxCell other)
        {
            return X == other.X && Z == other.Z;
        }

        public override bool Equals(object obj)
        {
            return obj is GrayboxCell other && Equals(other);
        }

        public override int GetHashCode()
        {
            return unchecked(X * 397 ^ Z);
        }

        public override string ToString()
        {
            return "(" + X + "," + Z + ")";
        }
    }
}
