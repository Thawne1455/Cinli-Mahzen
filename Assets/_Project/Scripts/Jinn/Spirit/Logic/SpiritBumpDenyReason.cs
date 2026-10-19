namespace CinliMahzen.Jinn
{
    /// <summary>
    /// Why the authority refused a Spirit Bump request (Teknik §6.1). Log/debug only — it is not sent over the network.
    /// Values are stable; append new ones at the end.
    /// </summary>
    public enum SpiritBumpDenyReason : byte
    {
        /// <summary>Accepted.</summary>
        None = 0,
        /// <summary>The requester is not an evil jinn.</summary>
        RequesterNotEvil = 1,
        /// <summary>The target is not the good jinn.</summary>
        TargetNotGood = 2,
        /// <summary>The good jinn is not focusing a reference (authoritative flag).</summary>
        TargetNotFocusing = 3,
        /// <summary>The requester's <c>SpiritBumpCooldown</c> has not passed.</summary>
        Cooldown = 4,
        /// <summary>Farther than <c>SpiritBumpRange + NetRangeTolerance</c> (or the distance is unknown / not a number).</summary>
        OutOfRange = 5,
        /// <summary>The authority has no pawn for the requester or the target, so it cannot measure the distance.</summary>
        NoPawn = 6,
        /// <summary>The call reached a machine that is not the authority.</summary>
        NotAuthority = 7,
    }
}
