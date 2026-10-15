using System;
using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.Objectives;
using NUnit.Framework;

namespace CinliMahzen.Tests.EditMode.Objectives
{
    /// <summary>PuzzleState rules (Teknik §7.1, TODO 2.1): ops, mass sabotage, lock, cooldown, snapshot.</summary>
    public class PuzzleStateTests
    {
        private const int SeedCount = 1000;
        private const PuzzleOpKind CycleAndSwap = PuzzleOpKind.Cycle | PuzzleOpKind.Swap;

        private static readonly int[] SigilSolution = { 1, 0, 1, 1, 0, 0, 1, 0, 1, 1, 0, 1 };

        // ---------- factories: one per puzzle type (Teknik §7.1 table) ----------

        /// <summary>3 portraits x 4 rotations.</summary>
        private static PuzzleState Paintings()
        {
            return new PuzzleState(new[] { 4, 4, 4 }, new[] { 1, 3, 0 }, PuzzleOpKind.Cycle);
        }

        /// <summary>2 parts: 6 surrounding objects, 3 staff tilts.</summary>
        private static PuzzleState Statue()
        {
            return new PuzzleState(new[] { 6, 3 }, new[] { 2, 1 }, PuzzleOpKind.Cycle);
        }

        /// <summary>4 sockets x 6 motifs, motifs 1 and 4 are traps.</summary>
        private static PuzzleState Wires()
        {
            return new PuzzleState(new[] { 6, 6, 6, 6 }, new[] { 0, 3, 5, 2 }, PuzzleOpKind.Cycle, 0, new[] { 1, 4 });
        }

        /// <summary>12 lines x painted/empty.</summary>
        private static PuzzleState Sigil(int referenceRotation = 0)
        {
            return new PuzzleState(Filled(12, 2), SigilSolution, PuzzleOpKind.Set, referenceRotation);
        }

        /// <summary>5 books (order) + 1 page of 4.</summary>
        private static PuzzleState Books()
        {
            return new PuzzleState(new[] { 5, 5, 5, 5, 5, 4 }, new[] { 2, 0, 4, 1, 3, 2 }, CycleAndSwap);
        }

        /// <summary>6 pieces, permutation.</summary>
        private static PuzzleState Jigsaw()
        {
            return new PuzzleState(Filled(6, 6), new[] { 3, 1, 4, 0, 5, 2 }, PuzzleOpKind.Swap);
        }

        private static readonly Func<PuzzleState>[] AllFactories =
        {
            Paintings, Statue, Wires, () => Sigil(), Books, Jigsaw,
        };

        private static int[] Filled(int length, int value)
        {
            var a = new int[length];
            for (int i = 0; i < length; i++)
            {
                a[i] = value;
            }
            return a;
        }

        private static int[] Values(PuzzleState s)
        {
            var a = new int[s.PartCount];
            for (int i = 0; i < a.Length; i++)
            {
                a[i] = s.Get(i);
            }
            return a;
        }

        private static void AssertValuesInRange(PuzzleState s)
        {
            for (int i = 0; i < s.PartCount; i++)
            {
                Assert.GreaterOrEqual(s.Get(i), 0, "part " + i);
                Assert.Less(s.Get(i), s.ValueCount(i), "part " + i);
            }
        }

        private static void AssertScrambleNeverSolved(Func<PuzzleState> make)
        {
            for (int seed = 0; seed < SeedCount; seed++)
            {
                PuzzleState s = make();
                Assert.IsTrue(s.TryScrambleAll(new GameRandom(seed)), "seed " + seed);
                Assert.IsFalse(s.IsSolved, "seed " + seed + " produced the solved arrangement");
                AssertValuesInRange(s);
            }
        }

        // ---------- start state ----------

        [Test]
        public void StartsSolvedAndUnlocked()
        {
            foreach (Func<PuzzleState> make in AllFactories)
            {
                PuzzleState s = make();
                Assert.IsTrue(s.IsSolved);
                Assert.IsFalse(s.IsLocked);
                Assert.AreEqual(0d, s.ScrambleCooldownUntil);
                for (int i = 0; i < s.PartCount; i++)
                {
                    Assert.AreEqual(s.Solution[i], s.Get(i));
                }
            }
        }

        [Test]
        public void ExposesShapeAndReferenceData()
        {
            PuzzleState wires = Wires();
            Assert.AreEqual(4, wires.PartCount);
            Assert.AreEqual(6, wires.ValueCount(2));
            Assert.AreEqual(PuzzleOpKind.Cycle, wires.AllowedOps);
            CollectionAssert.AreEqual(new[] { 0, 3, 5, 2 }, wires.Solution);
            CollectionAssert.AreEqual(new[] { 1, 4 }, wires.TrapValues);
            Assert.AreEqual(0, wires.ReferenceRotation);

            PuzzleState statue = Statue();
            Assert.AreEqual(6, statue.ValueCount(0));
            Assert.AreEqual(3, statue.ValueCount(1));
            Assert.AreEqual(0, statue.TrapValues.Count);

            PuzzleState sigil = Sigil(270);
            Assert.AreEqual(12, sigil.PartCount);
            Assert.AreEqual(270, sigil.ReferenceRotation);
        }

