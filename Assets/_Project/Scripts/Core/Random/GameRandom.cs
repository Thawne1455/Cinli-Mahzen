using System;
using System.Collections.Generic;

namespace CinliMahzen.Core
{
    /// <summary>
    /// Seeded RNG for anything that affects game state (Teknik §1.2). Same seed → same sequence on every client.
    /// UnityEngine.Random is for cosmetics only.
    /// </summary>
    public sealed class GameRandom
    {
        private readonly Random _rng;

        public GameRandom(int seed)
        {
            Seed = seed;
            _rng = new Random(seed);
        }

        public int Seed { get; }

        /// <summary>[0, 1)</summary>
        public float Value => (float)_rng.NextDouble();

        /// <summary>[minInclusive, maxExclusive) — same convention as UnityEngine.Random.Range(int,int).</summary>
        public int Range(int minInclusive, int maxExclusive)
        {
            return maxExclusive <= minInclusive ? minInclusive : _rng.Next(minInclusive, maxExclusive);
        }

        /// <summary>[min, max)</summary>
        public float Range(float min, float max)
        {
            return min + (float)_rng.NextDouble() * (max - min);
        }

        public bool Chance(float probability)
        {
            return _rng.NextDouble() < probability;
        }

        public T Pick<T>(IList<T> list)
        {
            if (list == null || list.Count == 0)
                throw new ArgumentException("Pick on empty list");
            return list[_rng.Next(list.Count)];
        }

        /// <summary>In-place Fisher–Yates.</summary>
        public void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                T tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }

        /// <summary>Returns an index chosen proportionally to <paramref name="weights"/>; -1 if all weights are ≤ 0.</summary>
        public int WeightedPick(IList<float> weights)
        {
            float total = 0f;
            for (int i = 0; i < weights.Count; i++)
            {
                if (weights[i] > 0f)
                    total += weights[i];
            }
            if (total <= 0f)
                return -1;

            float r = (float)_rng.NextDouble() * total;
            int last = -1;
            for (int i = 0; i < weights.Count; i++)
            {
                if (weights[i] <= 0f)
                    continue;
                last = i;
                if (r < weights[i])
                    return i;
                r -= weights[i];
            }
            return last;
        }
    }
}
