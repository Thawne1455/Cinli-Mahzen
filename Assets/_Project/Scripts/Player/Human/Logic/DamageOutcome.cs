namespace CinliMahzen.Player
{
    /// <summary>
    /// What <see cref="HumanHealthCore.ApplyDamage"/> did with a hit. Values are stable — append only.
    /// </summary>
    public enum DamageOutcome : byte
    {
        /// <summary>No state change: human is fainted/lost, invulnerable, or the amount was not positive. Broadcast nothing.</summary>
        Ignored = 0,

        /// <summary>HP dropped but stayed above 0. Broadcast <c>HumanDamaged</c>.</summary>
        Damaged = 1,

        /// <summary>HP hit 0 and the human is down (a revive is pending). Broadcast <c>HumanDamaged</c> then <c>HumanFainted</c>.</summary>
        Fainted = 2,

        /// <summary>HP hit 0 on the faint that reached <c>FaintsToLose</c>: the human never gets up again.
        /// Broadcast <c>HumanDamaged</c> then <c>HumanFainted</c> (the score still counts it), then end the round (<c>JinnsWin(Faint)</c>).</summary>
        Lost = 3,
    }
}
