using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.Core.Events;
using CinliMahzen.Core.Net;
using CinliMahzen.Jinn;
using CinliMahzen.Possession;
using CinliMahzen.Tests.EditMode.B;
using NUnit.Framework;
using UnityEngine;

namespace CinliMahzen.Tests.EditMode.Jinn
{
    /// <summary>
    /// TODO 3.5: the good jinn's abilities that live in plain classes — exorcism (<see cref="PossessionAuthority.ProcessExorciseAuthority"/>),
    /// the JinnStunned broadcasts, pings (<see cref="PingSystem"/>) and the focus energy drain (<see cref="JinnEnergyService"/>).
    /// </summary>
    public class GoodJinnAbilityTests
    {
        private static readonly PlayerId HumanId = new PlayerId(0);
        private static readonly PlayerId GoodId = new PlayerId(1);
        private static readonly PlayerId EvilId = new PlayerId(2);
        private static readonly NetId Chair = new NetId(40000);
        private static readonly NetId Barrel = new NetId(40001);

        private double _now;
        private PlayerId _local;
        private OfflineNetBridge _bridge;
        private readonly List<NetMsg> _sent = new List<NetMsg>();

        [SetUp]
        public void SetUp()
        {
            EventBus.ClearAll();
            GameServices.ResetToStubs();
            _now = 50d;
            _local = HumanId;
            _bridge = new OfflineNetBridge(() => _now, () => _local);
            _sent.Clear();
            foreach (MsgCode code in new[] { MsgCode.JinnStunned, MsgCode.PossessEnded, MsgCode.PingPlaced, MsgCode.ReferenceFocusChanged })
                _bridge.Register(code, _sent.Add);
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.ClearAll();
            GameServices.ResetToStubs();
        }

        private List<NetMsg> Sent(MsgCode code) => _sent.FindAll(m => m.Code == code);

        // ---------- exorcism ----------

        private PossessionAuthority NewPossession(FakePossessionWorld world)
        {
            world.SetRole(HumanId, Role.Human);
            world.SetRole(GoodId, Role.GoodJinn);
            world.SetRole(EvilId, Role.EvilJinn);
            var authority = new PossessionAuthority(_bridge, world, new PossessionRules(3f, 0.5f, 1.2f, 10f), 2f, 2.5f)
            {
                Exorcise = new ExorciseSettings(3.5f, 18f, 4f, 4f),
            };
            authority.Attach();
            authority.Register(Chair, default);
            authority.Register(Barrel, new PossessableTraits(false, true));
            return authority;
        }

        private void EnterFully(PossessionAuthority authority, PlayerId who, NetId obj)
        {
            Assert.AreEqual(PossessDenyReason.None, authority.ProcessPossessAuthority(who, obj));
            _now += 1.2;
            authority.TickAuthority(_now);
            _sent.Clear();
        }

        [Test]
        public void Exorcise_EjectsStunsAndLocksTheEvilJinn_ThenCoolsDown()
        {
            var world = new FakePossessionWorld();
            PossessionAuthority authority = NewPossession(world);
            EnterFully(authority, EvilId, Chair);

            Assert.AreEqual(EvilId, authority.ProcessExorciseAuthority(GoodId, Chair));

            Assert.IsTrue(PossessEndedMsg.TryDecode(Sent(MsgCode.PossessEnded)[0], out _, out _, out PossessEndReason reason, out _));
            Assert.AreEqual(PossessEndReason.Exorcised, reason);
            Assert.IsTrue(JinnStunnedMsg.TryDecode(Sent(MsgCode.JinnStunned)[0], out PlayerId stunned, out double until));
            Assert.AreEqual(EvilId, stunned);
            Assert.AreEqual(_now + 4d, until, 1e-9);
            Assert.AreEqual(PossessDenyReason.Stunned, authority.ProcessPossessAuthority(EvilId, Barrel), "possess lock");

            _now += 4d;
            Assert.AreEqual(PossessDenyReason.None, authority.ProcessPossessAuthority(EvilId, Barrel), "lock over");
            _now += 1.2;
            authority.TickAuthority(_now);
            Assert.AreEqual(PlayerId.None, authority.ProcessExorciseAuthority(GoodId, Barrel), "18 s cooldown");
            authority.Detach();
        }

        [Test]
        public void Exorcise_IsRefused_ForTheWrongRole_AnEmptyObject_OrOutOfRange()
        {
            var world = new FakePossessionWorld();
            PossessionAuthority authority = NewPossession(world);

            Assert.AreEqual(PlayerId.None, authority.ProcessExorciseAuthority(GoodId, Chair), "nobody inside");
            EnterFully(authority, EvilId, Chair);
            Assert.AreEqual(PlayerId.None, authority.ProcessExorciseAuthority(EvilId, Chair), "an evil jinn cannot exorcise");
            Assert.AreEqual(PlayerId.None, authority.ProcessExorciseAuthority(HumanId, Chair));
            world.SetDistance(GoodId, Chair, 5f);
            Assert.AreEqual(PlayerId.None, authority.ProcessExorciseAuthority(GoodId, Chair), "out of range");
            Assert.AreEqual(0, Sent(MsgCode.PossessEnded).Count);
            authority.Detach();
        }

