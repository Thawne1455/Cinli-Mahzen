using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.Core.Events;
using CinliMahzen.Core.Net;
using UnityEngine;

namespace CinliMahzen.Objectives
{
    /// <summary>
    /// The answer of a puzzle in another room (Oyun §4.1–4.2, Teknik §6.5, TODO 2.5), root of <c>P_PuzzleRef_*</c> on the SpiritOnly layer
    /// (the human's camera never draws it; both jinns see where it hangs). <see cref="Bind"/> draws the round's answer once
    /// (+ <c>ReferenceRotation</c> for the seal stones, crossed-out trap values for the talisman switch).
    /// <para><b>Spirit Veil:</b> the content is hidden behind <see cref="veil"/> unless the LOCAL player is the good jinn and is focusing
    /// (<see cref="ReferenceFocusChangedEvt"/>, the authority's flag mirrored by SpiritController). So an evil jinn only sees the veiled
    /// frame, and switching hotseat players (<see cref="IPlayerRegistry.LocalPlayerChanged"/>) re-evaluates it.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PuzzleReference : MonoBehaviour
    {
        [Tooltip("In part index order; each shows the answer value of its part.")]
        [SerializeField] private PuzzlePartDisplay[] parts;
        [Tooltip("Parent of the answer drawing; hidden while veiled.")]
        [SerializeField] private GameObject content;
        [Tooltip("Shown while veiled (the blurred / covered look).")]
        [SerializeField] private GameObject veil;
        [Tooltip("Turned by ReferenceRotation around its forward axis (seal stones). Empty = the content transform.")]
        [SerializeField] private Transform rotationRoot;
        [Tooltip("Optional: one mark per value; the marks of the trap values are shown (talisman switch X marks).")]
        [SerializeField] private GameObject[] trapMarks;
        [Tooltip("Optional (talisman switch, TODO 3.2): decoy pictures; display i shows trap value i (each carries its own X mark).")]
        [SerializeField] private PuzzlePartDisplay[] trapDisplays;

        private readonly bool[] _focusing = new bool[256];
        private bool _revealed;
        private Quaternion _rotationRest = Quaternion.identity;
        private bool _rotationRestKnown;
        private bool _subscribed;
        private IPlayerRegistry _players;
        private System.Action<PlayerId> _onLocalPlayerChanged;
        private System.Action _onRolesChanged;

        /// <summary>True while this machine draws the answer (local good jinn focusing).</summary>
        public bool IsRevealed => _revealed;

        public int PartCount => parts != null ? parts.Length : 0;

        /// <summary>
        /// Draws the answer. <paramref name="rotation"/> is 0/90/180/270; <paramref name="trapValues"/> may be null. False (and an error) when
        /// the answer has a different number of parts than the reference.
        /// </summary>
        public bool Bind(IReadOnlyList<int> solution, int rotation, IReadOnlyList<int> trapValues)
        {
            if (solution == null || solution.Count != PartCount)
            {
                CMLog.Error("Puzzle", name + ": answer has " + (solution == null ? 0 : solution.Count) + " values, the reference " + PartCount + " parts");
                return false;
            }

            for (int i = 0; i < parts.Length; i++)
                parts[i].Show(solution[i]);

            Transform root = rotationRoot != null ? rotationRoot : (content != null ? content.transform : null);
            if (root != null)
            {
                // Composed with the root's own pose (the seal stones' root is tipped so its Z is world up), never overwriting it.
                if (!_rotationRestKnown)
                {
                    _rotationRest = root.localRotation;
                    _rotationRestKnown = true;
                }
                root.localRotation = _rotationRest * Quaternion.Euler(0f, 0f, -rotation);
            }

            if (trapMarks != null)
            {
                for (int i = 0; i < trapMarks.Length; i++)
                {
                    if (trapMarks[i] != null)
                        trapMarks[i].SetActive(Contains(trapValues, i));
                }
            }

            if (trapDisplays != null)
            {
                for (int i = 0; i < trapDisplays.Length; i++)
                {
                    bool shown = trapValues != null && i < trapValues.Count;
                    if (trapDisplays[i] == null)
                        continue;
                    trapDisplays[i].gameObject.SetActive(shown);
                    if (shown)
                        trapDisplays[i].Show(trapValues[i]);
                }
            }

            Subscribe();
            ApplyReveal();
            return true;
        }

        private static bool Contains(IReadOnlyList<int> values, int value)
        {
            if (values == null)
                return false;
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] == value)
                    return true;
            }
            return false;
        }

        private void OnEnable()
        {
            Subscribe();
            ApplyReveal();
        }

        private void OnDisable()
        {
            if (!_subscribed)
                return;
            _subscribed = false;
            EventBus.Unsubscribe<ReferenceFocusChangedEvt>(OnFocusChanged);
            if (_players != null)
            {
                _players.LocalPlayerChanged -= _onLocalPlayerChanged;
                _players.RolesChanged -= _onRolesChanged;
                _players = null;
            }
        }

        private void Subscribe()
        {
            if (_subscribed)
                return;
            _subscribed = true;
            EventBus.Subscribe<ReferenceFocusChangedEvt>(OnFocusChanged);
            // Hotseat: another local player (or new roles) may change who is looking.
            _players = GameServices.Players;
            if (_players != null)
            {
                _onLocalPlayerChanged = _onLocalPlayerChanged ?? (p => ApplyReveal());
                _onRolesChanged = _onRolesChanged ?? ApplyReveal;
                _players.LocalPlayerChanged += _onLocalPlayerChanged;
                _players.RolesChanged += _onRolesChanged;
            }
        }

        private void OnFocusChanged(ReferenceFocusChangedEvt e)
        {
            if (!e.Jinn.IsValid)
                return;
            _focusing[e.Jinn.Value] = e.IsFocusing;
            ApplyReveal();
        }

        private void ApplyReveal()
        {
            INetBridge net = GameServices.Net;
            IPlayerRegistry players = GameServices.Players;
            PlayerId local = net != null ? net.LocalPlayer : PlayerId.None;
            bool reveal = local.IsValid && players != null && players.GetRole(local) == Role.GoodJinn && _focusing[local.Value];

            _revealed = reveal;
            if (content != null)
                content.SetActive(reveal);
            if (veil != null)
                veil.SetActive(!reveal);
        }
    }
}
