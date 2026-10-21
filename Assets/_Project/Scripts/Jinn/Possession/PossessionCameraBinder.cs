using CinliMahzen.Core;
using CinliMahzen.Core.Events;
using CinliMahzen.Possession;
using UnityEngine;

namespace CinliMahzen.Jinn
{
    /// <summary>
    /// Ties a spirit pawn to the object its jinn is inside (TODO 1.7). Sibling of <see cref="SpiritPawn"/> on P_Spirit. On every machine it
    /// follows <see cref="PossessionChangedEvt"/> for the pawn's owner: once the jinn is inside (Lurking / Charging / Recovering) the body is
    /// hidden, the played pawn's camera orbits the object (<see cref="PossessableDefinition.CameraDistance"/>) and the spirit sits on the
    /// object each frame (so distance checks and the Spirit Bump see it there); when it comes out (Free / Spent) everything goes back.
    /// The input map follows by itself (LocalInputSource listens to the same event).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpiritPawn))]
    public sealed class PossessionCameraBinder : MonoBehaviour
    {
        private SpiritPawn _pawn;
        private Transform _follow;

        /// <summary>The object the jinn is inside, or null.</summary>
        public Transform Following => _follow;

        private void Awake()
        {
            _pawn = GetComponent<SpiritPawn>();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<PossessionChangedEvt>(OnPossessionChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PossessionChangedEvt>(OnPossessionChanged);
            Release();
        }

        private void OnPossessionChanged(PossessionChangedEvt e)
        {
            if (e.Player != _pawn.Owner)
                return;

            if (PossessionMirror.IsInside(e.State))
            {
                if (_follow != null)
                    return;
                Possessable target = Possessable.Find(e.Obj);
                if (target == null)
                {
                    CMLog.Warn("Possession", name + ": " + e.Obj + " is not a known possessable - camera stays");
                    return;
                }
                _follow = target.transform;
                float distance = target.Definition != null ? target.Definition.CameraDistance : 0f;
                _pawn.EnterPossessedView(target.CameraTarget, distance);
            }
            else if (e.State != PossessionPhase.Entering)
            {
                Release();
            }
        }

        private void Release()
        {
            if (_follow == null)
                return;
            _follow = null;
            _pawn.ExitPossessedView();
        }

        private void LateUpdate()
        {
            if (_follow == null)
                return;
            // The object may have been destroyed (round reset, broken) before its PossessEnded arrived.
            if (!_follow)
            {
                Release();
                return;
            }
            transform.position = _follow.position;
        }
    }
}
