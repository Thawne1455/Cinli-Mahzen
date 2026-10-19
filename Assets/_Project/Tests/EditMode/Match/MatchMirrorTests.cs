using System;
using CinliMahzen.Core;
using CinliMahzen.Core.Net;
using CinliMahzen.Match;
using NUnit.Framework;

namespace CinliMahzen.Tests.EditMode.Match
{
    /// <summary>
    /// The state every machine keeps from the match broadcasts (<see cref="MatchMirror"/>, Teknik §4.3 / §9): it must end up exactly where
    /// the authority's <see cref="MatchStateMachine"/> is, having seen nothing but the encoded messages.
    /// </summary>
    public class MatchMirrorTests
    {
        private const float WakeDelay = 10f;
        private const float BaseRound = 360f;
        private const float Bonus = 90f;
        private const double Delta = 1e-9;
        private const int MatchSeed = 77;

        private double _clock;
        private MatchMirror _mirror;

        private static MatchSettings Settings()
        {
            return new MatchSettings(3f, 5f, BaseRound, Bonus, WakeDelay, 8f, 4, 3);
        }

        [SetUp]
        public void SetUp()
        {
            _clock = 0d;
            _mirror = new MatchMirror(Settings(), () => _clock);
        }

        // ---------------------------------------------------------------- the mirror on its own

