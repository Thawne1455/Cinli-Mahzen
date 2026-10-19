using UnityEngine;

namespace CinliMahzen.World
{
    /// <summary>Where a guaranteed item lies in the tool shed (Teknik 8): exactly one point carries the blessed oil (Oyun 4.4). The item is placed here at round setup.</summary>
    public sealed class ToolShedItemPoint : NetHouseMarker
    {
        [Tooltip("ItemDefinition.Id of the item placed here, e.g. \"blessed_oil\".")]
        [SerializeField] private string itemId = string.Empty;

        public string ItemId
        {
            get { return itemId ?? string.Empty; }
        }

        /// <summary>Used by the graybox builder and tests.</summary>
        public void Configure(string newItemId)
        {
            itemId = newItemId ?? string.Empty;
        }

        protected override Color GizmoColor
        {
            get { return new Color(0.2f, 0.8f, 0.7f); }
        }
    }
}
