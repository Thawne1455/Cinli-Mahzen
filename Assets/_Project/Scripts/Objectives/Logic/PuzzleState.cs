using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CinliMahzen.Core;

namespace CinliMahzen.Objectives
{
    /// <summary>
    /// Generic puzzle model (Teknik §7.1), plain C#: N parts, part i holds a value in [0, K_i).
    /// Every puzzle type (Paintings, Statue, Wires, Sigil, Books, Jigsaw) is reduced to this.
    /// <code>
    /// start = solved ──TryScrambleAll (evil jinn)──► scrambled ──TryApply (human)──► ... ──► IsSolved ──TryLock──► Locked
    ///      ▲                                                                                                   │
    ///      └────────────────────────── ResetToSolution (round start) ◄──────────────────────────────────────┘
    /// </code>
    /// This class is only the rules of the state. Authority checks (role, range, light, lantern, energy, cooldown,
    /// required item) belong to PuzzleSystem. On the authority every mutation goes Request → TryApply/TryScrambleAll/TryLock
    /// → broadcast; clients mirror the broadcast with <see cref="ApplySnapshot"/>. Invariant: IsLocked implies IsSolved.
    /// Hot-path methods (Get, IsSolved, TryApply) do not allocate.
    /// </summary>
    public sealed class PuzzleState
    {
        /// <summary>Valid reference rotations are multiples of this (degrees): 0, 90, 180, 270.</summary>
        public const int RotationStep = 90;

        private const int FullTurn = 360;
        private const int CycleForward = 1;
        private const int CycleBackward = -1;

        private readonly int[] _valueCounts;
        private readonly int[] _solution;
        private readonly int[] _trapValues;
        private readonly int[] _values;
        private readonly ReadOnlyCollection<int> _solutionView;
        private readonly ReadOnlyCollection<int> _trapValuesView;
        private readonly bool _isPermutation;

        /// <summary>
        /// Creates a puzzle that starts SOLVED (Oyun §4.2) and unlocked. All arrays are copied.
        /// </summary>
        /// <param name="valueCounts">K per part (each &gt;= 2); its length is the part count.</param>
        /// <param name="solution">The answer: one value in [0, K_i) per part.</param>
        /// <param name="allowedOps">Operation kinds the human may use (not None, known flags only).
        /// Exactly <see cref="PuzzleOpKind.Swap"/> makes it a permutation puzzle (Jigsaw): every K must equal the part count
        /// and the solution must be a permutation of 0..N-1. Mixed kinds such as Books (Swap | Cycle) are plain per-part puzzles.</param>
        /// <param name="referenceRotation">Rotation of the reference view in degrees: 0, 90, 180 or 270 (Sigil).</param>
        /// <param name="trapValues">Trap values shown with an X on the reference (Wires), each in [0, max K); null = none.</param>
        /// <exception cref="ArgumentException">Any input is invalid (also null arrays).</exception>
        public PuzzleState(int[] valueCounts, int[] solution, PuzzleOpKind allowedOps, int referenceRotation = 0, int[] trapValues = null)
        {
            if (valueCounts == null || valueCounts.Length == 0)
            {
                throw new ArgumentException("valueCounts is null or empty", nameof(valueCounts));
            }
            if (solution == null || solution.Length != valueCounts.Length)
            {
                throw new ArgumentException("solution is null or its length differs from valueCounts", nameof(solution));
            }
            if (!PuzzleDefinition.IsValidOpKinds(allowedOps))
            {
                throw new ArgumentException("allowedOps must be a non-empty combination of Cycle, Set and Swap", nameof(allowedOps));
            }
            if (referenceRotation < 0 || referenceRotation >= FullTurn || referenceRotation % RotationStep != 0)
            {
                throw new ArgumentException("referenceRotation must be 0, 90, 180 or 270", nameof(referenceRotation));
            }

            int count = valueCounts.Length;
            int maxValueCount = 0;
            for (int i = 0; i < count; i++)
            {
                int k = valueCounts[i];
                if (k < PuzzleDefinition.MinValueCount)
                {
                    throw new ArgumentException("valueCounts[" + i + "] = " + k + " (must be >= " + PuzzleDefinition.MinValueCount + ")", nameof(valueCounts));
                }
                if (solution[i] < 0 || solution[i] >= k)
                {
                    throw new ArgumentException("solution[" + i + "] = " + solution[i] + " is outside [0, " + k + ")", nameof(solution));
                }
                if (k > maxValueCount)
                {
                    maxValueCount = k;
                }
            }

            _isPermutation = allowedOps == PuzzleOpKind.Swap;
            if (_isPermutation)
            {
                var seen = new bool[count];
                for (int i = 0; i < count; i++)
                {
                    if (valueCounts[i] != count)
                    {
                        throw new ArgumentException("permutation puzzle needs valueCounts[" + i + "] == part count (" + count + ")", nameof(valueCounts));
                    }
                    if (seen[solution[i]])
                    {
                        throw new ArgumentException("permutation puzzle needs a solution without repeated values", nameof(solution));
                    }
                    seen[solution[i]] = true;
                }
            }

            int trapCount = trapValues != null ? trapValues.Length : 0;
            _trapValues = new int[trapCount];
            for (int i = 0; i < trapCount; i++)
            {
                if (trapValues[i] < 0 || trapValues[i] >= maxValueCount)
                {
                    throw new ArgumentException("trapValues[" + i + "] = " + trapValues[i] + " is outside [0, " + maxValueCount + ")", nameof(trapValues));
                }
                _trapValues[i] = trapValues[i];
            }

            _valueCounts = new int[count];
            _solution = new int[count];
            _values = new int[count];
            Array.Copy(valueCounts, _valueCounts, count);
            Array.Copy(solution, _solution, count);
            Array.Copy(solution, _values, count);

            _solutionView = new ReadOnlyCollection<int>(_solution);
            _trapValuesView = new ReadOnlyCollection<int>(_trapValues);
            AllowedOps = allowedOps;
            ReferenceRotation = referenceRotation;
        }

