using CinliMahzen.Core;
using CinliMahzen.Core.Events;
using CinliMahzen.Core.Net;
using CinliMahzen.Possession;
using UnityEngine;

namespace CinliMahzen.Jinn
{
    /// <summary>
    /// The played good jinn's Q and R (Oyun §5.2, §11; TODO 3.5). Sibling of <see cref="SpiritPawn"/> on P_Spirit; only while the pawn is the
    /// local good jinn in spirit form.
    /// <list type="bullet">
    /// <item>Q (<see cref="ISpiritInput.PingPressed"/>): a ping where the eyes look (first surface within <see cref="pingRayLength"/>, or that far
    /// ahead) → <c>ReqPing{point}</c>.</item>
    /// <item>R held (<see cref="ISpiritInput.ExorciseHeld"/>) on an object a jinn is inside, within <c>ExorciseRange</c>: after
    /// <c>ExorciseHoldTime</c> → <c>ReqExorcise{obj}</c>. Prompt "interact.exorcise" while aiming at one.</item>
    /// </list>
    /// The authority checks role, cooldowns, range and that an evil jinn is really inside.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpiritPawn))]
    public sealed class GoodJinnAbilityInput : MonoBehaviour
    {
        private const string ExorcisePrompt = "interact.exorcise";
        private static readonly InteractPromptEvt HiddenPrompt = new InteractPromptEvt(string.Empty, 0f, false);

        [Tooltip("How far (m) Q looks for a surface to ping.")]
        [SerializeField] private float pingRayLength = 30f;
        [Tooltip("Largest angle (degrees) between the view and an object to exorcise.")]
        [Range(1f, 60f)] [SerializeField] private float exorciseAngle = 25f;

        private readonly InteractHoldTracker _exorciseHold = new InteractHoldTracker();
        private SpiritPawn _pawn;
        private bool _promptShown;
        private float _promptProgress = -1f;

        private void Awake()
        {
            _pawn = GetComponent<SpiritPawn>();
        }

        private void OnDisable()
        {
            _exorciseHold.Reset();
            HidePrompt();
        }

        private void Update()
        {
            if (!_pawn.IsLocalView || _pawn.IsInPossessedView || _pawn.Role != Role.GoodJinn || !(_pawn.Input is ISpiritInput input))
            {
                _exorciseHold.Reset();
                HidePrompt();
                return;
            }
            INetBridge net = GameServices.Net;
            GameBalanceConfig config = GameServices.Config;
            if (net == null || config == null)
                return;

            Transform eyes = _pawn.Eyes;
            if (input.PingPressed)
                net.SendToAuthority(PingMsg.EncodeRequest(PingPoint(eyes)));

            Possessable target = ExorciseTarget(eyes, config.ExorciseRange);
            NetId targetId = target != null ? target.NetId : NetId.Invalid;
            HoldStep step = _exorciseHold.Update(targetId, config.ExorciseHoldTime, input.ExorciseHeld, Time.deltaTime);
            if (step == HoldStep.Completed && target != null)
                net.SendToAuthority(ReqExorciseMsg.Encode(target.NetId));

            if (target != null)
            {
                // Only on change (like the Interactor).
                if (!_promptShown || _exorciseHold.Progress != _promptProgress)
                {
                    _promptProgress = _exorciseHold.Progress;
                    EventBus.Publish(new InteractPromptEvt(ExorcisePrompt, _promptProgress, true));
                }
                _promptShown = true;
            }
            else
            {
                HidePrompt();
            }
        }

        private Vector3 PingPoint(Transform eyes)
        {
            // Spirits have no collider, so the ray cannot hit the jinn itself.
            if (Physics.Raycast(eyes.position, eyes.forward, out RaycastHit hit, pingRayLength, ~0, QueryTriggerInteraction.Ignore))
                return hit.point;
            return eyes.position + eyes.forward * Mathf.Min(10f, pingRayLength);
        }

        /// <summary>An object someone else is inside, in front of the eyes within range (the authority checks it is an evil jinn).</summary>
        private Possessable ExorciseTarget(Transform eyes, float range)
        {
            PossessionSystem system = PossessionSystem.Instance;
            if (system == null || system.Mirror == null)
                return null;
            Possessable p = Possessable.FindLookTarget(eyes.position, eyes.forward, range, exorciseAngle, false);
            if (p == null || !system.Mirror.IsPossessed(p.NetId) || system.Mirror.PossessorOf(p.NetId) == _pawn.Owner)
                return null;
            return p;
        }

        private void HidePrompt()
        {
            if (!_promptShown)
                return;
            _promptShown = false;
            _promptProgress = -1f;
            EventBus.Publish(HiddenPrompt);
        }
    }
}