        [Test]
        public void Exorcise_DoesNotTouchTheGoodJinnsOwnPossession()
        {
            var world = new FakePossessionWorld();
            PossessionAuthority authority = NewPossession(world);
            world.SetRole(new PlayerId(3), Role.GoodJinn);
            EnterFully(authority, GoodId, Barrel);

            Assert.AreEqual(PlayerId.None, authority.ProcessExorciseAuthority(new PlayerId(3), Barrel));
            authority.Detach();
        }

        [Test]
        public void Kick_AlsoBroadcastsJinnStunned()
        {
            var world = new FakePossessionWorld();
            PossessionAuthority authority = NewPossession(world);
            EnterFully(authority, EvilId, Chair);

            authority.KickAuthority(Chair, HumanId);

            Assert.IsTrue(JinnStunnedMsg.TryDecode(Sent(MsgCode.JinnStunned)[0], out PlayerId stunned, out double until));
            Assert.AreEqual(EvilId, stunned);
            Assert.AreEqual(_now + 2.5d, until, 1e-9);
            authority.Detach();
        }

        // ---------- pings ----------

        private OfflinePlayerRegistry Players()
        {
            var players = new OfflinePlayerRegistry();
            players.SetRoles(new[] { Role.Human, Role.GoodJinn, Role.EvilJinn, Role.EvilJinn });
            return players;
        }

        [Test]
        public void Ping_OnlyTheGoodJinn_WithCooldown_MaxTwoActive_AndExpires()
        {
            var pings = new PingSystem(_bridge, Players(), 8f, 3f, 2);
            pings.Attach();
            var removed = new List<int>();
            pings.PingRemoved += removed.Add;

            Assert.IsFalse(pings.ProcessPingAuthority(EvilId, Vector3.zero));
            Assert.IsTrue(pings.ProcessPingAuthority(GoodId, Vector3.one));
            Assert.IsFalse(pings.ProcessPingAuthority(GoodId, Vector3.one), "3 s cooldown");
            Assert.AreEqual(1, pings.Active.Count);
            Assert.IsTrue(PingMsg.TryDecodePlaced(Sent(MsgCode.PingPlaced)[0], out PlayerId by, out Vector3 at, out double until));
            Assert.AreEqual(GoodId, by);
            Assert.AreEqual(Vector3.one, at);
            Assert.AreEqual(_now + 8d, until, 1e-9);

            _now += 3d;
            pings.ProcessPingAuthority(GoodId, Vector3.up);
            _now += 3d;
            pings.ProcessPingAuthority(GoodId, Vector3.right);
            Assert.AreEqual(2, pings.Active.Count, "the oldest makes room");
            Assert.AreEqual(1, removed.Count);

            pings.Tick(_now + 100d);
            Assert.AreEqual(0, pings.Active.Count);
            pings.Detach();
        }

        [Test]
        public void Ping_ThroughTheBridge_AndNewRoundClears()
        {
            var pings = new PingSystem(_bridge, Players(), 8f, 3f, 2);
            pings.Attach();
            _local = GoodId;
            _bridge.SendToAuthority(PingMsg.EncodeRequest(new Vector3(1f, 2f, 3f)));
            Assert.AreEqual(1, pings.Active.Count);

            EventBus.Publish(new RoundStartedEvt(1, 1));
            Assert.AreEqual(0, pings.Active.Count);
            pings.Detach();
        }

        // ---------- focus energy ----------

        [Test]
        public void FocusDrain_PaysPerSecond_AndRunningDryRaisesExhausted()
        {
            OfflinePlayerRegistry players = Players();
            var config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            JinnEnergyService energy = JinnEnergyService.FromConfig(_bridge, players, config, 1f, 0.25f);
            energy.Attach();
            var exhausted = new List<PlayerId>();
            energy.DrainExhausted += exhausted.Add;
            float start = energy.Get(GoodId);

            energy.SetDrainAuthority(GoodId, 2f);
            _now += 1d;
            energy.TickAuthority(1f);
            Assert.AreEqual(start - 2f + config.GoodEnergyRegen, energy.Get(GoodId), 1e-3f, "regen minus the focus cost");

            energy.SetDrainAuthority(GoodId, 10000f);
            _now += 1d;
            energy.TickAuthority(1f);
            Assert.AreEqual(0f, energy.Get(GoodId), 1e-3f);
            CollectionAssert.AreEqual(new[] { GoodId }, exhausted);
            Assert.AreEqual(0f, energy.DrainOf(GoodId), "the drain stops with it");

            energy.Detach();
            Object.DestroyImmediate(config);
        }
    }
}
