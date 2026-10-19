using CinliMahzen.Core;
using UnityEngine;

namespace CinliMahzen.Match
{
    /// <summary>One pawn to create, as planned by <see cref="PawnSpawnPlanner"/>.</summary>
    public readonly struct PawnSpawnEntry
    {
        /// <summary>Slot of the owner in the registry (0..3 offline).</summary>
        public readonly int PlayerIndex;
        public readonly PlayerId Owner;
        public readonly Role Role;
        public readonly PawnKind Kind;
        public readonly NetId NetId;
        public readonly Vector3 Position;

        public PawnSpawnEntry(int playerIndex, PlayerId owner, Role role, PawnKind kind, NetId netId, Vector3 position)
        {
            PlayerIndex = playerIndex;
            Owner = owner;
            Role = role;
            Kind = kind;
            NetId = netId;
            Position = position;
        }
    }
}
