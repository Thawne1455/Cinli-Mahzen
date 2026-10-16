using CinliMahzen.Player;
using NUnit.Framework;

namespace CinliMahzen.Tests.EditMode.Player
{
    public class YawSpinTests
    {
        // Mother's Slipper: 180 degrees in 0.15 s (PD_Slipper's CameraSpin180 status duration).
        private const float Degrees = 180f;
        private const float Duration = 0.15f;
        private const float Frame = 1f / 60f;
        private const float Epsilon = 1e-3f;

        private YawSpin _spin;

        [SetUp]
        public void SetUp()
        {
            _spin = new YawSpin();
        }

        [Test]
        public void Idle_StepReturnsZero()
        {
            Assert.IsFalse(_spin.IsActive);
            Assert.AreEqual(0f, _spin.Step(Frame));
        }

        [Test]
        public void SixtyFps_TotalsExactlyTheRequestedDegrees_AndEndsWithinTheDuration()
        {
            _spin.Begin(Degrees, Duration);
            Assert.IsTrue(_spin.IsActive);

            float total = 0f;
            float elapsed = 0f;
            int frames = 0;
            while (_spin.IsActive && frames < 100)
            {
                total += _spin.Step(Frame);
                elapsed += Frame;
                frames++;
            }

            Assert.IsFalse(_spin.IsActive, "spin must end");
            Assert.AreEqual(Degrees, total, Epsilon);
            Assert.LessOrEqual(elapsed, Duration + Frame + Epsilon, "ends at the end of the duration (+ one frame of rounding)");
            Assert.GreaterOrEqual(elapsed, Duration - Epsilon);
        }

        [Test]
        public void EachFrame_AdvancesLinearly()
        {
            _spin.Begin(Degrees, Duration);

            float first = _spin.Step(Frame);
            float second = _spin.Step(Frame);

            Assert.AreEqual(Degrees * Frame / Duration, first, Epsilon);
            Assert.AreEqual(first, second, Epsilon);
        }

        [Test]
        public void HugeDt_DoesNotOvershoot()
        {
            _spin.Begin(Degrees, Duration);

            float delta = _spin.Step(5f);

            Assert.AreEqual(Degrees, delta);
            Assert.IsFalse(_spin.IsActive);
            Assert.AreEqual(0f, _spin.Step(5f), "nothing left after the spin ended");
        }

        [Test]
        public void ZeroDuration_DeliversEverythingOnTheNextStep()
        {
            _spin.Begin(Degrees, 0f);
            Assert.IsTrue(_spin.IsActive);

            Assert.AreEqual(Degrees, _spin.Step(Frame));
            Assert.IsFalse(_spin.IsActive);
        }

        [Test]
        public void ZeroDt_ChangesNothing()
        {
            _spin.Begin(Degrees, Duration);

            Assert.AreEqual(0f, _spin.Step(0f));
            Assert.IsTrue(_spin.IsActive);
            Assert.AreEqual(Degrees, _spin.RemainingDegrees, Epsilon);
        }

        [Test]
        public void NegativeDegrees_KeepTheirSign()
        {
            _spin.Begin(-90f, 0.1f);

            float total = 0f;
            for (int i = 0; i < 20 && _spin.IsActive; i++)
                total += _spin.Step(Frame);

            Assert.AreEqual(-90f, total, Epsilon);
        }

        [Test]
        public void BeginWhileActive_KeepsTheRemainingDegrees()
        {
            _spin.Begin(Degrees, Duration);
            float done = _spin.Step(Duration * 0.5f);   // half way
            _spin.Begin(Degrees, Duration);              // a second hit

            float total = done;
            for (int i = 0; i < 100 && _spin.IsActive; i++)
                total += _spin.Step(Frame);

            Assert.AreEqual(2f * Degrees, total, Epsilon);
        }

        [Test]
        public void Cancel_StopsTheSpin()
        {
            _spin.Begin(Degrees, Duration);
            _spin.Step(Frame);
            _spin.Cancel();

            Assert.IsFalse(_spin.IsActive);
            Assert.AreEqual(0f, _spin.Step(Frame));
            Assert.AreEqual(0f, _spin.RemainingDegrees);
        }

        [Test]
        public void ZeroDegrees_NeverBecomesActive()
        {
            _spin.Begin(0f, Duration);
            Assert.IsFalse(_spin.IsActive);
        }
    }
}
