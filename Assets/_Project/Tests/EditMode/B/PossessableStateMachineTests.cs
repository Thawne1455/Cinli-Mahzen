using CinliMahzen.Core;
using CinliMahzen.Possession;
using NUnit.Framework;

namespace CinliMahzen.Tests.EditMode.B
{
    public class PossessableStateMachineTests
    {
        private static readonly PlayerId P3 = new PlayerId(2);

        private static PossessableStateMachine Possessed(double enterEnd = 1.2d)
        {
            var sm = new PossessableStateMachine();
            sm.BeginEnter(P3, enterEnd);
            sm.Tick(enterEnd);
            return sm;
        }

        [Test]
        public void StartsFree()
        {
            var sm = new PossessableStateMachine();
            Assert.AreEqual(PossessableState.Free, sm.State);
            Assert.AreEqual(PlayerId.None, sm.Occupant);
            Assert.IsFalse(sm.IsOccupied);
            Assert.IsFalse(sm.IsBlessed(0d));
        }

        [Test]
        public void Enter_CompletesAfterPossessTime()
        {
            var sm = new PossessableStateMachine();
            Assert.IsTrue(sm.BeginEnter(P3, 1.2d));
            Assert.AreEqual(PossessableState.Entering, sm.State);
            Assert.IsTrue(sm.IsOccupied);
            Assert.IsFalse(sm.IsPossessed);
            Assert.AreEqual(PossessableTransition.None, sm.Tick(1.19d));
            Assert.AreEqual(PossessableTransition.EnterCompleted, sm.Tick(1.2d));
            Assert.AreEqual(PossessableState.Lurking, sm.State);
            Assert.AreEqual(P3, sm.Occupant);
            Assert.IsTrue(sm.IsPossessed);
        }

        [Test]
        public void BeginEnter_FailsWhenNotFree()
        {
            var sm = Possessed();
            Assert.IsFalse(sm.BeginEnter(new PlayerId(3), 5d));
            Assert.AreEqual(P3, sm.Occupant);
        }

        [Test]
        public void Exit_DuringEntering_CancelsToFree()
        {
            var sm = new PossessableStateMachine();
            sm.BeginEnter(P3, 1.2d);
            Assert.AreEqual(P3, sm.Exit());
            Assert.AreEqual(PossessableState.Free, sm.State);
            Assert.AreEqual(PossessableTransition.None, sm.Tick(2d));
        }

        [Test]
        public void Exit_WhenFree_ReturnsNone()
        {
            var sm = new PossessableStateMachine();
            Assert.AreEqual(PlayerId.None, sm.Exit());
        }

        [Test]
        public void Charge_ReportsReadyOnce_ThenResolveAndRecover()
        {
            var sm = Possessed();
            Assert.IsTrue(sm.BeginCharge(0, 3d));
            Assert.AreEqual(PossessableState.Charging, sm.State);
            Assert.AreEqual(0, sm.ActionIndex);
            Assert.AreEqual(PossessableTransition.None, sm.Tick(2.9d));
            Assert.AreEqual(PossessableTransition.ChargeReady, sm.Tick(3d));
            Assert.AreEqual(PossessableTransition.None, sm.Tick(3.1d), "ChargeReady must fire once");

            Assert.IsTrue(sm.Resolve(false, 4d));
            Assert.AreEqual(PossessableState.Recovering, sm.State);
            Assert.AreEqual(PossessableTransition.None, sm.Tick(3.5d));
            Assert.AreEqual(PossessableTransition.RecoverFinished, sm.Tick(4d));
            Assert.AreEqual(PossessableState.Lurking, sm.State);
            Assert.AreEqual(-1, sm.ActionIndex);
        }

        [Test]
        public void Charge_OnlyFromLurking()
        {
            var sm = new PossessableStateMachine();
            Assert.IsFalse(sm.BeginCharge(0, 1d), "free object cannot charge");
            sm.BeginEnter(P3, 1.2d);
            Assert.IsFalse(sm.BeginCharge(0, 1d), "entering object cannot charge");
            sm.Tick(1.2d);
            Assert.IsFalse(sm.BeginCharge(-1, 2d), "invalid action index");
            Assert.IsTrue(sm.BeginCharge(1, 2d));
            Assert.IsFalse(sm.BeginCharge(0, 2d), "already charging");
        }

        [Test]
        public void CancelCharge_BackToLurking()
        {
            var sm = Possessed();
            sm.BeginCharge(0, 3d);
            Assert.IsTrue(sm.CancelCharge());
            Assert.AreEqual(PossessableState.Lurking, sm.State);
            Assert.AreEqual(PossessableTransition.None, sm.Tick(5d));
            Assert.IsFalse(sm.Resolve(false, 6d), "cannot resolve a cancelled charge");
        }

        [Test]
        public void SingleUse_EjectsAndBecomesSpentAfterRecover()
        {
            var sm = Possessed();
            sm.BeginCharge(0, 2.2d);
            sm.Tick(2.2d);
            sm.Resolve(true, 3d);
            Assert.IsTrue(sm.WillBeSpent);
            Assert.AreEqual(P3, sm.Occupant, "jinn stays in for the result animation");
            Assert.AreEqual(PossessableTransition.SpentEjected, sm.Tick(3d));
            Assert.AreEqual(PossessableState.Spent, sm.State);
            Assert.AreEqual(PlayerId.None, sm.Occupant);
            Assert.IsFalse(sm.IsOccupied);
            Assert.IsFalse(sm.BeginEnter(P3, 10d), "spent object cannot be entered");
        }

        [Test]
        public void SingleUse_ExitDuringRecover_StillSpent()
        {
            var sm = Possessed();
            sm.BeginCharge(0, 2.2d);
            sm.Tick(2.2d);
            sm.Resolve(true, 5d);
            Assert.AreEqual(P3, sm.Exit());
            Assert.AreEqual(PossessableState.Spent, sm.State);
        }

        [Test]
        public void Exit_DuringCharging_FreesObject()
        {
            var sm = Possessed();
            sm.BeginCharge(0, 3d);
            Assert.AreEqual(P3, sm.Exit());
            Assert.AreEqual(PossessableState.Free, sm.State);
            Assert.AreEqual(-1, sm.ActionIndex);
            Assert.AreEqual(PossessableTransition.None, sm.Tick(3d));
        }

        [Test]
        public void Bless_ExpiresAndNeverShortens()
        {
            var sm = new PossessableStateMachine();
            sm.Bless(20d);
            sm.Bless(15d);
            Assert.IsTrue(sm.IsBlessed(19.9d));
            Assert.IsFalse(sm.IsBlessed(20d));
            sm.ClearBless();
            Assert.IsFalse(sm.IsBlessed(0d));
        }

        [Test]
        public void Reset_ClearsEverything()
        {
            var sm = Possessed();
            sm.Bless(100d);
            sm.BeginCharge(0, 3d);
            sm.Reset();
            Assert.AreEqual(PossessableState.Free, sm.State);
            Assert.AreEqual(PlayerId.None, sm.Occupant);
            Assert.IsFalse(sm.IsBlessed(0d));
        }

        [Test]
        public void MarkSpent_FromAnyState()
        {
            var sm = Possessed();
            sm.MarkSpent();
            Assert.AreEqual(PossessableState.Spent, sm.State);
            Assert.AreEqual(PlayerId.None, sm.Occupant);
        }
    }
}
