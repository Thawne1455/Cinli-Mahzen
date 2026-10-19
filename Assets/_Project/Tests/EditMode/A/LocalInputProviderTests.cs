using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.Core.Events;
using CinliMahzen.Player;
using NUnit.Framework;

namespace CinliMahzen.Tests.EditMode.A
{
    /// <summary>
    /// TODO 1.3: <see cref="LocalInputSource"/> as the <see cref="ILocalInputProvider"/> pawns bind through (Jinn cannot see
    /// CM.Player), and its starting role read from the registry (bootstrap hook order is not fixed).
    /// </summary>
    public class LocalInputProviderTests
    {
        private static readonly PlayerId EvilPlayer = new PlayerId(2);

        private readonly List<LocalInputSource> _sources = new List<LocalInputSource>();

        [SetUp]
        public void SetUp()
        {
            GameServices.ResetToStubs();
            EventBus.ClearAll();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (LocalInputSource source in _sources)
                source.Dispose();
            _sources.Clear();
            EventBus.ClearAll();
            GameServices.ResetToStubs();
        }

        private LocalInputSource NewSource(PlayerId local)
        {
            var source = new LocalInputSource(() => local);
            _sources.Add(source);
            return source;
        }

        [Test]
        public void Idle_IsTheSharedNullInput()
        {
            ILocalInputProvider provider = NewSource(new PlayerId(0));

            Assert.AreSame(NullInput.Instance, provider.Idle);
            Assert.IsInstanceOf<IHumanInput>(provider.Idle);
            Assert.IsInstanceOf<ISpiritInput>(provider.Idle);
        }

        [Test]
        public void AcquireLocal_Human_EnablesTheHumanMap_AndReturnsTheLiveSource()
        {
            LocalInputSource source = NewSource(new PlayerId(0));
            ILocalInputProvider provider = source;

            IPawnInput input = provider.AcquireLocal(Role.Human);

            Assert.AreSame(source, input);
            Assert.IsInstanceOf<IHumanInput>(input);
            Assert.AreEqual(InputMode.Human, source.Mode);
            Assert.IsTrue(source.Actions.Human.enabled);
        }

        [Test]
        public void AcquireLocal_BothJinnRoles_EnableTheSpiritMap()
        {
            LocalInputSource source = NewSource(new PlayerId(0));
            ILocalInputProvider provider = source;

            IPawnInput good = provider.AcquireLocal(Role.GoodJinn);
            Assert.AreEqual(InputMode.Spirit, source.Mode);
            Assert.IsInstanceOf<ISpiritInput>(good);

            provider.AcquireLocal(Role.Human);
            IPawnInput evil = provider.AcquireLocal(Role.EvilJinn);
            Assert.AreEqual(InputMode.Spirit, source.Mode);
            Assert.AreSame(source, evil);
            Assert.IsFalse(source.Actions.Human.enabled);
            Assert.IsTrue(source.Actions.Spirit.enabled);
        }

        [Test]
        public void AcquireLocal_Repeated_KeepsTheMap()
        {
            LocalInputSource source = NewSource(new PlayerId(0));
            ILocalInputProvider provider = source;

            provider.AcquireLocal(Role.EvilJinn);
            provider.AcquireLocal(Role.EvilJinn);

            Assert.AreEqual(InputMode.Spirit, source.Mode);
            Assert.IsTrue(source.Actions.Spirit.enabled);
        }

        [Test]
        public void AcquireLocal_NoRole_DisablesTheGameplayMaps()
        {
            LocalInputSource source = NewSource(new PlayerId(0));
            ILocalInputProvider provider = source;
            provider.AcquireLocal(Role.Human);

            provider.AcquireLocal(Role.None);

            Assert.AreEqual(InputMode.None, source.Mode);
            Assert.IsFalse(source.Actions.Human.enabled);
            Assert.IsFalse(source.Actions.Spirit.enabled);
        }

        [Test]
        public void StartingRole_ComesFromTheRegistry_SoPossessionWorksBeforeTheFirstRoleEvent()
        {
            // Roles were assigned before this source existed: no LocalRoleChangedEvt will ever reach it for them.
            var registry = new OfflinePlayerRegistry();
            registry.SetRoles(new[] { Role.Human, Role.GoodJinn, Role.EvilJinn, Role.EvilJinn });
            GameServices.Register<IPlayerRegistry>(registry);
            LocalInputSource source = NewSource(EvilPlayer);

            EventBus.Publish(new PossessionChangedEvt(EvilPlayer, new NetId(1000), PossessionPhase.Lurking));

            Assert.AreEqual(InputMode.Possessed, source.Mode);
        }

        [Test]
        public void StartingRole_WithoutRegistry_IsNone_AndPossessionIsIgnored()
        {
            LocalInputSource source = NewSource(EvilPlayer);

            EventBus.Publish(new PossessionChangedEvt(EvilPlayer, new NetId(1000), PossessionPhase.Lurking));

            Assert.AreEqual(InputMode.None, source.Mode);
        }
    }
}
