using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using CinliMahzen.Core;
using CinliMahzen.Core.Events;
using CinliMahzen.Core.Net;
using UnityEngine;

namespace CinliMahzen.Objectives
{
    /// <summary>One planned puzzle as the round flow sees it: which stage it belongs to and what it rewards where.</summary>
    public readonly struct ObjectivePuzzleInfo
    {
        public readonly int Slot;
        public readonly int Stage;
        public readonly string RewardItemId;
        public readonly Vector3 RewardPosition;

        public ObjectivePuzzleInfo(int slot, int stage, string rewardItemId, Vector3 rewardPosition)
        {
            Slot = slot;
            Stage = stage;
            RewardItemId = rewardItemId ?? string.Empty;
            RewardPosition = rewardPosition;
        }
    }

    /// <summary>
    /// The round's progression after the puzzles (Teknik §5.4, §7.3, TODO 2.7), plain C#: rewards, stage gates and the human's quest-item
    /// inventory. It is also the scene's <see cref="IInventoryQuery"/>.
    /// <code>
    /// PuzzleCompletedEvt (authority) ─► reward? ItemSpawned{runtime id, item, reward point}
    ///                                 └► every puzzle of the stage done? GateOpened{stage}
    /// GateOpened (every machine)      ─► door opens, StageIndex = stage + 1, StageCompletedEvt (the MatchController adds StageTimeBonus)
    /// ItemPickup.ExecuteAuthority     ─► PickupAuthority: ItemPickedUp + InventoryChanged
    /// ReqDropItem{slot} / human faints ─► InventoryChanged + ItemDropped{new runtime id, item, at the human}
    /// </code>
    /// <see cref="BeginRound"/> runs on every machine at the same round start (from the same plan), so all of them hold the same puzzle list.
    /// Stage 0 (garden, front door) has no puzzles: its gate opens with the house key (TODO 3.6) or <see cref="CompleteCurrentStageAuthority"/>.
    /// </summary>
    public sealed class ObjectiveFlowCore : IInventoryQuery, IObjectiveDebugCommands
    {
        private const string Category = "Objectives";

        private readonly INetBridge _net;
        private readonly PuzzleSystemCore _puzzles;
        private readonly IPlayerRegistry _players;
        private readonly IObjectiveScene _scene;
        private readonly Func<NetId> _allocateId;
        private readonly InventoryState _inventory;
        private readonly List<ObjectivePuzzleInfo> _round = new List<ObjectivePuzzleInfo>(8);
        private readonly HashSet<int> _completedSlots = new HashSet<int>();
        private readonly HashSet<int> _openedGates = new HashSet<int>();
        private readonly Dictionary<NetId, string> _groundItems = new Dictionary<NetId, string>();
        private readonly List<NetId> _scratch = new List<NetId>(8);
        private int _stagesActive;

        private readonly Action<NetMsg> _onGateOpened;
        private readonly Action<NetMsg> _onItemSpawned;
        private readonly Action<NetMsg> _onItemPickedUp;
        private readonly Action<NetMsg> _onInventoryChanged;
        private readonly Action<NetMsg> _onReqDrop;
        private readonly Action<PuzzleCompletedEvt> _onPuzzleCompleted;
        private readonly Action<HumanFaintedEvt> _onHumanFainted;
        private bool _attached;

