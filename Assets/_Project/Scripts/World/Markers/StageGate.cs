using CinliMahzen.Core;
using UnityEngine;

namespace CinliMahzen.World
{
    /// <summary>
    /// The lockable passage that ends a stage (Teknik 8, Oyun 3.1): stage 0 = front door, 1..N = stair gates, and the cellar hatch
    /// (<see cref="IsCellarHatch"/>, stage StagesActive + 1, locked with the cellar key).
    /// Opening it gives the stage time bonus. A <see cref="GateLock"/> on the same GameObject makes it need a key item.
    /// </summary>
    public sealed class StageGate : NetHouseMarker, IInteractable
    {
        // TODO 3.6: a gate with a GateLock opens when the human uses it carrying the key (front door: house key, +StageTimeBonus).
        private GateLock _lock;
        private bool _lockLooked;

        private GateLock Lock
        {
            get
            {
                if (!_lockLooked)
                {
                    _lock = GetComponent<GateLock>();
                    _lockLooked = true;
                }
                return _lock;
            }
        }

        public string PromptKey => "interact.use_item";

        public float HoldTime => 0f;

        public float NoiseOnInteract => 0.4f;

        public bool CanInteract(PlayerId who, Role role)
        {
            return role == Role.Human && Lock != null && GameServices.TryGet(out IObjectiveInteractions objectives) && objectives.IsGateClosed(NetId);
        }

        public bool ValidateAuthority(PlayerId who)
        {
            return Lock != null && GameServices.TryGet(out IObjectiveInteractions _);
        }

        public void ExecuteAuthority(PlayerId who)
        {
            Debug.Assert(GameServices.Net.IsAuthority);
            if (GameServices.TryGet(out IObjectiveInteractions objectives))
                objectives.UseGateAuthority(who, NetId);
        }

        [Tooltip("The stage this gate completes. Ignored for the cellar hatch.")]
        [SerializeField] private int stageIndex;
        [Tooltip("The door piece that opens and closes. Empty = this transform.")]
        [SerializeField] private Transform door;
        [Tooltip("TODO 4.1: the cellar hatch. It completes the stage after the last active puzzle stage (StagesActive + 1), so the same house "
            + "works with or without the attic stage.")]
        [SerializeField] private bool isCellarHatch;

        /// <summary>The stage this gate completes; for the cellar hatch use <see cref="StageFor"/>.</summary>
        public int StageIndex
        {
            get { return stageIndex; }
        }

        public bool IsCellarHatch
        {
            get { return isCellarHatch; }
        }

        public Transform Door
        {
            get { return door != null ? door : transform; }
        }

        /// <summary>The stage this gate completes when <paramref name="stagesActive"/> puzzle stages are played.</summary>
        public int StageFor(int stagesActive)
        {
            return HouseContract.GateStage(stageIndex, isCellarHatch, stagesActive);
        }

        /// <summary>Used by the graybox builder and tests.</summary>
        public void Configure(int newStageIndex, Transform newDoor, bool newIsCellarHatch = false)
        {
            stageIndex = newStageIndex;
            door = newDoor;
            isCellarHatch = newIsCellarHatch;
        }

        protected override Color GizmoColor
        {
            get { return Color.white; }
        }
    }
}
