using UnityEngine;

namespace CinliMahzen.Player
{
    /// <summary>
    /// Third-person orbit pose, plain C# (TODO 1.2: the orbit camera never enters a wall). The physics query lives in the
    /// CameraRig: it casts a sphere from the pivot toward the camera and passes the distance of the nearest hit here
    /// (<see cref="NoHit"/> when the way is free); the solver only shortens the distance and builds the pose.
    /// Angles are degrees, Unity convention: positive pitch looks down, so the camera sits above the pivot.
    /// </summary>
    public static class OrbitSolver
    {
        /// <summary>"The ray toward the camera is free": use as the hit distance when nothing was hit.</summary>
        public const float NoHit = float.PositiveInfinity;

        public static float ClampPitch(float pitch, float minPitch, float maxPitch)
        {
            return Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        /// <summary>Unit vector from the pivot toward the camera for the given look angles.</summary>
        public static Vector3 BackDirection(float yaw, float pitch)
        {
            return Quaternion.Euler(pitch, yaw, 0f) * Vector3.back;
        }

        /// <summary>
        /// Camera distance after the wall check. <paramref name="hitDistance"/> is how far the cast sphere travelled before it
        /// touched something; the camera stops <paramref name="skin"/> short of that. Never longer than
        /// <paramref name="desired"/>, never shorter than <paramref name="minDistance"/> (or <paramref name="desired"/> itself
        /// when that is smaller), so a pivot jammed against a wall still gets a usable view.
        /// </summary>
        public static float ResolveDistance(float desired, float hitDistance, float skin, float minDistance)
        {
            if (float.IsNaN(hitDistance))
                hitDistance = NoHit;

            float floor = Mathf.Min(minDistance, desired);
            float limit = hitDistance - skin;
            float distance = limit < desired ? limit : desired;
            return distance > floor ? distance : floor;
        }

        /// <summary>The camera pose: <paramref name="distance"/> behind <paramref name="pivot"/> along the look direction, looking at it.</summary>
        public static void Solve(
            Vector3 pivot, float yaw, float pitch, float desiredDistance, float hitDistance, float skin, float minDistance,
            out Vector3 position, out Quaternion rotation)
        {
            float distance = ResolveDistance(desiredDistance, hitDistance, skin, minDistance);
            rotation = Quaternion.Euler(pitch, yaw, 0f);
            position = pivot - rotation * Vector3.forward * distance;
        }
    }
}
