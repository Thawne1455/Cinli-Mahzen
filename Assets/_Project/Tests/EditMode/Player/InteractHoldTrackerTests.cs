using CinliMahzen.Core;
using CinliMahzen.Player;
using NUnit.Framework;

namespace CinliMahzen.Tests.EditMode.Player
{
    public class InteractHoldTrackerTests
    {
        private static readonly NetId Target = new NetId(1001);
        private static readonly NetId OtherTarget = new NetId(1002);

        private InteractHoldTracker _tracker;

        [SetUp]
        public void SetUp()
        {
            _tracker = new InteractHoldTracker();
        }

        [Test]
        public void HoldOneSecond_CompletesOnTheFourthQuarterSecond()
        {
            Assert.AreEqual(HoldStep.Holding, _tracker.Update(Target, 1f, true, 0.25f));
            Assert.AreEqual(0.25f, _tracker.Progress, 1e-6f);
            Assert.AreEqual(HoldStep.Holding, _tracker.Update(Target, 1f, true, 0.25f));
            Assert.AreEqual(HoldStep.Holding, _tracker.Update(Target, 1f, true, 0.25f));
            Assert.AreEqual(HoldStep.Completed, _tracker.Update(Target, 1f, true, 0.25f));
            Assert.AreEqual(1f, _tracker.Progress, 1e-6f);
        }

        [Test]
        public void EarlyRelease_Cancels_AndResetsProgress()
        {
            Assert.AreEqual(HoldStep.Holding, _tracker.Update(Target, 1f, true, 0.5f));

            Assert.AreEqual(HoldStep.Cancelled, _tracker.Update(Target, 1f, false, 0.1f));
            Assert.AreEqual(0f, _tracker.Progress);
            Assert.AreEqual(HoldStep.Idle, _tracker.Update(Target, 1f, false, 0.1f));
        }

        [Test]
        public void TargetChangeMidHold_ResetsProgress_AndCancels()
        {
            Assert.AreEqual(HoldStep.Holding, _tracker.Update(Target, 1f, true, 0.5f));

            Assert.AreEqual(HoldStep.Cancelled, _tracker.Update(OtherTarget, 1f, true, 0.1f));
            Assert.AreEqual(0f, _tracker.Progress);
        }

        [Test]
        public void TargetChangeMidHold_DoesNotCarryTheHoldOntoTheNewTarget()
        {
            _tracker.Update(Target, 1f, true, 0.5f);
            _tracker.Update(OtherTarget, 1f, true, 0.1f);

            Assert.AreEqual(HoldStep.Idle, _tracker.Update(OtherTarget, 1f, true, 1f));
            Assert.AreEqual(HoldStep.Idle, _tracker.Update(OtherTarget, 1f, false, 0f));
            Assert.AreEqual(HoldStep.Holding, _tracker.Update(OtherTarget, 1f, true, 0.5f));
        }

        [Test]
        public void ZeroHoldTime_CompletesOnThePressFrame()
        {
            Assert.AreEqual(HoldStep.Completed, _tracker.Update(Target, 0f, true, 0f));
            Assert.AreEqual(1f, _tracker.Progress);
        }

        [Test]
        public void Completed_DoesNotRetriggerWithoutRelease()
        {
            Assert.AreEqual(HoldStep.Completed, _tracker.Update(Target, 1f, true, 1f));

            Assert.AreEqual(HoldStep.Idle, _tracker.Update(Target, 1f, true, 1f));
            Assert.AreEqual(HoldStep.Idle, _tracker.Update(Target, 0f, true, 0f));
            Assert.AreEqual(1f, _tracker.Progress, "Progress stays full until the button is released");
        }

        [Test]
        public void Completed_ThenRelease_AllowsANewHold()
        {
            _tracker.Update(Target, 1f, true, 1f);
            Assert.AreEqual(HoldStep.Idle, _tracker.Update(Target, 1f, false, 0f));

            Assert.AreEqual(HoldStep.Holding, _tracker.Update(Target, 1f, true, 0.5f));
            Assert.AreEqual(0.5f, _tracker.Progress, 1e-6f);
        }

        [Test]
        public void NoTarget_PressDoesNothing_AndIsNotCarriedOntoALaterTarget()
        {
            Assert.AreEqual(HoldStep.Idle, _tracker.Update(NetId.Invalid, 1f, true, 0.5f));
            Assert.AreEqual(HoldStep.Idle, _tracker.Update(Target, 1f, true, 1f));
            Assert.AreEqual(0f, _tracker.Progress);

            Assert.AreEqual(HoldStep.Idle, _tracker.Update(Target, 1f, false, 0f));
            Assert.AreEqual(HoldStep.Holding, _tracker.Update(Target, 1f, true, 0.5f));
        }

        [Test]
        public void LosingTheTargetWhileHolding_Cancels()
        {
            Assert.AreEqual(HoldStep.Holding, _tracker.Update(Target, 1f, true, 0.5f));

            Assert.AreEqual(HoldStep.Cancelled, _tracker.Update(NetId.Invalid, 0f, true, 0.1f));
            Assert.AreEqual(0f, _tracker.Progress);
        }

        [Test]
        public void Reset_DropsAHoldInProgress()
        {
            _tracker.Update(Target, 1f, true, 0.5f);
            _tracker.Reset();

            Assert.AreEqual(0f, _tracker.Progress);
            Assert.AreEqual(HoldStep.Idle, _tracker.Update(Target, 1f, false, 0f));
        }
    }
}
