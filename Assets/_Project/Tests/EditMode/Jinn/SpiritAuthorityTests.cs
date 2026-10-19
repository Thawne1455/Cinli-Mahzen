using System;
using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.Core.Events;
using CinliMahzen.Core.Net;
using CinliMahzen.Jinn;
using NUnit.Framework;
using UnityEngine;

namespace CinliMahzen.Tests.EditMode.Jinn
{
    /// <summary>
    /// The authority loop of the reference focus and the Spirit Bump (Teknik §4.3, §6.1) on an <see cref="OfflineNetBridge"/>:
    /// Request → SpiritAuthority validates → Broadcast. Hotseat roles of round 0: P0 Human, P1 Good Jinn, P2 and P3 Evil Jinn.
    /// </summary>
    public class SpiritAuthorityTests
    {
        private static readonly PlayerId Human = new PlayerId(0);
        private static readonly PlayerId Good = new PlayerId(1);
        private static readonly PlayerId Evil = new PlayerId(2);
        private static readonly PlayerId OtherEvil = new PlayerId(3);

        // Oyun §14: SpiritBumpRange 1.5, NetRangeTolerance 0.5, SpiritBumpCooldown 6.
        private static readonly SpiritBumpSettings Settings = new SpiritBumpSettings(1.5f, 0.5f, 6f);

        private double _now;
        private PlayerId _local;
        private OfflineNetBridge _net;
        private OfflinePlayerRegistry _players;
        private Dictionary<byte, Vector3> _positions;
        private SpiritAuthority _authority;
        private List<NetMsg> _broadcasts;

        [SetUp]
        public void SetUp()
        {
            EventBus.ClearAll();

            _now = 100d;
            _local = Human;
            _net = new OfflineNetBridge(() => _now, () => _local);
            _players = new OfflinePlayerRegistry();
            _players.SetRoles(new[] { Role.Human, Role.GoodJinn, Role.EvilJinn, Role.EvilJinn });

            _positions = new Dictionary<byte, Vector3>
            {
                { Good.Value, new Vector3(5f, 2f, 5f) },
                { Evil.Value, new Vector3(6f, 2f, 5f) },
                { OtherEvil.Value, new Vector3(-9f, 2f, 5f) },
            };

            _broadcasts = new List<NetMsg>();
            _net.Register(MsgCode.ReferenceFocusChanged, m => _broadcasts.Add(m));
            _net.Register(MsgCode.SpiritBumped, m => _broadcasts.Add(m));

            _authority = new SpiritAuthority(_net, _players, Settings, PositionOf);
            _authority.Attach();
        }

        [TearDown]
        public void TearDown()
        {
            _authority.Detach();
            EventBus.ClearAll();
        }

        private Vector3? PositionOf(PlayerId id)
        {
            return _positions.TryGetValue(id.Value, out Vector3 p) ? p : (Vector3?)null;
        }

        private void SendAs(PlayerId sender, NetMsg msg)
        {
            _local = sender;
            _net.SendToAuthority(msg);
        }

        private void Focus(bool on)
        {
            SendAs(Good, ReqFocusReferenceMsg.Encode(on));
        }

        private void RequestBump(PlayerId evil, PlayerId good)
        {
            SendAs(evil, ReqSpiritBumpMsg.Encode(good));
        }

        private void AssertFocusChanged(NetMsg msg, PlayerId expectedPlayer, bool expectedFocusing)
        {
            Assert.IsTrue(ReferenceFocusChangedMsg.TryDecode(msg, out PlayerId player, out bool focusing), "not a ReferenceFocusChanged: " + msg.Code);
            Assert.AreEqual(expectedPlayer, player);
            Assert.AreEqual(expectedFocusing, focusing);
        }

        // ---- Focus ----

        [Test]
        public void Focus_FromTheGoodJinn_IsBroadcastOnce()
        {
            Focus(true);

            Assert.AreEqual(1, _broadcasts.Count);
            AssertFocusChanged(_broadcasts[0], Good, true);
            Assert.IsTrue(_authority.Focus.IsFocusing(Good));
        }