        // ---------- Cycle ----------

        [Test]
        public void Cycle_Forward_StepsToNextValue()
        {
            PuzzleState s = Paintings();
            Assert.IsTrue(s.TryApply(PuzzleOp.Cycle(0, 1)));
            Assert.AreEqual(2, s.Get(0));
            Assert.AreEqual(3, s.Get(1), "other parts untouched");
            Assert.IsFalse(s.IsSolved);
        }

        [Test]
        public void Cycle_Backward_StepsToPreviousValue()
        {
            PuzzleState s = Paintings();
            Assert.IsTrue(s.TryApply(PuzzleOp.Cycle(1, -1)));
            Assert.AreEqual(2, s.Get(1));
        }

        [Test]
        public void Cycle_WrapsForward_LastValueToZero()
        {
            PuzzleState s = Paintings(); // part 1 holds K-1 = 3
            Assert.IsTrue(s.TryApply(PuzzleOp.Cycle(1, 1)));
            Assert.AreEqual(0, s.Get(1));
        }

        [Test]
        public void Cycle_WrapsBackward_ZeroToLastValue()
        {
            PuzzleState s = Paintings(); // part 2 holds 0
            Assert.IsTrue(s.TryApply(PuzzleOp.Cycle(2, -1)));
            Assert.AreEqual(3, s.Get(2));
        }

        [Test]
        public void Cycle_UsesEachPartsOwnValueCount()
        {
            PuzzleState s = Statue(); // part 0: K=6, part 1: K=3
            s.TryApply(PuzzleOp.Set(0, 5)); // not allowed (Cycle only) — must stay at 2
            Assert.AreEqual(2, s.Get(0));

            Assert.IsTrue(s.TryApply(PuzzleOp.Cycle(1, 1))); // 1 -> 2
            Assert.IsTrue(s.TryApply(PuzzleOp.Cycle(1, 1))); // 2 -> 0 (wraps at 3)
            Assert.AreEqual(0, s.Get(1));

            for (int i = 0; i < 3; i++)
            {
                s.TryApply(PuzzleOp.Cycle(0, 1)); // 2 -> 3 -> 4 -> 5
            }
            Assert.AreEqual(5, s.Get(0));
            s.TryApply(PuzzleOp.Cycle(0, 1)); // 5 -> 0 (wraps at 6)
            Assert.AreEqual(0, s.Get(0));
        }

        [Test]
        public void Cycle_FullTurn_ReturnsToStartAndIsSolved()
        {
            PuzzleState s = Paintings();
            for (int i = 0; i < 4; i++)
            {
                Assert.IsTrue(s.TryApply(PuzzleOp.Cycle(0, 1)));
            }
            Assert.IsTrue(s.IsSolved);
        }

        [TestCase(0)]
        [TestCase(2)]
        [TestCase(-2)]
        public void Cycle_InvalidDirection_Rejected(int dir)
        {
            PuzzleState s = Paintings();
            Assert.IsFalse(s.TryApply(PuzzleOp.Cycle(0, dir)));
            Assert.IsTrue(s.IsSolved);
        }

        [TestCase(-1)]
        [TestCase(3)]
        [TestCase(100)]
        public void Cycle_PartOutOfRange_Rejected(int part)
        {
            PuzzleState s = Paintings();
            Assert.IsFalse(s.TryApply(PuzzleOp.Cycle(part, 1)));
            Assert.IsTrue(s.IsSolved);
        }

        // ---------- Set ----------

        [Test]
        public void Set_ValidValue_ChangesOnlyThatPart()
        {
            PuzzleState s = Sigil(); // SigilSolution[0] == 1
            Assert.IsTrue(s.TryApply(PuzzleOp.Set(0, 0)));
            Assert.AreEqual(0, s.Get(0));
            Assert.AreEqual(SigilSolution[1], s.Get(1));
            Assert.IsFalse(s.IsSolved);
        }

        [Test]
        public void Set_BackToSolutionValue_SolvesAgain()
        {
            PuzzleState s = Sigil();
            s.TryApply(PuzzleOp.Set(4, 1)); // solution is 0
            Assert.IsFalse(s.IsSolved);
            Assert.IsTrue(s.TryApply(PuzzleOp.Set(4, 0)));
            Assert.IsTrue(s.IsSolved);
        }

        [Test]
        public void Set_SameValue_IsAcceptedAndChangesNothing()
        {
            PuzzleState s = Sigil();
            Assert.IsTrue(s.TryApply(PuzzleOp.Set(0, SigilSolution[0])));
            Assert.IsTrue(s.IsSolved);
        }

        [TestCase(-1)]
        [TestCase(2)]
        [TestCase(99)]
        public void Set_ValueOutOfRange_Rejected(int value)
        {
            PuzzleState s = Sigil();
            Assert.IsFalse(s.TryApply(PuzzleOp.Set(0, value)));
            Assert.AreEqual(SigilSolution[0], s.Get(0));
        }

