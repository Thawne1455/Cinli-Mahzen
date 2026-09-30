using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.Possession;
using NUnit.Framework;

namespace CinliMahzen.Tests.EditMode.B
{
    /// <summary>Tech §6.2 — every arbiter rule has a positive and a negative case, plus race and Öfke.</summary>
    public class PossessionArbiterCoreTests
    {
        // Oyun §12: PossessRange 3, PossessTime 1.2, ReenterCooldown 10; tolerance Tech §6.2 (+0.5).
        private const float Range = 3f;
        private const float Tolerance = 0.5f;
        private const float PossessTime = 1.2f;
        private const float Reenter = 10f;
        private const double Eps = 1e-6;

        private static readonly PlayerId Human = new PlayerId(0);
        private static readonly PlayerId Good = new PlayerId(1);
        private static readonly PlayerId Evil3 = new PlayerId(2);
        private static readonly PlayerId Evil4 = new PlayerId(3);
        private static readonly NetId Shelf = new NetId(1000);
        private static readonly NetId Barrel = new NetId(1001);

        private FakePossessionWorld _world;
        private PossessionArbiterCore _arbiter;

        [SetUp]
        public void SetUp()
        {
            _world = new FakePossessionWorld();
            _world.SetRole(Human, Role.Human);
            _world.SetRole(Good, Role.GoodJinn);
            _world.SetRole(Evil3, Role.EvilJinn);
            _world.SetRole(Evil4, Role.EvilJinn);
            _arbiter = new PossessionArbiterCore(_world, new PossessionRules(Range, Tolerance, PossessTime, Reenter));
            _arbiter.Register(Shelf);
            _arbiter.Register(Barrel);
        }

        private PossessDenyReason Possess(PlayerId p, NetId obj, double now)
        {
            return _arbiter.TryPossess(p, obj, now, out _);
        }

        private void EnterFully(PlayerId p, NetId obj, double now)
        {
            Assert.AreEqual(PossessDenyReason.None, Possess(p, obj, now));
            _arbiter.Tick(now + PossessTime, new List<PossessionTransitionInfo>());
        }

        // ------------------------------------------------------------ happy path

        [Test]
        public void AllRulesPass_BeginsEntering()
        {
            Assert.AreEqual(PossessDenyReason.None, _arbiter.TryPossess(Evil3, Shelf, 100d, out double end));
            Assert.AreEqual(100d + PossessTime, end, 1e-4);
            Assert.IsTrue(_arbiter.IsPossessed(Shelf));
            Assert.AreEqual(Evil3, _arbiter.PossessorOf(Shelf));
            Assert.IsTrue(_arbiter.TryGetPossessed(Evil3, out NetId obj));
            Assert.AreEqual(Shelf, obj);
            _arbiter.TryGetMachine(Shelf, out PossessableStateMachine sm);
            Assert.AreEqual(PossessableState.Entering, sm.State);
        }

        [Test]
        public void Check_DoesNotChangeState()
        {
            Assert.AreEqual(PossessDenyReason.None, _arbiter.Check(Evil3, Shelf, 0d));
            Assert.IsFalse(_arbiter.IsPossessed(Shelf));
        }

        [Test]
        public void Tick_CompletesEnterAfterPossessTime()
        {
            var results = new List<PossessionTransitionInfo>();
            _arbiter.TryPossess(Evil3, Shelf, 0d, out double end);
            Assert.AreEqual(0, _arbiter.Tick(end - 0.01d, results));
            Assert.AreEqual(1, _arbiter.Tick(end, results));
            Assert.AreEqual(Shelf, results[0].Object);
            Assert.AreEqual(Evil3, results[0].Player);
            Assert.AreEqual(PossessableTransition.EnterCompleted, results[0].Kind);
            _arbiter.TryGetMachine(Shelf, out PossessableStateMachine sm);
            Assert.AreEqual(PossessableState.Lurking, sm.State);
        }

        // ------------------------------------------------------------ rule 1: role + stun

        [Test]
        public void Rule1_Role_HumanAndGoodJinnDenied()
        {
            Assert.AreEqual(PossessDenyReason.NotEvilJinn, Possess(Human, Shelf, 0d));
            Assert.AreEqual(PossessDenyReason.NotEvilJinn, Possess(Good, Shelf, 0d));
            Assert.AreEqual(PossessDenyReason.NotEvilJinn, Possess(new PlayerId(9), Shelf, 0d), "unknown player");
            Assert.IsFalse(_arbiter.IsPossessed(Shelf));
        }