        [Test]
        public void Focus_RequestedAgain_BroadcastsNothingNew()
        {
            Focus(true);
            Focus(true);

            Assert.AreEqual(1, _broadcasts.Count);
        }

        [Test]
        public void Focus_Released_IsBroadcast()
        {
            Focus(true);
            Focus(false);

            Assert.AreEqual(2, _broadcasts.Count);
            AssertFocusChanged(_broadcasts[1], Good, false);
            Assert.IsFalse(_authority.Focus.IsFocusing(Good));
        }

        [Test]
        public void Focus_ReleasedWhenNeverStarted_BroadcastsNothing()
        {
            Focus(false);

            Assert.AreEqual(0, _broadcasts.Count);
        }

        [Test]
        public void Focus_FromAnyoneButTheGoodJinn_IsIgnored()
        {
            SendAs(Evil, ReqFocusReferenceMsg.Encode(true));
            SendAs(OtherEvil, ReqFocusReferenceMsg.Encode(true));
            SendAs(Human, ReqFocusReferenceMsg.Encode(true));

            Assert.AreEqual(0, _broadcasts.Count);
            Assert.AreEqual(0, _authority.Focus.Count);
        }

        [Test]
        public void Focus_OnANonAuthorityClient_IsIgnored()
        {
            _net.IsAuthority = false;

            Focus(true);

            Assert.AreEqual(0, _broadcasts.Count);
            Assert.AreEqual(0, _authority.Focus.Count);
        }

        [Test]
        public void Focus_MalformedRequest_IsIgnored()
        {
            var malformed = new NetMsg { Code = MsgCode.ReqFocusReference, Payload = new object[] { 1 } };

            SendAs(Good, malformed);

            Assert.AreEqual(0, _broadcasts.Count);
            Assert.AreEqual(0, _authority.Focus.Count);
        }

        [Test]
        public void ProcessFocusRequestAuthority_ReportsWhetherTheFlagChanged()
        {
            Assert.IsTrue(_authority.ProcessFocusRequestAuthority(Good, true));
            Assert.IsFalse(_authority.ProcessFocusRequestAuthority(Good, true), "no change");
            Assert.IsFalse(_authority.ProcessFocusRequestAuthority(Evil, true), "wrong role");
            Assert.IsTrue(_authority.ProcessFocusRequestAuthority(Good, false));
        }

        // ---- Spirit Bump ----

        [Test]
        public void Bump_OnAFocusingGoodJinnInRange_CutsTheFocusAndBroadcastsBoth_InOrder()
        {
            Focus(true);
            _broadcasts.Clear();

            RequestBump(Evil, Good);

            Assert.AreEqual(2, _broadcasts.Count);
            AssertFocusChanged(_broadcasts[0], Good, false);
            Assert.AreEqual(MsgCode.SpiritBumped, _broadcasts[1].Code);
            Assert.IsTrue(SpiritBumpedMsg.TryDecode(_broadcasts[1], out PlayerId evil, out PlayerId good));
            Assert.AreEqual(Evil, evil);
            Assert.AreEqual(Good, good);
            Assert.IsFalse(_authority.Focus.IsFocusing(Good));
        }

        [Test]
        public void Bump_StartsTheRequestersCooldown()
        {
            Focus(true);

            RequestBump(Evil, Good);

            Assert.AreEqual(100d + Settings.Cooldown, _authority.GetBumpCooldownUntil(Evil), 1e-9);
            Assert.AreEqual(0d, _authority.GetBumpCooldownUntil(OtherEvil), "the other evil jinn is not on cooldown");
        }

        [Test]
        public void Bump_PublishesNothingOnTheEventBus_ThatIsTheClientsJob()
        {
            // The authority only broadcasts; SpiritBumpedEvt / ReferenceFocusChangedEvt come from each machine's controller.
            int events = 0;
            EventBus.Subscribe<SpiritBumpedEvt>(e => events++);
            EventBus.Subscribe<ReferenceFocusChangedEvt>(e => events++);
            Focus(true);

            RequestBump(Evil, Good);

            Assert.AreEqual(0, events);
        }