        /// <param name="allocateId">Authority: a fresh runtime NetId for a ground item (IEntityRegistry.AllocateRuntimeId).</param>
        public ObjectiveFlowCore(INetBridge net, PuzzleSystemCore puzzles, IPlayerRegistry players, int itemSlotCount,
            IObjectiveScene scene, Func<NetId> allocateId)
        {
            _net = net ?? throw new ArgumentNullException(nameof(net));
            _puzzles = puzzles ?? throw new ArgumentNullException(nameof(puzzles));
            _players = players ?? throw new ArgumentNullException(nameof(players));
            _scene = scene ?? throw new ArgumentNullException(nameof(scene));
            _allocateId = allocateId ?? throw new ArgumentNullException(nameof(allocateId));
            _inventory = new InventoryState(itemSlotCount);

            _onGateOpened = OnGateOpened;
            _onItemSpawned = OnItemSpawned;
            _onItemPickedUp = OnItemPickedUp;
            _onInventoryChanged = OnInventoryChanged;
            _onReqDrop = OnReqDropItem;
            _onPuzzleCompleted = OnPuzzleCompleted;
            _onHumanFainted = OnHumanFainted;
        }

        public InventoryState Inventory => _inventory;

        /// <summary>Ground items on this machine: entity id → item id.</summary>
        public IReadOnlyDictionary<NetId, string> GroundItems => _groundItems;

        public bool IsGateOpen(int stage)
        {
            return _openedGates.Contains(stage);
        }

        // ---- IInventoryQuery ----

        public bool HasItem(PlayerId player, string itemId)
        {
            return _inventory.Has(player, itemId);
        }

        // ---- wiring ----

        public void Attach()
        {
            if (_attached)
                return;
            _attached = true;
            _net.Register(MsgCode.GateOpened, _onGateOpened);
            _net.Register(MsgCode.ItemSpawned, _onItemSpawned);
            _net.Register(MsgCode.ItemDropped, _onItemSpawned);
            _net.Register(MsgCode.ItemPickedUp, _onItemPickedUp);
            _net.Register(MsgCode.InventoryChanged, _onInventoryChanged);
            _net.Register(MsgCode.ReqDropItem, _onReqDrop);
            EventBus.Subscribe(_onPuzzleCompleted);
            EventBus.Subscribe(_onHumanFainted);
        }

        public void Detach()
        {
            if (!_attached)
                return;
            _attached = false;
            _net.Unregister(MsgCode.GateOpened, _onGateOpened);
            _net.Unregister(MsgCode.ItemSpawned, _onItemSpawned);
            _net.Unregister(MsgCode.ItemDropped, _onItemSpawned);
            _net.Unregister(MsgCode.ItemPickedUp, _onItemPickedUp);
            _net.Unregister(MsgCode.InventoryChanged, _onInventoryChanged);
            _net.Unregister(MsgCode.ReqDropItem, _onReqDrop);
            EventBus.Unsubscribe(_onPuzzleCompleted);
            EventBus.Unsubscribe(_onHumanFainted);
        }

        // ---- round ----

        /// <summary>
        /// Every machine, at round start (after the puzzles are registered): forgets the last round's progress, inventory and ground items
        /// (their pickups are removed through the scene) and starts in the garden (stage 0).
        /// </summary>
        public void BeginRound(IReadOnlyList<ObjectivePuzzleInfo> puzzles, int stagesActive)
        {
            _round.Clear();
            if (puzzles != null)
            {
                for (int i = 0; i < puzzles.Count; i++)
                    _round.Add(puzzles[i]);
            }
            _stagesActive = stagesActive;
            _completedSlots.Clear();
            _openedGates.Clear();
            _inventory.Clear();

            _scratch.Clear();
            foreach (KeyValuePair<NetId, string> pair in _groundItems)
                _scratch.Add(pair.Key);
            for (int i = 0; i < _scratch.Count; i++)
                _scene.DespawnItem(_scratch[i]);
            _groundItems.Clear();

            _puzzles.StageIndex = 0;
            _puzzles.StageCount = stagesActive + 1;
            _puzzles.Phase = ObjectivePhase.Garden;
        }

        // ---- authority entry points ----

        /// <summary>Authority only. Opens the gate that ends <paramref name="stage"/> (GateOpened). False when it is already open.</summary>
        public bool OpenGateAuthority(int stage)
        {
            Debug.Assert(_net.IsAuthority, "OpenGateAuthority must only run on the authority");
            if (stage < 0 || _openedGates.Contains(stage))
                return false;
            _net.Broadcast(GateOpenedMsg.Encode(stage));
            return true;
        }

