using CinliMahzen.Player;
using NUnit.Framework;

namespace CinliMahzen.Tests.EditMode.Player
{
    public class StaminaModelTests
    {
        // Oyun §14 initial values, but passed explicitly: the model takes whatever the config says.
        private const float Max = 100f;
        private const float Drain = 20f;
        private const float Regen = 15f;
        private const float Delay = 1f;
        private const float Epsilon = 1e-3f;

        private StaminaModel _stamina;

        [SetUp]
        public void SetUp()
        {
            _stamina = new StaminaModel(Max, Drain, Regen, Delay);
        }

        [Test]
        public void StartsFull_AndExposesConfig()
        {
            Assert.AreEqual(Max, _stamina.Value);
            Assert.AreEqual(Max, _stamina.Max);
            Assert.AreEqual(Drain, _stamina.Drain);
            Assert.AreEqual(Regen, _stamina.Regen);
            Assert.AreEqual(Delay, _stamina.RegenDelay);
        }

        [Test]
        public void Sprinting_DrainsPerSecond()
        {
            bool sprinting = _stamina.Tick(1f, true, true, 1d);

            Assert.IsTrue(sprinting);
            Assert.AreEqual(Max - Drain, _stamina.Value, Epsilon);
        }

        [Test]
        public void NoSprint_WhenNotWanted_OrNotMoving()
        {
            Assert.IsFalse(_stamina.Tick(1f, false, true, 1d));
            Assert.IsFalse(_stamina.Tick(1f, true, false, 2d));
            Assert.AreEqual(Max, _stamina.Value, Epsilon);
        }

        [Test]
        public void AtZero_NoSprint()
        {
            double now = 0d;
            // Max / Drain = 5 s of sprinting empties the bar.
            for (int i = 0; i < 5; i++)
            {
                now += 1d;
                Assert.IsTrue(_stamina.Tick(1f, true, true, now), "second " + i);
            }
            Assert.AreEqual(0f, _stamina.Value, Epsilon);

            now += 0.1d;
            Assert.IsFalse(_stamina.Tick(0.1f, true, true, now), "empty bar cannot sprint");
            Assert.AreEqual(0f, _stamina.Value, Epsilon);
        }

        [Test]
        public void Value_NeverGoesBelowZero_EvenWithHugeDt()
        {
            _stamina.Tick(100f, true, true, 100d);
            Assert.AreEqual(0f, _stamina.Value);
        }

        [Test]
        public void NoRegen_BeforeDelay_ThenRegen()
        {
            _stamina.Tick(2f, true, true, 2d); // 100 -> 60, last sprint at t = 2
            Assert.AreEqual(60f, _stamina.Value, Epsilon);

            _stamina.Tick(0.5f, false, false, 2.5d);
            _stamina.Tick(0.4f, false, false, 2.9d);
            Assert.AreEqual(60f, _stamina.Value, Epsilon, "regen waits for RegenDelay");

            _stamina.Tick(1f, false, false, 3.5d); // 1.5 s since the last sprint
            Assert.AreEqual(60f + Regen, _stamina.Value, Epsilon);
        }

        [Test]
        public void Regen_CapsAtMax()
        {
            _stamina.Tick(1f, true, true, 1d);
            _stamina.Tick(100f, false, false, 200d);

            Assert.AreEqual(Max, _stamina.Value);
        }

        [Test]
        public void ReleasedSprint_RestartsTheDelay()
        {
            _stamina.Tick(1f, true, true, 1d);   // 80
            _stamina.Tick(1f, false, false, 2.5d); // 1.5 s later: regen +15 -> 95
            Assert.AreEqual(95f, _stamina.Value, Epsilon);

            _stamina.Tick(0.1f, true, true, 2.6d); // sprint again: -2 -> 93, delay restarts
            _stamina.Tick(0.5f, false, false, 3.1d);
            Assert.AreEqual(93f, _stamina.Value, Epsilon, "delay counts from the latest sprint");
        }

        [Test]
        public void Sprint_ResumesAfterRegen()
        {
            _stamina.Tick(100f, true, true, 5d);   // empty
            Assert.IsFalse(_stamina.Tick(0.1f, true, true, 5.1d));

            _stamina.Tick(1f, false, false, 7d);   // 2 s later: +15
            Assert.AreEqual(Regen, _stamina.Value, Epsilon);
            Assert.IsTrue(_stamina.Tick(0.1f, true, true, 7.1d));
        }

        [Test]
        public void Reset_RefillsAndForgetsDelay()
        {
            _stamina.Tick(3f, true, true, 3d);
            _stamina.Reset();

            Assert.AreEqual(Max, _stamina.Value);
            _stamina.Tick(1f, true, true, 3.1d);
            _stamina.Reset();
            _stamina.Tick(1f, false, false, 3.2d);
            Assert.AreEqual(Max, _stamina.Value, Epsilon);
        }

        [Test]
        public void NegativeDt_IsTreatedAsZero()
        {
            _stamina.Tick(-1f, true, true, 1d);
            Assert.AreEqual(Max, _stamina.Value, Epsilon);
        }
    }
}