        [Test]
        public void Rule1_Role_EvilJinnAllowed()
        {
            Assert.AreEqual(PossessDenyReason.None, Possess(Evil4, Shelf, 0d));
        }

        [Test]
        public void Rule1_Stunned_Denied()
        {
            _world.LockUntil(Evil3, 4d);
            Assert.AreEqual(PossessDenyReason.Stunned, Possess(Evil3, Shelf, 3.9d));
        }

        [Test]
        public void Rule1_StunExpired_Allowed()
        {
            _world.LockUntil(Evil3, 4d);
            Assert.AreEqual(PossessDenyReason.None, Possess(Evil3, Shelf, 4d));
        }

        // ------------------------------------------------------------ rule 2: phase + wake delay

        [Test]
        public void Rule2_NotPlaying_Denied()
        {
            _world.IsPlaying = false;
            Assert.AreEqual(PossessDenyReason.NotPlaying, Possess(Evil3, Shelf, 0d));
        }

        [Test]
        public void Rule2_JinnsAsleep_Denied()
        {
            _world.JinnsAwake = false;
            Assert.AreEqual(PossessDenyReason.JinnsAsleep, Possess(Evil3, Shelf, 0d));
        }

        [Test]
        public void Rule2_PlayingAndAwake_Allowed()
        {
            _world.IsPlaying = true;
            _world.JinnsAwake = true;
            Assert.AreEqual(PossessDenyReason.None, Possess(Evil3, Shelf, 0d));
        }

        // ------------------------------------------------------------ rule 3: object state

        [Test]
        public void Rule3_UnknownObject_Denied()
        {
            Assert.AreEqual(PossessDenyReason.InvalidTarget, Possess(Evil3, new NetId(4242), 0d));
        }

        [Test]
        public void Rule3_Occupied_Denied()
        {
            EnterFully(Evil3, Shelf, 0d);
            Assert.AreEqual(PossessDenyReason.Occupied, Possess(Evil4, Shelf, 5d));
        }

        [Test]
        public void Rule3_Spent_Denied()
        {
            _arbiter.TryGetMachine(Shelf, out PossessableStateMachine sm);
            sm.MarkSpent();
            Assert.AreEqual(PossessDenyReason.Spent, Possess(Evil3, Shelf, 0d));
        }

        [Test]
        public void Rule3_Blessed_Denied_ThenAllowedAfterExpiry()
        {
            _arbiter.Bless(Shelf, 20d);
            Assert.AreEqual(PossessDenyReason.Blessed, Possess(Evil3, Shelf, 19.9d));
            Assert.AreEqual(PossessDenyReason.None, Possess(Evil3, Shelf, 20d));
        }

        [Test]
        public void Rule3_BlockedBySalt_Denied()
        {
            _world.SetBlocked(Shelf, true);
            Assert.AreEqual(PossessDenyReason.Blocked, Possess(Evil3, Shelf, 0d));
            Assert.AreEqual(PossessDenyReason.None, Possess(Evil3, Barrel, 0d), "salt only covers the shelf");
        }

        [Test]
        public void Rule3_FreeUnblessedUnblocked_Allowed()
        {
            _world.SetBlocked(Shelf, false);
            Assert.AreEqual(PossessDenyReason.None, Possess(Evil3, Shelf, 0d));
        }

        // ------------------------------------------------------------ rule 4: distance

        [Test]
        public void Rule4_WithinRangePlusTolerance_Allowed()
        {
            _world.SetDistance(Evil3, Shelf, Range + Tolerance);
            Assert.AreEqual(PossessDenyReason.None, Possess(Evil3, Shelf, 0d));
        }

        [Test]
        public void Rule4_BeyondTolerance_Denied()
        {
            _world.SetDistance(Evil3, Shelf, Range + Tolerance + 0.01f);
            Assert.AreEqual(PossessDenyReason.OutOfRange, Possess(Evil3, Shelf, 0d));
        }

