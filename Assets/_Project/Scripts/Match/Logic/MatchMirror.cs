using System;
using CinliMahzen.Core;

namespace CinliMahzen.Match
{
    /// <summary>
    /// The match state as every machine sees it (Teknik §4.3, §9), plain C#. The authority runs the <see cref="MatchStateMachine"/>
    /// and <i>broadcasts</i>; every machine — the authority included — feeds the broadcast messages into one of these, and everything
    /// else reads <see cref="IMatchInfo"/> from it. So a client never needs the machine, and the authority's readers see exactly what
    /// the clients see (Request → authority → broadcast → everyone applies).
    /// <list type="bullet">
    /// <item><b>State.</b> <see cref="ApplyStateChanged"/> takes a <c>MatchStateChanged</c>. The same state arriving again just updates
    /// the end time (the debug "skip" re-sends it). Entering RoundSetup starts a new round (faints back to 0); Lobby clears the match.</item>
    /// <item><b>Sunrise counter.</b> <see cref="ApplyTimeAdded"/> takes the <c>newEndTime</c> of a <c>RoundTimeAdded</c>.</item>
    /// <item><b>JinnsAwake.</b> <see cref="IMatchInfo"/> has no time parameter, so the mirror asks the clock it was built with
    /// (<c>Net.Time</c>); <see cref="IsJinnsAwakeAt"/> answers for an exact time. The Playing start is derived from the first Playing
    /// message (<c>StateEndTime - BaseRoundDuration</c>), which also works for a client that joins mid-round.</item>
    /// </list>
    /// Nothing here allocates after construction.
    /// </summary>
    public sealed class MatchMirror : IMatchInfo
    {
        private readonly MatchSettings _settings;
        private readonly Func<double> _clock;
        private double _playingStart;

        /// <param name="settings">Same settings the authority's machine uses (<see cref="MatchSettings.FromConfig"/>).</param>
        /// <param name="clock">Current <c>Net.Time</c>; read by <see cref="JinnsAwake"/> only.</param>
        public MatchMirror(MatchSettings settings, Func<double> clock)
        {
            if (clock == null)
            {
                throw new ArgumentNullException(nameof(clock));
            }

            _settings = settings;
            _clock = clock;
            Clear();
        }

        public MatchState State { get; private set; }

        /// <summary>Current round, 0-based.</summary>
        public int RoundIndex { get; private set; }

        /// <summary><c>Net.Time</c> at which the current state ends; <c>double.MaxValue</c> without a timer.</summary>
        public double StateEndTime { get; private set; }

        /// <summary>Human faints this round (last <see cref="ApplyFaints"/>); 0 when a round is set up.</summary>
        public int Faints { get; private set; }

        /// <summary>Setup seed of the current round, from the latest <c>RoundSetup</c> (0 before the first one).</summary>
        public int Seed { get; private set; }

        /// <summary>Winner of the most recently ended round (<see cref="RoundResult.None"/> until one ends).</summary>
        public RoundResult LastResult { get; private set; }

        /// <summary>Why the most recent round ended.</summary>
        public RoundEndReason LastReason { get; private set; }

        /// <summary><c>Net.Time</c> at which Playing started this round; <c>double.MaxValue</c> until it does.</summary>
        public double PlayingStartTime => _playingStart;

        /// <summary>True once <c>JinnWakeDelay</c> has passed in Playing, judged by the clock.</summary>
        public bool JinnsAwake => IsJinnsAwakeAt(_clock());

        /// <summary>True when the state is Playing and <paramref name="now"/> is at least <c>JinnWakeDelay</c> after Playing began.</summary>
        public bool IsJinnsAwakeAt(double now)
        {
            return State == MatchState.Playing && now >= _playingStart + _settings.JinnWakeDelay;
        }

        /// <summary>Applies a <c>MatchStateChanged</c> (every machine, in broadcast order).</summary>
        public void ApplyStateChanged(MatchState state, int roundIndex, double stateEndTime)
        {
            MatchState previous = State;
            State = state;
            RoundIndex = roundIndex;
            StateEndTime = stateEndTime;

            switch (state)
            {
                case MatchState.Lobby:
                    Clear();
                    RoundIndex = roundIndex;
                    StateEndTime = stateEndTime;
                    break;

                case MatchState.RoundSetup:
                    if (previous != MatchState.RoundSetup)
                    {
                        Faints = 0;
                        _playingStart = double.MaxValue;
                    }

                    break;

                case MatchState.Playing:
                    if (previous != MatchState.Playing)
                    {
                        _playingStart = stateEndTime - _settings.BaseRoundDuration;
                    }

                    break;
            }
        }

        /// <summary>
        /// Applies a <c>RoundTimeAdded</c>: the Sunrise counter now ends at <paramref name="newEndTime"/>.
        /// False (nothing changes) outside Playing — the message cannot belong to the current round then.
        /// </summary>
        public bool ApplyTimeAdded(double newEndTime)
        {
            if (State != MatchState.Playing)
            {
                return false;
            }

            StateEndTime = newEndTime;
            return true;
        }

        /// <summary>Applies the human's total faint count this round (from <c>HumanFaintedEvt.FaintCount</c>).</summary>
        public void ApplyFaints(int faintCount)
        {
            Faints = Math.Max(0, faintCount);
        }

        /// <summary>Applies the seed of a <c>RoundSetup</c>.</summary>
        public void ApplyRoundSetup(int seed)
        {
            Seed = seed;
        }

        /// <summary>Applies a <c>RoundEnded</c>.</summary>
        public void ApplyRoundEnded(RoundResult result, RoundEndReason reason)
        {
            LastResult = result;
            LastReason = reason;
        }

        private void Clear()
        {
            State = MatchState.Lobby;
            RoundIndex = 0;
            StateEndTime = double.MaxValue;
            Faints = 0;
            Seed = 0;
            LastResult = RoundResult.None;
            LastReason = RoundEndReason.None;
            _playingStart = double.MaxValue;
        }
    }
}
