using System;
using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.Match;
using NUnit.Framework;
using UnityEngine;

namespace CinliMahzen.Tests.EditMode.Match
{
    /// <summary>Teknik §9 / TODO 1.10 — flow timings, dynamic Sunrise counter, JinnsAwake, the three round-end conditions.</summary>
    public class MatchStateMachineTests
    {
        // Oyun §14 defaults (GameBalanceConfigTests pins the asset to the same numbers).
        private const float RoleReveal = 3f;
        private const float Intro = 5f;
        private const float BaseRound = 360f;
        private const float Bonus = 90f;
        private const float WakeDelay = 10f;
        private const float RoundEnd = 8f;
        private const int Rounds = 4;
        private const int FaintsToLose = 3;

        /// <summary>RoleReveal + Intro + Playing + RoundEnd when nothing shortens or extends the round.</summary>
        private const double TimeoutRoundLength = RoleReveal + Intro + BaseRound + RoundEnd;

        private MatchStateMachine _m;
        private List<int> _seedCalls;

        private static MatchSettings DefaultSettings()
        {
            return new MatchSettings(RoleReveal, Intro, BaseRound, Bonus, WakeDelay, RoundEnd, Rounds, FaintsToLose);
        }

        private static int SeedFor(int round)
        {
            return 1000 + round;
        }

        [SetUp]
        public void SetUp()
        {
            _seedCalls = new List<int>();
            _m = new MatchStateMachine(DefaultSettings(), round =>
            {
                _seedCalls.Add(round);
                return SeedFor(round);
            });
        }

        // ---------------------------------------------------------------- helpers

        /// <summary>Fresh match → Playing. Returns the Playing start time (setupDoneAt + RoleReveal + Intro).</summary>
        private double ReachPlaying(double setupDoneAt = 0d)
        {
            Assert.IsTrue(_m.StartMatch(out _));
            Assert.IsTrue(_m.CompleteRoundSetup(setupDoneAt, out _));
            Assert.IsTrue(_m.Tick(setupDoneAt + RoleReveal, out _));
            double playingStart = setupDoneAt + RoleReveal + Intro;
            Assert.IsTrue(_m.Tick(playingStart, out _));
            Assert.AreEqual(MatchState.Playing, _m.State);
            return playingStart;
        }

        /// <summary>
        /// Plays the machine like a controller would (tick at each state's end, complete RoundSetup immediately)
        /// until <paramref name="target"/> is entered. Returns the time at which it was entered.
        /// </summary>
        private double DriveTo(MatchState target, double now)
        {
            int guard = 0;
            while (_m.State != target)
            {
                Assert.Less(guard++, 50, "did not reach " + target);
                if (_m.State == MatchState.RoundSetup)
                {
                    Assert.IsTrue(_m.CompleteRoundSetup(now, out _));
                }
                else if (_m.StateEndTime == double.MaxValue)
                {
                    Assert.Fail("stuck in " + _m.State + " while driving to " + target);
                }
                else
                {
                    now = Math.Max(now, _m.StateEndTime);
                    Assert.IsTrue(_m.Tick(now, out _));
                }
            }

            return now;
        }

        private int Drain(double now, List<MatchTransition> log = null)
        {
            int count = 0;
            while (_m.Tick(now, out MatchTransition t))
            {
                log?.Add(t);
                count++;
                Assert.Less(count, 100, "runaway tick loop");
            }

            return count;
        }

        private static void AssertTransition(MatchTransition t, MatchState from, MatchState to, int round, double endTime)
        {
            Assert.AreEqual(from, t.From, "from of " + t);
            Assert.AreEqual(to, t.To, "to of " + t);
            Assert.AreEqual(round, t.RoundIndex, "round of " + t);
            Assert.AreEqual(endTime, t.StateEndTime, "end time of " + t);
        }

        private void AssertRoundEventsRejected()
        {
            MatchState state = _m.State;
            double end = _m.StateEndTime;
            int faints = _m.Faints;

            Assert.IsFalse(_m.OnHumanFainted(99, 0d, out _), "faint in " + state);
            Assert.IsFalse(_m.OnTreasureDug(0d, out _), "treasure in " + state);
            Assert.IsFalse(_m.TryEndRound(RoundResult.JinnsWin, RoundEndReason.Timeout, 0d, out _), "end in " + state);
            Assert.IsFalse(_m.TryAddStageTimeBonus(0d, out double newEnd), "bonus in " + state);

            Assert.AreEqual(end, newEnd, "unchanged end time is reported");
            Assert.AreEqual(state, _m.State);
            Assert.AreEqual(end, _m.StateEndTime);
            Assert.AreEqual(faints, _m.Faints);
        }

        // ---------------------------------------------------------------- construction / Lobby / RoundSetup

