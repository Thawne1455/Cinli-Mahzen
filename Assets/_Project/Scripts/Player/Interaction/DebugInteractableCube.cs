using CinliMahzen.Core;
using UnityEngine;

namespace CinliMahzen.Player
{
    /// <summary>
    /// Test target for hold-to-interact (TODO 1.4). A cube on the Interactable layer whose ExecuteAuthority only logs.
    /// Placed with CinliMahzen/Setup/Place Test Interactable In Open Scene. Remove when real interactables arrive.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DebugInteractableCube : MonoBehaviour, IInteractable, INetEntity
    {
        private const string PromptKeyValue = "interact.hold";

        [Tooltip("Network id, level range 1000..59999. Test value 1001.")]
        [SerializeField] private int netId;
        [Tooltip("Seconds the interact button must be held. 0 = instant.")]
        [Min(0f)] [SerializeField] private float holdTime = 1f;

        public NetId NetId => new NetId(netId);

        public string PromptKey => PromptKeyValue;

        public float HoldTime => holdTime;

        public float NoiseOnInteract => 0f;

        public bool CanInteract(PlayerId who, Role role)
        {
            return true;
        }

        public bool ValidateAuthority(PlayerId who)
        {
            return true;
        }

        public void ExecuteAuthority(PlayerId who)
        {
            Debug.Assert(GameServices.Net.IsAuthority);
            CMLog.Info("Interact", "ExecuteAuthority " + NetId + " by " + who);
        }

        private void OnEnable()
        {
            GameServices.Entities?.Register(this);
        }

        private void OnDisable()
        {
            GameServices.Entities?.Unregister(this);
        }
    }
}
