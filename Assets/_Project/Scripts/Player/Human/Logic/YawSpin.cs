namespace CinliMahzen.Player
{
    /// <summary>
    /// Timed yaw turn (Mother's Slipper, StatusType.CameraSpin180: 180 degrees in 0.15 s), plain C#.
    /// <see cref="Step"/> returns the yaw delta to apply this frame; the deltas add up to exactly the requested degrees
    /// (the last frame delivers whatever remains, so a long frame never overshoots).
    /// </summary>
    public sealed class YawSpin
    {
        private float _remainingDegrees;
        private float _timeLeft;

        public bool IsActive { get; private set; }

        /// <summary>Degrees still to be delivered by <see cref="Step"/>.</summary>
        public float RemainingDegrees => IsActive ? _remainingDegrees : 0f;

        /// <summary>
        /// Starts a spin of <paramref name="degrees"/> over <paramref name="duration"/> seconds (0 = on the next step).
        /// A spin already running is not lost: its remaining degrees are added and the timer restarts.
        /// </summary>
        public void Begin(float degrees, float duration)
        {
            float total = degrees + RemainingDegrees;
            if (total == 0f)
            {
                Cancel();
                return;
            }
            _remainingDegrees = total;
            _timeLeft = duration > 0f ? duration : 0f;
            IsActive = true;
        }

        public void Cancel()
        {
            IsActive = false;
            _remainingDegrees = 0f;
            _timeLeft = 0f;
        }

        /// <summary>Yaw delta (degrees, signed) for a frame of <paramref name="dt"/> seconds.</summary>
        public float Step(float dt)
        {
            if (!IsActive || dt <= 0f)
                return 0f;

            if (dt >= _timeLeft)
            {
                float rest = _remainingDegrees;
                Cancel();
                return rest;
            }

            float delta = _remainingDegrees * (dt / _timeLeft);
            _remainingDegrees -= delta;
            _timeLeft -= dt;
            return delta;
        }
    }
}
