using CinliMahzen.Core;
using UnityEngine;

namespace CinliMahzen.World
{
    /// <summary>Where a guaranteed item lies in the tool shed (Teknik 8): exactly one point carries the blessed oil (Oyun 4.4). The item is placed here at round setup.</summary>
    public sealed class ToolShedItemPoint : NetHouseMarker, IInteractable
    {
        // TODO 3.6: the human takes the item (the blessed oil) once per round.
        public string PromptKey => "interact.pickup";

        public float HoldTime => 0f;

        public float NoiseOnInteract => 0.1f;

        public bool CanInteract(PlayerId who, Role role)
        {
            return role == Role.Human && GameServices.TryGet(out IObjectiveInteractions objectives) && objectives.IsToolShedItemAvailable(NetId);
        }

        public bool ValidateAuthority(PlayerId who)
        {
            return GameServices.TryGet(out IObjectiveInteractions _);
        }

        public void ExecuteAuthority(PlayerId who)
        {
            Debug.Assert(GameServices.Net.IsAuthority);
            if (GameServices.TryGet(out IObjectiveInteractions objectives))
                objectives.CollectToolShedItemAuthority(who, NetId);
        }

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
