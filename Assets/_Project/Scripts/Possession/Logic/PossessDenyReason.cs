namespace CinliMahzen.Possession
{
    /// <summary>
    /// Why <see cref="PossessionArbiterCore"/> refused a possession request (payload of <c>PossessDenied</c>).
    /// Values are serialized as a byte on the wire — append only, never renumber.
    /// </summary>
    public enum PossessDenyReason : byte
    {
        None = 0,
        // Rule 1 — player
        NotEvilJinn = 1,
        Stunned = 2,
        // Rule 2 — match phase
        NotPlaying = 3,
        JinnsAsleep = 4,
        // Rule 3 — object
        InvalidTarget = 5,
        Spent = 6,
        Occupied = 7,
        Blessed = 8,
        Blocked = 9,
        // Rule 4 — distance
        OutOfRange = 10,
        // Rule 5 — per player/object re-enter cooldown
        Cooldown = 11,
        // Rule 6 — player already inside something
        AlreadyPossessing = 12,
    }
}
