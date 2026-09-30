namespace CinliMahzen.Core
{
    /// <summary>
    /// Possession state as seen by other modules (PossessionChangedEvt). Values mirror
    /// CinliMahzen.Possession.PossessableState so B can cast directly — keep them in sync.
    /// </summary>
    public enum PossessionPhase : byte
    {
        Free = 0,
        Entering = 1,
        Lurking = 2,
        Charging = 3,
        Recovering = 4,
        Spent = 5,
    }
}