        public bool CompleteCurrentStageAuthority()
        {
            return OpenGateAuthority(_puzzles.StageIndex);
        }

        /// <summary>
        /// Authority only (ItemPickup.ExecuteAuthority; range is checked by the interaction system). The human takes ground item
        /// <paramref name="item"/>: ItemPickedUp + InventoryChanged. False when the item is gone, the player is not the human or the
        /// inventory is full (or already holds that item).
        /// </summary>
        public bool PickupAuthority(PlayerId who, NetId item)
        {
            Debug.Assert(_net.IsAuthority, "PickupAuthority must only run on the authority");
            if (!_groundItems.TryGetValue(item, out string itemId))
                return false;
            if (_players.GetRole(who) != Role.Human)
            {
                CMLog.Info(Category, "Pickup of " + itemId + " by " + who + " refused: only the human carries items");
                return false;
            }
            if (_inventory.TryAdd(who, itemId) < 0)
            {
                CMLog.Info(Category, "Pickup of " + itemId + " by " + who + " refused: inventory full or item already held");
                return false;
            }

            _net.Broadcast(ItemPickedUpMsg.Encode(item, who));
            _net.Broadcast(InventoryChangedMsg.Encode(who, _inventory.Snapshot(who)));
            return true;
        }

        /// <summary>Authority only. <paramref name="who"/> drops slot <paramref name="slot"/> where its pawn stands. False when the slot is empty.</summary>
        public bool DropAuthority(PlayerId who, int slot)
        {
            Debug.Assert(_net.IsAuthority, "DropAuthority must only run on the authority");
            Vector3? position = _scene.PawnPosition(who);
            if (!position.HasValue || !_inventory.TryRemoveAt(who, slot, out string itemId))
                return false;

            _net.Broadcast(InventoryChangedMsg.Encode(who, _inventory.Snapshot(who)));
            _net.Broadcast(ItemSpawnedMsg.Encode(_allocateId(), itemId, position.Value, true));
            CMLog.Info(Category, who + " dropped " + itemId);
            return true;
        }

        // ---- handlers ----

        private void OnPuzzleCompleted(PuzzleCompletedEvt e)
        {
            if (!_net.IsAuthority)
                return;
            int index = IndexOfSlot(e.PuzzleSlot);
            if (index < 0 || !_completedSlots.Add(e.PuzzleSlot))
                return;

            ObjectivePuzzleInfo info = _round[index];
            if (info.RewardItemId.Length > 0)
                _net.Broadcast(ItemSpawnedMsg.Encode(_allocateId(), info.RewardItemId, info.RewardPosition));

            if (IsStageDone(info.Stage))
                OpenGateAuthority(info.Stage);
        }

        private void OnGateOpened(NetMsg msg)
        {
            if (!GateOpenedMsg.TryDecode(msg, out int stage))
            {
                CMLog.Warn(Category, "Malformed GateOpened ignored");
                return;
            }
            if (!_openedGates.Add(stage))
                return;

            _scene.OpenGate(stage);
            if (stage + 1 > _puzzles.StageIndex)
            {
                _puzzles.StageIndex = stage + 1;
                _puzzles.Phase = stage + 1 > _stagesActive ? ObjectivePhase.Cellar : ObjectivePhase.House;
            }
            CMLog.Info(Category, "Gate of stage " + stage + " open - now stage " + _puzzles.StageIndex + " (" + _puzzles.Phase + ")");
            EventBus.Publish(new StageCompletedEvt(stage));
        }

        private void OnItemSpawned(NetMsg msg)
        {
            if (!ItemSpawnedMsg.TryDecode(msg, out NetId item, out string itemId, out Vector3 position, out bool _))
            {
                CMLog.Warn(Category, "Malformed " + msg.Code + " ignored");
                return;
            }
            if (_groundItems.ContainsKey(item))
                return;
            _groundItems.Add(item, itemId);
            _scene.SpawnItem(item, itemId, position);
        }

