namespace CinliMahzen.Core
{
    /// <summary>
    /// Evil jinn energy as other modules see it (Teknik §4.8). Implemented by Jinn (next to JinnEnergyCore); consumed by Objectives
    /// (PuzzleSystem pays ScrambleEnergyCost for a mass sabotage) and anything else that charges energy without referencing CM.Jinn.
    /// </summary>
    public interface IJinnEnergyService
    {
        /// <summary>Current energy of <paramref name="player"/>.</summary>
        float Get(PlayerId player);

        /// <summary>
        /// Authority only. Deducts <paramref name="amount"/> and returns true when the player can afford it; returns false and changes
        /// nothing otherwise. A zero or negative amount always succeeds and deducts nothing.
        /// </summary>
        bool TrySpendAuthority(PlayerId player, float amount);
    }
}
