using UnityEngine;

namespace CinliMahzen.Player
{
    /// <summary>
    /// Steering of the dummy human bot (TODO 3.12), plain C#: follows the corners of a NavMesh path on the ground plane and turns the
    /// human toward the next corner. The bot feeds the result into the same <see cref="CinliMahzen.Core.IHumanInput"/> a player uses,
    /// so the human moves through its CharacterController exactly like a played one (no NavMeshAgent drives it).
    /// </summary>
    public sealed class BotSteering
    {
        private readonly Vector3[] _corners;
        private int _count;
        private int _index;

        /// <param name="maxCorners">Corner buffer size (a longer path is cut).</param>
        /// <param name="arriveDistance">Horizontal distance at which a corner counts as reached (m).</param>
        public BotSteering(int maxCorners, float arriveDistance)
        {
            _corners = new Vector3[Mathf.Max(2, maxCorners)];
            ArriveDistance = arriveDistance;
        }

        public float ArriveDistance { get; }

        /// <summary>The buffer to fill (e.g. <c>NavMeshPath.GetCornersNonAlloc</c>); then call <see cref="SetPath"/> with the count.</summary>
        public Vector3[] Buffer => _corners;

        public bool HasPath => _index < _count;

        public int CornerCount => _count;

        /// <summary>Starts following the first <paramref name="count"/> corners of <see cref="Buffer"/>. Corner 0 is the start, so it is skipped.</summary>
        public void SetPath(int count)
        {
            _count = Mathf.Clamp(count, 0, _corners.Length);
            _index = _count > 1 ? 1 : _count;
        }

        public void Clear()
        {
            _count = 0;
            _index = 0;
        }

        /// <summary>
        /// One step from <paramref name="position"/> facing <paramref name="yawDeg"/>: the local move (x strafe, y forward, length ≤ 1) and the
        /// yaw change (degrees) toward the current corner, at most <paramref name="maxTurn"/>. Advances past reached corners; zero when done.
        /// </summary>
        public void Step(Vector3 position, float yawDeg, float maxTurn, out Vector2 move, out float yawDelta)
        {
            while (_index < _count && HorizontalDistance(position, _corners[_index]) <= ArriveDistance)
                _index++;
            if (_index >= _count)
            {
                move = Vector2.zero;
                yawDelta = 0f;
                return;
            }
            Vector3 target = _corners[_index];
            float toYaw = YawTo(position, target);
            yawDelta = Mathf.Clamp(Mathf.DeltaAngle(yawDeg, toYaw), -maxTurn, maxTurn);
            move = LocalMove(position, yawDeg, target);
        }

        /// <summary>World yaw (degrees, 0 = +Z, 90 = +X) from <paramref name="from"/> toward <paramref name="to"/> on the ground plane.</summary>
        public static float YawTo(Vector3 from, Vector3 to)
        {
            float dx = to.x - from.x;
            float dz = to.z - from.z;
            return Mathf.Atan2(dx, dz) * Mathf.Rad2Deg;
        }

        /// <summary>Unit move in the walker's local frame (x right, y forward) toward <paramref name="target"/>; zero when on top of it.</summary>
        public static Vector2 LocalMove(Vector3 position, float yawDeg, Vector3 target)
        {
            var dir = new Vector3(target.x - position.x, 0f, target.z - position.z);
            if (dir.sqrMagnitude < 1e-6f)
                return Vector2.zero;
            dir.Normalize();
            Vector3 local = Quaternion.Euler(0f, -yawDeg, 0f) * dir;
            return new Vector2(local.x, local.z);
        }

        public static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}