        [TestCase(-1)]
        [TestCase(12)]
        public void Set_PartOutOfRange_Rejected(int part)
        {
            PuzzleState s = Sigil();
            Assert.IsFalse(s.TryApply(PuzzleOp.Set(part, 0)));
            Assert.IsTrue(s.IsSolved);
        }

        // ---------- Swap ----------

        [Test]
        public void Swap_ExchangesTwoValues()
        {
            PuzzleState s = Jigsaw(); // { 3, 1, 4, 0, 5, 2 }
            Assert.IsTrue(s.TryApply(PuzzleOp.Swap(0, 3)));
            CollectionAssert.AreEqual(new[] { 0, 1, 4, 3, 5, 2 }, Values(s));
            Assert.IsFalse(s.IsSolved);
        }

        [Test]
        public void Swap_Twice_RestoresSolution()
        {
            PuzzleState s = Jigsaw();
            Assert.IsTrue(s.TryApply(PuzzleOp.Swap(1, 5)));
            Assert.IsTrue(s.TryApply(PuzzleOp.Swap(5, 1)));
            Assert.IsTrue(s.IsSolved);
        }

        [Test]
        public void Swap_SamePart_Rejected()
        {
            PuzzleState s = Jigsaw();
            Assert.IsFalse(s.TryApply(PuzzleOp.Swap(2, 2)));
            Assert.IsTrue(s.IsSolved);
        }

        [TestCase(-1, 2)]
        [TestCase(2, -1)]
        [TestCase(6, 2)]
        [TestCase(2, 6)]
        public void Swap_PartOutOfRange_Rejected(int a, int b)
        {
            PuzzleState s = Jigsaw();
            Assert.IsFalse(s.TryApply(PuzzleOp.Swap(a, b)));
            Assert.IsTrue(s.IsSolved);
        }

        [Test]
        public void Swap_ValueThatDoesNotFitOtherPart_Rejected()
        {
            PuzzleState s = Books(); // { 2, 0, 4, 1, 3, 2 }; part 5 is the page (K = 4)
            Assert.IsFalse(s.TryApply(PuzzleOp.Swap(2, 5)), "value 4 does not fit K = 4");
            CollectionAssert.AreEqual(new[] { 2, 0, 4, 1, 3, 2 }, Values(s));
        }

        [Test]
        public void Swap_ValueThatFitsOtherPart_Accepted()
        {
            PuzzleState s = Books();
            Assert.IsTrue(s.TryApply(PuzzleOp.Swap(1, 5))); // 0 <-> 2, both fit
            Assert.AreEqual(2, s.Get(1));
            Assert.AreEqual(0, s.Get(5));
        }

        // ---------- op filtering ----------

        [Test]
        public void DisallowedOpKinds_AreRejectedAndChangeNothing()
        {
            PuzzleState paintings = Paintings();
            Assert.IsFalse(paintings.TryApply(PuzzleOp.Set(0, 0)));
            Assert.IsFalse(paintings.TryApply(PuzzleOp.Swap(0, 1)));
            Assert.IsTrue(paintings.IsSolved);

            PuzzleState sigil = Sigil();
            Assert.IsFalse(sigil.TryApply(PuzzleOp.Cycle(0, 1)));
            Assert.IsFalse(sigil.TryApply(PuzzleOp.Swap(0, 1)));
            Assert.IsTrue(sigil.IsSolved);

            PuzzleState jigsaw = Jigsaw();
            Assert.IsFalse(jigsaw.TryApply(PuzzleOp.Cycle(0, 1)));
            Assert.IsFalse(jigsaw.TryApply(PuzzleOp.Set(0, 0)));
            Assert.IsTrue(jigsaw.IsSolved);

            PuzzleState books = Books();
            Assert.IsFalse(books.TryApply(PuzzleOp.Set(0, 0)));
            Assert.IsTrue(books.IsSolved);
        }

        [Test]
        public void MixedOpKinds_BooksAcceptsCycleAndSwap()
        {
            PuzzleState books = Books();
            Assert.IsTrue(books.TryApply(PuzzleOp.Cycle(5, 1)));
            Assert.AreEqual(3, books.Get(5));
            Assert.IsTrue(books.TryApply(PuzzleOp.Swap(0, 1)));
            Assert.AreEqual(0, books.Get(0));
            Assert.AreEqual(2, books.Get(1));
        }

        [Test]
        public void DefaultOp_IsRejected()
        {
            foreach (Func<PuzzleState> make in AllFactories)
            {
                PuzzleState s = make();
                Assert.IsFalse(s.TryApply(default(PuzzleOp)));
                Assert.IsTrue(s.IsSolved);
            }
        }

