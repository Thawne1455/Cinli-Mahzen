using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.Core.Events;
using CinliMahzen.Core.Net;
using CinliMahzen.Player;
using NUnit.Framework;
using UnityEngine;

namespace CinliMahzen.Tests.EditMode.Player
{
    /// <summary>TODO 3.8: <see cref="HumanAbilityAuthority"/> — lantern pulse (strong lantern only, cooldown, cone) and kick (cooldown, nearest possessed).</summary>
    public class HumanAbilityAuthorityTests
    {
        private static readonly PlayerId Human = new PlayerId(0);
        private static readonly PlayerId Evil = new PlayerId(2);

        private sealed class FakeQuery : IPossessionQuery
        {
            public readonly List<NetId> InCone = new List<NetId>();
            public float LastRange;
            public float LastAngle;

            public bool IsPossessed(NetId obj) => InCone.Contains(obj);
            public PlayerId PossessorOf(NetId obj) => InCone.Contains(obj) ? Evil : PlayerId.None;

            public void GetPossessedInCone(Vector3 origin, Vector3 dir, float range, float angleDeg, List<NetId> results)
            {
                LastRange = range;
                LastAngle = angleDeg;
                results.Clear();
                results.AddRange(InCone);
            }
        }

        private sealed class FakeKickable : IKickable, INetEntity
        {
            public NetId NetId { get; set; }
            public readonly List<PlayerId> KickedBy = new List<PlayerId>();
            public void OnKickedAuthority(PlayerId by) => KickedBy.Add(by);
        }

        private double _now;
        private OfflineNetBridge _bridge;
        private GameBalanceConfig _config;
        private EntityRegistry _entities;
        private FakeQuery _query;
        private HumanAbilityAuthority _abilities;
        private readonly List<NetMsg> _sent = new List<NetMsg>();
        private readonly Dictionary<NetId, Vector3> _positions = new Dictionary<NetId, Vector3>();

        [SetUp]
        public void SetUp()
        {
            EventBus.ClearAll();
            GameServices.ResetToStubs();
            _now = 10d;
            _bridge = new OfflineNetBridge(() => _now, () => Human);
            var players = new OfflinePlayerRegistry();
            players.SetRoles(new[] { Role.Human, Role.GoodJinn, Role.EvilJinn, Role.EvilJinn });
            _config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            _entities = new EntityRegistry();
            _query = new FakeQuery();
            GameServices.Register<IPossessionQuery>(_query);
            _abilities = new HumanAbilityAuthority(_bridge, players, _entities, _config)
            {
                EntityPosition = id => _positions.TryGetValue(id, out Vector3 p) ? p : (Vector3?)null,
            };
            _abilities.Attach();
            _sent.Clear();
            _bridge.Register(MsgCode.LanternPulsed, _sent.Add);
            _bridge.Register(MsgCode.KickResult, _sent.Add);
        }

        [TearDown]
        public void TearDown()
        {
            _abilities.Detach();
            EventBus.ClearAll();
            GameServices.ResetToStubs();
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void Pulse_NeedsTheBlessedOil()
        {
            Assert.AreEqual(-1, _abilities.ProcessPulseAuthority(Human, Vector3.zero, Vector3.forward), "weak lantern");

            EventBus.Publish(new BlessedOilCollectedEvt(Human));
            _query.InCone.Add(new NetId(40001));
            Assert.AreEqual(1, _abilities.ProcessPulseAuthority(Human, Vector3.zero, Vector3.forward));

            Assert.AreEqual(_config.LanternPulseRange, _query.LastRange);
            Assert.AreEqual(_config.LanternPulseAngle, _query.LastAngle);
            Assert.IsTrue(HumanActionMsgs.TryDecodePulsed(_sent[0], out PlayerId who, out int[] objects, out double until));
            Assert.AreEqual(Human, who);
            CollectionAssert.AreEqual(new[] { 40001 }, objects);
            Assert.AreEqual(_now + _config.LanternPulseDuration, until, 1e-6);
        }

        [Test]
        public void Pulse_HasACooldown_AndOnlyTheHumanPulses()
        {
            EventBus.Publish(new BlessedOilCollectedEvt(Human));
            Assert.AreEqual(-1, _abilities.ProcessPulseAuthority(Evil, Vector3.zero, Vector3.forward));
            Assert.AreEqual(0, _abilities.ProcessPulseAuthority(Human, Vector3.zero, Vector3.forward));
            Assert.AreEqual(-1, _abilities.ProcessPulseAuthority(Human, Vector3.zero, Vector3.forward), "12 s cooldown");
            _now += _config.LanternPulseCooldown;
            Assert.AreEqual(0, _abilities.ProcessPulseAuthority(Human, Vector3.zero, Vector3.forward));
        }

        [Test]
        public void NewRound_MakesTheLanternWeakAgain()
        {
            EventBus.Publish(new BlessedOilCollectedEvt(Human));
            Assert.IsTrue(_abilities.IsStrong(Human));
            EventBus.Publish(new RoundStartedEvt(1, 2));
            Assert.IsFalse(_abilities.IsStrong(Human));
        }

        [Test]
        public void Kick_HitsTheNearestPossessedObject_AndCoolsDown()
        {
            var near = new FakeKickable { NetId = new NetId(40010) };
            var far = new FakeKickable { NetId = new NetId(40011) };
            _entities.Register(near);
            _entities.Register(far);
            _positions[near.NetId] = new Vector3(0f, 0f, 1f);
            _positions[far.NetId] = new Vector3(0f, 0f, 1.8f);
            _query.InCone.Add(far.NetId);
            _query.InCone.Add(near.NetId);

            Assert.AreEqual(near.NetId, _abilities.ProcessKickAuthority(Human, Vector3.zero, Vector3.forward));
            CollectionAssert.AreEqual(new[] { Human }, near.KickedBy);
            Assert.AreEqual(0, far.KickedBy.Count);
            Assert.AreEqual(_config.KickRange + _config.NetRangeTolerance, _query.LastRange, 1e-6f);

            Assert.AreEqual(NetId.Invalid, _abilities.ProcessKickAuthority(Human, Vector3.zero, Vector3.forward), "6 s cooldown");
            Assert.AreEqual(1, near.KickedBy.Count);
        }

        [Test]
        public void Kick_WithNothingInFront_ReportsNothing()
        {
            Assert.AreEqual(NetId.Invalid, _abilities.ProcessKickAuthority(Human, Vector3.zero, Vector3.forward));
            Assert.IsTrue(HumanActionMsgs.TryDecodeKickResult(_sent[0], out PlayerId who, out NetId obj));
            Assert.AreEqual(Human, who);
            Assert.AreEqual(NetId.Invalid, obj);
        }
    }
}
