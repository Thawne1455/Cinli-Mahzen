using CinliMahzen.Core;
using CinliMahzen.Core.Net;

namespace CinliMahzen.Match
{
    /// <summary>
    /// <c>RoundEnded</c> (Teknik §4.3) — authority → all, together with the <c>MatchStateChanged</c> into RoundEnd.
    /// Payload: <c>{ byte result, byte reason, int roundIndex }</c>. Decoding never throws.
    /// </summary>
    public static class RoundEndedMsg
    {
        private const int PayloadLength = 3;

        public static NetMsg Encode(RoundResult result, RoundEndReason reason, int roundIndex)
        {
            return new NetMsg
            {
                Code = MsgCode.RoundEnded,
                Payload = new object[] { (byte)result, (byte)reason, roundIndex },
            };
        }

        /// <summary>False for a wrong code, missing/short payload, wrong element types, unknown enum values or a negative round.</summary>
        public static bool TryDecode(in NetMsg msg, out RoundResult result, out RoundEndReason reason, out int roundIndex)
        {
            result = RoundResult.None;
            reason = RoundEndReason.None;
            roundIndex = 0;

            object[] p = msg.Payload;
            if (msg.Code != MsgCode.RoundEnded || p == null || p.Length < PayloadLength)
            {
                return false;
            }

            if (!(p[0] is byte rawResult) || !(p[1] is byte rawReason) || !(p[2] is int round))
            {
                return false;
            }

            if (!MatchMsgValidation.IsDefinedByte<RoundResult>(rawResult) ||
                !MatchMsgValidation.IsDefinedByte<RoundEndReason>(rawReason) ||
                round < 0)
            {
                return false;
            }

            result = (RoundResult)rawResult;
            reason = (RoundEndReason)rawReason;
            roundIndex = round;
            return true;
        }
    }
}
