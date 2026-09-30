using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.Possession;

namespace CinliMahzen.Tests.EditMode.B
{
    /// <summary>Test double for <see cref="IPossessionWorld"/>: everything is allowed until a test says otherwise.</summary>
    public sealed class FakePossessionWorld : IPossessionWorld
    {
        private readonly Dictionary<PlayerId, Role> _roles = new Dictionary<PlayerId, Role>();
        private readonly Dictionary<PlayerId, double> _lockedUntil = new Dictionary<PlayerId, double>();
        private readonly HashSet<NetId> _blocked = new HashSet<NetId>();
        private readonly Dictionary<long, float> _distances = new Dictionary<long, float>();

        public bool IsPlaying { get; set; } = true;
        public bool JinnsAwake { get; set; } = true;

        /// <summary>Distance used when no explicit one was set. Negative = unknown entity.</summary>
        public float DefaultDistance { get; set; } = 1f;

        public void SetRole(PlayerId player, Role role)
        {
            _roles[player] = role;
        }

        public void LockUntil(PlayerId player, double until)
        {
            _lockedUntil[player] = until;
        }

        public void SetBlocked(NetId obj, bool blocked)
        {
            if (blocked)
            {
                _blocked.Add(obj);
            }
            else
            {
                _blocked.Remove(obj);
            }
        }

        public void SetDistance(PlayerId player, NetId obj, float distance)
        {
            _distances[CooldownTracker.Key(player.Value, obj.Value)] = distance;
        }

        public Role GetRole(PlayerId player)
        {
            return _roles.TryGetValue(player, out Role r) ? r : Role.None;
        }

        public bool IsPossessionLocked(PlayerId player, double now)
        {
            return _lockedUntil.TryGetValue(player, out double until) && now < until;
        }

        public bool IsObjectBlocked(NetId obj)
        {
            return _blocked.Contains(obj);
        }

        public bool TryGetDistance(PlayerId player, NetId obj, out float distance)
        {
            if (!_distances.TryGetValue(CooldownTracker.Key(player.Value, obj.Value), out distance))
            {
                distance = DefaultDistance;
            }
            return distance >= 0f;
        }
    }
}
