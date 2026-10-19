using System;
using System.Collections.Generic;
using CinliMahzen.Core;
using UnityEngine;

namespace CinliMahzen.World
{
    /// <summary>
    /// Plain-data snapshot of every house marker (Teknik 7.5, 8): rooms and their boxes, stages, slots, gates, spots and
    /// spawn points. Built once by <see cref="HouseIndexBuilder"/> (from marker components in <see cref="HouseService.Index"/>, or
    /// from graybox data in tests) and never changed afterwards. No MonoBehaviour is referenced, so RoundSetupPlanner and the
    /// validator are testable in EditMode.
    ///
    /// Order: rooms by id, stages by index, NetId-carrying lists by NetId (= sorted hierarchy path), spawn points by path.
    /// All of that is independent of hierarchy order, so every machine sees the same lists.
    /// </summary>
    public sealed class HouseIndex
    {
        /// <summary>Room id meaning "outside every room" (same value as <see cref="IRoomService.RoomOf"/>).</summary>
        public const int NoRoom = -1;

        /// <summary>How many house objects fit into the level NetId range 1000..59999.</summary>
        public const int MaxNetIdCount = NetId.RuntimeMin - NetId.LevelMin;

        private const string NetKeySeparator = "#";

        public IReadOnlyList<HouseRoomEntry> Rooms { get; }
        public IReadOnlyList<HouseStageEntry> Stages { get; }
        public IReadOnlyList<HousePuzzleSlotEntry> PuzzleSlots { get; }
        public IReadOnlyList<HouseReferenceSlotEntry> ReferenceSlots { get; }
        public IReadOnlyList<HouseGateEntry> Gates { get; }
        public IReadOnlyList<HouseSpotEntry> Lamps { get; }
        public IReadOnlyList<HouseSpotEntry> LightSwitches { get; }
        public IReadOnlyList<HouseSpotEntry> KeyHideSpots { get; }
        public IReadOnlyList<HouseSpotEntry> CologneSpots { get; }
        public IReadOnlyList<HouseSpotEntry> DigSpots { get; }
        public IReadOnlyList<HouseSpotEntry> ToolShedItems { get; }
        public IReadOnlyList<Vector3> HumanSpawns { get; }
        public IReadOnlyList<Vector3> JinnSpawns { get; }

        /// <summary>Union of every room box (zero-size at the origin when there are no rooms).</summary>
        public Bounds RoomBounds { get; }

        internal HouseIndex(List<HouseRoomEntry> rooms, List<HouseStageEntry> stages, List<HousePuzzleSlotEntry> puzzleSlots,
            List<HouseReferenceSlotEntry> referenceSlots, List<HouseGateEntry> gates, List<HouseSpotEntry> lamps,
            List<HouseSpotEntry> lightSwitches, List<HouseSpotEntry> keyHideSpots, List<HouseSpotEntry> cologneSpots,
            List<HouseSpotEntry> digSpots, List<HouseSpotEntry> toolShedItems, List<Vector3> humanSpawns, List<Vector3> jinnSpawns)
        {
            Rooms = rooms;
            Stages = stages;
            PuzzleSlots = puzzleSlots;
            ReferenceSlots = referenceSlots;
            Gates = gates;
            Lamps = lamps;
            LightSwitches = lightSwitches;
            KeyHideSpots = keyHideSpots;
            CologneSpots = cologneSpots;
            DigSpots = digSpots;
            ToolShedItems = toolShedItems;
            HumanSpawns = humanSpawns;
            JinnSpawns = jinnSpawns;

            Bounds union = new Bounds();
            bool first = true;
            for (int i = 0; i < rooms.Count; i++)
            {
                IReadOnlyList<Bounds> boxes = rooms[i].Boxes;
                for (int b = 0; b < boxes.Count; b++)
                {
                    if (first)
                    {
                        union = boxes[b];
                        first = false;
                    }
                    else
                    {
                        union.Encapsulate(boxes[b]);
                    }
                }
            }
            RoomBounds = union;
        }

        // ------------------------------------------------------------------------------------------------
        // rooms
        // ------------------------------------------------------------------------------------------------

        /// <summary>
        /// Id of the room that contains <paramref name="p"/>, or <see cref="NoRoom"/> (-1). Boxes are half-open (see
        /// <see cref="HouseRoomEntry.TryGetContainingVolume"/>). When several rooms contain the point (overlapping or nested
        /// boxes) the room whose containing box has the SMALLEST volume wins; equal volumes go to the lowest room id.
        /// No allocation.
        /// </summary>
        public int RoomOf(Vector3 p)
        {
            int best = NoRoom;
            float bestVolume = 0f;
            for (int i = 0; i < Rooms.Count; i++)
            {
                float volume;
                if (!Rooms[i].TryGetContainingVolume(p, out volume))
                {
                    continue;
                }
                if (best == NoRoom || volume < bestVolume)
                {
                    best = Rooms[i].Id;
                    bestVolume = volume;
                }
            }
            return best;
        }

