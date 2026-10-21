using CinliMahzen.Core;
using UnityEngine;

namespace CinliMahzen.Possession
{
    /// <summary>The human's damage and status receivers, as found by a hit check.</summary>
    public readonly struct HumanTarget
    {
        public readonly IDamageable Damageable;
        public readonly IStatusReceiver Status;

        public HumanTarget(IDamageable damageable, IStatusReceiver status)
        {
            Damageable = damageable;
            Status = status;
        }

        public bool IsValid => Damageable != null;
    }

    /// <summary>
    /// What <see cref="PossessionActionRunner"/> needs from the scene (TODO 1.8): object data and physics queries on the authority. Implemented
    /// by <see cref="PossessionSystem"/> (layers HumanHitbox / Environment); EditMode tests use a fake. Hit checks are authority-only
    /// overlap / sphere casts — no Rigidbody decides anything (CLAUDE.md rule 3).
    /// </summary>
    public interface IActionWorld
    {
        Role GetRole(PlayerId player);

        /// <summary>Definition, world position and yaw (degrees) of a registered possessable.</summary>
        bool TryGetObject(NetId obj, out PossessableDefinition definition, out Vector3 position, out float yaw);

        /// <summary>The human's hitbox overlaps the box (world centre, half extents, rotation).</summary>
        bool FindHumanInBox(Vector3 center, Vector3 halfExtents, Quaternion rotation, out HumanTarget target);

        /// <summary>
        /// Sphere of <paramref name="radius"/> moved from <paramref name="from"/> to <paramref name="to"/>: true when it hits the human's hitbox or
        /// the environment first; <paramref name="point"/> is where it stopped, <paramref name="target"/> valid only for the human.
        /// </summary>
        bool CastSegment(Vector3 from, Vector3 to, float radius, out Vector3 point, out HumanTarget target);

        /// <summary>How far a sphere of <paramref name="radius"/> can move from <paramref name="origin"/> along <paramref name="direction"/> before the environment (≤ max).</summary>
        float FreeDistance(Vector3 origin, Vector3 direction, float radius, float maxDistance);

        int RoomOf(Vector3 position);

        bool IsHumanFainted { get; }

        /// <summary>TODO 3.7: the round is in the cellar finale (the good jinn's Spirit Block works only there).</summary>
        bool IsCellarPhase { get; }

        /// <summary>
        /// TODO 3.7: the first good jinn spirit (a sphere of <paramref name="radius"/> around its pawn) the segment passes through, and where.
        /// Spirits have no colliders, so this is plain geometry against the good jinn pawns.
        /// </summary>
        bool CastGoodJinn(Vector3 from, Vector3 to, float radius, out PlayerId goodJinn, out Vector3 point);

        Vector3 HumanPosition { get; }
    }
}
