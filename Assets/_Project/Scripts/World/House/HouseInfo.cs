using System.Collections.Generic;
using CinliMahzen.Core;
using UnityEngine;

namespace CinliMahzen.World
{
    /// <summary>
    /// <see cref="IHouseInfo"/> over a <see cref="HouseIndex"/> (Teknik 4.8): the spirit bounds are the union of all room boxes
    /// plus a padding on every side; the spawns come from the spawn markers. Without a HumanSpawnMarker the human spawn is the origin.
    /// </summary>
    public sealed class HouseInfo : IHouseInfo
    {
        private readonly IReadOnlyList<Vector3> _jinnSpawns;

        public Bounds Bounds { get; }
        public Vector3 HumanSpawn { get; }

        public IReadOnlyList<Vector3> JinnSpawns
        {
            get { return _jinnSpawns; }
        }

        /// <param name="padding">Metres added to every side of the room union (HouseService.boundsPadding).</param>
        public HouseInfo(HouseIndex index, Vector3 padding)
        {
            Bounds bounds = index.RoomBounds;
            bounds.Expand(padding * 2f);
            Bounds = bounds;
            HumanSpawn = index.HumanSpawns.Count > 0 ? index.HumanSpawns[0] : Vector3.zero;
            _jinnSpawns = index.JinnSpawns;
        }
    }
}
