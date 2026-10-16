using CinliMahzen.Core;
using CinliMahzen.Core.Events;
using CinliMahzen.Core.Net;
using UnityEngine;

namespace CinliMahzen.Player
{
    /// <summary>
    /// Local hold-to-interact (Teknik §4.5). Runs only on the locally controlled human. A raycast from the head against
    /// the Interactable layer finds the target, <see cref="InteractHoldTracker"/> counts the hold, and a completed hold sends
    /// ReqInteract to the authority (<see cref="InteractionAuthority"/>). Nothing game-relevant changes here; the prompt
    /// goes out as <see cref="InteractPromptEvt"/> whenever its value changes.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(HumanController))]
    public sealed class Interactor : MonoBehaviour
    {
        private const string InteractableLayerName = "Interactable";

        private static readonly InteractPromptEvt HiddenPrompt = new InteractPromptEvt(string.Empty, 0f, false);

        private readonly InteractHoldTracker _tracker = new InteractHoldTracker();

        private HumanController _human;
        private Transform _eye;
        private GameBalanceConfig _config;
        private INetBridge _net;
        private int _layerMask;
        private Collider _hitCollider;
        private IInteractable _target;
        private InteractPromptEvt _lastPrompt = HiddenPrompt;

        private void Awake()
        {
            _human = GetComponent<HumanController>();
            _eye = _human.Head != null ? _human.Head : transform;
            _layerMask = LayerMask.GetMask(InteractableLayerName);
            if (_layerMask == 0)
                CMLog.Error("Interact", "Layer '" + InteractableLayerName + "' missing (Teknik section 2) - interactor is inert");
        }

        private void OnEnable()
        {
            _net = GameServices.Net;
            _config = GameServices.Config;
            if (_config == null)
                CMLog.Error("Interact", name + ": GameServices.Config missing - interactor is inert");
        }

        private void OnDisable()
        {
            _tracker.Reset();
            _hitCollider = null;
            _target = null;
            PublishIfChanged(HiddenPrompt);
        }

        private void Update()
        {
            if (!_human.IsLocallyControlled || _config == null || _net == null)
            {
                _tracker.Reset();
                PublishIfChanged(HiddenPrompt);
                return;
            }

            RefreshTarget();
            bool usable = _target != null && _target.CanInteract(_human.Owner, _human.Role);
            NetId targetId = usable ? _target.NetId : NetId.Invalid;
            float holdTime = usable ? _target.HoldTime : 0f;

            IHumanInput input = _human.Input as IHumanInput;
            bool held = input != null && input.InteractHeld;

            // Holding does not call HumanMotor.EnterInteracting yet: the movement lock is wired in with the search/dig tasks.
            HoldStep step = _tracker.Update(targetId, holdTime, held, Time.deltaTime);
            if (step == HoldStep.Completed)
                _net.SendToAuthority(ReqInteractMsg.Encode(targetId));

            PublishIfChanged(usable ? new InteractPromptEvt(_target.PromptKey, _tracker.Progress, true) : HiddenPrompt);
        }

        /// <summary>The GetComponent lookup runs only when the hit collider changes, never per frame.</summary>
        private void RefreshTarget()
        {
            Collider hit = null;
            if (Physics.Raycast(_eye.position, _eye.forward, out RaycastHit info, _config.InteractRange, _layerMask, QueryTriggerInteraction.Collide))
                hit = info.collider;

            // ReferenceEquals: Unity's == treats a destroyed collider as null, which would keep a stale target.
            if (ReferenceEquals(hit, _hitCollider))
                return;
            _hitCollider = hit;
            _target = hit != null ? hit.GetComponentInParent<IInteractable>() : null;
        }

        private void PublishIfChanged(InteractPromptEvt next)
        {
            if (next.Visible == _lastPrompt.Visible
                && next.Progress == _lastPrompt.Progress
                && next.PromptKey == _lastPrompt.PromptKey)
                return;

            _lastPrompt = next;
            EventBus.Publish(next);
        }
    }
}
