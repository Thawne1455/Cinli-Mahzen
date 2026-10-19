using CinliMahzen.Core;
using CinliMahzen.Core.Events;
using UnityEngine;

namespace CinliMahzen.Jinn
{
    /// <summary>
    /// Hold-E for a jinn in spirit form (TODO 2.6), the spirit counterpart of the human's Interactor. Sibling of <see cref="SpiritPawn"/>
    /// on P_Spirit; works only while the pawn is the local view and not inside an object. Each frame a raycast from the eyes on the
    /// Interactable layer finds an <see cref="ISpiritHoldTarget"/> (GetComponentInParent, only when the hit collider changes) within its
    /// <see cref="ISpiritHoldTarget.SpiritRange"/>; <see cref="InteractHoldTracker"/> counts <see cref="ISpiritInput.PrimaryHeld"/> and a
    /// completed hold calls <see cref="ISpiritHoldTarget.SendRequest"/>. The prompt goes out as <see cref="InteractPromptEvt"/> (only on change).
    /// <para>While it has a usable target, E belongs to it: <see cref="JinnPossessInput"/> does not start a possession on the same press.</para>
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpiritPawn))]
    public sealed class SpiritInteractor : MonoBehaviour
    {
        private const string InteractableLayerName = "Interactable";
        private const float MaxRayLength = 10f;

        private static readonly InteractPromptEvt HiddenPrompt = new InteractPromptEvt(string.Empty, 0f, false);

        private readonly InteractHoldTracker _tracker = new InteractHoldTracker();
        private SpiritPawn _pawn;
        private int _layerMask;
        private Collider _hitCollider;
        private ISpiritHoldTarget _target;
        private float _hitDistance;
        private InteractPromptEvt _lastPrompt = HiddenPrompt;
        private bool _usable;

        /// <summary>True while the local jinn looks at a target it can use this frame (E is then reserved for it).</summary>
        public bool HasUsableTarget => _usable;

        private void Awake()
        {
            _pawn = GetComponent<SpiritPawn>();
            _layerMask = LayerMask.GetMask(InteractableLayerName);
            if (_layerMask == 0)
                CMLog.Warn("Spirit", "Layer '" + InteractableLayerName + "' missing - spirits cannot hold-interact");
        }

        private void OnDisable()
        {
            _tracker.Reset();
            _hitCollider = null;
            _target = null;
            _usable = false;
            PublishIfChanged(HiddenPrompt);
        }

        private void Update()
        {
            if (!_pawn.IsLocalView || _pawn.IsInPossessedView || !(_pawn.Input is ISpiritInput input))
            {
                _tracker.Reset();
                _usable = false;
                PublishIfChanged(HiddenPrompt);
                return;
            }

            RefreshTarget();
            _usable = _target != null && _hitDistance <= _target.SpiritRange && _target.CanSpiritInteract(_pawn.Owner, _pawn.Role);
            // The tracker only compares identities: the target component's instance id, so sliding from one part of a puzzle to the next keeps the hold.
            NetId targetId = _usable && _target is Object targetObject ? new NetId(targetObject.GetInstanceID()) : NetId.Invalid;
            float holdTime = _usable ? _target.SpiritHoldTime : 0f;

            HoldStep step = _tracker.Update(targetId, holdTime, input.PrimaryHeld, Time.deltaTime);
            if (step == HoldStep.Completed)
                _target.SendRequest(_pawn.Owner);

            PublishIfChanged(_usable ? new InteractPromptEvt(_target.SpiritPromptKey, _tracker.Progress, true) : HiddenPrompt);
        }

        /// <summary>The GetComponentInParent lookup runs only when the hit collider changes.</summary>
        private void RefreshTarget()
        {
            Transform eyes = _pawn.Eyes;
            Collider hit = null;
            _hitDistance = float.MaxValue;
            if (Physics.Raycast(eyes.position, eyes.forward, out RaycastHit info, MaxRayLength, _layerMask, QueryTriggerInteraction.Collide))
            {
                hit = info.collider;
                _hitDistance = info.distance;
            }

            if (ReferenceEquals(hit, _hitCollider))
                return;
            _hitCollider = hit;
            _target = hit != null ? hit.GetComponentInParent<ISpiritHoldTarget>() : null;
        }

        private void PublishIfChanged(InteractPromptEvt next)
        {
            if (next.Visible == _lastPrompt.Visible && next.Progress == _lastPrompt.Progress && next.PromptKey == _lastPrompt.PromptKey)
                return;
            _lastPrompt = next;
            EventBus.Publish(next);
        }
    }
}
