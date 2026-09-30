using UnityEngine;

namespace CinliMahzen.Core
{
    /// <summary>Inventory item definition (Teknik §4.9). Assets: ScriptableObjects/Items/ID_*.asset.</summary>
    [CreateAssetMenu(menuName = "CinliMahzen/Items/Item Definition", fileName = "ID_New")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string nameKey;
        [SerializeField] private Sprite icon;
        [SerializeField] private ItemUseType useType;
        [SerializeField] private GameObject prefab;

        /// <summary>Stable id used on the wire and in loot tables, e.g. "salt".</summary>
        public string Id => id;
        /// <summary>Loc key, e.g. "item.salt".</summary>
        public string NameKey => nameKey;
        public Sprite Icon => icon;
        public ItemUseType UseType => useType;
        /// <summary>Pickup / deployed prefab (null until art exists).</summary>
        public GameObject Prefab => prefab;
    }
}
