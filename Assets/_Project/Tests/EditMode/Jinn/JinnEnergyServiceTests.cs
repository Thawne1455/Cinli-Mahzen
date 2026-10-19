using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.Core.Events;
using CinliMahzen.Core.Net;
using CinliMahzen.Jinn;
using NUnit.Framework;
using UnityEngine;

namespace CinliMahzen.Tests.EditMode.Jinn
{
    /// <summary>TODO 1.8: <see cref="JinnEnergyService"/> — tanks per jinn role, spends, regen, sync broadcasts, round reset, F6.</summary>
    public class JinnEnergyServiceTests
    {
        private static readonly PlayerId Human = new PlayerId(0);
        private static readonly PlayerId Good = new PlayerId(1);
        private static readonly PlayerId Evil = new PlayerId(2);

        private double _now;
        private OfflineNetBridge _bridge;
        private OfflinePlayerRegistry _players;
        private GameBalanceConfig _config;
        private JinnEnergyService _service;
        private readonly List<NetMsg> _sent = new List<NetMsg>();

        [SetUp]
        public void SetUp()
        {
            EventBus.ClearAll();
            _now = 10d;
            _bridge = new OfflineNetBridge(() => _now, () => Human);
            _players = new OfflinePlayerRegistry();
            _players.SetRoles(new[] { Role.Human, Role.GoodJinn, Role.EvilJinn, Role.EvilJinn });
            _config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            _service = JinnEnergyService.FromConfig(_bridge, _players, _config, 1f, 0.25f);
            _service.Attach();
            _sent.Clear();
            _bridge.Register(MsgCode.EnergyChanged, _sent.Add);
        }

        [TearDown]
        public void TearDown()
        {
            _service.Detach();
            EventBus.ClearAll();
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void JinnsStartWithEnergyStart_TheHumanHasNoTank()
        {
            Assert.AreEqual(_config.EnergyStart, _service.Get(Evil));
            Assert.AreEqual(Mathf.Min(_config.EnergyStart, _config.GoodEnergyMax), _service.Get(Good));
            Assert.AreEqual(0f, _service.Get(Human));
            Assert.IsFalse(_service.TrySpendAuthority(Human, 1f));
            Assert.IsTrue(_service.TrySpendAuthority(Human, 0f), "a free action always succeeds");
        }

        [Test]
        public void Spend_DeductsAndBroadcastsAtOnce_AndRefusesMoreThanTheBalance()
        {
            Assert.IsTrue(_service.TrySpendAuthority(Evil, 25f));
            Assert.AreEqual(_config.EnergyStart - 25f, _service.Get(Evil));
            Assert.AreEqual(1, _sent.Count);
            Assert.IsTrue(EnergyChangedMsg.TryDecode(_sent[0], out PlayerId who, out float value));
            Assert.AreEqual(Evil, who);
            Assert.AreEqual(_config.EnergyStart - 25f, value);

            Assert.IsFalse(_service.TrySpendAuthority(Evil, 1000f));
            Assert.AreEqual(_config.EnergyStart - 25f, _service.Get(Evil), "nothing deducted on a refusal");
        }

        [Test]
        public void Regen_FillsOverTime_UpToMax_WithThrottledSync()
        {
            _service.TrySpendAuthority(Evil, _config.EnergyStart);
            _sent.Clear();

            _now += 2d;
            _service.TickAuthority(2f);

            Assert.AreEqual(_config.EnergyRegen * 2f, _service.Get(Evil), 1e-4f);
            Assert.IsNotEmpty(_sent, "regen beyond SyncMinDelta is broadcast");

            _now += 1000d;
            _service.TickAuthority(1000f);
            Assert.AreEqual(_config.EnergyMax, _service.Get(Evil), 1e-4f);
        }

        [Test]
        public void Unlimited_MakesEverySpendFree()
        {
            _service.Unlimited = true;
            Assert.IsTrue(_service.TrySpendAuthority(Evil, 1000f));
            Assert.AreEqual(_config.EnergyStart, _service.Get(Evil));
        }

        [Test]
        public void NewRound_AndNewRoles_RefillTheTanks()
        {
            _service.TrySpendAuthority(Evil, 30f);
            EventBus.Publish(new RoundStartedEvt(1, 3));
            Assert.AreEqual(_config.EnergyStart, _service.Get(Evil));

            _players.SetRoles(new[] { Role.EvilJinn, Role.Human, Role.GoodJinn, Role.EvilJinn });
            Assert.AreEqual(0f, _service.Get(Good), "player 1 is the human now");
            Assert.AreEqual(_config.EnergyStart, _service.Get(Human), "player 0 is an evil jinn now");
        }
    }
}
