namespace CinliMahzen.Core
{
    /// <summary>State of a single lamp (Oyun §7, Teknik §4.8). Broken lamps only come back via the good jinn's repair. Wire value — do not reorder.</summary>
    public enum LampState : byte
    {
        Off = 0,
        On = 1,
        Broken = 2,
    }
}
