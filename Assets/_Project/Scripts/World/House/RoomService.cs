using System;
using System.Collections.Generic;
using CinliMahzen.Core;
using UnityEngine;

namespace CinliMahzen.World
{
    /// <summary>
    /// <see cref="IRoomService"/> over a <see cref="HouseIndex"/> (Teknik 4.8): room lookup from the room boxes, per-room light
    /// state and per-room "noise suppressed until" time. Plain C#, no Unity objects.
    ///
    /// The state set here is the already-authorised result: the light system (TODO 3.4) and the radio call
    /// <see cref="SetRoomLight"/> / <see cref="SuppressNoiseUntil"/> after the authority decided and broadcast it
    /// (CLAUDE.md rule 1). Rooms start dark and unmuted. Unknown rooms (including -1 = outside) are always Off / not suppressed.
    /// </summary>
    public sealed class RoomService : IRoomService
    {
        private readonly HouseIndex _index;
        private readonly Func<double> _now;
        private readonly Dictionary<int, int> _slotOfRoom = new Dictionary<int, int>();
        private readonly RoomLightState[] _lights;
        private readonly double[] _noiseUntil;

        /// <param name="index">The house to serve.</param>
        /// <param name="now">Time source, normally GameServices.Net.Time (Teknik rule 5).</param>
        public RoomService(HouseIndex index, Func<double> now)
        {
            if (index == null)
            {
                throw new ArgumentNullException("index");
            }
            if (now == null)
            {
                throw new ArgumentNullException("now");
            }

            _index = index;
            _now = now;
            _lights = new RoomLightState[index.Rooms.Count];
            _noiseUntil = new double[index.Rooms.Count];
            for (int i = 0; i < index.Rooms.Count; i++)
            {
                // A duplicate room id (a validator error) keeps the first room's slot.
                if (!_slotOfRoom.ContainsKey(index.Rooms[i].Id))
                {
                    _slotOfRoom.Add(index.Rooms[i].Id, i);
                }
            }
        }

        public int RoomOf(Vector3 p)
        {
            return _index.RoomOf(p);
        }

        public RoomLightState LightOf(int room)
        {
            int slot;
            return _slotOfRoom.TryGetValue(room, out slot) ? _lights[slot] : RoomLightState.Off;
        }

        public bool IsNoiseSuppressed(int room)
        {
            int slot;
            return _slotOfRoom.TryGetValue(room, out slot) && _now() < _noiseUntil[slot];
        }

        /// <summary>Sets the light state of a room. Returns false (nothing changed) for an unknown room.</summary>
        public bool SetRoomLight(int room, RoomLightState state)
        {
            int slot;
            if (!_slotOfRoom.TryGetValue(room, out slot))
            {
                return false;
            }
            _lights[slot] = state;
            return true;
        }

        /// <summary>
        /// Mutes telegraph and scramble sounds in the room until the given time (same clock as the time source). Replaces the
        /// previous value; pass 0 (or any past time) to end the effect early. Returns false for an unknown room.
        /// </summary>
        public bool SuppressNoiseUntil(int room, double until)
        {
            int slot;
            if (!_slotOfRoom.TryGetValue(room, out slot))
            {
                return false;
            }
            _noiseUntil[slot] = until;
            return true;
        }

        /// <summary>New round: every room dark again, no noise suppression.</summary>
        public void ResetState()
        {
            for (int i = 0; i < _lights.Length; i++)
            {
                _lights[i] = RoomLightState.Off;
                _noiseUntil[i] = 0d;
            }
        }
    }
}
