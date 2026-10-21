using CinliMahzen.Core;
using UnityEngine;

namespace CinliMahzen.Objectives
{
    /// <summary>
    /// One part of an interactive puzzle (Teknik §7.4, TODO 2.5): the human looks at it and presses E (the Interactor's
    /// <c>ReqInteract</c>) → on the authority <see cref="ExecuteAuthority"/> asks <see cref="PuzzleSystemCore.ProcessOpAuthority"/> for
    /// <c>Cycle(part, +1)</c>, whose rules (role, range, item, lock) decide; the PuzzleStateChanged broadcast then redraws every machine
    /// through <see cref="PuzzleView"/>. Needs a collider on the Interactable layer. Its NetId comes from <see cref="PuzzleNetIds"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PuzzlePart : MonoBehaviour, IInteractable, INetEntity
    {
        public enum PartOp
        {
            /// <summary>One press moves the value by <see cref="direction"/> (wraps): portraits, statue, motif plugs.</summary>
            Cycle = 0,
            /// <summary>One press sets the next value with a Set op (painted ⇄ empty): seal stone lines (TODO 3.3).</summary>
            Toggle = 1,
        }

        [SerializeField] private PuzzlePartDisplay display;
        [SerializeField] private PartOp op = PartOp.Cycle;
        [Tooltip("Loc key of the prompt (interact.puzzle_turn, interact.puzzle_paint).")]
        [SerializeField] private string promptKey = "interact.puzzle_turn";
        [Tooltip("Cycle direction of one press (+1 or -1).")]
        [SerializeField] private int direction = 1;
        [Tooltip("0..1 for the noise system.")]
        [Range(0f, 1f)] [SerializeField] private float noise = 0.2f;

        private PuzzleView _view;
        private int _index = -1;
        private NetId _netId;

        public NetId NetId => _netId;

        public string PromptKey => promptKey;

        public float HoldTime => 0f;

        public float NoiseOnInteract => noise;

        public PuzzlePartDisplay Display => display;

        /// <summary>Part index in the puzzle state (-1 until bound).</summary>
        public int Index => _index;

        internal void Bind(PuzzleView view, int index, NetId id)
        {
            _view = view;
            _index = index;
            _netId = id;
        }

        public bool CanInteract(PlayerId who, Role role)
        {
            return role == Role.Human && _view != null && _view.IsBound && !_view.IsLocked;
        }

        public bool ValidateAuthority(PlayerId who)
        {
            return _view != null && _view.IsBound && _view.Core != null;
        }

        public void ExecuteAuthority(PlayerId who)
        {
            Debug.Assert(GameServices.Net.IsAuthority);
            _view.Core.ProcessOpAuthority(who, _view.Slot, BuildOp());
        }

        /// <summary>The op one press asks for (Toggle reads the current value from the state).</summary>
        public PuzzleOp BuildOp()
        {
            if (op == PartOp.Toggle && _view != null && _view.Core != null && _view.Core.TryGetSlot(_view.Slot, out PuzzleSlotRuntime runtime))
            {
                PuzzleState state = runtime.State;
                return PuzzleOp.Set(_index, (state.Get(_index) + 1) % state.ValueCount(_index));
            }
            return PuzzleOp.Cycle(_index, direction >= 0 ? 1 : -1);
        }
    }
}
