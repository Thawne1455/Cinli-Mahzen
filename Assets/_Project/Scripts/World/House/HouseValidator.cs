using System;
using System.Collections.Generic;
using CinliMahzen.Core;
using UnityEngine;

namespace CinliMahzen.World
{
    /// <summary>
    /// Checks a <see cref="HouseIndex"/> against the marker contract of Teknik 8 (numbers and ids in <see cref="HouseContract"/>).
    /// Plain C#, so the same rules run in EditMode tests (graybox data) and in the editor menu (real prefab).
    ///
    /// Errors: unique room ids/keys; every marker's room id names an existing room; slots lie in rooms of their stage; at least
    /// 3 PuzzleSlots per puzzle stage, 8 ReferenceSlots, 5 KeyHideSpots in the garden, 3 CologneSpawnSpots inside the house;
    /// every indoor room has a lamp and a light switch; exactly 1 HumanSpawn (garden) and 3 JinnSpawns (house); exactly one
    /// ToolShedItemPoint with the blessed oil; the front door needs the house key; at most one cellar hatch, locked with the cellar
    /// key; every stage but the last has a gate; every room except the cellar (floor -1) is in a StageZone; no NetId collision or
    /// out-of-range id.
    /// Warnings: fewer than 3 DigSpots in the cellar, no cellar hatch, markers that lie outside the room they name, overlapping room
    /// boxes, all slots of a stage in one room.
    /// </summary>
    public static class HouseValidator
    {
        /// <summary>Boxes that overlap by less than this (metres) only touch.</summary>
        private const float OverlapTolerance = 0.001f;

        public static List<HouseIssue> Validate(HouseIndex index)
        {
            return Validate(index, null);
        }

        /// <param name="index">The house to check.</param>
        /// <param name="knownItemIds">ItemDefinition ids that exist; gate locks and tool shed items must use one of them. Null skips that check.</param>
        public static List<HouseIssue> Validate(HouseIndex index, ICollection<string> knownItemIds)
        {
            var issues = new List<HouseIssue>();
            if (index == null)
            {
                Error(issues, "house index is null");
                return issues;
            }

            CheckRooms(index, issues);
            CheckStages(index, issues);
            CheckPuzzleSlots(index, issues);
            CheckReferenceSlots(index, issues);
            CheckGates(index, knownItemIds, issues);
            CheckLights(index, issues);
            CheckKeyHideSpots(index, issues);
            CheckCologneSpots(index, issues);
            CheckDigSpots(index, issues);
            CheckSpawns(index, issues);
            CheckToolShedItems(index, knownItemIds, issues);
            CheckNetIds(index, issues);
            return issues;
        }

        public static int CountErrors(IList<HouseIssue> issues)
        {
            return Count(issues, HouseIssueSeverity.Error);
        }

        public static int CountWarnings(IList<HouseIssue> issues)
        {
            return Count(issues, HouseIssueSeverity.Warning);
        }

        // ------------------------------------------------------------------------------------------------
        // rooms and stages
        // ------------------------------------------------------------------------------------------------

