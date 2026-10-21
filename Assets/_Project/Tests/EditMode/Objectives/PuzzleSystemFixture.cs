using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.Core.Events;
using CinliMahzen.Core.Net;
using CinliMahzen.Objectives;
using NUnit.Framework;
using UnityEngine;

namespace CinliMahzen.Tests.EditMode.Objectives
{
    /// <summary>
    /// Shared world for the PuzzleSystemCore tests: an offline bridge on a fake clock (the local player is whoever a test says, so a
    /// SendToAuthority request arrives "from" that player), four players with roles, the stub bridges in GameServices (all settable),
    /// one Paintings puzzle in slot 0 at the origin, pawns 1-2 m away, and recorders for broadcasts, events and damage.
    /// Slot 0: 3 parts x 4 values, Cycle, solution { 1, 3, 0 }, room 0, stage 1.
    /// </summary>
    public abstract class PuzzleSystemFixture
    {
        protected const int Slot = 0;
        protected const int SlotStage = 1;

        protected static readonly PlayerId HumanId = new PlayerId(0);
        protected static readonly PlayerId GoodJinnId = new PlayerId(1);
        protected static readonly PlayerId EvilId = new PlayerId(2);
        protected static readonly PlayerId OtherEvilId = new PlayerId(3);

        protected static readonly int[] PaintingsCounts = { 4, 4, 4 };
        protected static readonly int[] PaintingsSolution = { 1, 3, 0 };

        protected sealed class FakeDamageable : IDamageable
        {
            public readonly List<DamageInfo> Hits = new List<DamageInfo>();

            public NetId NetId => new NetId(1);

            public void ApplyDamageAuthority(in DamageInfo info)
            {
                Hits.Add(info);
            }
        }

        protected double Now;
        protected PlayerId LocalPlayer;
        protected OfflineNetBridge Bridge;
        protected OfflinePlayerRegistry Players;
        protected GameBalanceConfig Config;
        protected StubRoomService Rooms;
        protected StubHumanStatusQuery HumanStatus;
        protected StubMatchInfo MatchInfo;
        protected StubJinnEnergyService EnergyService;
        protected StubInventoryQuery Inventory;
        protected FakeDamageable HumanBody;
        protected PuzzleSystemCore Puzzles;
        protected PuzzleSlotRuntime Runtime;

        /// <summary>Pawn positions by owner; remove an entry to simulate a player without a pawn.</summary>
        protected readonly Dictionary<PlayerId, Vector3> PawnPositions = new Dictionary<PlayerId, Vector3>();

        /// <summary>False simulates "no damageable human pawn in the scene".</summary>
        protected bool HasHumanBody = true;

        protected readonly List<NetMsg> Broadcasts = new List<NetMsg>();
        protected readonly List<PuzzleStateChangedEvt> StateChangedEvents = new List<PuzzleStateChangedEvt>();
        protected readonly List<PuzzleScrambledEvt> ScrambledEvents = new List<PuzzleScrambledEvt>();
        protected readonly List<PuzzleCompletedEvt> CompletedEvents = new List<PuzzleCompletedEvt>();

