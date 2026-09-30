namespace CinliMahzen.Jinn
{
    /// <summary>
    /// One evil jinn's energy (Tech §6.5), plain C#. Authoritative on the authority
    /// (Tick / TrySpend), mirrored on clients with <see cref="ApplyRemote"/>.
    /// </summary>
    public sealed class JinnEnergyCore
    {
        private JinnEnergySettings _settings;
        private float _lastSentValue;
        private double _lastSentTime;
        private bool _forceSync;

        public JinnEnergyCore(in JinnEnergySettings settings)
        {
            _settings = settings;
            Reset();
        }

        public float Current { get; private set; }

        public float Max => _settings.Max;

        public float Normalized => _settings.Max > 0f ? Current / _settings.Max : 0f;

        public bool RageActive { get; private set; }

        /// <summary>Debug (F6): every cost is affordable and nothing is deducted.</summary>
        public bool Unlimited { get; set; }

        /// <summary>Effective regen per second (Öfke applied).</summary>
        public float EffectiveRegen => _settings.Regen * (RageActive ? _settings.RageRegenMult : 1f);

        /// <summary>Back to EnergyStart, Öfke off (new round). Forces a sync.</summary>
        public void Reset()
        {
            Current = Clamp(_settings.Start);
            RageActive = false;
            _forceSync = true;
        }

        public void Configure(in JinnEnergySettings settings)
        {
            _settings = settings;
            Current = Clamp(Current);
        }

        /// <summary>Authority: regenerates. Returns the amount actually gained.</summary>
        public float Tick(float deltaTime)
        {
            if (deltaTime <= 0f || Current >= _settings.Max)
            {
                return 0f;
            }
            float before = Current;
            Current = Clamp(Current + EffectiveRegen * deltaTime);
            return Current - before;
        }

        public bool CanAfford(float cost)
        {
            return Unlimited || cost <= 0f || Current >= cost;
        }

        /// <summary>Authority: deducts <paramref name="cost"/> if affordable. Forces a sync.</summary>
        public bool TrySpend(float cost)
        {
            if (!CanAfford(cost))
            {
                return false;
            }
            if (!Unlimited && cost > 0f)
            {
                Current = Clamp(Current - cost);
                _forceSync = true;
            }
            return true;
        }

        /// <summary>Öfke on/off. Forces a sync so HUDs switch immediately.</summary>
        public void SetRage(bool active)
        {
            if (RageActive == active)
            {
                return;
            }
            RageActive = active;
            _forceSync = true;
        }

        /// <summary>Client: applies the value from an <c>EnergyChanged</c> broadcast.</summary>
        public void ApplyRemote(float value)
        {
            Current = Clamp(value);
        }

        /// <summary>
        /// Authority: true if an <c>EnergyChanged</c> broadcast is due — after a spend / reset / Öfke
        /// change, or when regen changed the value by ≥ SyncMinDelta (or filled it up) and
        /// SyncMinInterval passed since the last one.
        /// </summary>
        public bool NeedsSync(double now)
        {
            if (_forceSync)
            {
                return true;
            }
            float delta = Current - _lastSentValue;
            if (delta < 0f)
            {
                delta = -delta;
            }
            if (delta <= 0f)
            {
                return false;
            }
            bool bigEnough = delta >= _settings.SyncMinDelta || Current >= _settings.Max;
            return bigEnough && now - _lastSentTime >= _settings.SyncMinInterval;
        }

        /// <summary>Authority: call right after broadcasting.</summary>
        public void MarkSynced(double now)
        {
            _lastSentValue = Current;
            _lastSentTime = now;
            _forceSync = false;
        }

        private float Clamp(float v)
        {
            if (v < 0f)
            {
                return 0f;
            }
            return v > _settings.Max ? _settings.Max : v;
        }
    }
}
