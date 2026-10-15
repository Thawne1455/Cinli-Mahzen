using System;

namespace CinliMahzen.Core
{
    /// <summary>
    /// Operations a human can perform on a puzzle (Teknik §7.1). Flags: a definition lists every kind it allows
    /// (Books = Swap | Cycle); a single operation (PuzzleOp) always carries exactly one flag.
    /// Serialized in PZ_*.asset — do not change values; append new flags at the end.
    /// </summary>
    [Flags]
    public enum PuzzleOpKind : byte
    {
        None = 0,
        /// <summary>Step one part to its next/previous value (wraps).</summary>
        Cycle = 1,
        /// <summary>Set one part to an explicit value.</summary>
        Set = 2,
        /// <summary>Exchange the values of two parts.</summary>
        Swap = 4,
    }
}
