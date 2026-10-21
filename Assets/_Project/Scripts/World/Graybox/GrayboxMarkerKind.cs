namespace CinliMahzen.World.Graybox
{
    /// <summary>Which marker component a <see cref="GrayboxMarkerSpec"/> becomes. Int values are stable; append new kinds at the end.</summary>
    public enum GrayboxMarkerKind
    {
        /// <summary>RoomMarker on the room group (one trigger box per room rectangle).</summary>
        Room = 0,

        /// <summary>StageZone under the house-level Markers group.</summary>
        StageZone = 1,

        PuzzleSlot = 2,
        ReferenceSlot = 3,

        /// <summary>StageGate (+ GateLock when the spec has an item id) pointing at the door piece of a layout door.</summary>
        StageGate = 4,

        Lamp = 5,
        LightSwitch = 6,
        KeyHideSpot = 7,
        CologneSpawnSpot = 8,
        HumanSpawn = 9,
        JinnSpawn = 10,
        ToolShedItem = 11,

        /// <summary>TODO 4.1: DigSpot in the cellar.</summary>
        DigSpot = 12
    }
}
