using System.Collections.Generic;

namespace CinliMahzen.Possession
{
    /// <summary>
    /// Plain C# cooldown bookkeeping (Tech §6.5). Keys are caller-composed longs
    /// (see <see cref="Key"/>), values are absolute ready times on the <c>Net.Time</c> clock.
    /// Öfke (Rage) is modelled as a duration multiplier: it is applied to every cooldown
    /// started while it is active and can optionally rescale the ones already running.
    /// </summary>
    public sealed class CooldownTracker
    {
        private readonly Dictionary<long, int> _indexByKey = new Dictionary<long, int>();
        private readonly List<long> _keys = new List<long>();
        private readonly List<double> _readyAt = new List<double>();

        public CooldownTracker(float multiplier = 1f)
        {
            Multiplier = multiplier;
        }

        /// <summary>Duration multiplier (1 = normal, <c>RageCooldownMult</c> while Öfke is active).</summary>
        public float Multiplier { get; private set; }

        public int Count => _keys.Count;

        /// <summary>Composes a key from two ids, e.g. (player, object) or (object, actionIndex).</summary>
        public static long Key(int a, int b)
        {
            return ((long)a << 32) | (uint)b;
        }

        /// <summary>Starts (or restarts) a cooldown. Duration is scaled by <see cref="Multiplier"/>.</summary>
        public void Start(long key, double now, float baseDuration)
        {
            double readyAt = now + (baseDuration > 0f ? baseDuration * Multiplier : 0f);
            if (_indexByKey.TryGetValue(key, out int i))
            {
                _readyAt[i] = readyAt;
                return;
            }
            _indexByKey.Add(key, _keys.Count);
            _keys.Add(key);
            _readyAt.Add(readyAt);
        }

        /// <summary>Sets an absolute ready time as received from the authority (no multiplier applied).</summary>
        public void SetReadyAt(long key, double readyAt)
        {
            if (_indexByKey.TryGetValue(key, out int i))
            {
                _readyAt[i] = readyAt;
                return;
            }
            _indexByKey.Add(key, _keys.Count);
            _keys.Add(key);
            _readyAt.Add(readyAt);
        }

        public bool IsReady(long key, double now)
        {
            return !_indexByKey.TryGetValue(key, out int i) || now >= _readyAt[i];
        }

        /// <summary>Seconds until ready (0 if ready or unknown).</summary>
        public double Remaining(long key, double now)
        {
            if (!_indexByKey.TryGetValue(key, out int i))
            {
                return 0d;
            }
            double r = _readyAt[i] - now;
            return r > 0d ? r : 0d;
        }

        /// <summary>Absolute ready time, or <see cref="double.NegativeInfinity"/> if never started.</summary>
        public double ReadyAt(long key)
        {
            return _indexByKey.TryGetValue(key, out int i) ? _readyAt[i] : double.NegativeInfinity;
        }

        /// <summary>
        /// Changes the multiplier. With <paramref name="rescaleRunning"/> the remaining time of every
        /// running cooldown is scaled by <c>newMultiplier / oldMultiplier</c> (Öfke kicks in immediately).
        /// </summary>
        public void SetMultiplier(float multiplier, double now, bool rescaleRunning = true)
        {
            if (multiplier <= 0f)
            {
                return;
            }
            if (rescaleRunning && multiplier != Multiplier)
            {
                double ratio = multiplier / (double)Multiplier;
                for (int i = 0; i < _readyAt.Count; i++)
                {
                    double remaining = _readyAt[i] - now;
                    if (remaining > 0d)
                    {
                        _readyAt[i] = now + remaining * ratio;
                    }
                }
            }
            Multiplier = multiplier;
        }

        public void Clear(long key)
        {
            if (!_indexByKey.TryGetValue(key, out int i))
            {
                return;
            }
            int last = _keys.Count - 1;
            if (i != last)
            {
                long movedKey = _keys[last];
                _keys[i] = movedKey;
                _readyAt[i] = _readyAt[last];
                _indexByKey[movedKey] = i;
            }
            _keys.RemoveAt(last);
            _readyAt.RemoveAt(last);
            _indexByKey.Remove(key);
        }

        /// <summary>Forgets every cooldown and resets the multiplier (new round).</summary>
        public void Reset(float multiplier = 1f)
        {
            _indexByKey.Clear();
            _keys.Clear();
            _readyAt.Clear();
            Multiplier = multiplier;
        }
    }
}
