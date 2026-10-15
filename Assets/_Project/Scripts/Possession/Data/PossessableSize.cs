namespace CinliMahzen.Possession
{
    /// <summary>
    /// Size / family class of a possessable (Tech §6.3): picks the hit-check proxy, camera distance preset
    /// and entering feel. Serialized as int — append only.
    /// </summary>
    public enum PossessableSize : byte
    {
        /// <summary>Bottle, candle, plate — thrown, breaks.</summary>
        Small = 0,
        /// <summary>Mother's Slipper (one per floor).</summary>
        Slipper = 1,
        /// <summary>Chair, stool, barrel, chest, table.</summary>
        Large = 2,
        Shelf = 3,
        /// <summary>Candle / chandelier / wall lamp that can be burst.</summary>
        Lamp = 4,
        /// <summary>Old radio / gramophone.</summary>
        Radio = 5,
        /// <summary>Lemon cologne.</summary>
        Cologne = 6,
        /// <summary>A dropped quest item.</summary>
        QuestItem = 7,
    }
}
