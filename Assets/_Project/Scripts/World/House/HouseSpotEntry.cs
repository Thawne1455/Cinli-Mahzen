using CinliMahzen.Core;
using UnityEngine;

namespace CinliMahzen.World
{
    /// <summary>
    /// Plain-data copy of a point marker: <see cref="LampMarker"/>, <see cref="LightSwitch"/>, <see cref="KeyHideSpot"/>,
    /// <see cref="CologneSpawnSpot"/>, <see cref="DigSpot"/> or <see cref="ToolShedItemPoint"/> (see <see cref="Kind"/>).
    /// </summary>
    public sealed class HouseSpotEntry : HouseNetEntry
    {
        public HouseSpotKind Kind { get; }

        /// <summary>RoomMarker id written on the marker, or <see cref="HouseIndex.NoRoom"/> when the marker has no room field (resolve it from <see cref="Position"/>).</summary>
        public int RoomId { get; }

        /// <summary>ItemDefinition.Id for <see cref="HouseSpotKind.ToolShedItem"/>; empty for every other kind.</summary>
        public string ItemId { get; }

        public Vector3 Position { get; }

        public HouseSpotEntry(string path, HouseSpotKind kind, int roomId, string itemId, Vector3 position, NetId netId = default)
            : base(path, netId)
        {
            Kind = kind;
            RoomId = roomId;
            ItemId = itemId ?? string.Empty;
            Position = position;
        }
    }
}
