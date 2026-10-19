using CinliMahzen.Core;

namespace CinliMahzen.World
{
    /// <summary>
    /// A house marker that other systems address over the network: it has a deterministic <see cref="NetId"/> in the level
    /// range (Teknik 4.1, 4.4). The id is NOT serialized: <see cref="HouseService.Index"/> assigns it from the sorted
    /// hierarchy paths, so every machine that loads the same prefab gets the same ids.
    /// </summary>
    public abstract class NetHouseMarker : HouseMarker, INetEntity
    {
        private NetId _netId;

        /// <summary>Invalid (0) until <see cref="HouseService.Index"/> has run.</summary>
        public NetId NetId
        {
            get { return _netId; }
        }

        internal void AssignNetId(NetId id)
        {
            _netId = id;
        }
    }
}
