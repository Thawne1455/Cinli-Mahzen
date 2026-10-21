using System;
using System.Collections.Generic;
using UnityEngine;

namespace CinliMahzen.World.Graybox
{
    /// <summary>
    /// Pure function from the graybox marker data to a <see cref="HouseIndex"/> (TODO 2.2): the same index
    /// <see cref="HouseService.Index"/> would build from the real prefab, without any Unity object. Tests use it to validate the
    /// shipped house on paper; the keys passed to <see cref="HouseIndex.MakeNetKey"/> match what the editor builder creates.
    /// </summary>
    public static class GrayboxHouseIndexFactory
    {
        /// <summary>Index of <see cref="GrayboxMarkers.Default"/>.</summary>
        public static HouseIndex BuildDefault()
        {
            return Build(GrayboxMarkers.Default);
        }

        /// <summary>Throws <see cref="InvalidOperationException"/> when the NetIds cannot be assigned (duplicate path).</summary>
        public static HouseIndex Build(GrayboxMarkers markers)
        {
            return CreateBuilder(markers).Build();
        }

        /// <summary>A builder filled from the data; add or change entries before calling Build to make variants.</summary>
        public static HouseIndexBuilder CreateBuilder(GrayboxMarkers markers)
        {
            if (markers == null)
            {
                throw new ArgumentNullException("markers");
            }

            var builder = new HouseIndexBuilder();
            for (int i = 0; i < markers.Specs.Count; i++)
            {
                AddSpec(builder, markers.Specs[i]);
            }
            return builder;
        }

        private static void AddSpec(HouseIndexBuilder builder, GrayboxMarkerSpec spec)
        {
            int roomId = GrayboxMarkers.RoomIdOf(spec.RoomKey);
            switch (spec.Kind)
            {
                case GrayboxMarkerKind.Room:
                    builder.Add(new HouseRoomEntry(roomId, spec.RoomKey, spec.Floor, spec.IsOutdoor, spec.Boxes));
                    break;

                case GrayboxMarkerKind.StageZone:
                    var ids = new List<int>(spec.RoomKeys.Count);
                    for (int i = 0; i < spec.RoomKeys.Count; i++)
                    {
                        ids.Add(GrayboxMarkers.RoomIdOf(spec.RoomKeys[i]));
                    }
                    builder.Add(new HouseStageEntry(spec.Stage, ids));
                    break;

                case GrayboxMarkerKind.PuzzleSlot:
                    builder.Add(new HousePuzzleSlotEntry(KeyOf(spec, typeof(PuzzleSlot)), spec.Stage, roomId, spec.Size,
                        spec.AllowedTypes, spec.Position, spec.RewardPosition));
                    break;

                case GrayboxMarkerKind.ReferenceSlot:
                    builder.Add(new HouseReferenceSlotEntry(KeyOf(spec, typeof(ReferenceSlot)), roomId, spec.Size,
                        spec.AllowedTypes, spec.Position));
                    break;

                case GrayboxMarkerKind.StageGate:
                    builder.Add(new HouseGateEntry(KeyOf(spec, typeof(StageGate)), spec.Stage, spec.ItemId, spec.Position, default,
                        spec.IsCellarHatch));
                    break;

                case GrayboxMarkerKind.Lamp:
                    builder.Add(new HouseSpotEntry(KeyOf(spec, typeof(LampMarker)), HouseSpotKind.Lamp, roomId, string.Empty, spec.Position));
                    break;

                case GrayboxMarkerKind.LightSwitch:
                    builder.Add(new HouseSpotEntry(KeyOf(spec, typeof(LightSwitch)), HouseSpotKind.LightSwitch, roomId, string.Empty, spec.Position));
                    break;

                case GrayboxMarkerKind.KeyHideSpot:
                    // KeyHideSpot has no room field: the index resolves its room from the position.
                    builder.Add(new HouseSpotEntry(KeyOf(spec, typeof(KeyHideSpot)), HouseSpotKind.KeyHide, HouseIndex.NoRoom, string.Empty, spec.Position));
                    break;

                case GrayboxMarkerKind.CologneSpawnSpot:
                    builder.Add(new HouseSpotEntry(KeyOf(spec, typeof(CologneSpawnSpot)), HouseSpotKind.Cologne, roomId, string.Empty, spec.Position));
                    break;

                case GrayboxMarkerKind.ToolShedItem:
                    builder.Add(new HouseSpotEntry(KeyOf(spec, typeof(ToolShedItemPoint)), HouseSpotKind.ToolShedItem, HouseIndex.NoRoom, spec.ItemId, spec.Position));
                    break;

                case GrayboxMarkerKind.DigSpot:
                    // DigSpot has no room field either.
                    builder.Add(new HouseSpotEntry(KeyOf(spec, typeof(DigSpot)), HouseSpotKind.Dig, HouseIndex.NoRoom, string.Empty, spec.Position));
                    break;

                case GrayboxMarkerKind.HumanSpawn:
                    builder.AddHumanSpawn(KeyOf(spec, typeof(HumanSpawnMarker)), spec.Position);
                    break;

                case GrayboxMarkerKind.JinnSpawn:
                    builder.AddJinnSpawn(KeyOf(spec, typeof(JinnSpawnMarker)), spec.Position);
                    break;

                default:
                    throw new ArgumentOutOfRangeException("spec", "Unknown marker kind " + spec.Kind);
            }
        }

        private static string KeyOf(GrayboxMarkerSpec spec, Type markerType)
        {
            return HouseIndex.MakeNetKey(spec.Path, markerType.Name);
        }
    }
}
