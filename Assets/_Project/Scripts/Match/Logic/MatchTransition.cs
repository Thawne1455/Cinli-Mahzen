using System.Globalization;
using CinliMahzen.Core;

namespace CinliMahzen.Match
{
    /// <summary>
    /// One state change reported by <see cref="MatchStateMachine"/>. The authority publishes it as
    /// <c>MatchStateChanged</c> (see <see cref="MatchStateChangedMsg"/>). <c>default</c> is returned alongside
    /// <c>false</c> and must be ignored.
    /// </summary>
    public readonly struct MatchTransition
    {
        public readonly MatchState From;
        public readonly MatchState To;

        /// <summary>Round the machine is in after the transition (0-based).</summary>
        public readonly int RoundIndex;

        /// <summary><c>Net.Time</c> at which the new state ends; <c>double.MaxValue</c> for states without a timer.</summary>
        public readonly double StateEndTime;

        public MatchTransition(MatchState from, MatchState to, int roundIndex, double stateEndTime)
        {
            From = from;
            To = to;
            RoundIndex = roundIndex;
            StateEndTime = stateEndTime;
        }

        public override string ToString()
        {
            return From + " -> " + To + " (round " + RoundIndex + ", ends " +
                   StateEndTime.ToString("0.###", CultureInfo.InvariantCulture) + ")";
        }
    }
}
