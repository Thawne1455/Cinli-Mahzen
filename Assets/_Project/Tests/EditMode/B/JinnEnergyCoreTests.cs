using CinliMahzen.Jinn;
using NUnit.Framework;

namespace CinliMahzen.Tests.EditMode.B
{
    public class JinnEnergyCoreTests
    {
        // Oyun §12: EnergyMax 100, EnergyStart 50, EnergyRegen 5, RageRegenMult 2. Sync: Δ ≥ 1, 4 Hz.
        private static JinnEnergySettings Settings()
        {
            return new JinnEnergySettings(100f, 50f, 5f, 2f, 1f, 0.25f);
        }

        [Test]
        public void StartsAtEnergyStart()
        {
            var e = new JinnEnergyCore(Settings());
            Assert.AreEqual(50f, e.Current, 1e-4f);
            Assert.AreEqual(0.5f, e.Normalized, 1e-4f);
            Assert.IsFalse(e.RageActive);
        }

        [Test]
        public void Regen_FivePerSecond()
        {
            var e = new JinnEnergyCore(Settings());
            float gained = e.Tick(2f);
            Assert.AreEqual(10f, gained, 1e-4f);
            Assert.AreEqual(60f, e.Current, 1e-4f);
        }

        [Test]
        public void Regen_ClampsAtMax()
        {
            var e = new JinnEnergyCore(Settings());
            e.Tick(100f);
            Assert.AreEqual(100f, e.Current, 1e-4f);
            Assert.AreEqual(0f, e.Tick(1f), 1e-4f);
        }

        [Test]
        public void Rage_DoublesRegen()
        {
            var e = new JinnEnergyCore(Settings());
            e.SetRage(true);
            Assert.AreEqual(10f, e.EffectiveRegen, 1e-4f);
            e.Tick(2f);
            Assert.AreEqual(70f, e.Current, 1e-4f);
        }

        [Test]
        public void RageOff_RestoresNormalRegen()
        {
            var e = new JinnEnergyCore(Settings());
            e.SetRage(true);
            e.SetRage(false);
            e.Tick(1f);
            Assert.AreEqual(55f, e.Current, 1e-4f);
        }

        [Test]
        public void Spend_DeductsWhenAffordable()
        {
            var e = new JinnEnergyCore(Settings());
            Assert.IsTrue(e.TrySpend(40f));
            Assert.AreEqual(10f, e.Current, 1e-4f);
        }

        [Test]
        public void Spend_FailsWhenInsufficient_AndKeepsEnergy()
        {
            var e = new JinnEnergyCore(Settings());
            Assert.IsFalse(e.CanAfford(60f));
            Assert.IsFalse(e.TrySpend(60f));
            Assert.AreEqual(50f, e.Current, 1e-4f);
        }

        [Test]
        public void Spend_ExactAmount_Succeeds()
        {
            var e = new JinnEnergyCore(Settings());
            Assert.IsTrue(e.TrySpend(50f));
            Assert.AreEqual(0f, e.Current, 1e-4f);
        }

        [Test]
        public void Unlimited_NeverDeducts()
        {
            var e = new JinnEnergyCore(Settings()) { Unlimited = true };
            Assert.IsTrue(e.TrySpend(60f));
            Assert.IsTrue(e.TrySpend(1000f));
            Assert.AreEqual(50f, e.Current, 1e-4f);
        }

        [Test]
        public void Reset_RestoresStartAndClearsRage()
        {
            var e = new JinnEnergyCore(Settings());
            e.TrySpend(30f);
            e.SetRage(true);
            e.Reset();
            Assert.AreEqual(50f, e.Current, 1e-4f);
            Assert.IsFalse(e.RageActive);
        }

        [Test]
        public void ApplyRemote_Clamps()
        {
            var e = new JinnEnergyCore(Settings());
            e.ApplyRemote(130f);
            Assert.AreEqual(100f, e.Current, 1e-4f);
            e.ApplyRemote(-5f);
            Assert.AreEqual(0f, e.Current, 1e-4f);
        }

        [Test]
        public void Sync_ForcedAfterSpendAndRage()
        {
            var e = new JinnEnergyCore(Settings());
            Assert.IsTrue(e.NeedsSync(0d), "reset forces the first sync");
            e.MarkSynced(0d);
            Assert.IsFalse(e.NeedsSync(0d));

            e.TrySpend(10f);
            Assert.IsTrue(e.NeedsSync(0.01d), "spend is broadcast immediately");
            e.MarkSynced(0.01d);

            e.SetRage(true);
            Assert.IsTrue(e.NeedsSync(0.02d), "rage change is broadcast immediately");
        }

        [Test]
        public void Sync_RegenThrottledByDeltaAndRate()
        {
            var e = new JinnEnergyCore(Settings());
            e.MarkSynced(0d);

            e.Tick(0.1f);                        // +0.5 → below delta
            Assert.IsFalse(e.NeedsSync(0.1d));

            e.Tick(0.1f);                        // +1.0 total, but only 0.2 s since last sync (4 Hz)
            Assert.IsFalse(e.NeedsSync(0.2d));
            Assert.IsTrue(e.NeedsSync(0.25d));
            e.MarkSynced(0.25d);
            Assert.IsFalse(e.NeedsSync(0.3d));
        }

        [Test]
        public void Sync_ReachingMaxIsSentEvenBelowDelta()
        {
            var e = new JinnEnergyCore(Settings());
            e.ApplyRemote(99.8f);
            e.MarkSynced(0d);
            e.Tick(1f);                          // +0.2 → full
            Assert.IsTrue(e.NeedsSync(1d));
        }
    }
}