        [Test]
        public void Bump_WhenTheGoodJinnIsNotFocusing_IsRejected()
        {
            RequestBump(Evil, Good);

            Assert.AreEqual(0, _broadcasts.Count);
            Assert.AreEqual(0d, _authority.GetBumpCooldownUntil(Evil), "a rejected request starts no cooldown");
        }

        [Test]
        public void Bump_OutOfRange_IsRejected_ButTheNetworkToleranceIsGranted()
        {
            Focus(true);
            _broadcasts.Clear();

            _positions[Evil.Value] = _positions[Good.Value] + new Vector3(2.1f, 0f, 0f); // > 1.5 + 0.5
            RequestBump(Evil, Good);
            Assert.AreEqual(0, _broadcasts.Count, "2.1 m is beyond range + tolerance");

            _positions[Evil.Value] = _positions[Good.Value] + new Vector3(1.8f, 0f, 0f); // > 1.5 but within the tolerance
            RequestBump(Evil, Good);
            Assert.AreEqual(2, _broadcasts.Count, "1.8 m is inside range + tolerance");
        }

        [Test]
        public void Bump_FromARequesterWhoIsNotAnEvilJinn_IsRejected()
        {
            Focus(true);
            _broadcasts.Clear();

            RequestBump(Human, Good);
            RequestBump(Good, Good);

            Assert.AreEqual(0, _broadcasts.Count);
            Assert.IsTrue(_authority.Focus.IsFocusing(Good), "the focus survives");
        }

        [Test]
        public void Bump_AgainDuringTheCooldown_IsRejected_AndAllowedOnceItIsOver()
        {
            Focus(true);
            RequestBump(Evil, Good);
            Focus(true);
            _broadcasts.Clear();

            _now = 105.9d;
            RequestBump(Evil, Good);
            Assert.AreEqual(0, _broadcasts.Count, "still on cooldown");
            Assert.IsTrue(_authority.Focus.IsFocusing(Good));

            _now = 106d;
            RequestBump(Evil, Good);
            Assert.AreEqual(2, _broadcasts.Count, "cooldown over");
        }

        [Test]
        public void Bump_CooldownIsPerEvilJinn()
        {
            Focus(true);
            RequestBump(Evil, Good);
            Focus(true);
            _positions[OtherEvil.Value] = _positions[Good.Value];
            _broadcasts.Clear();

            RequestBump(OtherEvil, Good);

            Assert.AreEqual(2, _broadcasts.Count);
            Assert.IsTrue(SpiritBumpedMsg.TryDecode(_broadcasts[1], out PlayerId evil, out _));
            Assert.AreEqual(OtherEvil, evil);
        }

        [Test]
        public void Bump_NonAuthorityClient_IsIgnored()
        {
            Focus(true);
            _broadcasts.Clear();
            _net.IsAuthority = false;

            RequestBump(Evil, Good);

            Assert.AreEqual(0, _broadcasts.Count);
            Assert.IsTrue(_authority.Focus.IsFocusing(Good));
        }

        [Test]
        public void Bump_MalformedRequest_IsIgnored()
        {
            Focus(true);
            _broadcasts.Clear();

            SendAs(Evil, new NetMsg { Code = MsgCode.ReqSpiritBump, Payload = new object[] { "P1" } });

            Assert.AreEqual(0, _broadcasts.Count);
        }

        [Test]
        public void ProcessBumpRequestAuthority_ReportsTheReason()
        {
            Assert.AreEqual(SpiritBumpDenyReason.TargetNotFocusing, _authority.ProcessBumpRequestAuthority(Evil, Good));

            _authority.ProcessFocusRequestAuthority(Good, true);
            Assert.AreEqual(SpiritBumpDenyReason.RequesterNotEvil, _authority.ProcessBumpRequestAuthority(Human, Good));
            Assert.AreEqual(SpiritBumpDenyReason.TargetNotGood, _authority.ProcessBumpRequestAuthority(Evil, OtherEvil));

            _positions[Evil.Value] = new Vector3(50f, 2f, 5f);
            Assert.AreEqual(SpiritBumpDenyReason.OutOfRange, _authority.ProcessBumpRequestAuthority(Evil, Good));

            _positions[Evil.Value] = _positions[Good.Value];
            Assert.AreEqual(SpiritBumpDenyReason.None, _authority.ProcessBumpRequestAuthority(Evil, Good));
            Assert.AreEqual(SpiritBumpDenyReason.TargetNotFocusing, _authority.ProcessBumpRequestAuthority(Evil, Good), "the bump cut the focus");
        }

