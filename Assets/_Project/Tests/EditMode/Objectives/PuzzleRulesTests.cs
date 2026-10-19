using CinliMahzen.Core;
using CinliMahzen.Objectives;
using NUnit.Framework;
using UnityEngine;

namespace CinliMahzen.Tests.EditMode.Objectives
{
    /// <summary>
    /// The pure authority rules of the three puzzle requests (Teknik §7.3, Oyun §4.2, TODO 2.4): every refusal reason, the accepted
    /// case, the order in which rules win, and the range / seed helpers.
    /// </summary>
    public class PuzzleRulesTests
    {
        private const double Now = 100d;
        private const float Cost = 25f;

        // ---------- baselines: a request that passes every rule ----------

        private static PuzzleCheckFacts ValidOp()
        {
            return new PuzzleCheckFacts
            {
                Role = Role.Human,
                InRange = true,
                HasRequiredItem = true,
            };
        }

        private static PuzzleCheckFacts ValidScramble()
        {
            return new PuzzleCheckFacts
            {
                Role = Role.EvilJinn,
                JinnsAwake = true,
                InRange = true,
                RoomLight = RoomLightState.Off,
                Now = Now,
                ScrambleCooldownUntil = 0d,
                Energy = 100f,
                EnergyCost = Cost,
            };
        }

        private static PuzzleCheckFacts ValidConfirm()
        {
            return new PuzzleCheckFacts
            {
                Role = Role.Human,
                InRange = true,
                Now = Now,
                ConfirmCooldownUntil = 0d,
            };
        }

        // ---------- IsInRange ----------

        [Test]
        public void IsInRange_UsesRangePlusTolerance_AsAnInclusiveLimit()
        {
            // Interact range 2.2 + tolerance 0.5 = 2.7 m.
            Assert.IsTrue(PuzzleRules.IsInRange(Vector3.zero, new Vector3(2.7f, 0f, 0f), 2.2f, 0.5f));
            Assert.IsTrue(PuzzleRules.IsInRange(Vector3.zero, new Vector3(2.6f, 0f, 0f), 2.2f, 0.5f));
            Assert.IsFalse(PuzzleRules.IsInRange(Vector3.zero, new Vector3(2.8f, 0f, 0f), 2.2f, 0.5f));
        }

        [Test]
        public void IsInRange_IsThreeDimensional_AndSymmetric()
        {
            var a = new Vector3(1f, 2f, 3f);
            var b = new Vector3(2f, 1f, 1f);

            Assert.AreEqual(PuzzleRules.IsInRange(a, b, 2.2f, 0f), PuzzleRules.IsInRange(b, a, 2.2f, 0f));
            Assert.IsTrue(PuzzleRules.IsInRange(a, b, 2.5f, 0f), "distance sqrt(6) = 2.449");
            Assert.IsFalse(PuzzleRules.IsInRange(a, b, 2.4f, 0f));
        }

        [Test]
        public void IsInRange_NegativeOrNaNLimit_IsNeverInRange()
        {
            Assert.IsFalse(PuzzleRules.IsInRange(Vector3.zero, Vector3.zero, -1f, 0f));
            Assert.IsFalse(PuzzleRules.IsInRange(Vector3.zero, Vector3.zero, float.NaN, 0.5f));
            Assert.IsTrue(PuzzleRules.IsInRange(Vector3.zero, Vector3.zero, 0f, 0f), "standing on the target is in range of a zero limit");
        }

        // ---------- CheckOp ----------

        [Test]
        public void CheckOp_AcceptsTheBaselineHumanRequest()
        {
            Assert.AreEqual(PuzzleDenyReason.None, PuzzleRules.CheckOp(ValidOp()));
        }

        [Test]
        public void CheckOp_RefusesEveryNonHumanRole()
        {
            foreach (Role role in new[] { Role.None, Role.GoodJinn, Role.EvilJinn })
            {
                PuzzleCheckFacts f = ValidOp();
                f.Role = role;
                Assert.AreEqual(PuzzleDenyReason.WrongRole, PuzzleRules.CheckOp(f), role.ToString());
            }
        }

        [Test]
        public void CheckOp_RefusesFaintedHuman()
        {
            PuzzleCheckFacts f = ValidOp();
            f.HumanFainted = true;
            Assert.AreEqual(PuzzleDenyReason.HumanFainted, PuzzleRules.CheckOp(f));
        }

        [Test]
        public void CheckOp_RefusesLockedPuzzle()
        {
            PuzzleCheckFacts f = ValidOp();
            f.IsLocked = true;
            Assert.AreEqual(PuzzleDenyReason.Locked, PuzzleRules.CheckOp(f));
        }

