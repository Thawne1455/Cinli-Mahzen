using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.World;
using UnityEngine;

namespace CinliMahzen.Tests.EditMode.World
{
    /// <summary>
    /// A small hand-made house for the House* tests: five rooms, every contract rule satisfied (only the two "arrives later"
    /// warnings remain: no DigSpots and no stage 2 gate). Tests take <see cref="Valid"/>, change the public lists and call
    /// <see cref="Build"/> to break exactly one rule.
    ///
    /// Rooms (X east, Y up, Z north): garden (id 0, outdoor, stage 0) X-20..20 Z-20..0; hall (1) and kitchen (2) on the ground
    /// floor (stage 1) X0..10 / X10..20 Z0..10; upstairs (3) and study (4) one floor up (stage 2), same footprints.
    /// </summary>
    internal sealed class HouseFixture
    {
        public const int GardenId = 0;
        public const int HallId = 1;
        public const int KitchenId = 2;
        public const int UpstairsId = 3;
        public const int StudyId = 4;

        public static readonly PuzzleType[] AnyWallTypes = { PuzzleType.Paintings, PuzzleType.Wires };

        public readonly List<HouseRoomEntry> Rooms = new List<HouseRoomEntry>();
        public readonly List<HouseStageEntry> Stages = new List<HouseStageEntry>();
        public readonly List<HousePuzzleSlotEntry> PuzzleSlots = new List<HousePuzzleSlotEntry>();
        public readonly List<HouseReferenceSlotEntry> ReferenceSlots = new List<HouseReferenceSlotEntry>();
        public readonly List<HouseGateEntry> Gates = new List<HouseGateEntry>();
        public readonly List<HouseSpotEntry> Lamps = new List<HouseSpotEntry>();
        public readonly List<HouseSpotEntry> Switches = new List<HouseSpotEntry>();
        public readonly List<HouseSpotEntry> KeyHides = new List<HouseSpotEntry>();
        public readonly List<HouseSpotEntry> Colognes = new List<HouseSpotEntry>();
        public readonly List<HouseSpotEntry> Digs = new List<HouseSpotEntry>();
        public readonly List<HouseSpotEntry> ToolShed = new List<HouseSpotEntry>();
        public readonly List<Vector3> HumanSpawns = new List<Vector3>();
        public readonly List<Vector3> JinnSpawns = new List<Vector3>();

        private int _serial;

        public static Bounds Box(float minX, float minY, float minZ, float maxX, float maxY, float maxZ)
        {
            var size = new Vector3(maxX - minX, maxY - minY, maxZ - minZ);
            return new Bounds(new Vector3(minX, minY, minZ) + size * 0.5f, size);
        }

        /// <summary>
        /// A position at the given height above the floor inside the room. House rooms: x and z are offsets (0..10) from the
        /// room's south-west corner, a floor is 4 m high. Garden: x is 0..20 from its west end, z is 0..18 and runs southwards
        /// from the house wall (so a larger z is further from the house).
        /// </summary>
        public static Vector3 Inside(int roomId, float x, float height, float z)
        {
            switch (roomId)
            {
                case GardenId: return new Vector3(-10f + x, height, -2f - z);
                case HallId: return new Vector3(x, height, z);
                case KitchenId: return new Vector3(10f + x, height, z);
                case UpstairsId: return new Vector3(x, 4f + height, z);
                default: return new Vector3(10f + x, 4f + height, z);
            }
        }

