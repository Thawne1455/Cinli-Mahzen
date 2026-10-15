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
        private readonly Dictionary<NetId, RoomLightState> _lights = new Dictionary<NetId, RoomLightState>();
        private readonly Dictionary<long, float> _distances = new Dictionary<long, float>();

        public bool IsPlaying { get; set; } = true;
        public bool JinnsAwake { get; set; } = true;

        /// <summary>Light state of every object without an explicit one (v2.1: houses start dark).</summary>
        public RoomLightState DefaultLight { get; set; } = RoomLightState.Off;

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

        public void SetLight(NetId obj, RoomLightState light)
        {
            _lights[obj] = light;
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

        public RoomLightState LightOfObject(NetId obj)
        {
            return _lights.TryGetValue(obj, out RoomLightState l) ? l : DefaultLight;
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