        [Test]
        public void CheckOp_RefusesOutOfRange()
        {
            PuzzleCheckFacts f = ValidOp();
            f.InRange = false;
            Assert.AreEqual(PuzzleDenyReason.OutOfRange, PuzzleRules.CheckOp(f));
        }

        [Test]
        public void CheckOp_RefusesMissingItem()
        {
            PuzzleCheckFacts f = ValidOp();
            f.HasRequiredItem = false;
            Assert.AreEqual(PuzzleDenyReason.MissingItem, PuzzleRules.CheckOp(f));
        }

        [Test]
        public void CheckOp_IgnoresSabotageAndConfirmFacts()
        {
            PuzzleCheckFacts f = ValidOp();
            f.RoomLight = RoomLightState.On;
            f.InLanternSafeZone = true;
            f.ScrambleCooldownUntil = double.MaxValue;
            f.ConfirmCooldownUntil = double.MaxValue;
            f.Energy = 0f;
            f.EnergyCost = Cost;
            Assert.AreEqual(PuzzleDenyReason.None, PuzzleRules.CheckOp(f));
        }

        [Test]
        public void CheckOp_FirstFailingRuleWins()
        {
            PuzzleCheckFacts f = default; // everything unsatisfied
            Assert.AreEqual(PuzzleDenyReason.WrongRole, PuzzleRules.CheckOp(f));

            f.Role = Role.Human;
            f.HumanFainted = true;
            f.IsLocked = true;
            Assert.AreEqual(PuzzleDenyReason.HumanFainted, PuzzleRules.CheckOp(f));

            f.HumanFainted = false;
            Assert.AreEqual(PuzzleDenyReason.Locked, PuzzleRules.CheckOp(f));

            f.IsLocked = false;
            Assert.AreEqual(PuzzleDenyReason.OutOfRange, PuzzleRules.CheckOp(f));

            f.InRange = true;
            Assert.AreEqual(PuzzleDenyReason.MissingItem, PuzzleRules.CheckOp(f));
        }

        // ---------- CheckScramble ----------

        [Test]
        public void CheckScramble_AcceptsTheBaselineEvilJinnRequest()
        {
            Assert.AreEqual(PuzzleDenyReason.None, PuzzleRules.CheckScramble(ValidScramble()));
        }

        [Test]
        public void CheckScramble_RefusesEveryNonEvilRole()
        {
            foreach (Role role in new[] { Role.None, Role.Human, Role.GoodJinn })
            {
                PuzzleCheckFacts f = ValidScramble();
                f.Role = role;
                Assert.AreEqual(PuzzleDenyReason.WrongRole, PuzzleRules.CheckScramble(f), role.ToString());
            }
        }

        [Test]
        public void CheckScramble_RefusesWhileJinnsAsleep()
        {
            PuzzleCheckFacts f = ValidScramble();
            f.JinnsAwake = false;
            Assert.AreEqual(PuzzleDenyReason.JinnsAsleep, PuzzleRules.CheckScramble(f));
        }

        [Test]
        public void CheckScramble_RefusesLockedPuzzle()
        {
            PuzzleCheckFacts f = ValidScramble();
            f.IsLocked = true;
            Assert.AreEqual(PuzzleDenyReason.Locked, PuzzleRules.CheckScramble(f));
        }

        [Test]
        public void CheckScramble_RefusesOutOfRange()
        {
            PuzzleCheckFacts f = ValidScramble();
            f.InRange = false;
            Assert.AreEqual(PuzzleDenyReason.OutOfRange, PuzzleRules.CheckScramble(f));
        }

        [Test]
        public void CheckScramble_RefusesALitRoom()
        {
            PuzzleCheckFacts f = ValidScramble();
            f.RoomLight = RoomLightState.On;
            Assert.AreEqual(PuzzleDenyReason.LitRoom, PuzzleRules.CheckScramble(f));
        }

        [Test]
        public void CheckScramble_RefusesInsideTheLanternSafeZone()
        {
            PuzzleCheckFacts f = ValidScramble();
            f.InLanternSafeZone = true;
            Assert.AreEqual(PuzzleDenyReason.LanternProtected, PuzzleRules.CheckScramble(f));
        }

        [Test]
        public void CheckScramble_RefusesDuringCooldown_AndAllowsExactlyAtItsEnd()
        {
            PuzzleCheckFacts f = ValidScramble();

            f.ScrambleCooldownUntil = Now + 15d;
            Assert.AreEqual(PuzzleDenyReason.ScrambleCooldown, PuzzleRules.CheckScramble(f));

            f.ScrambleCooldownUntil = Now + 0.001d;
            Assert.AreEqual(PuzzleDenyReason.ScrambleCooldown, PuzzleRules.CheckScramble(f));

            f.ScrambleCooldownUntil = Now;
            Assert.AreEqual(PuzzleDenyReason.None, PuzzleRules.CheckScramble(f), "now == cooldownUntil: the cooldown is over");

            f.ScrambleCooldownUntil = Now - 1d;
            Assert.AreEqual(PuzzleDenyReason.None, PuzzleRules.CheckScramble(f));
        }

