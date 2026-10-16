namespace CinliMahzen.World.Graybox
{
    /// <summary>What stands on one cell border. Int values are stable.</summary>
    public enum GrayboxWallKind
    {
        /// <summary>Full-height 4 m wall (Env_Wall).</summary>
        Wall = 0,

        /// <summary>Wall with an opening; see <see cref="GrayboxWallSegment.Door"/>.</summary>
        Door = 1,

        /// <summary>Low garden fence on the outer border of an outdoor room.</summary>
        Fence = 2,

        /// <summary>Low railing around the upper-floor stairwell opening.</summary>
        Railing = 3
    }
}
