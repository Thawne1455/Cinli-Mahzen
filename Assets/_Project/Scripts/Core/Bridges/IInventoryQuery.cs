namespace CinliMahzen.Core
{
    /// <summary>
    /// Read access to the human's carried items (Teknik §4.8). Implemented by Player's Inventory (TODO 2.7);
    /// consumed by Objectives (a puzzle that needs an item, e.g. "paint", "fuse_key").
    /// </summary>
    public interface IInventoryQuery
    {
        /// <summary>True when <paramref name="player"/> carries the item whose ItemDefinition.Id is <paramref name="itemId"/>. A null or empty id is never carried.</summary>
        bool HasItem(PlayerId player, string itemId);
    }
}