        private static void CheckRooms(HouseIndex index, List<HouseIssue> issues)
        {
            if (index.Rooms.Count == 0)
            {
                Error(issues, "no RoomMarker found (the house needs one per room)");
            }

            var byId = new Dictionary<int, HouseRoomEntry>();
            var byKey = new Dictionary<string, HouseRoomEntry>(StringComparer.Ordinal);
            for (int i = 0; i < index.Rooms.Count; i++)
            {
                HouseRoomEntry room = index.Rooms[i];
                if (room.Id < 0)
                {
                    Error(issues, "room " + room + " has a negative id (-1 means outside every room)");
                }

                HouseRoomEntry other;
                if (byId.TryGetValue(room.Id, out other))
                {
                    Error(issues, "duplicate room id " + room.Id + ": '" + other.Key + "' and '" + room.Key + "'");
                }
                else
                {
                    byId.Add(room.Id, room);
                }

                if (string.IsNullOrEmpty(room.Key))
                {
                    Error(issues, "room #" + room.Id + " has an empty key");
                }
                else if (byKey.TryGetValue(room.Key, out other))
                {
                    Error(issues, "duplicate room key '" + room.Key + "' (ids " + other.Id + " and " + room.Id + ")");
                }
                else
                {
                    byKey.Add(room.Key, room);
                }

                if (room.Floor < HouseContract.MinFloor || room.Floor > HouseContract.MaxFloor)
                {
                    Error(issues, "room " + room + ": floor " + room.Floor + " is outside " + HouseContract.MinFloor + ".." + HouseContract.MaxFloor);
                }

                if (room.Boxes.Count == 0)
                {
                    Error(issues, "room " + room + " has no usable box (no enabled BoxCollider)");
                }
                for (int b = 0; b < room.Boxes.Count; b++)
                {
                    Vector3 size = room.Boxes[b].size;
                    if (size.x <= 0f || size.y <= 0f || size.z <= 0f)
                    {
                        Error(issues, "room " + room + " has a box with no volume (size " + size + ")");
                    }
                }
            }

            for (int a = 0; a < index.Rooms.Count; a++)
            {
                for (int b = a + 1; b < index.Rooms.Count; b++)
                {
                    if (RoomsOverlap(index.Rooms[a], index.Rooms[b]))
                    {
                        Warn(issues, "rooms '" + index.Rooms[a].Key + "' and '" + index.Rooms[b].Key
                            + "' overlap; RoomOf picks the smaller box inside the overlap");
                    }
                }
            }
        }

