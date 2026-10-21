using UnityEngine;

namespace CinliMahzen.Core.Events
{
    /// <summary>
    /// Published by Objectives on every machine when a round is set up: the round plan put the Lemon Cologne on this CologneSpawnSpot (same seed →
    /// same spot everywhere, so no network message). The scene's cologne possessable moves there and makes it its round-start pose.
    /// </summary>
    public readonly struct ColognePlacedEvt
    {
        public readonly Vector3 Position;
        public readonly float Yaw;

        public ColognePlacedEvt(Vector3 position, float yaw)
        {
            Position = position;
            Yaw = yaw;
        }
    }
}
