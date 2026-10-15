using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.Possession;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CinliMahzen.Tests.EditMode.B
{
    /// <summary>Tech §6.2 (v2.1) — every arbiter rule has a positive and a negative case, plus race and Öfke.</summary>
    public class PossessionArbiterCoreTests
    {
        // Oyun §14: PossessRange 3, PossessTime 1.2, ReenterCooldown 10; tolerance NetRangeTolerance 0.5.
        private const float Range = 3f;
        private const float Tolerance = 0.5f;
        private const float PossessTime = 1.2f;
        private const float Reenter = 10f;

        private static readonly PlayerId Human = new PlayerId(0);
        private static readonly PlayerId Good = new PlayerId(1);
        private static readonly PlayerId Evil3 = new PlayerId(2);
        private static readonly PlayerId Evil4 = new PlayerId(3);
        private static readonly NetId Shelf = new NetId(1000);
        private static readonly NetId Barrel = new NetId(1001);
        private static readonly NetId Lamp = new NetId(1002);
        private static readonly NetId Table = new NetId(1003);
        private static readonly NetId Cologne = new NetId(1004);

        // Mirror PD_Lamp / PD_Table + PD_Cologne; Shelf and Barrel use the default traits (evil only, not a lamp).
        private static readonly PossessableTraits LampTraits = new PossessableTraits(true, false);
        private static readonly PossessableTraits GoodAllowedTraits = new PossessableTraits(false, true);

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

        /// <summary>Adds Lamp (lamp), Table and Cologne (good jinn allowed) — kept out of SetUp so ObjectCount stays 2.</summary>
        private void RegisterSpecials()
        {
            _arbiter.Register(Lamp, LampTraits);
            _arbiter.Register(Table, GoodAllowedTraits);
            _arbiter.Register(Cologne, GoodAllowedTraits);
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
        public void Rule1_Role_HumanDenied()
        {
            RegisterSpecials();
            Assert.AreEqual(PossessDenyReason.RoleNotAllowed, Possess(Human, Shelf, 0d));
            Assert.AreEqual(PossessDenyReason.RoleNotAllowed, Possess(Human, Table, 0d), "even a good-jinn-friendly object");
            Assert.AreEqual(PossessDenyReason.RoleNotAllowed, Possess(new PlayerId(9), Shelf, 0d), "unknown player");
            Assert.IsFalse(_arbiter.IsPossessed(Shelf));
        }

        [Test]
        public void Rule1_Role_EvilJinnAllowed()
        {
            Assert.AreEqual(PossessDenyReason.None, Possess(Evil4, Shelf, 0d));
        }

        [Test]
        public void Rule1_GoodJinn_AllowedOnlyOnFlaggedObjects()
        {
            RegisterSpecials();
            Assert.AreEqual(PossessDenyReason.GoodJinnNotAllowed, Possess(Good, Shelf, 0d));
            Assert.AreEqual(PossessDenyReason.GoodJinnNotAllowed, Possess(Good, Lamp, 0d));
            Assert.IsFalse(_arbiter.IsPossessed(Shelf));
            Assert.AreEqual(PossessDenyReason.None, Possess(Good, Table, 0d));
            Assert.AreEqual(Good, _arbiter.PossessorOf(Table));
        }

        [Test]
        public void Rule1_GoodJinn_UnknownObject_IsInvalidTarget()
        {
            Assert.AreEqual(PossessDenyReason.InvalidTarget, Possess(Good, new NetId(4242), 0d));
        }

        [Test]
        public void Rule1_GoodJinn_Stunned_ThenAllowed()
        {
            RegisterSpecials();
            _world.LockUntil(Good, 4d);
            Assert.AreEqual(PossessDenyReason.Stunned, Possess(Good, Table, 3.9d));
            Assert.AreEqual(PossessDenyReason.None, Possess(Good, Table, 4d));
        }

        [Test]
        public void Rule1_GoodJinn_FollowsTheOtherRules()
        {
            RegisterSpecials();
            _world.JinnsAwake = false;
            Assert.AreEqual(PossessDenyReason.JinnsAsleep, Possess(Good, Table, 0d));
            _world.JinnsAwake = true;
            _world.SetDistance(Good, Table, Range + Tolerance + 0.01f);
            Assert.AreEqual(PossessDenyReason.OutOfRange, Possess(Good, Table, 0d));
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
        public void Rule3_Free_Allowed()
        {
            Assert.AreEqual(PossessDenyReason.None, Possess(Evil3, Shelf, 0d));
        }

        // ------------------------------------------------------------ rule 4: light

        [Test]
        public void Rule4_DarkRoom_EvilJinnAllowed()
        {
            _world.SetLight(Shelf, RoomLightState.Off);
            Assert.AreEqual(PossessDenyReason.None, Possess(Evil3, Shelf, 0d));
        }

        [Test]
        public void Rule4_LitRoom_EvilJinnDenied()
        {
            _world.SetLight(Shelf, RoomLightState.On);
            Assert.AreEqual(PossessDenyReason.Lit, Possess(Evil3, Shelf, 0d));
            Assert.IsFalse(_arbiter.IsPossessed(Shelf));
        }

        [Test]
        public void Rule4_LitRoom_LampIsTheOnlyException()
        {
            RegisterSpecials();
            _world.SetLight(Lamp, RoomLightState.On);
            _world.SetLight(Table, RoomLightState.On);
            Assert.AreEqual(PossessDenyReason.None, Possess(Evil3, Lamp, 0d));
            Assert.AreEqual(PossessDenyReason.Lit, Possess(Evil4, Table, 0d), "allowGoodJinn is not a light exception");
        }

        [Test]
        public void Rule4_LitRoom_LampStillNeedsRange()
        {
            RegisterSpecials();
            _world.SetLight(Lamp, RoomLightState.On);
            _world.SetDistance(Evil3, Lamp, Range + Tolerance + 0.01f);
            Assert.AreEqual(PossessDenyReason.OutOfRange, Possess(Evil3, Lamp, 0d));
        }

        [Test]
        public void Rule4_LightIsPerObjectRoom()
        {
            _world.SetLight(Shelf, RoomLightState.On);
            _world.SetLight(Barrel, RoomLightState.Off);
            Assert.AreEqual(PossessDenyReason.Lit, Possess(Evil3, Shelf, 0d));
            Assert.AreEqual(PossessDenyReason.None, Possess(Evil3, Barrel, 0d));
        }

        [Test]
        public void Rule4_LightSwitchedOnThenOff_FollowsTheWorld()
        {
            Assert.AreEqual(PossessDenyReason.None, _arbiter.Check(Evil3, Shelf, 0d));
            _world.SetLight(Shelf, RoomLightState.On);
            Assert.AreEqual(PossessDenyReason.Lit, _arbiter.Check(Evil3, Shelf, 1d));
            _world.SetLight(Shelf, RoomLightState.Off);
            Assert.AreEqual(PossessDenyReason.None, _arbiter.Check(Evil3, Shelf, 2d));
        }

        [Test]
        public void Rule4_GoodJinn_IgnoresLight()
        {
            RegisterSpecials();
            _world.SetLight(Table, RoomLightState.On);
            Assert.AreEqual(PossessDenyReason.None, Possess(Good, Table, 0d));
        }

        [Test]
        public void Rule4_CologneLikeObject_EvilInDarkAndGoodAnywhere()
        {
            RegisterSpecials();
            Assert.AreEqual(PossessDenyReason.None, Possess(Evil3, Cologne, 0d), "evil jinn, dark room");
            _arbiter.Release(Evil3, 1d, out _);
            _world.SetLight(Cologne, RoomLightState.On);
            Assert.AreEqual(PossessDenyReason.Lit, Possess(Evil4, Cologne, 1d), "evil jinn, lit room");
            Assert.AreEqual(PossessDenyReason.None, Possess(Good, Cologne, 1d), "good jinn, lit room");
        }

        [Test]
        public void Rule4_LightIsCheckedBeforeDistance()
        {
            _world.SetLight(Shelf, RoomLightState.On);
            _world.SetDistance(Evil3, Shelf, 99f);
            Assert.AreEqual(PossessDenyReason.Lit, Possess(Evil3, Shelf, 0d));
        }

        [Test]
        public void Rule4_OccupiedIsReportedBeforeLit()
        {
            EnterFully(Evil4, Shelf, 0d);
            _world.SetLight(Shelf, RoomLightState.On);
            Assert.AreEqual(PossessDenyReason.Occupied, Possess(Evil3, Shelf, 1d));
        }

        // ------------------------------------------------------------ rule 5: distance

        [Test]
        public void Rule5_WithinRangePlusTolerance_Allowed()
        {
            _world.SetDistance(Evil3, Shelf, Range + Tolerance);
            Assert.AreEqual(PossessDenyReason.None, Possess(Evil3, Shelf, 0d));
        }

        [Test]
        public void Rule5_BeyondTolerance_Denied()
        {
            _world.SetDistance(Evil3, Shelf, Range + Tolerance + 0.01f);
            Assert.AreEqual(PossessDenyReason.OutOfRange, Possess(Evil3, Shelf, 0d));
        }

        [Test]
        public void Rule5_UnknownDistance_Denied()
        {
            _world.SetDistance(Evil3, Shelf, -1f);
            Assert.AreEqual(PossessDenyReason.OutOfRange, Possess(Evil3, Shelf, 0d));
        }

        // ------------------------------------------------------------ rule 6: re-enter cooldown + already possessing

        [Test]
        public void Rule6_ReenterWithinCooldown_Denied()
        {
            EnterFully(Evil3, Shelf, 0d);
            Assert.IsTrue(_arbiter.Release(Evil3, 5d, out NetId left));
            Assert.AreEqual(Shelf, left);
            Assert.AreEqual(PossessDenyReason.Cooldown, Possess(Evil3, Shelf, 5d + Reenter - 0.01d));
        }

        [Test]
        public void Rule6_ReenterAfterCooldown_Allowed()
        {
            EnterFully(Evil3, Shelf, 0d);
            _arbiter.Release(Evil3, 5d, out _);
            Assert.AreEqual(PossessDenyReason.None, Possess(Evil3, Shelf, 5d + Reenter));
        }

        [Test]
        public void Rule6_CooldownIsPerPlayerAndPerObject()
        {
            EnterFully(Evil3, Shelf, 0d);
            _arbiter.Release(Evil3, 5d, out _);
            Assert.AreEqual(PossessDenyReason.None, Possess(Evil3, Barrel, 6d), "other object is fine");
            Assert.AreEqual(PossessDenyReason.None, Possess(Evil4, Shelf, 6d), "other jinn is fine");
        }

        [Test]
        public void Rule6_GoodJinn_HasTheSameReenterCooldown()
        {
            RegisterSpecials();
            EnterFully(Good, Table, 0d);
            _arbiter.Release(Good, 5d, out _);
            Assert.AreEqual(PossessDenyReason.Cooldown, Possess(Good, Table, 5d + Reenter - 0.01d));
            Assert.AreEqual(PossessDenyReason.None, Possess(Good, Table, 5d + Reenter));
        }

        [Test]
        public void Rule6_Rage_ShortensReenterCooldown()
        {
            EnterFully(Evil3, Shelf, 0d);
            _arbiter.SetCooldownMultiplier(0.6f, 2d);
            _arbiter.Release(Evil3, 5d, out _);
            Assert.AreEqual(5d + Reenter * 0.6f, _arbiter.ReenterReadyAt(Evil3, Shelf), 1e-4);
            Assert.AreEqual(PossessDenyReason.Cooldown, Possess(Evil3, Shelf, 10.9d));
            Assert.AreEqual(PossessDenyReason.None, Possess(Evil3, Shelf, 11.001d));
        }

        [Test]
        public void Rule6_Rage_RescalesRunningReenterCooldown()
        {
            EnterFully(Evil3, Shelf, 0d);
            _arbiter.Release(Evil3, 5d, out _);        // ready at 15
            _arbiter.SetCooldownMultiplier(0.6f, 7d);  // 8 s left → 4.8 s → ready at 11.8
            Assert.AreEqual(11.8d, _arbiter.ReenterReadyAt(Evil3, Shelf), 1e-4);
        }

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
        public void Race_GoodAndEvilJinnSameObject_FirstWins()
        {
            RegisterSpecials();
            Assert.AreEqual(PossessDenyReason.None, Possess(Good, Table, 10d));
            Assert.AreEqual(PossessDenyReason.Occupied, Possess(Evil3, Table, 10d));
            Assert.AreEqual(Good, _arbiter.PossessorOf(Table));
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
            _world.SetLight(Shelf, RoomLightState.On);
            Assert.AreEqual(PossessDenyReason.RoleNotAllowed, Possess(Human, Shelf, 0d));
            Assert.AreEqual(PossessDenyReason.GoodJinnNotAllowed, Possess(Good, Shelf, 0d));
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
        public void Register_StoresTraits_AndReRegisterReplacesThem()
        {
            Assert.IsTrue(_arbiter.TryGetTraits(Shelf, out PossessableTraits plain));
            Assert.IsFalse(plain.IsLamp);
            Assert.IsFalse(plain.AllowGoodJinn);
            _arbiter.Register(Shelf, new PossessableTraits(true, true));
            Assert.IsTrue(_arbiter.TryGetTraits(Shelf, out PossessableTraits updated));
            Assert.IsTrue(updated.IsLamp);
            Assert.IsTrue(updated.AllowGoodJinn);
            Assert.IsFalse(_arbiter.TryGetTraits(new NetId(4242), out _));
        }

        [Test]
        public void Unregister_KeepsTraitsOfTheOtherObjectsAligned()
        {
            RegisterSpecials();                       // Shelf, Barrel, Lamp, Table, Cologne
            _arbiter.Unregister(Shelf);               // last one (Cologne) moves into slot 0
            _arbiter.TryGetTraits(Cologne, out PossessableTraits cologne);
            _arbiter.TryGetTraits(Lamp, out PossessableTraits lamp);
            _arbiter.TryGetTraits(Barrel, out PossessableTraits barrel);
            Assert.IsTrue(cologne.AllowGoodJinn);
            Assert.IsTrue(lamp.IsLamp);
            Assert.IsFalse(barrel.AllowGoodJinn);
            Assert.AreEqual(PossessDenyReason.None, Possess(Good, Cologne, 0d));
            Assert.AreEqual(PossessDenyReason.GoodJinnNotAllowed, Possess(Good, Barrel, 0d));
        }

        [Test]
        public void Traits_From_CopiesDefinitionFlags()
        {
            var def = ScriptableObject.CreateInstance<PossessableDefinition>();
            try
            {
                var so = new SerializedObject(def);
                so.FindProperty("isLamp").boolValue = true;
                so.FindProperty("allowGoodJinn").boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
                PossessableTraits t = PossessableTraits.From(def);
                Assert.IsTrue(t.IsLamp);
                Assert.IsTrue(t.AllowGoodJinn);
                Assert.IsFalse(PossessableTraits.From(null).IsLamp);
            }
            finally
            {
                Object.DestroyImmediate(def);
            }
        }

        [Test]
        public void Clear_ForgetsObjectsPossessionsAndCooldowns()
        {
            RegisterSpecials();
            EnterFully(Evil3, Shelf, 0d);
            _arbiter.SetCooldownMultiplier(0.6f, 0d);
            _arbiter.Clear();
            Assert.AreEqual(0, _arbiter.ObjectCount);
            Assert.IsFalse(_arbiter.TryGetPossessed(Evil3, out _));
            Assert.IsFalse(_arbiter.TryGetTraits(Table, out _));
            Assert.AreEqual(1f, _arbiter.CooldownMultiplier);
        }
    }
}
