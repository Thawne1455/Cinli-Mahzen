using CinliMahzen.Core;
using CinliMahzen.Core.Net;
using CinliMahzen.Possession;
using UnityEngine;

namespace CinliMahzen.Jinn
{
    /// <summary>
    /// The played jinn's possession requests (TODO 1.7). Sibling of <see cref="SpiritPawn"/> on P_Spirit; reads the pawn's input only while
    /// it is the local view. In spirit form E (evil, <see cref="ISpiritInput.PrimaryPressed"/>) or Shift+E (good,
    /// <see cref="ISpiritInput.PossessAsGoodPressed"/>) asks to enter the object it looks at (<see cref="Possessable.FindLookTarget"/>, within
    /// <c>PossessRange</c>); inside an object Space / E (<see cref="IPossessedInput.ExitPressed"/>) asks to leave. Everything is decided by
    /// the authority; nothing changes locally until its broadcast arrives.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpiritPawn))]
    public sealed class JinnPossessInput : MonoBehaviour
    {
        [Tooltip("Largest angle (degrees) between the view and an object for it to count as looked at.")]
        [Range(1f, 60f)] [SerializeField] private float lookAngle = 20f;

        private SpiritPawn _pawn;
        private SpiritInteractor _interactor;

        private void Awake()
        {
            _pawn = GetComponent<SpiritPawn>();
            _interactor = GetComponent<SpiritInteractor>();
        }

        private void Update()
        {
            if (!_pawn.IsLocalView)
                return;
            INetBridge net = GameServices.Net;
            PossessionSystem system = PossessionSystem.Instance;
            if (net == null || system == null || system.Mirror == null)
                return;

            bool inSomething = system.Mirror.TryGetObjectOf(_pawn.Owner, out NetId _);
            if (_pawn.IsInPossessedView)
            {
                if (_pawn.Input is IPossessedInput possessed && possessed.ExitPressed)
                    net.SendToAuthority(ReqUnpossessMsg.Encode());
                return;
            }
            if (inSomething)
                return; // entering: wait for PossessCompleted

            if (!(_pawn.Input is ISpiritInput spirit))
                return;
            Role role = _pawn.Role;
            bool pressed = role == Role.EvilJinn ? spirit.PrimaryPressed : role == Role.GoodJinn && spirit.PossessAsGoodPressed;
            if (!pressed)
                return;
            // E on a puzzle (or a lamp) is a hold for the SpiritInteractor, not a possession.
            if (role == Role.EvilJinn && _interactor != null && _interactor.HasUsableTarget)
                return;

            GameBalanceConfig config = GameServices.Config;
            if (config == null)
                return;
            Transform eyes = _pawn.Eyes;
            Possessable target = Possessable.FindLookTarget(eyes.position, eyes.forward, config.PossessRange, lookAngle, role == Role.GoodJinn);
            if (target != null)
                net.SendToAuthority(ReqPossessMsg.Encode(target.NetId));
        }

        /// <summary>Debug: asks to possess the nearest object in range regardless of view direction. False when there is none.</summary>
        public bool DebugRequestNearest()
        {
            INetBridge net = GameServices.Net;
            GameBalanceConfig config = GameServices.Config;
            if (net == null || config == null)
                return false;
            Possessable target = Possessable.FindLookTarget(transform.position, transform.forward, config.PossessRange, 180f, _pawn.Role == Role.GoodJinn);
            if (target == null)
                return false;
            net.SendToAuthority(ReqPossessMsg.Encode(target.NetId));
            return true;
        }
    }
}
