using CinliMahzen.Core;
using CinliMahzen.Core.Net;
using UnityEngine;

namespace CinliMahzen.Objectives
{
    /// <summary>
    /// The body of a puzzle as the evil jinn's sabotage target (Oyun §4.2, Teknik §7.3–7.4, TODO 2.6). On the root of <c>P_Puzzle_*</c> next
    /// to <see cref="PuzzleView"/>, which binds it. The jinn's SpiritInteractor hits any part collider (Interactable layer), finds this
    /// component as the parent <see cref="ISpiritHoldTarget"/>, counts <c>ScrambleHoldTime</c> (1.5 s) of E within <c>ScrambleRange</c> and then
    /// <see cref="SendRequest"/> sends <c>ReqPuzzleScramble{slot}</c>. The authority (<see cref="PuzzleSystemCore.ProcessScrambleAuthority"/>)
    /// checks role, wake time, lit room, lantern safe zone, the 15 s cooldown and the 25 energy, then scrambles every part at once.
    /// The local prediction below only hides the prompt when the sabotage obviously cannot happen.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PuzzleScrambleTarget : MonoBehaviour, ISpiritHoldTarget, INetEntity
    {
        private const string PromptKeyValue = "interact.puzzle_scramble";

        private PuzzleView _view;
        private NetId _netId;

        public NetId NetId => _netId;

        public string SpiritPromptKey => PromptKeyValue;

        public float SpiritHoldTime
        {
            get
            {
                GameBalanceConfig config = GameServices.Config;
                return config != null ? config.ScrambleHoldTime : 0f;
            }
        }

        public float SpiritRange
        {
            get
            {
                GameBalanceConfig config = GameServices.Config;
                return config != null ? config.ScrambleRange + config.NetRangeTolerance : 0f;
            }
        }

        public bool IsBound => _view != null && _view.IsBound;

        internal void Bind(PuzzleView view, NetId id)
        {
            _view = view;
            _netId = id;
        }

        internal void Unbind()
        {
            _view = null;
            _netId = NetId.Invalid;
        }

        public bool CanSpiritInteract(PlayerId who, Role role)
        {
            if (role != Role.EvilJinn || !IsBound || _view.IsLocked)
                return false;
            PuzzleSystemCore core = _view.Core;
            return !core.IsPuzzleScrambleOnCooldown(_view.Slot, out float _);
        }

        public void SendRequest(PlayerId who)
        {
            if (!IsBound)
                return;
            GameServices.Net?.SendToAuthority(ReqPuzzleScrambleMsg.Encode(_view.Slot));
        }
    }
}
