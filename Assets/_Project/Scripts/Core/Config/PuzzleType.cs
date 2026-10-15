namespace CinliMahzen.Core
{
    /// <summary>Puzzle kinds (Oyun §4.3, Teknik §7.1). Serialized in PZ_*.asset — do not reorder; append new values at the end.</summary>
    public enum PuzzleType : byte
    {
        /// <summary>P1 — 3 portraits x 4 rotations (Cycle).</summary>
        Paintings = 0,
        /// <summary>P2 — statue: 6 surrounding objects + 3 staff tilts (Cycle).</summary>
        Statue = 1,
        /// <summary>P3 — talisman switch: 4 sockets x 6 village motifs (Cycle), 2 trap motifs on the reference.</summary>
        Wires = 2,
        /// <summary>P4 — seal stones: 12 lines x painted/empty (Set, needs paint), reference may be rotated.</summary>
        Sigil = 3,
        /// <summary>P5 — books &amp; fireplace: 5-book order + 1 page of 4 (Swap | Cycle). Later.</summary>
        Books = 4,
        /// <summary>P6 — jigsaw: 6 pieces permutation (Swap). Later.</summary>
        Jigsaw = 5,
    }
}
