using CinliMahzen.Core;
using CinliMahzen.Core.Events;
using CinliMahzen.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CinliMahzen.Tests.EditMode.A
{
    public class LocalInputSourceTests
    {
        private static readonly PlayerId Local = new PlayerId(2);
        private LocalInputSource _input;

        [SetUp]
        public void SetUp()
        {
            EventBus.ClearAll();
            _input = new LocalInputSource(() => Local);
        }

        [TearDown]
        public void TearDown()
        {
            _input.Dispose();
            EventBus.ClearAll();
        }

        [Test]
        public void Starts_WithNoGameplayMap_UiAndDebugOn()
        {
            Assert.AreEqual(InputMode.None, _input.Mode);
            Assert.IsFalse(_input.Actions.Human.enabled);
            Assert.IsFalse(_input.Actions.Spirit.enabled);
            Assert.IsFalse(_input.Actions.Possessed.enabled);
            Assert.IsTrue(_input.Actions.UI.enabled);
            Assert.IsTrue(_input.Actions.Debug.enabled);
        }

        [Test]
        public void RoleChange_SwitchesMap_AndLogs()
        {
            LogAssert.Expect(LogType.Log, "[Input] Map -> Human");
            EventBus.Publish(new LocalRoleChangedEvt(Role.Human));
            Assert.AreEqual(InputMode.Human, _input.Mode);
            Assert.IsTrue(_input.Actions.Human.enabled);

            LogAssert.Expect(LogType.Log, "[Input] Map -> Spirit");
            EventBus.Publish(new LocalRoleChangedEvt(Role.GoodJinn));
            Assert.AreEqual(InputMode.Spirit, _input.Mode);
            Assert.IsFalse(_input.Actions.Human.enabled);
            Assert.IsTrue(_input.Actions.Spirit.enabled);
        }

        [Test]
        public void LocalEvilJinn_Possession_SwitchesToPossessedAndBack()
        {
            EventBus.Publish(new LocalRoleChangedEvt(Role.EvilJinn));
            Assert.AreEqual(InputMode.Spirit, _input.Mode);

            EventBus.Publish(new PossessionChangedEvt(Local, new NetId(1000), PossessionPhase.Entering));
            Assert.AreEqual(InputMode.Spirit, _input.Mode, "still flying while entering");

            EventBus.Publish(new PossessionChangedEvt(Local, new NetId(1000), PossessionPhase.Lurking));
            Assert.AreEqual(InputMode.Possessed, _input.Mode);
            Assert.IsTrue(_input.Actions.Possessed.enabled);
            Assert.IsFalse(_input.Actions.Spirit.enabled);

            EventBus.Publish(new PossessionChangedEvt(Local, new NetId(1000), PossessionPhase.Free));
            Assert.AreEqual(InputMode.Spirit, _input.Mode);
        }

        [Test]
        public void OtherPlayersPossession_IsIgnored()
        {
            EventBus.Publish(new LocalRoleChangedEvt(Role.EvilJinn));
            EventBus.Publish(new PossessionChangedEvt(new PlayerId(3), new NetId(1000), PossessionPhase.Lurking));
            Assert.AreEqual(InputMode.Spirit, _input.Mode);
        }

        [Test]
        public void IdleInput_ReadsZero()
        {
            EventBus.Publish(new LocalRoleChangedEvt(Role.Human));
            IHumanInput h = _input;
            Assert.AreEqual(Vector2.zero, h.Move);
            Assert.AreEqual(-1, h.SelectSlot);
            Assert.IsFalse(h.KickPressed);
        }
    }
}
