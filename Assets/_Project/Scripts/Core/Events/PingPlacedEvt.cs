using UnityEngine;

namespace CinliMahzen.Core.Events
{
    /// <summary>Published by Jinn on every machine when a good jinn's ping is placed (TODO 4.3 stats: "Navigasyon").</summary>
    public readonly struct PingPlacedEvt
    {
        public readonly PlayerId Player;
        public readonly Vector3 Point;

        public PingPlacedEvt(PlayerId player, Vector3 point)
        {
            Player = player;
            Point = point;
        }
    }
}
