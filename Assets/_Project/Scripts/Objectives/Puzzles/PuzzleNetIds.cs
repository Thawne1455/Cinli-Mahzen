using CinliMahzen.Core;

namespace CinliMahzen.Objectives
{
    /// <summary>
    /// Deterministic NetIds of the interactive pieces of a spawned puzzle (TODO 2.5): parts, confirm lever and scramble target. Every machine
    /// spawns the same puzzles from the same round plan, so a puzzle's <c>ordinal</c> (its index in the plan, 0 for the debug puzzle) gives the
    /// same ids everywhere without a message. Range <see cref="Base"/>..<see cref="Base"/> + <see cref="MaxPuzzles"/> * <see cref="Stride"/>
    /// (30000..39999) — between the house markers (1000 up) and the possessables (40000 up).
    /// </summary>
    public static class PuzzleNetIds
    {
        public const int Base = 30000;
        public const int Stride = 16;
        public const int MaxPuzzles = 625;

        /// <summary>Parts use offsets 0..<see cref="MaxParts"/>-1 of a puzzle's block.</summary>
        public const int MaxParts = 14;

        private const int LeverOffset = 14;
        private const int ScrambleOffset = 15;

        public static bool IsValidOrdinal(int ordinal)
        {
            return ordinal >= 0 && ordinal < MaxPuzzles;
        }

        public static NetId Part(int ordinal, int part)
        {
            return new NetId(Base + ordinal * Stride + part);
        }

        public static NetId Lever(int ordinal)
        {
            return new NetId(Base + ordinal * Stride + LeverOffset);
        }

        public static NetId ScrambleTarget(int ordinal)
        {
            return new NetId(Base + ordinal * Stride + ScrambleOffset);
        }
    }
}
