using CinliMahzen.Core;
using UnityEngine;

namespace CinliMahzen.World
{
    /// <summary>Plain-data copy of one <see cref="StageGate"/> (and its optional <see cref="GateLock"/>).</summary>
    public sealed class HouseGateEntry : HouseNetEntry
    {
        /// <summary>The stage this gate completes; for the cellar hatch use <see cref="StageFor"/>.</summary>
        public int StageIndex { get; }

        /// <summary>ItemDefinition.Id the gate needs; empty when it has no lock.</summary>
        public string RequiredItemId { get; }

        public Vector3 Position { get; }

        /// <summary>TODO 4.1: the cellar hatch (completes stage StagesActive + 1, see <see cref="HouseContract.GateStage"/>).</summary>
        public bool IsCellarHatch { get; }

        public bool HasLock
        {
            get { return RequiredItemId.Length > 0; }
        }

        public HouseGateEntry(string path, int stageIndex, string requiredItemId, Vector3 position, NetId netId = default,
            bool isCellarHatch = false)
            : base(path, netId)
        {
            StageIndex = stageIndex;
            RequiredItemId = requiredItemId ?? string.Empty;
            Position = position;
            IsCellarHatch = isCellarHatch;
        }

        /// <summary>The stage this gate completes when <paramref name="stagesActive"/> puzzle stages are played.</summary>
        public int StageFor(int stagesActive)
        {
            return HouseContract.GateStage(StageIndex, IsCellarHatch, stagesActive);
        }
    }
}
