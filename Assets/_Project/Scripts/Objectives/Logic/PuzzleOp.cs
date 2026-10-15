using CinliMahzen.Core;

namespace CinliMahzen.Objectives
{
    /// <summary>
    /// One human operation on a puzzle (Teknik §7.1). Immutable; build it with the factories.
    /// The factories do not validate — <see cref="PuzzleState.TryApply"/> does (range, allowed kind, lock).
    /// A default(PuzzleOp) has Kind == None and is always rejected.
    /// </summary>
    public readonly struct PuzzleOp
    {
        private PuzzleOp(PuzzleOpKind kind, int part, int value, int partB)
        {
            Kind = kind;
            Part = part;
            Value = value;
            PartB = partB;
        }

        /// <summary>Exactly one flag: Cycle, Set or Swap (None for default(PuzzleOp)).</summary>
        public PuzzleOpKind Kind { get; }

        /// <summary>Target part (Swap: the first part).</summary>
        public int Part { get; }

        /// <summary>Cycle: direction (+1 / -1). Set: the new value. Swap: unused (0).</summary>
        public int Value { get; }

        /// <summary>Swap: the second part. Otherwise unused (0).</summary>
        public int PartB { get; }

        /// <summary>Step <paramref name="part"/> by <paramref name="dir"/> (+1 next value, -1 previous value; wraps).</summary>
        public static PuzzleOp Cycle(int part, int dir)
        {
            return new PuzzleOp(PuzzleOpKind.Cycle, part, dir, 0);
        }

        /// <summary>Set <paramref name="part"/> to <paramref name="value"/>.</summary>
        public static PuzzleOp Set(int part, int value)
        {
            return new PuzzleOp(PuzzleOpKind.Set, part, value, 0);
        }

        /// <summary>Exchange the values of parts <paramref name="a"/> and <paramref name="b"/>.</summary>
        public static PuzzleOp Swap(int a, int b)
        {
            return new PuzzleOp(PuzzleOpKind.Swap, a, 0, b);
        }

        public override string ToString()
        {
            switch (Kind)
            {
                case PuzzleOpKind.Cycle:
                    return "Cycle(part " + Part + ", dir " + Value + ")";
                case PuzzleOpKind.Set:
                    return "Set(part " + Part + ", value " + Value + ")";
                case PuzzleOpKind.Swap:
                    return "Swap(parts " + Part + " <-> " + PartB + ")";
                default:
                    return "None";
            }
        }
    }
}
