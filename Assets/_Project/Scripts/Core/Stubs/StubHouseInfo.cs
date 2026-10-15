using System.Collections.Generic;
using UnityEngine;

namespace CinliMahzen.Core
{
    /// <summary>Placeholder until World's HouseService: a 40x10x40 box around the origin. Settable for tests/sandboxes.</summary>
    public sealed class StubHouseInfo : IHouseInfo
    {
        private readonly List<Vector3> _jinnSpawns = new List<Vector3>
        {
            new Vector3(5f, 1.5f, 5f),
            new Vector3(-5f, 1.5f, 5f),
            new Vector3(0f, 1.5f, -5f),
        };

        public Bounds Bounds { get; set; } = new Bounds(new Vector3(0f, 5f, 0f), new Vector3(40f, 10f, 40f));
        public Vector3 HumanSpawn { get; set; } = Vector3.zero;
        public IReadOnlyList<Vector3> JinnSpawns => _jinnSpawns;
    }
}
