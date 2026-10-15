using System.Collections.Generic;
using UnityEngine;

namespace CinliMahzen.Core
{
    /// <summary>Placeholder until World's room service: the whole world is room 0, dark and not muted. Per-room state is settable for tests/sandboxes.</summary>
    public sealed class StubRoomService : IRoomService
    {
        private readonly Dictionary<int, RoomLightState> _lights = new Dictionary<int, RoomLightState>();
        private readonly HashSet<int> _suppressed = new HashSet<int>();

        public int RoomOf(Vector3 p) => 0;

        public RoomLightState LightOf(int room)
        {
            return _lights.TryGetValue(room, out RoomLightState s) ? s : RoomLightState.Off;
        }

        public bool IsNoiseSuppressed(int room) => _suppressed.Contains(room);

        public void SetLight(int room, RoomLightState state)
        {
            _lights[room] = state;
        }

        public void SetNoiseSuppressed(int room, bool suppressed)
        {
            if (suppressed)
                _suppressed.Add(room);
            else
                _suppressed.Remove(room);
        }
    }
}
