using System.Collections.Generic;

namespace CinliMahzen.World.Graybox
{
    /// <summary>
    /// A room as a union of cell rectangles (L and U shapes are several rectangles). The garden is a room too
    /// (<see cref="IsOutdoor"/>): floor 0 is shared by the garden, the shed and the ground-floor rooms.
    /// </summary>
    public sealed class GrayboxRoom
    {
        /// <summary>Stable lower-case key ("hall"); used for lookups and as the future RoomMarker id.</summary>
        public string Id { get; }

        /// <summary>English identifier used in the hierarchy ("Room_Hall").</summary>
        public string Name { get; }

        /// <summary>One character for ASCII maps.</summary>
        public char Symbol { get; }

        /// <summary>0 = ground level (garden, shed, ground floor), 1 = first floor.</summary>
        public int Floor { get; }

        /// <summary>Stage the room belongs to (GDD 3.1): 0 garden and shed, 1 ground floor, 2 first floor.</summary>
        public int Stage { get; }

        public bool IsOutdoor { get; }
        public GrayboxFloorStyle FloorStyle { get; }
        public IReadOnlyList<GrayboxRect> Rects { get; }

        /// <summary>Localisation key for the room name ("room.hall"). The Loc entry arrives with the room markers (TODO 2.2).</summary>
        public string NameKey
        {
            get { return "room." + Id; }
        }

        public GrayboxRoom(string id, string name, char symbol, int floor, int stage, bool isOutdoor,
            GrayboxFloorStyle floorStyle, params GrayboxRect[] rects)
        {
            Id = id;
            Name = name;
            Symbol = symbol;
            Floor = floor;
            Stage = stage;
            IsOutdoor = isOutdoor;
            FloorStyle = floorStyle;
            Rects = rects ?? new GrayboxRect[0];
        }

        public bool Contains(GrayboxCell cell)
        {
            for (int i = 0; i < Rects.Count; i++)
            {
                if (Rects[i].Contains(cell))
                {
                    return true;
                }
            }
            return false;
        }

        public int CellCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < Rects.Count; i++)
                {
                    count += Rects[i].CellCount;
                }
                return count;
            }
        }

        /// <summary>Appends every cell of every rectangle (no de-duplication; overlapping rects repeat cells).</summary>
        public void AppendCellsTo(List<GrayboxCell> cells)
        {
            for (int i = 0; i < Rects.Count; i++)
            {
                Rects[i].AppendCellsTo(cells);
            }
        }

        public override string ToString()
        {
            return Id + " (floor " + Floor + ")";
        }
    }
}
