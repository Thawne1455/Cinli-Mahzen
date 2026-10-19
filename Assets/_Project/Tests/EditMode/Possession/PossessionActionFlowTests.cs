using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.Core.Events;
using CinliMahzen.Core.Net;
using CinliMahzen.Possession;
using CinliMahzen.Tests.EditMode.B;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CinliMahzen.Tests.EditMode.Possession
{
    /// <summary>
    /// TODO 1.8: <see cref="PossessionActionRunner"/> with the real PD_* assets over an offline bridge. A fake action world places the objects
    /// on the X axis facing +Z, the human on +Z, and answers the hit checks geometrically (the human is a 0.4 m sphere, walls are a configurable
    /// free distance). Energy comes from the stub service (unlimited unless a test gives a player a real balance).
    /// </summary>
    public class PossessionActionFlowTests
    {
        private const string Folder = "Assets/_Project/ScriptableObjects/Possessables/";
        private const float HumanRadius = 0.4f;

        private static readonly PlayerId HumanId = new PlayerId(0);
        private static readonly PlayerId GoodId = new PlayerId(1);
        private static readonly PlayerId EvilId = new PlayerId(2);

        private sealed class FakeHuman : IDamageable, IStatusReceiver
        {
            public readonly List<DamageInfo> Hits = new List<DamageInfo>();
            public readonly List<StatusType> Statuses = new List<StatusType>();
            public readonly List<float> StatusDurations = new List<float>();

            public NetId NetId => new NetId(1);

            public void ApplyDamageAuthority(in DamageInfo info) => Hits.Add(info);

            public void ApplyStatusAuthority(StatusType t, float duration, PlayerId source)
            {
                Statuses.Add(t);
                StatusDurations.Add(duration);
            }
        }

        private sealed class FakeActionWorld : IActionWorld
        {
            public readonly Dictionary<NetId, PossessableDefinition> Defs = new Dictionary<NetId, PossessableDefinition>();
            public readonly Dictionary<NetId, Vector3> Positions = new Dictionary<NetId, Vector3>();
            public readonly Dictionary<PlayerId, Role> Roles = new Dictionary<PlayerId, Role>();
            public readonly FakeHuman Human = new FakeHuman();
            public Vector3 HumanAt = new Vector3(0f, 1f, 3f);
            public float Wall = float.MaxValue;
            public bool Fainted;
            public int Room = 4;

            public Role GetRole(PlayerId player) => Roles.TryGetValue(player, out Role r) ? r : Role.None;

            public bool TryGetObject(NetId obj, out PossessableDefinition definition, out Vector3 position, out float yaw)
            {
                yaw = 0f;
                position = Positions.TryGetValue(obj, out Vector3 p) ? p : Vector3.zero;
                return Defs.TryGetValue(obj, out definition);
            }

            public bool FindHumanInBox(Vector3 center, Vector3 halfExtents, Quaternion rotation, out HumanTarget target)
            {
                Vector3 local = Quaternion.Inverse(rotation) * (HumanAt - center);
                bool inside = Mathf.Abs(local.x) <= halfExtents.x + HumanRadius && Mathf.Abs(local.y) <= halfExtents.y + HumanRadius
                    && Mathf.Abs(local.z) <= halfExtents.z + HumanRadius;
                target = inside ? new HumanTarget(Human, Human) : default;
                return inside;
            }

            public bool CastSegment(Vector3 from, Vector3 to, float radius, out Vector3 point, out HumanTarget target)
            {
                target = default;
                point = to;
                Vector3 d = to - from;
                float t = d.sqrMagnitude > 0f ? Mathf.Clamp01(Vector3.Dot(HumanAt - from, d) / d.sqrMagnitude) : 0f;
                Vector3 closest = from + d * t;
                if ((closest - HumanAt).magnitude > radius + HumanRadius)
                    return false;
                point = closest;
                target = new HumanTarget(Human, Human);
                return true;
            }

            public float FreeDistance(Vector3 origin, Vector3 direction, float radius, float maxDistance) => Mathf.Min(maxDistance, Wall);

            public int RoomOf(Vector3 position) => Room;

            public bool IsHumanFainted => Fainted;

            public bool Cellar;
            public PlayerId GoodJinn = PlayerId.None;
            public Vector3 GoodJinnAt;

            public bool IsCellarPhase => Cellar;

            public bool CastGoodJinn(Vector3 from, Vector3 to, float radius, out PlayerId goodJinn, out Vector3 point)
            {
                goodJinn = PlayerId.None;
                point = to;
                if (!GoodJinn.IsValid)
                    return false;
                Vector3 d = to - from;
                float t = d.sqrMagnitude > 0f ? Mathf.Clamp01(Vector3.Dot(GoodJinnAt - from, d) / d.sqrMagnitude) : 0f;
                Vector3 closest = from + d * t;
                if ((closest - GoodJinnAt).magnitude > radius)
                    return false;
                goodJinn = GoodJinn;
                point = closest;
                return true;
            }

            public Vector3 HumanPosition => HumanAt;
        }

        private double _now;
        private PlayerId _local;
        private OfflineNetBridge _bridge;
        private FakePossessionWorld _possessionWorld;
        private FakeActionWorld _world;
        private PossessionAuthority _authority;
        private PossessionMirror _mirror;
        private PossessionActionRunner _runner;
        private StubJinnEnergyService _energy;
        private readonly List<NetMsg> _sent = new List<NetMsg>();
        private int _nextId = 40000;

        [SetUp]
        public void SetUp()
        {
            EventBus.ClearAll();
            GameServices.ResetToStubs();
            _now = 100d;
            _local = EvilId;
            _bridge = new OfflineNetBridge(() => _now, () => _local);
            _possessionWorld = new FakePossessionWorld();
            _world = new FakeActionWorld();
            foreach (var pair in new[] { (HumanId, Role.Human), (GoodId, Role.GoodJinn), (EvilId, Role.EvilJinn) })
            {
                _possessionWorld.SetRole(pair.Item1, pair.Item2);
                _world.Roles[pair.Item1] = pair.Item2;
            }

            _authority = new PossessionAuthority(_bridge, _possessionWorld, new PossessionRules(3f, 0.5f, 1.2f, 10f), 2f, 2.5f);
            _mirror = new PossessionMirror(_bridge);
            _runner = new PossessionActionRunner(_bridge, _authority, _world, 3f, 0.5f)
            {
                SpiritBlockRadius = 1f,
                SpiritBlockCost = 30f,
                RageCooldownMult = 0.6f,
            };
            _authority.Attach();
            _mirror.Attach();
            _runner.Attach();

            _energy = new StubJinnEnergyService();
            GameServices.Register<IJinnEnergyService>(_energy);

            _sent.Clear();
            foreach (MsgCode code in new[] { MsgCode.ActionTelegraph, MsgCode.ActionResolved, MsgCode.ActionDenied, MsgCode.ObjectStateChanged,
                         MsgCode.ProjectileSpawned, MsgCode.ProjectileImpact, MsgCode.ObjectMoved, MsgCode.RadioStateChanged,
                         MsgCode.CologneReviveTriggered, MsgCode.PossessEnded })
                _bridge.Register(code, _sent.Add);
        }

        [TearDown]
        public void TearDown()
        {
            _runner.Detach();
            _mirror.Detach();
            _authority.Detach();
            EventBus.ClearAll();
            GameServices.ResetToStubs();
        }

        // ---------- helpers ----------

        private NetId Place(string asset, Vector3 position)
        {
            var def = AssetDatabase.LoadAssetAtPath<PossessableDefinition>(Folder + asset + ".asset");
            Assert.IsNotNull(def, asset + " missing");
            var id = new NetId(_nextId++);
            _world.Defs[id] = def;
            _world.Positions[id] = position;
            _authority.Register(id, PossessableTraits.From(def));
            return id;
        }

        private void Enter(PlayerId who, NetId obj)
        {
            _local = who;
            _bridge.SendToAuthority(ReqPossessMsg.Encode(obj));
            Tick(1.2);
            Assert.AreEqual(PossessionPhase.Lurking, _mirror.PhaseOf(obj), "precondition: inside");
            _sent.Clear();
        }

        private void Act(PlayerId who, int index, Vector3 aim)
        {
            _local = who;
            _bridge.SendToAuthority(ReqActionMsg.Encode(index, aim));
        }

        private void Tick(double dt)
        {
            _now += dt;
            _authority.TickAuthority(_now);
            _runner.TickAuthority(_now);
        }

        /// <summary>Advances in small steps (projectiles need several).</summary>
        private void Run(double seconds)
        {
            int steps = (int)System.Math.Ceiling(seconds / 0.02);
            for (int i = 0; i < steps; i++)
                Tick(0.02);
        }

        private List<NetMsg> Sent(MsgCode code) => _sent.FindAll(m => m.Code == code);

        private ActionDenyReason LastDenial()
        {
            List<NetMsg> denied = Sent(MsgCode.ActionDenied);
            Assert.IsNotEmpty(denied, "nothing was denied");
            Assert.IsTrue(ActionDeniedMsg.TryDecode(denied[denied.Count - 1], out _, out ActionDenyReason reason));
            return reason;
        }

        // ---------- telegraph and phases ----------

        [Test]
        public void Topple_TelegraphsThenHitsWithKnockdown_AndTheShelfIsSpentAfterRecovery()
        {
            NetId shelf = Place("PD_Shelf", Vector3.zero);
            _world.HumanAt = new Vector3(0f, 1f, 1.2f);
            Enter(EvilId, shelf);

            Act(EvilId, 0, Vector3.forward);

            Assert.AreEqual(1, Sent(MsgCode.ActionTelegraph).Count);
            Assert.AreEqual(PossessionPhase.Charging, _mirror.PhaseOf(shelf));
            Assert.AreEqual(0, _world.Human.Hits.Count, "nothing before the telegraph time");

            Tick(1.0);

            Assert.AreEqual(1, _world.Human.Hits.Count);
            DamageInfo hit = _world.Human.Hits[0];
            Assert.AreEqual(1, hit.Amount);
            Assert.AreEqual(DamageFlags.Knockdown, hit.Flags & DamageFlags.Knockdown);
            Assert.AreEqual(2f, hit.KnockdownTime);
            Assert.AreEqual(EvilId, hit.Attacker);
            Assert.AreEqual(shelf, hit.SourceObject);
            Assert.IsTrue(ActionResolvedMsg.TryDecode(Sent(MsgCode.ActionResolved)[0], out _, out _, out bool hitHuman, out bool spent, out _, out _, out _));
            Assert.IsTrue(hitHuman);
            Assert.IsTrue(spent);
            Assert.AreEqual(PossessionPhase.Recovering, _mirror.PhaseOf(shelf));

            Tick(1.0);
            Assert.AreEqual(PossessionPhase.Spent, _mirror.PhaseOf(shelf), "single use: the jinn is thrown out and the shelf is used up");
        }

        [Test]
        public void Topple_MissesAHumanOutsideTheBox()
        {
            NetId shelf = Place("PD_Shelf", Vector3.zero);
            _world.HumanAt = new Vector3(0f, 1f, 6f);
            Enter(EvilId, shelf);

            Act(EvilId, 0, Vector3.forward);
            Tick(1.0);

            Assert.AreEqual(0, _world.Human.Hits.Count);
        }

        [Test]
        public void Recovery_EndsInLurking_WithObjectStateChanged()
        {
            NetId radio = Place("PD_Radio", Vector3.zero);
            Enter(EvilId, radio);

            Act(EvilId, 0, Vector3.forward);
            Tick(0.3);
            Assert.AreEqual(PossessionPhase.Recovering, _mirror.PhaseOf(radio));
            Tick(0.3);

            Assert.AreEqual(1, Sent(MsgCode.ObjectStateChanged).Count);
            Assert.AreEqual(PossessionPhase.Lurking, _mirror.PhaseOf(radio));
        }

        // ---------- refusals ----------

        [Test]
        public void NotInside_Busy_NoSuchAction_AndNotUsable_AreDenied()
        {
            NetId cologne = Place("PD_Cologne", Vector3.zero);
            NetId bottle = Place("PD_Bottle", new Vector3(5f, 0f, 0f));

            Act(EvilId, 0, Vector3.forward);
            Assert.AreEqual(ActionDenyReason.NotInside, LastDenial());

            Enter(EvilId, cologne);
            Act(EvilId, 1, Vector3.forward);
            Assert.AreEqual(ActionDenyReason.NotUsable, LastDenial(), "revive_splash is the good jinn's");

            Act(EvilId, 0, Vector3.forward);
            Act(EvilId, 0, Vector3.forward);
            Assert.AreEqual(ActionDenyReason.Busy, LastDenial(), "still charging");

            _local = EvilId;
            _bridge.SendToAuthority(ReqUnpossessMsg.Encode());
            _now += 20;
            Enter(EvilId, bottle);
            Act(EvilId, 1, Vector3.forward);
            Assert.AreEqual(ActionDenyReason.NoSuchAction, LastDenial(), "the bottle has no secondary action");
        }

        [Test]
        public void Cooldown_BlocksTheSameActionUntilItRunsOut()
        {
            NetId slipper = Place("PD_Slipper", Vector3.zero);
            _world.HumanAt = new Vector3(50f, 0f, 0f);
            Enter(EvilId, slipper);

            Act(EvilId, 0, Vector3.forward);
            Run(3.0);
            Assert.AreEqual(PossessionPhase.Lurking, _mirror.PhaseOf(slipper), "recovered");

            Act(EvilId, 0, Vector3.forward);
            Assert.AreEqual(ActionDenyReason.Cooldown, LastDenial());

            Run(15.0);
            _sent.Clear();
            Act(EvilId, 0, Vector3.forward);
            Assert.AreEqual(1, Sent(MsgCode.ActionTelegraph).Count, "15 s later it works again");
        }

        [Test]
        public void NotEnoughEnergy_DeniesWithoutCharging()
        {
            NetId slipper = Place("PD_Slipper", Vector3.zero);
            Enter(EvilId, slipper);
            _energy.SetEnergy(EvilId, 10f);

            Act(EvilId, 0, Vector3.forward);

            Assert.AreEqual(ActionDenyReason.NotEnoughEnergy, LastDenial());
            Assert.AreEqual(PossessionPhase.Lurking, _mirror.PhaseOf(slipper));
            Assert.AreEqual(10f, _energy.Get(EvilId), "nothing deducted");
        }

        [Test]
        public void Energy_IsSpentOnTheTelegraph()
        {
            NetId radio = Place("PD_Radio", Vector3.zero);
            Enter(EvilId, radio);
            _energy.SetEnergy(EvilId, 50f);

            Act(EvilId, 0, Vector3.forward);

            Assert.AreEqual(35f, _energy.Get(EvilId), "play_tune costs 15");
        }

        // ---------- projectiles ----------

        [Test]
        public void BottleThrow_FliesHitsTheHuman_AndBreaks()
        {
            NetId bottle = Place("PD_Bottle", Vector3.zero);
            _world.HumanAt = new Vector3(0f, 1.6f, 5f); // the bottle arcs 1.5 m above its 0.3 m launch height at mid-range
            Enter(EvilId, bottle);

            Act(EvilId, 0, Vector3.forward);
            Tick(0.5);
            Assert.AreEqual(1, Sent(MsgCode.ProjectileSpawned).Count);
            Assert.AreEqual(1, _runner.FlightCount);

            Run(1.0);

            Assert.AreEqual(0, _runner.FlightCount);
            Assert.IsTrue(ProjectileImpactMsg.TryDecode(Sent(MsgCode.ProjectileImpact)[0], out NetId obj, out _, out bool hitHuman));
            Assert.AreEqual(bottle, obj);
            Assert.IsTrue(hitHuman);
            Assert.AreEqual(1, _world.Human.Hits.Count);

            Run(2.0);
            Assert.AreEqual(PossessionPhase.Spent, _mirror.PhaseOf(bottle));
        }

        [Test]
        public void Slipper_HitAlsoSpinsTheHumansCamera()
        {
            NetId slipper = Place("PD_Slipper", Vector3.zero);
            _world.HumanAt = new Vector3(0f, 0.3f, 6f);
            Enter(EvilId, slipper);

            Act(EvilId, 0, Vector3.forward);
            Run(2.0);

            Assert.AreEqual(1, _world.Human.Hits.Count);
            CollectionAssert.AreEqual(new[] { StatusType.CameraSpin180 }, _world.Human.Statuses);
            Assert.AreEqual(0.15f, _world.Human.StatusDurations[0], 1e-5f);
            Assert.IsTrue(ProjectileSpawnedMsg.TryDecode(Sent(MsgCode.ProjectileSpawned)[0], out _, out ProjectilePath path));
            Assert.AreEqual(0f, path.Arc, "Mother's Slipper flies straight");
        }

        [Test]
        public void AMissedThrow_LandsAtTheEndOfItsRange_WithoutDamage()
        {
            NetId bottle = Place("PD_Bottle", Vector3.zero);
            _world.HumanAt = new Vector3(20f, 0f, 0f);
            Enter(EvilId, bottle);

            Act(EvilId, 0, Vector3.forward);
            Run(2.0);

            Assert.IsTrue(ProjectileImpactMsg.TryDecode(Sent(MsgCode.ProjectileImpact)[0], out _, out Vector3 point, out bool hitHuman));
            Assert.IsFalse(hitHuman);
            Assert.AreEqual(10f, point.z, 0.01f, "range 10");
            Assert.AreEqual(0, _world.Human.Hits.Count);
        }

        // ---------- moving actions ----------

        [Test]
        public void Lunge_DashesForward_AndHitsAtTheEnd()
        {
            NetId chair = Place("PD_Chair", Vector3.zero);
            _world.HumanAt = new Vector3(0f, 0.8f, 2.4f);
            Enter(EvilId, chair);

            Act(EvilId, 0, Vector3.right);
            Tick(0.4);

            Assert.IsTrue(ActionResolvedMsg.TryDecode(Sent(MsgCode.ActionResolved)[0], out _, out _, out bool hit, out _, out Vector3 end, out float yaw, out _));
            Assert.AreEqual(new Vector3(0f, 0f, 2f), end, "the chair's yaw limit is 0: always straight ahead, range 2");
            Assert.AreEqual(0f, yaw, 1e-3f);
            Assert.IsTrue(hit);
            Assert.AreEqual(1, _world.Human.Hits.Count);
        }

        [Test]
        public void Slide_StopsAtAWall_AndHurtsNobody()
        {
            NetId barrel = Place("PD_Barrel", Vector3.zero);
            _world.HumanAt = new Vector3(0f, 0.5f, 0.6f);
            _world.Wall = 0.5f;
            Enter(GoodId, barrel);

            Act(GoodId, 0, Vector3.forward);
            Tick(0.6);

            Assert.IsTrue(ActionResolvedMsg.TryDecode(Sent(MsgCode.ActionResolved)[0], out _, out _, out bool hit, out _, out Vector3 end, out _, out _));
            Assert.AreEqual(0.5f, end.z, 1e-4f);
            Assert.IsFalse(hit);
            Assert.AreEqual(0, _world.Human.Hits.Count, "the good jinn's slide is harmless");
        }

        [Test]
        public void ClampToFacing_LimitsTheTurn()
        {
            Vector3 dir = PossessionActionRunner.ClampToFacing(Vector3.right, 0f, 45f);
            Assert.AreEqual(45f, PossessionActionRunner.YawOf(dir), 1e-3f);
            dir = PossessionActionRunner.ClampToFacing(new Vector3(-0.2f, 0f, 1f), 0f, 45f);
            Assert.Less(PossessionActionRunner.YawOf(dir), 0f);
            Assert.Greater(PossessionActionRunner.YawOf(dir), -45f);
            Assert.AreEqual(Vector3.forward, PossessionActionRunner.ClampToFacing(Vector3.up, 0f, 45f), "no horizontal aim: straight ahead");
        }

        // ---------- room effects ----------

        [Test]
        public void PlayTune_MutesTheRoomForEffectDuration()
        {
            NetId radio = Place("PD_Radio", Vector3.zero);
            Enter(EvilId, radio);

            Act(EvilId, 0, Vector3.forward);
            Tick(0.3);

            Assert.IsTrue(RadioStateChangedMsg.TryDecode(Sent(MsgCode.RadioStateChanged)[0], out int room, out double until));
            Assert.AreEqual(4, room);
            Assert.AreEqual(_now + 12d, until, 1e-6);
        }

        [Test]
        public void ReviveSplash_WakesAFaintedHumanInRange_Only()
        {
            NetId cologne = Place("PD_Cologne", Vector3.zero);
            _world.HumanAt = new Vector3(0f, 0f, 2f);
            Enter(GoodId, cologne);

            Act(GoodId, 1, Vector3.forward);
            Tick(0.5);
            Assert.AreEqual(0, Sent(MsgCode.CologneReviveTriggered).Count, "the human is not fainted");

            Run(1.0);
            _world.Fainted = true;
            _sent.Clear();
            // Splash was single use: the cologne is spent now; a fresh one is needed.
            NetId fresh = Place("PD_Cologne", Vector3.zero);
            _now += 20;
            Enter(GoodId, fresh);
            Act(GoodId, 1, Vector3.forward);
            Tick(0.5);

            Assert.IsTrue(CologneReviveTriggeredMsg.TryDecode(Sent(MsgCode.CologneReviveTriggered)[0], out PlayerId by));
            Assert.AreEqual(GoodId, by);
        }

        // ---------- hop ----------

        [Test]
        public void Hop_MovesAMovableObject_AtMostEveryHopInterval()
        {
            NetId chair = Place("PD_Chair", Vector3.zero);
            Enter(EvilId, chair);

            Assert.AreEqual(ActionDenyReason.None, _runner.ProcessHopAuthority(EvilId, Vector3.right));
            Assert.IsTrue(ObjectMovedMsg.TryDecode(Sent(MsgCode.ObjectMoved)[0], out NetId obj, out Vector3 position, out float yaw));
            Assert.AreEqual(chair, obj);
            Assert.AreEqual(new Vector3(1f, 0f, 0f), position);
            Assert.AreEqual(90f, yaw, 1e-3f);

            Assert.AreEqual(ActionDenyReason.Cooldown, _runner.ProcessHopAuthority(EvilId, Vector3.right));
            Tick(0.5);
            Assert.AreEqual(ActionDenyReason.None, _runner.ProcessHopAuthority(EvilId, Vector3.right));
        }

        [Test]
        public void Hop_IsRefused_ForAnImmovableObject_OrAgainstAWall()
        {
            NetId barrel = Place("PD_Barrel", Vector3.zero);
            NetId stool = Place("PD_Stool", new Vector3(3f, 0f, 0f));
            Enter(EvilId, barrel);
            Assert.AreEqual(ActionDenyReason.NoSuchAction, _runner.ProcessHopAuthority(EvilId, Vector3.forward));

            _local = EvilId;
            _bridge.SendToAuthority(ReqUnpossessMsg.Encode());
            Enter(EvilId, stool);
            _world.Wall = 0f;
            Assert.AreEqual(ActionDenyReason.Blocked, _runner.ProcessHopAuthority(EvilId, Vector3.forward));
        }

        // ---------- cellar (TODO 3.7) ----------

        [Test]
        public void SpiritBlock_InTheCellar_StopsTheThrow_AndCostsTheGoodJinn()
        {
            NetId bottle = Place("PD_Bottle", Vector3.zero);
            _world.HumanAt = new Vector3(0f, 1.6f, 5f);
            _world.Cellar = true;
            _world.GoodJinn = GoodId;
            _world.GoodJinnAt = new Vector3(0f, 1.2f, 2.5f);
            _energy.SetEnergy(GoodId, 50f);
            _bridge.Register(MsgCode.SpiritBlockedProjectile, _sent.Add);
            Enter(EvilId, bottle);

            Act(EvilId, 0, Vector3.forward);
            Tick(0.5);
            Run(1.0);

            Assert.AreEqual(1, Sent(MsgCode.SpiritBlockedProjectile).Count);
            Assert.AreEqual(0, _world.Human.Hits.Count, "the human is protected");
            Assert.AreEqual(20f, _energy.Get(GoodId), "30 energy");
            Assert.IsTrue(ProjectileImpactMsg.TryDecode(Sent(MsgCode.ProjectileImpact)[0], out _, out _, out bool hit));
            Assert.IsFalse(hit);
        }

        [Test]
        public void SpiritBlock_NeedsTheCellar_AndEnoughEnergy()
        {
            NetId bottle = Place("PD_Bottle", Vector3.zero);
            NetId plate = Place("PD_Plate", new Vector3(2f, 0f, 0f));
            _world.HumanAt = new Vector3(0f, 1.6f, 5f);
            _world.GoodJinn = GoodId;
            _world.GoodJinnAt = new Vector3(0f, 1.2f, 2.5f);
            Enter(EvilId, bottle);

            Act(EvilId, 0, Vector3.forward);
            Tick(0.5);
            Run(1.0);
            Assert.AreEqual(1, _world.Human.Hits.Count, "outside the cellar the good jinn does not block");

            _world.Cellar = true;
            _energy.SetEnergy(GoodId, 10f);
            _world.HumanAt = new Vector3(2f, 1.6f, 5f);
            _world.GoodJinnAt = new Vector3(2f, 1.2f, 2.5f);
            Run(3.0);
            Enter(EvilId, plate);
            Act(EvilId, 0, Vector3.forward);
            Tick(0.5);
            Run(1.0);
            Assert.AreEqual(2, _world.Human.Hits.Count, "too little energy: it goes through");
        }

        [Test]
        public void Rage_ShortensCooldowns()
        {
            NetId slipper = Place("PD_Slipper", Vector3.zero);
            _world.HumanAt = new Vector3(50f, 0f, 0f);
            Enter(EvilId, slipper);
            _bridge.Broadcast(CellarMsgs.EncodeRage(true));

            Act(EvilId, 0, Vector3.forward);
            Tick(0.6);
            double readyAt = _runner.CooldownReadyAt(slipper, 0);
            Assert.AreEqual(_now + 15d * 0.6d, readyAt, 1e-6, "15 s × RageCooldownMult");
        }

        // ---------- round ----------

        [Test]
        public void NewRound_ClearsCooldownsAndFlights()
        {
            NetId slipper = Place("PD_Slipper", Vector3.zero);
            _world.HumanAt = new Vector3(50f, 0f, 0f);
            Enter(EvilId, slipper);
            Act(EvilId, 0, Vector3.forward);
            Tick(0.6);
            Assert.AreEqual(1, _runner.FlightCount);

            EventBus.Publish(new RoundStartedEvt(1, 7));

            Assert.AreEqual(0, _runner.FlightCount);
            Assert.LessOrEqual(_runner.CooldownReadyAt(slipper, 0), _now, "no cooldown left");
        }
    }
}
