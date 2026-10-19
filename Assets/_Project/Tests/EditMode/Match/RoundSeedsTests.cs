using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.Match;
using NUnit.Framework;

namespace CinliMahzen.Tests.EditMode.Match
{
    /// <summary>
    /// Match seed → round seeds (Teknik §9, CLAUDE.md "game-state randomness is a seeded GameRandom"): the same match seed must give the
    /// same round seeds on every run and machine, every seed must be positive (0 means "no seed" in Lobby).
    /// </summary>
    public class RoundSeedsTests
    {
        private static readonly int[] SampleMatchSeeds =
        {
            int.MinValue, -1234567, -1, 0, 1, 2, 12345, 987654321, int.MaxValue - 1, int.MaxValue,
        };

        [Test]
        public void ForRound_SameInput_SameSeed()
        {
            Assert.AreEqual(RoundSeeds.ForRound(12345, 0), RoundSeeds.ForRound(12345, 0));
            Assert.AreEqual(RoundSeeds.ForRound(12345, 3), RoundSeeds.ForRound(12345, 3));
        }

        [Test]
        public void ForRound_IsTheNthDrawOfAGameRandom()
        {
            // Pins the derivation: round r = the (r + 1)-th Range(1, int.MaxValue) draw of GameRandom(matchSeed).
            var rng = new GameRandom(4242);
            for (int round = 0; round < 6; round++)
            {
                int expected = rng.Range(1, int.MaxValue);
                Assert.AreEqual(expected, RoundSeeds.ForRound(4242, round), "round " + round);
            }
        }

        [Test]
        public void ForRound_DoesNotDependOnCallOrder()
        {
            int third = RoundSeeds.ForRound(777, 3);
            RoundSeeds.ForRound(777, 0);
            RoundSeeds.ForRound(777, 5);
            RoundSeeds.ForRound(999, 3);
            Assert.AreEqual(third, RoundSeeds.ForRound(777, 3));
        }

        [Test]
        public void ForRound_RoundsOfOneMatch_AllDiffer()
        {
            var seen = new HashSet<int>();
            for (int round = 0; round < RoleRotation.DefaultPlayerCount; round++)
            {
                Assert.IsTrue(seen.Add(RoundSeeds.ForRound(12345, round)), "round " + round + " repeated an earlier round seed");
            }
        }

        [Test]
        public void ForRound_DifferentMatchSeeds_GiveDifferentRoundSeeds()
        {
            Assert.AreNotEqual(RoundSeeds.ForRound(1, 0), RoundSeeds.ForRound(2, 0));
            Assert.AreNotEqual(RoundSeeds.ForRound(1, 2), RoundSeeds.ForRound(2, 2));
        }

        [Test]
        public void ForRound_AlwaysPositive()
        {
            foreach (int matchSeed in SampleMatchSeeds)
            {
                for (int round = 0; round < 8; round++)
                {
                    Assert.GreaterOrEqual(RoundSeeds.ForRound(matchSeed, round), 1, "match seed " + matchSeed + ", round " + round);
                }
            }
        }

        [Test]
        public void ForRound_NegativeRound_CountsAsRoundZero()
        {
            Assert.AreEqual(RoundSeeds.ForRound(12345, 0), RoundSeeds.ForRound(12345, -1));
            Assert.AreEqual(RoundSeeds.ForRound(12345, 0), RoundSeeds.ForRound(12345, int.MinValue));
        }

        [Test]
        public void CreateMatchSeed_SameEntropy_SameSeed()
        {
            Assert.AreEqual(RoundSeeds.CreateMatchSeed(31337), RoundSeeds.CreateMatchSeed(31337));
        }

        [Test]
        public void CreateMatchSeed_NeighbouringEntropy_GivesUnrelatedSeeds()
        {
            // TickCount values of two consecutive restarts are close; the seeds must not be.
            Assert.AreNotEqual(RoundSeeds.CreateMatchSeed(1000), RoundSeeds.CreateMatchSeed(1001));
        }

        [Test]
        public void CreateMatchSeed_AlwaysPositive()
        {
            foreach (int entropy in SampleMatchSeeds)
            {
                Assert.GreaterOrEqual(RoundSeeds.CreateMatchSeed(entropy), 1, "entropy " + entropy);
            }
        }

        [Test]
        public void CreateMatchSeed_NeverReturnsTheSeedToAvoid()
        {
            int first = RoundSeeds.CreateMatchSeed(555);
            int again = RoundSeeds.CreateMatchSeed(555, first);

            Assert.AreNotEqual(first, again, "same entropy, but the previous seed must be avoided");
            Assert.GreaterOrEqual(again, 1);
            Assert.AreEqual(first + 1, again);
        }

        [Test]
        public void CreateMatchSeed_AvoidingSomethingElse_ChangesNothing()
        {
            int plain = RoundSeeds.CreateMatchSeed(555);
            Assert.AreEqual(plain, RoundSeeds.CreateMatchSeed(555, plain + 100));
            Assert.AreEqual(plain, RoundSeeds.CreateMatchSeed(555, 0));
        }
    }
}
