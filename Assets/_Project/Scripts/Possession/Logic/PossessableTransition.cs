namespace CinliMahzen.Possession
{
    /// <summary>Timer-driven transitions reported by <see cref="PossessableStateMachine.Tick"/>.</summary>
    public enum PossessableTransition : byte
    {
        None = 0,
        /// <summary>Entering → Lurking. Authority broadcasts <c>PossessCompleted</c>.</summary>
        EnterCompleted = 1,
        /// <summary>Charging reached its resolve time. Authority must run the hit-check and call Resolve.</summary>
        ChargeReady = 2,
        /// <summary>Recovering → Lurking.</summary>
        RecoverFinished = 3,
        /// <summary>Recovering → Spent; the occupant was ejected (single-use action).</summary>
        SpentEjected = 4,
    }
}