        [Test]
        public void PuzzleOp_FactoriesFillFields()
        {
            PuzzleOp cycle = PuzzleOp.Cycle(2, -1);
            Assert.AreEqual(PuzzleOpKind.Cycle, cycle.Kind);
            Assert.AreEqual(2, cycle.Part);
            Assert.AreEqual(-1, cycle.Value);

            PuzzleOp set = PuzzleOp.Set(7, 1);
            Assert.AreEqual(PuzzleOpKind.Set, set.Kind);
            Assert.AreEqual(7, set.Part);
            Assert.AreEqual(1, set.Value);

            PuzzleOp swap = PuzzleOp.Swap(1, 4);
            Assert.AreEqual(PuzzleOpKind.Swap, swap.Kind);
            Assert.AreEqual(1, swap.Part);
            Assert.AreEqual(4, swap.PartB);

            Assert.AreEqual(PuzzleOpKind.None, default(PuzzleOp).Kind);
        }

        // ---------- IsSolved ----------

        [Test]
        public void IsSolved_FollowsEveryPart()
        {
            PuzzleState s = Wires();
            Assert.IsTrue(s.IsSolved);
            s.TryApply(PuzzleOp.Cycle(3, 1));
            Assert.IsFalse(s.IsSolved);
            s.TryApply(PuzzleOp.Cycle(0, 1));
            Assert.IsFalse(s.IsSolved);
            s.TryApply(PuzzleOp.Cycle(3, -1));
            Assert.IsFalse(s.IsSolved, "part 0 is still off");
            s.TryApply(PuzzleOp.Cycle(0, -1));
            Assert.IsTrue(s.IsSolved);
        }

        // ---------- Lock ----------

        [Test]
        public void TryLock_WhenSolved_Locks()
        {
            PuzzleState s = Paintings();
            Assert.IsTrue(s.TryLock());
            Assert.IsTrue(s.IsLocked);
            Assert.IsTrue(s.IsSolved);
        }

        [Test]
        public void TryLock_WhenNotSolved_Rejected()
        {
            PuzzleState s = Paintings();
            s.TryApply(PuzzleOp.Cycle(0, 1));
            Assert.IsFalse(s.TryLock());
            Assert.IsFalse(s.IsLocked);
        }

        [Test]
        public void TryLock_Twice_SecondCallRejected()
        {
            PuzzleState s = Paintings();
            Assert.IsTrue(s.TryLock());
            Assert.IsFalse(s.TryLock(), "a locked puzzle must not complete twice");
            Assert.IsTrue(s.IsLocked);
        }

        [Test]
        public void Locked_RejectsEveryOpKind()
        {
            PuzzleState books = Books();
            Assert.IsTrue(books.TryLock());
            Assert.IsFalse(books.TryApply(PuzzleOp.Cycle(0, 1)));
            Assert.IsFalse(books.TryApply(PuzzleOp.Swap(0, 1)));
            Assert.IsTrue(books.IsSolved);

            PuzzleState sigil = Sigil();
            Assert.IsTrue(sigil.TryLock());
            Assert.IsFalse(sigil.TryApply(PuzzleOp.Set(0, 0)));
            Assert.IsTrue(sigil.IsSolved);
        }

        [Test]
        public void Locked_RejectsScramble()
        {
            foreach (Func<PuzzleState> make in AllFactories)
            {
                PuzzleState s = make();
                Assert.IsTrue(s.TryLock());
                Assert.IsFalse(s.TryScrambleAll(new GameRandom(1)));
                Assert.IsTrue(s.IsSolved);
                Assert.IsTrue(s.IsLocked);
            }
        }

        [Test]
        public void ResetToSolution_RestoresValuesUnlocksAndClearsCooldown()
        {
            PuzzleState s = Paintings();
            s.TryScrambleAll(new GameRandom(5));
            s.ScrambleCooldownUntil = 99d;
            s.ResetToSolution();
            Assert.IsTrue(s.IsSolved);
            Assert.IsFalse(s.IsLocked);
            Assert.AreEqual(0d, s.ScrambleCooldownUntil);
            Assert.IsFalse(s.IsScrambleOnCooldown(0d));

            Assert.IsTrue(s.TryLock());
            s.ScrambleCooldownUntil = 50d;
            s.ResetToSolution();
            Assert.IsFalse(s.IsLocked);
            Assert.AreEqual(0d, s.ScrambleCooldownUntil);
            Assert.IsTrue(s.TryApply(PuzzleOp.Cycle(0, 1)), "ops work again after the reset");
        }

        // ---------- mass sabotage ----------

        [Test]
        public void Scramble_Paintings_NeverSolved()
        {
            AssertScrambleNeverSolved(Paintings);
        }

        [Test]
        public void Scramble_Statue_NeverSolved()
        {
            AssertScrambleNeverSolved(Statue);
        }

        [Test]
        public void Scramble_Wires_NeverSolved()
        {
            AssertScrambleNeverSolved(Wires);
        }

        [Test]
        public void Scramble_Sigil_NeverSolved()
        {
            AssertScrambleNeverSolved(() => Sigil());
        }

