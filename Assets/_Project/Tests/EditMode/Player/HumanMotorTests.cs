using CinliMahzen.Player;
using NUnit.Framework;

namespace CinliMahzen.Tests.EditMode.Player
{
    public class HumanMotorTests
    {
        private HumanMotor _motor;

        [SetUp]
        public void SetUp()
        {
            _motor = new HumanMotor();
        }

        [Test]
        public void StartsNormal_CanMoveLookAndSprint()
        {
            Assert.AreEqual(HumanMotorState.Normal, _motor.State);
            Assert.IsTrue(_motor.CanMove);
            Assert.IsTrue(_motor.CanLook);
            Assert.IsTrue(_motor.CanSprint);
        }

        [Test]
        public void Interacting_BlocksMoveAndSprint_ButNotLook()
        {
            Assert.IsTrue(_motor.EnterInteracting());

            Assert.AreEqual(HumanMotorState.Interacting, _motor.State);
            Assert.IsFalse(_motor.CanMove);
            Assert.IsFalse(_motor.CanSprint);
            Assert.IsTrue(_motor.CanLook);

            _motor.ExitInteracting();
            Assert.AreEqual(HumanMotorState.Normal, _motor.State);
            Assert.IsTrue(_motor.CanMove);
        }

        [Test]
        public void KnockedDown_BlocksMoveAndSprint_ButNotLook()
        {
            _motor.KnockDown(5d);

            Assert.AreEqual(HumanMotorState.KnockedDown, _motor.State);
            Assert.IsFalse(_motor.CanMove);
            Assert.IsFalse(_motor.CanSprint);
            Assert.IsTrue(_motor.CanLook);
        }

        [Test]
        public void Fainted_BlocksMoveSprintAndLook()
        {
            _motor.SetFainted(true);

            Assert.AreEqual(HumanMotorState.Fainted, _motor.State);
            Assert.IsFalse(_motor.CanMove);
            Assert.IsFalse(_motor.CanSprint);
            Assert.IsFalse(_motor.CanLook);

            _motor.SetFainted(false);
            Assert.AreEqual(HumanMotorState.Normal, _motor.State);
            Assert.IsTrue(_motor.CanLook);
        }

        [Test]
        public void KnockDown_EndsWhenTimeReachesUntil()
        {
            _motor.KnockDown(5d);

            _motor.Tick(4.99d);
            Assert.AreEqual(HumanMotorState.KnockedDown, _motor.State);

            _motor.Tick(5d);
            Assert.AreEqual(HumanMotorState.Normal, _motor.State);
            Assert.IsTrue(_motor.CanMove);
        }

        [Test]
        public void KnockDown_WhileFainted_IsIgnored()
        {
            _motor.SetFainted(true);
            _motor.KnockDown(5d);

            Assert.AreEqual(HumanMotorState.Fainted, _motor.State);
            _motor.Tick(10d);
            Assert.AreEqual(HumanMotorState.Fainted, _motor.State, "tick must not wake a fainted human");
        }

        [Test]
        public void Fainting_DropsPendingKnockdown()
        {
            _motor.KnockDown(5d);
            _motor.SetFainted(true);
            _motor.SetFainted(false);

            Assert.AreEqual(HumanMotorState.Normal, _motor.State);
            _motor.Tick(0d);
            Assert.AreEqual(HumanMotorState.Normal, _motor.State);
        }

        [Test]
        public void KnockDown_CancelsInteraction()
        {
            _motor.EnterInteracting();
            _motor.KnockDown(3d);

            Assert.AreEqual(HumanMotorState.KnockedDown, _motor.State);
            _motor.ExitInteracting();
            Assert.AreEqual(HumanMotorState.KnockedDown, _motor.State, "ExitInteracting must not end a knockdown");
        }

        [Test]
        public void EnterInteracting_RefusedWhenNotNormal()
        {
            _motor.KnockDown(3d);
            Assert.IsFalse(_motor.EnterInteracting());
            Assert.AreEqual(HumanMotorState.KnockedDown, _motor.State);

            _motor.SetFainted(true);
            Assert.IsFalse(_motor.EnterInteracting());
            Assert.AreEqual(HumanMotorState.Fainted, _motor.State);
        }

        [Test]
        public void SecondKnockDown_OnlyExtends()
        {
            _motor.KnockDown(5d);
            _motor.KnockDown(3d);
            Assert.AreEqual(5d, _motor.KnockedDownUntil);

            _motor.KnockDown(8d);
            Assert.AreEqual(8d, _motor.KnockedDownUntil);

            _motor.Tick(7d);
            Assert.AreEqual(HumanMotorState.KnockedDown, _motor.State);
            _motor.Tick(8d);
            Assert.AreEqual(HumanMotorState.Normal, _motor.State);
        }

        [Test]
        public void SetFaintedFalse_DoesNotTouchOtherStates()
        {
            _motor.EnterInteracting();
            _motor.SetFainted(false);
            Assert.AreEqual(HumanMotorState.Interacting, _motor.State);
        }

        [Test]
        public void MotorStateValues_AreStable()
        {
            Assert.AreEqual(0, (int)HumanMotorState.Normal);
            Assert.AreEqual(1, (int)HumanMotorState.Interacting);
            Assert.AreEqual(2, (int)HumanMotorState.KnockedDown);
            Assert.AreEqual(3, (int)HumanMotorState.Fainted);
        }
    }
}
