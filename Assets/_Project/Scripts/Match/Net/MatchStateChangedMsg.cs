using CinliMahzen.Core;
using CinliMahzen.Core.Net;

namespace CinliMahzen.Match
{
    /// <summary>
    /// <c>MatchStateChanged</c> (Teknik §4.3) — authority → all. Payload: <c>{ byte state, int roundIndex, double stateEndTime }</c>.
    /// Decoding is lenient about extra trailing elements and strict about everything else; it never throws.
    /// </summary>
    public static class MatchStateChangedMsg
    {
        private const int PayloadLength = 3;

        public static NetMsg Encode(MatchState state, int roundIndex, double stateEndTime)
        {
            return new NetMsg
            {
                Code = MsgCode.MatchStateChanged,
                Payload = new object[] { (byte)state, roundIndex, stateEndTime },
            };
        }

        public static NetMsg Encode(in MatchTransition transition)
        {
            return Encode(transition.To, transition.RoundIndex, transition.StateEndTime);
        }

        /// <summary>False for a wrong code, missing/short payload, wrong element types, an unknown state, a negative round or a non-finite time.</summary>
        public static bool TryDecode(in NetMsg msg, out MatchState state, out int roundIndex, out double stateEndTime)
        {
            state = MatchState.Lobby;
            roundIndex = 0;
            stateEndTime = 0d;

            object[] p = msg.Payload;
            if (msg.Code != MsgCode.MatchStateChanged || p == null || p.Length < PayloadLength)
            {
                return false;
            }

            if (!(p[0] is byte rawState) || !(p[1] is int round) || !(p[2] is double endTime))
            {
                return false;
            }

            if (!MatchMsgValidation.IsDefinedByte<MatchState>(rawState) || round < 0 || !MatchMsgValidation.IsFinite(endTime))
            {
                return false;
            }

            state = (MatchState)rawState;
            roundIndex = round;
            stateEndTime = endTime;
            return true;
        }
    }
}
