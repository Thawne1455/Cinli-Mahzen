using CinliMahzen.Core.Net;

namespace CinliMahzen.Match
{
    /// <summary>
    /// <c>RoundTimeAdded</c> (Teknik §4.3) — authority → all, after a gate opened.
    /// Payload: <c>{ float bonusSec, double newEndTime }</c>. Decoding never throws.
    /// </summary>
    public static class RoundTimeAddedMsg
    {
        private const int PayloadLength = 2;

        public static NetMsg Encode(float bonusSec, double newEndTime)
        {
            return new NetMsg
            {
                Code = MsgCode.RoundTimeAdded,
                Payload = new object[] { bonusSec, newEndTime },
            };
        }

        /// <summary>False for a wrong code, missing/short payload, wrong element types or non-finite numbers.</summary>
        public static bool TryDecode(in NetMsg msg, out float bonusSec, out double newEndTime)
        {
            bonusSec = 0f;
            newEndTime = 0d;

            object[] p = msg.Payload;
            if (msg.Code != MsgCode.RoundTimeAdded || p == null || p.Length < PayloadLength)
            {
                return false;
            }

            if (!(p[0] is float bonus) || !(p[1] is double endTime))
            {
                return false;
            }

            if (!MatchMsgValidation.IsFinite(bonus) || !MatchMsgValidation.IsFinite(endTime))
            {
                return false;
            }

            bonusSec = bonus;
            newEndTime = endTime;
            return true;
        }
    }
}
