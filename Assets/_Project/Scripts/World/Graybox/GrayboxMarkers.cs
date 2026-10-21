using System;
using System.Collections.Generic;
using CinliMahzen.Core;
using UnityEngine;

namespace CinliMahzen.World.Graybox
{
    /// <summary>
    /// The marker data of the hand-built graybox house (TODO 2.2, Teknik 8): where every RoomMarker box, StageZone, PuzzleSlot,
    /// ReferenceSlot, StageGate (+GateLock), lamp, light switch, key hiding place, cologne spot, spawn point and the tool shed
    /// item lies. Like <see cref="GrayboxLayout"/> this is fixed hand-authored data, not a generator (CLAUDE.md section 5):
    /// the editor builder (CinliMahzen/House/Build Graybox) turns <see cref="Specs"/> into marker components and
    /// <see cref="GrayboxHouseIndexFactory"/> turns them into a <see cref="HouseIndex"/> for tests.
    ///
    /// Derived from the layout (so they follow it when walls move): the room boxes, the stage zones (the cellar has none), the gates
    /// (front door = stage 0 with the house key lock, stair gates = stages 1 and 2, the cellar hatch with the cellar key lock) and the
    /// key hiding places (every well, woodpile and pot). Authored in <c>AddAuthored</c> below: the rest (DigSpots included, TODO 4.1).
    ///
    /// Coordinates are house-local metres: cell (cx, cz) spans X 4cx..4cx+4, Z 4cz..4cz+4 (see <see cref="GrayboxLayout"/>);
    /// walls are 1 m thick centred on the grid lines, so a marker on a wall face sits 0.6 m from the grid line. Heights are
    /// given above the floor surface. A marker's forward (+Z) points from the wall into the room.
    /// </summary>
    public sealed class GrayboxMarkers
    {
        // ---- heights above the floor surface (m) ----------------------------------------------------------
        public const float LampHeight = 3.2f;
        public const float SwitchHeight = 1.4f;
        public const float WallSlotHeight = 1.6f;
        public const float TableSlotHeight = 0.9f;
        public const float FloorSlotHeight = 0.1f;
        public const float SpawnHeight = 0.1f;
        public const float SpiritSpawnHeight = 1.6f;
        public const float KeyHideWellHeight = 1.0f;
        public const float KeyHideWoodpileHeight = 1.0f;
        public const float KeyHidePotHeight = 0.6f;

        /// <summary>The reward item appears this far in front of its puzzle slot, at this height above the floor.</summary>
        public const float RewardDistance = 1.0f;

        public const float RewardHeight = 1.0f;

        /// <summary>Room boxes of the garden reach one floor above the attic, so a flying spirit above the garden is still "in the garden".</summary>
        public const float OutdoorBoxHeight = GrayboxLayout.FloorHeight * (GrayboxLayout.MaxFloor + 2);

        /// <summary>DigSpots lie on the cellar floor.</summary>
        public const float DigSpotHeight = 0f;

        /// <summary>
        /// Stable room ids: the id of a room is its index in this table (RoomMarker.roomId, IRoomService, RoundPlan). Append new
        /// rooms at the END and never reorder, exactly like enum values.
        /// </summary>
        private static readonly string[] RoomKeyTable =
        {
            "garden", "shed",
            "hall", "kitchen", "living", "pantry", "stairwell",
            "landing", "bedroom1", "bedroom2", "corridor", "bathroom", "study",
            "atticstairs", "attic", "ritual", "cellar"
        };

        private static readonly PuzzleType[] WallTypes = { PuzzleType.Paintings, PuzzleType.Wires, PuzzleType.Sigil };
        private static readonly PuzzleType[] FloorTypes = { PuzzleType.Statue };
        private static readonly PuzzleType[] TableTypes = { PuzzleType.Statue, PuzzleType.Sigil };

        private static GrayboxMarkers _default;