        [Test]
        public void Constructor_NullClock_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new MatchMirror(Settings(), null));
        }

        [Test]
        public void Initial_IsLobby_WithoutTimerOrRoundData()
        {
            Assert.AreEqual(MatchState.Lobby, _mirror.State);
            Assert.AreEqual(0, _mirror.RoundIndex);
            Assert.AreEqual(double.MaxValue, _mirror.StateEndTime);
            Assert.AreEqual(0, _mirror.Faints);
            Assert.AreEqual(0, _mirror.Seed);
            Assert.AreEqual(RoundResult.None, _mirror.LastResult);
            Assert.AreEqual(RoundEndReason.None, _mirror.LastReason);
            Assert.AreEqual(double.MaxValue, _mirror.PlayingStartTime);
            Assert.IsFalse(_mirror.JinnsAwake);
        }

        [Test]
        public void ApplyStateChanged_StoresStateRoundAndEndTime()
        {
            _mirror.ApplyStateChanged(MatchState.RoleReveal, 2, 53.5);

            Assert.AreEqual(MatchState.RoleReveal, _mirror.State);
            Assert.AreEqual(2, _mirror.RoundIndex);
            Assert.AreEqual(53.5, _mirror.StateEndTime);
        }

        [Test]
        public void ApplyStateChanged_Playing_DerivesPlayingStartFromTheEndTime()
        {
            _mirror.ApplyStateChanged(MatchState.Playing, 0, 18d + BaseRound);

            Assert.AreEqual(18d, _mirror.PlayingStartTime, Delta);
        }

        [Test]
        public void ApplyStateChanged_PlayingAgain_OnlyMovesTheEndTime()
        {
            _mirror.ApplyStateChanged(MatchState.Playing, 0, 18d + BaseRound);
            _mirror.ApplyStateChanged(MatchState.Playing, 0, 40d);

            Assert.AreEqual(40d, _mirror.StateEndTime);
            Assert.AreEqual(18d, _mirror.PlayingStartTime, Delta, "a re-sent Playing (debug skip) must not move the start");
        }

        [Test]
        public void ApplyStateChanged_RoundSetup_StartsANewRound()
        {
            _mirror.ApplyStateChanged(MatchState.Playing, 0, 18d + BaseRound);
            _mirror.ApplyFaints(2);
            _mirror.ApplyRoundEnded(RoundResult.JinnsWin, RoundEndReason.Faint);
            _mirror.ApplyRoundSetup(1111);

            _mirror.ApplyStateChanged(MatchState.RoundSetup, 1, double.MaxValue);

            Assert.AreEqual(1, _mirror.RoundIndex);
            Assert.AreEqual(0, _mirror.Faints, "faints are per round");
            Assert.AreEqual(double.MaxValue, _mirror.PlayingStartTime);
            Assert.AreEqual(1111, _mirror.Seed, "the seed stays until the RoundSetup message brings the new one");
            Assert.AreEqual(RoundResult.JinnsWin, _mirror.LastResult, "the last result survives into the next round");
            Assert.AreEqual(RoundEndReason.Faint, _mirror.LastReason);
        }

        [Test]
        public void ApplyStateChanged_Lobby_ClearsTheMatch()
        {
            _mirror.ApplyStateChanged(MatchState.Playing, 3, 18d + BaseRound);
            _mirror.ApplyRoundSetup(55);
            _mirror.ApplyFaints(2);
            _mirror.ApplyRoundEnded(RoundResult.SeekersWin, RoundEndReason.TreasureDug);

            _mirror.ApplyStateChanged(MatchState.Lobby, 0, double.MaxValue);

            Assert.AreEqual(MatchState.Lobby, _mirror.State);
            Assert.AreEqual(0, _mirror.RoundIndex);
            Assert.AreEqual(double.MaxValue, _mirror.StateEndTime);
            Assert.AreEqual(0, _mirror.Faints);
            Assert.AreEqual(0, _mirror.Seed);
            Assert.AreEqual(RoundResult.None, _mirror.LastResult);
            Assert.AreEqual(RoundEndReason.None, _mirror.LastReason);
            Assert.AreEqual(double.MaxValue, _mirror.PlayingStartTime);
        }

        [Test]
        public void JinnsAwake_FollowsTheClock_OnlyWhilePlaying()
        {
            _mirror.ApplyStateChanged(MatchState.Playing, 0, 18d + BaseRound);

            _clock = 18d + WakeDelay - 0.1;
            Assert.IsFalse(_mirror.JinnsAwake, "still asleep");
            _clock = 18d + WakeDelay;
            Assert.IsTrue(_mirror.JinnsAwake, "awake exactly at JinnWakeDelay");

            _mirror.ApplyStateChanged(MatchState.RoundEnd, 0, _clock + 8d);
            Assert.IsFalse(_mirror.JinnsAwake, "not in Playing any more");
        }

        [Test]
        public void IsJinnsAwakeAt_AnswersForAnExactTime()
        {
            _mirror.ApplyStateChanged(MatchState.Playing, 0, 18d + BaseRound);

            Assert.IsFalse(_mirror.IsJinnsAwakeAt(27.9));
            Assert.IsTrue(_mirror.IsJinnsAwakeAt(28d));
        }

        [Test]
        public void ApplyTimeAdded_InPlaying_MovesTheEndTime()
        {
            _mirror.ApplyStateChanged(MatchState.Playing, 0, 18d + BaseRound);

            Assert.IsTrue(_mirror.ApplyTimeAdded(18d + BaseRound + Bonus));

            Assert.AreEqual(18d + BaseRound + Bonus, _mirror.StateEndTime, Delta);
            Assert.AreEqual(18d, _mirror.PlayingStartTime, Delta, "a bonus does not move the Playing start");
        }

        [Test]
        public void ApplyTimeAdded_OutsidePlaying_IsRejected()
        {
            _mirror.ApplyStateChanged(MatchState.Intro, 0, 18d);

            Assert.IsFalse(_mirror.ApplyTimeAdded(999d));

            Assert.AreEqual(18d, _mirror.StateEndTime);
        }

        [Test]
        public void ApplyFaints_StoresTheCount_AndClampsNegatives()
        {
            _mirror.ApplyFaints(2);
            Assert.AreEqual(2, _mirror.Faints);

            _mirror.ApplyFaints(-5);
            Assert.AreEqual(0, _mirror.Faints);
        }

        [Test]
        public void ApplyRoundSetup_StoresTheSeed()
        {
            _mirror.ApplyRoundSetup(424242);
            Assert.AreEqual(424242, _mirror.Seed);
        }

        [Test]
        public void ApplyRoundEnded_StoresResultAndReason()
        {
            _mirror.ApplyRoundEnded(RoundResult.SeekersWin, RoundEndReason.TreasureDug);

            Assert.AreEqual(RoundResult.SeekersWin, _mirror.LastResult);
            Assert.AreEqual(RoundEndReason.TreasureDug, _mirror.LastReason);
        }

        // ---------------------------------------------------------------- mirror vs. the authority's machine

        /// <summary>What the MatchController broadcasts for a transition and what every machine's handlers then apply (through the real codecs).</summary>
        private void Forward(MatchStateMachine machine, in MatchTransition transition)
        {
            NetMsg changed = MatchStateChangedMsg.Encode(transition);
            Assert.IsTrue(MatchStateChangedMsg.TryDecode(changed, out MatchState state, out int round, out double endTime));
            _mirror.ApplyStateChanged(state, round, endTime);

            if (transition.To == MatchState.RoundSetup)
            {
                NetMsg setup = RoundSetupMsg.Encode(machine.Seed, RoleRotation.CreateRoles(transition.RoundIndex));
                Assert.IsTrue(RoundSetupMsg.TryDecode(setup, out int seed, out Role[] _));
                _mirror.ApplyRoundSetup(seed);
            }
            else if (transition.To == MatchState.RoundEnd)
            {
                NetMsg ended = RoundEndedMsg.Encode(machine.LastResult, machine.LastReason, transition.RoundIndex);
                Assert.IsTrue(RoundEndedMsg.TryDecode(ended, out RoundResult result, out RoundEndReason reason, out int _));
                _mirror.ApplyRoundEnded(result, reason);
            }
        }

        private void AssertMirrorEquals(MatchStateMachine machine, string when)
        {
            Assert.AreEqual(machine.State, _mirror.State, when + ": state");
            Assert.AreEqual(machine.RoundIndex, _mirror.RoundIndex, when + ": round");
            Assert.AreEqual(machine.StateEndTime, _mirror.StateEndTime, Delta, when + ": end time");
            Assert.AreEqual(machine.Faints, _mirror.Faints, when + ": faints");
            Assert.AreEqual(machine.LastResult, _mirror.LastResult, when + ": last result");
            Assert.AreEqual(machine.LastReason, _mirror.LastReason, when + ": last reason");
            if (machine.State != MatchState.Lobby)
            {
                Assert.AreEqual(machine.Seed, _mirror.Seed, when + ": seed");
            }
        }

        [Test]
        public void Mirror_FollowsTheMachineThroughAFaintRoundIntoTheNextRound()
        {
            var machine = new MatchStateMachine(Settings(), round => RoundSeeds.ForRound(MatchSeed, round));
            AssertMirrorEquals(machine, "Lobby");

            Assert.IsTrue(machine.StartMatch(out MatchTransition t));
            Forward(machine, t);
            AssertMirrorEquals(machine, "RoundSetup");
            Assert.AreEqual(RoundSeeds.ForRound(MatchSeed, 0), _mirror.Seed);

            Assert.IsTrue(machine.CompleteRoundSetup(100d, out t));
            Forward(machine, t);
            AssertMirrorEquals(machine, "RoleReveal");

            Assert.IsTrue(machine.Tick(103d, out t));
            Forward(machine, t);
            AssertMirrorEquals(machine, "Intro");

            Assert.IsTrue(machine.Tick(108d, out t));
            Forward(machine, t);
            AssertMirrorEquals(machine, "Playing");
            Assert.AreEqual(machine.PlayingStartTime, _mirror.PlayingStartTime, Delta);
            Assert.AreEqual(machine.IsJinnsAwakeAt(117.9), _mirror.IsJinnsAwakeAt(117.9));
            Assert.AreEqual(machine.IsJinnsAwakeAt(118d), _mirror.IsJinnsAwakeAt(118d));

            Assert.IsTrue(machine.TryAddStageTimeBonus(120d, out double newEnd));
            Assert.IsTrue(RoundTimeAddedMsg.TryDecode(RoundTimeAddedMsg.Encode(Bonus, newEnd), out float bonus, out double decodedEnd));
            Assert.AreEqual(Bonus, bonus);
            Assert.IsTrue(_mirror.ApplyTimeAdded(decodedEnd));
            AssertMirrorEquals(machine, "after +90 s");
            Assert.AreEqual(108d + BaseRound + Bonus, _mirror.StateEndTime, Delta);

            for (int faint = 1; faint <= 2; faint++)
            {
                Assert.IsFalse(machine.OnHumanFainted(faint, 130d + faint, out _));
                _mirror.ApplyFaints(faint);
                AssertMirrorEquals(machine, "faint " + faint);
            }

            Assert.IsTrue(machine.OnHumanFainted(3, 140d, out t));
            _mirror.ApplyFaints(3);
            Forward(machine, t);
            AssertMirrorEquals(machine, "RoundEnd");
            Assert.AreEqual(RoundResult.JinnsWin, _mirror.LastResult);
            Assert.AreEqual(RoundEndReason.Faint, _mirror.LastReason);
            Assert.AreEqual(148d, _mirror.StateEndTime, Delta, "RoundEnd lasts 8 s from the last faint");

            Assert.IsTrue(machine.Tick(148d, out t));
            Forward(machine, t);
            AssertMirrorEquals(machine, "next RoundSetup");
            Assert.AreEqual(1, _mirror.RoundIndex);
            Assert.AreEqual(0, _mirror.Faints);
            Assert.AreEqual(RoundSeeds.ForRound(MatchSeed, 1), _mirror.Seed);
        }

        [Test]
        public void Mirror_FollowsASkippedTimer()
        {
            var machine = new MatchStateMachine(Settings(), round => RoundSeeds.ForRound(MatchSeed, round));
            Assert.IsTrue(machine.StartMatch(out MatchTransition t));
            Forward(machine, t);
            Assert.IsTrue(machine.CompleteRoundSetup(100d, out t));
            Forward(machine, t);

            // The controller's SkipStateTimer: pull the end in, re-send the unchanged state, then tick on.
            Assert.IsTrue(machine.TrySkipTimer(101d));
            _mirror.ApplyStateChanged(machine.State, machine.RoundIndex, machine.StateEndTime);
            AssertMirrorEquals(machine, "RoleReveal skipped");
            Assert.AreEqual(101d, _mirror.StateEndTime, Delta);

            Assert.IsTrue(machine.Tick(101d, out t));
            Forward(machine, t);
            AssertMirrorEquals(machine, "Intro after skip");
            Assert.AreEqual(106d, _mirror.StateEndTime, Delta, "Intro still lasts its full 5 s, counted from the pulled-in end");
        }
    }
}
