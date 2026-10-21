using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.Core.Events;
using CinliMahzen.Core.Net;
using CinliMahzen.Possession;
using CinliMahzen.Tests.EditMode.B;
using NUnit.Framework;

namespace CinliMahzen.Tests.EditMode.Possession
{
    /// <summary>
    /// TODO 1.7: <see cref="PossessionAuthority"/> + <see cref="PossessionMirror"/> over an offline bridge on a fake clock. Requests go through
    /// the bridge as the player a test names; the mirror's <see cref="PossessionChangedEvt"/>s and every broadcast are recorded.
    /// </summary>
    public class PossessionFlowTests
    {
        private const float Range = 3f;
        private const float Tolerance = 0.5f;
        private const float PossessTime = 1.2f;
        private const float Reenter = 10f;
        private const float LitDelay = 2f;
        private const float KickStun = 2.5f;

        private static readonly PlayerId HumanId = new PlayerId(0);
        private static readonly PlayerId GoodId = new PlayerId(1);
        private static readonly PlayerId EvilId = new PlayerId(2);
        private static readonly PlayerId OtherEvilId = new PlayerId(3);

        private static readonly NetId Chair = new NetId(40000);
        private static readonly NetId Lamp = new NetId(40001);
        private static readonly NetId Cologne = new NetId(40002);
        private static readonly NetId Stool = new NetId(40003);

        private double _now;
        private PlayerId _local;
        private OfflineNetBridge _bridge;
        private FakePossessionWorld _world;
        private PossessionAuthority _authority;
        private PossessionMirror _mirror;
        private readonly List<NetMsg> _broadcasts = new List<NetMsg>();
        private readonly List<PossessionChangedEvt> _events = new List<PossessionChangedEvt>();
        private readonly List<PossessDenyReason> _denials = new List<PossessDenyReason>();

        [SetUp]
        public void SetUp()
        {
            EventBus.ClearAll();
            _now = 100d;
            _local = EvilId;
            _bridge = new OfflineNetBridge(() => _now, () => _local);

            _world = new FakePossessionWorld();
            _world.SetRole(HumanId, Role.Human);
            _world.SetRole(GoodId, Role.GoodJinn);
            _world.SetRole(EvilId, Role.EvilJinn);
            _world.SetRole(OtherEvilId, Role.EvilJinn);

            _authority = new PossessionAuthority(_bridge, _world, new PossessionRules(Range, Tolerance, PossessTime, Reenter), LitDelay, KickStun);
            _mirror = new PossessionMirror(_bridge);
            _authority.Attach();
            _mirror.Attach();

            _authority.Register(Chair, default);
            _authority.Register(Lamp, new PossessableTraits(true, false));
            _authority.Register(Cologne, new PossessableTraits(false, true));
            _authority.Register(Stool, default);

            _broadcasts.Clear();
            _events.Clear();
            _denials.Clear();
            foreach (MsgCode code in new[] { MsgCode.PossessBegan, MsgCode.PossessCompleted, MsgCode.PossessEnded, MsgCode.PossessDenied })
                _bridge.Register(code, _broadcasts.Add);
            EventBus.Subscribe<PossessionChangedEvt>(_events.Add);
            _mirror.Denied += (player, obj, reason) => _denials.Add(reason);
        }

        [TearDown]
        public void TearDown()
        {
            _authority.Detach();
            _mirror.Detach();
            EventBus.ClearAll();
        }

        // ---------- helpers ----------

        private void RequestAs(PlayerId who, NetId obj)
        {
            _local = who;
            _bridge.SendToAuthority(ReqPossessMsg.Encode(obj));
        }

        private void UnpossessAs(PlayerId who)
        {
            _local = who;
            _bridge.SendToAuthority(ReqUnpossessMsg.Encode());
        }

        private void Tick(double dt)
        {
            _now += dt;
            _authority.TickAuthority(_now);
        }

        /// <summary>Request + wait out PossessTime: the jinn is inside (Lurking).</summary>
        private void EnterFully(PlayerId who, NetId obj)
        {
            RequestAs(who, obj);
            Assert.AreEqual(PossessionPhase.Entering, _mirror.PhaseOf(obj), "precondition: entering");
            Tick(PossessTime);
            Assert.AreEqual(PossessionPhase.Lurking, _mirror.PhaseOf(obj), "precondition: inside");
            Clear();
        }

