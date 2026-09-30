using CinliMahzen.Core;

namespace CinliMahzen.Possession
{
    /// <summary>
    /// Per-object possession state machine (Tech §6.2), plain C#.
    /// <code>
    /// Free ─BeginEnter─► Entering ─(PossessTime)─► Lurking ◄─► Charging ─Resolve─► Recovering ─► Lurking
    ///   ▲                   │ Exit                    │ Exit (voluntary / exorcised / salt)          │ single use
    ///   └───────────────────┴─────────────────────────┘                                              ▼
    ///                                                                                   Spent (never again)
    /// Blessed = timer overlay: a blessed object cannot be entered until the timer runs out.
    /// </code>
    /// The authority drives it through <see cref="PossessionArbiterCore"/> and the action runner;
    /// clients use the same methods to mirror broadcast results.
    /// All times are absolute <c>Net.Time</c> values.
    /// </summary>
    public sealed class PossessableStateMachine
    {
        private bool _spentAfterRecover;
        private bool _chargeReadyReported;

        public PossessableStateMachine()
        {
            Reset();
        }

        public PossessableState State { get; private set; }

        /// <summary>Jinn inside (Entering or possessed), otherwise <see cref="PlayerId.None"/>.</summary>
        public PlayerId Occupant { get; private set; }

        /// <summary>End of the current timed state: Entering end, Charging resolve time or Recovering end.</summary>
        public double StateEndTime { get; private set; }

        /// <summary>Action being charged / recovered from (0 = primary, 1 = secondary), -1 when none.</summary>
        public int ActionIndex { get; private set; }

        public double BlessedUntil { get; private set; }

        public bool IsOccupied =>
            State == PossessableState.Entering || State == PossessableState.Lurking ||
            State == PossessableState.Charging || State == PossessableState.Recovering;

        /// <summary>True once entering finished (Lurking, Charging or Recovering).</summary>
        public bool IsPossessed =>
            State == PossessableState.Lurking || State == PossessableState.Charging ||
            State == PossessableState.Recovering;

        public bool IsSpent => State == PossessableState.Spent;

        /// <summary>True while the last resolved action was single use (object turns Spent when the jinn leaves).</summary>
        public bool WillBeSpent => _spentAfterRecover;

        public bool IsBlessed(double now)
        {
            return now < BlessedUntil;
        }

        /// <summary>Free → Entering. <paramref name="endTime"/> = now + PossessTime.</summary>
        public bool BeginEnter(PlayerId who, double endTime)
        {
            if (State != PossessableState.Free)
            {
                return false;
            }
            State = PossessableState.Entering;
            Occupant = who;
            StateEndTime = endTime;
            ActionIndex = -1;
            return true;
        }

        /// <summary>Entering → Lurking (authority via <see cref="Tick"/>, clients on <c>PossessCompleted</c>).</summary>
        public bool CompleteEnter()
        {
            if (State != PossessableState.Entering)
            {
                return false;
            }
            State = PossessableState.Lurking;
            StateEndTime = 0d;
            return true;
        }

        /// <summary>Lurking → Charging. <paramref name="resolveAt"/> = now + TelegraphTime.</summary>
        public bool BeginCharge(int actionIndex, double resolveAt)
        {
            if (State != PossessableState.Lurking || actionIndex < 0)
            {
                return false;
            }
            State = PossessableState.Charging;
            ActionIndex = actionIndex;
            StateEndTime = resolveAt;
            _chargeReadyReported = false;
            return true;
        }

        /// <summary>Charging → Lurking without resolving (kick, released too early, debug).</summary>
        public bool CancelCharge()
        {
            if (State != PossessableState.Charging)
            {
                return false;
            }
            State = PossessableState.Lurking;
            ActionIndex = -1;
            StateEndTime = 0d;
            return true;
        }

        /// <summary>
        /// Charging → Recovering. With <paramref name="spent"/> the object becomes Spent and the
        /// occupant is ejected once recovery finishes (or immediately if the jinn exits earlier).
        /// </summary>
        public bool Resolve(bool spent, double recoverUntil)
        {
            if (State != PossessableState.Charging)
            {
                return false;
            }
            State = PossessableState.Recovering;
            StateEndTime = recoverUntil;
            _spentAfterRecover = spent;
            return true;
        }

        /// <summary>Recovering → Lurking, or → Spent (occupant ejected) after a single-use action.</summary>
        public bool FinishRecover()
        {
            if (State != PossessableState.Recovering)
            {
                return false;
            }
            if (_spentAfterRecover)
            {
                MarkSpent();
                return true;
            }
            State = PossessableState.Lurking;
            ActionIndex = -1;
            StateEndTime = 0d;
            return true;
        }

        /// <summary>
        /// Occupant leaves (any occupied state). Returns who left, or <see cref="PlayerId.None"/>
        /// if the object was not occupied. A pending single-use result turns the object Spent.
        /// </summary>
        public PlayerId Exit()
        {
            if (!IsOccupied)
            {
                return PlayerId.None;
            }
            PlayerId who = Occupant;
            if (_spentAfterRecover)
            {
                MarkSpent();
                return who;
            }
            State = PossessableState.Free;
            Occupant = PlayerId.None;
            ActionIndex = -1;
            StateEndTime = 0d;
            return who;
        }

        /// <summary>Extends the blessing to <paramref name="until"/> (never shortens it).</summary>
        public void Bless(double until)
        {
            if (until > BlessedUntil)
            {
                BlessedUntil = until;
            }
        }

        public void ClearBless()
        {
            BlessedUntil = double.NegativeInfinity;
        }

        /// <summary>Forces Spent (occupant cleared). Used on resolve-eject and by clients mirroring state.</summary>
        public void MarkSpent()
        {
            State = PossessableState.Spent;
            Occupant = PlayerId.None;
            ActionIndex = -1;
            StateEndTime = 0d;
            _spentAfterRecover = false;
            _chargeReadyReported = false;
        }

        /// <summary>Back to a fresh Free, unblessed object (new round).</summary>
        public void Reset()
        {
            State = PossessableState.Free;
            Occupant = PlayerId.None;
            StateEndTime = 0d;
            ActionIndex = -1;
            BlessedUntil = double.NegativeInfinity;
            _spentAfterRecover = false;
            _chargeReadyReported = false;
        }

        /// <summary>
        /// Advances timer-driven transitions. Read <see cref="Occupant"/> before calling if you need
        /// to know who was ejected on <see cref="PossessableTransition.SpentEjected"/>.
        /// <see cref="PossessableTransition.ChargeReady"/> is reported once per charge.
        /// </summary>
        public PossessableTransition Tick(double now)
        {
            switch (State)
            {
                case PossessableState.Entering:
                    if (now >= StateEndTime)
                    {
                        CompleteEnter();
                        return PossessableTransition.EnterCompleted;
                    }
                    break;

                case PossessableState.Charging:
                    if (!_chargeReadyReported && now >= StateEndTime)
                    {
                        _chargeReadyReported = true;
                        return PossessableTransition.ChargeReady;
                    }
                    break;

                case PossessableState.Recovering:
                    if (now >= StateEndTime)
                    {
                        bool spent = _spentAfterRecover;
                        FinishRecover();
                        return spent ? PossessableTransition.SpentEjected : PossessableTransition.RecoverFinished;
                    }
                    break;
            }
            return PossessableTransition.None;
        }
    }
}
