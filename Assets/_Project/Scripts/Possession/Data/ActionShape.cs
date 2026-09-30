namespace CinliMahzen.Possession
{
    /// <summary>
    /// Authority hit-check shape (Tech §6.3). ShapeSize meaning:
    /// Box = full size (x width, y height, z depth); Sphere = x radius; Cone = x range, y full angle (deg);
    /// Projectile = x hit radius (speed/range/arc in the Projectile* fields); Custom = behaviour-defined.
    /// Serialized as int — append only.
    /// </summary>
    public enum ActionShape
    {
        Box = 0,
        Sphere = 1,
        Cone = 2,
        Projectile = 3,
        Custom = 4,
    }
}
