using CinliMahzen.Core;
using CinliMahzen.Jinn;
using NUnit.Framework;
using UnityEngine;

namespace CinliMahzen.Tests.EditMode.Jinn
{
    /// <summary>The client pre-check (<see cref="SpiritBumpRules.CanRequest"/>) and the authority check (<see cref="SpiritBumpRules.Validate"/>).</summary>
    public class SpiritBumpRulesTests
    {
        // Oyun §14: SpiritBumpRange 1.5, SpiritBumpCooldown 6, NetRangeTolerance 0.5.
        private const float Range = 1.5f;
        private const float Tolerance = 0.5f;
        private const double Now = 100d;
        private const double CooldownOver = 100d;
        private const double CooldownRunning = 103d;

        // ---- CanRequest ----

        [Test]
        public void CanRequest_AllConditionsMet_IsTrue()
        {
            Assert.IsTrue(SpiritBumpRules.CanRequest(true, true, true, 1f, Range, Now, CooldownOver));
        }

        [Test]
        public void CanRequest_NotAnEvilJinn_IsFalse()
        {
            Assert.IsFalse(SpiritBumpRules.CanRequest(false, true, true, 1f, Range, Now, CooldownOver));
        }

        [Test]
        public void CanRequest_NotBoosting_IsFalse()
        {
            Assert.IsFalse(SpiritBumpRules.CanRequest(true, false, true, 1f, Range, Now, CooldownOver));
        }

        [Test]
        public void CanRequest_TargetNotAFocusingGoodJinn_IsFalse()
        {
            Assert.IsFalse(SpiritBumpRules.CanRequest(true, true, false, 1f, Range, Now, CooldownOver));
        }

        [Test]
        public void CanRequest_CooldownRunning_IsFalse()
        {
            Assert.IsFalse(SpiritBumpRules.CanRequest(true, true, true, 1f, Range, Now, CooldownRunning));
        }

        [Test]
        public void CanRequest_CooldownEndingExactlyNow_IsTrue()
        {
            Assert.IsTrue(SpiritBumpRules.CanRequest(true, true, true, 1f, Range, 103d, CooldownRunning));
        }

        [Test]
        public void CanRequest_RangeIsInclusive_AndHasNoNetworkTolerance()
        {
            Assert.IsTrue(SpiritBumpRules.CanRequest(true, true, true, Range, Range, Now, CooldownOver), "exactly on the limit");
            Assert.IsTrue(SpiritBumpRules.CanRequest(true, true, true, 0f, Range, Now, CooldownOver), "inside the body");
            Assert.IsFalse(SpiritBumpRules.CanRequest(true, true, true, Range + 0.01f, Range, Now, CooldownOver), "just outside");
            Assert.IsFalse(SpiritBumpRules.CanRequest(true, true, true, Range + Tolerance, Range, Now, CooldownOver), "the tolerance is the authority's slack");
        }

        [Test]
        public void CanRequest_UnknownOrNegativeDistance_IsFalse()
        {
            Assert.IsFalse(SpiritBumpRules.CanRequest(true, true, true, float.NaN, Range, Now, CooldownOver));
            Assert.IsFalse(SpiritBumpRules.CanRequest(true, true, true, -1f, Range, Now, CooldownOver));
        }

        // ---- Validate ----

        private static SpiritBumpDenyReason Validate(
            Role requester = Role.EvilJinn, Role target = Role.GoodJinn, bool focusing = true,
            float distance = 1f, double now = Now, double cooldownUntil = CooldownOver)
        {
            return SpiritBumpRules.Validate(requester, target, focusing, distance, Range, Tolerance, now, cooldownUntil);
        }

        [Test]
        public void Validate_AllConditionsMet_IsAccepted()
        {
            Assert.AreEqual(SpiritBumpDenyReason.None, Validate());
        }

        [Test]
        public void Validate_RequesterMustBeAnEvilJinn()
        {
            Assert.AreEqual(SpiritBumpDenyReason.RequesterNotEvil, Validate(requester: Role.GoodJinn));
            Assert.AreEqual(SpiritBumpDenyReason.RequesterNotEvil, Validate(requester: Role.Human));
            Assert.AreEqual(SpiritBumpDenyReason.RequesterNotEvil, Validate(requester: Role.None));
        }

