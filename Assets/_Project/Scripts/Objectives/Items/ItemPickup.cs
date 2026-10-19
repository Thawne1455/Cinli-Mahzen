using CinliMahzen.Core;
using UnityEngine;

namespace CinliMahzen.Objectives
{
    /// <summary>
    /// A quest item lying on the ground (Teknik §5.4, TODO 2.7): a puzzle reward or an item the human dropped. Spawned on every machine by
    /// <c>RoundSetupApplier</c> from <c>ItemSpawned</c> / <c>ItemDropped</c> with a runtime NetId. The human looks at it and presses E → on the
    /// authority <see cref="ObjectiveFlowCore.PickupAuthority"/> moves it into the inventory (ItemPickedUp removes it everywhere).
    /// Needs a collider on the Interactable layer. Becoming a small possessable (PD_QuestItem, the jinns' fling) comes with TODO 1.8.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ItemPickup : MonoBehaviour, IInteractable, INetEntity
    {
        private const string PromptKeyValue = "interact.pickup";

        private ObjectiveFlowCore _flow;
        private NetId _netId;
        private string _itemId = string.Empty;

        public NetId NetId => _netId;

        /// <summary>ItemDefinition.Id of the item.</summary>
        public string ItemId => _itemId;

        public string PromptKey => PromptKeyValue;

        public float HoldTime => 0f;

        public float NoiseOnInteract => 0.1f;

        public void Bind(ObjectiveFlowCore flow, NetId id, string itemId)
        {
            _flow = flow;
            _netId = id;
            _itemId = itemId ?? string.Empty;
        }

        public bool CanInteract(PlayerId who, Role role)
        {
            return role == Role.Human && _flow != null && !_flow.Inventory.IsFull(who);
        }

        public bool ValidateAuthority(PlayerId who)
        {
            return _flow != null;
        }

        public void ExecuteAuthority(PlayerId who)
        {
            Debug.Assert(GameServices.Net.IsAuthority);
            _flow.PickupAuthority(who, _netId);
        }
    }
}
