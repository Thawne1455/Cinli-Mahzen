namespace CinliMahzen.Core
{
    /// <summary>Where the seekers are in the round's objective chain (Teknik §4.8). Wire value — do not reorder.</summary>
    public enum ObjectivePhase : byte
    {
        /// <summary>Garden: find the house key, collect the Blessed Oil.</summary>
        Garden = 0,
        /// <summary>House stages: puzzle pairs behind stage gates.</summary>
        House = 1,
        /// <summary>Cellar: 3-layer dig; good jinn's Spirit Block is active.</summary>
        Cellar = 2,
        /// <summary>Treasure dug.</summary>
        Done = 3,
    }
}
