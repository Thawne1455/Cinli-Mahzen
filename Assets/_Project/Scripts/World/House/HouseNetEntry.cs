using CinliMahzen.Core;

namespace CinliMahzen.World
{
    /// <summary>
    /// Base of the index entries that carry a <see cref="NetId"/> (Teknik 4.1, 4.4). The id is assigned once by
    /// <see cref="HouseIndexBuilder"/> from the sorted <see cref="Path"/>s, unless the entry was created with an explicit id
    /// (tests do that to provoke collisions).
    /// </summary>
    public abstract class HouseNetEntry
    {
        /// <summary>Unique key of the marker: hierarchy path relative to the house root plus "#" plus the marker type name.</summary>
        public string Path { get; }

        /// <summary>Invalid (0) until the index is built.</summary>
        public NetId NetId { get; private set; }

        protected HouseNetEntry(string path, NetId netId)
        {
            Path = path ?? string.Empty;
            NetId = netId;
        }

        internal void AssignNetId(NetId id)
        {
            NetId = id;
        }

        public override string ToString()
        {
            return Path + " " + NetId;
        }
    }
}