        [Test]
        public void CheckScramble_RefusesTooLittleEnergy_AndAcceptsExactlyTheCost()
        {
            PuzzleCheckFacts f = ValidScramble();

            f.Energy = Cost - 0.01f;
            Assert.AreEqual(PuzzleDenyReason.InsufficientEnergy, PuzzleRules.CheckScramble(f));

            f.Energy = Cost;
            Assert.AreEqual(PuzzleDenyReason.None, PuzzleRules.CheckScramble(f));

            f.Energy = float.NaN;
            Assert.AreEqual(PuzzleDenyReason.InsufficientEnergy, PuzzleRules.CheckScramble(f), "NaN energy is never enough");
        }

        [Test]
        public void CheckScramble_AFreeScrambleNeedsNoEnergy()
        {
            PuzzleCheckFacts f = ValidScramble();
            f.EnergyCost = 0f;
            f.Energy = 0f;
            Assert.AreEqual(PuzzleDenyReason.None, PuzzleRules.CheckScramble(f));
        }

        [Test]
        public void CheckScramble_IgnoresOpAndConfirmFacts()
        {
            PuzzleCheckFacts f = ValidScramble();
            f.HasRequiredItem = false;
            f.HumanFainted = true;
            f.ConfirmCooldownUntil = double.MaxValue;
            Assert.AreEqual(PuzzleDenyReason.None, PuzzleRules.CheckScramble(f));
        }

        [Test]
        public void CheckScramble_FirstFailingRuleWins()
        {
            PuzzleCheckFacts f = default;
            f.Now = Now;
            f.EnergyCost = Cost;
            f.RoomLight = RoomLightState.On;
            f.InLanternSafeZone = true;
            f.IsLocked = true;
            f.ScrambleCooldownUntil = Now + 5d;
            Assert.AreEqual(PuzzleDenyReason.WrongRole, PuzzleRules.CheckScramble(f));

            f.Role = Role.EvilJinn;
            Assert.AreEqual(PuzzleDenyReason.JinnsAsleep, PuzzleRules.CheckScramble(f));

            f.JinnsAwake = true;
            Assert.AreEqual(PuzzleDenyReason.Locked, PuzzleRules.CheckScramble(f));

            f.IsLocked = false;
            Assert.AreEqual(PuzzleDenyReason.OutOfRange, PuzzleRules.CheckScramble(f));

            f.InRange = true;
            Assert.AreEqual(PuzzleDenyReason.LitRoom, PuzzleRules.CheckScramble(f));

            f.RoomLight = RoomLightState.Off;
            Assert.AreEqual(PuzzleDenyReason.LanternProtected, PuzzleRules.CheckScramble(f));

            f.InLanternSafeZone = false;
            Assert.AreEqual(PuzzleDenyReason.ScrambleCooldown, PuzzleRules.CheckScramble(f));

            f.ScrambleCooldownUntil = 0d;
            Assert.AreEqual(PuzzleDenyReason.InsufficientEnergy, PuzzleRules.CheckScramble(f));

            f.Energy = Cost;
            Assert.AreEqual(PuzzleDenyReason.None, PuzzleRules.CheckScramble(f));
        }

        // ---------- CheckConfirm ----------

        [Test]
        public void CheckConfirm_AcceptsTheBaselineHumanRequest()
        {
            Assert.AreEqual(PuzzleDenyReason.None, PuzzleRules.CheckConfirm(ValidConfirm()));
        }

        [Test]
        public void CheckConfirm_RefusesEveryNonHumanRole()
        {
            foreach (Role role in new[] { Role.None, Role.GoodJinn, Role.EvilJinn })
            {
                PuzzleCheckFacts f = ValidConfirm();
                f.Role = role;
                Assert.AreEqual(PuzzleDenyReason.WrongRole, PuzzleRules.CheckConfirm(f), role.ToString());
            }
        }

