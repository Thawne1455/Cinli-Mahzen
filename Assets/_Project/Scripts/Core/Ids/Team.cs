namespace CinliMahzen.Core
{
    /// <summary>Team a role belongs to (Teknik §4.1). Seekers = Human + GoodJinn.</summary>
    public enum Team : byte
    {
        None = 0,
        Seekers = 1,
        Jinns = 2,
    }
}
