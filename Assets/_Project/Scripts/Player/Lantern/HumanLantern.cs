using CinliMahzen.Core;
using CinliMahzen.Core.Events;
using CinliMahzen.Core.Net;
using UnityEngine;

namespace CinliMahzen.Player
{
    /// <summary>
    /// The human's fixed lantern (Oyun §5.1, Teknik §5.4, TODO 3.8). Sibling of <see cref="HumanController"/> on P_Human. Every machine: a spot
    /// light on the head, weak (<c>LanternWeakRange</c>) until the Blessed Oil (<see cref="BlessedOilCollectedEvt"/> for this pawn's owner) makes it
    /// strong (<c>LanternStrongRange</c>); a new round makes it weak again. <see cref="IsStrong"/> backs
    /// <see cref="IHumanStatusQuery.IsBlessedLanternActive"/> (the 2.5 m sabotage shield). While played: right click
    /// (<see cref="IHumanInput.LanternPulsePressed"/>, strong only) sends <c>ReqLanternPulse</c>, F (<see cref="IHumanInput.KickPressed"/>) sends
    /// <c>ReqKick</c>, both with the eye and view direction; <see cref="HumanAbilityAuthority"/> decides.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(HumanController))]
    public sealed class HumanLantern : MonoBehaviour
    {
        [Tooltip("The lantern's light. Empty = a spot light is created on the head at Awake.")]
        [SerializeField] private Light lanternLight;
        [SerializeField] private float spotAngle = 60f;
        [SerializeField] private Color color = new Color(1f, 0.85f, 0.55f);

        private HumanController _human;
        private bool _strong;

        public bool IsStrong => _strong;

        private void Awake()
        {
            _human = GetComponent<HumanController>();
            if (lanternLight == null)
            {
                Transform head = _human.Head != null ? _human.Head : transform;
                var go = new GameObject("LanternLight");
                go.transform.SetParent(head, false);
                lanternLight = go.AddComponent<Light>();
                lanternLight.type = LightType.Spot;
                lanternLight.spotAngle = spotAngle;
                lanternLight.color = color;
                lanternLight.shadows = LightShadows.Soft;
            }
            Apply();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<BlessedOilCollectedEvt>(OnOilCollected);
            EventBus.Subscribe<RoundStartedEvt>(OnRoundStarted);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<BlessedOilCollectedEvt>(OnOilCollected);
            EventBus.Unsubscribe<RoundStartedEvt>(OnRoundStarted);
        }

        private void OnOilCollected(BlessedOilCollectedEvt e)
        {
            if (e.Collector != _human.Owner)
                return;
            _strong = true;
            Apply();
        }

        private void OnRoundStarted(RoundStartedEvt e)
        {
            _strong = false;
            Apply();
        }

        private void Apply()
        {
            GameBalanceConfig config = GameServices.Config;
            if (lanternLight == null || config == null)
                return;
            lanternLight.range = _strong ? config.LanternStrongRange : config.LanternWeakRange;
        }

        private void Update()
        {
            if (!_human.IsLocallyControlled || !(_human.Input is IHumanInput input))
                return;
            INetBridge net = GameServices.Net;
            if (net == null)
                return;
            Transform eye = _human.Head != null ? _human.Head : transform;
            if (input.LanternPulsePressed && _strong)
                net.SendToAuthority(HumanActionMsgs.EncodeAimRequest(MsgCode.ReqLanternPulse, eye.position, eye.forward));
            if (input.KickPressed)
                net.SendToAuthority(HumanActionMsgs.EncodeAimRequest(MsgCode.ReqKick, eye.position, eye.forward));
        }
    }
}