        /// <summary>The room with this id, or null.</summary>
        public HouseRoomEntry FindRoom(int roomId)
        {
            for (int i = 0; i < Rooms.Count; i++)
            {
                if (Rooms[i].Id == roomId)
                {
                    return Rooms[i];
                }
            }
            return null;
        }

        /// <summary>The stage zone with this index, or null.</summary>
        public HouseStageEntry FindStage(int stageIndex)
        {
            for (int i = 0; i < Stages.Count; i++)
            {
                if (Stages[i].Index == stageIndex)
                {
                    return Stages[i];
                }
            }
            return null;
        }

        /// <summary>Index of the (first) stage zone that lists the room, or -1.</summary>
        public int StageOfRoom(int roomId)
        {
            for (int i = 0; i < Stages.Count; i++)
            {
                if (Stages[i].Contains(roomId))
                {
                    return Stages[i].Index;
                }
            }
            return -1;
        }

        /// <summary>The room a spot is in: its own room id when it has one, otherwise the room that contains its position (or <see cref="NoRoom"/>).</summary>
        public int ResolveRoom(HouseSpotEntry spot)
        {
            return spot.RoomId != NoRoom ? spot.RoomId : RoomOf(spot.Position);
        }

        public IReadOnlyList<HouseSpotEntry> GetSpots(HouseSpotKind kind)
        {
            switch (kind)
            {
                case HouseSpotKind.Lamp: return Lamps;
                case HouseSpotKind.LightSwitch: return LightSwitches;
                case HouseSpotKind.KeyHide: return KeyHideSpots;
                case HouseSpotKind.Cologne: return CologneSpots;
                case HouseSpotKind.Dig: return DigSpots;
                default: return ToolShedItems;
            }
        }

        /// <summary>Appends every NetId-carrying entry (slots, gates and all spots) to <paramref name="result"/>.</summary>
        public void CollectNetEntries(List<HouseNetEntry> result)
        {
            for (int i = 0; i < PuzzleSlots.Count; i++)
            {
                result.Add(PuzzleSlots[i]);
            }
            for (int i = 0; i < ReferenceSlots.Count; i++)
            {
                result.Add(ReferenceSlots[i]);
            }
            for (int i = 0; i < Gates.Count; i++)
            {
                result.Add(Gates[i]);
            }
            AddAll(result, Lamps);
            AddAll(result, LightSwitches);
            AddAll(result, KeyHideSpots);
            AddAll(result, CologneSpots);
            AddAll(result, DigSpots);
            AddAll(result, ToolShedItems);
        }

        private static void AddAll(List<HouseNetEntry> result, IReadOnlyList<HouseSpotEntry> spots)
        {
            for (int i = 0; i < spots.Count; i++)
            {
                result.Add(spots[i]);
            }
        }

        // ------------------------------------------------------------------------------------------------
        // NetIds
        // ------------------------------------------------------------------------------------------------

        /// <summary>The unique key of a marker: <paramref name="hierarchyPath"/> + "#" + marker type name (two markers of different types may share one GameObject).</summary>
        public static string MakeNetKey(string hierarchyPath, string markerTypeName)
        {
            return hierarchyPath + NetKeySeparator + markerTypeName;
        }

        /// <summary>
        /// Deterministic NetIds for house objects (Teknik 4.1, 4.4): the paths are sorted with an ORDINAL comparison and the
        /// i-th path in that order gets <see cref="NetId.LevelMin"/> + i. The result is parallel to the input, so the input
        /// order does not matter. Throws <see cref="ArgumentException"/> for a null/empty or duplicate path and
        /// <see cref="InvalidOperationException"/> when the ids would pass <see cref="NetId.RuntimeMin"/> - 1 (59999).
        /// </summary>
        public static int[] AssignNetIds(IList<string> hierarchyPaths)
        {
            if (hierarchyPaths == null)
            {
                throw new ArgumentNullException("hierarchyPaths");
            }

            int count = hierarchyPaths.Count;
            if (count > MaxNetIdCount)
            {
                throw new InvalidOperationException("Too many house objects for the level NetId range: " + count + " (maximum "
                    + MaxNetIdCount + ", ids " + NetId.LevelMin + ".." + (NetId.RuntimeMin - 1) + ").");
            }

            var keys = new string[count];
            var order = new int[count];
            for (int i = 0; i < count; i++)
            {
                string path = hierarchyPaths[i];
                if (string.IsNullOrEmpty(path))
                {
                    throw new ArgumentException("Hierarchy path #" + i + " is null or empty.", "hierarchyPaths");
                }
                keys[i] = path;
                order[i] = i;
            }

            Array.Sort(keys, order, StringComparer.Ordinal);

            var ids = new int[count];
            for (int rank = 0; rank < count; rank++)
            {
                if (rank > 0 && string.Equals(keys[rank], keys[rank - 1], StringComparison.Ordinal))
                {
                    throw new ArgumentException("Duplicate hierarchy path: " + keys[rank], "hierarchyPaths");
                }
                ids[order[rank]] = NetId.LevelMin + rank;
            }
            return ids;
        }
    }
}
