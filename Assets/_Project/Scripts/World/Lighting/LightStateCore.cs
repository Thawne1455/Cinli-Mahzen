using System.Collections.Generic;
using CinliMahzen.Core;

namespace CinliMahzen.World
{
    /// <summary>
    /// Lamps and the room light they make (Oyun §7, TODO 3.4), plain C#. A room is lit while at least one of its lamps is On; a Broken lamp
    /// gives no light and cannot be switched on (only the good jinn's repair, TODO 3.5, makes it Off again). Everything starts Off.
    /// </summary>
    public sealed class LightStateCore
    {
        private readonly List<NetId> _ids = new List<NetId>();
        private readonly List<int> _rooms = new List<int>();
        private readonly List<LampState> _states = new List<LampState>();

        public int LampCount => _ids.Count;

        /// <summary>Adds a lamp of <paramref name="room"/> (Off). A repeated id is ignored.</summary>
        public void AddLamp(NetId lamp, int room)
        {
            if (IndexOf(lamp) >= 0)
                return;
            _ids.Add(lamp);
            _rooms.Add(room);
            _states.Add(LampState.Off);
        }

        public bool IsLamp(NetId lamp)
        {
            return IndexOf(lamp) >= 0;
        }

        public LampState StateOf(NetId lamp)
        {
            int i = IndexOf(lamp);
            return i >= 0 ? _states[i] : LampState.Off;
        }

        /// <summary>Room of a lamp, or -1.</summary>
        public int RoomOfLamp(NetId lamp)
        {
            int i = IndexOf(lamp);
            return i >= 0 ? _rooms[i] : -1;
        }

        public NetId LampAt(int index) => _ids[index];

        public RoomLightState LightOf(int room)
        {
            for (int i = 0; i < _ids.Count; i++)
            {
                if (_rooms[i] == room && _states[i] == LampState.On)
                    return RoomLightState.On;
            }
            return RoomLightState.Off;
        }

        /// <summary>Sets a lamp. Returns false for an unknown lamp or no change.</summary>
        public bool Set(NetId lamp, LampState state)
        {
            int i = IndexOf(lamp);
            if (i < 0 || _states[i] == state)
                return false;
            _states[i] = state;
            return true;
        }

        /// <summary>
        /// What the room's wall switch does: if any lamp of the room is On, every On lamp goes Off; otherwise every Off lamp goes On (Broken
        /// lamps stay broken). Appends the lamps that change and their new state; nothing is applied (the authority broadcasts first).
        /// </summary>
        public void PlanToggle(int room, List<NetId> lamps, List<LampState> states)
        {
            bool anyOn = LightOf(room) == RoomLightState.On;
            for (int i = 0; i < _ids.Count; i++)
            {
                if (_rooms[i] != room)
                    continue;
                if (anyOn && _states[i] == LampState.On)
                {
                    lamps.Add(_ids[i]);
                    states.Add(LampState.Off);
                }
                else if (!anyOn && _states[i] == LampState.Off)
                {
                    lamps.Add(_ids[i]);
                    states.Add(LampState.On);
                }
            }
        }

        /// <summary>Every lamp Off (new round; broken lamps are new again).</summary>
        public void Reset()
        {
            for (int i = 0; i < _states.Count; i++)
                _states[i] = LampState.Off;
        }

        private int IndexOf(NetId lamp)
        {
            for (int i = 0; i < _ids.Count; i++)
            {
                if (_ids[i] == lamp)
                    return i;
            }
            return -1;
        }
    }
}
