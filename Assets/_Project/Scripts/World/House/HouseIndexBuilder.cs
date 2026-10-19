using System;
using System.Collections.Generic;
using CinliMahzen.Core;
using UnityEngine;

namespace CinliMahzen.World
{
    /// <summary>
    /// Collects index entries and turns them into a <see cref="HouseIndex"/>. Both <see cref="HouseService"/> (from marker
    /// components) and the graybox data (<c>GrayboxHouseIndexFactory</c>, tests) go through it, so NetId assignment has exactly
    /// one implementation. Single use: build once, then drop the builder.
    ///
    /// <see cref="TryBuild"/> gives every NetId-carrying entry that has no id yet the id from
    /// <see cref="HouseIndex.AssignNetIds"/> (sorted hierarchy paths). Entries created with an explicit NetId keep it
    /// (tests use that to provoke collisions; the validator reports them).
    /// </summary>
    public sealed class HouseIndexBuilder
    {
        private readonly List<HouseRoomEntry> _rooms = new List<HouseRoomEntry>();
        private readonly List<HouseStageEntry> _stages = new List<HouseStageEntry>();
        private readonly List<HousePuzzleSlotEntry> _puzzleSlots = new List<HousePuzzleSlotEntry>();
        private readonly List<HouseReferenceSlotEntry> _referenceSlots = new List<HouseReferenceSlotEntry>();
        private readonly List<HouseGateEntry> _gates = new List<HouseGateEntry>();
        private readonly List<HouseSpotEntry> _lamps = new List<HouseSpotEntry>();
        private readonly List<HouseSpotEntry> _lightSwitches = new List<HouseSpotEntry>();
        private readonly List<HouseSpotEntry> _keyHideSpots = new List<HouseSpotEntry>();
        private readonly List<HouseSpotEntry> _cologneSpots = new List<HouseSpotEntry>();
        private readonly List<HouseSpotEntry> _digSpots = new List<HouseSpotEntry>();
        private readonly List<HouseSpotEntry> _toolShedItems = new List<HouseSpotEntry>();
        private readonly List<KeyValuePair<string, Vector3>> _humanSpawns = new List<KeyValuePair<string, Vector3>>();
        private readonly List<KeyValuePair<string, Vector3>> _jinnSpawns = new List<KeyValuePair<string, Vector3>>();

        public HouseIndexBuilder Add(HouseRoomEntry room)
        {
            _rooms.Add(room);
            return this;
        }

        public HouseIndexBuilder Add(HouseStageEntry stage)
        {
            _stages.Add(stage);
            return this;
        }

        public HouseIndexBuilder Add(HousePuzzleSlotEntry slot)
        {
            _puzzleSlots.Add(slot);
            return this;
        }

        public HouseIndexBuilder Add(HouseReferenceSlotEntry slot)
        {
            _referenceSlots.Add(slot);
            return this;
        }

        public HouseIndexBuilder Add(HouseGateEntry gate)
        {
            _gates.Add(gate);
            return this;
        }

        public HouseIndexBuilder Add(HouseSpotEntry spot)
        {
            SpotList(spot.Kind).Add(spot);
            return this;
        }

        /// <summary>Spawn points have no NetId; the path only fixes their order.</summary>
        public HouseIndexBuilder AddHumanSpawn(string path, Vector3 position)
        {
            _humanSpawns.Add(new KeyValuePair<string, Vector3>(path ?? string.Empty, position));
            return this;
        }

        public HouseIndexBuilder AddJinnSpawn(string path, Vector3 position)
        {
            _jinnSpawns.Add(new KeyValuePair<string, Vector3>(path ?? string.Empty, position));
            return this;
        }

        /// <summary>Like <see cref="TryBuild"/> but throws <see cref="InvalidOperationException"/> on failure.</summary>
        public HouseIndex Build()
        {
            HouseIndex index;
            string error;
            if (!TryBuild(out index, out error))
            {
                throw new InvalidOperationException(error);
            }
            return index;
        }

