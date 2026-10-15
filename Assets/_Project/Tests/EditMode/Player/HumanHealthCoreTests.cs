using System;
using CinliMahzen.Core;
using CinliMahzen.Player;
using NUnit.Framework;
using UnityEngine;

namespace CinliMahzen.Tests.EditMode.Player
{
    public class HumanHealthCoreTests
    {
        // Binary-exact values so the time-boundary asserts below are exact.
        private const int MaxHp = 3;
        private const float Invuln = 1.5f;
        private const float FaintTime = 12f;
        private const int FaintsToLose = 3;

        private static HumanHealthSettings Settings(int faintsToLose = FaintsToLose)
        {
            return new HumanHealthSettings(MaxHp, Invuln, FaintTime, faintsToLose);
        }

        private static HumanHealthCore NewCore(int faintsToLose = FaintsToLose)
        {
            return new HumanHealthCore(Settings(faintsToLose));
        }

        private static DamageInfo Hit(int amount = 1, DamageFlags flags = DamageFlags.None, float knockdownTime = 0f)
        {
            return new DamageInfo
            {
                Amount = amount,
                Attacker = new PlayerId(2),
                SourceObject = new NetId(1001),
                CauseId = "test.hit",
                Flags = flags,
                KnockdownTime = knockdownTime,
                Point = new Vector3(1f, 2f, 3f),
                Direction = Vector3.forward,
            };
        }

        /// <summary>Knocks the human down with one lethal hit at <paramref name="now"/>.</summary>
        private static DamageResult Faint(HumanHealthCore core, double now)
        {
            return core.ApplyDamage(Hit(MaxHp), now);
        }

        // ---------------------------------------------------------------- construction

        [Test]
        public void New_StartsAtFullHealth()
        {
            var core = NewCore();
            Assert.AreEqual(MaxHp, core.MaxHp);
            Assert.AreEqual(MaxHp, core.Hp);
            Assert.AreEqual(0, core.Faints);
            Assert.IsFalse(core.IsFainted);
            Assert.IsFalse(core.HasLost);
            Assert.IsFalse(core.IsInvulnerable(0d));
        }

        [Test]
        public void New_RejectsInvalidSettings()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new HumanHealthCore(new HumanHealthSettings(0, Invuln, FaintTime, FaintsToLose)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new HumanHealthCore(new HumanHealthSettings(MaxHp, Invuln, FaintTime, 0)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new HumanHealthCore(default(HumanHealthSettings)));
        }

