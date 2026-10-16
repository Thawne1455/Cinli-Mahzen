using CinliMahzen.Player;
using NUnit.Framework;

namespace CinliMahzen.Tests.EditMode.Player
{
    public class BlurPulseTests
    {
        // Binary-exact values so the time-boundary asserts below are exact.
        private const float Duration = 2f;   // SpiritBumpBlurTime
        private const float FadeIn = 0.25f;
        private const float FadeOut = 0.5f;
        private const float Frame = 1f / 60f;
        private const float Epsilon = 1e-4f;

        // No fades: the envelope is a plain 0 / 1 box, which makes the timing asserts simple.
        private static BlurPulse Box() => new BlurPulse(0f, 0f);

        private static BlurPulse Faded() => new BlurPulse(FadeIn, FadeOut);

        [Test]
        public void Idle_IsZeroAndInactive()
        {
            BlurPulse pulse = Box();

            Assert.IsFalse(pulse.IsActive);
            Assert.AreEqual(0f, pulse.Tick(Frame));
            Assert.AreEqual(0f, pulse.Intensity);
            Assert.AreEqual(0f, pulse.Remaining);
        }

        [Test]
        public void Trigger_ThenTicksDuringThePulse_ReportFullStrength()
        {
            BlurPulse pulse = Box();
            pulse.Trigger(Duration);

            Assert.IsTrue(pulse.IsActive);
            Assert.AreEqual(1f, pulse.Tick(0.5f));
            Assert.AreEqual(1f, pulse.Tick(0.5f));
            Assert.AreEqual(1f, pulse.Tick(0.5f));
            Assert.IsTrue(pulse.IsActive);
        }

        [Test]
        public void AfterTheDuration_IsZeroAndInactive()
        {
            BlurPulse pulse = Box();
            pulse.Trigger(Duration);

            pulse.Tick(1f);
            Assert.AreEqual(0f, pulse.Tick(1f), "exactly at the end");
            Assert.IsFalse(pulse.IsActive);
            Assert.AreEqual(0f, pulse.Tick(Frame), "stays off");
        }

        [Test]
        public void SixtyFps_EndsWithinTheDuration()
        {
            BlurPulse pulse = Faded();
            pulse.Trigger(Duration);

            float elapsed = 0f;
            int frames = 0;
            while (pulse.IsActive && frames < 1000)
            {
                pulse.Tick(Frame);
                elapsed += Frame;
                frames++;
            }

            Assert.IsFalse(pulse.IsActive);
            Assert.GreaterOrEqual(elapsed, Duration - Epsilon);
            Assert.LessOrEqual(elapsed, Duration + Frame + Epsilon);
        }

        [Test]
        public void HugeDt_EndsThePulse()
        {
            BlurPulse pulse = Box();
            pulse.Trigger(Duration);

            Assert.AreEqual(0f, pulse.Tick(60f));
            Assert.IsFalse(pulse.IsActive);
        }

        [Test]
        public void FadeIn_RampsUpToFullStrength()
        {
            BlurPulse pulse = Faded();
            pulse.Trigger(Duration);

            Assert.AreEqual(0f, pulse.Intensity, Epsilon, "right after the trigger");
            Assert.AreEqual(0.5f, pulse.Tick(FadeIn / 2f), Epsilon);
            Assert.AreEqual(1f, pulse.Tick(FadeIn / 2f), Epsilon);
            Assert.AreEqual(1f, pulse.Tick(0.5f), Epsilon, "plateau");
        }

        [Test]
        public void FadeOut_RampsDownToZeroAtTheEnd()
        {
            BlurPulse pulse = Faded();
            pulse.Trigger(Duration);

            pulse.Tick(Duration - FadeOut);   // start of the fade-out
            Assert.AreEqual(1f, pulse.Intensity, Epsilon);
            Assert.AreEqual(0.5f, pulse.Tick(FadeOut / 2f), Epsilon);
            Assert.AreEqual(0f, pulse.Tick(FadeOut / 2f), Epsilon);
            Assert.IsFalse(pulse.IsActive);
        }

