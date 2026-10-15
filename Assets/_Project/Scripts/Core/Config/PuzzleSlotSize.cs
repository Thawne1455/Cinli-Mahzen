namespace CinliMahzen.Core
{
    /// <summary>
    /// Physical size class of a puzzle / reference slot (Teknik §7.2, §8): a PuzzleDefinition only fits slots of the same size.
    /// Serialized in PZ_*.asset and slot markers — do not reorder; append new values at the end.
    /// </summary>
    public enum PuzzleSlotSize : byte
    {
        Wall = 0,
        Floor = 1,
        Table = 2,
    }
}
