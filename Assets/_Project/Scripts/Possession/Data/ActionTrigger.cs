namespace CinliMahzen.Possession
{
    /// <summary>How a possessed object's action is triggered (Tech §6.3). Serialized as int — append only.</summary>
    public enum ActionTrigger
    {
        /// <summary>Click → telegraph (TelegraphTime) → resolve.</summary>
        Press = 0,
        /// <summary>Hold to charge for TelegraphTime (barrel, keg); release early cancels.</summary>
        HoldToCharge = 1,
        /// <summary>Click toggles an armed trap (mimic); resolves when a human triggers it.</summary>
        ToggleArm = 2,
    }
}
