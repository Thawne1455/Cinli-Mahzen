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
        private const string PromptKeyValue = "interact.puzzle_turn";

        [SerializeField] private PuzzlePartDisplay display;
        [Tooltip("Cycle direction of one press (+1 or -1).")]
        [SerializeField] private int direction = 1;
        [Tooltip("0..1 for the noise system.")]
        [Range(0f, 1f)] [SerializeField] private float noise = 0.2f;

        private PuzzleView _view;
        private int _index = -1;
        private NetId _netId;

        public NetId NetId => _netId;

        public string PromptKey => PromptKeyValue;

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
            _view.Core.ProcessOpAuthority(who, _view.Slot, PuzzleOp.Cycle(_index, direction >= 0 ? 1 : -1));
        }
    }
}
