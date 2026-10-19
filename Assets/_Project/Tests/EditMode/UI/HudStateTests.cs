using CinliMahzen.Core;
using CinliMahzen.Core.Events;
using CinliMahzen.Core.Net;
using CinliMahzen.UI;
using NUnit.Framework;

namespace CinliMahzen.Tests.EditMode.UI
{
    /// <summary>TODO 3.11: <see cref="HudState"/> (change-driven HUD values) and <see cref="HudFormat"/>.</summary>
    public class HudStateTests
    {
        private double _now;
        private OfflineNetBridge _bridge;
        private HudState _state;

        [SetUp]
        public void SetUp()
        {
            EventBus.ClearAll();
            _now = 10d;
            _bridge = new OfflineNetBridge(() => _now, () => new PlayerId(1));
            _state = new HudState(_bridge, () => new PlayerId(1));
            _state.Attach();
        }

        [TearDown]
        public void TearDown()
        {
            _state.Detach();
            EventBus.ClearAll();
        }

        [Test]
        public void Inventory_FollowsTheBroadcast_AndClearsOnNewRound()
        {
            int version = _state.InventoryVersion;
            _bridge.Broadcast(InventoryChangedMsg.Encode(new PlayerId(0), new[] { "paint", "", "shovel" }));

            Assert.AreEqual(3, _state.InventorySlots.Count);
            Assert.AreEqual("shovel", _state.InventorySlots[2]);
            Assert.Greater(_state.InventoryVersion, version);

            EventBus.Publish(new RoundStartedEvt(1, 5));
            Assert.AreEqual(0, _state.InventorySlots.Count);
        }

        [Test]
        public void Rage_AndScrambleCooldown_AreRoundValues()
        {
            _bridge.Broadcast(CellarMsgs.EncodeRage(true));
            EventBus.Publish(new PuzzleScrambledEvt(2, new PlayerId(2), 40d));
            EventBus.Publish(new PuzzleScrambledEvt(3, new PlayerId(3), 30d));

            Assert.IsTrue(_state.Rage);
            Assert.AreEqual(40d, _state.ScrambleCooldownUntil, "the latest end wins");

            EventBus.Publish(new RoundStartedEvt(1, 5));
            Assert.IsFalse(_state.Rage);
            Assert.AreEqual(double.MinValue, _state.ScrambleCooldownUntil);
        }

        [Test]
        public void Focus_OnlyForTheLocalPlayer()
        {
            EventBus.Publish(new ReferenceFocusChangedEvt(new PlayerId(3), true));
            Assert.IsFalse(_state.LocalFocusing);
            EventBus.Publish(new ReferenceFocusChangedEvt(new PlayerId(1), true));
            Assert.IsTrue(_state.LocalFocusing);
        }

        [Test]
        public void PuzzlesSolved_CountsDistinctSlots()
        {
            EventBus.Publish(new PuzzleCompletedEvt(0, 1));
            EventBus.Publish(new PuzzleCompletedEvt(0, 1));
            EventBus.Publish(new PuzzleCompletedEvt(4, 2));
            Assert.AreEqual(2, _state.PuzzlesSolved);
        }

        [Test]
        public void TimeBonus_RemembersWhenAndHowMuch()
        {
            EventBus.Publish(new RoundTimeAddedEvt(90f, 500d));
            Assert.AreEqual(10d, _state.TimeBonusAt);
            Assert.AreEqual(90f, _state.TimeBonusSeconds);
        }

        [Test]
        public void RoundReport_SumsDeltas_IgnoresResets()
        {
            EventBus.Publish(new ScoreChangedEvt(new PlayerId(0), 50, 50, ScoreReason.Puzzle));
            EventBus.Publish(new ScoreChangedEvt(new PlayerId(0), 242, 192, ScoreReason.TreasureWin));
            EventBus.Publish(new ScoreChangedEvt(new PlayerId(2), 0, -100, ScoreReason.Reset));
            EventBus.Publish(new RoundEndedEvt(RoundResult.SeekersWin, RoundEndReason.TreasureDug));

            Assert.AreEqual(242, _state.RoundDeltaOf(new PlayerId(0)));
            Assert.AreEqual(0, _state.RoundDeltaOf(new PlayerId(2)));
            Assert.AreEqual(RoundResult.SeekersWin, _state.LastResult);
            Assert.AreEqual(RoundEndReason.TreasureDug, _state.LastReason);

            EventBus.Publish(new RoundStartedEvt(1, 5));
            Assert.AreEqual(0, _state.RoundDeltaOf(new PlayerId(0)));
        }

        [Test]
        public void MatchEnd_IsKeptUntilANewMatch()
        {
            EventBus.Publish(new MatchEndedEvt(new PlayerId(2), 610));
            Assert.IsTrue(_state.MatchEnded);
            Assert.AreEqual(new PlayerId(2), _state.Winner);

            EventBus.Publish(new RoundStartedEvt(0, 9));
            Assert.IsFalse(_state.MatchEnded);
        }

        [Test]
        public void PhaseOf_FollowsPossession()
        {
            EventBus.Publish(new PossessionChangedEvt(new PlayerId(2), new NetId(40001), PossessionPhase.Charging));
            Assert.AreEqual(PossessionPhase.Charging, _state.PhaseOf(new PlayerId(2)));
            Assert.AreEqual(PossessionPhase.Free, _state.PhaseOf(new PlayerId(200)));
        }

        // ---------- HudFormat ----------

        [Test]
        public void Format_ClockHeartsBar()
        {
            Assert.AreEqual("6:00", HudFormat.Clock(360));
            Assert.AreEqual("0:09", HudFormat.Clock(9));
            Assert.AreEqual("0:00", HudFormat.Clock(-4));
            Assert.AreEqual("♥♥♡", HudFormat.Hearts(2, 3));
            Assert.AreEqual(5, HudFormat.BarCells(0.55f, 10));
            Assert.AreEqual(0, HudFormat.BarCells(float.NaN, 10));
            Assert.AreEqual(10, HudFormat.Bar(2f, 10).Length);
        }

        [Test]
        public void Format_ArrowPointsTowardTheTarget()
        {
            Assert.AreEqual('↑', HudFormat.Arrow(0f));
            Assert.AreEqual('→', HudFormat.Arrow(90f));
            Assert.AreEqual('←', HudFormat.Arrow(-90f));
            Assert.AreEqual('↓', HudFormat.Arrow(180f));
            Assert.AreEqual('↗', HudFormat.Arrow(40f));
        }

        [Test]
        public void Format_KeysExistInTheLocTable()
        {
            string[] keys =
            {
                HudFormat.RoleKey(Role.Human), HudFormat.RoleKey(Role.None), HudFormat.ResultKey(RoundResult.JinnsWin),
                HudFormat.ReasonKey(RoundEndReason.Timeout), "hud.sunrise", "hud.stage", "hud.energy", "hud.rage", "hud.match_end",
            };
            foreach (string key in keys)
                Assert.IsTrue(Loc.Has(key), key);
        }
    }
}
