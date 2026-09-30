namespace CinliMahzen.Possession
{
    /// <summary>
    /// Object-side possession state (Tech §6.2). "Blessed" is not a state: it is a timer overlay
    /// kept by <see cref="PossessableStateMachine"/> (a Free object may be blessed).
    /// </summary>
    public enum PossessableState : byte
    {
        Free = 0,
        /// <summary>A jinn is entering (PossessTime, object shakes + creaks).</summary>
        Entering = 1,
        /// <summary>Possessed, idle ("sinsi" mode).</summary>
        Lurking = 2,
        /// <summary>Possessed, an action is telegraphing / charging.</summary>
        Charging = 3,
        /// <summary>Possessed, the action resolved and its result animation is playing.</summary>
        Recovering = 4,
        /// <summary>Single-use action consumed; can never be possessed again this round.</summary>
        Spent = 5,
    }
}
