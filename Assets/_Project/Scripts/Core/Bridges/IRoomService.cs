using UnityEngine;

namespace CinliMahzen.Core
{
    /// <summary>Room lookup and per-room state (Teknik §4.8). Implemented by World.</summary>
    public interface IRoomService
    {
        /// <summary>Room id containing <paramref name="p"/> (RoomMarker), -1 if outside every room.</summary>
        int RoomOf(Vector3 p);
        /// <summary>Lit rooms block evil jinn possession (except lamps) and puzzle scrambling. Room -1 (outside) is always Off.</summary>
        RoomLightState LightOf(int room);
        /// <summary>True while an Old Radio plays in the room (12 s): telegraph and scramble sounds are muted there.</summary>
        bool IsNoiseSuppressed(int room);
    }
}
