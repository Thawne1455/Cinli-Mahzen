namespace CinliMahzen.World.Graybox
{
    /// <summary>What kind of opening a <see cref="GrayboxDoor"/> is. Int values are stable.</summary>
    public enum GrayboxDoorKind
    {
        /// <summary>Plain doorway between rooms, never blocked by the layout itself.</summary>
        Open = 0,

        /// <summary>Ordinary interior door (graybox: frame only, no leaf yet).</summary>
        Door = 1,

        /// <summary>Stage gate (GDD 3.1): locked until the stage puzzles are solved. Graybox: leaf drawn, collider off.</summary>
        StageGate = 2,

        /// <summary>The single front door between the garden and the house (needs the house key). Graybox: leaf drawn, collider off.</summary>
        FrontDoor = 3,

        /// <summary>
        /// TODO 4.1: the cellar hatch on the arrival border of the cellar stairs (one side is the stair shaft). A StageGate locked with the
        /// cellar key; its stage is the one after the last active puzzle stage (StageGate.IsCellarHatch). Graybox: leaf drawn, collider off.
        /// </summary>
        CellarHatch = 4
    }
}
