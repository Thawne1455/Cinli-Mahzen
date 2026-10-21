using UnityEngine;

namespace CinliMahzen.Possession
{
    /// <summary>
    /// A thrown object's flight (Teknik §6.4, TODO 1.8), plain data: a straight line along <see cref="Direction"/> plus a parabolic bump of
    /// apex <see cref="Arc"/> (0 = straight, gravity-free: Mother's Slipper). Pure function of time, so every machine draws the same flight
    /// from one <c>ProjectileSpawned</c>; the authority alone steps it for hits.
    /// </summary>
    public readonly struct ProjectilePath
    {
        public readonly Vector3 Origin;
        /// <summary>Unit direction of travel.</summary>
        public readonly Vector3 Direction;
        public readonly float Speed;
        public readonly float Range;
        public readonly float Arc;
        public readonly double StartTime;

        public ProjectilePath(Vector3 origin, Vector3 direction, float speed, float range, float arc, double startTime)
        {
            Origin = origin;
            Direction = direction.sqrMagnitude > 1e-12f ? direction.normalized : Vector3.forward;
            Speed = speed;
            Range = range;
            Arc = arc;
            StartTime = startTime;
        }

        /// <summary>Seconds from launch to the end of the range.</summary>
        public float Duration => Speed > 0f ? Range / Speed : 0f;

        public double EndTime => StartTime + Duration;

        /// <summary>Distance travelled along <see cref="Direction"/> at <paramref name="time"/> (clamped to 0..Range).</summary>
        public float DistanceAt(double time)
        {
            float s = (float)((time - StartTime) * Speed);
            return s < 0f ? 0f : (s > Range ? Range : s);
        }

        public Vector3 PositionAt(double time)
        {
            float s = DistanceAt(time);
            float u = Range > 0f ? s / Range : 1f;
            return Origin + Direction * s + Vector3.up * (4f * Arc * u * (1f - u));
        }

        public bool IsFinished(double time)
        {
            return time >= EndTime;
        }
    }
}
