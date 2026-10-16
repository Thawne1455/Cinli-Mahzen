namespace CinliMahzen.World.Graybox
{
    /// <summary>Corner element at a grid vertex. Int values are stable.</summary>
    public enum GrayboxPostKind
    {
        /// <summary>Full-height pillar where at least one full wall ends (Env_Pillar).</summary>
        Pillar = 0,

        /// <summary>Short post where only fences or railings meet (Env_Column).</summary>
        FencePost = 1
    }
}
