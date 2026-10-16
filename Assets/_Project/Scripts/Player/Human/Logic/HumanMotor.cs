namespace CinliMahzen.Player
{
    /// <summary>
    /// Human body state machine (Teknik §5.2), plain C#. Time is passed in (GameServices.Net.Time), never read here.
    /// Priority: Fainted &gt; KnockedDown &gt; Interacting &gt; Normal. Fainted blocks everything; a knockdown cancels an interaction.
    /// </summary>
    public sealed class HumanMotor
    {
        private double _knockedDownUntil;

        public HumanMotorState State { get; private set; } = HumanMotorState.Normal;

        /// <summary>Net time at which a knockdown ends (meaningful while <see cref="HumanMotorState.KnockedDown"/>).</summary>
        public double KnockedDownUntil => _knockedDownUntil;

        /// <summary>Walking is possible only in Normal.</summary>
        public bool CanMove => State == HumanMotorState.Normal;

        /// <summary>Looking is possible in every state except Fainted.</summary>
        public bool CanLook => State != HumanMotorState.Fainted;

        /// <summary>Sprinting needs the same freedom as walking.</summary>
        public bool CanSprint => State == HumanMotorState.Normal;

        /// <summary>Normal → Interacting. False (no change) in any other state.</summary>
        public bool EnterInteracting()
        {
            if (State != HumanMotorState.Normal)
                return false;
            State = HumanMotorState.Interacting;
            return true;
        }

        /// <summary>Interacting → Normal. Ignored in any other state (e.g. a knockdown already ended the interaction).</summary>
        public void ExitInteracting()
        {
            if (State == HumanMotorState.Interacting)
                State = HumanMotorState.Normal;
        }

        /// <summary>
        /// Knocked down until net time <paramref name="until"/>. Ignored while fainted; from Normal or Interacting it
        /// starts a knockdown, while already knocked down it only ever extends the end time.
        /// </summary>
        public void KnockDown(double until)
        {
            if (State == HumanMotorState.Fainted)
                return;
            if (State == HumanMotorState.KnockedDown)
            {
                if (until > _knockedDownUntil)
                    _knockedDownUntil = until;
                return;
            }
            State = HumanMotorState.KnockedDown;
            _knockedDownUntil = until;
        }

        /// <summary>Leaves the knockdown once <paramref name="now"/> reaches its end time. Call every frame.</summary>
        public void Tick(double now)
        {
            if (State == HumanMotorState.KnockedDown && now >= _knockedDownUntil)
                State = HumanMotorState.Normal;
        }

        /// <summary>
        /// true: Fainted from any state (a pending knockdown is dropped). false: Fainted → Normal; other states unchanged.
        /// </summary>
        public void SetFainted(bool fainted)
        {
            if (fainted)
            {
                State = HumanMotorState.Fainted;
                _knockedDownUntil = 0d;
            }
            else if (State == HumanMotorState.Fainted)
            {
                State = HumanMotorState.Normal;
            }
        }
    }
}
