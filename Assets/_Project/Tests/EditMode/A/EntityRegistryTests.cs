using CinliMahzen.Core;
using CinliMahzen.Core.Net;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CinliMahzen.Tests.EditMode.A
{
    public class EntityRegistryTests
    {
        private sealed class FakeEntity : INetEntity
        {
            public FakeEntity(int id) { NetId = new NetId(id); }
            public NetId NetId { get; }
            public override string ToString() => "Fake" + NetId;
        }

        private sealed class KickableEntity : INetEntity, IKickable
        {
            public KickableEntity(int id) { NetId = new NetId(id); }
            public NetId NetId { get; }
            public void OnKickedAuthority(PlayerId by) { }
        }

        private EntityRegistry _reg;

        [SetUp]
        public void SetUp()
        {
            GameServices.ResetToStubs();
            _reg = new EntityRegistry();
        }

        [TearDown]
        public void TearDown() => GameServices.ResetToStubs();

        [Test]
        public void Register_TryGet_ByInterface()
        {
            var k = new KickableEntity(1000);
            _reg.Register(k);

            Assert.IsTrue(_reg.TryGet(new NetId(1000), out IKickable got));
            Assert.AreSame(k, got);
            Assert.IsFalse(_reg.TryGet(new NetId(1000), out IDamageable _), "wrong interface");
            Assert.IsFalse(_reg.TryGet(new NetId(1001), out INetEntity _), "unknown id");
        }

        [Test]
        public void Unregister_RemovesOnlySameInstance()
        {
            var a = new FakeEntity(5);
            _reg.Register(a);
            _reg.Unregister(new FakeEntity(5));
            Assert.IsTrue(_reg.TryGet(new NetId(5), out INetEntity _));
            _reg.Unregister(a);
            Assert.IsFalse(_reg.TryGet(new NetId(5), out INetEntity _));
        }

        [Test]
        public void DuplicateId_Rejected()
        {
            var a = new FakeEntity(7);
            _reg.Register(a);
            LogAssert.Expect(LogType.Error, "[Entities] Register: N7 already used by FakeN7, rejected FakeN7");
            _reg.Register(new FakeEntity(7));
            Assert.IsTrue(_reg.TryGet(new NetId(7), out INetEntity got));
            Assert.AreSame(a, got);
        }

        [Test]
        public void InvalidId_Rejected()
        {
            LogAssert.Expect(LogType.Error, "[Entities] Register: FakeN0 has no NetId");
            _reg.Register(new FakeEntity(0));
            Assert.AreEqual(0, _reg.Count);
        }

        [Test]
        public void AllocateRuntimeId_StartsAt60000_Increments_SkipsUsed()
        {
            _reg.Register(new FakeEntity(NetId.RuntimeMin + 1));
            Assert.AreEqual(NetId.RuntimeMin, _reg.AllocateRuntimeId().Value);
            Assert.AreEqual(NetId.RuntimeMin + 2, _reg.AllocateRuntimeId().Value, "60001 is taken");
            _reg.Clear();
            Assert.AreEqual(NetId.RuntimeMin, _reg.AllocateRuntimeId().Value, "Clear restarts");
        }

        [Test]
        public void AllocateRuntimeId_NonAuthority_LogsError()
        {
            GameServices.Register<INetBridge>(new OfflineNetBridge(() => 0d, () => new PlayerId(0)) { IsAuthority = false });
            LogAssert.Expect(LogType.Error, "[Entities] AllocateRuntimeId called on a non-authority client");
            _reg.AllocateRuntimeId();
        }
    }
}