        private void Clear()
        {
            _broadcasts.Clear();
            _events.Clear();
            _denials.Clear();
        }

        private List<NetMsg> Sent(MsgCode code)
        {
            return _broadcasts.FindAll(m => m.Code == code);
        }

        private PossessEndReason LastEndReason()
        {
            List<NetMsg> ended = Sent(MsgCode.PossessEnded);
            Assert.IsNotEmpty(ended, "no PossessEnded was sent");
            Assert.IsTrue(PossessEndedMsg.TryDecode(ended[ended.Count - 1], out _, out _, out PossessEndReason reason, out _));
            return reason;
        }

        // ---------- entering ----------

        [Test]
        public void Request_BeginsEntering_ThenCompletesAfterPossessTime()
        {
            RequestAs(EvilId, Chair);

            Assert.AreEqual(1, Sent(MsgCode.PossessBegan).Count);
            Assert.IsTrue(PossessBeganMsg.TryDecode(Sent(MsgCode.PossessBegan)[0], out PlayerId who, out NetId obj, out double end));
            Assert.AreEqual(EvilId, who);
            Assert.AreEqual(Chair, obj);
            Assert.AreEqual(_now + PossessTime, end, 1e-9);

            Assert.AreEqual(PossessionPhase.Entering, _mirror.PhaseOf(Chair));
            Assert.AreEqual(EvilId, _mirror.PossessorOf(Chair));
            Assert.IsTrue(_mirror.IsPossessed(Chair), "entering counts as possessed");
            Assert.IsTrue(_mirror.TryGetObjectOf(EvilId, out NetId inside));
            Assert.AreEqual(Chair, inside);
            Assert.AreEqual(1, _events.Count);
            Assert.AreEqual(PossessionPhase.Entering, _events[0].State);

            Tick(PossessTime * 0.5);
            Assert.AreEqual(0, Sent(MsgCode.PossessCompleted).Count, "not yet");
            Assert.AreEqual(PossessionPhase.Entering, _mirror.PhaseOf(Chair));

            Tick(PossessTime * 0.5);
            Assert.AreEqual(1, Sent(MsgCode.PossessCompleted).Count);
            Assert.AreEqual(PossessionPhase.Lurking, _mirror.PhaseOf(Chair));
            Assert.AreEqual(PossessionPhase.Lurking, _events[_events.Count - 1].State);
            Assert.AreEqual(EvilId, _events[_events.Count - 1].Player);
        }

        [Test]
        public void SecondRequest_ForAnOccupiedObject_IsDenied()
        {
            RequestAs(EvilId, Chair);
            Clear();

            RequestAs(OtherEvilId, Chair);

            Assert.AreEqual(0, Sent(MsgCode.PossessBegan).Count);
            Assert.AreEqual(1, Sent(MsgCode.PossessDenied).Count);
            CollectionAssert.AreEqual(new[] { PossessDenyReason.Occupied }, _denials);
            Assert.AreEqual(EvilId, _mirror.PossessorOf(Chair));
        }

        [Test]
        public void Human_IsRefused_AndTheDenialNamesTheRequester()
        {
            RequestAs(HumanId, Chair);

            Assert.IsTrue(PossessDeniedMsg.TryDecode(Sent(MsgCode.PossessDenied)[0], out PlayerId who, out NetId obj, out PossessDenyReason reason));
            Assert.AreEqual(HumanId, who);
            Assert.AreEqual(Chair, obj);
            Assert.AreEqual(PossessDenyReason.RoleNotAllowed, reason);
            Assert.AreEqual(PossessionPhase.Free, _mirror.PhaseOf(Chair));
            Assert.AreEqual(0, _events.Count);
        }

        [Test]
        public void GoodJinn_EntersOnlyObjectsThatAllowIt()
        {
            RequestAs(GoodId, Chair);
            CollectionAssert.AreEqual(new[] { PossessDenyReason.GoodJinnNotAllowed }, _denials);

            RequestAs(GoodId, Cologne);
            Assert.AreEqual(PossessionPhase.Entering, _mirror.PhaseOf(Cologne));
        }

        [Test]
        public void UnknownObject_IsDenied()
        {
            RequestAs(EvilId, new NetId(49999));
            CollectionAssert.AreEqual(new[] { PossessDenyReason.InvalidTarget }, _denials);
        }