        [Test]
        public void Rule4_UnknownDistance_Denied()
        {
            _world.SetDistance(Evil3, Shelf, -1f);
            Assert.AreEqual(PossessDenyReason.OutOfRange, Possess(Evil3, Shelf, 0d));
        }

        // ------------------------------------------------------------ rule 5: re-enter cooldown

        [Test]
        public void Rule5_ReenterWithinCooldown_Denied()
        {
            EnterFully(Evil3, Shelf, 0d);
            Assert.IsTrue(_arbiter.Release(Evil3, 5d, out NetId left));
            Assert.AreEqual(Shelf, left);
            Assert.AreEqual(PossessDenyReason.Cooldown, Possess(Evil3, Shelf, 5d + Reenter - 0.01d));
        }

        [Test]
        public void Rule5_ReenterAfterCooldown_Allowed()
        {
            EnterFully(Evil3, Shelf, 0d);
            _arbiter.Release(Evil3, 5d, out _);
            Assert.AreEqual(PossessDenyReason.None, Possess(Evil3, Shelf, 5d + Reenter));
        }

        [Test]
        public void Rule5_CooldownIsPerPlayerAndPerObject()
        {
            EnterFully(Evil3, Shelf, 0d);
            _arbiter.Release(Evil3, 5d, out _);
            Assert.AreEqual(PossessDenyReason.None, Possess(Evil3, Barrel, 6d), "other object is fine");
            Assert.AreEqual(PossessDenyReason.None, Possess(Evil4, Shelf, 6d), "other jinn is fine");
        }

        [Test]
        public void Rule5_Rage_ShortensReenterCooldown()
        {
            EnterFully(Evil3, Shelf, 0d);
            _arbiter.SetCooldownMultiplier(0.6f, 2d);
            _arbiter.Release(Evil3, 5d, out _);
            Assert.AreEqual(5d + Reenter * 0.6f, _arbiter.ReenterReadyAt(Evil3, Shelf), 1e-4);
            Assert.AreEqual(PossessDenyReason.Cooldown, Possess(Evil3, Shelf, 10.9d));
            Assert.AreEqual(PossessDenyReason.None, Possess(Evil3, Shelf, 11.001d));
        }

        [Test]
        public void Rule5_Rage_RescalesRunningReenterCooldown()
        {
            EnterFully(Evil3, Shelf, 0d);
            _arbiter.Release(Evil3, 5d, out _);        // ready at 15
            _arbiter.SetCooldownMultiplier(0.6f, 7d);  // 8 s left → 4.8 s → ready at 11.8
            Assert.AreEqual(11.8d, _arbiter.ReenterReadyAt(Evil3, Shelf), 1e-4);
        }

        // ------------------------------------------------------------ rule 6: already possessing

        [Test]
        public void Rule6_AlreadyInsideAnother_Denied()
        {
            EnterFully(Evil3, Shelf, 0d);
            Assert.AreEqual(PossessDenyReason.AlreadyPossessing, Possess(Evil3, Barrel, 3d));
        }

        [Test]
        public void Rule6_EnteringAnotherCounts_Denied()
        {
            Possess(Evil3, Shelf, 0d);
            Assert.AreEqual(PossessDenyReason.AlreadyPossessing, Possess(Evil3, Barrel, 0.5d));
        }

        [Test]
        public void Rule6_AfterLeaving_CanPossessAnother()
        {
            EnterFully(Evil3, Shelf, 0d);
            _arbiter.Release(Evil3, 3d, out _);
            Assert.AreEqual(PossessDenyReason.None, Possess(Evil3, Barrel, 3d));
        }

        // ------------------------------------------------------------ race + ordering

        [Test]
        public void Race_TwoJinnsSameObjectSameTime_FirstWins()
        {
            Assert.AreEqual(PossessDenyReason.None, Possess(Evil3, Shelf, 10d));
            Assert.AreEqual(PossessDenyReason.Occupied, Possess(Evil4, Shelf, 10d));
            Assert.AreEqual(Evil3, _arbiter.PossessorOf(Shelf));
            Assert.IsFalse(_arbiter.TryGetPossessed(Evil4, out _));
        }

