namespace CinliMahzen.Core
{
    /// <summary>How an inventory item is used. Serialized — do not reorder.</summary>
    public enum ItemUseType : byte
    {
        /// <summary>Works while carried (Nazar).</summary>
        Passive = 0,
        /// <summary>Placed into the world on use (Salt).</summary>
        Deploy = 1,
    }
}