        [Test]
        public void Scramble_Books_NeverSolved()
        {
            AssertScrambleNeverSolved(Books);
        }

        [Test]
        public void Scramble_Jigsaw_NeverSolved()
        {
            AssertScrambleNeverSolved(Jigsaw);
        }

        [Test]
        public void Scramble_SinglePartWithTwoValues_AlwaysTheOtherValue()
        {
            AssertScrambleNeverSolved(() => new PuzzleState(new[] { 2 }, new[] { 0 }, PuzzleOpKind.Cycle));
            AssertScrambleNeverSolved(() => new PuzzleState(new[] { 2 }, new[] { 1 }, PuzzleOpKind.Set));

            for (int seed = 0; seed < SeedCount; seed++)
            {
                var s = new PuzzleState(new[] { 2 }, new[] { 0 }, PuzzleOpKind.Cycle);
                s.TryScrambleAll(new GameRandom(seed));
                Assert.AreEqual(1, s.Get(0), "seed " + seed);
            }
        }

        [Test]
        public void Scramble_AllZeroSolution_NeverSolved()
        {
            AssertScrambleNeverSolved(() => new PuzzleState(Filled(12, 2), new int[12], PuzzleOpKind.Set));
            AssertScrambleNeverSolved(() => new PuzzleState(new[] { 4, 4, 4 }, new[] { 0, 0, 0 }, PuzzleOpKind.Cycle));
        }

        [Test]
        public void Scramble_AllMaxSolution_NeverSolved()
        {
            AssertScrambleNeverSolved(() => new PuzzleState(new[] { 4, 4, 4 }, new[] { 3, 3, 3 }, PuzzleOpKind.Cycle));
        }

        [Test]
        public void Scramble_TwoPartPermutation_NeverSolved()
        {
            // 50 % of the shuffles are the identity, so this exercises the deterministic fix a lot.
            AssertScrambleNeverSolved(() => new PuzzleState(new[] { 2, 2 }, new[] { 0, 1 }, PuzzleOpKind.Swap));
            AssertScrambleNeverSolved(() => new PuzzleState(new[] { 2, 2 }, new[] { 1, 0 }, PuzzleOpKind.Swap));
        }

        [Test]
        public void Scramble_FromAnyCurrentArrangement_NeverSolved()
        {
            PuzzleState s = Paintings();
            for (int seed = 0; seed < SeedCount; seed++)
            {
                s.TryApply(PuzzleOp.Cycle(seed % s.PartCount, 1)); // humans fiddling in between
                Assert.IsTrue(s.TryScrambleAll(new GameRandom(seed)));
                Assert.IsFalse(s.IsSolved, "seed " + seed);
            }
        }

