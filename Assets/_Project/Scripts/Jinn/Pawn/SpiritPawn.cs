using CinliMahzen.Core;
using UnityEngine;

namespace CinliMahzen.Jinn
{
    /// <summary>
    /// Jinn pawn (TODO 1.3): ownership/role from <see cref="PawnBase"/>, the Spirit input map and the first-person camera while it is
    /// the played pawn, and the role's placeholder look. It does not move itself: the flight (noclip, bounds), the reference focus and the
    /// Spirit Bump (TODO 1.6) live in <see cref="SpiritController"/>, a sibling component on the same GameObject that reads
    /// <see cref="PawnBase.Input"/> as <c>ISpiritInput</c> while <see cref="IsLocalView"/>. Kept separate (rather than a subclass) so the
    /// ownership / locality flow stays as it is and the flight can be switched off on its own while possessing (TODO 1.7).
    /// <para>
    /// Look: the prefab carries one placeholder visual per jinn kind (<c>P_EvilJinnVisual</c> on layer EvilJinnVisual, <c>P_GoodJinnVisual</c> on
    /// GoodJinnVisual, so camera culling treats them as before); the one matching <see cref="PawnBase.Role"/> is shown. The played pawn hides
    /// its own body — its camera sits inside it.
    /// </para>
    /// Input and camera binding is the shared <see cref="PawnBase.OnLocalityChanged"/> flow, so hotseat F1-F4 works like it does for the human.
    /// </summary>
    [DisallowMultipleComponent]
    public class SpiritPawn : PawnBase
    {
        [Header("References")]
        [Tooltip("Transform the local camera is locked to (first-person view). Falls back to the pawn's own transform.")]
        [SerializeField] private Transform eyes;
        [Tooltip("Placeholder look of an Evil Jinn (child, nested P_EvilJinnVisual).")]
        [SerializeField] private GameObject evilVisual;
        [Tooltip("Placeholder look of the Good Jinn (child, nested P_GoodJinnVisual).")]
        [SerializeField] private GameObject goodVisual;

        [Header("Cursor")]
        [Tooltip("Lock and hide the cursor while this pawn is controlled locally (Esc releases it in the editor).")]
        [SerializeField] private bool lockCursor = true;

        private bool _isLocalView;
        private Transform _possessedTarget;
        private float _possessedDistance;

        /// <summary>Camera anchor of the pawn.</summary>
        public Transform Eyes => eyes != null ? eyes : transform;

        /// <summary>True while this pawn is the one being played on this machine (set by the locality flow, same moment its input is bound).</summary>
        public bool IsLocalView => _isLocalView;

        /// <summary>True while the jinn is inside an object (its body is hidden; the local camera orbits the object).</summary>
        public bool IsInPossessedView => _possessedTarget != null;

        /// <summary>
        /// The jinn is now inside an object (TODO 1.7, called by PossessionCameraBinder): the body is hidden and, while this pawn is played,
        /// the camera orbits <paramref name="target"/> at <paramref name="distance"/> (&lt;= 0: the rig's default).
        /// </summary>
        public void EnterPossessedView(Transform target, float distance)
        {
            _possessedTarget = target;
            _possessedDistance = distance;
            if (_isLocalView)
                AttachOrbitCamera();
            RefreshVisual();
        }

        /// <summary>The jinn came out: the body shows again and the played pawn's camera goes back to <see cref="Eyes"/>.</summary>
        public void ExitPossessedView()
        {
            if (_possessedTarget == null)
                return;
            _possessedTarget = null;
            if (_isLocalView)
                AttachFpsCamera(Eyes);
            RefreshVisual();
        }

        private void AttachOrbitCamera()
        {
            ICameraRig rig = GameServices.Get<ICameraRig>();
            if (rig == null)
            {
                CMLog.Warn("Pawn", name + ": no ICameraRig registered — orbit camera not attached");
                return;
            }
            rig.SetOrbit(_possessedTarget, _possessedDistance);
        }

        protected override void OnOwnerChanged()
        {
            RefreshVisual();
        }

        /// <summary>
        /// Local pawn: live input for its role (Spirit map), the cursor lock and the camera on <see cref="Eyes"/>. Not local: idle input;
        /// the cursor is left to the pawn that became local (see HumanController.OnLocalityChanged).
        /// </summary>
        protected override void OnLocalityChanged(bool isLocal)
        {
            _isLocalView = isLocal;
            ApplyInput(isLocal, Role);
            if (isLocal)
            {
                if (lockCursor)
                    AcquireCursorLock();
                else
                    ReleaseCursorLock();
                if (_possessedTarget != null)
                    AttachOrbitCamera();
                else
                    AttachFpsCamera(Eyes);
            }
            else
            {
                ReleaseCursorLock();
            }
            RefreshVisual();
        }

        private void RefreshVisual()
        {
            bool shown = !_isLocalView && _possessedTarget == null;
            if (evilVisual != null)
                evilVisual.SetActive(shown && Role == Role.EvilJinn);
            if (goodVisual != null)
                goodVisual.SetActive(shown && Role == Role.GoodJinn);
        }
    }
}
