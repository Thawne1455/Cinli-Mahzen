using CinliMahzen.Core;
using UnityEngine;

namespace CinliMahzen.World
{
    /// <summary>Plain-data copy of one <see cref="StageGate"/> (and its optional <see cref="GateLock"/>).</summary>
    public sealed class HouseGateEntry : HouseNetEntry
    {
        /// <summary>The stage this gate completes.</summary>
        public int StageIndex { get; }

        /// <summary>ItemDefinition.Id the gate needs; empty when it has no lock.</summary>
        public string RequiredItemId { get; }

        public Vector3 Position { get; }

        public bool HasLock
        {
            get { return RequiredItemId.Length > 0; }
        }

        public HouseGateEntry(string path, int stageIndex, string requiredItemId, Vector3 position, NetId netId = default)
            : base(path, netId)
        {
            StageIndex = stageIndex;
            RequiredItemId = requiredItemId ?? string.Empty;
            Position = position;
        }
    }
}