        public int PartCount => _values.Length;

        /// <summary>K of <paramref name="part"/>.</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="part"/> is not a valid part index.</exception>
        public int ValueCount(int part)
        {
            RequirePart(part);
            return _valueCounts[part];
        }

        /// <summary>Current value of <paramref name="part"/>.</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="part"/> is not a valid part index.</exception>
        public int Get(int part)
        {
            RequirePart(part);
            return _values[part];
        }

        /// <summary>The answer (read-only view; never changes).</summary>
        public IReadOnlyList<int> Solution => _solutionView;

        /// <summary>Rotation of the reference view in degrees (0, 90, 180, 270) — Sigil. 0 for the others.</summary>
        public int ReferenceRotation { get; }

        /// <summary>Trap values shown with an X on the reference (Wires: 2 of them). Empty when the puzzle has none. Read-only view.</summary>
        public IReadOnlyList<int> TrapValues => _trapValuesView;

        /// <summary>Operation kinds the human may use.</summary>
        public PuzzleOpKind AllowedOps { get; }

        /// <summary>True when every part equals the solution.</summary>
        public bool IsSolved
        {
            get
            {
                for (int i = 0; i < _values.Length; i++)
                {
                    if (_values[i] != _solution[i])
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        /// <summary>True after a successful <see cref="TryLock"/> (puzzle confirmed): nobody can change it anymore.</summary>
        public bool IsLocked { get; private set; }

        /// <summary>
        /// Absolute Net.Time before which no new mass sabotage is allowed. Plain data owned by PuzzleSystem
        /// (it sets it to Net.Time + ScramblePuzzleCooldown after a sabotage); the state never reads or writes it by itself.
        /// </summary>
        public double ScrambleCooldownUntil { get; set; }

        /// <summary>True while <paramref name="now"/> (Net.Time) is before <see cref="ScrambleCooldownUntil"/>.</summary>
        public bool IsScrambleOnCooldown(double now)
        {
            return now < ScrambleCooldownUntil;
        }

        /// <summary>
        /// Applies one human operation. Returns false (state unchanged) when the puzzle is locked, the op kind is not in
        /// <see cref="AllowedOps"/> (or is None), a part/value is out of range, or the op is meaningless.
        /// <list type="bullet">
        /// <item>Cycle: Value must be +1 or -1; the part wraps inside its own K (K-1 → 0, 0 → K-1).</item>
        /// <item>Set: Value in [0, K). Setting the current value is accepted (true) and changes nothing.</item>
        /// <item>Swap: two different parts; each value must be valid for the other part's K (always true in a permutation puzzle).</item>
        /// </list>
        /// </summary>
        public bool TryApply(in PuzzleOp op)
        {
            if (IsLocked)
            {
                return false;
            }

            PuzzleOpKind kind = op.Kind;
            if ((AllowedOps & kind) == PuzzleOpKind.None)
            {
                return false;
            }

            switch (kind)
            {
                case PuzzleOpKind.Cycle:
                    return ApplyCycle(op.Part, op.Value);
                case PuzzleOpKind.Set:
                    return ApplySet(op.Part, op.Value);
                case PuzzleOpKind.Swap:
                    return ApplySwap(op.Part, op.PartB);
                default:
                    return false;
            }
        }

        /// <summary>
        /// Evil-jinn mass sabotage (Oyun §4.2): puts the puzzle into a random arrangement that is GUARANTEED not solved.
        /// Returns false (state unchanged) when locked.
        /// <list type="bullet">
        /// <item>Permutation puzzles (AllowedOps == Swap, Jigsaw): the solution's values are shuffled, so the result is still a permutation.</item>
        /// <item>All others (also Books): every part gets rng.Range(0, K). For Books this means the first five parts may stop being a permutation.</item>
        /// <item>If the roll equals the solution, a deterministic fix is applied (permutation: swap parts 0 and 1; otherwise part 0 steps +1, wrapping) — no retry loop.</item>
        /// </list>
        /// Same seed and same call order → same result on every machine. This method does NOT check or change
        /// <see cref="ScrambleCooldownUntil"/>; PuzzleSystem does that (and the energy/light/lantern rules).
        /// </summary>
        public bool TryScrambleAll(GameRandom rng)
        {
            if (rng == null)
            {
                throw new ArgumentNullException(nameof(rng));
            }
            if (IsLocked)
            {
                return false;
            }

            if (_isPermutation)
            {
                Array.Copy(_solution, _values, _values.Length);
                rng.Shuffle<int>(_values);
            }
            else
            {
                for (int i = 0; i < _values.Length; i++)
                {
                    _values[i] = rng.Range(0, _valueCounts[i]);
                }
            }

            if (IsSolved)
            {
                ForceUnsolved();
            }
            return true;
        }

        /// <summary>
        /// Confirms the puzzle: locks it when it is solved and not yet locked. Returns false otherwise, so the authority can
        /// use the result directly to decide between PuzzleCompleted (true) and the confirm-fail penalty (false), and a
        /// second confirm on a locked puzzle never completes it twice.
        /// </summary>
        public bool TryLock()
        {
            if (IsLocked || !IsSolved)
            {
                return false;
            }
            IsLocked = true;
            return true;
        }

        /// <summary>Round start: values = solution, unlocked, <see cref="ScrambleCooldownUntil"/> = 0.</summary>
        public void ResetToSolution()
        {
            Array.Copy(_solution, _values, _values.Length);
            IsLocked = false;
            ScrambleCooldownUntil = 0d;
        }

        /// <summary>Copies the current values into the first <see cref="PartCount"/> entries of <paramref name="buffer"/> (for broadcasts; no allocation).</summary>
        /// <exception cref="ArgumentException">Buffer is null or shorter than <see cref="PartCount"/>.</exception>
        public void CopyValuesTo(int[] buffer)
        {
            if (buffer == null || buffer.Length < _values.Length)
            {
                throw new ArgumentException("buffer is null or shorter than PartCount", nameof(buffer));
            }
            Array.Copy(_values, buffer, _values.Length);
        }

        /// <summary>
        /// Client side: mirrors the authority's broadcast (PuzzleStateChanged / PuzzleScrambled / PuzzleCompleted).
        /// The snapshot is the authority's truth, so it overrides lock state in both directions. It does not carry
        /// <see cref="ScrambleCooldownUntil"/> (set that separately from the message).
        /// Validates everything first and changes nothing when invalid — returns false instead of throwing, because the
        /// input comes from the network. Invalid: wrong length, a value outside [0, K), repeated values in a permutation
        /// puzzle, or <paramref name="locked"/> while the values are not the solution.
        /// </summary>
        public bool ApplySnapshot(int[] values, bool locked)
        {
            if (values == null || values.Length != _values.Length)
            {
                return false;
            }

            bool matchesSolution = true;
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] < 0 || values[i] >= _valueCounts[i])
                {
                    return false;
                }
                if (values[i] != _solution[i])
                {
                    matchesSolution = false;
                }
            }
            if (locked && !matchesSolution)
            {
                return false;
            }
            if (_isPermutation && HasRepeatedValue(values))
            {
                return false;
            }

            Array.Copy(values, _values, values.Length);
            IsLocked = locked;
            return true;
        }

