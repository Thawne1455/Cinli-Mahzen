using UnityEngine;

namespace CinliMahzen.Core
{
    /// <summary>Quest item definition (Oyun §6, Teknik §5.4). Assets: ScriptableObjects/Items/ID_*.asset.</summary>
    [CreateAssetMenu(menuName = "CinliMahzen/Items/Item Definition", fileName = "ID_New")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string nameKey;
        [SerializeField] private Sprite icon;
        [SerializeField] private ItemKind kind;
        [SerializeField] private GameObject prefab;

        /// <summary>Stable id used on the wire, in PuzzleDefinition.RequiredItemId and GateLock, e.g. "fuse_key".</summary>
        public string Id => id;
        /// <summary>Loc key, e.g. "item.fuse_key".</summary>
        public string NameKey => nameKey;
        public Sprite Icon => icon;
        public ItemKind Kind => kind;
        /// <summary>Ground/pickup prefab P_Item_* (null until art exists). On the ground it is a small possessable (PD_QuestItem).</summary>
        public GameObject Prefab => prefab;
    }
}
