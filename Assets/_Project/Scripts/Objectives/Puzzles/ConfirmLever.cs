using CinliMahzen.Core;
using UnityEngine;

namespace CinliMahzen.Objectives
{
    /// <summary>
    /// The confirm lever of a puzzle (Teknik §7.3–7.4, TODO 2.5): the human pulls it (E) → on the authority
    /// <see cref="PuzzleSystemCore.ProcessConfirmAuthority"/> locks a solved puzzle (PuzzleCompleted) or shocks the human and locks the lever for
    /// <c>ConfirmFailCooldown</c>. The handle visual tips over once the puzzle is completed. Needs a collider on the Interactable layer.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ConfirmLever : MonoBehaviour, IInteractable, INetEntity
    {
        private const string PromptKeyValue = "interact.puzzle_confirm";

        [Tooltip("Turns by doneAngle around its local X axis when the puzzle is completed. Empty = no visual change.")]
        [SerializeField] private Transform handle;
        [SerializeField] private float doneAngle = 60f;
        [Range(0f, 1f)] [SerializeField] private float noise = 0.5f;

        private PuzzleView _view;
        private NetId _netId;
        private Quaternion _handleRest;
        private bool _restKnown;

        public NetId NetId => _netId;

        public string PromptKey => PromptKeyValue;

        public float HoldTime => 0f;

        public float NoiseOnInteract => noise;

        internal void Bind(PuzzleView view, NetId id)
        {
            _view = view;
            _netId = id;
        }

        /// <summary>Handle up (open puzzle) or down (completed).</summary>
        internal void ShowDone(bool done)
        {
            if (handle == null)
                return;
            if (!_restKnown)
            {
                _handleRest = handle.localRotation;
                _restKnown = true;
            }
            handle.localRotation = done ? _handleRest * Quaternion.Euler(doneAngle, 0f, 0f) : _handleRest;
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
            _view.Core.ProcessConfirmAuthority(who, _view.Slot);
        }
    }
}
