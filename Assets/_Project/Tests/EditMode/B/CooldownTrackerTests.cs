using CinliMahzen.Possession;
using NUnit.Framework;

namespace CinliMahzen.Tests.EditMode.B
{
    public class CooldownTrackerTests
    {
        private const double Eps = 1e-6;

        [Test]
        public void UnknownKey_IsReady()
        {
            var t = new CooldownTracker();
            Assert.IsTrue(t.IsReady(42, 0d));
            Assert.AreEqual(0d, t.Remaining(42, 0d), Eps);
        }

        [Test]
        public void Start_BlocksUntilDurationElapsed()
        {
            var t = new CooldownTracker();
            t.Start(1, 10d, 6f);
            Assert.IsFalse(t.IsReady(1, 10d));
            Assert.IsFalse(t.IsReady(1, 15.99d));
            Assert.IsTrue(t.IsReady(1, 16d));
            Assert.AreEqual(2d, t.Remaining(1, 14d), Eps);
        }

        [Test]
        public void Keys_AreIndependent()
        {
            var t = new CooldownTracker();
            long a = CooldownTracker.Key(1, 1000);
            long b = CooldownTracker.Key(1, 1001);
            long c = CooldownTracker.Key(2, 1000);
            t.Start(a, 0d, 10f);
            Assert.IsFalse(t.IsReady(a, 5d));
            Assert.IsTrue(t.IsReady(b, 5d));
            Assert.IsTrue(t.IsReady(c, 5d));
        }

        [Test]
        public void Key_DistinguishesComponentOrder()
        {
            Assert.AreNotEqual(CooldownTracker.Key(1, 2), CooldownTracker.Key(2, 1));
        }

        [Test]
        public void Restart_OverwritesReadyTime()
        {
            var t = new CooldownTracker();
            t.Start(1, 0d, 10f);
            t.Start(1, 20d, 5f);
            Assert.AreEqual(25d, t.ReadyAt(1), Eps);
            Assert.AreEqual(1, t.Count);
        }

        [Test]
        public void Rage_ShortensNewCooldowns()
        {
            var t = new CooldownTracker();
            t.SetMultiplier(0.6f, 0d);
            t.Start(1, 0d, 10f);
            Assert.AreEqual(6d, t.ReadyAt(1), 1e-4);
        }

        [Test]
        public void Rage_RescalesRunningCooldowns()
        {
            var t = new CooldownTracker();
            t.Start(1, 0d, 10f);          // ready at 10
            t.SetMultiplier(0.6f, 2d);    // 8 s left → 4.8 s
            Assert.AreEqual(6.8d, t.ReadyAt(1), 1e-4);
            Assert.IsFalse(t.IsReady(1, 6.7d));
            Assert.IsTrue(t.IsReady(1, 6.8d + Eps));
        }

        [Test]
        public void Rage_WithoutRescale_LeavesRunningCooldowns()
        {
            var t = new CooldownTracker();
            t.Start(1, 0d, 10f);
            t.SetMultiplier(0.6f, 2d, rescaleRunning: false);
            Assert.AreEqual(10d, t.ReadyAt(1), Eps);
        }

        [Test]
        public void Rage_DoesNotTouchFinishedCooldowns()
        {
            var t = new CooldownTracker();
            t.Start(1, 0d, 1f);
            t.SetMultiplier(0.6f, 5d);
            Assert.AreEqual(1d, t.ReadyAt(1), Eps);
            Assert.IsTrue(t.IsReady(1, 5d));
        }

        [Test]
        public void InvalidMultiplier_IsIgnored()
        {
            var t = new CooldownTracker();
            t.SetMultiplier(0f, 0d);
            Assert.AreEqual(1f, t.Multiplier);
            t.SetMultiplier(-1f, 0d);
            Assert.AreEqual(1f, t.Multiplier);
        }

        [Test]
        public void SetReadyAt_IgnoresMultiplier()
        {
            var t = new CooldownTracker(0.6f);
            t.SetReadyAt(7, 12d);
            Assert.AreEqual(12d, t.ReadyAt(7), Eps);
        }

        [Test]
        public void Clear_RemovesOnlyThatKey()
        {
            var t = new CooldownTracker();
            t.Start(1, 0d, 10f);
            t.Start(2, 0d, 10f);
            t.Start(3, 0d, 10f);
            t.Clear(1);
            Assert.IsTrue(t.IsReady(1, 0d));
            Assert.IsFalse(t.IsReady(2, 0d));
            Assert.IsFalse(t.IsReady(3, 0d));
            Assert.AreEqual(2, t.Count);
            t.Clear(99);
            Assert.AreEqual(2, t.Count);
        }

        [Test]
        public void Reset_ForgetsAllAndRestoresMultiplier()
        {
            var t = new CooldownTracker();
            t.SetMultiplier(0.6f, 0d);
            t.Start(1, 0d, 10f);
            t.Reset();
            Assert.AreEqual(0, t.Count);
            Assert.AreEqual(1f, t.Multiplier);
            Assert.IsTrue(t.IsReady(1, 0d));
        }

        [Test]
        public void ZeroDuration_IsReadyImmediately()
        {
            var t = new CooldownTracker();
            t.Start(1, 3d, 0f);
            Assert.IsTrue(t.IsReady(1, 3d));
        }
    }
}
