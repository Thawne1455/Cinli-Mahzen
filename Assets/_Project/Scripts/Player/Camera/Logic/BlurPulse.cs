using UnityEngine;

namespace CinliMahzen.Player
{
    /// <summary>
    /// Timed blur envelope, plain C# (Spirit Bump: the good jinn's view blurs for SpiritBumpBlurTime). <see cref="Tick"/> returns
    /// the strength 0..1 the rig feeds to the blur volume weight and the shake: a fade-in after the trigger, full strength,
    /// a fade-out over the last seconds, then 0.
    /// Triggering while a pulse runs extends it to the later end and never adds up, so the same bump reaching the rig twice
    /// (SpiritBumpedEvt and the VisionBlurred status) still blurs for one duration, not two. The fade-in is not replayed.
    /// </summary>
    public sealed class BlurPulse
    {
        private readonly float _fadeInTime;
        private readonly float _fadeOutTime;
        private float _elapsed;
        private float _remaining;

        /// <param name="fadeInTime">Seconds to reach full strength; 0 = instantly.</param>
        /// <param name="fadeOutTime">Seconds before the end over which the strength drops to 0; 0 = cut off.</param>
        public BlurPulse(float fadeInTime, float fadeOutTime)
        {
            _fadeInTime = fadeInTime > 0f ? fadeInTime : 0f;
            _fadeOutTime = fadeOutTime > 0f ? fadeOutTime : 0f;
        }

        public bool IsActive => _remaining > 0f;

        /// <summary>Seconds until the pulse ends (0 when idle).</summary>
        public float Remaining => _remaining > 0f ? _remaining : 0f;

        /// <summary>Strength computed by the last <see cref="Trigger"/> / <see cref="Tick"/>.</summary>
        public float Intensity { get; private set; }

        /// <summary>Starts a pulse, or stretches the running one so it ends at least <paramref name="duration"/> from now. NaN and values &lt;= 0 are ignored.</summary>
        public void Trigger(float duration)
        {
            if (!(duration > 0f))
                return;

            if (_remaining <= 0f)
            {
                _elapsed = 0f;
                _remaining = duration;
            }
            else if (duration > _remaining)
            {
                _remaining = duration;
            }
            Intensity = Envelope();
        }

        /// <summary>Advances by <paramref name="dt"/> seconds and returns the strength 0..1 for this frame.</summary>
        public float Tick(float dt)
        {
            if (_remaining <= 0f)
            {
                Intensity = 0f;
                return 0f;
            }

            if (dt > 0f)
            {
                _elapsed += dt;
                _remaining -= dt;
            }
            if (_remaining <= 0f)
            {
                _remaining = 0f;
                Intensity = 0f;
                return 0f;
            }

            Intensity = Envelope();
            return Intensity;
        }

        public void Cancel()
        {
            _remaining = 0f;
            _elapsed = 0f;
            Intensity = 0f;
        }

        private float Envelope()
        {
            float strength = 1f;
            if (_fadeInTime > 0f)
                strength = Mathf.Min(strength, _elapsed / _fadeInTime);
            if (_fadeOutTime > 0f)
                strength = Mathf.Min(strength, _remaining / _fadeOutTime);
            return Mathf.Clamp01(strength);
        }
    }
}
