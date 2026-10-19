using System.Collections.Generic;
using UnityEngine;

namespace CinliMahzen.World
{
    /// <summary>Plain-data copy of one <see cref="RoomMarker"/>: id, key, floor and the world-space boxes that make up the room.</summary>
    public sealed class HouseRoomEntry
    {
        public int Id { get; }
        public string Key { get; }
        public int Floor { get; }
        public bool IsOutdoor { get; }

        /// <summary>World-space axis-aligned boxes (one per rectangle of an L-shaped room).</summary>
        public IReadOnlyList<Bounds> Boxes { get; }

        /// <summary>Smallest box that encloses every box of the room (zero-size at the origin when the room has no box).</summary>
        public Bounds Union { get; }

        public HouseRoomEntry(int id, string key, int floor, bool isOutdoor, IEnumerable<Bounds> boxes)
        {
            Id = id;
            Key = key ?? string.Empty;
            Floor = floor;
            IsOutdoor = isOutdoor;

            var copy = new List<Bounds>();
            if (boxes != null)
            {
                copy.AddRange(boxes);
            }
            Boxes = copy;

            Bounds union = new Bounds();
            for (int i = 0; i < copy.Count; i++)
            {
                if (i == 0)
                {
                    union = copy[i];
                }
                else
                {
                    union.Encapsulate(copy[i]);
                }
            }
            Union = union;
        }

        /// <summary>
        /// True when the point is inside one of the boxes; <paramref name="volume"/> is the volume of the smallest such box.
        /// A box is half-open (min inclusive, max exclusive on every axis), so neighbouring boxes never both contain a point.
        /// </summary>
        public bool TryGetContainingVolume(Vector3 point, out float volume)
        {
            bool found = false;
            volume = 0f;
            for (int i = 0; i < Boxes.Count; i++)
            {
                if (!BoxContains(Boxes[i], point))
                {
                    continue;
                }
                Vector3 size = Boxes[i].size;
                float candidate = size.x * size.y * size.z;
                if (!found || candidate < volume)
                {
                    volume = candidate;
                    found = true;
                }
            }
            return found;
        }

        /// <summary>Half-open containment: min inclusive, max exclusive.</summary>
        public static bool BoxContains(Bounds box, Vector3 point)
        {
            Vector3 min = box.min;
            Vector3 max = box.max;
            return point.x >= min.x && point.x < max.x
                && point.y >= min.y && point.y < max.y
                && point.z >= min.z && point.z < max.z;
        }

        public override string ToString()
        {
            return Key + " (#" + Id + ", floor " + Floor + ")";
        }
    }
}