        private void OnItemPickedUp(NetMsg msg)
        {
            if (!ItemPickedUpMsg.TryDecode(msg, out NetId item, out PlayerId _))
            {
                CMLog.Warn(Category, "Malformed ItemPickedUp ignored");
                return;
            }
            if (_groundItems.Remove(item))
                _scene.DespawnItem(item);
        }

        private void OnInventoryChanged(NetMsg msg)
        {
            if (!InventoryChangedMsg.TryDecode(msg, out PlayerId player, out string[] slots) || !_inventory.Apply(player, slots))
                CMLog.Warn(Category, "Malformed or wrong-sized InventoryChanged ignored");
        }

        private void OnReqDropItem(NetMsg msg)
        {
            if (!_net.IsAuthority)
                return;
            if (!ReqDropItemMsg.TryDecode(msg, out int slot))
            {
                CMLog.Warn(Category, "Malformed ReqDropItem ignored");
                return;
            }
            DropAuthority(msg.Sender, slot);
        }

        /// <summary>A fainting human loses the item picked up last (Oyun §6). Runs on the authority.</summary>
        private void OnHumanFainted(HumanFaintedEvt e)
        {
            if (!_net.IsAuthority)
                return;
            IReadOnlyList<PlayerInfo> all = _players.Players;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].Role != Role.Human)
                    continue;
                int slot = _inventory.LastFilledSlot(all[i].Id);
                if (slot >= 0)
                    DropAuthority(all[i].Id, slot);
                return;
            }
        }

        // ---- helpers ----

        private int IndexOfSlot(int slot)
        {
            for (int i = 0; i < _round.Count; i++)
            {
                if (_round[i].Slot == slot)
                    return i;
            }
            return -1;
        }

        private bool IsStageDone(int stage)
        {
            bool any = false;
            for (int i = 0; i < _round.Count; i++)
            {
                if (_round[i].Stage != stage)
                    continue;
                any = true;
                if (!_completedSlots.Contains(_round[i].Slot))
                    return false;
            }
            return any;
        }

        /// <summary>JSON for Dump State ("objectives"): stage, phase, open gates, the human's slots, ground items.</summary>
        public string BuildDebugJson()
        {
            var sb = new StringBuilder("{\"stage\":");
            sb.Append(_puzzles.StageIndex.ToString(CultureInfo.InvariantCulture))
                .Append(",\"phase\":\"").Append(_puzzles.Phase.ToString()).Append("\",\"gates\":[");
            bool first = true;
            foreach (int g in _openedGates)
            {
                if (!first)
                    sb.Append(',');
                first = false;
                sb.Append(g.ToString(CultureInfo.InvariantCulture));
            }
            sb.Append("],\"inventory\":{");
            first = true;
            IReadOnlyList<PlayerInfo> all = _players.Players;
            for (int i = 0; i < all.Count; i++)
            {
                if (_inventory.LastFilledSlot(all[i].Id) < 0)
                    continue;
                if (!first)
                    sb.Append(',');
                first = false;
                sb.Append('"').Append(all[i].Id.ToString()).Append("\":[");
                for (int s = 0; s < _inventory.SlotCount; s++)
                {
                    if (s > 0)
                        sb.Append(',');
                    sb.Append(DebugState.Quote(_inventory.Get(all[i].Id, s)));
                }
                sb.Append(']');
            }
            sb.Append("},\"ground\":[");
            first = true;
            foreach (KeyValuePair<NetId, string> pair in _groundItems)
            {
                if (!first)
                    sb.Append(',');
                first = false;
                sb.Append(DebugState.Quote(pair.Value + "@" + pair.Key));
            }
            return sb.Append("]}").ToString();
        }
    }
}