        /// <summary>
        /// Assigns the NetIds and creates the index. Fails (false + message) on a duplicate hierarchy path or when there are
        /// more than <see cref="HouseIndex.MaxNetIdCount"/> NetId-carrying entries.
        /// </summary>
        public bool TryBuild(out HouseIndex index, out string error)
        {
            index = null;
            error = null;

            var all = new List<HouseNetEntry>();
            for (int i = 0; i < _puzzleSlots.Count; i++)
            {
                all.Add(_puzzleSlots[i]);
            }
            for (int i = 0; i < _referenceSlots.Count; i++)
            {
                all.Add(_referenceSlots[i]);
            }
            for (int i = 0; i < _gates.Count; i++)
            {
                all.Add(_gates[i]);
            }
            AddSpots(all, _lamps);
            AddSpots(all, _lightSwitches);
            AddSpots(all, _keyHideSpots);
            AddSpots(all, _cologneSpots);
            AddSpots(all, _digSpots);
            AddSpots(all, _toolShedItems);

            var pending = new List<HouseNetEntry>();
            var paths = new List<string>();
            for (int i = 0; i < all.Count; i++)
            {
                if (!all[i].NetId.IsValid)
                {
                    pending.Add(all[i]);
                    paths.Add(all[i].Path);
                }
            }

            int[] ids;
            try
            {
                ids = HouseIndex.AssignNetIds(paths);
            }
            catch (ArgumentException e)
            {
                error = e.Message;
                return false;
            }
            catch (InvalidOperationException e)
            {
                error = e.Message;
                return false;
            }

            for (int i = 0; i < pending.Count; i++)
            {
                pending[i].AssignNetId(new NetId(ids[i]));
            }

            var rooms = new List<HouseRoomEntry>(_rooms);
            rooms.Sort(CompareRooms);
            var stages = new List<HouseStageEntry>(_stages);
            stages.Sort(CompareStages);

            index = new HouseIndex(
                rooms,
                stages,
                SortedCopy(_puzzleSlots),
                SortedCopy(_referenceSlots),
                SortedCopy(_gates),
                SortedCopy(_lamps),
                SortedCopy(_lightSwitches),
                SortedCopy(_keyHideSpots),
                SortedCopy(_cologneSpots),
                SortedCopy(_digSpots),
                SortedCopy(_toolShedItems),
                SortedSpawns(_humanSpawns),
                SortedSpawns(_jinnSpawns));
            return true;
        }

        private List<HouseSpotEntry> SpotList(HouseSpotKind kind)
        {
            switch (kind)
            {
                case HouseSpotKind.Lamp: return _lamps;
                case HouseSpotKind.LightSwitch: return _lightSwitches;
                case HouseSpotKind.KeyHide: return _keyHideSpots;
                case HouseSpotKind.Cologne: return _cologneSpots;
                case HouseSpotKind.Dig: return _digSpots;
                default: return _toolShedItems;
            }
        }

        private static void AddSpots(List<HouseNetEntry> all, List<HouseSpotEntry> spots)
        {
            for (int i = 0; i < spots.Count; i++)
            {
                all.Add(spots[i]);
            }
        }

        private static List<T> SortedCopy<T>(List<T> source) where T : HouseNetEntry
        {
            var copy = new List<T>(source);
            copy.Sort(CompareNetEntries);
            return copy;
        }

        private static int CompareNetEntries(HouseNetEntry a, HouseNetEntry b)
        {
            int byId = a.NetId.Value.CompareTo(b.NetId.Value);
            return byId != 0 ? byId : string.CompareOrdinal(a.Path, b.Path);
        }

        private static int CompareRooms(HouseRoomEntry a, HouseRoomEntry b)
        {
            return a.Id.CompareTo(b.Id);
        }

        private static int CompareStages(HouseStageEntry a, HouseStageEntry b)
        {
            return a.Index.CompareTo(b.Index);
        }

        private static List<Vector3> SortedSpawns(List<KeyValuePair<string, Vector3>> spawns)
        {
            var copy = new List<KeyValuePair<string, Vector3>>(spawns);
            copy.Sort(CompareSpawns);
            var positions = new List<Vector3>(copy.Count);
            for (int i = 0; i < copy.Count; i++)
            {
                positions.Add(copy[i].Value);
            }
            return positions;
        }

        private static int CompareSpawns(KeyValuePair<string, Vector3> a, KeyValuePair<string, Vector3> b)
        {
            return string.CompareOrdinal(a.Key, b.Key);
        }
    }
}
