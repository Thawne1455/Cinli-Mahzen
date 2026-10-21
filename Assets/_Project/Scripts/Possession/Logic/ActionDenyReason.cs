namespace CinliMahzen.Possession
{
    /// <summary>Why an action or hop request was refused (payload of <c>ActionDenied</c>, TODO 1.8). Byte on the wire — append only.</summary>
    public enum ActionDenyReason : byte
    {
        None = 0,
        /// <summary>The requester is not inside an object.</summary>
        NotInside = 1,
        /// <summary>The object is still entering, charging or recovering.</summary>
        Busy = 2,
        /// <summary>The object has no action with that index (or no movement mode for a hop).</summary>
        NoSuchAction = 3,
        /// <summary>The requester's role may not use this action (e.g. the good jinn and a harmful action).</summary>
        NotUsable = 4,
        Cooldown = 5,
        NotEnoughEnergy = 6,
        /// <summary>Hop: nothing to move (zero direction) or a wall right in front.</summary>
        Blocked = 7,
    }
}