        [Test]
        public void ShortPulse_NeverReachesFullStrengthWhenTheFadesOverlap()
        {
            BlurPulse pulse = Faded();
            pulse.Trigger(0.5f);   // shorter than fade-in + fade-out

            float peak = 0f;
            for (int i = 0; i < 100 && pulse.IsActive; i++)
                peak = System.Math.Max(peak, pulse.Tick(Frame));

            Assert.Less(peak, 1f);
            Assert.Greater(peak, 0f);
        }

        [Test]
        public void IntensityStaysInsideZeroToOne()
        {
            BlurPulse pulse = Faded();
            pulse.Trigger(Duration);

            for (int i = 0; i < 200; i++)
            {
                float v = pulse.Tick(Frame);
                Assert.GreaterOrEqual(v, 0f);
                Assert.LessOrEqual(v, 1f);
            }
        }

        // ---- re-triggering ----

        [Test]
        public void TriggerWhileActive_ExtendsTheEnd()
        {
            BlurPulse pulse = Box();
            pulse.Trigger(Duration);
            pulse.Tick(1.5f);                      // 0.5 s left
            Assert.AreEqual(0.5f, pulse.Remaining, Epsilon);

            pulse.Trigger(Duration);               // a second bump

            Assert.AreEqual(Duration, pulse.Remaining, Epsilon);
            Assert.AreEqual(1f, pulse.Tick(1.75f), "still blurred 1.75 s later");
            Assert.AreEqual(0f, pulse.Tick(0.25f));
        }

        [Test]
        public void TriggerWhileActive_WithAShorterDuration_DoesNotCutThePulseShort()
        {
            BlurPulse pulse = Box();
            pulse.Trigger(Duration);
            pulse.Tick(0.5f);                      // 1.5 s left

            pulse.Trigger(0.25f);

            Assert.AreEqual(1.5f, pulse.Remaining, Epsilon);
        }

        [Test]
        public void TheSameBumpTriggeredTwice_DoesNotDoubleTheTime()
        {
            // SpiritBumpedEvt and the StatusApplied(VisionBlurred) message can both reach the rig for one bump.
            BlurPulse pulse = Box();
            pulse.Trigger(Duration);
            pulse.Trigger(Duration);

            Assert.AreEqual(Duration, pulse.Remaining, Epsilon);
            pulse.Tick(Duration);
            Assert.IsFalse(pulse.IsActive);
        }

        [Test]
        public void TriggerWhileActive_DoesNotReplayTheFadeIn()
        {
            BlurPulse pulse = Faded();
            pulse.Trigger(Duration);
            pulse.Tick(1f);                        // plateau

            pulse.Trigger(Duration);

            Assert.AreEqual(1f, pulse.Intensity, Epsilon);
        }

        [Test]
        public void TriggerAfterTheEnd_StartsANewPulse()
        {
            BlurPulse pulse = Faded();
            pulse.Trigger(Duration);
            pulse.Tick(Duration + 1f);
            Assert.IsFalse(pulse.IsActive);

            pulse.Trigger(Duration);

            Assert.IsTrue(pulse.IsActive);
            Assert.AreEqual(Duration, pulse.Remaining, Epsilon);
            Assert.AreEqual(0f, pulse.Intensity, Epsilon, "fade-in starts over");
        }

        [Test]
        public void InvalidDurations_AreIgnored()
        {
            BlurPulse pulse = Box();

            pulse.Trigger(0f);
            pulse.Trigger(-1f);
            pulse.Trigger(float.NaN);

            Assert.IsFalse(pulse.IsActive);
            Assert.AreEqual(0f, pulse.Tick(Frame));
        }

        [Test]
        public void Cancel_EndsThePulseAtOnce()
        {
            BlurPulse pulse = Box();
            pulse.Trigger(Duration);

            pulse.Cancel();

            Assert.IsFalse(pulse.IsActive);
            Assert.AreEqual(0f, pulse.Intensity);
            Assert.AreEqual(0f, pulse.Tick(Frame));
        }

        [Test]
        public void ZeroDt_ChangesNothing()
        {
            BlurPulse pulse = Box();
            pulse.Trigger(Duration);

            pulse.Tick(0f);

            Assert.AreEqual(Duration, pulse.Remaining);
        }
    }
}
