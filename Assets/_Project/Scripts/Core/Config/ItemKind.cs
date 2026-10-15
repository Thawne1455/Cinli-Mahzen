namespace CinliMahzen.Core
{
    /// <summary>What an item does when picked up (Oyun §6). Serialized in ID_*.asset — do not reorder.</summary>
    public enum ItemKind : byte
    {
        /// <summary>Takes one of the ItemSlotCount inventory slots (house key, fuse key, paint, shovel, cellar key).</summary>
        QuestItem = 0,
        /// <summary>Consumed on pickup, switches the fixed lantern to strong mode (Blessed Oil). Takes no slot.</summary>
        LanternUpgrade = 1,
    }
}
