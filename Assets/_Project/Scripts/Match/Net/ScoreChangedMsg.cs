using CinliMahzen.Core;
using CinliMahzen.Core.Net;

namespace CinliMahzen.Match
{
    /// <summary>
    /// <c>ScoreChanged</c> (Teknik §9, TODO 3.10) — authority → all. Payload: <c>{ byte player, int total, int delta, byte reason }</c>.
    /// The total travels (not only the delta), so a lost or duplicated message cannot drift a client's score. Decoding never throws.
    /// </summary>
    public static class ScoreChangedMsg
    {
        private const int PayloadLength = 4;

        public static NetMsg Encode(PlayerId player, int total, int delta, ScoreReason reason)
        {
            return new NetMsg
            {
                Code = MsgCode.ScoreChanged,
                Payload = new object[] { player.Value, total, delta, (byte)reason },
            };
        }

        /// <summary>False for a wrong code, short payload, wrong element types, an invalid player or an unknown reason.</summary>
        public static bool TryDecode(in NetMsg msg, out PlayerId player, out int total, out int delta, out ScoreReason reason)
        {
            player = PlayerId.None;
            total = 0;
            delta = 0;
            reason = ScoreReason.Reset;

            object[] p = msg.Payload;
            if (msg.Code != MsgCode.ScoreChanged || p == null || p.Length < PayloadLength)
            {
                return false;
            }

            if (!(p[0] is byte rawPlayer) || !(p[1] is int rawTotal) || !(p[2] is int rawDelta) || !(p[3] is byte rawReason))
            {
                return false;
            }

            if (rawPlayer == PlayerId.None.Value || !MatchMsgValidation.IsDefinedByte<ScoreReason>(rawReason))
            {
                return false;
            }

            player = new PlayerId(rawPlayer);
            total = rawTotal;
            delta = rawDelta;
            reason = (ScoreReason)rawReason;
            return true;
        }
    }
}
