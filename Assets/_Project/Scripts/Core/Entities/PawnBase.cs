using UnityEngine;

namespace CinliMahzen.Core
{
    /// <summary>
    /// Base of every pawn (Teknik §5.1): the human (CM.Player HumanController) and the spirit forms (CM.Jinn SpiritPawn / SpiritController).
    /// It lives in CM.Core because CM.Jinn cannot see CM.Player, yet both pawn kinds share ownership, role, input and
    /// <see cref="INetEntity"/> registration. The serialized netId / owner / role are the initial state of a hand-placed pawn;
    /// the PawnSpawner (TODO 1.3) overrides them at runtime through <see cref="SetNetId"/> and <see cref="SetOwner"/>.
    /// <para>
    /// Locality (hotseat, TODO 1.3): the pawn watches <c>IPlayerRegistry.LocalPlayerChanged</c> and calls <see cref="OnLocalityChanged"/>
    /// when it starts being / stops being the played pawn — once in <c>Start</c> (scene singletons such as the CameraRig have registered
    /// in their Awake by then), again on every hotseat switch and after <see cref="SetOwner"/>. A subclass that overrides Start must call
    /// <c>base.Start()</c>. Inside the hook a subclass uses <see cref="ApplyInput"/>, <see cref="AttachFpsCamera"/> and
    /// <see cref="AcquireCursorLock"/> / <see cref="ReleaseCursorLock"/>, so human and spirit bind identically.
    /// </para>
    /// </summary>
    public abstract class PawnBase : MonoBehaviour, INetEntity
    {
        // The pawn that currently holds the cursor lock. Hotseat switches notify the old and the new local pawn in no fixed order, so
        // the lock is owned: a pawn only releases what it holds, and the new local pawn's Acquire always wins.
        private static PawnBase s_cursorOwner;

        [Tooltip("Network id, pawn range 1..999 (Teknik 4.1). 0 = invalid, the pawn is not registered.")]
        [SerializeField] private int netId;
        [Tooltip("PlayerId value of the owning player (255 = none).")]
        [SerializeField] private byte ownerId = 255;
        [SerializeField] private Role role;

        private readonly LocalityTracker _locality = new LocalityTracker();
        private IPawnInput _input;
        private IPlayerRegistry _players;
        private bool _registered;
        private bool _started;

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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCursorOwner()
        {
            s_cursorOwner = null;
        }

        /// <summary>
        /// Assigns the id. A pawn that is already enabled registers (or re-registers) under the new id at once; one that is still
        /// inactive (the spawner configures it before activation) registers in OnEnable.
        /// </summary>
        public void SetNetId(NetId id)
        {
            bool wasRegistered = _registered;
            if (wasRegistered)
                Unregister();
            netId = id.Value;
            if (wasRegistered || isActiveAndEnabled)
                Register();
        }

        public void SetOwner(PlayerId owner, Role newRole)
        {
            ownerId = owner.Value;
            role = newRole;
            OnOwnerChanged();
            _locality.Reset();
            RefreshLocality();
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

        /// <summary>
        /// The pawn started / stopped being the locally played one. Runs once when the pawn starts, after every hotseat switch that changes
        /// the answer, and after <see cref="SetOwner"/> (even when the answer stays the same, because the role may have changed).
        /// </summary>
        protected virtual void OnLocalityChanged(bool isLocal)
        {
        }

        protected virtual void OnEnable()
        {
            Register();
            _players = GameServices.Players;
            if (_players != null)
                _players.LocalPlayerChanged += OnLocalPlayerChanged;
            RefreshLocality();
        }

        protected virtual void Start()
        {
            _started = true;
            RefreshLocality();
        }

        protected virtual void OnDisable()
        {
            if (_players != null)
            {
                _players.LocalPlayerChanged -= OnLocalPlayerChanged;
                _players = null;
            }
            _locality.Reset();
            ReleaseCursorLock();
            Unregister();
        }

        /// <summary>
        /// Gives the pawn the local player's live input (action map for <paramref name="inputRole"/>) when <paramref name="isLocal"/>,
        /// the idle input otherwise. Without an <see cref="ILocalInputProvider"/> the input is cleared (pawns treat null as idle).
        /// </summary>
        protected void ApplyInput(bool isLocal, Role inputRole)
        {
            ILocalInputProvider provider = GameServices.Get<ILocalInputProvider>();
            if (provider == null)
            {
                if (isLocal)
                    CMLog.Warn("Pawn", name + ": no ILocalInputProvider registered — idle input");
                SetInput(null);
                return;
            }
            SetInput(isLocal ? provider.AcquireLocal(inputRole) : provider.Idle);
        }

        /// <summary>
        /// Hands the local camera to <paramref name="anchor"/> (first-person view locked to it). The rig ignores a repeat of the same anchor.
        /// </summary>
        protected void AttachFpsCamera(Transform anchor)
        {
            if (anchor == null)
            {
                CMLog.Warn("Pawn", name + ": no camera anchor assigned — camera not attached");
                return;
            }
            ICameraRig rig = GameServices.Get<ICameraRig>();
            if (rig == null)
            {
                CMLog.Warn("Pawn", name + ": no ICameraRig registered (add the CameraRig: CinliMahzen/Setup/Add Camera Rig To Open Scene) — camera not attached");
                return;
            }
            rig.SetFps(anchor);
        }

        /// <summary>Locks and hides the cursor for the local pawn; this pawn becomes the lock's owner.</summary>
        protected void AcquireCursorLock()
        {
            s_cursorOwner = this;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        /// <summary>Frees the cursor, but only when this pawn is the one holding the lock (a newly local pawn may already have taken it).</summary>
        protected void ReleaseCursorLock()
        {
            if (s_cursorOwner != this)
                return;
            s_cursorOwner = null;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnLocalPlayerChanged(PlayerId localPlayer)
        {
            RefreshLocality();
        }

        // Nothing happens before Start (the first answer must come after all Awakes) or while disabled.
        private void RefreshLocality()
        {
            if (!_started || !isActiveAndEnabled)
                return;
            bool local = IsLocallyControlled;
            if (_locality.TryUpdate(local))
                OnLocalityChanged(local);
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
