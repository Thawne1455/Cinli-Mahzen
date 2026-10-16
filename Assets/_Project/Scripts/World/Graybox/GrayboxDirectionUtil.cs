namespace CinliMahzen.World.Graybox
{
    /// <summary>Helpers for <see cref="GrayboxDirection"/>.</summary>
    public static class GrayboxDirectionUtil
    {
        public const int DirectionCount = 4;

        public static int DeltaX(this GrayboxDirection dir)
        {
            switch (dir)
            {
                case GrayboxDirection.East: return 1;
                case GrayboxDirection.West: return -1;
                default: return 0;
            }
        }

        public static int DeltaZ(this GrayboxDirection dir)
        {
            switch (dir)
            {
                case GrayboxDirection.North: return 1;
                case GrayboxDirection.South: return -1;
                default: return 0;
            }
        }

        public static GrayboxDirection Opposite(this GrayboxDirection dir)
        {
            return (GrayboxDirection)(((int)dir + 2) % DirectionCount);
        }

        /// <summary>Rotation about +Y (degrees) that turns local +Z into this direction.</summary>
        public static float YawDegrees(this GrayboxDirection dir)
        {
            return 90f * (int)dir;
        }

        /// <summary>One-character arrow for ASCII maps.</summary>
        public static char Arrow(this GrayboxDirection dir)
        {
            switch (dir)
            {
                case GrayboxDirection.North: return '^';
                case GrayboxDirection.East: return '>';
                case GrayboxDirection.South: return 'v';
                default: return '<';
            }
        }
    }
}