        [Test]
        public void Race_OrderIsDeterministic_ReversedOrderReversedWinner()
        {
            Assert.AreEqual(PossessDenyReason.None, Possess(Evil4, Shelf, 10d));
            Assert.AreEqual(PossessDenyReason.Occupied, Possess(Evil3, Shelf, 10d));
            Assert.AreEqual(Evil4, _arbiter.PossessorOf(Shelf));
        }

        [Test]
        public void Race_LoserCanTakeTheObjectAfterWinnerLeaves()
        {
            Possess(Evil3, Shelf, 10d);
            Possess(Evil4, Shelf, 10d);
            _arbiter.Release(Evil3, 12d, out _);
            Assert.AreEqual(PossessDenyReason.None, Possess(Evil4, Shelf, 12d), "cooldown is only for the leaver");
        }

        [Test]
        public void RuleOrder_PlayerChecksBeforeObjectChecks()
        {
            _world.IsPlaying = false;
            _world.SetBlocked(Shelf, true);
            Assert.AreEqual(PossessDenyReason.NotEvilJinn, Possess(Good, Shelf, 0d));
            Assert.AreEqual(PossessDenyReason.NotPlaying, Possess(Evil3, Shelf, 0d));
        }

        // ------------------------------------------------------------ release / eject / spent

        [Test]
        public void Release_WhenNotPossessing_ReturnsFalse()
        {
            Assert.IsFalse(_arbiter.Release(Evil3, 0d, out _));
        }

        [Test]
        public void Eject_ByObject_ReleasesOccupantWithCooldown()
        {
            EnterFully(Evil3, Shelf, 0d);
            Assert.AreEqual(Evil3, _arbiter.Eject(Shelf, 4d));
            Assert.IsFalse(_arbiter.IsPossessed(Shelf));
            Assert.IsFalse(_arbiter.TryGetPossessed(Evil3, out _));
            Assert.AreEqual(PossessDenyReason.Cooldown, Possess(Evil3, Shelf, 5d));
            Assert.AreEqual(PlayerId.None, _arbiter.Eject(Barrel, 5d), "nothing to eject");
        }

        [Test]
        public void SingleUseResolve_EjectsAndSpends()
        {
            var results = new List<PossessionTransitionInfo>();
            EnterFully(Evil3, Shelf, 0d);
            _arbiter.TryGetMachine(Shelf, out PossessableStateMachine sm);
            sm.BeginCharge(0, 3d);
            _arbiter.Tick(3d, results);
            Assert.AreEqual(PossessableTransition.ChargeReady, results[0].Kind);
            sm.Resolve(true, 4d);
            results.Clear();
            _arbiter.Tick(4d, results);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(PossessableTransition.SpentEjected, results[0].Kind);
            Assert.AreEqual(Evil3, results[0].Player);
            Assert.IsFalse(_arbiter.TryGetPossessed(Evil3, out _));
            Assert.AreEqual(PossessDenyReason.Spent, Possess(Evil4, Shelf, 20d));
            Assert.AreEqual(PossessDenyReason.None, Possess(Evil3, Barrel, 4d), "ejected jinn is free again");
        }

        [Test]
        public void Unregister_OccupiedObject_ReleasesPlayer()
        {
            EnterFully(Evil3, Shelf, 0d);
            Assert.AreEqual(Evil3, _arbiter.Unregister(Shelf));
            Assert.IsFalse(_arbiter.TryGetPossessed(Evil3, out _));
            Assert.AreEqual(1, _arbiter.ObjectCount);
            Assert.AreEqual(PossessDenyReason.None, Possess(Evil3, Barrel, 1d));
        }

        [Test]
        public void Register_IsIdempotent()
        {
            PossessableStateMachine a = _arbiter.Register(Shelf);
            PossessableStateMachine b = _arbiter.Register(Shelf);
            Assert.AreSame(a, b);
            Assert.AreEqual(2, _arbiter.ObjectCount);
        }

        [Test]
        public void Clear_ForgetsObjectsPossessionsAndCooldowns()
        {
            EnterFully(Evil3, Shelf, 0d);
            _arbiter.SetCooldownMultiplier(0.6f, 0d);
            _arbiter.Clear();
            Assert.AreEqual(0, _arbiter.ObjectCount);
            Assert.IsFalse(_arbiter.TryGetPossessed(Evil3, out _));
            Assert.AreEqual(1f, _arbiter.CooldownMultiplier);
        }
    }
}
