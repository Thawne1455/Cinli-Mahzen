using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.Core.Events;
using CinliMahzen.Core.Net;
using CinliMahzen.Match;
using CinliMahzen.Objectives;
using CinliMahzen.Possession;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CinliMahzen.Tests.EditMode.Match
{
    /// <summary>TODO 4.3: match statistics from replicated events, and the match-end titles.</summary>
    public class StatsServiceTests
    {
        private const string Folder = "Assets/_Project/ScriptableObjects/Possessables/";

        private OfflineNetBridge _bridge;
        private OfflinePlayerRegistry _players;
        private StatsService _stats;

        private static readonly PlayerId P0 = new PlayerId(0);
        private static readonly PlayerId P1 = new PlayerId(1);
        private static readonly PlayerId P2 = new PlayerId(2);
        private static readonly PlayerId P3 = new PlayerId(3);

        [SetUp]
        public void SetUp()
        {
            EventBus.ClearAll();
            _bridge = new OfflineNetBridge(() => 0d, () => P0);
            _players = new OfflinePlayerRegistry();
            _players.SetRoles(new[] { Role.Human, Role.GoodJinn, Role.EvilJinn, Role.EvilJinn });
            _stats = new StatsService(_bridge, _players);
            _stats.Attach();
        }

        [TearDown]
        public void TearDown()
        {
            _stats.Detach();
            EventBus.ClearAll();
        }

        private static HumanDamagedEvt Damage(string cause, PlayerId attacker)
        {
            var info = new DamageInfo { Amount = 1, Attacker = attacker, CauseId = cause };
            return new HumanDamagedEvt(info, 2);
        }

        [Test]
        public void EveryStat_IsCountedFromItsEvent()
        {
            EventBus.Publish(Damage(StatsService.ConfirmFailCauseId, PlayerId.None));
            EventBus.Publish(Damage(StatsService.SlipperCauseId, P2));
            EventBus.Publish(Damage("chair.lunge", P3));
            EventBus.Publish(new PuzzleScrambledEvt(1, P3, 20d));
            EventBus.Publish(new ActionResolvedEvt(new NetId(40001), 0, false, P2, StatsService.BurstActionId));
            EventBus.Publish(new ActionResolvedEvt(new NetId(40002), 0, true, P3, "throw"));
            EventBus.Publish(new PingPlacedEvt(P1, Vector3.zero));
            EventBus.Publish(new SpiritBlockedEvt(P1, new NetId(60001)));
            _bridge.Broadcast(HumanActionMsgs.EncodeKickResult(P0, NetId.Invalid));
            _bridge.Broadcast(HumanActionMsgs.EncodeKickResult(P0, new NetId(40001)));
            _bridge.Broadcast(CologneReviveTriggeredMsg.Encode(P1));

            Assert.AreEqual(1, _stats.RoundCount(P0, StatKind.WrongConfirm), "the round's human");
            Assert.AreEqual(1, _stats.RoundCount(P2, StatKind.SlipperHit));
            Assert.AreEqual(0, _stats.RoundCount(P3, StatKind.SlipperHit));
            Assert.AreEqual(1, _stats.RoundCount(P3, StatKind.Scramble));
            Assert.AreEqual(1, _stats.RoundCount(P2, StatKind.LampBurst));
            Assert.AreEqual(0, _stats.RoundCount(P3, StatKind.LampBurst));
            Assert.AreEqual(1, _stats.RoundCount(P1, StatKind.Ping));
            Assert.AreEqual(2, _stats.RoundCount(P1, StatKind.Rescue), "cologne + spirit block");
            Assert.AreEqual(1, _stats.RoundCount(P0, StatKind.MissedKick), "only the kick that hit nothing");
        }

        [Test]
        public void RoundCounts_Reset_MatchCountsStay_UntilANewMatch()
        {
            EventBus.Publish(new PuzzleScrambledEvt(1, P3, 20d));
            EventBus.Publish(new RoundStartedEvt(1, 4));
            EventBus.Publish(new PuzzleScrambledEvt(2, P3, 40d));

            Assert.AreEqual(1, _stats.RoundCount(P3, StatKind.Scramble));
            Assert.AreEqual(2, _stats.MatchCount(P3, StatKind.Scramble));

            EventBus.Publish(new RoundStartedEvt(0, 9));
            Assert.AreEqual(0, _stats.MatchCount(P3, StatKind.Scramble));
        }

        [Test]
        public void Titles_GoToTheMost_SharedOnATie_NoneAtZero()
        {
            _stats.Count(P2, StatKind.Scramble);
            _stats.Count(P3, StatKind.Scramble);
            _stats.Count(P3, StatKind.Scramble);
            _stats.Count(P1, StatKind.Ping);
            _stats.Count(P0, StatKind.MissedKick);
            _stats.Count(P2, StatKind.MissedKick);

            _stats.ComputeTitles();
            IReadOnlyList<MatchTitle> titles = _stats.Titles;

            Assert.AreEqual(4, titles.Count);
            Assert.AreEqual(TitleKind.Saboteur, titles[0].Kind);
            Assert.AreEqual(P3, titles[0].Player);
            Assert.AreEqual(2, titles[0].Count);
            Assert.AreEqual(TitleKind.Navigator, titles[1].Kind);
            Assert.AreEqual(TitleKind.Kicker, titles[2].Kind);
            Assert.AreEqual(TitleKind.Kicker, titles[3].Kind, "tied kickers share it");
        }

        [Test]
        public void EveryTitle_HasItsOwnStat()
        {
            var seen = new HashSet<StatKind>();
            for (int t = 0; t <= (int)TitleKind.Cologne; t++)
                Assert.IsTrue(seen.Add(TitleCalculator.StatOf((TitleKind)t)), ((TitleKind)t).ToString());
        }

        [Test]
        public void Ids_MatchTheGameData()
        {
            Assert.AreEqual(PuzzleSystemCore.ConfirmFailCauseId, StatsService.ConfirmFailCauseId);

            var slipper = AssetDatabase.LoadAssetAtPath<PossessableDefinition>(Folder + "PD_Slipper.asset");
            Assert.IsNotNull(slipper);
            Assert.AreEqual(StatsService.SlipperCauseId, slipper.GetAction(0).CauseId);

            var lamp = AssetDatabase.LoadAssetAtPath<PossessableDefinition>(Folder + "PD_Lamp.asset");
            Assert.IsNotNull(lamp);
            Assert.AreEqual(StatsService.BurstActionId, lamp.GetAction(0).Id);
        }
    }
}