        [SetUp]
        public void SetUpWorld()
        {
            GameServices.ResetToStubs();
            EventBus.ClearAll();

            Now = 100d;
            LocalPlayer = HumanId;
            Bridge = new OfflineNetBridge(() => Now, () => LocalPlayer);
            GameServices.Register<INetBridge>(Bridge);

            Players = new OfflinePlayerRegistry();
            Players.SetRoles(new[] { Role.Human, Role.GoodJinn, Role.EvilJinn, Role.EvilJinn });

            Config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            GameServices.Register(Config);

            Rooms = new StubRoomService();
            HumanStatus = new StubHumanStatusQuery { SafeZoneRadius = Config.LanternSafeZoneRadius };
            MatchInfo = new StubMatchInfo();
            EnergyService = new StubJinnEnergyService();
            Inventory = new StubInventoryQuery();
            GameServices.Register<IRoomService>(Rooms);
            GameServices.Register<IHumanStatusQuery>(HumanStatus);
            GameServices.Register<IMatchInfo>(MatchInfo);
            GameServices.Register<IJinnEnergyService>(EnergyService);
            GameServices.Register<IInventoryQuery>(Inventory);

            HumanBody = new FakeDamageable();
            PawnPositions[HumanId] = new Vector3(1f, 0f, 0f);
            PawnPositions[GoodJinnId] = new Vector3(0f, 0f, 1f);
            PawnPositions[EvilId] = new Vector3(2f, 0f, 0f);
            PawnPositions[OtherEvilId] = new Vector3(0f, 0f, 2f);
            HasHumanBody = true;

            Puzzles = new PuzzleSystemCore(Bridge, Config, Players, PawnPositionOf, DamageableOf);
            Puzzles.Attach();
            Runtime = NewPaintings(Vector3.zero, SlotStage);
            Puzzles.RegisterSlot(Slot, Runtime);

            Broadcasts.Clear();
            StateChangedEvents.Clear();
            ScrambledEvents.Clear();
            CompletedEvents.Clear();
            foreach (MsgCode code in new[] { MsgCode.PuzzleStateChanged, MsgCode.PuzzleScrambled, MsgCode.PuzzleConfirmResult, MsgCode.PuzzleCompleted })
            {
                Bridge.Register(code, Broadcasts.Add);
            }
            EventBus.Subscribe<PuzzleStateChangedEvt>(StateChangedEvents.Add);
            EventBus.Subscribe<PuzzleScrambledEvt>(ScrambledEvents.Add);
            EventBus.Subscribe<PuzzleCompletedEvt>(CompletedEvents.Add);
        }

        [TearDown]
        public void TearDownWorld()
        {
            Puzzles.Detach();
            EventBus.ClearAll();
            GameServices.ResetToStubs();
            Object.DestroyImmediate(Config);
        }

        // ---------- builders ----------

        protected static PuzzleState NewPaintingsState()
        {
            return new PuzzleState(PaintingsCounts, PaintingsSolution, PuzzleOpKind.Cycle);
        }

        protected static PuzzleSlotRuntime NewPaintings(Vector3 position, int stage, string requiredItemId = "")
        {
            return new PuzzleSlotRuntime(NewPaintingsState(), requiredItemId, position, 0, stage);
        }

        /// <summary>A Sigil-like puzzle: 2 lines x painted/empty, Set only, needs paint. Solution { 1, 0 }.</summary>
        protected static PuzzleSlotRuntime NewPaintedPuzzle(Vector3 position)
        {
            var state = new PuzzleState(new[] { 2, 2 }, new[] { 1, 0 }, PuzzleOpKind.Set);
            return new PuzzleSlotRuntime(state, "paint", position, 0, SlotStage);
        }

        // ---------- helpers ----------

        protected Vector3? PawnPositionOf(PlayerId who)
        {
            return PawnPositions.TryGetValue(who, out Vector3 p) ? p : (Vector3?)null;
        }

        protected IDamageable DamageableOf(PlayerId who)
        {
            return HasHumanBody ? HumanBody : null;
        }

        /// <summary>Sends a request through the bridge as <paramref name="who"/> (the message's Sender).</summary>
        protected void SendAs(PlayerId who, NetMsg request)
        {
            LocalPlayer = who;
            Bridge.SendToAuthority(request);
        }

        /// <summary>The broadcasts of one code, oldest first.</summary>
        protected List<NetMsg> Sent(MsgCode code)
        {
            return Broadcasts.FindAll(m => m.Code == code);
        }

        protected int[] Values()
        {
            var values = new int[Runtime.State.PartCount];
            Runtime.State.CopyValuesTo(values);
            return values;
        }

        /// <summary>Moves part 0 off its solution value so the puzzle is no longer solved.</summary>
        protected void BreakThePuzzle()
        {
            Assert.IsTrue(Runtime.State.TryApply(PuzzleOp.Cycle(0, 1)));
            Assert.IsFalse(Runtime.State.IsSolved);
        }

        protected void ClearRecordings()
        {
            Broadcasts.Clear();
            StateChangedEvents.Clear();
            ScrambledEvents.Clear();
            CompletedEvents.Clear();
            HumanBody.Hits.Clear();
        }
    }
}
