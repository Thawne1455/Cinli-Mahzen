namespace CinliMahzen.Core
{
    /// <summary>Timed status effects (Teknik §4.5). PD_*.asset files store the int values — never reorder.</summary>
    public enum StatusType : byte
    {
        None = 0,
        Knockdown = 1,
        Grabbed = 2,
        Drunk = 3,
        Darkness = 4,
        Slowed = 5,
        Stunned = 6,
    }
}
