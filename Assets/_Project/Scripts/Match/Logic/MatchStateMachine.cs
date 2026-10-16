using System;
using CinliMahzen.Core;

namespace CinliMahzen.Match
{
    /// <summary>
    /// Match flow state machine (Teknik §9), plain C#. Runs on the authority only; the MonoBehaviour wrapper
    /// (<c>MatchController</c>, 1.10 integration) feeds it <c>Net.Time</c> as <c>now</c> and turns every returned
    /// <see cref="MatchTransition"/> into a <c>MatchStateChanged</c> broadcast. No Unity time API is used here.
    /// <code>
    /// Lobby ─StartMatch─► RoundSetup ─CompleteRoundSetup─► RoleReveal ─► Intro ─► Playing ─► RoundEnd ─┬─► RoundSetup (round + 1)
    ///                                                                                                  └─► MatchEnd (last round)
    /// </code>
    /// <list type="bullet">
    /// <item><b>Timers.</b> RoleReveal, Intro, Playing and RoundEnd end at <see cref="StateEndTime"/>; <see cref="Tick"/>
    /// advances one state per call. A state's duration is measured from the <i>scheduled</i> end of the previous state, not
    /// from the tick time, so a late tick (or a huge jump) never shifts the schedule — call <see cref="Tick"/> in a loop
    /// until it returns false.</item>
    /// <item><b>RoundSetup has no timer.</b> It is a gate: on entering it the authority takes <see cref="Seed"/> and the roles
    /// (<see cref="RoleRotation"/>), broadcasts <c>RoundSetup</c>, builds the house and then calls
    /// <see cref="CompleteRoundSetup"/> (offline: same frame; online: after every client reported <c>HouseReady</c>).
    /// <see cref="Tick"/> never leaves RoundSetup by itself.</item>
    /// <item><b>Sunrise counter.</b> Playing starts with <c>StateEndTime = start + BaseRoundDuration</c>;
    /// <see cref="TryAddStageTimeBonus"/> adds <c>StageTimeBonus</c> per opened gate. Reaching <see cref="StateEndTime"/> ends
    /// the round (<see cref="RoundEndReason.Timeout"/>, <see cref="RoundResult.JinnsWin"/>).</item>
    /// <item><b>Event order.</b> Round events (<see cref="TryEndRound"/>, <see cref="OnHumanFainted"/>,
    /// <see cref="OnTreasureDug"/>, <see cref="TryAddStageTimeBonus"/>) are accepted only while Playing and
    /// <c>now &lt; StateEndTime</c>: the sun rising wins a tie, and a second round end is rejected. Call
    /// <see cref="Tick"/> first each frame, then deliver events.</item>
    /// </list>
    /// Nothing here allocates after construction; the seed provider is only invoked when a round starts.
    /// </summary>
    public sealed class MatchStateMachine : IMatchInfo
    {
        private readonly MatchSettings _settings;
        private readonly Func<int, int> _seedForRound;
        private double _playingStart;
        private double _lastTickTime;

        /// <param name="settings">Durations and limits (<see cref="MatchSettings.FromConfig"/>).</param>
        /// <param name="seedForRound">Round index (0-based) → seed of that round's setup. Called once per round, when it enters RoundSetup.
        /// The caller owns the randomness (<c>GameRandom</c>); the machine only stores the result in <see cref="Seed"/>.</param>
        public MatchStateMachine(MatchSettings settings, Func<int, int> seedForRound)
        {
            if (seedForRound == null)
            {
                throw new ArgumentNullException(nameof(seedForRound));
            }

            _settings = settings;
            _seedForRound = seedForRound;
            ResetFields();
        }

        public MatchSettings Settings => _settings;

        public MatchState State { get; private set; }

        /// <summary>Current round, 0-based. Stays at the last round in MatchEnd.</summary>
        public int RoundIndex { get; private set; }

        /// <summary>
        /// <c>Net.Time</c> at which the current state ends; grows by <c>StageTimeBonus</c> per opened gate while Playing.
        /// <c>double.MaxValue</c> in Lobby, RoundSetup and MatchEnd (no timer).
        /// </summary>
        public double StateEndTime { get; private set; }

        /// <summary>Human faints this round, as last reported through <see cref="OnHumanFainted"/>; reset when a round starts.</summary>
        public int Faints { get; private set; }

