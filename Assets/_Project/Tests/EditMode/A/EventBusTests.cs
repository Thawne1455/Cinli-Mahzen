using System;
using CinliMahzen.Core;
using CinliMahzen.Core.Events;
using NUnit.Framework;

namespace CinliMahzen.Tests.EditMode.A
{
    public class EventBusTests
    {
        [SetUp]
        public void SetUp() => EventBus.ClearAll();

        [TearDown]
        public void TearDown() => EventBus.ClearAll();

        [Test]
        public void Subscribe_Publish_Delivers()
        {
            int got = -1;
            Action<KeyFragmentCollectedEvt> h = e => got = e.Count;
            EventBus.Subscribe(h);

            EventBus.Publish(new KeyFragmentCollectedEvt(2));

            Assert.AreEqual(2, got);
        }

        [Test]
        public void Unsubscribe_StopsDelivery()
        {
            int calls = 0;
            Action<NoiseEvt> h = _ => calls++;
            EventBus.Subscribe(h);
            EventBus.Publish(new NoiseEvt(default, 1f));
            EventBus.Unsubscribe(h);
            EventBus.Publish(new NoiseEvt(default, 1f));

            Assert.AreEqual(1, calls);
            Assert.AreEqual(0, EventBus.SubscriberCount<NoiseEvt>());
        }

        [Test]
        public void DuplicateSubscribe_Ignored()
        {
            int calls = 0;
            Action<RoundStartedEvt> h = _ => calls++;
            EventBus.Subscribe(h);
            EventBus.Subscribe(h);
            EventBus.Publish(new RoundStartedEvt(0, 1));
            Assert.AreEqual(1, calls);
        }

        [Test]
        public void EventTypes_AreIndependent()
        {
            int a = 0, b = 0;
            EventBus.Subscribe<PhaseChangedEvt>(_ => a++);
            EventBus.Subscribe<GoldStateEvt>(_ => b++);
            EventBus.Publish(new PhaseChangedEvt(ObjectivePhase.Vault));
            Assert.AreEqual(1, a);
            Assert.AreEqual(0, b);
        }

        [Test]
        public void UnsubscribeDuringPublish_IsSafe()
        {
            int first = 0, second = 0;
            Action<LocalRoleChangedEvt> h2 = _ => second++;
            Action<LocalRoleChangedEvt> h1 = null;
            h1 = _ =>
            {
                first++;
                EventBus.Unsubscribe(h1);
                EventBus.Unsubscribe(h2);
            };
            EventBus.Subscribe(h1);
            EventBus.Subscribe(h2);

            EventBus.Publish(new LocalRoleChangedEvt(Role.Human));
            EventBus.Publish(new LocalRoleChangedEvt(Role.Human));

            Assert.AreEqual(1, first);
            Assert.AreEqual(0, second);
            Assert.AreEqual(0, EventBus.SubscriberCount<LocalRoleChangedEvt>());
        }

        [Test]
        public void ClearAll_RemovesEverything()
        {
            int calls = 0;
            EventBus.Subscribe<RoundEndedEvt>(_ => calls++);
            EventBus.Subscribe<HumanDiedEvt>(_ => calls++);
            EventBus.ClearAll();
            EventBus.Publish(new RoundEndedEvt(RoundResult.JinnsWin, RoundEndReason.Kill));
            EventBus.Publish(new HumanDiedEvt(default));
            Assert.AreEqual(0, calls);
        }
    }
}
