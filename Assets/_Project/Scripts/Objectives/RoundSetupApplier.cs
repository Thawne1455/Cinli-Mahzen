using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.Core.Events;
using CinliMahzen.Core.Net;
using CinliMahzen.World;
using UnityEngine;

namespace CinliMahzen.Objectives
{
    /// <summary>
    /// Puts the round plan into the house (Teknik §7.5–7.6, TODO 2.7). Lives next to <see cref="PuzzleSystem"/> (one "Objectives" object per
    /// Game scene: <c>CinliMahzen/Setup/Add Objectives To Open Scene</c>). On every machine, at <see cref="RoundStartedEvt"/>:
    /// <list type="number">
    /// <item>last round's puzzles, references and ground items go away, the slots are cleared and the stage gates close;</item>
    /// <item><see cref="RoundSetupPlanner"/> plans the round from the seed (same seed → same plan everywhere);</item>
    /// <item>each planned puzzle becomes a <see cref="PuzzleState"/> (answer = start state) registered under its slot marker's NetId, its
    /// <c>P_Puzzle_*</c> is spawned on the PuzzleSlot marker and bound with its plan index as ordinal, its <c>P_PuzzleRef_*</c> on the
    /// ReferenceSlot marker shows the answer;</item>
    /// <item><see cref="ObjectiveFlowCore.BeginRound"/> starts the stage / reward / inventory flow in the garden.</item>
    /// </list>
    /// It is the flow's <see cref="IObjectiveScene"/> (pickups, gate doors) and publishes the flow as <see cref="IInventoryQuery"/> and
    /// <see cref="IObjectiveDebugCommands"/> (F11). Dump State section: "objectives".
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PuzzleSystem))]
    public sealed class RoundSetupApplier : MonoBehaviour, IObjectiveScene
    {
        private const string Category = "RoundSetup";
        private const string DebugStateKey = "objectives";
        private const string InteractableLayerName = "Interactable";
        private const string SpiritOnlyLayerName = "SpiritOnly";

        [Tooltip("PuzzleDefinition pool (PZ_*). Order does not matter for the plan.")]
        [SerializeField] private PuzzleDefinition[] pool;
        [Tooltip("ItemDefinitions (ID_*): the ground prefab of a reward / dropped item is looked up here by id. Missing prefab = a small cube.")]
        [SerializeField] private ItemDefinition[] items;
        [Tooltip("Edge (m) of the placeholder cube of an item without a prefab.")]
        [SerializeField] private float placeholderSize = 0.3f;
        [Tooltip("Size (m) of the jinn-only glow over the house key's hiding place (TODO 3.6).")]
        [SerializeField] private float keyGlowSize = 0.4f;

        private readonly RoundSetupPlanner _planner = new RoundSetupPlanner();
        private readonly List<GameObject> _roundObjects = new List<GameObject>(16);
        private readonly Dictionary<NetId, ItemPickup> _pickups = new Dictionary<NetId, ItemPickup>();
        private readonly List<ObjectivePuzzleInfo> _infos = new List<ObjectivePuzzleInfo>(8);
        private readonly List<StageGate> _gates = new List<StageGate>(4);
        private readonly Dictionary<NetId, GameObject> _gardenVisuals = new Dictionary<NetId, GameObject>();
        private readonly List<ObjectiveFlowCore.LockedGate> _lockedGates = new List<ObjectiveFlowCore.LockedGate>(4);
        private readonly List<ObjectiveFlowCore.ShedItem> _shedItems = new List<ObjectiveFlowCore.ShedItem>(2);

        private PuzzleSystem _system;
        private ObjectiveFlowCore _flow;
        private HouseService _house;
        private RoundPlan _plan;
        private bool _started;

        /// <summary>The flow (stages, rewards, inventory). Null before Start or when inert.</summary>
        public ObjectiveFlowCore Flow => _flow;

        /// <summary>The plan of the running round, or null.</summary>
        public RoundPlan Plan => _plan;

        private void Awake()
        {
            _system = GetComponent<PuzzleSystem>();
        }

        // Start, not Awake: the PuzzleSystem on the same object builds its core in its own Awake.
        private void Start()
        {
            INetBridge net = GameServices.Net;
            IPlayerRegistry players = GameServices.Players;
            GameBalanceConfig config = GameServices.Config;
            if (_system == null || _system.Core == null || net == null || players == null || config == null)
            {
                CMLog.Error(Category, name + ": PuzzleSystem core / GameServices missing - rounds are not set up");
                enabled = false;
                return;
            }

            _flow = new ObjectiveFlowCore(net, _system.Core, players, config.ItemSlotCount, this, AllocateId);
            _flow.Attach();
            GameServices.Register<IInventoryQuery>(_flow);
            GameServices.Register<IObjectiveDebugCommands>(_flow);
            GameServices.Register<IObjectiveInteractions>(_flow);
            DebugState.Register(DebugStateKey, _flow.BuildDebugJson);
            EventBus.Subscribe<RoundStartedEvt>(OnRoundStarted);
            _started = true;
        }

        private void OnDestroy()
        {
            if (!_started)
                return;
            EventBus.Unsubscribe<RoundStartedEvt>(OnRoundStarted);
            _flow.Detach();
            GameServices.Unregister<IInventoryQuery>(_flow);
            GameServices.Unregister<IObjectiveDebugCommands>(_flow);
            GameServices.Unregister<IObjectiveInteractions>(_flow);
            DebugState.Unregister(DebugStateKey);
        }

        private void OnRoundStarted(RoundStartedEvt e)
        {
            Apply(e.Seed);
        }

        /// <summary>Plans and builds the round of <paramref name="seed"/> (every machine). False (and an error) when it cannot.</summary>
        public bool Apply(int seed)
        {
            ClearRound();
            if (!FindHouse())
                return false;

            GameBalanceConfig config = GameServices.Config;
            PlannerSettings settings = PlannerSettings.FromConfig(config);
            if (!_planner.TryPlan(_house.HouseIndex, pool, settings, seed, out _plan, out string error))
            {
                CMLog.Error(Category, "Round plan failed for seed " + seed + ": " + error);
                _flow.BeginRound(null, settings.StagesActive);
                return false;
            }

            PuzzleSystemCore core = _system.Core;
            IEntityRegistry entities = GameServices.Entities;
            for (int i = 0; i < _plan.Puzzles.Count; i++)
                BuildPuzzle(core, entities, _plan.Puzzles[i], i);

            _flow.BeginRound(_infos, settings.StagesActive);
            SetUpGarden();
            CMLog.Info(Category, "Round set up from seed " + seed + ": " + _plan.Puzzles.Count + " puzzles, hash " + _plan.ComputeHash().ToString("X16"));
            return true;
        }

        private void BuildPuzzle(PuzzleSystemCore core, IEntityRegistry entities, PlannedPuzzle p, int ordinal)
        {
            PuzzleDefinition def = pool[p.DefinitionIndex];
            int[] traps = p.TrapValues.Count > 0 ? p.CopyTrapValues() : null;
            var state = new PuzzleState(def.CopyValueCounts(), p.CopySolution(), def.OpKind, p.ReferenceRotation, traps);

            Transform slotMarker = MarkerTransform<PuzzleSlot>(entities, p.PuzzleSlotId);
            Vector3 position = slotMarker != null ? slotMarker.position : Vector3.zero;
            Quaternion rotation = slotMarker != null ? slotMarker.rotation : Quaternion.identity;
            var runtime = new PuzzleSlotRuntime(state, def, position, p.RoomId, p.StageIndex);
            int slotKey = p.PuzzleSlotId.Value;
            core.RegisterSlot(slotKey, runtime);

            if (def.InteractivePrefab != null)
            {
                GameObject go = Instantiate(def.InteractivePrefab, position, rotation);
                go.name = def.InteractivePrefab.name + "_" + ordinal;
                _roundObjects.Add(go);
                PuzzleView view = go.GetComponent<PuzzleView>();
                if (view != null && view.Bind(core, slotKey, ordinal))
                {
                    ConfirmLever lever = go.GetComponentInChildren<ConfirmLever>();
                    if (lever != null)
                        runtime.ConfirmPosition = lever.transform.position;
                }
            }
            else
            {
                CMLog.Warn(Category, def.name + " has no interactive prefab - slot " + slotKey + " is playable only through debug menus");
            }

            Transform referenceMarker = MarkerTransform<ReferenceSlot>(entities, p.ReferenceSlotId);
            if (def.ReferencePrefab != null && referenceMarker != null)
            {
                GameObject go = Instantiate(def.ReferencePrefab, referenceMarker.position, referenceMarker.rotation);
                go.name = def.ReferencePrefab.name + "_" + ordinal;
                _roundObjects.Add(go);
                go.GetComponent<PuzzleReference>()?.Bind(p.Solution, p.ReferenceRotation, p.TrapValues);
            }

            _infos.Add(new ObjectivePuzzleInfo(slotKey, p.StageIndex, p.RewardItemId, RewardPositionOf(p.PuzzleSlotId, position)));
        }

        /// <summary>TODO 3.6: the key's hiding place (with a jinn-only glow), the locked gates and the tool shed items.</summary>
        private void SetUpGarden()
        {
            HouseIndex index = _house.HouseIndex;
            _lockedGates.Clear();
            for (int i = 0; i < index.Gates.Count; i++)
            {
                if (index.Gates[i].HasLock)
                    _lockedGates.Add(new ObjectiveFlowCore.LockedGate(index.Gates[i].NetId, index.Gates[i].StageIndex, index.Gates[i].RequiredItemId));
            }
            _shedItems.Clear();
            for (int i = 0; i < index.ToolShedItems.Count; i++)
            {
                HouseSpotEntry point = index.ToolShedItems[i];
                _shedItems.Add(new ObjectiveFlowCore.ShedItem(point.NetId, point.ItemId));
                _gardenVisuals[point.NetId] = Placeholder("Shed_" + point.ItemId, point.Position, placeholderSize, -1);
            }
            _flow.SetGarden(_plan.HouseKeySpotId, _lockedGates, _shedItems);

            for (int i = 0; i < index.KeyHideSpots.Count; i++)
            {
                if (index.KeyHideSpots[i].NetId != _plan.HouseKeySpotId)
                    continue;
                // The jinns (both) see where the key hides; distance rules come with the visibility service (TODO 3.9).
                _gardenVisuals[_plan.HouseKeySpotId] = Placeholder("KeyGlow", index.KeyHideSpots[i].Position + Vector3.up * 0.5f, keyGlowSize,
                    LayerMask.NameToLayer(SpiritOnlyLayerName));
            }
        }

        private GameObject Placeholder(string name, Vector3 position, float size, int layer)
        {
            GameObject go = GameObject.CreatePrimitive(layer >= 0 ? PrimitiveType.Sphere : PrimitiveType.Cube);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            go.transform.position = position;
            go.transform.localScale = Vector3.one * size;
            if (layer >= 0)
                go.layer = layer;
            _roundObjects.Add(go);
            return go;
        }

        public void OnKeyFound(NetId spot)
        {
            RemoveGardenVisual(spot);
        }

        public void OnToolShedItemTaken(NetId point)
        {
            RemoveGardenVisual(point);
        }

        private void RemoveGardenVisual(NetId id)
        {
            if (_gardenVisuals.TryGetValue(id, out GameObject go))
            {
                _gardenVisuals.Remove(id);
                if (go != null)
                    Destroy(go);
            }
        }

        private Vector3 RewardPositionOf(NetId slotId, Vector3 fallback)
        {
            IReadOnlyList<HousePuzzleSlotEntry> slots = _house.HouseIndex.PuzzleSlots;
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].NetId == slotId)
                    return slots[i].RewardPosition;
            }
            return fallback;
        }

        private static Transform MarkerTransform<T>(IEntityRegistry entities, NetId id) where T : Component
        {
            return entities != null && entities.TryGet(id, out T marker) ? marker.transform : null;
        }

        private bool FindHouse()
        {
            if (_house == null)
            {
                _house = FindFirstObjectByType<HouseService>();
                if (_house != null)
                    _house.GetComponentsInChildren(true, _gates);
            }
            if (_house == null || _house.HouseIndex == null)
            {
                CMLog.Error(Category, "No indexed HouseService in the scene - the round has no puzzles");
                return false;
            }
            return true;
        }

        private void ClearRound()
        {
            for (int i = 0; i < _roundObjects.Count; i++)
            {
                if (_roundObjects[i] != null)
                    Destroy(_roundObjects[i]);
            }
            _roundObjects.Clear();
            _gardenVisuals.Clear();
            _infos.Clear();
            _system.ClearSlots();
            for (int i = 0; i < _gates.Count; i++)
                SetDoorOpen(_gates[i], false);
            _plan = null;
        }

        // ---- IObjectiveScene ----

        public void SpawnItem(NetId item, string itemId, Vector3 position)
        {
            GameObject go;
            ItemDefinition def = FindItem(itemId);
            if (def != null && def.Prefab != null)
            {
                go = Instantiate(def.Prefab, position, Quaternion.identity);
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.position = position;
                go.transform.localScale = Vector3.one * placeholderSize;
            }
            go.name = "Item_" + itemId + "_" + item.Value;
            int layer = LayerMask.NameToLayer(InteractableLayerName);
            if (layer >= 0)
                go.layer = layer;
            if (go.GetComponentInChildren<Collider>() == null)
                go.AddComponent<BoxCollider>();

            ItemPickup pickup = go.GetComponent<ItemPickup>();
            if (pickup == null)
                pickup = go.AddComponent<ItemPickup>();
            pickup.Bind(_flow, item, itemId);
            GameServices.Entities?.Register(pickup);
            _pickups[item] = pickup;
        }

        public void DespawnItem(NetId item)
        {
            if (!_pickups.TryGetValue(item, out ItemPickup pickup))
                return;
            _pickups.Remove(item);
            if (pickup == null)
                return;
            GameServices.Entities?.Unregister(pickup);
            Destroy(pickup.gameObject);
        }

        public void OpenGate(int stage)
        {
            bool any = false;
            for (int i = 0; i < _gates.Count; i++)
            {
                if (_gates[i] != null && _gates[i].StageIndex == stage)
                {
                    SetDoorOpen(_gates[i], true);
                    any = true;
                }
            }
            if (!any)
                CMLog.Info(Category, "Stage " + stage + " has no StageGate marker - nothing to open");
        }

        /// <summary>
        /// Graybox: an open gate's door piece disappears (doorways do not block in v0; a swinging door comes with the art pass). A gate without
        /// its own door piece (Door is the marker itself) is left alone so the marker keeps running.
        /// </summary>
        private static void SetDoorOpen(StageGate gate, bool open)
        {
            if (gate == null || gate.Door == gate.transform)
                return;
            gate.Door.gameObject.SetActive(!open);
        }

        public Vector3? PawnPosition(PlayerId player)
        {
            PawnBase[] pawns = FindObjectsByType<PawnBase>(FindObjectsSortMode.None);
            for (int i = 0; i < pawns.Length; i++)
            {
                if (pawns[i].Owner == player)
                    return pawns[i].transform.position;
            }
            return null;
        }

        private ItemDefinition FindItem(string itemId)
        {
            if (items == null)
                return null;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null && items[i].Id == itemId)
                    return items[i];
            }
            return null;
        }

        private static NetId AllocateId()
        {
            IEntityRegistry entities = GameServices.Entities;
            return entities != null ? entities.AllocateRuntimeId() : NetId.Invalid;
        }
    }
}
