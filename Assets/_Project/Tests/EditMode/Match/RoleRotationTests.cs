using CinliMahzen.Core;
using CinliMahzen.Match;
using NUnit.Framework;

namespace CinliMahzen.Tests.EditMode.Match
{
    /// <summary>
    /// Oyun §10 role rotation (<see cref="RoleRotation"/>): in round r the Human is player r, the Good Jinn is player r+1
    /// and the other two players are Evil Jinns, all modulo the player count. Pure functions, no Unity needed.
    /// </summary>
    public class RoleRotationTests
    {
        private const int Players = RoleRotation.DefaultPlayerCount;

        private static void AssertRoles(int round, params Role[] expected)
        {
            CollectionAssert.AreEqual(expected, RoleRotation.CreateRoles(round), "round " + round);
        }

        private static int Count(Role[] roles, Role wanted)
        {
            int n = 0;
            foreach (Role role in roles)
            {
                if (role == wanted)
                {
                    n++;
                }
            }

            return n;
        }

        [Test]
        public void DefaultPlayerCount_IsFour()
        {
            Assert.AreEqual(4, RoleRotation.DefaultPlayerCount);
        }

        [Test]
        public void FourRoundsOfFourPlayers_MatchTheGddTable()
        {
            AssertRoles(0, Role.Human, Role.GoodJinn, Role.EvilJinn, Role.EvilJinn);
            AssertRoles(1, Role.EvilJinn, Role.Human, Role.GoodJinn, Role.EvilJinn);
            AssertRoles(2, Role.EvilJinn, Role.EvilJinn, Role.Human, Role.GoodJinn);
            AssertRoles(3, Role.GoodJinn, Role.EvilJinn, Role.EvilJinn, Role.Human);
        }

        [Test]
        public void EveryRound_HasExactlyOneHumanOneGoodJinnAndTwoEvilJinns()
        {
            for (int round = 0; round < Players; round++)
            {
                Role[] roles = RoleRotation.CreateRoles(round);

                Assert.AreEqual(Players, roles.Length, "round " + round);
                Assert.AreEqual(1, Count(roles, Role.Human), "humans in round " + round);
                Assert.AreEqual(1, Count(roles, Role.GoodJinn), "good jinns in round " + round);
                Assert.AreEqual(2, Count(roles, Role.EvilJinn), "evil jinns in round " + round);
            }
        }

        [Test]
        public void OverFourRounds_EveryPlayerIsHumanOnce_GoodJinnOnce_EvilJinnTwice()
        {
            for (int player = 0; player < Players; player++)
            {
                int human = 0;
                int goodJinn = 0;
                int evilJinn = 0;

                for (int round = 0; round < Players; round++)
                {
                    switch (RoleRotation.RoleFor(round, player))
                    {
                        case Role.Human:
                            human++;
                            break;
                        case Role.GoodJinn:
                            goodJinn++;
                            break;
                        case Role.EvilJinn:
                            evilJinn++;
                            break;
                        default:
                            Assert.Fail("player " + player + " has no role in round " + round);
                            break;
                    }
                }

                Assert.AreEqual(1, human, "player " + player + " as Human");
                Assert.AreEqual(1, goodJinn, "player " + player + " as Good Jinn");
                Assert.AreEqual(2, evilJinn, "player " + player + " as Evil Jinn");
            }
        }

        [Test]
        public void HumanAndGoodJinnIndices_FollowRoundModuloPlayerCount()
        {
            for (int round = 0; round < 2 * Players; round++)
            {
                Assert.AreEqual(round % Players, RoleRotation.HumanPlayerIndex(round), "human of round " + round);
                Assert.AreEqual((round + 1) % Players, RoleRotation.GoodJinnPlayerIndex(round), "good jinn of round " + round);
                Assert.AreEqual(Role.Human, RoleRotation.RoleFor(round, RoleRotation.HumanPlayerIndex(round)));
                Assert.AreEqual(Role.GoodJinn, RoleRotation.RoleFor(round, RoleRotation.GoodJinnPlayerIndex(round)));
            }
        }

        [Test]
        public void RoundsBeyondTheFourth_WrapAround()
        {
            CollectionAssert.AreEqual(RoleRotation.CreateRoles(0), RoleRotation.CreateRoles(4));
            CollectionAssert.AreEqual(RoleRotation.CreateRoles(1), RoleRotation.CreateRoles(5));
            Assert.AreEqual(Role.Human, RoleRotation.RoleFor(7, 3));
            Assert.AreEqual(0, RoleRotation.HumanPlayerIndex(8));
        }

        [Test]
        public void NegativeRounds_WrapLikePositiveOnes()
        {
            Assert.AreEqual(3, RoleRotation.HumanPlayerIndex(-1));
            Assert.AreEqual(0, RoleRotation.GoodJinnPlayerIndex(-1));
            Assert.AreEqual(Role.Human, RoleRotation.RoleFor(-4, 0));
            Assert.AreEqual(Role.GoodJinn, RoleRotation.RoleFor(-1, 0));
            CollectionAssert.AreEqual(RoleRotation.CreateRoles(3), RoleRotation.CreateRoles(-1));
        }

        [Test]
        public void RoleFor_AgreesWithCreateRoles_OnEveryCell()
        {
            for (int round = 0; round < Players; round++)
            {
                Role[] roles = RoleRotation.CreateRoles(round);

                for (int player = 0; player < Players; player++)
                {
                    Assert.AreEqual(roles[player], RoleRotation.RoleFor(round, player), "round " + round + " player " + player);
                }
            }
        }

        [Test]
        public void RoleFor_OutOfRangePlayer_IsNone()
        {
            Assert.AreEqual(Role.None, RoleRotation.RoleFor(0, -1));
            Assert.AreEqual(Role.None, RoleRotation.RoleFor(0, Players));
        }

        [Test]
        public void TwoPlayers_GiveOnlyHumanAndGoodJinn()
        {
            CollectionAssert.AreEqual(new[] { Role.Human, Role.GoodJinn }, RoleRotation.CreateRoles(0, 2));
            CollectionAssert.AreEqual(new[] { Role.GoodJinn, Role.Human }, RoleRotation.CreateRoles(1, 2));
            CollectionAssert.AreEqual(new[] { Role.Human, Role.GoodJinn }, RoleRotation.CreateRoles(2, 2));
        }

        [Test]
        public void UnusablePlayerCount_YieldsNoUsableRoles()
        {
            Assert.AreEqual(-1, RoleRotation.HumanPlayerIndex(0, 1));
            Assert.AreEqual(-1, RoleRotation.GoodJinnPlayerIndex(0, 1));
            Assert.AreEqual(Role.None, RoleRotation.RoleFor(0, 0, 1));
            Assert.AreEqual(0, RoleRotation.CreateRoles(0, 0).Length);
            Assert.AreEqual(0, RoleRotation.CreateRoles(0, -2).Length);
            CollectionAssert.AreEqual(new[] { Role.None }, RoleRotation.CreateRoles(0, 1));
        }
    }
}
