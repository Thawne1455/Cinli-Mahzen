namespace CinliMahzen.Possession
{
    /// <summary>Why a jinn left an object (payload of <c>PossessEnded</c>). Append only.</summary>
    public enum PossessEndReason : byte
    {
        /// <summary>Space / E.</summary>
        Voluntary = 0,
        /// <summary>Good jinn exorcism.</summary>
        Exorcised = 1,
        /// <summary>Salt zone placed over the object.</summary>
        Salted = 2,
        /// <summary>Single-use action consumed the object.</summary>
        Spent = 3,
        /// <summary>Round end, object removed, debug.</summary>
        Forced = 4,
    }
}
