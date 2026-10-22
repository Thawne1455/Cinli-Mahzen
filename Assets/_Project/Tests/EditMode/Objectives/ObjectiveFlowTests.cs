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
    /// TODO 2.7: <see cref="ObjectiveFlowCore"/> on the shared puzzle world. Stage 1 has two puzzles: slot 0 (the fixture's Paintings, reward
    /// "paint") and slot 1 (a second Paintings, no reward); stage 2 has slot 2 (reward "shovel"). A fake scene records spawns, despawns
    /// and opened gates; StageCompletedEvt is recorded too (the MatchController adds the time bonus from it).
    /// </summary>
    public class ObjectiveFlowTests : PuzzleSystemFixture
    {
        private const int SlotB = 1;
        private const int SlotC = 2;
        private static readonly Vector3 RewardA = new Vector3(0f, 1f, 1f);
        private static readonly Vector3 RewardC = new Vector3(5f, 1f, 0f);

        internal sealed class FakeScene : IObjectiveScene
        {
            public readonly Dictionary<NetId, string> Items = new Dictionary<NetId, string>();
            public readonly Dictionary<NetId, Vector3> Positions = new Dictionary<NetId, Vector3>();
            public readonly List<int> OpenedGates = new List<int>();
            public readonly List<NetId> Despawned = new List<NetId>();
            public readonly List<NetId> KeysFound = new List<NetId>();
            public readonly List<NetId> ShedTaken = new List<NetId>();
            public System.Func<PlayerId, Vector3?> Pawn;

            public void SpawnItem(NetId item, string itemId, Vector3 position)
            {
                Items[item] = itemId;
                Positions[item] = position;
            }

            public void DespawnItem(NetId item)
            {
                Items.Remove(item);
                Despawned.Add(item);
            }

            public void OpenGate(int stage) => OpenedGates.Add(stage);

            public void OnKeyFound(NetId spot) => KeysFound.Add(spot);

            public void OnToolShedItemTaken(NetId point) => ShedTaken.Add(point);

            public Vector3? PawnPosition(PlayerId player) => Pawn(player);
        }

        private FakeScene _scene;
        private ObjectiveFlowCore _flow;
        private int _nextId;
        private readonly List<StageCompletedEvt> _stages = new List<StageCompletedEvt>();
        private readonly List<NetMsg> _flowMsgs = new List<NetMsg>();

        [SetUp]
        public void SetUpFlow()
        {
            _nextId = NetId.RuntimeMin;
            _scene = new FakeScene { Pawn = PawnPositionOf };
            _flow = new ObjectiveFlowCore(Bridge, Puzzles, Players, Config.ItemSlotCount, _scene, () => new NetId(_nextId++));
            _flow.Attach();

            Puzzles.RegisterSlot(SlotB, NewPaintings(new Vector3(0f, 0f, 0.5f), SlotStage));
            Puzzles.RegisterSlot(SlotC, NewPaintings(new Vector3(0f, 0f, -0.5f), 2));
            _flow.BeginRound(new[]
            {
                new ObjectivePuzzleInfo(Slot, SlotStage, "paint", RewardA),
                new ObjectivePuzzleInfo(SlotB, SlotStage, "", Vector3.zero),
                new ObjectivePuzzleInfo(SlotC, 2, "shovel", RewardC),
            }, 2);

            _stages.Clear();
            _flowMsgs.Clear();
            EventBus.Subscribe<StageCompletedEvt>(_stages.Add);
            foreach (MsgCode code in new[] { MsgCode.GateOpened, MsgCode.ItemSpawned, MsgCode.ItemDropped, MsgCode.ItemPickedUp, MsgCode.InventoryChanged })
                Bridge.Register(code, _flowMsgs.Add);
        }

        [TearDown]
        public void TearDownFlow()
        {
            _flow.Detach();
        }

        private List<NetMsg> FlowSent(MsgCode code)
        {
            return _flowMsgs.FindAll(m => m.Code == code);
        }

        private void Complete(int slot)
        {
            Assert.AreEqual(PuzzleDenyReason.None, Puzzles.ProcessConfirmAuthority(HumanId, slot), "confirm slot " + slot);
        }

        private NetId OnlyGroundItem(string itemId)
        {
            foreach (KeyValuePair<NetId, string> pair in _flow.GroundItems)
            {
                if (pair.Value == itemId)
                    return pair.Key;
            }
            Assert.Fail(itemId + " is not on the ground");
            return NetId.Invalid;
        }

        // ---------- start ----------

        [Test]
        public void BeginRound_StartsInTheGarden()
        {
            Assert.AreEqual(0, Puzzles.StageIndex);
            Assert.AreEqual(3, Puzzles.StageCount, "garden + 2 puzzle stages");
            Assert.AreEqual(ObjectivePhase.Garden, Puzzles.Phase);
            Assert.IsFalse(_flow.IsGateOpen(0));
        }

        // ---------- rewards and gates ----------

        [Test]
        public void CompletingARewardPuzzle_SpawnsItsItemAtTheRewardPoint_ButTheGateWaits()
        {
            Complete(Slot);

            Assert.AreEqual(1, FlowSent(MsgCode.ItemSpawned).Count);
            NetId paint = OnlyGroundItem("paint");
            Assert.GreaterOrEqual(paint.Value, NetId.RuntimeMin);
            Assert.AreEqual(RewardA, _scene.Positions[paint]);
            Assert.AreEqual(0, FlowSent(MsgCode.GateOpened).Count, "the other stage 1 puzzle is still open");
        }

        [Test]
        public void CompletingTheLastPuzzleOfAStage_OpensItsGate_AndAdvancesTheStage()
        {
            Complete(Slot);
            Complete(SlotB);

            Assert.AreEqual(1, FlowSent(MsgCode.GateOpened).Count);
            CollectionAssert.AreEqual(new[] { SlotStage }, _scene.OpenedGates);
            Assert.AreEqual(1, _stages.Count);
            Assert.AreEqual(SlotStage, _stages[0].Stage);
            Assert.AreEqual(SlotStage + 1, Puzzles.StageIndex);
            Assert.AreEqual(ObjectivePhase.House, Puzzles.Phase);
            Assert.IsTrue(_flow.IsGateOpen(SlotStage));
        }

        [Test]
        public void TheLastStagesGate_LeadsToTheCellar()
        {
            Complete(SlotC);

            Assert.AreEqual(3, Puzzles.StageIndex);
            Assert.AreEqual(ObjectivePhase.Cellar, Puzzles.Phase);
            Assert.IsNotNull(OnlyGroundItem("shovel"));
        }

        [Test]
        public void ARewardlessPuzzle_SpawnsNothing()
        {
            Complete(SlotB);
            Assert.AreEqual(0, FlowSent(MsgCode.ItemSpawned).Count);
        }

        [Test]
        public void CompleteCurrentStage_OpensTheGardenGateFirst_ThenRefusesAgain()
        {
            Assert.IsTrue(_flow.CompleteCurrentStageAuthority());
            Assert.AreEqual(1, Puzzles.StageIndex);
            Assert.AreEqual(ObjectivePhase.House, Puzzles.Phase);
            CollectionAssert.AreEqual(new[] { 0 }, _scene.OpenedGates);

            Assert.IsFalse(_flow.OpenGateAuthority(0), "already open");
            Assert.AreEqual(1, _stages.Count);
        }

        // ---------- inventory ----------

        [Test]
        public void HumanPicksUpTheReward_ItLeavesTheGround_AndTheInventoryHasIt()
        {
            Complete(Slot);
            NetId paint = OnlyGroundItem("paint");

            Assert.IsTrue(_flow.PickupAuthority(HumanId, paint));

            Assert.IsTrue(_flow.HasItem(HumanId, "paint"));
            Assert.AreEqual(0, _flow.GroundItems.Count);
            CollectionAssert.Contains(_scene.Despawned, paint);
            Assert.AreEqual(1, FlowSent(MsgCode.ItemPickedUp).Count);
            Assert.AreEqual(1, FlowSent(MsgCode.InventoryChanged).Count);
            Assert.IsFalse(_flow.PickupAuthority(HumanId, paint), "already taken");
        }

        [Test]
        public void SolvingAPuzzle_UsesUpTheItemItNeeded_OnceNoUnsolvedPuzzleNeedsItAnymore()
        {
            // Slot B (stage 1) and slot C (stage 2) both need the paint that slot A rewards.
            Puzzles.RegisterSlot(SlotB, NewPaintings(new Vector3(0f, 0f, 0.5f), SlotStage, "paint"));
            Puzzles.RegisterSlot(SlotC, NewPaintings(new Vector3(0f, 0f, -0.5f), 2, "paint"));
            Complete(Slot);
            Assert.IsTrue(_flow.PickupAuthority(HumanId, OnlyGroundItem("paint")));

            Complete(SlotB);
            Assert.IsTrue(_flow.HasItem(HumanId, "paint"), "slot C still needs the paint");

            _flowMsgs.Clear();
            Complete(SlotC);
            Assert.IsFalse(_flow.HasItem(HumanId, "paint"), "nothing needs it anymore: the slot is free again");
            Assert.AreEqual(1, FlowSent(MsgCode.InventoryChanged).Count);
        }

        [Test]
        public void SolvingAPuzzleThatNeedsNoItem_KeepsTheInventory()
        {
            Complete(Slot);
            Assert.IsTrue(_flow.PickupAuthority(HumanId, OnlyGroundItem("paint")));

            Complete(SlotB);

            Assert.IsTrue(_flow.HasItem(HumanId, "paint"));
        }

        [Test]
        public void JinnsCannotPickUpItems()
        {
            Complete(Slot);
            NetId paint = OnlyGroundItem("paint");

            Assert.IsFalse(_flow.PickupAuthority(EvilId, paint));
            Assert.IsFalse(_flow.PickupAuthority(GoodJinnId, paint));
            Assert.AreEqual(1, _flow.GroundItems.Count);
        }

        [Test]
        public void TheInventory_IsWhatThePuzzleSystemAsksForItems()
        {
            GameServices.Register<IInventoryQuery>(_flow);
            Runtime = NewPaintedPuzzle(Vector3.zero);
            Puzzles.RegisterSlot(7, Runtime);

            Assert.AreEqual(PuzzleDenyReason.MissingItem, Puzzles.ProcessOpAuthority(HumanId, 7, PuzzleOp.Set(0, 0)));

            Complete(Slot);
            Assert.IsTrue(_flow.PickupAuthority(HumanId, OnlyGroundItem("paint")));
            Assert.AreEqual(PuzzleDenyReason.None, Puzzles.ProcessOpAuthority(HumanId, 7, PuzzleOp.Set(0, 0)));
        }

        [Test]
        public void DropRequest_PutsTheItemAtTheHumansFeet_WithANewId()
        {
            Complete(Slot);
            NetId reward = OnlyGroundItem("paint");
            _flow.PickupAuthority(HumanId, reward);
            _flowMsgs.Clear();

            SendAs(HumanId, ReqDropItemMsg.Encode(0));

            Assert.IsFalse(_flow.HasItem(HumanId, "paint"));
            Assert.AreEqual(1, FlowSent(MsgCode.ItemDropped).Count);
            NetId dropped = OnlyGroundItem("paint");
            Assert.AreNotEqual(reward, dropped);
            Assert.AreEqual(PawnPositions[HumanId], _scene.Positions[dropped]);
        }

        [Test]
        public void DropRequest_ForAnEmptySlot_DoesNothing()
        {
            SendAs(HumanId, ReqDropItemMsg.Encode(1));
            Assert.AreEqual(0, _flowMsgs.Count);
        }

        [Test]
        public void FaintingHuman_DropsTheLastPickedItem()
        {
            Complete(Slot);
            Complete(SlotC);
            _flow.PickupAuthority(HumanId, OnlyGroundItem("paint"));
            _flow.PickupAuthority(HumanId, OnlyGroundItem("shovel"));

            EventBus.Publish(new HumanFaintedEvt(default(DamageInfo), 1));

            Assert.IsTrue(_flow.HasItem(HumanId, "paint"));
            Assert.IsFalse(_flow.HasItem(HumanId, "shovel"));
            Assert.IsNotNull(OnlyGroundItem("shovel"));
        }

        [Test]
        public void NewRound_EmptiesTheInventory_AndRemovesGroundItems()
        {
            Complete(Slot);
            NetId paint = OnlyGroundItem("paint");
            Complete(SlotC);
            _flow.PickupAuthority(HumanId, OnlyGroundItem("shovel"));

            _flow.BeginRound(new[] { new ObjectivePuzzleInfo(Slot, SlotStage, "", Vector3.zero) }, 2);

            Assert.IsFalse(_flow.HasItem(HumanId, "shovel"));
            Assert.AreEqual(0, _flow.GroundItems.Count);
            CollectionAssert.Contains(_scene.Despawned, paint);
            Assert.AreEqual(0, Puzzles.StageIndex);
            Assert.IsFalse(_flow.IsGateOpen(SlotStage));
        }

        [Test]
        public void DebugJson_DescribesStageGatesInventoryAndGround()
        {
            Complete(Slot);
            string json = _flow.BuildDebugJson();
            StringAssert.Contains("\"stage\":0", json);
            StringAssert.Contains("paint@", json);
        }

        // ---------- cellar (TODO 3.7) ----------

        private static readonly NetId DigAt = new NetId(1040);

        [Test]
        public void WithAHatch_TheLastStageGivesTheCellarKey_AndTheHatchOpensTheCellar_WithRage()
        {
            var rage = new List<NetMsg>();
            Bridge.Register(MsgCode.RageStarted, rage.Add);
            _flow.SetGarden(NetId.Invalid, new[] { new ObjectiveFlowCore.LockedGate(new NetId(1050), 3, ObjectiveFlowCore.CellarKeyItemId) }, null);
            _flow.SetCellar(DigAt, 3, 6f, true);

            Complete(SlotC);
            Assert.AreEqual(ObjectivePhase.House, Puzzles.Phase, "the hatch is still closed");
            NetId key = OnlyGroundItem(ObjectiveFlowCore.CellarKeyItemId);
            Assert.AreEqual(0, rage.Count);

            Assert.IsTrue(_flow.PickupAuthority(HumanId, key));
            _flow.UseGateAuthority(HumanId, new NetId(1050));

            Assert.AreEqual(ObjectivePhase.Cellar, Puzzles.Phase);
            Assert.AreEqual(1, rage.Count);
            Assert.IsTrue(CellarMsgs.TryDecodeRage(rage[0], out bool active));
            Assert.IsTrue(active);
        }

        [Test]
        public void Digging_NeedsTheShovelTheCellarAndTheRealSpot_ThreeLayers_ThenTreasure()
        {
            var dug = new List<TreasureDugEvt>();
            var progress = new List<DigProgressEvt>();
            EventBus.Subscribe<TreasureDugEvt>(dug.Add);
            EventBus.Subscribe<DigProgressEvt>(progress.Add);
            _flow.SetCellar(DigAt, 3, 6f, false);
            Assert.AreEqual(6f, _flow.DigLayerTime);

            Assert.IsFalse(_flow.CanDigAt(HumanId, DigAt), "the cellar is closed");
            Complete(SlotC);
            Assert.AreEqual(ObjectivePhase.Cellar, Puzzles.Phase, "no hatch: the last stage opens the cellar");
            Assert.IsFalse(_flow.CanDigAt(HumanId, DigAt), "no shovel yet");
            _flow.PickupAuthority(HumanId, OnlyGroundItem("shovel"));
            Assert.IsFalse(_flow.CanDigAt(HumanId, new NetId(1041)), "not the real spot");
            Assert.IsTrue(_flow.CanDigAt(HumanId, DigAt));

            _flow.DigAuthority(HumanId, DigAt);
            _flow.DigAuthority(HumanId, DigAt);
            Assert.AreEqual(2, _flow.DigLayers);
            Assert.AreEqual(2, Puzzles.DigCheckpoint);
            Assert.AreEqual(2f / 3f, Puzzles.DigProgress, 1e-5f);
            Assert.AreEqual(0, dug.Count);

            _flow.DigAuthority(HumanId, DigAt);
            Assert.AreEqual(1, dug.Count);
            Assert.AreEqual(HumanId, dug[0].Digger);
            Assert.AreEqual(ObjectivePhase.Done, Puzzles.Phase);
            Assert.AreEqual(3, progress.Count);
            Assert.IsFalse(_flow.CanDigAt(HumanId, DigAt), "dug up");
        }

        [Test]
        public void CellarCodecs_RoundTrip()
        {
            Assert.IsTrue(CellarMsgs.TryDecodeCheckpoint(CellarMsgs.EncodeCheckpoint(DigAt, 2), out NetId spot, out int layer));
            Assert.AreEqual(DigAt, spot);
            Assert.AreEqual(2, layer);
            Assert.IsTrue(CellarMsgs.TryDecodeTreasure(CellarMsgs.EncodeTreasure(HumanId), out PlayerId who));
            Assert.AreEqual(HumanId, who);
            Assert.IsTrue(CellarMsgs.TryDecodeRage(CellarMsgs.EncodeRage(false), out bool active));
            Assert.IsFalse(active);
        }

        // ---------- garden (TODO 3.6) ----------

        private static readonly NetId KeySpot = new NetId(1010);
        private static readonly NetId OtherSpot = new NetId(1011);
        private static readonly NetId FrontDoor = new NetId(1020);
        private static readonly NetId OilPoint = new NetId(1030);

        private void SetGarden()
        {
            _flow.SetGarden(KeySpot,
                new[] { new ObjectiveFlowCore.LockedGate(FrontDoor, 0, ObjectiveFlowCore.HouseKeyItemId) },
                new[] { new ObjectiveFlowCore.ShedItem(OilPoint, ObjectiveFlowCore.BlessedOilItemId) });
        }

        [Test]
        public void Search_FindsTheKeyOnlyInThePlannedSpot()
        {
            SetGarden();
            _flow.SearchSpotAuthority(HumanId, OtherSpot);
            Assert.IsFalse(_flow.HasItem(HumanId, ObjectiveFlowCore.HouseKeyItemId));

            _flow.SearchSpotAuthority(EvilId, KeySpot);
            Assert.IsFalse(_flow.IsKeyFound, "only the human searches");

            _flow.SearchSpotAuthority(HumanId, KeySpot);
            Assert.IsTrue(_flow.IsKeyFound);
            Assert.IsTrue(_flow.HasItem(HumanId, ObjectiveFlowCore.HouseKeyItemId));
            CollectionAssert.AreEqual(new[] { KeySpot }, _scene.KeysFound);

            _flow.SearchSpotAuthority(HumanId, KeySpot);
            Assert.AreEqual(1, _scene.KeysFound.Count, "found once");
        }

        [Test]
        public void FrontDoor_OpensWithTheKey_UsesItUp_AndAdvancesTheStage()
        {
            SetGarden();
            Assert.IsTrue(_flow.IsGateClosed(FrontDoor));
            _flow.UseGateAuthority(HumanId, FrontDoor);
            Assert.AreEqual(0, _stages.Count, "no key yet");

            _flow.SearchSpotAuthority(HumanId, KeySpot);
            _flow.UseGateAuthority(HumanId, FrontDoor);

            Assert.AreEqual(1, _stages.Count);
            Assert.AreEqual(0, _stages[0].Stage);
            Assert.AreEqual(1, Puzzles.StageIndex);
            Assert.IsFalse(_flow.IsGateClosed(FrontDoor));
            Assert.IsFalse(_flow.HasItem(HumanId, ObjectiveFlowCore.HouseKeyItemId), "the key stays in the lock");
        }

        [Test]
        public void BlessedOil_IsTakenOnce_AndPublishesTheEvent_WithoutASlot()
        {
            SetGarden();
            var collected = new List<BlessedOilCollectedEvt>();
            EventBus.Subscribe<BlessedOilCollectedEvt>(collected.Add);

            Assert.IsTrue(_flow.IsToolShedItemAvailable(OilPoint));
            _flow.CollectToolShedItemAuthority(HumanId, OilPoint);

            Assert.AreEqual(1, collected.Count);
            Assert.AreEqual(HumanId, collected[0].Collector);
            Assert.IsFalse(_flow.IsToolShedItemAvailable(OilPoint));
            CollectionAssert.AreEqual(new[] { OilPoint }, _scene.ShedTaken);
            Assert.IsFalse(_flow.HasItem(HumanId, ObjectiveFlowCore.BlessedOilItemId), "the oil takes no inventory slot");

            _flow.CollectToolShedItemAuthority(HumanId, OilPoint);
            Assert.AreEqual(1, collected.Count);
        }

        [Test]
        public void KeyWithAFullInventory_LandsAtTheHumansFeet()
        {
            SetGarden();
            _flow.Inventory.TryAdd(HumanId, "a");
            _flow.Inventory.TryAdd(HumanId, "b");
            _flow.Inventory.TryAdd(HumanId, "c");

            _flow.SearchSpotAuthority(HumanId, KeySpot);

            Assert.IsNotNull(OnlyGroundItem(ObjectiveFlowCore.HouseKeyItemId));
        }

        [Test]
        public void GardenCodecs_RoundTrip()
        {
            Assert.IsTrue(KeyFoundMsg.TryDecode(KeyFoundMsg.Encode(KeySpot, HumanId), out NetId spot, out PlayerId who));
            Assert.AreEqual(KeySpot, spot);
            Assert.AreEqual(HumanId, who);
            Assert.IsTrue(BlessedOilCollectedMsg.TryDecode(BlessedOilCollectedMsg.Encode(OilPoint, HumanId), out NetId point, out PlayerId by));
            Assert.AreEqual(OilPoint, point);
            Assert.AreEqual(HumanId, by);
        }
    }
}
