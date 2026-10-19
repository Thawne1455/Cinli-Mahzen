using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.Core.Events;
using CinliMahzen.Core.Net;
using CinliMahzen.Match;
using NUnit.Framework;

namespace CinliMahzen.Tests.EditMode.Match
{
    /// <summary>TODO 3.10: tiered scoring (Oyun §10) through <see cref="ScoreService"/> over an offline bridge.</summary>
    public class ScoreServiceTests
    {
        private static readonly ScoreRules Rules = new ScoreRules(50, 50, 150, 50, 200, 40);

        private OfflineNetBridge _bridge;
        private OfflinePlayerRegistry _players;
        private StubObjectiveInfo _objectives;
        private MatchState _state;
        private ScoreService _score;
        private readonly List<ScoreChangedEvt> _events = new List<ScoreChangedEvt>();

        [SetUp]
        public void SetUp()
        {
            EventBus.ClearAll();
            _bridge = new OfflineNetBridge(() => 0d, () => new PlayerId(0));
            _players = new OfflinePlayerRegistry();
            _players.SetRoles(new[] { Role.Human, Role.GoodJinn, Role.EvilJinn, Role.EvilJinn });
            _objectives = new StubObjectiveInfo { PuzzleCount = 4 };
            _state = MatchState.Playing;
            _score = new ScoreService(_bridge, _players, Rules, () => _objectives, () => _state);
            _score.Attach();
            _events.Clear();
            EventBus.Subscribe<ScoreChangedEvt>(_events.Add);
        }

        [TearDown]
        public void TearDown()
        {
            _score.Detach();
            EventBus.ClearAll();
        }

        private int S(int player) => _score.ScoreOf(new PlayerId((byte)player));

        [Test]
        public void SolvedPuzzle_RewardsBothSeekers_Once()
        {
            EventBus.Publish(new PuzzleCompletedEvt(3, 1));
            EventBus.Publish(new PuzzleCompletedEvt(3, 1)); // duplicate

            Assert.AreEqual(50, S(0));
            Assert.AreEqual(50, S(1));
            Assert.AreEqual(0, S(2));
            Assert.AreEqual(0, S(3));
            Assert.AreEqual(50, _players.Get(new PlayerId(0)).Score, "the registry follows");
            Assert.AreEqual(2, _events.Count);
            Assert.AreEqual(ScoreReason.Puzzle, _events[0].Reason);
        }

        [Test]
        public void NothingCounts_OutsidePlaying()
        {
            _state = MatchState.RoundEnd;
            EventBus.Publish(new PuzzleCompletedEvt(1, 1));
            Assert.AreEqual(0, S(0));
        }

        [Test]
        public void CellarOpening_RewardsSeekersOncePerRound()
        {
            EventBus.Publish(new StageCompletedEvt(1));
            Assert.AreEqual(0, S(0), "a house gate is not the cellar");

            _objectives.Phase = ObjectivePhase.Cellar;
            EventBus.Publish(new StageCompletedEvt(2));
            EventBus.Publish(new StageCompletedEvt(3));
            Assert.AreEqual(50, S(0));
            Assert.AreEqual(50, S(1));

            EventBus.Publish(new RoundStartedEvt(1, 7));
            EventBus.Publish(new StageCompletedEvt(2));
            Assert.AreEqual(100, S(0), "again in the next round");
        }

        [Test]
        public void Faint_RewardsEveryEvilJinn()
        {
            _score.AwardFaintAuthority();
            _score.AwardFaintAuthority();

            Assert.AreEqual(100, S(2));
            Assert.AreEqual(100, S(3));
            Assert.AreEqual(0, S(0));
        }

        [Test]
        public void Treasure_GivesWinPointsPlusWholeSecondsLeft()
        {
            _score.AwardRoundEndAuthority(RoundResult.SeekersWin, 42.9);

            Assert.AreEqual(192, S(0));
            Assert.AreEqual(192, S(1));
            Assert.AreEqual(0, S(2));
            Assert.AreEqual(ScoreReason.TreasureWin, _events[0].Reason);
        }

        [Test]
        public void JinnWin_AddsUnfinishedPuzzles()
        {
            EventBus.Publish(new PuzzleCompletedEvt(0, 1)); // 1 of 4 solved

            _score.AwardRoundEndAuthority(RoundResult.JinnsWin, 0d);

            Assert.AreEqual(200 + 3 * 40, S(2));
            Assert.AreEqual(200 + 3 * 40, S(3));
            Assert.AreEqual(50, S(0));
        }

        [Test]
        public void Scores_FollowTheRoleRotation()
        {
            _score.AwardRoundEndAuthority(RoundResult.SeekersWin, 0d);   // P0, P1
            _players.SetRoles(new[] { Role.EvilJinn, Role.Human, Role.GoodJinn, Role.EvilJinn });
            EventBus.Publish(new RoundStartedEvt(1, 3));
            _score.AwardFaintAuthority();                                  // P0, P3

            Assert.AreEqual(200, S(0));
            Assert.AreEqual(150, S(1));
            Assert.AreEqual(0, S(2));
            Assert.AreEqual(50, S(3));
        }

        [Test]
        public void ResetMatch_ZeroesEveryone()
        {
            _score.AwardFaintAuthority();
            _score.ResetMatchAuthority();

            for (int i = 0; i < 4; i++)
                Assert.AreEqual(0, S(i));
            Assert.AreEqual(0, _players.Get(new PlayerId(2)).Score);
        }

        [Test]
        public void Leader_IsTheTopScorer_OrNoneOnATie()
        {
            Assert.AreEqual(PlayerId.None, _score.Leader(out _), "all zero");

            _score.AwardRoundEndAuthority(RoundResult.SeekersWin, 10d);  // P0, P1 = 160
            _players.SetRoles(new[] { Role.EvilJinn, Role.Human, Role.GoodJinn, Role.EvilJinn });
            _score.AwardFaintAuthority();                                  // P0 = 210, P3 = 50

            Assert.AreEqual(new PlayerId(0), _score.Leader(out int top));
            Assert.AreEqual(210, top);
        }

        [Test]
        public void ScoreChanged_RoundTrip_AndRejectsBadData()
        {
            NetMsg msg = ScoreChangedMsg.Encode(new PlayerId(2), 310, 50, ScoreReason.Faint);
            Assert.IsTrue(ScoreChangedMsg.TryDecode(msg, out PlayerId player, out int total, out int delta, out ScoreReason reason));
            Assert.AreEqual(new PlayerId(2), player);
            Assert.AreEqual(310, total);
            Assert.AreEqual(50, delta);
            Assert.AreEqual(ScoreReason.Faint, reason);

            Assert.IsFalse(ScoreChangedMsg.TryDecode(new NetMsg { Code = MsgCode.ScoreChanged, Payload = new object[] { (byte)1, 5, 5, (byte)99 } }, out _, out _, out _, out _));
            Assert.IsFalse(ScoreChangedMsg.TryDecode(new NetMsg { Code = MsgCode.ScoreChanged, Payload = new object[] { (byte)255, 5, 5, (byte)1 } }, out _, out _, out _, out _));
            Assert.IsFalse(ScoreChangedMsg.TryDecode(new NetMsg { Code = MsgCode.RoundEnded, Payload = msg.Payload }, out _, out _, out _, out _));
        }
    }
}
