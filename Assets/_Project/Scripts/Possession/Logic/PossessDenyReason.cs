namespace CinliMahzen.Possession
{
    /// <summary>
    /// Why <see cref="PossessionArbiterCore"/> refused a possession request (payload of <c>PossessDenied</c>).
    /// Values are serialized as a byte on the wire — append only, never renumber or reuse.
    /// </summary>
    public enum PossessDenyReason : byte
    {
        None = 0,
        // Rule 1 — player
        /// <summary>Human / unknown role (only evil and good jinns can possess).</summary>
        RoleNotAllowed = 1,
        Stunned = 2,
        // Rule 2 — match phase
        NotPlaying = 3,
        JinnsAsleep = 4,
        // Rule 3 — object
        InvalidTarget = 5,
        Spent = 6,
        Occupied = 7,
        // 8, 9 retired (v1 bless/salt) — do not reuse
        // Rule 5 — distance
        OutOfRange = 10,
        // Rule 6 — per player/object re-enter cooldown
        Cooldown = 11,
        // Rule 6 — player already inside something
        AlreadyPossessing = 12,
        // v2.1
        /// <summary>Rule 4: evil jinn tried to enter an object in a lit room (lamps are exempt).</summary>
        Lit = 13,
        /// <summary>Rule 1: good jinn tried to enter an object that is not flagged <c>AllowGoodJinn</c>.</summary>
        GoodJinnNotAllowed = 14,
    }
}