        [Test]
        public void MalformedRequest_IsIgnored()
        {
            _local = EvilId;
            _bridge.SendToAuthority(new NetMsg { Code = MsgCode.ReqPossess, Payload = new object[] { "chair" } });
            Assert.AreEqual(0, _broadcasts.Count);
        }

        // ---------- leaving ----------

        [Test]
        public void Unpossess_Leaves_AndStartsTheReenterCooldown()
        {
            EnterFully(EvilId, Chair);

            UnpossessAs(EvilId);

            Assert.AreEqual(PossessEndReason.Voluntary, LastEndReason());
            Assert.AreEqual(PossessionPhase.Free, _mirror.PhaseOf(Chair));
            Assert.IsFalse(_mirror.TryGetObjectOf(EvilId, out _));
            Assert.AreEqual(PossessionPhase.Free, _events[_events.Count - 1].State);

            Clear();
            RequestAs(EvilId, Chair);
            CollectionAssert.AreEqual(new[] { PossessDenyReason.Cooldown }, _denials, "same object inside ReenterCooldown");

            RequestAs(EvilId, Stool);
            Assert.AreEqual(PossessionPhase.Entering, _mirror.PhaseOf(Stool), "another object is fine");
        }

        [Test]
        public void Unpossess_WhileEntering_AlsoLeaves()
        {
            RequestAs(EvilId, Chair);
            Clear();

            UnpossessAs(EvilId);

            Assert.AreEqual(PossessEndReason.Voluntary, LastEndReason());
            Tick(PossessTime * 2);
            Assert.AreEqual(0, Sent(MsgCode.PossessCompleted).Count, "an abandoned entry never completes");
        }

        [Test]
        public void Unpossess_WhenNotInside_SendsNothing()
        {
            UnpossessAs(EvilId);
            Assert.AreEqual(0, _broadcasts.Count);
            Assert.IsFalse(_authority.ProcessUnpossessAuthority(EvilId));
        }

        // ---------- light ----------

        [Test]
        public void LitRoom_RefusesTheEvilJinn_ButNotForALamp()
        {
            _world.DefaultLight = RoomLightState.On;

            RequestAs(EvilId, Chair);
            CollectionAssert.AreEqual(new[] { PossessDenyReason.Lit }, _denials);

            RequestAs(EvilId, Lamp);
            Assert.AreEqual(PossessionPhase.Entering, _mirror.PhaseOf(Lamp));
        }

        [Test]
        public void RoomLitWhileInside_EjectsAfterTheDelay()
        {
            EnterFully(EvilId, Chair);
            _world.SetLight(Chair, RoomLightState.On);

            Tick(0d);
            Tick(LitDelay * 0.5);
            Assert.AreEqual(0, Sent(MsgCode.PossessEnded).Count, "still inside during the delay");

            Tick(LitDelay * 0.5);
            Assert.AreEqual(PossessEndReason.RoomLit, LastEndReason());
            Assert.AreEqual(PossessionPhase.Free, _mirror.PhaseOf(Chair));
        }

        [Test]
        public void LightGoingOffAgain_ResetsTheDelay()
        {
            EnterFully(EvilId, Chair);
            _world.SetLight(Chair, RoomLightState.On);
            Tick(0d);
            Tick(LitDelay * 0.75);

            _world.SetLight(Chair, RoomLightState.Off);
            Tick(0.1d);
            _world.SetLight(Chair, RoomLightState.On);
            Tick(0d);
            Tick(LitDelay * 0.75);

            Assert.AreEqual(0, Sent(MsgCode.PossessEnded).Count, "the delay restarted when the light came back");
        }

        [Test]
        public void LitRoom_DoesNotEjectFromALamp_OrTheGoodJinn()
        {
            EnterFully(EvilId, Lamp);
            EnterFully(GoodId, Cologne);
            _world.DefaultLight = RoomLightState.On;

            Tick(0d);
            Tick(LitDelay * 3);

            Assert.AreEqual(0, Sent(MsgCode.PossessEnded).Count);
            Assert.AreEqual(PossessionPhase.Lurking, _mirror.PhaseOf(Lamp));
            Assert.AreEqual(PossessionPhase.Lurking, _mirror.PhaseOf(Cologne));
        }

        // ---------- kick ----------

