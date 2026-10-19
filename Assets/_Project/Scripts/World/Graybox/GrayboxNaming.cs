namespace CinliMahzen.World.Graybox
{
    /// <summary>
    /// The hierarchy names of the graybox house prefab, in one place so the editor builder and the pure-data marker code agree
    /// (NetIds are derived from hierarchy paths, so the names are part of the data). Nothing at runtime looks anything up by
    /// these names (CLAUDE.md rule 11); they only name the objects the builder creates.
    ///
    /// Layout: P_House / Garden | Floor0 | Floor1 / Room_X (RoomMarker) / Markers / (markers); P_House / Markers / Stage_N.
    /// </summary>
    public static class GrayboxNaming
    {
        public const string GardenGroupName = "Garden";
        public const string FloorGroupPrefix = "Floor";
        public const string RoomPrefix = "Room_";
        public const string MarkersGroupName = "Markers";

        /// <summary>Rooms of this stage (the garden and the shed) hang under the garden group; every other room under its floor group.</summary>
        public const int GardenStage = 0;

        public const string PathSeparator = "/";

        public static string FloorGroupName(int floor)
        {
            return FloorGroupPrefix + floor;
        }

        public static string RoomGroupName(GrayboxRoom room)
        {
            return RoomPrefix + room.Name;
        }

        /// <summary>Name of the group that holds the room group: "Garden" or "Floor0"/"Floor1".</summary>
        public static string RoomParentName(GrayboxRoom room)
        {
            return room.Stage == GardenStage ? GardenGroupName : FloorGroupName(room.Floor);
        }

        /// <summary>Path of the room group relative to the house root, e.g. "Floor0/Room_Hall".</summary>
        public static string RoomGroupPath(GrayboxRoom room)
        {
            return RoomParentName(room) + PathSeparator + RoomGroupName(room);
        }

        /// <summary>Path of the group that holds a room's point markers, e.g. "Floor0/Room_Hall/Markers".</summary>
        public static string RoomMarkersPath(GrayboxRoom room)
        {
            return RoomGroupPath(room) + PathSeparator + MarkersGroupName;
        }
    }
}