        public static HouseFixture Valid()
        {
            var f = new HouseFixture();

            f.Rooms.Add(new HouseRoomEntry(GardenId, "garden", 0, true, new[] { Box(-20f, 0f, -20f, 20f, 12f, 0f) }));
            f.Rooms.Add(new HouseRoomEntry(HallId, "hall", 0, false, new[] { Box(0f, 0f, 0f, 10f, 4f, 10f) }));
            f.Rooms.Add(new HouseRoomEntry(KitchenId, "kitchen", 0, false, new[] { Box(10f, 0f, 0f, 20f, 4f, 10f) }));
            f.Rooms.Add(new HouseRoomEntry(UpstairsId, "upstairs", 1, false, new[] { Box(0f, 4f, 0f, 10f, 8f, 10f) }));
            f.Rooms.Add(new HouseRoomEntry(StudyId, "study", 1, false, new[] { Box(10f, 4f, 0f, 20f, 8f, 10f) }));

            f.Stages.Add(new HouseStageEntry(0, new[] { GardenId }));
            f.Stages.Add(new HouseStageEntry(1, new[] { HallId, KitchenId }));
            f.Stages.Add(new HouseStageEntry(2, new[] { UpstairsId, StudyId }));

            // Three puzzle slots per puzzle stage, in two different rooms.
            f.AddPuzzleSlot(1, HallId);
            f.AddPuzzleSlot(1, KitchenId);
            f.AddPuzzleSlot(1, HallId);
            f.AddPuzzleSlot(2, UpstairsId);
            f.AddPuzzleSlot(2, StudyId);
            f.AddPuzzleSlot(2, UpstairsId);

            int[] referenceRooms = { HallId, KitchenId, UpstairsId, StudyId, HallId, KitchenId, UpstairsId, StudyId };
            for (int i = 0; i < referenceRooms.Length; i++)
            {
                f.AddReferenceSlot(referenceRooms[i]);
            }

            f.Gates.Add(new HouseGateEntry(f.NextPath("StageGate", "StageGate"), 0, HouseContract.FrontDoorItemId, Inside(HallId, 5f, 0f, 0f)));
            f.Gates.Add(new HouseGateEntry(f.NextPath("StageGate", "StageGate"), 1, string.Empty, Inside(HallId, 5f, 0f, 9f)));

            int[] indoorRooms = { HallId, KitchenId, UpstairsId, StudyId };
            for (int i = 0; i < indoorRooms.Length; i++)
            {
                f.AddLamp(indoorRooms[i]);
                f.AddSwitch(indoorRooms[i]);
            }

            for (int i = 0; i < HouseContract.MinKeyHideSpots; i++)
            {
                f.KeyHides.Add(new HouseSpotEntry(f.NextPath("KeyHide", "KeyHideSpot"), HouseSpotKind.KeyHide, HouseIndex.NoRoom,
                    string.Empty, Inside(GardenId, 2f + i, 0.6f, 5f)));
            }

            int[] cologneRooms = { HallId, KitchenId, UpstairsId };
            for (int i = 0; i < cologneRooms.Length; i++)
            {
                f.Colognes.Add(new HouseSpotEntry(f.NextPath("Cologne", "CologneSpawnSpot"), HouseSpotKind.Cologne, cologneRooms[i],
                    string.Empty, Inside(cologneRooms[i], 8f, 0.9f, 8f)));
            }

            f.ToolShed.Add(new HouseSpotEntry(f.NextPath("ToolShedItem", "ToolShedItemPoint"), HouseSpotKind.ToolShedItem, HouseIndex.NoRoom,
                HouseContract.BlessedOilItemId, Inside(GardenId, 15f, 1f, 15f)));

            f.HumanSpawns.Add(Inside(GardenId, 10f, 0.1f, 2f));
            f.JinnSpawns.Add(Inside(HallId, 3f, 1.6f, 3f));
            f.JinnSpawns.Add(Inside(KitchenId, 3f, 1.6f, 3f));
            f.JinnSpawns.Add(Inside(UpstairsId, 3f, 1.6f, 3f));
            return f;
        }

        public string NextPath(string name, string markerType)
        {
            _serial++;
            return HouseIndex.MakeNetKey("Test/" + name + "_" + _serial.ToString("000"), markerType);
        }

        public HousePuzzleSlotEntry AddPuzzleSlot(int stage, int roomId, PuzzleSlotSize size = PuzzleSlotSize.Wall)
        {
            Vector3 position = Inside(roomId, 2f, 1.5f, 5f);
            var slot = new HousePuzzleSlotEntry(NextPath("PuzzleSlot", "PuzzleSlot"), stage, roomId, size, AnyWallTypes, position,
                position + Vector3.forward);
            PuzzleSlots.Add(slot);
            return slot;
        }

        public HouseReferenceSlotEntry AddReferenceSlot(int roomId, PuzzleSlotSize size = PuzzleSlotSize.Wall)
        {
            var slot = new HouseReferenceSlotEntry(NextPath("ReferenceSlot", "ReferenceSlot"), roomId, size, AnyWallTypes,
                Inside(roomId, 2f, 1.5f, 8f));
            ReferenceSlots.Add(slot);
            return slot;
        }

        public HouseSpotEntry AddLamp(int roomId)
        {
            var lamp = new HouseSpotEntry(NextPath("Lamp", "LampMarker"), HouseSpotKind.Lamp, roomId, string.Empty, Inside(roomId, 5f, 3.2f, 5f));
            Lamps.Add(lamp);
            return lamp;
        }

        public HouseSpotEntry AddSwitch(int roomId)
        {
            var light = new HouseSpotEntry(NextPath("LightSwitch", "LightSwitch"), HouseSpotKind.LightSwitch, roomId, string.Empty,
                Inside(roomId, 1f, 1.4f, 1f));
            Switches.Add(light);
            return light;
        }

        public HouseIndexBuilder ToBuilder()
        {
            var builder = new HouseIndexBuilder();
            for (int i = 0; i < Rooms.Count; i++) builder.Add(Rooms[i]);
            for (int i = 0; i < Stages.Count; i++) builder.Add(Stages[i]);
            for (int i = 0; i < PuzzleSlots.Count; i++) builder.Add(PuzzleSlots[i]);
            for (int i = 0; i < ReferenceSlots.Count; i++) builder.Add(ReferenceSlots[i]);
            for (int i = 0; i < Gates.Count; i++) builder.Add(Gates[i]);
            AddAll(builder, Lamps);
            AddAll(builder, Switches);
            AddAll(builder, KeyHides);
            AddAll(builder, Colognes);
            AddAll(builder, Digs);
            AddAll(builder, ToolShed);
            for (int i = 0; i < HumanSpawns.Count; i++) builder.AddHumanSpawn("Human_" + i, HumanSpawns[i]);
            for (int i = 0; i < JinnSpawns.Count; i++) builder.AddJinnSpawn("Jinn_" + i, JinnSpawns[i]);
            return builder;
        }

        public HouseIndex Build()
        {
            return ToBuilder().Build();
        }

        private static void AddAll(HouseIndexBuilder builder, List<HouseSpotEntry> spots)
        {
            for (int i = 0; i < spots.Count; i++)
            {
                builder.Add(spots[i]);
            }
        }
    }
}
