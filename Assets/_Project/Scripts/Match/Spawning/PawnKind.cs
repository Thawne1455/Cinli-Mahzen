namespace CinliMahzen.Match
{
    /// <summary>Which prefab a role is spawned from (<see cref="PawnSpawnPlanner.KindFor"/>). Values may be stored — do not reorder.</summary>
    public enum PawnKind : byte
    {
        None = 0,
        /// <summary>The first-person human (P_Human).</summary>
        Human = 1,
        /// <summary>Either jinn, flying as a spirit (P_Spirit).</summary>
        Spirit = 2,
    }
}