        [Test]
        public void Kick_Ejects_AndStunsTheJinnForKickStun()
        {
            EnterFully(EvilId, Chair);

            Assert.AreEqual(EvilId, _authority.KickAuthority(Chair, HumanId));
            Assert.AreEqual(PossessEndReason.Kicked, LastEndReason());
            Assert.IsTrue(_authority.IsKickLocked(EvilId, _now));

            Clear();
            RequestAs(EvilId, Stool);
            CollectionAssert.AreEqual(new[] { PossessDenyReason.Stunned }, _denials);

            Tick(KickStun);
            Clear();
            RequestAs(EvilId, Stool);
            Assert.AreEqual(PossessionPhase.Entering, _mirror.PhaseOf(Stool), "stun over");
        }

        [Test]
        public void Kick_OnAnEmptyObject_DoesNothing()
        {
            Assert.AreEqual(PlayerId.None, _authority.KickAuthority(Chair, HumanId));
            Assert.AreEqual(0, _broadcasts.Count);
            Assert.IsFalse(_authority.IsKickLocked(EvilId, _now));
        }

        // ---------- single use / removal / round ----------

        [Test]
        public void SingleUseAction_TurnsTheObjectSpent_AndItCannotBeEnteredAgain()
        {
            EnterFully(EvilId, Chair);
            Assert.IsTrue(_authority.Arbiter.TryGetMachine(Chair, out PossessableStateMachine sm));
            Assert.IsTrue(sm.BeginCharge(0, _now));
            Assert.IsTrue(sm.Resolve(true, _now + 0.5));

            Tick(0.5);

            Assert.IsTrue(PossessEndedMsg.TryDecode(Sent(MsgCode.PossessEnded)[0], out PlayerId who, out _, out PossessEndReason reason, out bool spent));
            Assert.AreEqual(EvilId, who);
            Assert.AreEqual(PossessEndReason.Spent, reason);
            Assert.IsTrue(spent);
            Assert.AreEqual(PossessionPhase.Spent, _mirror.PhaseOf(Chair));

            Clear();
            RequestAs(OtherEvilId, Chair);
            CollectionAssert.AreEqual(new[] { PossessDenyReason.Spent }, _denials);
        }

        [Test]
        public void Unregister_WhileOccupied_ForcesTheJinnOut()
        {
            EnterFully(EvilId, Chair);

            _authority.Unregister(Chair);

            Assert.AreEqual(PossessEndReason.Forced, LastEndReason());
            Assert.IsFalse(_mirror.TryGetObjectOf(EvilId, out _));
        }

        [Test]
        public void NewRound_FreesEverything_ClearsCooldownsAndStuns_AndTellsListeners()
        {
            EnterFully(EvilId, Chair);
            EnterFully(OtherEvilId, Stool);
            _authority.KickAuthority(Stool, HumanId);
            Clear();

            EventBus.Publish(new RoundStartedEvt(1, 1234));

            Assert.AreEqual(PossessionPhase.Free, _mirror.PhaseOf(Chair));
            Assert.IsFalse(_mirror.TryGetObjectOf(EvilId, out _));
            Assert.AreEqual(1, _events.Count, "one Free event for the jinn that was still inside");
            Assert.AreEqual(EvilId, _events[0].Player);
            Assert.AreEqual(PossessionPhase.Free, _events[0].State);
            Assert.AreEqual(0, _broadcasts.Count, "the reset itself sends nothing");

            RequestAs(OtherEvilId, Stool);
            Assert.AreEqual(PossessionPhase.Entering, _mirror.PhaseOf(Stool), "stun and cooldown were cleared");
            RequestAs(EvilId, Chair);
            Assert.AreEqual(PossessionPhase.Entering, _mirror.PhaseOf(Chair));
        }

        // ---------- mirror phases (action runner hook) ----------

        [Test]
        public void SetPhase_MovesBetweenInsidePhasesOnly()
        {
            RequestAs(EvilId, Chair);
            Assert.IsFalse(_mirror.SetPhase(Chair, PossessionPhase.Charging), "not inside yet");
            Tick(PossessTime);
            Clear();

            Assert.IsTrue(_mirror.SetPhase(Chair, PossessionPhase.Charging));
            Assert.AreEqual(PossessionPhase.Charging, _mirror.PhaseOf(Chair));
            Assert.AreEqual(1, _events.Count);
            Assert.IsFalse(_mirror.SetPhase(Chair, PossessionPhase.Free), "leaving goes through PossessEnded");
            Assert.IsTrue(PossessionMirror.IsInside(_mirror.PhaseOf(Chair)));
        }
    }
}