        /// <summary>Setup seed of the current round (valid from RoundSetup on; 0 in Lobby).</summary>
        public int Seed { get; private set; }

        /// <summary>Winner of the most recently ended round (<see cref="RoundResult.None"/> before the first one ends).</summary>
        public RoundResult LastResult { get; private set; }

        /// <summary>Why the most recent round ended.</summary>
        public RoundEndReason LastReason { get; private set; }

        /// <summary>
        /// <c>Net.Time</c> at which Playing started this round; <c>double.MaxValue</c> until it does.
        /// Stays set during RoundEnd so the round report can compute its length.
        /// </summary>
        public double PlayingStartTime => _playingStart;

        /// <summary>
        /// <see cref="IMatchInfo"/>: true once <c>JinnWakeDelay</c> has passed in Playing, judged by the <b>latest time given to
        /// <see cref="Tick"/></b> (the interface has no time parameter). Use <see cref="IsJinnsAwakeAt"/> to ask for an exact time.
        /// </summary>
        public bool JinnsAwake => IsJinnsAwakeAt(_lastTickTime);

        /// <summary>True when the state is Playing and <paramref name="now"/> is at least <c>JinnWakeDelay</c> after Playing began.</summary>
        public bool IsJinnsAwakeAt(double now)
        {
            return State == MatchState.Playing && now >= _playingStart + _settings.JinnWakeDelay;
        }

        /// <summary>Lobby → RoundSetup (round 0). False when not in Lobby.</summary>
        public bool StartMatch(out MatchTransition transition)
        {
            if (State != MatchState.Lobby)
            {
                transition = default;
                return false;
            }

            BeginRound(0, out transition);
            return true;
        }

        /// <summary>
        /// RoundSetup → RoleReveal; <c>StateEndTime = now + RoleRevealDuration</c>. False when not in RoundSetup.
        /// </summary>
        public bool CompleteRoundSetup(double now, out MatchTransition transition)
        {
            if (State != MatchState.RoundSetup)
            {
                transition = default;
                return false;
            }

            EnterState(MatchState.RoleReveal, now + _settings.RoleRevealDuration, out transition);
            return true;
        }

        /// <summary>
        /// Advances at most one timed state whose end has been reached (<c>now &gt;= StateEndTime</c>); returns whether it did.
        /// Call in a loop (<c>while (Tick(now, out t)) Publish(t);</c>). Also records <paramref name="now"/> for <see cref="JinnsAwake"/>.
        /// </summary>
        public bool Tick(double now, out MatchTransition transition)
        {
            if (now > _lastTickTime)
            {
                _lastTickTime = now;
            }

            switch (State)
            {
                case MatchState.RoleReveal:
                    if (now >= StateEndTime)
                    {
                        EnterState(MatchState.Intro, StateEndTime + _settings.IntroCountdown, out transition);
                        return true;
                    }
                    break;

                case MatchState.Intro:
                    if (now >= StateEndTime)
                    {
                        _playingStart = StateEndTime;
                        EnterState(MatchState.Playing, _playingStart + _settings.BaseRoundDuration, out transition);
                        return true;
                    }
                    break;

                case MatchState.Playing:
                    if (now >= StateEndTime)
                    {
                        EnterRoundEnd(RoundResult.JinnsWin, RoundEndReason.Timeout, StateEndTime, out transition);
                        return true;
                    }
                    break;

                case MatchState.RoundEnd:
                    if (now >= StateEndTime)
                    {
                        if (RoundIndex + 1 < _settings.RoundsPerMatch)
                        {
                            BeginRound(RoundIndex + 1, out transition);
                        }
                        else
                        {
                            EnterState(MatchState.MatchEnd, double.MaxValue, out transition);
                        }
                        return true;
                    }
                    break;
            }

            // Lobby, RoundSetup and MatchEnd have no timer; the others are not due yet.
            transition = default;
            return false;
        }

        /// <summary>
        /// Gate opened: <c>StateEndTime += StageTimeBonus</c> (Playing only, and only before the counter ran out).
        /// On success <paramref name="newEndTime"/> is the new end (the value for <c>RoundTimeAdded</c>); otherwise the unchanged <see cref="StateEndTime"/>.
        /// </summary>
        public bool TryAddStageTimeBonus(double now, out double newEndTime)
        {
            if (!AcceptsRoundEvents(now))
            {
                newEndTime = StateEndTime;
                return false;
            }

            StateEndTime += _settings.StageTimeBonus;
            newEndTime = StateEndTime;
            return true;
        }

