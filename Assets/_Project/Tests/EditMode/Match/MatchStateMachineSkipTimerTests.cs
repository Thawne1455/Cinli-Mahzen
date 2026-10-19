using CinliMahzen.Core;
using CinliMahzen.Match;
using NUnit.Framework;

namespace CinliMahzen.Tests.EditMode.Match
{
    /// <summary>
    /// <see cref="MatchStateMachine.TrySkipTimer"/> - the debug "Skip Match State" (TODO 1.10): pulls the end of a timed state back to
    /// "now" so MCP runs need not wait out RoleReveal, Intro or RoundEnd. Only the clock moves; the schedule logic is untouched.
    /// </summary>
    public class MatchStateMachineSkipTimerTests
    {
        private const float RoleReveal = 3f;
        private const float Intro = 5f;
        private const float BaseRound = 360f;
        private const float RoundEnd = 8f;

        private static MatchStateMachine NewMachine(int rounds = 4)
        {
            var settings = new MatchSettings(RoleReveal, Intro, BaseRound, 90f, 10f, RoundEnd, rounds, 3);
            return new MatchStateMachine(settings, round => 1000 + round);
        }

        /// <summary>Lobby → RoleReveal; setup completed at <paramref name="setupDoneAt"/>.</summary>
        private static MatchStateMachine AtRoleReveal(double setupDoneAt = 100d, int rounds = 4)
        {
            MatchStateMachine m = NewMachine(rounds);
            Assert.IsTrue(m.StartMatch(out _));
            Assert.IsTrue(m.CompleteRoundSetup(setupDoneAt, out _));
            return m;
        }

        [Test]
        public void RoleReveal_Skip_PullsTheEndToNow_AndTheNextTickLeavesIt()
        {
            MatchStateMachine m = AtRoleReveal(100d);

            Assert.IsTrue(m.TrySkipTimer(101d));

            Assert.AreEqual(MatchState.RoleReveal, m.State, "skipping alone does not change the state");
            Assert.AreEqual(101d, m.StateEndTime);
            Assert.IsTrue(m.Tick(101d, out MatchTransition t));
            Assert.AreEqual(MatchState.Intro, t.To);
            Assert.AreEqual(101d + Intro, t.StateEndTime, "Intro keeps its full length, counted from the pulled-in end");
        }

        [Test]
        public void Intro_Skip_StartsPlayingAtTheSkipTime()
        {
            MatchStateMachine m = AtRoleReveal(100d);
            Assert.IsTrue(m.Tick(103d, out _));
            Assert.AreEqual(MatchState.Intro, m.State);

            Assert.IsTrue(m.TrySkipTimer(104d));
            Assert.IsTrue(m.Tick(104d, out MatchTransition t));

            Assert.AreEqual(MatchState.Playing, t.To);
            Assert.AreEqual(104d, m.PlayingStartTime);
            Assert.AreEqual(104d + BaseRound, m.StateEndTime, "the Sunrise counter still starts at the full 360 s");
        }

        [Test]
        public void Playing_Skip_EndsTheRoundByTimeout()
        {
            MatchStateMachine m = AtRoleReveal(100d);
            m.Tick(103d, out _);
            m.Tick(108d, out _);
            Assert.AreEqual(MatchState.Playing, m.State);

            Assert.IsTrue(m.TrySkipTimer(120d));
            Assert.IsTrue(m.Tick(120d, out MatchTransition t));

            Assert.AreEqual(MatchState.RoundEnd, t.To);
            Assert.AreEqual(RoundResult.JinnsWin, m.LastResult);
            Assert.AreEqual(RoundEndReason.Timeout, m.LastReason);
            Assert.AreEqual(120d + RoundEnd, m.StateEndTime);
        }

        [Test]
        public void RoundEnd_Skip_GoesStraightToTheNextRoundSetup()
        {
            MatchStateMachine m = AtRoleReveal(100d);
            m.Tick(103d, out _);
            m.Tick(108d, out _);
            Assert.IsTrue(m.TryEndRound(RoundResult.JinnsWin, RoundEndReason.Faint, 130d, out _));
            Assert.AreEqual(MatchState.RoundEnd, m.State);

            Assert.IsTrue(m.TrySkipTimer(131d));
            Assert.IsTrue(m.Tick(131d, out MatchTransition t));

            Assert.AreEqual(MatchState.RoundSetup, t.To);
            Assert.AreEqual(1, t.RoundIndex);
        }

        [Test]
        public void StateAlreadyDue_IsNotSkipped()
        {
            MatchStateMachine m = AtRoleReveal(100d);

            Assert.IsFalse(m.TrySkipTimer(103d), "ends exactly now: nothing to pull in");
            Assert.IsFalse(m.TrySkipTimer(500d), "ends in the past: nothing to pull in");
            Assert.AreEqual(103d, m.StateEndTime);
        }

        [Test]
        public void SkippingTwice_ChangesNothingTheSecondTime()
        {
            MatchStateMachine m = AtRoleReveal(100d);

            Assert.IsTrue(m.TrySkipTimer(101d));
            Assert.IsFalse(m.TrySkipTimer(101d));
            Assert.AreEqual(101d, m.StateEndTime);
        }

        [Test]
        public void StatesWithoutATimer_AreNotSkippable()
        {
            MatchStateMachine m = NewMachine(rounds: 1);

            Assert.IsFalse(m.TrySkipTimer(0d), "Lobby");
            Assert.AreEqual(MatchState.Lobby, m.State);

            Assert.IsTrue(m.StartMatch(out _));
            Assert.IsFalse(m.TrySkipTimer(0d), "RoundSetup");
            Assert.AreEqual(double.MaxValue, m.StateEndTime);

            // Walk a one-round match to MatchEnd with skips only.
            Assert.IsTrue(m.CompleteRoundSetup(0d, out _));
            Assert.IsTrue(m.TrySkipTimer(1d));
            Assert.IsTrue(m.Tick(1d, out _));
            Assert.IsTrue(m.TrySkipTimer(2d));
            Assert.IsTrue(m.Tick(2d, out _));
            Assert.IsTrue(m.TrySkipTimer(3d));
            Assert.IsTrue(m.Tick(3d, out _));
            Assert.AreEqual(MatchState.RoundEnd, m.State);
            Assert.IsTrue(m.TrySkipTimer(4d));
            Assert.IsTrue(m.Tick(4d, out _));

            Assert.AreEqual(MatchState.MatchEnd, m.State);
            Assert.IsFalse(m.TrySkipTimer(5d), "MatchEnd");
            Assert.AreEqual(double.MaxValue, m.StateEndTime);
        }
    }
}
