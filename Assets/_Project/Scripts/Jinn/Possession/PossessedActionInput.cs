using CinliMahzen.Core;
using CinliMahzen.Core.Net;
using CinliMahzen.Possession;
using UnityEngine;

namespace CinliMahzen.Jinn
{
    /// <summary>
    /// The played jinn's controls inside an object (Oyun §11 "Eşyanın İçinde", TODO 1.8). Sibling of <see cref="SpiritPawn"/> on P_Spirit;
    /// works only while the pawn is the local view and inside an object (<see cref="SpiritPawn.IsInPossessedView"/>). Left / right click
    /// (<see cref="IPossessedInput.Action1Pressed"/> / <see cref="IPossessedInput.Action2Pressed"/>) send <c>ReqAction{index, aim}</c> with the orbit
    /// camera's forward as the aim; WASD on a movable object (chair, stool) sends a hop (<c>PossessedMove{direction}</c>, camera-relative) at most
    /// every <c>HopInterval</c>. The authority decides everything (energy, cooldown, telegraph).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpiritPawn))]
    public sealed class PossessedActionInput : MonoBehaviour
    {
        private SpiritPawn _pawn;
        private float _hopWait;

        private void Awake()
        {
            _pawn = GetComponent<SpiritPawn>();
        }

        private void Update()
        {
            if (!_pawn.IsLocalView || !_pawn.IsInPossessedView || !(_pawn.Input is IPossessedInput input))
                return;
            INetBridge net = GameServices.Net;
            if (net == null)
                return;

            Transform view = AimTransform();
            Vector3 aim = view != null ? view.forward : transform.forward;
            if (input.Action1Pressed)
                net.SendToAuthority(ReqActionMsg.Encode(PossessableDefinition.PrimaryIndex, aim));
            else if (input.Action2Pressed)
                net.SendToAuthority(ReqActionMsg.Encode(PossessableDefinition.SecondaryIndex, aim));

            if (_hopWait > 0f)
                _hopWait -= Time.deltaTime;
            Vector2 move = input.Move;
            if (move.sqrMagnitude < 0.01f || _hopWait > 0f)
                return;
            PossessionSystem system = PossessionSystem.Instance;
            if (system == null || system.Mirror == null || !system.Mirror.TryGetObjectOf(_pawn.Owner, out NetId obj))
                return;
            Possessable target = Possessable.Find(obj);
            if (target == null || target.Definition == null || !target.Definition.CanMove)
                return;

            Vector3 forward = view != null ? Vector3.ProjectOnPlane(view.forward, Vector3.up).normalized : Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            net.SendToAuthority(ReqPossessedMoveMsg.Encode(forward * move.y + right * move.x));
            // Local throttle only (cosmetic pacing of requests); the authority enforces HopInterval itself.
            _hopWait = target.Definition.HopInterval;
        }

        private static Transform AimTransform()
        {
            ICameraRig rig = GameServices.Get<ICameraRig>();
            return rig != null && rig.Camera != null ? rig.Camera.transform : null;
        }
    }
}
