namespace CinliMahzen.Player
{
    /// <summary>Result of one <see cref="InteractHoldTracker.Update"/> frame. Local only, never on the wire.</summary>
    public enum HoldStep : byte
    {
        Idle = 0,
        Holding = 1,
        Completed = 2,
        Cancelled = 3,
    }
}