        [Test]
        public void ProcessBumpRequestAuthority_WithoutAPawn_IsNoPawn()
        {
            _authority.ProcessFocusRequestAuthority(Good, true);

            _positions.Remove(Good.Value);
            Assert.AreEqual(SpiritBumpDenyReason.NoPawn, _authority.ProcessBumpRequestAuthority(Evil, Good));

            _positions[Good.Value] = new Vector3(5f, 2f, 5f);
            _positions.Remove(Evil.Value);
            Assert.AreEqual(SpiritBumpDenyReason.NoPawn, _authority.ProcessBumpRequestAuthority(Evil, Good));
        }

        [Test]
        public void ProcessBumpRequestAuthority_ChecksTheRolesBeforeLookingForPawns()
        {
            _positions.Clear();

            Assert.AreEqual(SpiritBumpDenyReason.RequesterNotEvil, _authority.ProcessBumpRequestAuthority(Human, Good));
        }

        // ---- New round ----

        [Test]
        public void NewRound_ClearsTheFocusAndBroadcastsIt()
        {
            Focus(true);
            _broadcasts.Clear();

            EventBus.Publish(new RoundStartedEvt(1, 1234));

            Assert.AreEqual(1, _broadcasts.Count);
            AssertFocusChanged(_broadcasts[0], Good, false);
            Assert.AreEqual(0, _authority.Focus.Count);
        }

        [Test]
        public void NewRound_ClearsTheCooldowns()
        {
            Focus(true);
            RequestBump(Evil, Good);

            EventBus.Publish(new RoundStartedEvt(1, 1234));

            Assert.AreEqual(0d, _authority.GetBumpCooldownUntil(Evil));
        }

        [Test]
        public void NewRoles_ClearTheFocusAndTheCooldowns()
        {
            Focus(true);
            RequestBump(Evil, Good);
            Focus(true);
            _broadcasts.Clear();

            _players.SetRoles(new[] { Role.GoodJinn, Role.Human, Role.EvilJinn, Role.EvilJinn });

            Assert.AreEqual(1, _broadcasts.Count);
            AssertFocusChanged(_broadcasts[0], Good, false);
            Assert.AreEqual(0, _authority.Focus.Count);
            Assert.AreEqual(0d, _authority.GetBumpCooldownUntil(Evil));
        }

        [Test]
        public void NewRound_WithNobodyFocusing_BroadcastsNothing()
        {
            EventBus.Publish(new RoundStartedEvt(1, 1234));

            Assert.AreEqual(0, _broadcasts.Count);
        }

        // ---- Attach / Detach ----

        [Test]
        public void Detach_StopsHandlingRequests()
        {
            _authority.Detach();

            Focus(true);

            Assert.AreEqual(0, _broadcasts.Count);
            Assert.AreEqual(0, _authority.Focus.Count);
        }

        [Test]
        public void Attach_Twice_DoesNotDoubleHandle()
        {
            _authority.Attach();

            Focus(true);

            Assert.AreEqual(1, _broadcasts.Count);
        }

        [Test]
        public void Constructor_RejectsMissingDependencies()
        {
            Assert.Throws<ArgumentNullException>(() => new SpiritAuthority(null, _players, Settings, PositionOf));
            Assert.Throws<ArgumentNullException>(() => new SpiritAuthority(_net, null, Settings, PositionOf));
            Assert.Throws<ArgumentNullException>(() => new SpiritAuthority(_net, _players, Settings, null));
        }
    }
}
