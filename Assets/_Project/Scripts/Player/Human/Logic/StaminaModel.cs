namespace CinliMahzen.Player
{
    /// <summary>
    /// Sprint stamina (Oyun §14: StaminaMax / StaminaDrain / StaminaRegen / StaminaRegenDelay), plain C#.
    /// Sprinting drains; once sprinting stops, regen starts after <see cref="RegenDelay"/> seconds. The caller passes the
    /// time (GameServices.Net.Time) so the model is deterministic and testable.
    /// </summary>
    public sealed class StaminaModel
    {
        private double _lastSprintTime = double.NegativeInfinity;

        public StaminaModel(float max, float drain, float regen, float regenDelay)
        {
            Max = max;
            Drain = drain;
            Regen = regen;
            RegenDelay = regenDelay;
            Value = max;
        }

        public float Max { get; }
        /// <summary>Per second while sprinting.</summary>
        public float Drain { get; }
        /// <summary>Per second while recovering.</summary>
        public float Regen { get; }
        /// <summary>Seconds after the last sprint before regen starts.</summary>
        public float RegenDelay { get; }
        public float Value { get; private set; }

        /// <summary>Back to full (new round / revive); the regen delay is forgotten.</summary>
        public void Reset()
        {
            Value = Max;
            _lastSprintTime = double.NegativeInfinity;
        }

        /// <summary>
        /// Advances one frame. Returns true when the human sprints this frame: sprint wanted, actually moving and stamina left.
        /// With 0 stamina there is no sprint; the frame that empties the bar still counts as a sprint frame.
        /// </summary>
        public bool Tick(float dt, bool wantsSprint, bool isMoving, double now)
        {
            if (dt < 0f)
                dt = 0f;

            bool sprinting = wantsSprint && isMoving && Value > 0f;
            if (sprinting)
            {
                Value -= Drain * dt;
                if (Value < 0f)
                    Value = 0f;
                _lastSprintTime = now;
            }
            else if (Value < Max && now - _lastSprintTime >= RegenDelay)
            {
                Value += Regen * dt;
                if (Value > Max)
                    Value = Max;
            }
            return sprinting;
        }
    }
}
