using System;
using CinliMahzen.Core;
using CinliMahzen.Player;
using NUnit.Framework;

namespace CinliMahzen.Tests.EditMode.Player
{
    public class HumanHealthMirrorTests
    {
        private const int MaxHp = 3;

        private static HumanHealthMirror NewMirror()
        {
            return new HumanHealthMirror(MaxHp);
        }

        [Test]
        public void New_StartsStandingAtFullHp()
        {
            HumanHealthMirror m = NewMirror();

            Assert.AreEqual(MaxHp, m.MaxHp);
            Assert.AreEqual(MaxHp, m.Hp);
            Assert.AreEqual(0, m.Faints);
            Assert.IsFalse(m.IsFainted);
        }

        [Test]
        public void New_RejectsMaxHpBelowOne()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new HumanHealthMirror(0));
        }

        [Test]
        public void ApplyDamaged_TakesTheAuthoritysHp()
        {
            HumanHealthMirror m = NewMirror();

            m.ApplyDamaged(2);

            Assert.AreEqual(2, m.Hp);
            Assert.IsFalse(m.IsFainted);
            Assert.AreEqual(0, m.Faints);
        }

        [Test]
        public void ApplyDamaged_ClampsToZeroAndMax()
        {
            HumanHealthMirror m = NewMirror();

            m.ApplyDamaged(-4);
            Assert.AreEqual(0, m.Hp);

            m.ApplyDamaged(MaxHp + 5);
            Assert.AreEqual(MaxHp, m.Hp);
        }

        [Test]
        public void ApplyFainted_LaysTheHumanDownWithZeroHp()
        {
            HumanHealthMirror m = NewMirror();
            m.ApplyDamaged(0);

            m.ApplyFainted(1);

            Assert.IsTrue(m.IsFainted);
            Assert.AreEqual(0, m.Hp);
            Assert.AreEqual(1, m.Faints);
        }

        [Test]
        public void ApplyFainted_NegativeCountBecomesZero()
        {
            HumanHealthMirror m = NewMirror();

            m.ApplyFainted(-2);

            Assert.AreEqual(0, m.Faints);
            Assert.IsTrue(m.IsFainted);
        }

        [Test]
        public void ApplyRevived_StandsUpWithTheReportedHpAndKeepsFaints()
        {
            HumanHealthMirror m = NewMirror();
            m.ApplyDamaged(0);
            m.ApplyFainted(2);

            bool wasFainted = m.ApplyRevived(MaxHp);

            Assert.IsTrue(wasFainted);
            Assert.IsFalse(m.IsFainted);
            Assert.AreEqual(MaxHp, m.Hp);
            Assert.AreEqual(2, m.Faints, "a revive never refunds a faint");
        }

        [Test]
        public void ApplyRevived_WhileStanding_ReportsItAndStillAppliesTheAuthoritysHp()
        {
            HumanHealthMirror m = NewMirror();
            m.ApplyDamaged(1);

            bool wasFainted = m.ApplyRevived(MaxHp);

            Assert.IsFalse(wasFainted, "this client missed the faint message");
            Assert.IsFalse(m.IsFainted);
            Assert.AreEqual(MaxHp, m.Hp);
        }

        [Test]
        public void Reset_RestoresFullHpAndClearsFaints()
        {
            HumanHealthMirror m = NewMirror();
            m.ApplyDamaged(0);
            m.ApplyFainted(3);

            m.Reset();

            Assert.AreEqual(MaxHp, m.Hp);
            Assert.AreEqual(0, m.Faints);
            Assert.IsFalse(m.IsFainted);
        }

        /// <summary>
        /// The authority's rules (HumanHealthCore) turned into the three messages the MonoBehaviour broadcasts, replayed into the
        /// mirror the way every client does: after each step the mirror shows exactly what the core decided.
        /// </summary>
        [Test]
        public void FollowsTheCore_ThroughFaintCologneReviveAndLoss()
        {
            var settings = new HumanHealthSettings(MaxHp, 1.5f, 12f, 3);
            var core = new HumanHealthCore(settings);
            HumanHealthMirror m = NewMirror();
            var hit = new DamageInfo { Amount = MaxHp, Flags = DamageFlags.IgnoreInvuln, CauseId = "test.faint" };
            double now = 100d;

            for (int faint = 1; faint <= 3; faint++)
            {
                DamageResult r = core.ApplyDamage(hit, now);
                Assert.IsTrue(r.IsApplied);
                m.ApplyDamaged(r.HpAfter);
                m.ApplyFainted(r.FaintCount);

                Assert.IsTrue(m.IsFainted, "faint " + faint);
                Assert.AreEqual(core.Hp, m.Hp, "faint " + faint);
                Assert.AreEqual(core.Faints, m.Faints, "faint " + faint);
                Assert.AreEqual(faint == 3 ? DamageOutcome.Lost : DamageOutcome.Fainted, r.Outcome);

                if (faint < 3)
                {
                    Assert.IsTrue(core.TryReviveWithCologne());
                    Assert.IsTrue(m.ApplyRevived(core.Hp));
                    Assert.IsFalse(m.IsFainted);
                    Assert.AreEqual(core.Hp, m.Hp);
                    Assert.AreEqual(core.Faints, m.Faints);
                }
                now += 1d;
            }

            Assert.IsFalse(core.TryReviveWithCologne(), "a lost human never gets up");
            Assert.IsTrue(core.HasLost);
            Assert.IsTrue(m.IsFainted);
            Assert.AreEqual(3, m.Faints);
        }
    }
}