        [Test]
        public void CheckConfirm_RefusesFaintedHuman_LockedPuzzle_AndOutOfRange()
        {
            PuzzleCheckFacts fainted = ValidConfirm();
            fainted.HumanFainted = true;
            Assert.AreEqual(PuzzleDenyReason.HumanFainted, PuzzleRules.CheckConfirm(fainted));

            PuzzleCheckFacts locked = ValidConfirm();
            locked.IsLocked = true;
            Assert.AreEqual(PuzzleDenyReason.Locked, PuzzleRules.CheckConfirm(locked));

            PuzzleCheckFacts far = ValidConfirm();
            far.InRange = false;
            Assert.AreEqual(PuzzleDenyReason.OutOfRange, PuzzleRules.CheckConfirm(far));
        }

        [Test]
        public void CheckConfirm_RefusesDuringTheLeverCooldown_AndAllowsExactlyAtItsEnd()
        {
            PuzzleCheckFacts f = ValidConfirm();

            f.ConfirmCooldownUntil = Now + 5d;
            Assert.AreEqual(PuzzleDenyReason.ConfirmCooldown, PuzzleRules.CheckConfirm(f));

            f.ConfirmCooldownUntil = Now;
            Assert.AreEqual(PuzzleDenyReason.None, PuzzleRules.CheckConfirm(f));
        }

        [Test]
        public void CheckConfirm_FirstFailingRuleWins()
        {
            PuzzleCheckFacts f = default;
            f.Now = Now;
            f.ConfirmCooldownUntil = Now + 5d;
            Assert.AreEqual(PuzzleDenyReason.WrongRole, PuzzleRules.CheckConfirm(f));

            f.Role = Role.Human;
            f.HumanFainted = true;
            f.IsLocked = true;
            Assert.AreEqual(PuzzleDenyReason.HumanFainted, PuzzleRules.CheckConfirm(f));

            f.HumanFainted = false;
            Assert.AreEqual(PuzzleDenyReason.Locked, PuzzleRules.CheckConfirm(f));

            f.IsLocked = false;
            Assert.AreEqual(PuzzleDenyReason.OutOfRange, PuzzleRules.CheckConfirm(f));

            f.InRange = true;
            Assert.AreEqual(PuzzleDenyReason.ConfirmCooldown, PuzzleRules.CheckConfirm(f));
        }

        // ---------- ScrambleSeed ----------

        [Test]
        public void ScrambleSeed_IsDeterministic_PositiveAndNotTheRoundSeed()
        {
            foreach (int roundSeed in new[] { int.MinValue, -7, 0, 1, 12345, 987654321, int.MaxValue })
            {
                int seed = PuzzleRules.ScrambleSeed(roundSeed);

                Assert.AreEqual(seed, PuzzleRules.ScrambleSeed(roundSeed), "deterministic for " + roundSeed);
                Assert.GreaterOrEqual(seed, 1, "positive for " + roundSeed);
                Assert.AreNotEqual(roundSeed, seed, "must differ from the round seed " + roundSeed);
            }
        }

        [Test]
        public void ScrambleSeed_SeparateRoundSeeds_GiveSeparateSabotageSeeds()
        {
            Assert.AreNotEqual(PuzzleRules.ScrambleSeed(1), PuzzleRules.ScrambleSeed(2));
            Assert.AreNotEqual(PuzzleRules.ScrambleSeed(100), PuzzleRules.ScrambleSeed(101));
        }

        // ---------- enum stability ----------

        [Test]
        public void PuzzleDenyReason_ValuesAreStable()
        {
            Assert.AreEqual(0, (byte)PuzzleDenyReason.None);
            Assert.AreEqual(1, (byte)PuzzleDenyReason.UnknownSlot);
            Assert.AreEqual(2, (byte)PuzzleDenyReason.WrongRole);
            Assert.AreEqual(3, (byte)PuzzleDenyReason.JinnsAsleep);
            Assert.AreEqual(4, (byte)PuzzleDenyReason.HumanFainted);
            Assert.AreEqual(5, (byte)PuzzleDenyReason.Locked);
            Assert.AreEqual(6, (byte)PuzzleDenyReason.PawnMissing);
            Assert.AreEqual(7, (byte)PuzzleDenyReason.OutOfRange);
            Assert.AreEqual(8, (byte)PuzzleDenyReason.MissingItem);
            Assert.AreEqual(9, (byte)PuzzleDenyReason.InvalidOp);
            Assert.AreEqual(10, (byte)PuzzleDenyReason.LitRoom);
            Assert.AreEqual(11, (byte)PuzzleDenyReason.LanternProtected);
            Assert.AreEqual(12, (byte)PuzzleDenyReason.ScrambleCooldown);
            Assert.AreEqual(13, (byte)PuzzleDenyReason.InsufficientEnergy);
            Assert.AreEqual(14, (byte)PuzzleDenyReason.ConfirmCooldown);
            Assert.AreEqual(15, (byte)PuzzleDenyReason.WrongAnswer);
        }
    }
}
