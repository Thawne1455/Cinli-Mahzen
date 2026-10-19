using System.Collections.Generic;
using System.Globalization;
using CinliMahzen.Core;
using CinliMahzen.Core.Events;
using CinliMahzen.Core.Net;
using UnityEngine;

namespace CinliMahzen.World
{
    /// <summary>
    /// Lives on the P_House root (Teknik 3, 8). <see cref="Index"/> runs ONCE at scene start: it reads every marker component
    /// below this object with GetComponentsInChildren (never by name, CLAUDE.md rule 11), builds the plain-data
    /// <see cref="HouseIndex"/>, gives the NetId-carrying markers their deterministic ids (sorted hierarchy paths relative to
    /// this object, plus the marker type name), registers them in the entity registry and publishes
    /// <see cref="IHouseInfo"/> and <see cref="IRoomService"/> through <see cref="GameServices"/>. OnDestroy undoes all of it.
    ///
    /// The house is not destroyed between rounds; round resets go through <see cref="RoomService.ResetState"/> and the round
    /// setup, not through a second Index().
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(HouseService.ExecutionOrder)]
    public sealed class HouseService : MonoBehaviour
    {
        /// <summary>Runs before ordinary scripts so IHouseInfo / IRoomService are real by their Awake/Start.</summary>
        public const int ExecutionOrder = -100;

        public const string DebugStateKey = "house";

        private const string LogCategory = "House";
        private const char PathSeparator = '/';

        [Tooltip("Metres added to every side of the union of the room boxes for IHouseInfo.Bounds (spirits are clamped inside).")]
        [SerializeField] private Vector3 boundsPadding = new Vector3(1f, 1f, 1f);

        private readonly List<NetHouseMarker> _netMarkers = new List<NetHouseMarker>();
        private HouseIndex _houseIndex;
        private RoomService _roomState;
        private HouseInfo _info;
        private IEntityRegistry _registry;
        private bool _published;
        private INetBridge _net;
        private System.Action<NetMsg> _onRadio;

        /// <summary>True after a successful <see cref="Index"/> (until <see cref="Release"/>).</summary>
        public bool IsIndexed
        {
            get { return _houseIndex != null; }
        }

        /// <summary>The plain-data snapshot of all markers; null before <see cref="Index"/>.</summary>
        public HouseIndex HouseIndex
        {
            get { return _houseIndex; }
        }

        /// <summary>Concrete room state for the light and radio systems of this assembly (they set it, others read <see cref="IRoomService"/>); null before <see cref="Index"/>.</summary>
        public RoomService RoomState
        {
            get { return _roomState; }
        }

        public HouseInfo Info
        {
            get { return _info; }
        }

        /// <summary>Message of the last failed <see cref="Index"/> / <see cref="TryBuildIndex"/>; empty when it worked.</summary>
        public string LastError { get; private set; } = string.Empty;

        private void Awake()
        {
            Index();
        }

        private void OnDestroy()
        {
            Release();
        }

        /// <summary>
        /// Reads the markers, assigns NetIds, registers the entities and the two services. Safe to call twice (the second call
        /// does nothing). Returns false (and logs an error) when the index cannot be built, e.g. two markers with the same
        /// hierarchy path; the services then stay on their stubs.
        /// </summary>
        public bool Index()
        {
            if (_houseIndex != null)
            {
                CMLog.Warn(LogCategory, name + ": Index() called twice, ignored.");
                return true;
            }

            HouseIndex index;
            var netEntries = new List<HouseNetEntry>();
            _netMarkers.Clear();
            if (!BuildIndex(out index, _netMarkers, netEntries))
            {
                _netMarkers.Clear();
                CMLog.Error(LogCategory, name + ": cannot build the house index: " + LastError);
                return false;
            }

            for (int i = 0; i < _netMarkers.Count; i++)
            {
                _netMarkers[i].AssignNetId(netEntries[i].NetId);
            }

            _houseIndex = index;
            _roomState = new RoomService(index, ReadNetTime);
            _info = new HouseInfo(index, boundsPadding);

            _registry = GameServices.Entities;
            if (_registry != null)
            {
                for (int i = 0; i < _netMarkers.Count; i++)
                {
                    _registry.Register(_netMarkers[i]);
                }
            }

            GameServices.Register<IHouseInfo>(_info);
            GameServices.Register<IRoomService>(_roomState);
            DebugState.Register(DebugStateKey, BuildDebugJson);
            _published = true;

            // TODO 1.8: the Old Radio's play_tune mutes its room on every machine.
            _net = GameServices.Net;
            if (_net != null)
            {
                _onRadio = _onRadio ?? OnRadioStateChanged;
                _net.Register(MsgCode.RadioStateChanged, _onRadio);
            }

            CMLog.Info(LogCategory, "Indexed " + Summary(index));
            return true;
        }

        private void OnRadioStateChanged(NetMsg msg)
        {
            if (_roomState == null || !RadioStateChangedMsg.TryDecode(msg, out int room, out double until))
                return;
            if (_roomState.SuppressNoiseUntil(room, until))
                EventBus.Publish(new RadioStateChangedEvt(room, true));
        }

        /// <summary>
        /// Builds a <see cref="HouseIndex"/> from the markers below this object WITHOUT touching the markers, the entity registry
        /// or <see cref="GameServices"/>. For the validator menu (works in edit mode and on prefab assets).
        /// </summary>
        public bool TryBuildIndex(out HouseIndex index, out string error)
        {
            bool ok = BuildIndex(out index, null, null);
            error = ok ? string.Empty : LastError;
            return ok;
        }

        /// <summary>Unregisters everything <see cref="Index"/> registered. Idempotent; called from OnDestroy.</summary>
        public void Release()
        {
            if (_registry != null)
            {
                for (int i = 0; i < _netMarkers.Count; i++)
                {
                    _registry.Unregister(_netMarkers[i]);
                }
                _registry = null;
            }
            _netMarkers.Clear();

            if (_net != null)
            {
                _net.Unregister(MsgCode.RadioStateChanged, _onRadio);
                _net = null;
            }

            if (_published)
            {
                GameServices.Unregister<IHouseInfo>(_info);
                GameServices.Unregister<IRoomService>(_roomState);
                DebugState.Unregister(DebugStateKey);
                _published = false;
            }

            _houseIndex = null;
            _roomState = null;
            _info = null;
        }

        // ------------------------------------------------------------------------------------------------
        // collecting
        // ------------------------------------------------------------------------------------------------

        /// <summary>
        /// Shared by Index and TryBuildIndex. When the two lists are given they receive, in parallel, every NetId-carrying
        /// marker and its (not yet numbered) entry.
        /// </summary>
        private bool BuildIndex(out HouseIndex index, List<NetHouseMarker> netMarkers, List<HouseNetEntry> netEntries)
        {
            index = null;
            LastError = string.Empty;
            var builder = new HouseIndexBuilder();

            var rooms = new List<RoomMarker>();
            GetComponentsInChildren(true, rooms);
            var boxes = new List<Bounds>();
            for (int i = 0; i < rooms.Count; i++)
            {
                boxes.Clear();
                rooms[i].CollectWorldBoxes(boxes);
                builder.Add(new HouseRoomEntry(rooms[i].RoomId, rooms[i].RoomKey, rooms[i].Floor, rooms[i].IsOutdoor, boxes));
            }

            var zones = new List<StageZone>();
            GetComponentsInChildren(true, zones);
            for (int i = 0; i < zones.Count; i++)
            {
                builder.Add(new HouseStageEntry(zones[i].StageIndex, zones[i].RoomIds));
            }

            var puzzleSlots = new List<PuzzleSlot>();
            GetComponentsInChildren(true, puzzleSlots);
            for (int i = 0; i < puzzleSlots.Count; i++)
            {
                PuzzleSlot slot = puzzleSlots[i];
                var entry = new HousePuzzleSlotEntry(KeyOf(slot), slot.StageIndex, slot.RoomId, slot.Size, slot.AllowedTypes,
                    slot.transform.position, slot.RewardPoint.position);
                builder.Add(entry);
                Track(netMarkers, netEntries, slot, entry);
            }

            var referenceSlots = new List<ReferenceSlot>();
            GetComponentsInChildren(true, referenceSlots);
            for (int i = 0; i < referenceSlots.Count; i++)
            {
                ReferenceSlot slot = referenceSlots[i];
                var entry = new HouseReferenceSlotEntry(KeyOf(slot), slot.RoomId, slot.Size, slot.AllowedTypes, slot.transform.position);
                builder.Add(entry);
                Track(netMarkers, netEntries, slot, entry);
            }

            var gates = new List<StageGate>();
            GetComponentsInChildren(true, gates);
            for (int i = 0; i < gates.Count; i++)
            {
                StageGate gate = gates[i];
                GateLock gateLock = gate.GetComponent<GateLock>();
                string requiredItem = gateLock != null ? gateLock.RequiredItemId : string.Empty;
                var entry = new HouseGateEntry(KeyOf(gate), gate.StageIndex, requiredItem, gate.Door.position);
                builder.Add(entry);
                Track(netMarkers, netEntries, gate, entry);
            }

            var lamps = new List<LampMarker>();
            GetComponentsInChildren(true, lamps);
            for (int i = 0; i < lamps.Count; i++)
            {
                AddSpot(builder, netMarkers, netEntries, lamps[i], HouseSpotKind.Lamp, lamps[i].RoomId, string.Empty);
            }

            var switches = new List<LightSwitch>();
            GetComponentsInChildren(true, switches);
            for (int i = 0; i < switches.Count; i++)
            {
                AddSpot(builder, netMarkers, netEntries, switches[i], HouseSpotKind.LightSwitch, switches[i].RoomId, string.Empty);
            }

            var keyHides = new List<KeyHideSpot>();
            GetComponentsInChildren(true, keyHides);
            for (int i = 0; i < keyHides.Count; i++)
            {
                AddSpot(builder, netMarkers, netEntries, keyHides[i], HouseSpotKind.KeyHide, HouseIndex.NoRoom, string.Empty);
            }

            var colognes = new List<CologneSpawnSpot>();
            GetComponentsInChildren(true, colognes);
            for (int i = 0; i < colognes.Count; i++)
            {
                AddSpot(builder, netMarkers, netEntries, colognes[i], HouseSpotKind.Cologne, colognes[i].RoomId, string.Empty);
            }

            var digSpots = new List<DigSpot>();
            GetComponentsInChildren(true, digSpots);
            for (int i = 0; i < digSpots.Count; i++)
            {
                AddSpot(builder, netMarkers, netEntries, digSpots[i], HouseSpotKind.Dig, HouseIndex.NoRoom, string.Empty);
            }

            var toolPoints = new List<ToolShedItemPoint>();
            GetComponentsInChildren(true, toolPoints);
            for (int i = 0; i < toolPoints.Count; i++)
            {
                AddSpot(builder, netMarkers, netEntries, toolPoints[i], HouseSpotKind.ToolShedItem, HouseIndex.NoRoom, toolPoints[i].ItemId);
            }

            var humanSpawns = new List<HumanSpawnMarker>();
            GetComponentsInChildren(true, humanSpawns);
            for (int i = 0; i < humanSpawns.Count; i++)
            {
                builder.AddHumanSpawn(KeyOf(humanSpawns[i]), humanSpawns[i].transform.position);
            }

            var jinnSpawns = new List<JinnSpawnMarker>();
            GetComponentsInChildren(true, jinnSpawns);
            for (int i = 0; i < jinnSpawns.Count; i++)
            {
                builder.AddJinnSpawn(KeyOf(jinnSpawns[i]), jinnSpawns[i].transform.position);
            }

            string error;
            if (!builder.TryBuild(out index, out error))
            {
                LastError = error;
                return false;
            }
            return true;
        }

        private void AddSpot(HouseIndexBuilder builder, List<NetHouseMarker> netMarkers, List<HouseNetEntry> netEntries,
            NetHouseMarker marker, HouseSpotKind kind, int roomId, string itemId)
        {
            var entry = new HouseSpotEntry(KeyOf(marker), kind, roomId, itemId, marker.transform.position);
            builder.Add(entry);
            Track(netMarkers, netEntries, marker, entry);
        }

        private static void Track(List<NetHouseMarker> netMarkers, List<HouseNetEntry> netEntries, NetHouseMarker marker, HouseNetEntry entry)
        {
            if (netMarkers != null && netEntries != null)
            {
                netMarkers.Add(marker);
                netEntries.Add(entry);
            }
        }

        /// <summary>Hierarchy path relative to this object + "#" + marker type name (see <see cref="HouseIndex.MakeNetKey"/>).</summary>
        private string KeyOf(Component marker)
        {
            var names = new List<string>();
            for (Transform t = marker.transform; t != null && t != transform; t = t.parent)
            {
                names.Add(t.name);
            }
            names.Reverse();
            return HouseIndex.MakeNetKey(string.Join(PathSeparator.ToString(), names), marker.GetType().Name);
        }

        // ------------------------------------------------------------------------------------------------
        // helpers
        // ------------------------------------------------------------------------------------------------

        private static double ReadNetTime()
        {
            var net = GameServices.Net;
            return net != null ? net.Time : 0d;
        }

        private static string Summary(HouseIndex index)
        {
            var net = new List<HouseNetEntry>();
            index.CollectNetEntries(net);
            int lowest = int.MaxValue;
            int highest = int.MinValue;
            for (int i = 0; i < net.Count; i++)
            {
                lowest = System.Math.Min(lowest, net[i].NetId.Value);
                highest = System.Math.Max(highest, net[i].NetId.Value);
            }
            string ids = net.Count > 0 ? "NetIds " + lowest + ".." + highest : "no NetIds";
            return index.Rooms.Count + " rooms, " + index.Stages.Count + " stages, " + index.PuzzleSlots.Count + " puzzle slots, "
                + index.ReferenceSlots.Count + " reference slots, " + index.Gates.Count + " gates, " + index.Lamps.Count + " lamps, "
                + index.LightSwitches.Count + " switches, " + index.KeyHideSpots.Count + " key spots, " + index.CologneSpots.Count
                + " cologne spots, " + index.DigSpots.Count + " dig spots, " + index.HumanSpawns.Count + "/" + index.JinnSpawns.Count
                + " human/jinn spawns, " + ids + ".";
        }

        private string BuildDebugJson()
        {
            if (_houseIndex == null)
            {
                return "null";
            }

            CultureInfo ci = CultureInfo.InvariantCulture;
            HouseIndex index = _houseIndex;
            Bounds bounds = _info.Bounds;
            Vector3 spawn = _info.HumanSpawn;
            return "{\"rooms\":" + index.Rooms.Count
                + ",\"stages\":" + index.Stages.Count
                + ",\"puzzleSlots\":" + index.PuzzleSlots.Count
                + ",\"referenceSlots\":" + index.ReferenceSlots.Count
                + ",\"gates\":" + index.Gates.Count
                + ",\"lamps\":" + index.Lamps.Count
                + ",\"keyHideSpots\":" + index.KeyHideSpots.Count
                + ",\"cologneSpots\":" + index.CologneSpots.Count
                + ",\"digSpots\":" + index.DigSpots.Count
                + ",\"netEntities\":" + _netMarkers.Count
                + ",\"humanSpawn\":[" + spawn.x.ToString("0.00", ci) + "," + spawn.y.ToString("0.00", ci) + "," + spawn.z.ToString("0.00", ci) + "]"
                + ",\"boundsMin\":[" + bounds.min.x.ToString("0.0", ci) + "," + bounds.min.y.ToString("0.0", ci) + "," + bounds.min.z.ToString("0.0", ci) + "]"
                + ",\"boundsMax\":[" + bounds.max.x.ToString("0.0", ci) + "," + bounds.max.y.ToString("0.0", ci) + "," + bounds.max.z.ToString("0.0", ci) + "]"
                + "}";
        }
    }
}