        [Test]
        public void Settings_FromConfig_MirrorsConfigValues()
        {
            var config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            try
            {
                var s = HumanHealthSettings.FromConfig(config);
                Assert.AreEqual(config.HumanMaxHp, s.MaxHp);
                Assert.AreEqual(config.HumanInvulnAfterHit, s.InvulnAfterHit);
                Assert.AreEqual(config.FaintDuration, s.FaintDuration);
                Assert.AreEqual(config.FaintsToLose, s.FaintsToLose);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void Settings_FromConfig_NullThrows()
        {
            Assert.Throws<ArgumentNullException>(() => HumanHealthSettings.FromConfig(null));
        }

        // ---------------------------------------------------------------- damage + invulnerability

        [Test]
        public void ApplyDamage_ReducesHp_AndReportsDamaged()
        {
            var core = NewCore();
            var r = core.ApplyDamage(Hit(), 10d);

            Assert.AreEqual(DamageOutcome.Damaged, r.Outcome);
            Assert.IsTrue(r.IsApplied);
            Assert.AreEqual(2, r.HpAfter);
            Assert.AreEqual(0, r.FaintCount);
            Assert.AreEqual(2, core.Hp);
            Assert.IsFalse(core.IsFainted);
        }

        [Test]
        public void ApplyDamage_StartsInvulnerabilityWindow()
        {
            var core = NewCore();
            core.ApplyDamage(Hit(), 10d);

            Assert.AreEqual(11.5d, core.InvulnUntil);
            Assert.IsTrue(core.IsInvulnerable(10d));
            Assert.IsTrue(core.IsInvulnerable(11.49d));
            Assert.IsFalse(core.IsInvulnerable(11.5d));
            Assert.IsFalse(core.IsInvulnerable(20d));
        }

        [Test]
        public void ApplyDamage_DuringInvulnerability_IsIgnored_AndDoesNotExtendWindow()
        {
            var core = NewCore();
            core.ApplyDamage(Hit(), 10d);

            var r = core.ApplyDamage(Hit(), 11d);

            Assert.AreEqual(DamageOutcome.Ignored, r.Outcome);
            Assert.IsFalse(r.IsApplied);
            Assert.AreEqual(2, r.HpAfter);
            Assert.AreEqual(0, r.FaintCount);
            Assert.AreEqual(2, core.Hp);
            Assert.AreEqual(11.5d, core.InvulnUntil);
        }

        [Test]
        public void ApplyDamage_JustBeforeWindowEnds_IsIgnored()
        {
            var core = NewCore();
            core.ApplyDamage(Hit(), 10d);

            Assert.AreEqual(DamageOutcome.Ignored, core.ApplyDamage(Hit(), 11.5d - 1e-9d).Outcome);
            Assert.AreEqual(2, core.Hp);
        }

        [Test]
        public void ApplyDamage_ExactlyAtWindowEnd_Lands()
        {
            var core = NewCore();
            core.ApplyDamage(Hit(), 10d);

            var r = core.ApplyDamage(Hit(), 11.5d);

            Assert.AreEqual(DamageOutcome.Damaged, r.Outcome);
            Assert.AreEqual(1, r.HpAfter);
            Assert.AreEqual(13d, core.InvulnUntil);
        }

        [Test]
        public void ApplyDamage_IgnoreInvulnFlag_BypassesWindow_AndRestartsIt()
        {
            var core = NewCore();
            core.ApplyDamage(Hit(), 10d);

            var r = core.ApplyDamage(Hit(1, DamageFlags.IgnoreInvuln), 10.25d);

            Assert.AreEqual(DamageOutcome.Damaged, r.Outcome);
            Assert.AreEqual(1, r.HpAfter);
            Assert.AreEqual(11.75d, core.InvulnUntil);
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(int.MinValue)]
        public void ApplyDamage_NonPositiveAmount_IsIgnored_EvenWithIgnoreInvuln(int amount)
        {
            var core = NewCore();

            var r = core.ApplyDamage(Hit(amount, DamageFlags.IgnoreInvuln), 10d);

            Assert.AreEqual(DamageOutcome.Ignored, r.Outcome);
            Assert.AreEqual(MaxHp, core.Hp);
            Assert.IsFalse(core.IsInvulnerable(10d), "an ignored hit must not start the window");
        }

        [Test]
        public void ApplyDamage_BiggerThanHp_ClampsAtZero()
        {
            var core = NewCore();

            var r = core.ApplyDamage(Hit(99), 10d);

            Assert.AreEqual(DamageOutcome.Fainted, r.Outcome);
            Assert.AreEqual(0, r.HpAfter);
            Assert.AreEqual(0, core.Hp);
        }

        [Test]
        public void ApplyDamage_ExactlyMaxHp_Faints()
        {
            var core = NewCore();
            Assert.AreEqual(DamageOutcome.Fainted, core.ApplyDamage(Hit(MaxHp), 10d).Outcome);
        }

        // ---------------------------------------------------------------- fainting

        [Test]
        public void ThreeSingleHits_FaintOnTheThird()
        {
            var core = NewCore();

            var first = core.ApplyDamage(Hit(), 10d);
            var second = core.ApplyDamage(Hit(), 20d);
            var third = core.ApplyDamage(Hit(), 30d);

            Assert.AreEqual(DamageOutcome.Damaged, first.Outcome);
            Assert.AreEqual(DamageOutcome.Damaged, second.Outcome);
            Assert.AreEqual(DamageOutcome.Fainted, third.Outcome);
            Assert.AreEqual(0, third.HpAfter);
            Assert.AreEqual(1, third.FaintCount);
            Assert.AreEqual(0, core.Hp);
            Assert.AreEqual(1, core.Faints);
            Assert.IsTrue(core.IsFainted);
            Assert.IsFalse(core.HasLost);
            Assert.AreEqual(42d, core.FaintedUntil);
        }

        [Test]
        public void ApplyDamage_WhileFainted_IsIgnored()
        {
            var core = NewCore();
            Faint(core, 10d);

            var r = core.ApplyDamage(Hit(), 10.5d);

            Assert.AreEqual(DamageOutcome.Ignored, r.Outcome);
            Assert.AreEqual(0, r.HpAfter);
            Assert.AreEqual(1, r.FaintCount);
            Assert.AreEqual(1, core.Faints);
            Assert.AreEqual(0, core.Hp);
        }

        [Test]
        public void ApplyDamage_WhileFainted_IsIgnored_EvenWithIgnoreInvuln_AndAfterWindow()
        {
            var core = NewCore();
            Faint(core, 10d);

            Assert.AreEqual(DamageOutcome.Ignored, core.ApplyDamage(Hit(1, DamageFlags.IgnoreInvuln), 10.5d).Outcome);
            Assert.AreEqual(DamageOutcome.Ignored, core.ApplyDamage(Hit(), 15d).Outcome, "window is over, still down");
            Assert.AreEqual(1, core.Faints);
        }

        [Test]
        public void Faint_AlsoStartsInvulnerabilityWindow()
        {
            var core = NewCore();
            Faint(core, 10d);
            Assert.AreEqual(11.5d, core.InvulnUntil);
        }

        // ---------------------------------------------------------------- natural revive

        [Test]
        public void Tick_BeforeFaintDuration_StaysFainted()
        {
            var core = NewCore();
            Faint(core, 10d);

            Assert.IsFalse(core.Tick(10d));
            Assert.IsFalse(core.Tick(21.99d));
            Assert.IsTrue(core.IsFainted);
            Assert.AreEqual(0, core.Hp);
        }

        [Test]
        public void Tick_AtFaintDuration_RevivesWithFullHp_AndKeepsFaintCount()
        {
            var core = NewCore();
            Faint(core, 10d);

            Assert.IsTrue(core.Tick(22d));

            Assert.IsFalse(core.IsFainted);
            Assert.AreEqual(MaxHp, core.Hp);
            Assert.AreEqual(1, core.Faints);
            Assert.IsFalse(core.HasLost);
        }

        [Test]
        public void Tick_AfterRevive_ReturnsFalse()
        {
            var core = NewCore();
            Faint(core, 10d);
            Assert.IsTrue(core.Tick(22d));

            Assert.IsFalse(core.Tick(22d));
            Assert.IsFalse(core.Tick(100d));
        }

        [Test]
        public void Tick_WhenNeverFainted_ReturnsFalse()
        {
            var core = NewCore();
            Assert.IsFalse(core.Tick(1000d));
            Assert.AreEqual(MaxHp, core.Hp);
        }

        [Test]
        public void Tick_WhenDamagedButNotFainted_DoesNotHeal()
        {
            var core = NewCore();
            core.ApplyDamage(Hit(), 10d);

            Assert.IsFalse(core.Tick(1000d));
            Assert.AreEqual(2, core.Hp);
        }

        [Test]
        public void AfterTickRevive_DamageApplies_Again()
        {
            var core = NewCore();
            Faint(core, 10d);
            core.Tick(22d);

            var r = core.ApplyDamage(Hit(), 22d);

            Assert.AreEqual(DamageOutcome.Damaged, r.Outcome);
            Assert.AreEqual(MaxHp - 1, r.HpAfter);
            Assert.AreEqual(1, r.FaintCount);
        }

        // ---------------------------------------------------------------- cologne

        [Test]
        public void Cologne_WhenNotFainted_ReturnsFalse_AndDoesNotHeal()
        {
            var core = NewCore();
            Assert.IsFalse(core.TryReviveWithCologne());

            core.ApplyDamage(Hit(), 10d);
            Assert.IsFalse(core.TryReviveWithCologne());
            Assert.AreEqual(2, core.Hp);
        }

        [Test]
        public void Cologne_WhenFainted_RevivesInstantly_WithFullHp_AndKeepsFaintCount()
        {
            var core = NewCore();
            Faint(core, 10d);

            Assert.IsTrue(core.TryReviveWithCologne());

            Assert.IsFalse(core.IsFainted);
            Assert.AreEqual(MaxHp, core.Hp);
            Assert.AreEqual(1, core.Faints, "cologne must not refund the faint");
            Assert.IsFalse(core.HasLost);
        }

        [Test]
        public void Cologne_Twice_SecondCallReturnsFalse()
        {
            var core = NewCore();
            Faint(core, 10d);

            Assert.IsTrue(core.TryReviveWithCologne());
            Assert.IsFalse(core.TryReviveWithCologne());
        }

        [Test]
        public void Cologne_ThenTick_DoesNotReviveAgain()
        {
            var core = NewCore();
            Faint(core, 10d);
            core.TryReviveWithCologne();

            Assert.IsFalse(core.Tick(22d));
            Assert.IsFalse(core.Tick(100d));
            Assert.AreEqual(MaxHp, core.Hp);
        }

        [Test]
        public void AfterCologneRevive_DamageApplies_Again()
        {
            var core = NewCore();
            Faint(core, 10d);
            core.TryReviveWithCologne();

            var r = core.ApplyDamage(Hit(1, DamageFlags.IgnoreInvuln), 10.25d);

            Assert.AreEqual(DamageOutcome.Damaged, r.Outcome);
            Assert.AreEqual(MaxHp - 1, r.HpAfter);
        }

        // ---------------------------------------------------------------- losing

        [Test]
        public void ThirdFaint_ReturnsLost()
        {
            var core = NewCore();

            Assert.AreEqual(DamageOutcome.Fainted, Faint(core, 10d).Outcome);
            core.Tick(22d);
            Assert.AreEqual(DamageOutcome.Fainted, Faint(core, 30d).Outcome);
            core.Tick(42d);
            var third = Faint(core, 50d);

            Assert.AreEqual(DamageOutcome.Lost, third.Outcome);
            Assert.IsTrue(third.IsApplied);
            Assert.AreEqual(0, third.HpAfter);
            Assert.AreEqual(3, third.FaintCount);
            Assert.AreEqual(3, core.Faints);
            Assert.IsTrue(core.HasLost);
            Assert.IsTrue(core.IsFainted, "the human is still lying on the floor");
            Assert.AreEqual(0, core.Hp);
        }

        [Test]
        public void ColognedFaints_StillCountTowardsLosing()
        {
            var core = NewCore();

            Faint(core, 10d);
            core.TryReviveWithCologne();
            Faint(core, 20d);
            core.TryReviveWithCologne();
            var third = Faint(core, 30d);

            Assert.AreEqual(DamageOutcome.Lost, third.Outcome);
            Assert.AreEqual(3, core.Faints);
        }

        [Test]
        public void Lost_NeverRevives_NeitherByTickNorCologne()
        {
            var core = NewCore();
            for (int i = 0; i < FaintsToLose; i++)
            {
                Faint(core, 10d * (i + 1));
                core.TryReviveWithCologne();
            }

            Assert.IsTrue(core.HasLost);
            Assert.IsFalse(core.Tick(10000d));
            Assert.IsFalse(core.TryReviveWithCologne());
            Assert.IsTrue(core.IsFainted);
            Assert.AreEqual(0, core.Hp);
        }

        [Test]
        public void Lost_IgnoresFurtherDamage()
        {
            var core = NewCore();
            for (int i = 0; i < FaintsToLose; i++)
            {
                Faint(core, 10d * (i + 1));
                core.TryReviveWithCologne();
            }

            var r = core.ApplyDamage(Hit(1, DamageFlags.IgnoreInvuln), 1000d);

            Assert.AreEqual(DamageOutcome.Ignored, r.Outcome);
            Assert.AreEqual(3, r.FaintCount);
            Assert.AreEqual(3, core.Faints, "the counter must not run past FaintsToLose");
        }

        [Test]
        public void FaintsToLoseOne_FirstFaintLoses()
        {
            var core = NewCore(1);

            var r = Faint(core, 10d);

            Assert.AreEqual(DamageOutcome.Lost, r.Outcome);
            Assert.AreEqual(1, r.FaintCount);
            Assert.IsTrue(core.HasLost);
            Assert.IsFalse(core.TryReviveWithCologne());
        }

        // ---------------------------------------------------------------- knockdown pass-through

        [Test]
        public void Knockdown_FlagAndTime_AreForwarded()
        {
            var core = NewCore();

            var r = core.ApplyDamage(Hit(1, DamageFlags.Knockdown, 2f), 10d);

            Assert.AreEqual(DamageOutcome.Damaged, r.Outcome);
            Assert.IsTrue(r.Knockdown);
            Assert.AreEqual(2f, r.KnockdownTime);
        }

        [Test]
        public void Knockdown_WithoutFlag_TimeIsNotForwarded()
        {
            var core = NewCore();

            var r = core.ApplyDamage(Hit(1, DamageFlags.None, 2f), 10d);

            Assert.IsFalse(r.Knockdown);
            Assert.AreEqual(0f, r.KnockdownTime);
        }

        [Test]
        public void Knockdown_IgnoreInvulnCombination_IsForwarded()
        {
            var core = NewCore();
            core.ApplyDamage(Hit(), 10d);

            var r = core.ApplyDamage(Hit(1, DamageFlags.Knockdown | DamageFlags.IgnoreInvuln, 2f), 10.5d);

            Assert.IsTrue(r.Knockdown);
            Assert.AreEqual(2f, r.KnockdownTime);
        }

        [Test]
        public void Knockdown_OnFaintingHit_IsStillForwarded()
        {
            var core = NewCore();

            var r = core.ApplyDamage(Hit(MaxHp, DamageFlags.Knockdown, 2f), 10d);

            Assert.AreEqual(DamageOutcome.Fainted, r.Outcome);
            Assert.IsTrue(r.Knockdown);
            Assert.AreEqual(2f, r.KnockdownTime);
        }

        [Test]
        public void Knockdown_OnIgnoredHit_IsNotForwarded()
        {
            var core = NewCore();
            core.ApplyDamage(Hit(), 10d);

            var r = core.ApplyDamage(Hit(1, DamageFlags.Knockdown, 2f), 10.5d);

            Assert.AreEqual(DamageOutcome.Ignored, r.Outcome);
            Assert.IsFalse(r.Knockdown);
            Assert.AreEqual(0f, r.KnockdownTime);
        }

        // ---------------------------------------------------------------- reset

        [Test]
        public void ResetForRound_FromLostState_RestoresEverything()
        {
            var core = NewCore();
            for (int i = 0; i < FaintsToLose; i++)
            {
                Faint(core, 10d * (i + 1));
                core.TryReviveWithCologne();
            }

            Assert.IsTrue(core.HasLost);

            core.ResetForRound();

            Assert.AreEqual(MaxHp, core.Hp);
            Assert.AreEqual(0, core.Faints);
            Assert.IsFalse(core.IsFainted);
            Assert.IsFalse(core.HasLost);
            Assert.AreEqual(0d, core.FaintedUntil);
            Assert.AreEqual(0d, core.InvulnUntil);
            Assert.IsFalse(core.IsInvulnerable(0d));
        }

        [Test]
        public void ResetForRound_ClearsInvulnerabilityWindow()
        {
            var core = NewCore();
            core.ApplyDamage(Hit(), 10d);
            Assert.IsTrue(core.IsInvulnerable(10.1d));

            core.ResetForRound();

            Assert.IsFalse(core.IsInvulnerable(10.1d));
            Assert.AreEqual(DamageOutcome.Damaged, core.ApplyDamage(Hit(), 10.1d).Outcome);
        }

        [Test]
        public void ResetForRound_WhileFainted_RevivesWithoutTick()
        {
            var core = NewCore();
            Faint(core, 10d);

            core.ResetForRound();

            Assert.IsFalse(core.IsFainted);
            Assert.IsFalse(core.Tick(22d));
            Assert.AreEqual(MaxHp, core.Hp);
            Assert.AreEqual(0, core.Faints);
        }
    }
}