        [Test]
        public void Constructor_NullSeedProvider_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new MatchStateMachine(DefaultSettings(), null));
        }

        [Test]
        public void Initial_IsLobby_WithoutTimerOrRoundData()
        {
            Assert.AreEqual(MatchState.Lobby, _m.State);
            Assert.AreEqual(0, _m.RoundIndex);
            Assert.AreEqual(double.MaxValue, _m.StateEndTime);
            Assert.AreEqual(0, _m.Faints);
            Assert.IsFalse(_m.JinnsAwake);
            Assert.AreEqual(RoundResult.None, _m.LastResult);
            Assert.AreEqual(RoundEndReason.None, _m.LastReason);
            Assert.AreEqual(double.MaxValue, _m.PlayingStartTime);
            Assert.AreEqual(0, _seedCalls.Count, "no seed requested before the match starts");
        }

        [Test]
        public void ImplementsIMatchInfo_ReflectsTheMachine()
        {
            IMatchInfo info = _m;
            double ps = ReachPlaying();
            Drain(ps + WakeDelay);
            _m.OnHumanFainted(1, ps + WakeDelay, out _);

            Assert.AreEqual(MatchState.Playing, info.State);
            Assert.AreEqual(0, info.RoundIndex);
            Assert.AreEqual(ps + BaseRound, info.StateEndTime);
            Assert.IsTrue(info.JinnsAwake);
            Assert.AreEqual(1, info.Faints);
        }

        [Test]
        public void Tick_InLobby_NeverAdvances()
        {
            Assert.IsFalse(_m.Tick(1e9, out MatchTransition t));
            Assert.AreEqual(default(MatchTransition), t);
            Assert.AreEqual(MatchState.Lobby, _m.State);
        }

        [Test]
        public void StartMatch_EntersRoundSetup_AndAsksForTheRoundSeed()
        {
            Assert.IsTrue(_m.StartMatch(out MatchTransition t));

            AssertTransition(t, MatchState.Lobby, MatchState.RoundSetup, 0, double.MaxValue);
            Assert.AreEqual(MatchState.RoundSetup, _m.State);
            Assert.AreEqual(0, _m.RoundIndex);
            Assert.AreEqual(SeedFor(0), _m.Seed);
            CollectionAssert.AreEqual(new[] { 0 }, _seedCalls);
        }

        [Test]
        public void StartMatch_OutsideLobby_IsRejected()
        {
            Assert.IsTrue(_m.StartMatch(out _));
            Assert.IsFalse(_m.StartMatch(out _));
            Assert.AreEqual(MatchState.RoundSetup, _m.State);

            DriveTo(MatchState.Playing, 0d);
            Assert.IsFalse(_m.StartMatch(out _));
            Assert.AreEqual(MatchState.Playing, _m.State);
            CollectionAssert.AreEqual(new[] { 0 }, _seedCalls, "rejected starts do not consume a seed");
        }

        [Test]
        public void RoundSetup_HasNoTimer_TickNeverLeavesIt()
        {
            _m.StartMatch(out _);

            Assert.IsFalse(_m.Tick(1e9, out _));
            Assert.AreEqual(MatchState.RoundSetup, _m.State);
            Assert.AreEqual(double.MaxValue, _m.StateEndTime);
        }

        [Test]
        public void CompleteRoundSetup_EntersRoleReveal_MeasuredFromTheGivenTime()
        {
            _m.StartMatch(out _);

            Assert.IsTrue(_m.CompleteRoundSetup(10d, out MatchTransition t));

            AssertTransition(t, MatchState.RoundSetup, MatchState.RoleReveal, 0, 13d);
            Assert.AreEqual(13d, _m.StateEndTime);
        }

        [Test]
        public void CompleteRoundSetup_OutsideRoundSetup_IsRejected()
        {
            Assert.IsFalse(_m.CompleteRoundSetup(0d, out _), "Lobby");

            _m.StartMatch(out _);
            Assert.IsTrue(_m.CompleteRoundSetup(0d, out _));
            Assert.IsFalse(_m.CompleteRoundSetup(0d, out _), "RoleReveal");
            Assert.AreEqual(MatchState.RoleReveal, _m.State);
            Assert.AreEqual(3d, _m.StateEndTime, "end time untouched");
        }

        // ---------------------------------------------------------------- flow timings

        [Test]
        public void FullRound_Timeline_RoleReveal3_Intro5_Playing360_RoundEnd8_NextRound()
        {
            _m.StartMatch(out _);
            _m.CompleteRoundSetup(0d, out _);
            Assert.AreEqual(MatchState.RoleReveal, _m.State);
            Assert.AreEqual(3d, _m.StateEndTime);

            Assert.IsFalse(_m.Tick(2.999d, out _));
            Assert.IsTrue(_m.Tick(3d, out MatchTransition t));
            AssertTransition(t, MatchState.RoleReveal, MatchState.Intro, 0, 8d);

            Assert.IsFalse(_m.Tick(7.999d, out _));
            Assert.IsTrue(_m.Tick(8d, out t));
            AssertTransition(t, MatchState.Intro, MatchState.Playing, 0, 368d);
            Assert.AreEqual(8d, _m.PlayingStartTime);

            Assert.IsFalse(_m.Tick(367.999d, out _));
            Assert.IsTrue(_m.Tick(368d, out t));
            AssertTransition(t, MatchState.Playing, MatchState.RoundEnd, 0, 376d);
            Assert.AreEqual(RoundResult.JinnsWin, _m.LastResult);
            Assert.AreEqual(RoundEndReason.Timeout, _m.LastReason);

            Assert.IsFalse(_m.Tick(375.999d, out _));
            Assert.IsTrue(_m.Tick(376d, out t));
            AssertTransition(t, MatchState.RoundEnd, MatchState.RoundSetup, 1, double.MaxValue);
            Assert.AreEqual(SeedFor(1), _m.Seed);
        }

        [Test]
        public void LateTick_DoesNotShiftTheSchedule()
        {
            _m.StartMatch(out _);
            _m.CompleteRoundSetup(0d, out _);

            Assert.IsTrue(_m.Tick(5d, out MatchTransition t), "RoleReveal ended at 3, tick arrives at 5");
            Assert.AreEqual(8d, t.StateEndTime, "Intro runs 3..8, not 5..10");
            Assert.IsFalse(_m.Tick(5d, out _));

            Assert.IsTrue(_m.Tick(20d, out t));
            AssertTransition(t, MatchState.Intro, MatchState.Playing, 0, 368d);
            Assert.AreEqual(8d, _m.PlayingStartTime);
        }

        [Test]
        public void SlowRoundSetup_OnlyDelaysTheRoleRevealStart()
        {
            _m.StartMatch(out _);
            _m.CompleteRoundSetup(2.5d, out _);

            Assert.AreEqual(5.5d, _m.StateEndTime);
            Assert.IsTrue(_m.Tick(5.5d, out MatchTransition t));
            Assert.AreEqual(10.5d, t.StateEndTime);
            Assert.IsTrue(_m.Tick(10.5d, out t));
            Assert.AreEqual(10.5d + BaseRound, t.StateEndTime);
        }

        [Test]
        public void FourRounds_ThenMatchEnd()
        {
            var setupRounds = new List<int>();
            double now = 0d;
            Assert.IsTrue(_m.StartMatch(out _));

            int guard = 0;
            while (_m.State != MatchState.MatchEnd)
            {
                Assert.Less(guard++, 100, "match did not end");
                if (_m.State == MatchState.RoundSetup)
                {
                    setupRounds.Add(_m.RoundIndex);
                    Assert.IsTrue(_m.CompleteRoundSetup(now, out _));
                }
                else
                {
                    now = _m.StateEndTime;
                    Assert.IsTrue(_m.Tick(now, out _));
                }
            }

            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, setupRounds);
            Assert.AreEqual(Rounds * TimeoutRoundLength, now);
            Assert.AreEqual(Rounds - 1, _m.RoundIndex, "MatchEnd keeps the last round index");
            Assert.AreEqual(double.MaxValue, _m.StateEndTime);
            Assert.AreEqual(RoundResult.JinnsWin, _m.LastResult);
            Assert.AreEqual(RoundEndReason.Timeout, _m.LastReason);
        }

        [Test]
        public void SeedProvider_IsCalledOncePerRound_InOrder_AndFeedsSeed()
        {
            var seeds = new List<int>();
            double now = 0d;
            _m.StartMatch(out _);
            seeds.Add(_m.Seed);
            for (int round = 1; round < Rounds; round++)
            {
                now = DriveTo(MatchState.Playing, now);
                now = DriveTo(MatchState.RoundEnd, now);
                now = DriveTo(MatchState.RoundSetup, now);
                Assert.AreEqual(round, _m.RoundIndex);
                seeds.Add(_m.Seed);
            }

            CollectionAssert.AreEqual(new[] { 1000, 1001, 1002, 1003 }, seeds);
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, _seedCalls);
        }

        [Test]
        public void RoundsPerMatch_ComesFromSettings()
        {
            var twoRounds = new MatchSettings(RoleReveal, Intro, BaseRound, Bonus, WakeDelay, RoundEnd, 2, FaintsToLose);
            _m = new MatchStateMachine(twoRounds, SeedFor);
            _m.StartMatch(out _);

            DriveTo(MatchState.MatchEnd, 0d);

            Assert.AreEqual(1, _m.RoundIndex);
        }

        [Test]
        public void MatchEnd_IsTerminal_UntilReturnToLobby()
        {
            _m.StartMatch(out _);
            DriveTo(MatchState.MatchEnd, 0d);

            Assert.IsFalse(_m.Tick(1e9, out _));
            Assert.IsFalse(_m.StartMatch(out _));
            Assert.IsFalse(_m.CompleteRoundSetup(0d, out _));
            AssertRoundEventsRejected();
            Assert.AreEqual(MatchState.MatchEnd, _m.State);

            Assert.IsTrue(_m.ReturnToLobby(out MatchTransition t));
            AssertTransition(t, MatchState.MatchEnd, MatchState.Lobby, 0, double.MaxValue);
            Assert.AreEqual(RoundResult.None, _m.LastResult, "a new match starts clean");
            Assert.AreEqual(RoundEndReason.None, _m.LastReason);

            Assert.IsTrue(_m.StartMatch(out t));
            AssertTransition(t, MatchState.Lobby, MatchState.RoundSetup, 0, double.MaxValue);
            Assert.AreEqual(SeedFor(0), _m.Seed);
        }

        [Test]
        public void ReturnToLobby_FromPlaying_AbortsAndClearsRoundData()
        {
            double ps = ReachPlaying();
            Drain(ps + WakeDelay);
            _m.OnHumanFainted(2, ps + WakeDelay, out _);
            Assert.IsTrue(_m.JinnsAwake);

            Assert.IsTrue(_m.ReturnToLobby(out MatchTransition t));

            AssertTransition(t, MatchState.Playing, MatchState.Lobby, 0, double.MaxValue);
            Assert.AreEqual(0, _m.Faints);
            Assert.IsFalse(_m.JinnsAwake);
            Assert.AreEqual(double.MaxValue, _m.PlayingStartTime);
            Assert.IsFalse(_m.ReturnToLobby(out _), "already in Lobby");
        }

        // ---------------------------------------------------------------- JinnsAwake

        [Test]
        public void JinnsAwake_IsFalseInEveryStateButPlaying()
        {
            Assert.IsFalse(_m.JinnsAwake, "Lobby");
            _m.StartMatch(out _);
            Assert.IsFalse(_m.JinnsAwake, "RoundSetup");
            _m.CompleteRoundSetup(0d, out _);
            Assert.IsFalse(_m.JinnsAwake, "RoleReveal");
            _m.Tick(3d, out _);
            Assert.IsFalse(_m.JinnsAwake, "Intro");
            _m.Tick(1e6, out _);                       // → Playing, tick time far past the wake delay
            _m.Tick(1e6, out _);                       // → RoundEnd (timeout)
            Assert.AreEqual(MatchState.RoundEnd, _m.State);
            Assert.IsFalse(_m.JinnsAwake, "RoundEnd");
            Assert.IsFalse(_m.IsJinnsAwakeAt(1e9), "RoundEnd, explicit time");
            DriveTo(MatchState.MatchEnd, 1e6);
            Assert.IsFalse(_m.JinnsAwake, "MatchEnd");
        }

        [Test]
        public void JinnsAwake_TurnsTrue_WhenJinnWakeDelayHasPassedInPlaying()
        {
            double ps = ReachPlaying();
            Assert.IsFalse(_m.JinnsAwake, "right at Playing start");

            _m.Tick(ps + 9.999d, out _);
            Assert.IsFalse(_m.JinnsAwake);

            _m.Tick(ps + 10d, out _);
            Assert.IsTrue(_m.JinnsAwake);
            Assert.AreEqual(MatchState.Playing, _m.State);
        }

        [Test]
        public void IsJinnsAwakeAt_AnswersForAnExplicitTime()
        {
            double ps = ReachPlaying();

            Assert.IsFalse(_m.IsJinnsAwakeAt(ps));
            Assert.IsFalse(_m.IsJinnsAwakeAt(ps + 9.999d));
            Assert.IsTrue(_m.IsJinnsAwakeAt(ps + 10d));
            Assert.IsTrue(_m.IsJinnsAwakeAt(ps + 200d));
        }

        [Test]
        public void JinnsAwake_IsMeasuredFromTheScheduledPlayingStart_NotFromTheTickThatEnteredIt()
        {
            _m.StartMatch(out _);
            _m.CompleteRoundSetup(0d, out _);

            _m.Tick(20d, out _);                       // RoleReveal → Intro
            Assert.IsFalse(_m.JinnsAwake);
            _m.Tick(20d, out _);                       // Intro → Playing, scheduled start 8

            Assert.AreEqual(MatchState.Playing, _m.State);
            Assert.AreEqual(8d, _m.PlayingStartTime);
            Assert.IsTrue(_m.JinnsAwake, "20 >= 8 + 10");
        }

        [Test]
        public void JinnsAwake_GoesFalseWhenTheRoundEnds_AndTheNextRoundSleepsAgain()
        {
            double ps = ReachPlaying();
            Drain(ps + 100d);
            Assert.IsTrue(_m.JinnsAwake);

            Assert.IsTrue(_m.OnTreasureDug(ps + 100d, out _));
            Assert.IsFalse(_m.JinnsAwake);

            double ps2 = DriveTo(MatchState.Playing, ps + 100d);
            Assert.IsFalse(_m.JinnsAwake, "stale tick time of the previous round must not wake them");
            _m.Tick(ps2 + WakeDelay, out _);
            Assert.IsTrue(_m.JinnsAwake);
        }

        // ---------------------------------------------------------------- dynamic Sunrise counter

        [Test]
        public void StageBonus_AddsStageTimeBonus_AndReportsTheNewEndTime()
        {
            double ps = ReachPlaying();

            Assert.IsTrue(_m.TryAddStageTimeBonus(ps + 30d, out double newEnd));

            Assert.AreEqual(ps + BaseRound + Bonus, newEnd);
            Assert.AreEqual(newEnd, _m.StateEndTime);
            Assert.AreEqual(MatchState.Playing, _m.State);
        }

        [Test]
        public void StageBonus_Twice_ExtendsThePlayingPhaseTo540Seconds()
        {
            double ps = ReachPlaying();

            Assert.IsTrue(_m.TryAddStageTimeBonus(ps + 30d, out _));
            Assert.IsTrue(_m.TryAddStageTimeBonus(ps + 120d, out double newEnd));

            Assert.AreEqual(BaseRound + 2 * Bonus, newEnd - ps);
            Assert.AreEqual(540d, newEnd - ps);
            Assert.IsFalse(_m.Tick(ps + 539.999d, out _));
            Assert.IsTrue(_m.Tick(ps + 540d, out MatchTransition t));
            AssertTransition(t, MatchState.Playing, MatchState.RoundEnd, 0, ps + 540d + RoundEnd);
            Assert.AreEqual(RoundEndReason.Timeout, _m.LastReason);
        }

        [Test]
        public void StageBonus_PostponesTheOriginalDeadline()
        {
            double ps = ReachPlaying();
            _m.TryAddStageTimeBonus(ps + 30d, out _);

            Assert.IsFalse(_m.Tick(ps + BaseRound, out _), "the old deadline no longer ends the round");
            Assert.AreEqual(MatchState.Playing, _m.State);
            Assert.IsFalse(_m.Tick(ps + BaseRound + Bonus - 0.001d, out _));
            Assert.IsTrue(_m.Tick(ps + BaseRound + Bonus, out _));
            Assert.AreEqual(MatchState.RoundEnd, _m.State);
        }

        [Test]
        public void StageBonus_IsAcceptedDuringJinnWakeDelay()
        {
            double ps = ReachPlaying();

            Assert.IsTrue(_m.TryAddStageTimeBonus(ps + 1d, out _));
        }

        [Test]
        public void StageBonus_AtOrAfterTheDeadline_IsRejected_SunriseWinsTheTie()
        {
            double ps = ReachPlaying();
            double deadline = ps + BaseRound;

            Assert.IsFalse(_m.TryAddStageTimeBonus(deadline, out double reported));
            Assert.AreEqual(deadline, reported);
            Assert.AreEqual(deadline, _m.StateEndTime);

            Assert.IsTrue(_m.Tick(deadline, out MatchTransition t));
            Assert.AreEqual(MatchState.RoundEnd, t.To);
            Assert.AreEqual(RoundEndReason.Timeout, _m.LastReason);
        }

        [Test]
        public void RoundEvents_AreRejectedOutsidePlaying_InEveryState()
        {
            AssertRoundEventsRejected();                          // Lobby
            _m.StartMatch(out _);
            AssertRoundEventsRejected();                          // RoundSetup
            _m.CompleteRoundSetup(0d, out _);
            AssertRoundEventsRejected();                          // RoleReveal
            _m.Tick(3d, out _);
            AssertRoundEventsRejected();                          // Intro
            DriveTo(MatchState.RoundEnd, 8d);
            AssertRoundEventsRejected();                          // RoundEnd
            DriveTo(MatchState.MatchEnd, 400d);
            AssertRoundEventsRejected();                          // MatchEnd
        }

        [Test]
        public void StageBonus_DoesNotCarryOverToTheNextRound()
        {
            double ps = ReachPlaying();
            _m.TryAddStageTimeBonus(ps + 10d, out _);
            _m.TryAddStageTimeBonus(ps + 20d, out _);

            double now = DriveTo(MatchState.RoundEnd, ps + 20d);   // the extended round times out
            Assert.AreEqual(ps + BaseRound + 2 * Bonus, now);
            double ps2 = DriveTo(MatchState.Playing, now);

            Assert.AreEqual(MatchState.Playing, _m.State);
            Assert.AreEqual(1, _m.RoundIndex);
            Assert.AreEqual(ps2 + BaseRound, _m.StateEndTime);
            Assert.AreEqual(ps2, _m.PlayingStartTime);
        }

        // ---------------------------------------------------------------- round-end conditions

        [Test]
        public void ThreeFaints_EndTheRound_JinnsWin_Faint()
        {
            double ps = ReachPlaying();

            Assert.IsFalse(_m.OnHumanFainted(1, ps + 20d, out _));
            Assert.AreEqual(1, _m.Faints);
            Assert.AreEqual(MatchState.Playing, _m.State);

            Assert.IsFalse(_m.OnHumanFainted(2, ps + 60d, out _));
            Assert.AreEqual(2, _m.Faints);
            Assert.AreEqual(MatchState.Playing, _m.State);

            Assert.IsTrue(_m.OnHumanFainted(3, ps + 100d, out MatchTransition t));
            AssertTransition(t, MatchState.Playing, MatchState.RoundEnd, 0, ps + 100d + RoundEnd);
            Assert.AreEqual(RoundResult.JinnsWin, _m.LastResult);
            Assert.AreEqual(RoundEndReason.Faint, _m.LastReason);
            Assert.AreEqual(3, _m.Faints);
            Assert.AreEqual(ps + 100d + RoundEnd, _m.StateEndTime);
        }

        [Test]
        public void FaintsToLose_ComesFromSettings()
        {
            var twoFaints = new MatchSettings(RoleReveal, Intro, BaseRound, Bonus, WakeDelay, RoundEnd, Rounds, 2);
            _m = new MatchStateMachine(twoFaints, SeedFor);
            double ps = ReachPlaying();

            Assert.IsFalse(_m.OnHumanFainted(1, ps + 5d, out _));
            Assert.IsTrue(_m.OnHumanFainted(2, ps + 6d, out _));
            Assert.AreEqual(RoundEndReason.Faint, _m.LastReason);
        }

        [Test]
        public void FaintBelowThreshold_DoesNotChangeTheCounterDeadline()
        {
            double ps = ReachPlaying();

            _m.OnHumanFainted(1, ps + 20d, out _);

            Assert.AreEqual(ps + BaseRound, _m.StateEndTime);
        }

        [Test]
        public void TreasureDug_EndsTheRound_SeekersWin_TreasureDug()
        {
            double ps = ReachPlaying();

            Assert.IsTrue(_m.OnTreasureDug(ps + 200d, out MatchTransition t));

            AssertTransition(t, MatchState.Playing, MatchState.RoundEnd, 0, ps + 200d + RoundEnd);
            Assert.AreEqual(RoundResult.SeekersWin, _m.LastResult);
            Assert.AreEqual(RoundEndReason.TreasureDug, _m.LastReason);
            Assert.IsFalse(_m.Tick(ps + 200d + RoundEnd - 0.001d, out _));
            Assert.IsTrue(_m.Tick(ps + 200d + RoundEnd, out t));
            AssertTransition(t, MatchState.RoundEnd, MatchState.RoundSetup, 1, double.MaxValue);
        }

        [Test]
        public void Timeout_EndsTheRound_JinnsWin_Timeout()
        {
            double ps = ReachPlaying();

            Assert.IsTrue(_m.Tick(ps + BaseRound, out MatchTransition t));

            AssertTransition(t, MatchState.Playing, MatchState.RoundEnd, 0, ps + BaseRound + RoundEnd);
            Assert.AreEqual(RoundResult.JinnsWin, _m.LastResult);
            Assert.AreEqual(RoundEndReason.Timeout, _m.LastReason);
        }

        [Test]
        public void Timeout_AfterALateTick_StillStartsRoundEndAtTheDeadline()
        {
            double ps = ReachPlaying();

            Assert.IsTrue(_m.Tick(ps + 500d, out MatchTransition t));

            Assert.AreEqual(ps + BaseRound + RoundEnd, t.StateEndTime, "RoundEnd is scheduled from the deadline, not the late tick");
        }

        [Test]
        public void TryEndRound_AcceptsOnlyTheThreeWinConditionPairs()
        {
            double ps = ReachPlaying();

            Assert.IsFalse(_m.TryEndRound(RoundResult.None, RoundEndReason.Timeout, ps + 1d, out _));
            Assert.IsFalse(_m.TryEndRound(RoundResult.JinnsWin, RoundEndReason.None, ps + 1d, out _));
            Assert.IsFalse(_m.TryEndRound(RoundResult.SeekersWin, RoundEndReason.Faint, ps + 1d, out _));
            Assert.IsFalse(_m.TryEndRound(RoundResult.SeekersWin, RoundEndReason.Timeout, ps + 1d, out _));
            Assert.IsFalse(_m.TryEndRound(RoundResult.JinnsWin, RoundEndReason.TreasureDug, ps + 1d, out _));
            Assert.AreEqual(MatchState.Playing, _m.State);

            Assert.IsTrue(_m.TryEndRound(RoundResult.JinnsWin, RoundEndReason.Timeout, ps + 1d, out MatchTransition t));
            AssertTransition(t, MatchState.Playing, MatchState.RoundEnd, 0, ps + 1d + RoundEnd);
        }

        [Test]
        public void SecondRoundEnd_IsRejected_AndTheFirstOutcomeStands()
        {
            double ps = ReachPlaying();
            Assert.IsTrue(_m.OnTreasureDug(ps + 200d, out _));
            double roundEndDeadline = _m.StateEndTime;

            Assert.IsFalse(_m.OnHumanFainted(3, ps + 201d, out _), "faint after the treasure");
            Assert.IsFalse(_m.OnTreasureDug(ps + 201d, out _), "treasure twice");
            Assert.IsFalse(_m.TryEndRound(RoundResult.JinnsWin, RoundEndReason.Faint, ps + 201d, out _));
            Assert.IsFalse(_m.TryAddStageTimeBonus(ps + 201d, out _));

            Assert.AreEqual(MatchState.RoundEnd, _m.State);
            Assert.AreEqual(RoundResult.SeekersWin, _m.LastResult);
            Assert.AreEqual(RoundEndReason.TreasureDug, _m.LastReason);
            Assert.AreEqual(roundEndDeadline, _m.StateEndTime);
            Assert.AreEqual(0, _m.Faints, "faints after the end are not counted");
        }

        [Test]
        public void OriginalPlayingDeadline_DoesNotEndTheRoundAgain_AfterAnEarlyFinish()
        {
            double ps = ReachPlaying();
            _m.OnTreasureDug(ps + 50d, out _);

            var log = new List<MatchTransition>();
            Drain(ps + BaseRound + 100d, log);

            Assert.AreEqual(1, log.Count, "only RoundEnd → RoundSetup");
            AssertTransition(log[0], MatchState.RoundEnd, MatchState.RoundSetup, 1, double.MaxValue);
            Assert.AreEqual(RoundEndReason.TreasureDug, _m.LastReason, "no late Timeout overwrote it");
        }

        [Test]
        public void EventAtOrAfterTheDeadline_IsRejected_AndTheTickReportsTimeout()
        {
            double ps = ReachPlaying();
            double deadline = ps + BaseRound;

            Assert.IsFalse(_m.OnTreasureDug(deadline, out _), "dug exactly when the sun rose");
            Assert.IsFalse(_m.OnHumanFainted(3, deadline + 1d, out _));
            Assert.AreEqual(0, _m.Faints);

            Assert.IsTrue(_m.Tick(deadline, out _));
            Assert.AreEqual(RoundResult.JinnsWin, _m.LastResult);
            Assert.AreEqual(RoundEndReason.Timeout, _m.LastReason);
        }

        [Test]
        public void Faints_AreKeptForTheRoundReport_ThenResetWhenTheNextRoundStarts()
        {
            double ps = ReachPlaying();
            _m.OnHumanFainted(2, ps + 30d, out _);
            _m.Tick(ps + BaseRound, out _);

            Assert.AreEqual(MatchState.RoundEnd, _m.State);
            Assert.AreEqual(2, _m.Faints, "still readable during RoundEnd");

            DriveTo(MatchState.RoundSetup, ps + BaseRound);
            Assert.AreEqual(1, _m.RoundIndex);
            Assert.AreEqual(0, _m.Faints);
        }

        [Test]
        public void Faints_ResetWhenTheNextRoundStarts_AfterALossByFaints()
        {
            double ps = ReachPlaying();
            _m.OnHumanFainted(1, ps + 10d, out _);
            _m.OnHumanFainted(2, ps + 20d, out _);
            _m.OnHumanFainted(3, ps + 30d, out _);
            Assert.AreEqual(3, _m.Faints);

            double ps2 = DriveTo(MatchState.Playing, ps + 30d);

            Assert.AreEqual(0, _m.Faints);
            Assert.IsFalse(_m.OnHumanFainted(1, ps2 + 5d, out _), "the new round counts from zero");
            Assert.AreEqual(MatchState.Playing, _m.State);
        }

        [Test]
        public void EveryRoundCanEndDifferently_InOneMatch()
        {
            double ps = ReachPlaying();
            _m.OnHumanFainted(3, ps + 10d, out _);
            Assert.AreEqual(RoundEndReason.Faint, _m.LastReason);

            double ps2 = DriveTo(MatchState.Playing, ps + 10d);
            _m.OnTreasureDug(ps2 + 10d, out _);
            Assert.AreEqual(RoundEndReason.TreasureDug, _m.LastReason);
            Assert.AreEqual(RoundResult.SeekersWin, _m.LastResult);

            double ps3 = DriveTo(MatchState.Playing, ps2 + 10d);
            _m.Tick(ps3 + BaseRound, out _);
            Assert.AreEqual(RoundEndReason.Timeout, _m.LastReason);
            Assert.AreEqual(2, _m.RoundIndex);
        }

        // ---------------------------------------------------------------- big time jumps / simulated frames

        [Test]
        public void BigJump_SingleTickLoop_DrainsEveryStateInOrder_WithTheExactSchedule()
        {
            _m.StartMatch(out _);
            _m.CompleteRoundSetup(0d, out _);
            var log = new List<MatchTransition>();

            int count = Drain(1000d, log);

            Assert.AreEqual(4, count);
            AssertTransition(log[0], MatchState.RoleReveal, MatchState.Intro, 0, 8d);
            AssertTransition(log[1], MatchState.Intro, MatchState.Playing, 0, 368d);
            AssertTransition(log[2], MatchState.Playing, MatchState.RoundEnd, 0, 376d);
            AssertTransition(log[3], MatchState.RoundEnd, MatchState.RoundSetup, 1, double.MaxValue);
            Assert.AreEqual(RoundEndReason.Timeout, _m.LastReason);

            Assert.IsFalse(_m.Tick(1000d, out _), "RoundSetup waits for CompleteRoundSetup");
            Assert.IsTrue(_m.CompleteRoundSetup(1000d, out MatchTransition t));
            Assert.AreEqual(1003d, t.StateEndTime, "the next round is scheduled from when the setup completed");
            Assert.IsFalse(_m.Tick(1000d, out _));
        }

        [Test]
        public void BigJump_LandingInsidePlaying_StopsThere_AndJinnsAreAwake()
        {
            _m.StartMatch(out _);
            _m.CompleteRoundSetup(0d, out _);

            int count = Drain(100d);

            Assert.AreEqual(2, count);
            Assert.AreEqual(MatchState.Playing, _m.State);
            Assert.AreEqual(368d, _m.StateEndTime);
            Assert.IsTrue(_m.JinnsAwake);
        }

        [Test]
        public void BigJump_WholeMatch_InThousandSecondSteps_VisitsEveryStateOnce()
        {
            double now = 0d;
            _m.StartMatch(out _);
            _m.CompleteRoundSetup(now, out _);
            var visited = new List<MatchState>();

            for (int step = 1; step <= 20 && _m.State != MatchState.MatchEnd; step++)
            {
                now = step * 1000d;
                while (_m.Tick(now, out MatchTransition t))
                {
                    visited.Add(t.To);
                    if (t.To == MatchState.RoundSetup)
                    {
                        Assert.IsTrue(_m.CompleteRoundSetup(now, out MatchTransition reveal));
                        visited.Add(reveal.To);
                    }
                }
            }

            var expected = new List<MatchState>();
            for (int round = 0; round < Rounds; round++)
            {
                expected.Add(MatchState.Intro);
                expected.Add(MatchState.Playing);
                expected.Add(MatchState.RoundEnd);
                if (round < Rounds - 1)
                {
                    expected.Add(MatchState.RoundSetup);
                    expected.Add(MatchState.RoleReveal);
                }
                else
                {
                    expected.Add(MatchState.MatchEnd);
                }
            }

            CollectionAssert.AreEqual(expected, visited);
            Assert.AreEqual(Rounds - 1, _m.RoundIndex);
        }

        [Test]
        public void SimulatedFrames_At64Fps_FollowTheAnalyticTimeline()
        {
            const double Frame = 1d / 64d;              // exactly representable: no accumulated drift
            var states = new List<MatchState>();
            var times = new List<double>();
            _m.StartMatch(out _);
            _m.CompleteRoundSetup(0d, out _);

            long frame = 0;
            while (_m.State != MatchState.MatchEnd)
            {
                Assert.Less(++frame, 200000L, "simulation did not finish");
                double now = frame * Frame;
                while (_m.Tick(now, out MatchTransition t))
                {
                    states.Add(t.To);
                    times.Add(now);
                    if (t.To == MatchState.RoundSetup)
                    {
                        Assert.IsTrue(_m.CompleteRoundSetup(now, out MatchTransition reveal));
                        states.Add(reveal.To);
                        times.Add(now);
                    }
                }
            }

            var expectedStates = new List<MatchState>();
            var expectedTimes = new List<double>();
            for (int round = 0; round < Rounds; round++)
            {
                double start = round * TimeoutRoundLength;
                expectedStates.Add(MatchState.Intro);
                expectedTimes.Add(start + RoleReveal);
                expectedStates.Add(MatchState.Playing);
                expectedTimes.Add(start + RoleReveal + Intro);
                expectedStates.Add(MatchState.RoundEnd);
                expectedTimes.Add(start + RoleReveal + Intro + BaseRound);
                if (round < Rounds - 1)
                {
                    expectedStates.Add(MatchState.RoundSetup);
                    expectedTimes.Add(start + TimeoutRoundLength);
                    expectedStates.Add(MatchState.RoleReveal);
                    expectedTimes.Add(start + TimeoutRoundLength);
                }
                else
                {
                    expectedStates.Add(MatchState.MatchEnd);
                    expectedTimes.Add(start + TimeoutRoundLength);
                }
            }

            CollectionAssert.AreEqual(expectedStates, states);
            CollectionAssert.AreEqual(expectedTimes, times);
        }

        // ---------------------------------------------------------------- settings

        [Test]
        public void MatchSettings_FromConfig_ReadsTheBalanceConfig()
        {
            var config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            try
            {
                MatchSettings s = MatchSettings.FromConfig(config);

                Assert.AreEqual(config.RoleRevealDuration, s.RoleRevealDuration);
                Assert.AreEqual(config.IntroCountdown, s.IntroCountdown);
                Assert.AreEqual(config.BaseRoundDuration, s.BaseRoundDuration);
                Assert.AreEqual(config.StageTimeBonus, s.StageTimeBonus);
                Assert.AreEqual(config.JinnWakeDelay, s.JinnWakeDelay);
                Assert.AreEqual(config.RoundEndDuration, s.RoundEndDuration);
                Assert.AreEqual(config.RoundsPerMatch, s.RoundsPerMatch);
                Assert.AreEqual(config.FaintsToLose, s.FaintsToLose);

                // ... and those are the Oyun §14 numbers this suite relies on.
                Assert.AreEqual(RoleReveal, s.RoleRevealDuration);
                Assert.AreEqual(Intro, s.IntroCountdown);
                Assert.AreEqual(BaseRound, s.BaseRoundDuration);
                Assert.AreEqual(Bonus, s.StageTimeBonus);
                Assert.AreEqual(WakeDelay, s.JinnWakeDelay);
                Assert.AreEqual(RoundEnd, s.RoundEndDuration);
                Assert.AreEqual(Rounds, s.RoundsPerMatch);
                Assert.AreEqual(FaintsToLose, s.FaintsToLose);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void MatchSettings_FromConfig_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => MatchSettings.FromConfig(null));
        }

        [Test]
        public void Settings_AreExposedToTheCaller()
        {
            Assert.AreEqual(Bonus, _m.Settings.StageTimeBonus);
            Assert.AreEqual(Rounds, _m.Settings.RoundsPerMatch);
        }
    }
}