        [Test]
        public void Validate_TargetMustBeTheGoodJinn()
        {
            Assert.AreEqual(SpiritBumpDenyReason.TargetNotGood, Validate(target: Role.EvilJinn));
            Assert.AreEqual(SpiritBumpDenyReason.TargetNotGood, Validate(target: Role.Human));
            Assert.AreEqual(SpiritBumpDenyReason.TargetNotGood, Validate(target: Role.None));
        }

        [Test]
        public void Validate_TargetMustBeFocusing()
        {
            Assert.AreEqual(SpiritBumpDenyReason.TargetNotFocusing, Validate(focusing: false));
        }

        [Test]
        public void Validate_CooldownMustBeOver()
        {
            Assert.AreEqual(SpiritBumpDenyReason.Cooldown, Validate(cooldownUntil: CooldownRunning));
            Assert.AreEqual(SpiritBumpDenyReason.None, Validate(now: 103d, cooldownUntil: CooldownRunning), "ends exactly now");
        }

        [Test]
        public void Validate_RangeAddsTheNetworkTolerance()
        {
            Assert.AreEqual(SpiritBumpDenyReason.None, Validate(distance: Range), "inside the range");
            Assert.AreEqual(SpiritBumpDenyReason.None, Validate(distance: Range + 0.3f), "inside the tolerance");
            Assert.AreEqual(SpiritBumpDenyReason.None, Validate(distance: Range + Tolerance), "exactly on range + tolerance");
            Assert.AreEqual(SpiritBumpDenyReason.OutOfRange, Validate(distance: Range + Tolerance + 0.01f), "just outside");
        }

        [Test]
        public void Validate_UnknownOrNegativeDistance_IsOutOfRange()
        {
            Assert.AreEqual(SpiritBumpDenyReason.OutOfRange, Validate(distance: float.NaN));
            Assert.AreEqual(SpiritBumpDenyReason.OutOfRange, Validate(distance: -0.5f));
        }

        [Test]
        public void Validate_FirstFailureWins_InTheDocumentedOrder()
        {
            // Everything wrong at once: roles first, then focus, cooldown, range.
            Assert.AreEqual(SpiritBumpDenyReason.RequesterNotEvil,
                Validate(Role.Human, Role.Human, false, 99f, Now, CooldownRunning));
            Assert.AreEqual(SpiritBumpDenyReason.TargetNotGood,
                Validate(Role.EvilJinn, Role.Human, false, 99f, Now, CooldownRunning));
            Assert.AreEqual(SpiritBumpDenyReason.TargetNotFocusing,
                Validate(Role.EvilJinn, Role.GoodJinn, false, 99f, Now, CooldownRunning));
            Assert.AreEqual(SpiritBumpDenyReason.Cooldown,
                Validate(Role.EvilJinn, Role.GoodJinn, true, 99f, Now, CooldownRunning));
            Assert.AreEqual(SpiritBumpDenyReason.OutOfRange,
                Validate(Role.EvilJinn, Role.GoodJinn, true, 99f, Now, CooldownOver));
        }

        // ---- Settings ----

        [Test]
        public void SettingsFromConfig_TakeTheGddValues()
        {
            GameBalanceConfig config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            try
            {
                SpiritBumpSettings settings = SpiritBumpSettings.FromConfig(config);

                Assert.AreEqual(config.SpiritBumpRange, settings.Range);
                Assert.AreEqual(config.NetRangeTolerance, settings.Tolerance);
                Assert.AreEqual(config.SpiritBumpCooldown, settings.Cooldown);
                Assert.AreEqual(1.5f, settings.Range);
                Assert.AreEqual(6f, settings.Cooldown);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void SettingsFromConfig_NullThrows()
        {
            Assert.Throws<System.ArgumentNullException>(() => SpiritBumpSettings.FromConfig(null));
        }
    }
}
