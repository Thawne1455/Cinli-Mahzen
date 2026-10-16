using UnityEngine;

namespace CinliMahzen.Core
{
    /// <summary>
    /// Base of every pawn (Teknik §5.1): the human (CM.Player HumanController) and the spirit forms (CM.Jinn SpiritController).
    /// It lives in CM.Core because CM.Jinn cannot see CM.Player, yet both pawn kinds share ownership, role, input and
    /// <see cref="INetEntity"/> registration. The serialized netId / owner / role are the initial state of a hand-placed pawn
    /// (Sandbox); the PawnSpawner (TODO 1.3) overrides them at runtime through <see cref="SetNetId"/> and <see cref="SetOwner"/>.
    /// </summary>
    public abstract class PawnBase : MonoBehaviour, INetEntity
    {
        [Tooltip("Network id, pawn range 1..999 (Teknik 4.1). 0 = invalid, the pawn is not registered.")]
        [SerializeField] private int netId;
        [Tooltip("PlayerId value of the owning player (255 = none).")]
        [SerializeField] private byte ownerId = 255;
        [SerializeField] private Role role;

        private IPawnInput _input;
        private bool _registered;

        public NetId NetId => new NetId(netId);

        public PlayerId Owner => new PlayerId(ownerId);

        public Role Role => role;

        /// <summary>
        /// Current input source, or null when none is assigned (CM.Core has no NullInput; the subclass treats null as idle).
        /// </summary>
        public IPawnInput Input => _input;

        /// <summary>True when this machine's local player owns the pawn (offline hotseat: the currently selected player).</summary>
        public bool IsLocallyControlled
        {
            get
            {
                Net.INetBridge net = GameServices.Net;
                return net != null && Owner == net.LocalPlayer;
            }
        }

        public void SetNetId(NetId id)
        {
            bool wasRegistered = _registered;
            if (wasRegistered)
                Unregister();
            netId = id.Value;
            if (wasRegistered)
                Register();
        }

        public void SetOwner(PlayerId owner, Role newRole)
        {
            ownerId = owner.Value;
            role = newRole;
            OnOwnerChanged();
        }

        public void SetInput(IPawnInput input)
        {
            _input = input;
            OnInputChanged();
        }

        /// <summary>Called after <see cref="SetOwner"/>.</summary>
        protected virtual void OnOwnerChanged()
        {
        }

        /// <summary>Called after <see cref="SetInput"/>.</summary>
        protected virtual void OnInputChanged()
        {
        }

        protected virtual void OnEnable()
        {
            Register();
        }

        protected virtual void OnDisable()
        {
            Unregister();
        }

        private void Register()
        {
            if (_registered)
                return;
            IEntityRegistry registry = GameServices.Entities;
            if (registry == null)
                return;
            if (!NetId.IsValid)
            {
                CMLog.Warn("Entities", name + " has no NetId (pawn range 1..999), not registered");
                return;
            }
            registry.Register(this);
            _registered = true;
        }

        private void Unregister()
        {
            if (!_registered)
                return;
            _registered = false;
            GameServices.Entities?.Unregister(this);
        }
    }
}
