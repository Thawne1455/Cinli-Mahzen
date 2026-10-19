namespace CinliMahzen.World
{
    /// <summary>
    /// The numbers and ids of the house marker contract (Teknik 8). They are structural rules of the hand-built house, not
    /// balance values, so they live here (next to <see cref="HouseValidator"/>) and not in GameBalanceConfig.
    /// </summary>
    public static class HouseContract
    {
        /// <summary>Stage 0 is the garden (and the tool shed); the first house floor is stage 1.</summary>
        public const int GardenStage = 0;

        /// <summary>Stages from this index on hold puzzles (the garden stage has none, Oyun 4.4).</summary>
        public const int FirstPuzzleStage = 1;

        /// <summary>RoomMarker floors: -1 cellar, 0 garden/ground floor, 1 first floor, 2 attic.</summary>
        public const int MinFloor = -1;

        public const int MaxFloor = 2;
        public const int CellarFloor = -1;

        /// <summary>PuzzleSlot markers per puzzle stage (at least).</summary>
        public const int MinPuzzleSlotsPerStage = 3;

        /// <summary>ReferenceSlot markers in the whole house (at least).</summary>
        public const int MinReferenceSlots = 8;

        /// <summary>KeyHideSpot markers in the garden (at least).</summary>
        public const int MinKeyHideSpots = 5;

        /// <summary>CologneSpawnSpot markers inside the house (at least).</summary>
        public const int MinCologneSpawnSpots = 3;

        /// <summary>DigSpot markers in the cellar (at least). The cellar arrives with TODO 3.7, so fewer is only a warning.</summary>
        public const int MinDigSpots = 3;

        public const int HumanSpawnCount = 1;
        public const int JinnSpawnCount = 3;

        /// <summary>ToolShedItemPoint markers that carry the blessed oil (exactly).</summary>
        public const int BlessedOilPointCount = 1;

        /// <summary>ItemDefinition.Id the stage 0 gate (front door) requires.</summary>
        public const string FrontDoorItemId = "house_key";

        /// <summary>ItemDefinition.Id of the oil in the tool shed.</summary>
        public const string BlessedOilItemId = "blessed_oil";

        /// <summary>Loc key of a room name is this prefix plus <see cref="RoomMarker.RoomKey"/> ("room.hall").</summary>
        public const string RoomNameKeyPrefix = "room.";
    }
}