        [Test]
        public void Scramble_Permutation_StaysAPermutation()
        {
            for (int seed = 0; seed < SeedCount; seed++)
            {
                PuzzleState s = Jigsaw();
                s.TryScrambleAll(new GameRandom(seed));
                int[] sorted = Values(s);
                Array.Sort(sorted);
                CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 5 }, sorted, "seed " + seed);
            }
        }

        [Test]
        public void Scramble_Permutation_IgnoresPreviousArrangement()
        {
            // Same seed from a solved and from a swapped state: the result only depends on the seed.
            PuzzleState a = Jigsaw();
            PuzzleState b = Jigsaw();
            b.TryApply(PuzzleOp.Swap(0, 5));
            a.TryScrambleAll(new GameRandom(42));
            b.TryScrambleAll(new GameRandom(42));
            CollectionAssert.AreEqual(Values(a), Values(b));
        }

        [Test]
        public void Scramble_SameSeed_SameArrangement()
        {
            foreach (Func<PuzzleState> make in AllFactories)
            {
                for (int seed = 0; seed < 50; seed++)
                {
                    PuzzleState a = make();
                    PuzzleState b = make();
                    a.TryScrambleAll(new GameRandom(seed));
                    b.TryScrambleAll(new GameRandom(seed));
                    CollectionAssert.AreEqual(Values(a), Values(b), "seed " + seed);
                }
            }
        }

        [Test]
        public void Scramble_DifferentSeeds_ProduceDifferentArrangements()
        {
            var seeds = new GameRandom(2024); // decorrelated seeds, consecutive System.Random seeds start out similar
            var seen = new HashSet<string>();
            for (int i = 0; i < 50; i++)
            {
                PuzzleState s = Sigil();
                s.TryScrambleAll(new GameRandom(seeds.Range(0, int.MaxValue)));
                seen.Add(string.Join(",", Values(s)));
            }
            Assert.Greater(seen.Count, 10);
        }

        [Test]
        public void Scramble_ReachesEveryValueOfEveryPart()
        {
            // Not a cheap +1 shift: over many seeds each part of the paintings takes all 4 rotations.
            var seeds = new GameRandom(77);
            var hit = new bool[3, 4];
            for (int i = 0; i < SeedCount; i++)
            {
                PuzzleState s = Paintings();
                s.TryScrambleAll(new GameRandom(seeds.Range(0, int.MaxValue)));
                for (int part = 0; part < 3; part++)
                {
                    hit[part, s.Get(part)] = true;
                }
            }
            for (int part = 0; part < 3; part++)
            {
                for (int value = 0; value < 4; value++)
                {
                    Assert.IsTrue(hit[part, value], "part " + part + " never reached " + value);
                }
            }
        }

        [Test]
        public void Scramble_DoesNotTouchSolutionTrapsRotationOrCooldown()
        {
            PuzzleState s = Wires();
            s.ScrambleCooldownUntil = 33d;
            Assert.IsTrue(s.TryScrambleAll(new GameRandom(7)));
            CollectionAssert.AreEqual(new[] { 0, 3, 5, 2 }, s.Solution);
            CollectionAssert.AreEqual(new[] { 1, 4 }, s.TrapValues);
            Assert.AreEqual(33d, s.ScrambleCooldownUntil, "cooldown is PuzzleSystem's job");
            Assert.IsFalse(s.IsLocked);
        }

        [Test]
        public void Scramble_NullRandom_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Paintings().TryScrambleAll(null));
        }

        [Test]
        public void Scramble_ThenHumanCanSolveByCycling()
        {
            PuzzleState s = Paintings();
            s.TryScrambleAll(new GameRandom(3));
            for (int part = 0; part < s.PartCount; part++)
            {
                for (int guard = 0; guard < s.ValueCount(part) && s.Get(part) != s.Solution[part]; guard++)
                {
                    Assert.IsTrue(s.TryApply(PuzzleOp.Cycle(part, 1)));
                }
            }
            Assert.IsTrue(s.IsSolved);
            Assert.IsTrue(s.TryLock());
        }

        // ---------- cooldown ----------

        [Test]
        public void ScrambleCooldown_DefaultsToNoCooldown()
        {
            PuzzleState s = Paintings();
            Assert.AreEqual(0d, s.ScrambleCooldownUntil);
            Assert.IsFalse(s.IsScrambleOnCooldown(0d));
            Assert.IsFalse(s.IsScrambleOnCooldown(123.4d));
        }

        [Test]
        public void ScrambleCooldown_IsOnCooldownUntilTheExactEndTime()
        {
            PuzzleState s = Paintings();
            s.ScrambleCooldownUntil = 15d;
            Assert.IsTrue(s.IsScrambleOnCooldown(0d));
            Assert.IsTrue(s.IsScrambleOnCooldown(14.999d));
            Assert.IsFalse(s.IsScrambleOnCooldown(15d), "Net.Time >= until is allowed again");
            Assert.IsFalse(s.IsScrambleOnCooldown(20d));
        }

        [Test]
        public void ScrambleCooldown_CanBeMovedBothWays()
        {
            PuzzleState s = Paintings();
            s.ScrambleCooldownUntil = 100d;
            s.ScrambleCooldownUntil = 10d;
            Assert.IsFalse(s.IsScrambleOnCooldown(10d));
            Assert.IsTrue(s.IsScrambleOnCooldown(9d));
        }

        // ---------- constructor validation ----------

        [Test]
        public void Ctor_NullOrEmptyValueCounts_Throws()
        {
            Assert.Throws<ArgumentException>(() => new PuzzleState(null, new[] { 0 }, PuzzleOpKind.Cycle));
            Assert.Throws<ArgumentException>(() => new PuzzleState(new int[0], new int[0], PuzzleOpKind.Cycle));
        }

        [Test]
        public void Ctor_NullOrMismatchedSolution_Throws()
        {
            Assert.Throws<ArgumentException>(() => new PuzzleState(new[] { 4, 4 }, null, PuzzleOpKind.Cycle));
            Assert.Throws<ArgumentException>(() => new PuzzleState(new[] { 4, 4 }, new[] { 0 }, PuzzleOpKind.Cycle));
            Assert.Throws<ArgumentException>(() => new PuzzleState(new[] { 4, 4 }, new[] { 0, 0, 0 }, PuzzleOpKind.Cycle));
        }

        [TestCase(1)]
        [TestCase(0)]
        [TestCase(-3)]
        public void Ctor_ValueCountBelowTwo_Throws(int k)
        {
            Assert.Throws<ArgumentException>(() => new PuzzleState(new[] { 4, k }, new[] { 0, 0 }, PuzzleOpKind.Cycle));
        }

        [TestCase(-1)]
        [TestCase(4)]
        [TestCase(9)]
        public void Ctor_SolutionValueOutOfRange_Throws(int value)
        {
            Assert.Throws<ArgumentException>(() => new PuzzleState(new[] { 4, 4 }, new[] { 0, value }, PuzzleOpKind.Cycle));
        }

        [Test]
        public void Ctor_SolutionRangeIsPerPart()
        {
            Assert.DoesNotThrow(() => new PuzzleState(new[] { 6, 3 }, new[] { 5, 2 }, PuzzleOpKind.Cycle));
            Assert.Throws<ArgumentException>(() => new PuzzleState(new[] { 6, 3 }, new[] { 5, 3 }, PuzzleOpKind.Cycle));
        }

        [TestCase(0)]
        [TestCase(90)]
        [TestCase(180)]
        [TestCase(270)]
        public void Ctor_ValidReferenceRotation_IsKept(int rotation)
        {
            Assert.AreEqual(rotation, Sigil(rotation).ReferenceRotation);
        }

        [TestCase(-90)]
        [TestCase(1)]
        [TestCase(45)]
        [TestCase(91)]
        [TestCase(360)]
        [TestCase(450)]
        public void Ctor_InvalidReferenceRotation_Throws(int rotation)
        {
            Assert.Throws<ArgumentException>(() => Sigil(rotation));
        }

        [TestCase(-1)]
        [TestCase(6)]
        [TestCase(100)]
        public void Ctor_TrapValueOutOfRange_Throws(int trap)
        {
            Assert.Throws<ArgumentException>(
                () => new PuzzleState(new[] { 6, 6, 6, 6 }, new[] { 0, 1, 2, 3 }, PuzzleOpKind.Cycle, 0, new[] { 4, trap }));
        }

        [Test]
        public void Ctor_NoTraps_GivesEmptyList()
        {
            PuzzleState s = Paintings();
            Assert.IsNotNull(s.TrapValues);
            Assert.AreEqual(0, s.TrapValues.Count);
        }

        [Test]
        public void Ctor_NoOps_Throws()
        {
            Assert.Throws<ArgumentException>(() => new PuzzleState(new[] { 4 }, new[] { 0 }, PuzzleOpKind.None));
        }

        [Test]
        public void Ctor_UnknownOpFlags_Throws()
        {
            Assert.Throws<ArgumentException>(() => new PuzzleState(new[] { 4 }, new[] { 0 }, (PuzzleOpKind)8));
            Assert.Throws<ArgumentException>(() => new PuzzleState(new[] { 4 }, new[] { 0 }, PuzzleOpKind.Cycle | (PuzzleOpKind)16));
        }

        [Test]
        public void Ctor_PermutationPuzzle_NeedsPermutationShape()
        {
            // K must equal the part count
            Assert.Throws<ArgumentException>(() => new PuzzleState(new[] { 3, 3 }, new[] { 0, 1 }, PuzzleOpKind.Swap));
            // repeated values in the solution
            Assert.Throws<ArgumentException>(() => new PuzzleState(new[] { 3, 3, 3 }, new[] { 0, 1, 1 }, PuzzleOpKind.Swap));
            // a single part can never be a permutation puzzle with K >= 2
            Assert.Throws<ArgumentException>(() => new PuzzleState(new[] { 2 }, new[] { 0 }, PuzzleOpKind.Swap));
            // valid
            Assert.DoesNotThrow(() => new PuzzleState(new[] { 3, 3, 3 }, new[] { 2, 0, 1 }, PuzzleOpKind.Swap));
        }

        [Test]
        public void Ctor_MixedSwapKinds_AreNotPermutationPuzzles()
        {
            // Books-like: Swap together with Cycle, repeated values and K != part count are fine.
            Assert.DoesNotThrow(() => new PuzzleState(new[] { 5, 5, 5 }, new[] { 1, 1, 1 }, CycleAndSwap));
        }

        [Test]
        public void Ctor_CopiesItsInputArrays()
        {
            int[] counts = { 4, 4 };
            int[] solution = { 1, 2 };
            int[] traps = { 3 };
            var s = new PuzzleState(counts, solution, PuzzleOpKind.Cycle, 0, traps);

            counts[0] = 99;
            solution[0] = 3;
            traps[0] = 0;

            Assert.AreEqual(4, s.ValueCount(0));
            Assert.AreEqual(1, s.Solution[0]);
            Assert.AreEqual(3, s.TrapValues[0]);
            Assert.IsTrue(s.IsSolved);
        }

        [Test]
        public void Exposed_Lists_AreReadOnly()
        {
            PuzzleState s = Wires();
            var solution = s.Solution as IList<int>;
            var traps = s.TrapValues as IList<int>;
            Assert.IsNotNull(solution);
            Assert.IsNotNull(traps);
            Assert.IsTrue(solution.IsReadOnly);
            Assert.IsTrue(traps.IsReadOnly);
            Assert.Throws<NotSupportedException>(() => solution[0] = 5);
            Assert.Throws<NotSupportedException>(() => traps[0] = 5);
            Assert.IsNull(s.Solution as int[]);
            Assert.IsNull(s.TrapValues as int[]);
        }

        [Test]
        public void GetAndValueCount_OutOfRange_Throw()
        {
            PuzzleState s = Paintings();
            Assert.Throws<ArgumentOutOfRangeException>(() => s.Get(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => s.Get(3));
            Assert.Throws<ArgumentOutOfRangeException>(() => s.ValueCount(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => s.ValueCount(3));
        }

        // ---------- network snapshot ----------

        [Test]
        public void CopyValuesTo_ThenApplySnapshot_RoundTrips()
        {
            foreach (Func<PuzzleState> make in AllFactories)
            {
                for (int seed = 0; seed < 20; seed++)
                {
                    PuzzleState authority = make();
                    PuzzleState client = make();
                    authority.TryScrambleAll(new GameRandom(seed));

                    var wire = new int[authority.PartCount];
                    authority.CopyValuesTo(wire);
                    Assert.IsTrue(client.ApplySnapshot(wire, authority.IsLocked));

                    CollectionAssert.AreEqual(Values(authority), Values(client));
                    Assert.AreEqual(authority.IsSolved, client.IsSolved);
                    Assert.IsFalse(client.IsLocked);
                }
            }
        }

        [Test]
        public void CopyValuesTo_LargerBuffer_FillsOnlyThePrefix()
        {
            PuzzleState s = Statue();
            int[] buffer = { 9, 9, 9, 9 };
            s.CopyValuesTo(buffer);
            CollectionAssert.AreEqual(new[] { 2, 1, 9, 9 }, buffer);
        }

        [Test]
        public void CopyValuesTo_BadBuffer_Throws()
        {
            PuzzleState s = Paintings();
            Assert.Throws<ArgumentException>(() => s.CopyValuesTo(null));
            Assert.Throws<ArgumentException>(() => s.CopyValuesTo(new int[2]));
        }

        [Test]
        public void ApplySnapshot_DoesNotAliasTheCallersArray()
        {
            PuzzleState s = Paintings();
            int[] wire = { 0, 0, 0 };
            Assert.IsTrue(s.ApplySnapshot(wire, false));
            wire[0] = 3;
            Assert.AreEqual(0, s.Get(0));
        }

        [Test]
        public void ApplySnapshot_Locked_MirrorsTheConfirmedPuzzle()
        {
            PuzzleState authority = Books();
            Assert.IsTrue(authority.TryLock());
            var wire = new int[authority.PartCount];
            authority.CopyValuesTo(wire);

            PuzzleState client = Books();
            client.TryScrambleAll(new GameRandom(11));
            Assert.IsTrue(client.ApplySnapshot(wire, authority.IsLocked));

            Assert.IsTrue(client.IsLocked);
            Assert.IsTrue(client.IsSolved);
            Assert.IsFalse(client.TryApply(PuzzleOp.Cycle(0, 1)));
        }

        [Test]
        public void ApplySnapshot_Unlocked_OverridesLocalLockState()
        {
            PuzzleState client = Paintings();
            client.TryLock();
            Assert.IsTrue(client.ApplySnapshot(new[] { 0, 0, 0 }, false));
            Assert.IsFalse(client.IsLocked);
            Assert.IsTrue(client.TryApply(PuzzleOp.Cycle(0, 1)));
        }

        [Test]
        public void ApplySnapshot_LockedButNotSolved_RejectedAndChangesNothing()
        {
            PuzzleState client = Paintings();
            Assert.IsFalse(client.ApplySnapshot(new[] { 0, 0, 0 }, true));
            Assert.IsFalse(client.IsLocked);
            CollectionAssert.AreEqual(new[] { 1, 3, 0 }, Values(client));
        }

        [Test]
        public void ApplySnapshot_WrongLength_RejectedAndChangesNothing()
        {
            PuzzleState client = Paintings();
            Assert.IsFalse(client.ApplySnapshot(null, false));
            Assert.IsFalse(client.ApplySnapshot(new[] { 0, 0 }, false));
            Assert.IsFalse(client.ApplySnapshot(new[] { 0, 0, 0, 0 }, false));
            Assert.IsTrue(client.IsSolved);
        }

        [Test]
        public void ApplySnapshot_ValueOutOfRange_RejectedAtomically()
        {
            PuzzleState client = Statue(); // K = {6, 3}
            Assert.IsFalse(client.ApplySnapshot(new[] { 0, 3 }, false), "part 1 only has 3 values");
            Assert.IsFalse(client.ApplySnapshot(new[] { 6, 0 }, false));
            Assert.IsFalse(client.ApplySnapshot(new[] { 0, -1 }, false));
            CollectionAssert.AreEqual(new[] { 2, 1 }, Values(client), "nothing may be half-applied");
        }

        [Test]
        public void ApplySnapshot_PermutationWithRepeatedValue_Rejected()
        {
            PuzzleState client = Jigsaw();
            Assert.IsFalse(client.ApplySnapshot(new[] { 0, 0, 1, 2, 3, 4 }, false));
            Assert.IsTrue(client.IsSolved);
        }

        [Test]
        public void ApplySnapshot_DoesNotTouchCooldown()
        {
            PuzzleState client = Paintings();
            client.ScrambleCooldownUntil = 42d;
            Assert.IsTrue(client.ApplySnapshot(new[] { 0, 0, 0 }, false));
            Assert.AreEqual(42d, client.ScrambleCooldownUntil);
        }
    }
}
