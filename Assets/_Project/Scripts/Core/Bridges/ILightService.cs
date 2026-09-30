using UnityEngine;

namespace CinliMahzen.Core
{
    /// <summary>Implemented by C (World).</summary>
    public interface ILightService
    {
        int RoomOf(Vector3 worldPos);
        /// <summary>Authority only.</summary>
        void SetRoomLightsAuthority(int roomId, bool on, float duration);
    }
}
