using UnityEngine;

namespace CinliMahzen.Core
{
    /// <summary>Placeholder until C1.5 LightService: one room, light commands are ignored.</summary>
    public sealed class StubLightService : ILightService
    {
        public int RoomOf(Vector3 worldPos) => 0;

        public void SetRoomLightsAuthority(int roomId, bool on, float duration)
        {
        }
    }
}
