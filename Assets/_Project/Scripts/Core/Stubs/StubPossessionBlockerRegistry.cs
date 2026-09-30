using System.Collections.Generic;
using UnityEngine;

namespace CinliMahzen.Core
{
    /// <summary>Minimal working registry until A2.x salt zones get their own service.</summary>
    public sealed class StubPossessionBlockerRegistry : IPossessionBlockerRegistry
    {
        private readonly List<IPossessionBlocker> _blockers = new List<IPossessionBlocker>();

        public void Add(IPossessionBlocker b)
        {
            if (b != null && !_blockers.Contains(b))
                _blockers.Add(b);
        }

        public void Remove(IPossessionBlocker b)
        {
            _blockers.Remove(b);
        }

        public bool IsBlocked(Vector3 p)
        {
            for (int i = 0; i < _blockers.Count; i++)
            {
                if (_blockers[i].Blocks(p))
                    return true;
            }
            return false;
        }
    }
}
