using System;
using System.Collections.Generic;
using CinliMahzen.Core;
using UnityEngine;

namespace CinliMahzen.World.Graybox
{
    /// <summary>
    /// One marker of the graybox house as plain data (TODO 2.2): what the editor builder turns into a marker component and
    /// what <see cref="GrayboxHouseIndexFactory"/> turns into index entries. Positions are house-local metres (X east, Y up,
    /// Z north); yaw turns forward (+Z) about +Y like <see cref="GrayboxDirectionUtil.YawDegrees"/>. Only the fields of the
    /// spec's <see cref="Kind"/> are meaningful; the rest keep neutral defaults. Created by <see cref="GrayboxMarkers"/>.
    /// </summary>
    public sealed class GrayboxMarkerSpec
    {
        public GrayboxMarkerKind Kind { get; }

        /// <summary>Key of the room whose group holds the marker (the room itself for <see cref="GrayboxMarkerKind.Room"/>); empty for house-level markers.</summary>
        public string RoomKey { get; }

        /// <summary>Hierarchy path of the parent object relative to the house root, e.g. "Floor0/Room_Hall/Markers".</summary>
        public string ParentPath { get; internal set; } = string.Empty;

        /// <summary>Name of the GameObject; unique inside its parent.</summary>
        public string Name { get; internal set; } = string.Empty;

        public Vector3 Position { get; private set; }
        public float YawDegrees { get; private set; }

        /// <summary>Room: floor (-1 cellar, 0 ground, 1 first, 2 attic).</summary>
        public int Floor { get; private set; }

        public bool IsOutdoor { get; private set; }

        /// <summary>Room: one box per rectangle, in house-local space.</summary>
        public IReadOnlyList<Bounds> Boxes { get; private set; } = Array.Empty<Bounds>();

        /// <summary>StageZone / PuzzleSlot / StageGate: the stage index.</summary>
        public int Stage { get; private set; }

        /// <summary>StageZone: keys of the rooms in the stage.</summary>
        public IReadOnlyList<string> RoomKeys { get; private set; } = Array.Empty<string>();

        public PuzzleSlotSize Size { get; private set; }
        public IReadOnlyList<PuzzleType> AllowedTypes { get; private set; } = Array.Empty<PuzzleType>();

        /// <summary>PuzzleSlot: where the reward item appears.</summary>
        public Vector3 RewardPosition { get; private set; }

        /// <summary>StageGate: the layout door whose door piece the gate controls.</summary>
        public GrayboxEdge Edge { get; private set; }

        /// <summary>StageGate: the cellar hatch (its stage follows StagesActive, see HouseContract.GateStage).</summary>
        public bool IsCellarHatch { get; private set; }

        /// <summary>StageGate: the lock item id (empty = no GateLock). ToolShedItem: the item placed there.</summary>
        public string ItemId { get; private set; } = string.Empty;

        /// <summary>Path of the GameObject relative to the house root.</summary>
        public string Path
        {
            get { return ParentPath.Length == 0 ? Name : ParentPath + GrayboxNaming.PathSeparator + Name; }
        }

        private GrayboxMarkerSpec(GrayboxMarkerKind kind, string roomKey)
        {
            Kind = kind;
            RoomKey = roomKey ?? string.Empty;
        }

        internal static GrayboxMarkerSpec Room(string roomKey, int floor, bool isOutdoor, IEnumerable<Bounds> boxes)
        {
            return new GrayboxMarkerSpec(GrayboxMarkerKind.Room, roomKey)
            {
                Floor = floor,
                IsOutdoor = isOutdoor,
                Boxes = new List<Bounds>(boxes)
            };
        }

        internal static GrayboxMarkerSpec StageZone(int stage, IEnumerable<string> roomKeys)
        {
            return new GrayboxMarkerSpec(GrayboxMarkerKind.StageZone, string.Empty)
            {
                Stage = stage,
                RoomKeys = new List<string>(roomKeys)
            };
        }

        internal static GrayboxMarkerSpec PuzzleSlot(string roomKey, int stage, PuzzleSlotSize size, IEnumerable<PuzzleType> types,
            Vector3 position, float yawDegrees, Vector3 rewardPosition)
        {
            return new GrayboxMarkerSpec(GrayboxMarkerKind.PuzzleSlot, roomKey)
            {
                Stage = stage,
                Size = size,
                AllowedTypes = new List<PuzzleType>(types),
                Position = position,
                YawDegrees = yawDegrees,
                RewardPosition = rewardPosition
            };
        }

        internal static GrayboxMarkerSpec ReferenceSlot(string roomKey, PuzzleSlotSize size, IEnumerable<PuzzleType> types,
            Vector3 position, float yawDegrees)
        {
            return new GrayboxMarkerSpec(GrayboxMarkerKind.ReferenceSlot, roomKey)
            {
                Size = size,
                AllowedTypes = new List<PuzzleType>(types),
                Position = position,
                YawDegrees = yawDegrees
            };
        }

        internal static GrayboxMarkerSpec StageGate(string roomKey, int stage, GrayboxEdge edge, string lockItemId, Vector3 position,
            bool isCellarHatch = false)
        {
            return new GrayboxMarkerSpec(GrayboxMarkerKind.StageGate, roomKey)
            {
                Stage = stage,
                Edge = edge,
                ItemId = lockItemId ?? string.Empty,
                Position = position,
                IsCellarHatch = isCellarHatch
            };
        }

        internal static GrayboxMarkerSpec ToolShedItem(string roomKey, string itemId, Vector3 position, float yawDegrees)
        {
            return new GrayboxMarkerSpec(GrayboxMarkerKind.ToolShedItem, roomKey)
            {
                ItemId = itemId ?? string.Empty,
                Position = position,
                YawDegrees = yawDegrees
            };
        }

        /// <summary>Lamp, LightSwitch, KeyHideSpot, CologneSpawnSpot, HumanSpawn, JinnSpawn and DigSpot: just a place and a facing.</summary>
        internal static GrayboxMarkerSpec Point(GrayboxMarkerKind kind, string roomKey, Vector3 position, float yawDegrees)
        {
            return new GrayboxMarkerSpec(kind, roomKey)
            {
                Position = position,
                YawDegrees = yawDegrees
            };
        }

        public override string ToString()
        {
            return Kind + " " + Path;
        }
    }
}
