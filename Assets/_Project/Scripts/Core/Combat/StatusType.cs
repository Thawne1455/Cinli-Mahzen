namespace CinliMahzen.Core
{
    /// <summary>
    /// Timed status effects (Teknik §4.5). PD_*.asset files store the int values — never reorder, only append.
    /// Grabbed / Drunk / Darkness are v1 leftovers kept for value stability; v2.1 does not apply them.
    /// </summary>
    public enum StatusType : byte
    {
        None = 0,
        Knockdown = 1,
        Grabbed = 2,
        Drunk = 3,
        Darkness = 4,
        Slowed = 5,
        Stunned = 6,
        /// <summary>Mother's Slipper hit: human's look yaw turns 180° in 0.15 s.</summary>
        CameraSpin180 = 7,
        /// <summary>Spirit Bump: good jinn's view blurs/shakes for SpiritBumpBlurTime.</summary>
        VisionBlurred = 8,
    }
}
