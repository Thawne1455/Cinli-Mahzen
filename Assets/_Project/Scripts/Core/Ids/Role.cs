namespace CinliMahzen.Core
{
    /// <summary>Player role in a round (Teknik §4.1). Values are serialized — do not reorder.</summary>
    public enum Role : byte
    {
        None = 0,
        Human = 1,
        GoodJinn = 2,
        EvilJinn = 3,
    }
}
