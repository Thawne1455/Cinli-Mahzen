using System.Collections.Generic;

namespace CinliMahzen.World
{
    /// <summary>Plain-data copy of one <see cref="StageZone"/>: the stage index and the ids of its rooms.</summary>
    public sealed class HouseStageEntry
    {
        public int Index { get; }
        public IReadOnlyList<int> RoomIds { get; }

        public HouseStageEntry(int index, IEnumerable<int> roomIds)
        {
            Index = index;
            var copy = new List<int>();
            if (roomIds != null)
            {
                copy.AddRange(roomIds);
            }
            RoomIds = copy;
        }

        public bool Contains(int roomId)
        {
            for (int i = 0; i < RoomIds.Count; i++)
            {
                if (RoomIds[i] == roomId)
                {
                    return true;
                }
            }
            return false;
        }

        public override string ToString()
        {
            return "Stage " + Index + " (" + RoomIds.Count + " rooms)";
        }
    }
}