        /// <summary>
        /// Playing → RoundEnd; <c>StateEndTime = now + RoundEndDuration</c>. Rejected outside Playing (so a second end is
        /// ignored), after the counter ran out, and for a <paramref name="result"/>/<paramref name="reason"/> pair that is not one of the
        /// three win conditions (Faint → JinnsWin, TreasureDug → SeekersWin, Timeout → JinnsWin).
        /// </summary>
        public bool TryEndRound(RoundResult result, RoundEndReason reason, double now, out MatchTransition transition)
        {
            if (!AcceptsRoundEvents(now) || result == RoundResult.None || ResultFor(reason) != result)
            {
                transition = default;
                return false;
            }

            EnterRoundEnd(result, reason, now, out transition);
            return true;
        }

        /// <summary>
        /// Records the human's total faint count this round. When it reaches <c>FaintsToLose</c> the round ends
        /// (JinnsWin, Faint) and the method returns true with the transition; otherwise it returns false.
        /// Ignored outside Playing.
        /// </summary>
        public bool OnHumanFainted(int faintCount, double now, out MatchTransition transition)
        {
            if (!AcceptsRoundEvents(now))
            {
                transition = default;
                return false;
            }

            Faints = faintCount;
            if (faintCount >= _settings.FaintsToLose)
            {
                return TryEndRound(RoundResult.JinnsWin, RoundEndReason.Faint, now, out transition);
            }

            transition = default;
            return false;
        }

        /// <summary>Treasure dug: ends the round (SeekersWin, TreasureDug). False when the round cannot end now.</summary>
        public bool OnTreasureDug(double now, out MatchTransition transition)
        {
            return TryEndRound(RoundResult.SeekersWin, RoundEndReason.TreasureDug, now, out transition);
        }

        /// <summary>
        /// Any state except Lobby → Lobby: a finished match (play again) or an aborted one. Clears round data,
        /// <see cref="LastResult"/> and <see cref="LastReason"/>. False when already in Lobby.
        /// </summary>
        public bool ReturnToLobby(out MatchTransition transition)
        {
            if (State == MatchState.Lobby)
            {
                transition = default;
                return false;
            }

            MatchState from = State;
            ResetFields();
            transition = new MatchTransition(from, State, RoundIndex, StateEndTime);
            return true;
        }

        private bool AcceptsRoundEvents(double now)
        {
            return State == MatchState.Playing && now < StateEndTime;
        }

        private static RoundResult ResultFor(RoundEndReason reason)
        {
            switch (reason)
            {
                case RoundEndReason.Faint:
                case RoundEndReason.Timeout:
                    return RoundResult.JinnsWin;
                case RoundEndReason.TreasureDug:
                    return RoundResult.SeekersWin;
                default:
                    return RoundResult.None;
            }
        }

        private void BeginRound(int roundIndex, out MatchTransition transition)
        {
            int seed = _seedForRound(roundIndex);
            RoundIndex = roundIndex;
            Seed = seed;
            Faints = 0;
            _playingStart = double.MaxValue;
            EnterState(MatchState.RoundSetup, double.MaxValue, out transition);
        }

        private void EnterRoundEnd(RoundResult result, RoundEndReason reason, double endedAt, out MatchTransition transition)
        {
            LastResult = result;
            LastReason = reason;
            EnterState(MatchState.RoundEnd, endedAt + _settings.RoundEndDuration, out transition);
        }

        private void EnterState(MatchState to, double endTime, out MatchTransition transition)
        {
            MatchState from = State;
            State = to;
            StateEndTime = endTime;
            transition = new MatchTransition(from, to, RoundIndex, endTime);
        }

        private void ResetFields()
        {
            State = MatchState.Lobby;
            RoundIndex = 0;
            StateEndTime = double.MaxValue;
            Faints = 0;
            Seed = 0;
            LastResult = RoundResult.None;
            LastReason = RoundEndReason.None;
            _playingStart = double.MaxValue;
            _lastTickTime = double.MinValue;
        }
    }
}
