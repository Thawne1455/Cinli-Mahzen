using UnityEngine;

namespace CinliMahzen.Player
{
    /// <summary>
    /// Timed blend between the camera's old pose and a (possibly moving) target pose, plain C# (TODO 1.2: mode changes take 0.3 s).
    /// <see cref="Begin"/> remembers where the view was; each <see cref="Evaluate"/> advances the clock and returns the eased pose
    /// between that start and the target of this frame, so a head that keeps moving during the blend is followed.
    /// The duration is passed in (the rig serializes it); a zero duration means "no blend".
    /// </summary>
    public sealed class CameraBlendState
    {
        private Vector3 _fromPosition;
        private Quaternion _fromRotation = Quaternion.identity;
        private float _duration;
        private float _elapsed;

        public bool IsBlending { get; private set; }

        /// <summary>0..1 share of the blend already played (1 when idle).</summary>
        public float Progress => IsBlending ? Mathf.Clamp01(_elapsed / _duration) : 1f;

        /// <summary>
        /// Starts a blend away from the given pose. <paramref name="duration"/> &lt;= 0 (or NaN) cancels instead, so the next
        /// <see cref="Evaluate"/> returns the target at once. A blend already running is replaced: the new one starts from
        /// whatever pose the caller passes (normally the camera's current pose).
        /// </summary>
        public void Begin(Vector3 fromPosition, Quaternion fromRotation, float duration)
        {
            if (!(duration > 0f))
            {
                Cancel();
                return;
            }
            _fromPosition = fromPosition;
            _fromRotation = fromRotation;
            _duration = duration;
            _elapsed = 0f;
            IsBlending = true;
        }

        public void Cancel()
        {
            IsBlending = false;
            _elapsed = 0f;
        }

        /// <summary>
        /// Advances by <paramref name="dt"/> seconds and returns the pose for this frame. Idle: the target itself.
        /// The frame that reaches the duration returns exactly the target and ends the blend.
        /// </summary>
        public void Evaluate(Vector3 targetPosition, Quaternion targetRotation, float dt, out Vector3 position, out Quaternion rotation)
        {
            if (!IsBlending)
            {
                position = targetPosition;
                rotation = targetRotation;
                return;
            }

            if (dt > 0f)
                _elapsed += dt;

            if (_elapsed >= _duration)
            {
                Cancel();
                position = targetPosition;
                rotation = targetRotation;
                return;
            }

            float t = _elapsed / _duration;
            float eased = t * t * (3f - 2f * t);   // smoothstep: gentle start and stop
            position = Vector3.Lerp(_fromPosition, targetPosition, eased);
            rotation = Quaternion.Slerp(_fromRotation, targetRotation, eased);
        }
    }
}