        private static bool RoomsOverlap(HouseRoomEntry a, HouseRoomEntry b)
        {
            for (int i = 0; i < a.Boxes.Count; i++)
            {
                for (int j = 0; j < b.Boxes.Count; j++)
                {
                    if (BoxesOverlap(a.Boxes[i], b.Boxes[j]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool BoxesOverlap(Bounds a, Bounds b)
        {
            Vector3 aMin = a.min;
            Vector3 aMax = a.max;
            Vector3 bMin = b.min;
            Vector3 bMax = b.max;
            return aMin.x < bMax.x - OverlapTolerance && bMin.x < aMax.x - OverlapTolerance
                && aMin.y < bMax.y - OverlapTolerance && bMin.y < aMax.y - OverlapTolerance
                && aMin.z < bMax.z - OverlapTolerance && bMin.z < aMax.z - OverlapTolerance;
        }

        private static void CheckStages(HouseIndex index, List<HouseIssue> issues)
        {
            if (index.Stages.Count == 0)
            {
                Error(issues, "no StageZone found (stage 0 = garden, 1.. = floors)");
            }

            var seen = new HashSet<int>();
            int highest = -1;
            for (int i = 0; i < index.Stages.Count; i++)
            {
                int stage = index.Stages[i].Index;
                if (stage < 0)
                {
                    Error(issues, "StageZone has a negative index " + stage);
                }
                else if (!seen.Add(stage))
                {
                    Error(issues, "duplicate StageZone index " + stage);
                }
                highest = Math.Max(highest, stage);
            }
            for (int stage = 0; stage <= highest; stage++)
            {
                if (!seen.Contains(stage))
                {
                    Error(issues, "StageZone indices must start at 0 without gaps: stage " + stage + " is missing");
                }
            }

            var stageOfRoom = new Dictionary<int, int>();
            for (int i = 0; i < index.Stages.Count; i++)
            {
                HouseStageEntry stage = index.Stages[i];
                for (int r = 0; r < stage.RoomIds.Count; r++)
                {
                    int roomId = stage.RoomIds[r];
                    if (index.FindRoom(roomId) == null)
                    {
                        Error(issues, "StageZone " + stage.Index + " lists unknown room id " + roomId);
                        continue;
                    }
                    int other;
                    if (stageOfRoom.TryGetValue(roomId, out other))
                    {
                        Error(issues, "room " + RoomLabel(index, roomId) + " is in more than one StageZone (" + other + " and " + stage.Index + ")");
                    }
                    else
                    {
                        stageOfRoom.Add(roomId, stage.Index);
                    }
                }
            }
            for (int i = 0; i < index.Rooms.Count; i++)
            {
                // The cellar belongs to no stage: it opens through the hatch after the last active puzzle stage.
                if (!stageOfRoom.ContainsKey(index.Rooms[i].Id) && index.Rooms[i].Floor != HouseContract.CellarFloor)
                {
                    Error(issues, "room " + index.Rooms[i] + " is not in any StageZone");
                }
            }
        }

        // ------------------------------------------------------------------------------------------------
        // slots and gates
        // ------------------------------------------------------------------------------------------------

        private static void CheckPuzzleSlots(HouseIndex index, List<HouseIssue> issues)
        {
            for (int s = 0; s < index.Stages.Count; s++)
            {
                int stage = index.Stages[s].Index;
                if (stage < HouseContract.FirstPuzzleStage)
                {
                    continue;
                }

                int count = 0;
                var rooms = new HashSet<int>();
                for (int i = 0; i < index.PuzzleSlots.Count; i++)
                {
                    if (index.PuzzleSlots[i].StageIndex == stage)
                    {
                        count++;
                        rooms.Add(index.PuzzleSlots[i].RoomId);
                    }
                }
                if (count < HouseContract.MinPuzzleSlotsPerStage)
                {
                    Error(issues, "stage " + stage + " has " + count + " PuzzleSlots (minimum " + HouseContract.MinPuzzleSlotsPerStage + ")");
                }
                else if (rooms.Count < 2)
                {
                    Warn(issues, "all PuzzleSlots of stage " + stage + " are in one room; the two parallel puzzles should be in different rooms");
                }
            }

            for (int i = 0; i < index.PuzzleSlots.Count; i++)
            {
                HousePuzzleSlotEntry slot = index.PuzzleSlots[i];
                HouseRoomEntry room = RequireRoom(index, issues, "PuzzleSlot " + slot.Path, slot.RoomId);
                HouseStageEntry stage = index.FindStage(slot.StageIndex);
                if (stage == null)
                {
                    Error(issues, "PuzzleSlot " + slot.Path + " belongs to stage " + slot.StageIndex + ", which has no StageZone");
                }
                else if (room != null && !stage.Contains(room.Id))
                {
                    Error(issues, "PuzzleSlot " + slot.Path + ": room '" + room.Key + "' is not a room of stage " + slot.StageIndex);
                }
                if (slot.AllowedTypes.Count == 0)
                {
                    Error(issues, "PuzzleSlot " + slot.Path + " has no allowed puzzle types");
                }
                CheckInsideRoom(index, issues, "PuzzleSlot " + slot.Path, slot.Position, room);
            }
        }

        private static void CheckReferenceSlots(HouseIndex index, List<HouseIssue> issues)
        {
            if (index.ReferenceSlots.Count < HouseContract.MinReferenceSlots)
            {
                Error(issues, "the house has " + index.ReferenceSlots.Count + " ReferenceSlots (minimum " + HouseContract.MinReferenceSlots + ")");
            }
            for (int i = 0; i < index.ReferenceSlots.Count; i++)
            {
                HouseReferenceSlotEntry slot = index.ReferenceSlots[i];
                HouseRoomEntry room = RequireRoom(index, issues, "ReferenceSlot " + slot.Path, slot.RoomId);
                if (slot.AllowedTypes.Count == 0)
                {
                    Error(issues, "ReferenceSlot " + slot.Path + " has no allowed puzzle types");
                }
                CheckInsideRoom(index, issues, "ReferenceSlot " + slot.Path, slot.Position, room);
            }
        }

        private static void CheckGates(HouseIndex index, ICollection<string> knownItemIds, List<HouseIssue> issues)
        {
            var gateCount = new Dictionary<int, int>();
            int hatches = 0;
            for (int i = 0; i < index.Gates.Count; i++)
            {
                HouseGateEntry gate = index.Gates[i];
                if (gate.HasLock && knownItemIds != null && !knownItemIds.Contains(gate.RequiredItemId))
                {
                    Error(issues, "GateLock on " + gate.Path + " requires unknown item '" + gate.RequiredItemId + "'");
                }
                if (gate.IsCellarHatch)
                {
                    // TODO 4.1: its stage follows StagesActive, so it is not counted per stage.
                    hatches++;
                    if (hatches == 2)
                    {
                        Error(issues, "the house has more than one cellar hatch");
                    }
                    if (gate.RequiredItemId != HouseContract.CellarKeyItemId)
                    {
                        Error(issues, "the cellar hatch " + gate.Path + " must require '" + HouseContract.CellarKeyItemId + "' but requires '"
                            + gate.RequiredItemId + "'");
                    }
                    continue;
                }

                if (index.FindStage(gate.StageIndex) == null)
                {
                    Error(issues, "StageGate " + gate.Path + " completes stage " + gate.StageIndex + ", which has no StageZone");
                }

                int count;
                gateCount.TryGetValue(gate.StageIndex, out count);
                gateCount[gate.StageIndex] = count + 1;
                if (count == 1)
                {
                    Error(issues, "stage " + gate.StageIndex + " has more than one StageGate");
                }

                if (gate.StageIndex == HouseContract.GardenStage && gate.RequiredItemId != HouseContract.FrontDoorItemId)
                {
                    Error(issues, "the stage 0 gate (front door) " + gate.Path + " must require '" + HouseContract.FrontDoorItemId
                        + "' but requires '" + gate.RequiredItemId + "'");
                }
            }

            int highest = -1;
            for (int i = 0; i < index.Stages.Count; i++)
            {
                highest = Math.Max(highest, index.Stages[i].Index);
            }
            for (int i = 0; i < index.Stages.Count; i++)
            {
                int stage = index.Stages[i].Index;
                if (gateCount.ContainsKey(stage))
                {
                    continue;
                }
                // The last stage needs no gate of its own: the cellar hatch (or the cellar itself) follows it.
                if (stage != highest)
                {
                    Error(issues, "stage " + stage + " has no StageGate");
                }
            }
            if (hatches == 0)
            {
                Warn(issues, "the house has no cellar hatch: the cellar opens right after the last puzzle stage");
            }
        }

        // ------------------------------------------------------------------------------------------------
        // spots
        // ------------------------------------------------------------------------------------------------

        private static void CheckLights(HouseIndex index, List<HouseIssue> issues)
        {
            for (int i = 0; i < index.Rooms.Count; i++)
            {
                HouseRoomEntry room = index.Rooms[i];
                if (room.IsOutdoor)
                {
                    continue;
                }
                if (CountInRoom(index.Lamps, room.Id) == 0)
                {
                    Error(issues, "room " + room + " has no LampMarker");
                }
                if (CountInRoom(index.LightSwitches, room.Id) == 0)
                {
                    Error(issues, "room " + room + " has no LightSwitch");
                }
            }

            CheckRoomSpots(index, issues, index.Lamps, "LampMarker");
            CheckRoomSpots(index, issues, index.LightSwitches, "LightSwitch");
        }

        private static void CheckRoomSpots(HouseIndex index, List<HouseIssue> issues, IReadOnlyList<HouseSpotEntry> spots, string label)
        {
            for (int i = 0; i < spots.Count; i++)
            {
                HouseRoomEntry room = RequireRoom(index, issues, label + " " + spots[i].Path, spots[i].RoomId);
                CheckInsideRoom(index, issues, label + " " + spots[i].Path, spots[i].Position, room);
            }
        }

        private static void CheckKeyHideSpots(HouseIndex index, List<HouseIssue> issues)
        {
            int inGarden = 0;
            for (int i = 0; i < index.KeyHideSpots.Count; i++)
            {
                HouseSpotEntry spot = index.KeyHideSpots[i];
                int room = index.ResolveRoom(spot);
                if (room != HouseIndex.NoRoom && index.StageOfRoom(room) == HouseContract.GardenStage)
                {
                    inGarden++;
                }
                else
                {
                    Warn(issues, "KeyHideSpot " + spot.Path + " is not in a garden room; it does not count");
                }
            }
            if (inGarden < HouseContract.MinKeyHideSpots)
            {
                Error(issues, "the garden has " + inGarden + " KeyHideSpots (minimum " + HouseContract.MinKeyHideSpots + ")");
            }
        }

        private static void CheckCologneSpots(HouseIndex index, List<HouseIssue> issues)
        {
            int inHouse = 0;
            for (int i = 0; i < index.CologneSpots.Count; i++)
            {
                HouseSpotEntry spot = index.CologneSpots[i];
                HouseRoomEntry room = RequireRoom(index, issues, "CologneSpawnSpot " + spot.Path, spot.RoomId);
                if (room == null)
                {
                    continue;
                }
                if (index.StageOfRoom(room.Id) <= HouseContract.GardenStage)
                {
                    Error(issues, "CologneSpawnSpot " + spot.Path + " is not inside the house (room '" + room.Key + "' is not in a house stage)");
                    continue;
                }
                inHouse++;
                CheckInsideRoom(index, issues, "CologneSpawnSpot " + spot.Path, spot.Position, room);
            }
            if (inHouse < HouseContract.MinCologneSpawnSpots)
            {
                Error(issues, "the house has " + inHouse + " CologneSpawnSpots (minimum " + HouseContract.MinCologneSpawnSpots + ")");
            }
        }

        private static void CheckDigSpots(HouseIndex index, List<HouseIssue> issues)
        {
            int inCellar = 0;
            for (int i = 0; i < index.DigSpots.Count; i++)
            {
                HouseSpotEntry spot = index.DigSpots[i];
                HouseRoomEntry room = index.FindRoom(index.ResolveRoom(spot));
                if (room != null && room.Floor == HouseContract.CellarFloor)
                {
                    inCellar++;
                }
                else
                {
                    Error(issues, "DigSpot " + spot.Path + " is not inside a cellar room");
                }
            }
            if (inCellar < HouseContract.MinDigSpots)
            {
                Warn(issues, "the cellar has " + inCellar + " DigSpots (minimum " + HouseContract.MinDigSpots
                    + "); the cellar is added with TODO 3.7");
            }
        }

        private static void CheckSpawns(HouseIndex index, List<HouseIssue> issues)
        {
            if (index.HumanSpawns.Count != HouseContract.HumanSpawnCount)
            {
                Error(issues, "expected exactly " + HouseContract.HumanSpawnCount + " HumanSpawnMarker, found " + index.HumanSpawns.Count);
            }
            for (int i = 0; i < index.HumanSpawns.Count; i++)
            {
                int room = index.RoomOf(index.HumanSpawns[i]);
                if (room == HouseIndex.NoRoom || index.StageOfRoom(room) != HouseContract.GardenStage)
                {
                    Error(issues, "HumanSpawnMarker at " + index.HumanSpawns[i].ToString("0.0") + " is not in the garden");
                }
            }

            if (index.JinnSpawns.Count != HouseContract.JinnSpawnCount)
            {
                Error(issues, "expected exactly " + HouseContract.JinnSpawnCount + " JinnSpawnMarkers, found " + index.JinnSpawns.Count);
            }
            var rooms = new HashSet<int>();
            int inHouse = 0;
            for (int i = 0; i < index.JinnSpawns.Count; i++)
            {
                int room = index.RoomOf(index.JinnSpawns[i]);
                if (room == HouseIndex.NoRoom || index.StageOfRoom(room) <= HouseContract.GardenStage)
                {
                    Error(issues, "JinnSpawnMarker at " + index.JinnSpawns[i].ToString("0.0") + " is not inside the house");
                    continue;
                }
                inHouse++;
                rooms.Add(room);
            }
            if (rooms.Count < inHouse)
            {
                Warn(issues, "JinnSpawnMarkers share a room; they should start in different rooms");
            }
        }

        private static void CheckToolShedItems(HouseIndex index, ICollection<string> knownItemIds, List<HouseIssue> issues)
        {
            int oilPoints = 0;
            for (int i = 0; i < index.ToolShedItems.Count; i++)
            {
                HouseSpotEntry point = index.ToolShedItems[i];
                if (point.ItemId.Length == 0)
                {
                    Error(issues, "ToolShedItemPoint " + point.Path + " has no itemId");
                }
                else if (knownItemIds != null && !knownItemIds.Contains(point.ItemId))
                {
                    Error(issues, "ToolShedItemPoint " + point.Path + " uses unknown item '" + point.ItemId + "'");
                }
                if (point.ItemId == HouseContract.BlessedOilItemId)
                {
                    oilPoints++;
                }

                int room = index.ResolveRoom(point);
                if (room == HouseIndex.NoRoom || index.StageOfRoom(room) != HouseContract.GardenStage)
                {
                    Error(issues, "ToolShedItemPoint " + point.Path + " is not in a stage 0 room (the tool shed)");
                }
            }
            if (oilPoints != HouseContract.BlessedOilPointCount)
            {
                Error(issues, "expected exactly " + HouseContract.BlessedOilPointCount + " ToolShedItemPoint with '"
                    + HouseContract.BlessedOilItemId + "', found " + oilPoints);
            }
        }

        // ------------------------------------------------------------------------------------------------
        // NetIds
        // ------------------------------------------------------------------------------------------------

        private static void CheckNetIds(HouseIndex index, List<HouseIssue> issues)
        {
            var entries = new List<HouseNetEntry>();
            index.CollectNetEntries(entries);
            var owners = new Dictionary<int, HouseNetEntry>();
            for (int i = 0; i < entries.Count; i++)
            {
                HouseNetEntry entry = entries[i];
                int id = entry.NetId.Value;
                if (id < NetId.LevelMin || id >= NetId.RuntimeMin)
                {
                    Error(issues, entry.Path + " has NetId " + id + " outside the level range " + NetId.LevelMin + ".." + (NetId.RuntimeMin - 1));
                }

                HouseNetEntry other;
                if (owners.TryGetValue(id, out other))
                {
                    Error(issues, "NetId " + id + " is used by both " + other.Path + " and " + entry.Path);
                }
                else
                {
                    owners.Add(id, entry);
                }
            }
        }

        // ------------------------------------------------------------------------------------------------
        // helpers
        // ------------------------------------------------------------------------------------------------

        /// <summary>The room with this id, or null after reporting an error ("unknown room").</summary>
        private static HouseRoomEntry RequireRoom(HouseIndex index, List<HouseIssue> issues, string owner, int roomId)
        {
            HouseRoomEntry room = index.FindRoom(roomId);
            if (room == null)
            {
                Error(issues, owner + " names unknown room id " + roomId);
            }
            return room;
        }

        private static void CheckInsideRoom(HouseIndex index, List<HouseIssue> issues, string owner, Vector3 position, HouseRoomEntry room)
        {
            if (room != null && index.RoomOf(position) != room.Id)
            {
                Warn(issues, owner + " at " + position.ToString("0.0") + " is not inside its room '" + room.Key + "'");
            }
        }

        private static int CountInRoom(IReadOnlyList<HouseSpotEntry> spots, int roomId)
        {
            int count = 0;
            for (int i = 0; i < spots.Count; i++)
            {
                if (spots[i].RoomId == roomId)
                {
                    count++;
                }
            }
            return count;
        }

        private static string RoomLabel(HouseIndex index, int roomId)
        {
            HouseRoomEntry room = index.FindRoom(roomId);
            return room != null ? room.ToString() : "#" + roomId;
        }

        private static int Count(IList<HouseIssue> issues, HouseIssueSeverity severity)
        {
            int count = 0;
            for (int i = 0; i < issues.Count; i++)
            {
                if (issues[i].Severity == severity)
                {
                    count++;
                }
            }
            return count;
        }

        private static void Error(List<HouseIssue> issues, string message)
        {
            issues.Add(new HouseIssue(HouseIssueSeverity.Error, message));
        }

        private static void Warn(List<HouseIssue> issues, string message)
        {
            issues.Add(new HouseIssue(HouseIssueSeverity.Warning, message));
        }
    }
}
