using System.Collections.Generic;

namespace CinliMahzen.Core
{
    /// <summary>
    /// NetId → entity map (Teknik §4.4). Pawn and level ids are assigned by their spawners;
    /// runtime ids (60000+) come from <see cref="AllocateRuntimeId"/> on the authority.
    /// </summary>
    public sealed class EntityRegistry : IEntityRegistry
    {
        private readonly Dictionary<int, INetEntity> _entities = new Dictionary<int, INetEntity>();
        private int _nextRuntimeId = NetId.RuntimeMin;

        public int Count => _entities.Count;

        public void Register(INetEntity e)
        {
            if (e == null)
                return;
            NetId id = e.NetId;
            if (!id.IsValid)
            {
                CMLog.Error("Entities", "Register: " + e + " has no NetId");
                return;
            }
            if (_entities.TryGetValue(id.Value, out INetEntity existing) && !ReferenceEquals(existing, e))
            {
                CMLog.Error("Entities", "Register: " + id + " already used by " + existing + ", rejected " + e);
                return;
            }
            _entities[id.Value] = e;
        }

        public void Unregister(INetEntity e)
        {
            if (e == null)
                return;
            int key = e.NetId.Value;
            if (_entities.TryGetValue(key, out INetEntity existing) && ReferenceEquals(existing, e))
                _entities.Remove(key);
        }

        public bool TryGet<T>(NetId id, out T entity) where T : class
        {
            if (_entities.TryGetValue(id.Value, out INetEntity e))
            {
                entity = e as T;
                return entity != null;
            }
            entity = null;
            return false;
        }

        public NetId AllocateRuntimeId()
        {
            Net.INetBridge net = GameServices.Net;
            if (net != null && !net.IsAuthority)
                CMLog.Error("Entities", "AllocateRuntimeId called on a non-authority client");

            while (_entities.ContainsKey(_nextRuntimeId))
                _nextRuntimeId++;
            return new NetId(_nextRuntimeId++);
        }

        /// <summary>Forgets everything and restarts runtime ids (new round).</summary>
        public void Clear()
        {
            _entities.Clear();
            _nextRuntimeId = NetId.RuntimeMin;
        }
    }
}