        private bool ApplyCycle(int part, int dir)
        {
            if (!IsValidPart(part) || (dir != CycleForward && dir != CycleBackward))
            {
                return false;
            }

            int k = _valueCounts[part];
            int next = _values[part] + dir;
            if (next < 0)
            {
                next = k - 1;
            }
            else if (next >= k)
            {
                next = 0;
            }
            _values[part] = next;
            return true;
        }

        private bool ApplySet(int part, int value)
        {
            if (!IsValidPart(part) || value < 0 || value >= _valueCounts[part])
            {
                return false;
            }
            _values[part] = value;
            return true;
        }

        private bool ApplySwap(int a, int b)
        {
            if (!IsValidPart(a) || !IsValidPart(b) || a == b)
            {
                return false;
            }

            int valueA = _values[a];
            int valueB = _values[b];
            if (valueB >= _valueCounts[a] || valueA >= _valueCounts[b])
            {
                return false;
            }
            _values[a] = valueB;
            _values[b] = valueA;
            return true;
        }

        /// <summary>Called when a scramble rolled exactly the solution: makes one deterministic change that is never the solution.</summary>
        private void ForceUnsolved()
        {
            if (_isPermutation)
            {
                // Permutation solutions have no repeated values, so exchanging two parts always differs from the solution.
                int first = _values[0];
                _values[0] = _values[1];
                _values[1] = first;
            }
            else
            {
                // K >= 2, so stepping part 0 by one (wrapping) always leaves its solution value.
                int next = _values[0] + CycleForward;
                _values[0] = next >= _valueCounts[0] ? 0 : next;
            }
        }

        private bool IsValidPart(int part)
        {
            return part >= 0 && part < _values.Length;
        }

        private void RequirePart(int part)
        {
            if (!IsValidPart(part))
            {
                throw new ArgumentOutOfRangeException(nameof(part), part, "part must be in [0, " + _values.Length + ")");
            }
        }

        private static bool HasRepeatedValue(int[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = i + 1; j < values.Length; j++)
                {
                    if (values[i] == values[j])
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
