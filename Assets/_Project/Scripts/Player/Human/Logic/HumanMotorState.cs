namespace CinliMahzen.Player
{
    /// <summary>
    /// What the human body is currently doing (Teknik §5.2). Values are stable — only append.
    /// Fainted is not a status effect (Teknik §4.5): it is this motor state plus HumanFaintedEvt.
    /// </summary>
    public enum HumanMotorState : byte
    {
        Normal = 0,
        /// <summary>Dig / push: the body stays put, the view stays free.</summary>
        Interacting = 1,
        /// <summary>Toppled shelf: no movement until the knockdown time is over.</summary>
        KnockedDown = 2,
        /// <summary>Fainted: neither movement nor looking.</summary>
        Fainted = 3,
    }
}
