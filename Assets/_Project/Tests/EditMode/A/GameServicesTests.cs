using CinliMahzen.Core;
using CinliMahzen.Core.Net;
using NUnit.Framework;

namespace CinliMahzen.Tests.EditMode.A
{
    public class GameServicesTests
    {
        private sealed class FakeMatch : IMatchInfo
        {
            public MatchState State => MatchState.Intro;
            public int RoundIndex => 2;
            public double StateEndTime => 5d;
            public bool JinnsAwake => false;
        }

        [SetUp]
        public void SetUp() => GameServices.ResetToStubs();

        [TearDown]
        public void TearDown() => GameServices.ResetToStubs();

        [Test]
        public void Stubs_AreDefault()
        {
            Assert.IsInstanceOf<StubMatchInfo>(GameServices.Match);
            Assert.IsInstanceOf<StubLevelInfo>(GameServices.Level);
            Assert.IsInstanceOf<StubObjectiveInfo>(GameServices.Get<IObjectiveInfo>());
            Assert.IsInstanceOf<StubLightService>(GameServices.Get<ILightService>());
            Assert.IsInstanceOf<StubPossessionQuery>(GameServices.Get<IPossessionQuery>());
            Assert.IsInstanceOf<StubPossessionBlockerRegistry>(GameServices.Get<IPossessionBlockerRegistry>());
            Assert.IsNull(GameServices.Net);
            Assert.IsNull(GameServices.Players);
            Assert.IsNull(GameServices.Entities);
        }

        [Test]
        public void Register_ReplacesStub_UnregisterRestoresIt()
        {
            var m = new FakeMatch();
            GameServices.Register<IMatchInfo>(m);
            Assert.AreSame(m, GameServices.Match);

            GameServices.Unregister<IMatchInfo>(new FakeMatch());
            Assert.AreSame(m, GameServices.Match, "other instance does not unregister");

            GameServices.Unregister<IMatchInfo>(m);
            Assert.IsInstanceOf<StubMatchInfo>(GameServices.Match);
        }

        [Test]
        public void Register_Net_And_Entities()
        {
            var net = new OfflineNetBridge(() => 0d, () => new PlayerId(0));
            var ents = new EntityRegistry();
            GameServices.Register<INetBridge>(net);
            GameServices.Register<IEntityRegistry>(ents);

            Assert.AreSame(net, GameServices.Net);
            Assert.AreSame(ents, GameServices.Entities);
            Assert.IsTrue(GameServices.TryGet(out INetBridge got));
            Assert.AreSame(net, got);
        }

        [Test]
        public void CustomService_TryGet()
        {
            Assert.IsFalse(GameServices.TryGet(out GameRandom _));
            var r = new GameRandom(1);
            GameServices.Register(r);
            Assert.AreSame(r, GameServices.Get<GameRandom>());
        }
    }
}