        /// <summary>The markers of <see cref="GrayboxLayout.Default"/>. Immutable, safe to share.</summary>
        public static GrayboxMarkers Default
        {
            get { return _default ?? (_default = Create(GrayboxLayout.Default)); }
        }

        /// <summary>Every marker, in a fixed order: rooms, stage zones, gates, key hiding places, authored markers.</summary>
        public IReadOnlyList<GrayboxMarkerSpec> Specs { get; }

        private GrayboxMarkers(List<GrayboxMarkerSpec> specs)
        {
            Specs = specs;
        }

        /// <summary><see cref="Default"/> for the shipped layout, a fresh instance for any other layout.</summary>
        public static GrayboxMarkers For(GrayboxLayout layout)
        {
            return ReferenceEquals(layout, GrayboxLayout.Default) ? Default : Create(layout);
        }

        /// <summary>The stable id of a room key, or -1 when the key is not in the table.</summary>
        public static int RoomIdOf(string roomKey)
        {
            for (int i = 0; i < RoomKeyTable.Length; i++)
            {
                if (string.Equals(RoomKeyTable[i], roomKey, StringComparison.Ordinal))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>The room key of an id, or null.</summary>
        public static string RoomKeyOf(int roomId)
        {
            return roomId >= 0 && roomId < RoomKeyTable.Length ? RoomKeyTable[roomId] : null;
        }

        public int CountOf(GrayboxMarkerKind kind)
        {
            int count = 0;
            for (int i = 0; i < Specs.Count; i++)
            {
                if (Specs[i].Kind == kind)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Builds the marker data for a layout. Throws <see cref="InvalidOperationException"/> when a layout room has no entry in
        /// the room id table or an authored marker names a room the layout does not have.
        /// </summary>
        public static GrayboxMarkers Create(GrayboxLayout layout)
        {
            if (layout == null)
            {
                throw new ArgumentNullException("layout");
            }

            var builder = new SpecBuilder(layout);
            builder.AddRooms();
            builder.AddStageZones();
            builder.AddGates();
            builder.AddKeyHideSpots();
            builder.AddAuthored();
            return new GrayboxMarkers(builder.Specs);
        }

        // ------------------------------------------------------------------------------------------------

        private static PuzzleType[] TypesFor(PuzzleSlotSize size)
        {
            switch (size)
            {
                case PuzzleSlotSize.Floor: return FloorTypes;
                case PuzzleSlotSize.Table: return TableTypes;
                default: return WallTypes;
            }
        }

        private static float SlotHeight(PuzzleSlotSize size)
        {
            switch (size)
            {
                case PuzzleSlotSize.Floor: return FloorSlotHeight;
                case PuzzleSlotSize.Table: return TableSlotHeight;
                default: return WallSlotHeight;
            }
        }

        private static float YawOf(GrayboxDirection facing)
        {
            return facing.YawDegrees();
        }

        private static string NamePrefix(GrayboxMarkerKind kind)
        {
            switch (kind)
            {
                case GrayboxMarkerKind.PuzzleSlot: return "PuzzleSlot";
                case GrayboxMarkerKind.ReferenceSlot: return "ReferenceSlot";
                case GrayboxMarkerKind.StageGate: return "StageGate";
                case GrayboxMarkerKind.Lamp: return "Lamp";
                case GrayboxMarkerKind.LightSwitch: return "LightSwitch";
                case GrayboxMarkerKind.KeyHideSpot: return "KeyHide";
                case GrayboxMarkerKind.CologneSpawnSpot: return "CologneSpot";
                case GrayboxMarkerKind.HumanSpawn: return "HumanSpawn";
                case GrayboxMarkerKind.JinnSpawn: return "JinnSpawn";
                case GrayboxMarkerKind.ToolShedItem: return "ToolShedItem";
                case GrayboxMarkerKind.DigSpot: return "DigSpot";
                default: return kind.ToString();
            }
        }

        /// <summary>Collects the specs, names them and keeps the per-parent counters that make names unique.</summary>
        private sealed class SpecBuilder
        {
            private readonly GrayboxLayout _layout;
            private readonly Dictionary<string, int> _counters = new Dictionary<string, int>();

            public List<GrayboxMarkerSpec> Specs { get; } = new List<GrayboxMarkerSpec>();

            public SpecBuilder(GrayboxLayout layout)
            {
                _layout = layout;
            }

            // ---- derived from the layout ----------------------------------------------------------------

            public void AddRooms()
            {
                for (int i = 0; i < _layout.Rooms.Count; i++)
                {
                    GrayboxRoom room = _layout.Rooms[i];
                    if (RoomIdOf(room.Id) < 0)
                    {
                        throw new InvalidOperationException("GrayboxMarkers: room '" + room.Id
                            + "' has no id in RoomKeyTable; append it there (never reorder the table).");
                    }

                    GrayboxMarkerSpec spec = GrayboxMarkerSpec.Room(room.Id, room.Floor, room.IsOutdoor, RoomBoxes(room));
                    spec.ParentPath = GrayboxNaming.RoomParentName(room);
                    spec.Name = GrayboxNaming.RoomGroupName(room);
                    Specs.Add(spec);
                }
            }

            public void AddStageZones()
            {
                // The cellar belongs to no stage: it opens through the hatch after the last active puzzle stage.
                var stages = new List<int>();
                for (int i = 0; i < _layout.Rooms.Count; i++)
                {
                    if (_layout.Rooms[i].Floor != GrayboxLayout.CellarFloor && !stages.Contains(_layout.Rooms[i].Stage))
                    {
                        stages.Add(_layout.Rooms[i].Stage);
                    }
                }
                stages.Sort();

                for (int s = 0; s < stages.Count; s++)
                {
                    var keys = new List<string>();
                    for (int i = 0; i < _layout.Rooms.Count; i++)
                    {
                        if (_layout.Rooms[i].Floor != GrayboxLayout.CellarFloor && _layout.Rooms[i].Stage == stages[s])
                        {
                            keys.Add(_layout.Rooms[i].Id);
                        }
                    }

                    GrayboxMarkerSpec spec = GrayboxMarkerSpec.StageZone(stages[s], keys);
                    spec.ParentPath = GrayboxNaming.MarkersGroupName;
                    spec.Name = "Stage_" + stages[s];
                    Specs.Add(spec);
                }
            }

            /// <summary>
            /// The front door completes stage 0 (needs the house key), a stage gate door completes the stage of the room it
            /// leads out of (the lower stage of its two rooms), the cellar hatch completes the stage after the last active one
            /// (needs the cellar key). The marker lives in the room on the A side of the door (the room side for the hatch).
            /// </summary>
            public void AddGates()
            {
                for (int i = 0; i < _layout.Doors.Count; i++)
                {
                    GrayboxDoor door = _layout.Doors[i];
                    if (door.Kind == GrayboxDoorKind.CellarHatch)
                    {
                        AddCellarHatch(door);
                        continue;
                    }
                    if (door.Kind != GrayboxDoorKind.StageGate && door.Kind != GrayboxDoorKind.FrontDoor)
                    {
                        continue;
                    }

                    GrayboxRoom a = _layout.RoomAt(door.Floor, door.CellA);
                    GrayboxRoom b = _layout.RoomAt(door.Floor, door.CellB);
                    if (a == null && b == null)
                    {
                        throw new InvalidOperationException("GrayboxMarkers: the " + door + " joins no room.");
                    }
                    GrayboxRoom home = a ?? b;
                    int stage = a == null ? b.Stage : (b == null ? a.Stage : Math.Min(a.Stage, b.Stage));
                    GrayboxEdge edge = door.Edge;
                    var position = new Vector3(edge.CenterCellX * GrayboxLayout.CellSize, door.Floor * GrayboxLayout.FloorHeight,
                        edge.CenterCellZ * GrayboxLayout.CellSize);
                    string lockItem = door.Kind == GrayboxDoorKind.FrontDoor ? HouseContract.FrontDoorItemId : string.Empty;

                    AddToRoom(GrayboxMarkerSpec.StageGate(home.Id, stage, edge, lockItem, position), home);
                }
            }

            /// <summary>TODO 4.1: the hatch over the cellar stairs. Its stage is resolved at runtime (HouseContract.GateStage).</summary>
            private void AddCellarHatch(GrayboxDoor door)
            {
                GrayboxRoom home = _layout.RoomAt(door.Floor, door.CellA) ?? _layout.RoomAt(door.Floor, door.CellB);
                if (home == null)
                {
                    throw new InvalidOperationException("GrayboxMarkers: the " + door + " joins no room.");
                }
                GrayboxEdge edge = door.Edge;
                var position = new Vector3(edge.CenterCellX * GrayboxLayout.CellSize, door.Floor * GrayboxLayout.FloorHeight,
                    edge.CenterCellZ * GrayboxLayout.CellSize);
                AddToRoom(GrayboxMarkerSpec.StageGate(home.Id, home.Stage, edge, HouseContract.CellarKeyItemId, position, true), home);
            }

            /// <summary>Every well, woodpile and pot of the garden is a place the key may hide in.</summary>
            public void AddKeyHideSpots()
            {
                for (int i = 0; i < _layout.Props.Count; i++)
                {
                    GrayboxProp prop = _layout.Props[i];
                    float height;
                    switch (prop.Kind)
                    {
                        case GrayboxPropKind.Well: height = KeyHideWellHeight; break;
                        case GrayboxPropKind.Woodpile: height = KeyHideWoodpileHeight; break;
                        case GrayboxPropKind.Pot: height = KeyHidePotHeight; break;
                        default: continue; // trees stand outside the fence
                    }

                    GrayboxRoom room = _layout.RoomAt(GrayboxLayout.GroundFloor, prop.Cell);
                    if (room == null)
                    {
                        throw new InvalidOperationException("GrayboxMarkers: " + prop + " does not stand in a room.");
                    }

                    var position = new Vector3(prop.CellX * GrayboxLayout.CellSize, GrayboxLayout.GroundFloor * GrayboxLayout.FloorHeight + height,
                        prop.CellZ * GrayboxLayout.CellSize);
                    AddToRoom(GrayboxMarkerSpec.Point(GrayboxMarkerKind.KeyHideSpot, room.Id, position, prop.YawDegrees), room);
                }
            }

            // ---- authored markers -----------------------------------------------------------------------

            /// <summary>
            /// The hand-placed markers. x/z are house-local metres, h is the height above the floor surface and
            /// <c>facing</c> the direction the marker looks into the room (away from its wall). Room extents are in
            /// <see cref="GrayboxLayout"/>: hall X4..16 Z0..4, kitchen X0..8 Z4..16 (+X0..4 Z0..4), living X12..20 Z4..16
            /// (+X16..20 Z0..4), pantry X8..12 Z12..16 (the cellar hatch hole X8..12 Z8..12 below it), stairwell X8..12 Z4..8, shed
            /// X24..28 Z16..20; first floor: landing X8..12 Z8..16, bedroom1 X0..8 Z8..16, bedroom2 X12..20 Z8..16, corridor X4..16
            /// Z0..4 (+ X12..16 Z4..8), attic stairs X4..8 Z4..8, bathroom X0..4 Z0..8, study X16..20 Z0..8; attic: loft X0..12 Z0..16
            /// around the shaft X4..8 Z4..8, ritual X12..20 Z0..16; cellar X0..20 Z0..12. GrayboxMarkersTests checks that every marker
            /// is inside its room.
            /// </summary>
            public void AddAuthored()
            {
                const GrayboxDirection N = GrayboxDirection.North;
                const GrayboxDirection E = GrayboxDirection.East;
                const GrayboxDirection S = GrayboxDirection.South;
                const GrayboxDirection W = GrayboxDirection.West;
                const PuzzleSlotSize Wall = PuzzleSlotSize.Wall;
                const PuzzleSlotSize Floor = PuzzleSlotSize.Floor;
                const PuzzleSlotSize Table = PuzzleSlotSize.Table;

                // Human: in the front yard, eight metres in front of the door, looking at the house.
                HumanSpawn("garden", 10f, -8f, N);

                // Stage 0 - tool shed (the blessed oil lies on a shelf at the back).
                Lamp("shed", 26f, 18f);
                Switch("shed", 24.9f, 16.6f, N);
                ToolShedItem("shed", HouseContract.BlessedOilItemId, 26.5f, 1.0f, 19.1f, S);

                // Stage 1 - ground floor.
                Lamp("hall", 10f, 2f);
                Switch("hall", 11.9f, 0.6f, N);
                PuzzleSlot("hall", Wall, 6.5f, 3.4f, S);
                ReferenceSlot("hall", Wall, 13.5f, 3.4f, S);

                Lamp("kitchen", 4f, 8f);
                Switch("kitchen", 7.4f, 12.2f, W);
                PuzzleSlot("kitchen", Wall, 0.6f, 9f, E);
                ReferenceSlot("kitchen", Wall, 2f, 15.4f, S);
                ReferenceSlot("kitchen", Floor, 6f, 6f, N);
                Cologne("kitchen", 6.5f, 0.95f, 15f);
                JinnSpawn("kitchen", 3f, 8f);

                Lamp("living", 16f, 8f);
                Switch("living", 16.6f, 3.5f, E);
                PuzzleSlot("living", Floor, 18f, 9f, W);
                ReferenceSlot("living", Table, 18f, 15f, S);
                ReferenceSlot("living", Wall, 19.4f, 10f, W);
                Cologne("living", 19f, 0.8f, 5f);
                JinnSpawn("living", 14f, 8f);

                // The pantry is one cell now: doors west and east, the cellar hatch in its south wall.
                Lamp("pantry", 10f, 14f);
                Switch("pantry", 8.6f, 12.6f, E);
                PuzzleSlot("pantry", Table, 9.2f, 14.8f, S);
                ReferenceSlot("pantry", Wall, 11.2f, 15.4f, S);

                // The lamp hangs on the west wall near the stairs entrance, out of the way of whoever climbs them (a lamp over the
                // middle of the steps would hit the climber's head: possessable lamps have a collider).
                Lamp("stairwell", 8.6f, 5f, 3.4f);
                Switch("stairwell", 8.6f, 4.7f, E);

                // Stage 2 - first floor.
                Lamp("landing", 10f, 12f);
                Switch("landing", 8.6f, 12.3f, E);
                ReferenceSlot("landing", Floor, 10f, 14.5f, S);

                Lamp("bedroom1", 4f, 12f);
                Switch("bedroom1", 7.4f, 12.3f, W);
                PuzzleSlot("bedroom1", Wall, 0.6f, 12f, E);
                ReferenceSlot("bedroom1", Table, 4f, 15f, S);
                JinnSpawn("bedroom1", 3f, 12f);

                Lamp("bedroom2", 16f, 12f);
                Switch("bedroom2", 12.6f, 12.3f, E);
                PuzzleSlot("bedroom2", Floor, 18f, 14f, W);
                ReferenceSlot("bedroom2", Wall, 19.4f, 11f, W);
                Cologne("bedroom2", 14f, 0.7f, 15f);

                Lamp("corridor", 10f, 2f);
                Switch("corridor", 4.6f, 3.5f, E);
                PuzzleSlot("corridor", Wall, 7f, 0.6f, N);
                ReferenceSlot("corridor", Wall, 13f, 0.6f, N);

                Lamp("bathroom", 2f, 4f);
                Switch("bathroom", 3.4f, 3.6f, W);
                ReferenceSlot("bathroom", Wall, 0.6f, 4.5f, E);
                Cologne("bathroom", 0.9f, 1.0f, 6.5f);

                Lamp("study", 18f, 4f);
                Switch("study", 16.6f, 3.6f, E);
                PuzzleSlot("study", Table, 19f, 3f, W);
                ReferenceSlot("study", Wall, 19.4f, 6f, W);

                // The attic stairs (behind the stage 2 gate); the lamp hangs on the west wall like the stairwell's.
                Lamp("atticstairs", 4.6f, 5f, 3.4f);
                Switch("atticstairs", 4.6f, 4.7f, E);

                // Stage 3 - attic (TODO 4.7). The loft wraps the attic stair shaft X4..8 Z4..8; the ritual room is X12..20.
                Lamp("attic", 6f, 12f);
                Switch("attic", 11.4f, 4f, W);
                PuzzleSlot("attic", Wall, 0.6f, 4f, E);
                PuzzleSlot("attic", Floor, 10f, 11f, N);
                ReferenceSlot("attic", Wall, 6f, 15.4f, S);
                ReferenceSlot("attic", Floor, 2f, 14f, S);

                Lamp("ritual", 16f, 8f);
                Switch("ritual", 12.6f, 4f, E);
                PuzzleSlot("ritual", Table, 16f, 9f, S);
                PuzzleSlot("ritual", Wall, 19.4f, 8f, W);
                ReferenceSlot("ritual", Wall, 16f, 15.4f, S);

                // The cellar (floor -1) under the front of the house: the stairs come down at X8..12 Z8..12 (bottom at Z8). It has no
                // door, so its switch hangs on the north wall beside the stairs.
                Lamp("cellar", 6f, 6f);
                Lamp("cellar", 16f, 6f);
                Switch("cellar", 7.2f, 11.4f, S);
                DigSpot("cellar", 3f, 3f);
                DigSpot("cellar", 3f, 9f);
                DigSpot("cellar", 16f, 3f);
                DigSpot("cellar", 17f, 9f);
            }

            private void DigSpot(string roomKey, float x, float z)
            {
                AddAt(GrayboxMarkerKind.DigSpot, roomKey, x, DigSpotHeight, z, GrayboxDirection.North);
            }

            private void HumanSpawn(string roomKey, float x, float z, GrayboxDirection facing)
            {
                AddAt(GrayboxMarkerKind.HumanSpawn, roomKey, x, SpawnHeight, z, facing);
            }

            private void JinnSpawn(string roomKey, float x, float z)
            {
                AddAt(GrayboxMarkerKind.JinnSpawn, roomKey, x, SpiritSpawnHeight, z, GrayboxDirection.North);
            }

            private void Lamp(string roomKey, float x, float z, float height = LampHeight)
            {
                AddAt(GrayboxMarkerKind.Lamp, roomKey, x, height, z, GrayboxDirection.North);
            }

            private void Switch(string roomKey, float x, float z, GrayboxDirection facing)
            {
                AddAt(GrayboxMarkerKind.LightSwitch, roomKey, x, SwitchHeight, z, facing);
            }

            private void Cologne(string roomKey, float x, float height, float z)
            {
                AddAt(GrayboxMarkerKind.CologneSpawnSpot, roomKey, x, height, z, GrayboxDirection.North);
            }

            private void ToolShedItem(string roomKey, string itemId, float x, float height, float z, GrayboxDirection facing)
            {
                GrayboxRoom room = Room(roomKey);
                AddToRoom(GrayboxMarkerSpec.ToolShedItem(roomKey, itemId, At(room, x, height, z), YawOf(facing)), room);
            }

            private void PuzzleSlot(string roomKey, PuzzleSlotSize size, float x, float z, GrayboxDirection facing)
            {
                GrayboxRoom room = Room(roomKey);
                Vector3 position = At(room, x, SlotHeight(size), z);
                float yaw = YawOf(facing);
                float radians = yaw * Mathf.Deg2Rad;
                var reward = new Vector3(position.x + Mathf.Sin(radians) * RewardDistance,
                    room.Floor * GrayboxLayout.FloorHeight + RewardHeight, position.z + Mathf.Cos(radians) * RewardDistance);
                AddToRoom(GrayboxMarkerSpec.PuzzleSlot(roomKey, room.Stage, size, TypesFor(size), position, yaw, reward), room);
            }

            private void ReferenceSlot(string roomKey, PuzzleSlotSize size, float x, float z, GrayboxDirection facing)
            {
                GrayboxRoom room = Room(roomKey);
                AddToRoom(GrayboxMarkerSpec.ReferenceSlot(roomKey, size, TypesFor(size), At(room, x, SlotHeight(size), z), YawOf(facing)), room);
            }

            private void AddAt(GrayboxMarkerKind kind, string roomKey, float x, float height, float z, GrayboxDirection facing)
            {
                GrayboxRoom room = Room(roomKey);
                AddToRoom(GrayboxMarkerSpec.Point(kind, roomKey, At(room, x, height, z), YawOf(facing)), room);
            }

            // ---- helpers --------------------------------------------------------------------------------

            private GrayboxRoom Room(string roomKey)
            {
                GrayboxRoom room = _layout.FindRoom(roomKey);
                if (room == null)
                {
                    throw new InvalidOperationException("GrayboxMarkers: the layout has no room '" + roomKey + "'.");
                }
                return room;
            }

            private static Vector3 At(GrayboxRoom room, float x, float heightAboveFloor, float z)
            {
                return new Vector3(x, room.Floor * GrayboxLayout.FloorHeight + heightAboveFloor, z);
            }

            /// <summary>Names the spec after its kind (Lamp_01, Lamp_02, ...) and parents it to the room's Markers group.</summary>
            private void AddToRoom(GrayboxMarkerSpec spec, GrayboxRoom room)
            {
                spec.ParentPath = GrayboxNaming.RoomMarkersPath(room);
                string prefix = NamePrefix(spec.Kind);
                string counterKey = spec.ParentPath + GrayboxNaming.PathSeparator + prefix;
                int count;
                _counters.TryGetValue(counterKey, out count);
                count++;
                _counters[counterKey] = count;
                spec.Name = prefix + "_" + count.ToString("00");
                Specs.Add(spec);
            }

            /// <summary>
            /// One box per rectangle of the room. Indoor boxes are one floor high; the garden's are tall; a rectangle that holds the
            /// cells of stairs starting on its floor is two floors high because the stair shaft stays open above it.
            /// </summary>
            private List<Bounds> RoomBoxes(GrayboxRoom room)
            {
                var boxes = new List<Bounds>(room.Rects.Count);
                for (int i = 0; i < room.Rects.Count; i++)
                {
                    GrayboxRect rect = room.Rects[i];
                    if (rect.CellCount == 0)
                    {
                        continue;
                    }

                    float height = BoxHeight(room, rect);
                    var size = new Vector3(rect.Width * GrayboxLayout.CellSize, height, rect.Depth * GrayboxLayout.CellSize);
                    var center = new Vector3((rect.X + rect.Width * 0.5f) * GrayboxLayout.CellSize,
                        room.Floor * GrayboxLayout.FloorHeight + height * 0.5f,
                        (rect.Z + rect.Depth * 0.5f) * GrayboxLayout.CellSize);
                    boxes.Add(new Bounds(center, size));
                }
                return boxes;
            }

            private float BoxHeight(GrayboxRoom room, GrayboxRect rect)
            {
                if (room.IsOutdoor)
                {
                    return OutdoorBoxHeight;
                }
                for (int s = 0; s < _layout.Stairs.Count; s++)
                {
                    if (_layout.Stairs[s].BaseFloor != room.Floor)
                    {
                        continue;
                    }
                    for (int c = 0; c < _layout.Stairs[s].Cells.Count; c++)
                    {
                        if (rect.Contains(_layout.Stairs[s].Cells[c]))
                        {
                            return GrayboxLayout.FloorHeight * 2f;
                        }
                    }
                }
                return GrayboxLayout.FloorHeight;
            }
        }
    }
}
