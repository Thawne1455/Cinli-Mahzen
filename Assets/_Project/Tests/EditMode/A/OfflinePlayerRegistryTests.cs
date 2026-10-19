using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using CinliMahzen.Core;
using CinliMahzen.Core.Events;
using CinliMahzen.Core.Net;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CinliMahzen.Tests.EditMode.A
{
    /// <summary>
    /// TODO 1.3: the hotseat player registry (four slots, local player, role assignment) and the network bridge that reads its
    /// local player from it. Plain C#; the EventBus is cleared around every test.
    /// </summary>
    public class OfflinePlayerRegistryTests
    {
        private static readonly Role[] Round0 = { Role.Human, Role.GoodJinn, Role.EvilJinn, Role.EvilJinn };
        // Round 1 of the rotation: the human moves on to P1, P0 becomes an Evil Jinn.
        private static readonly Role[] Round1 = { Role.EvilJinn, Role.Human, Role.GoodJinn, Role.EvilJinn };

        private readonly List<Role> _localRoleEvents = new List<Role>();
        private OfflinePlayerRegistry _reg;

        [SetUp]
        public void SetUp()
        {
            GameServices.ResetToStubs();
            EventBus.ClearAll();
            _localRoleEvents.Clear();
            EventBus.Subscribe<LocalRoleChangedEvt>(OnLocalRoleChanged);
            _reg = new OfflinePlayerRegistry();
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Unsubscribe<LocalRoleChangedEvt>(OnLocalRoleChanged);
            EventBus.ClearAll();
            GameServices.ResetToStubs();
        }

        private void OnLocalRoleChanged(LocalRoleChangedEvt e) => _localRoleEvents.Add(e.Role);

        // --- construction ---

        [Test]
        public void New_HasFourPlayers_P0ToP3_WithoutRoles_LocalIsP0()
        {
            Assert.AreEqual(4, _reg.Players.Count);
            for (int i = 0; i < 4; i++)
            {
                PlayerInfo p = _reg.Players[i];
                Assert.AreEqual(new PlayerId((byte)i), p.Id);
                Assert.AreEqual("P" + (i + 1), p.Nickname);
                Assert.AreEqual(Role.None, p.Role);
                Assert.AreEqual(0, p.Score);
            }
            Assert.AreEqual(new PlayerId(0), _reg.LocalPlayer);
        }

        [Test]
        public void New_WithBadPlayerCount_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new OfflinePlayerRegistry(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new OfflinePlayerRegistry(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new OfflinePlayerRegistry(255));
        }

        [Test]
        public void Get_And_GetRole_ResolveKnownPlayers_AndRejectUnknownOnes()
        {
            Assert.IsTrue(_reg.SetRoles(Round0));

            Assert.AreSame(_reg.Players[2], _reg.Get(new PlayerId(2)));
            Assert.AreEqual(Role.EvilJinn, _reg.GetRole(new PlayerId(2)));
            Assert.AreEqual(Role.Human, _reg.GetRole(new PlayerId(0)));

            Assert.IsNull(_reg.Get(PlayerId.None));
            Assert.IsNull(_reg.Get(new PlayerId(4)));
            Assert.AreEqual(Role.None, _reg.GetRole(PlayerId.None));
            Assert.AreEqual(Role.None, _reg.GetRole(new PlayerId(4)));
        }

        // --- SetLocalPlayer ---

        [Test]
        public void SetLocalPlayer_UpdatesLocalPlayer_BeforeRaisingTheEvent()
        {
            PlayerId seenAtEvent = PlayerId.None;
            PlayerId argument = PlayerId.None;
            int calls = 0;
            _reg.LocalPlayerChanged += id =>
            {
                calls++;
                argument = id;
                seenAtEvent = _reg.LocalPlayer;
            };

            Assert.IsTrue(_reg.SetLocalPlayer(new PlayerId(2)));

            Assert.AreEqual(1, calls);
            Assert.AreEqual(new PlayerId(2), argument);
            Assert.AreEqual(new PlayerId(2), seenAtEvent, "subscribers must already see the new local player");
            Assert.AreEqual(new PlayerId(2), _reg.LocalPlayer);
        }

        [Test]
        public void SetLocalPlayer_PublishesLocalRoleChanged_OnlyWhenTheLocalRoleDiffers()
        {
            _reg.SetRoles(Round0);
            _localRoleEvents.Clear();   // P0 None -> Human is not what this test is about

            _reg.SetLocalPlayer(new PlayerId(1));   // Human -> GoodJinn
            _reg.SetLocalPlayer(new PlayerId(2));   // GoodJinn -> EvilJinn
            _reg.SetLocalPlayer(new PlayerId(3));   // EvilJinn -> EvilJinn: same role, no event
            _reg.SetLocalPlayer(new PlayerId(0));   // EvilJinn -> Human

            CollectionAssert.AreEqual(new[] { Role.GoodJinn, Role.EvilJinn, Role.Human }, _localRoleEvents);
        }

        [Test]
        public void SetLocalPlayer_Current_RaisesNothing_AndSucceeds()
        {
            _reg.SetRoles(Round0);
            _localRoleEvents.Clear();
            int calls = 0;
            _reg.LocalPlayerChanged += _ => calls++;

            Assert.IsTrue(_reg.SetLocalPlayer(new PlayerId(0)));

            Assert.AreEqual(0, calls);
            Assert.AreEqual(0, _localRoleEvents.Count);
        }

        [Test]
        public void SetLocalPlayer_UnknownPlayer_IsRejected_AndChangesNothing()
        {
            int calls = 0;
            _reg.LocalPlayerChanged += _ => calls++;

            Assert.IsFalse(_reg.SetLocalPlayer(PlayerId.None));
            Assert.IsFalse(_reg.SetLocalPlayer(new PlayerId(4)));
            Assert.IsFalse(_reg.SetLocalPlayer(new PlayerId(200)));

            Assert.AreEqual(new PlayerId(0), _reg.LocalPlayer);
            Assert.AreEqual(0, calls);
            Assert.AreEqual(0, _localRoleEvents.Count);
        }

        [Test]
        public void LocalPlayerChanged_ThrowingSubscriber_DoesNotStopTheOthers()
        {
            int secondCalls = 0;
            _reg.LocalPlayerChanged += _ => throw new InvalidOperationException("boom");
            _reg.LocalPlayerChanged += _ => secondCalls++;

            LogAssert.Expect(LogType.Exception, new Regex("boom"));
            Assert.IsTrue(_reg.SetLocalPlayer(new PlayerId(1)));

            Assert.AreEqual(1, secondCalls);
            Assert.AreEqual(new PlayerId(1), _reg.LocalPlayer);
        }

        // --- SetRoles ---

        [Test]
        public void SetRoles_AssignsRoles_AndRaisesRolesChangedOnce()
        {
            int calls = 0;
            _reg.RolesChanged += () => calls++;

            Assert.IsTrue(_reg.SetRoles(Round0));

            Assert.AreEqual(1, calls);
            for (int i = 0; i < 4; i++)
                Assert.AreEqual(Round0[i], _reg.Players[i].Role, "slot " + i);
        }

        [Test]
        public void SetRoles_PublishesLocalRoleChanged_OnlyWhenTheLocalRoleChanged()
        {
            _reg.SetRoles(Round0);                       // local P0: None -> Human
            CollectionAssert.AreEqual(new[] { Role.Human }, _localRoleEvents);

            _reg.SetRoles(new[] { Role.Human, Role.EvilJinn, Role.GoodJinn, Role.EvilJinn });   // P0 is still the Human
            CollectionAssert.AreEqual(new[] { Role.Human }, _localRoleEvents, "no new event when the local role is unchanged");

            _reg.SetRoles(Round1);                       // local P0: Human -> EvilJinn
            CollectionAssert.AreEqual(new[] { Role.Human, Role.EvilJinn }, _localRoleEvents);
        }

        [Test]
        public void SetRoles_RaisesRolesChanged_EvenWhenNothingChanged()
        {
            int calls = 0;
            _reg.RolesChanged += () => calls++;

            _reg.SetRoles(Round0);
            _reg.SetRoles(Round0);

            Assert.AreEqual(2, calls, "a new round re-spawns the pawns even when the roles repeat");
        }

        [Test]
        public void SetRoles_FollowsTheCurrentLocalPlayer()
        {
            _reg.SetLocalPlayer(new PlayerId(2));
            _localRoleEvents.Clear();

            _reg.SetRoles(Round0);   // P2 becomes an Evil Jinn
            _reg.SetRoles(Round1);   // P2 becomes the Good Jinn

            CollectionAssert.AreEqual(new[] { Role.EvilJinn, Role.GoodJinn }, _localRoleEvents);
        }

        [Test]
        public void SetRoles_InvalidInput_IsRejected_AndChangesNothing()
        {
            _reg.SetRoles(Round0);
            _localRoleEvents.Clear();
            int calls = 0;
            _reg.RolesChanged += () => calls++;

            Assert.IsFalse(_reg.SetRoles(null));
            Assert.IsFalse(_reg.SetRoles(new[] { Role.Human, Role.GoodJinn }), "wrong length");
            Assert.IsFalse(_reg.SetRoles(new[] { Role.Human, Role.GoodJinn, Role.EvilJinn, Role.EvilJinn, Role.EvilJinn }), "wrong length");
            Assert.IsFalse(_reg.SetRoles(new[] { Role.Human, Role.GoodJinn, Role.EvilJinn, (Role)9 }), "undefined role");

            Assert.AreEqual(0, calls);
            Assert.AreEqual(0, _localRoleEvents.Count);
            for (int i = 0; i < 4; i++)
                Assert.AreEqual(Round0[i], _reg.Players[i].Role, "slot " + i);
        }

        // --- OfflineNetBridge reads the local player from the registry ---

        [Test]
        public void NetBridge_WithRegistryFunc_FollowsTheLocalPlayer()
        {
            // This is how GameBootstrap wires it.
            var net = new OfflineNetBridge(() => 0d, () => _reg.LocalPlayer);
            Assert.AreEqual(new PlayerId(0), net.LocalPlayer);

            _reg.SetLocalPlayer(new PlayerId(3));

            Assert.AreEqual(new PlayerId(3), net.LocalPlayer);
        }

        [Test]
        public void NetBridge_Default_ReadsTheRegisteredRegistry()
        {
            GameServices.Register<IPlayerRegistry>(_reg);
            var net = new OfflineNetBridge(() => 0d);

            _reg.SetLocalPlayer(new PlayerId(2));

            Assert.AreEqual(new PlayerId(2), net.LocalPlayer);
        }

        [Test]
        public void NetBridge_StampsMessagesWithTheCurrentLocalPlayer()
        {
            var net = new OfflineNetBridge(() => 1d, () => _reg.LocalPlayer);
            var senders = new List<PlayerId>();
            net.Register(MsgCode.ReqInteract, m => senders.Add(m.Sender));

            net.SendToAuthority(new NetMsg { Code = MsgCode.ReqInteract });
            _reg.SetLocalPlayer(new PlayerId(1));
            net.SendToAuthority(new NetMsg { Code = MsgCode.ReqInteract });

            CollectionAssert.AreEqual(new[] { new PlayerId(0), new PlayerId(1) }, senders);
        }
    }
}
