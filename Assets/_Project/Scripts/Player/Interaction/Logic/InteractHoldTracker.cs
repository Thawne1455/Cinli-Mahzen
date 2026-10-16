using CinliMahzen.Core;

namespace CinliMahzen.Player
{
    /// <summary>
    /// Hold-to-interact progress for the local interactor (Teknik §4.5), plain C#. Time is passed in as <c>dt</c>.
    /// One press is one hold. It ends as Completed (holdTime reached) or Cancelled (released, target lost or changed),
    /// and then it is spent: nothing starts again until the button is released. A press that begins without a target
    /// is spent too. Progress is 1 after Completed until release, 0 otherwise.
    /// </summary>
    public sealed class InteractHoldTracker
    {
        private NetId _target;
        private float _elapsed;
        private float _progress;
        private bool _holding;
        private bool _spent;

        /// <summary>0..1: rises while holding, stays 1 after Completed until release, 0 otherwise.</summary>
        public float Progress => _progress;

        /// <summary>Drops any hold and clears the spent state (interactor disabled or not the local pawn).</summary>
        public void Reset()
        {
            _target = NetId.Invalid;
            _elapsed = 0f;
            _progress = 0f;
            _holding = false;
            _spent = false;
        }

        /// <summary>
        /// Advances one frame. <paramref name="target"/> is <see cref="NetId.Invalid"/> when nothing usable is aimed at.
        /// <paramref name="holdTime"/> of 0 (or less) completes on the press frame.
        /// </summary>
        public HoldStep Update(NetId target, float holdTime, bool held, float dt)
        {
            if (!held)
            {
                HoldStep released = _holding ? HoldStep.Cancelled : HoldStep.Idle;
                Reset();
                return released;
            }

            if (_spent)
                return HoldStep.Idle;

            if (!target.IsValid || (_holding && target != _target))
            {
                HoldStep interrupted = _holding ? HoldStep.Cancelled : HoldStep.Idle;
                _target = NetId.Invalid;
                _elapsed = 0f;
                _progress = 0f;
                _holding = false;
                _spent = true;
                return interrupted;
            }

            if (!_holding)
            {
                _target = target;
                _elapsed = 0f;
                _holding = true;
            }

            _elapsed += dt;
            if (!(holdTime > 0f) || _elapsed >= holdTime)
            {
                _holding = false;
                _elapsed = 0f;
                _progress = 1f;
                _spent = true;
                return HoldStep.Completed;
            }

            _progress = _elapsed / holdTime;
            return HoldStep.Holding;
        }
    }
}
