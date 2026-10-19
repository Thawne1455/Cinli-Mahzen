using System;
using CinliMahzen.Core;
using CinliMahzen.Core.Net;

namespace CinliMahzen.Possession
{
    /// <summary>
    /// Encode/decode helper for <see cref="MsgCode.PossessEnded"/> (Teknik §6.2). Authority → everyone: <c>player</c> left <c>obj</c> for
    /// <c>reason</c>; <c>spent</c> = the object is used up for the rest of the round (shelf, cologne).
    /// Payload: { byte player, int obj, byte reason, bool spent }.
    /// </summary>
    public static class PossessEndedMsg
    {
        private const int PayloadLength = 4;

        public static NetMsg Encode(PlayerId player, NetId obj, PossessEndReason reason, bool spent)
        {
            return new NetMsg
            {
                Code = MsgCode.PossessEnded,
                Payload = new object[] { player.Value, obj.Value, (byte)reason, spent },
            };
        }

        /// <summary>False (never throws) for another code, a malformed payload, an invalid player / object id or an unknown reason.</summary>
        public static bool TryDecode(NetMsg msg, out PlayerId player, out NetId obj, out PossessEndReason reason, out bool spent)
        {
            player = PlayerId.None;
            obj = NetId.Invalid;
            reason = PossessEndReason.Forced;
            spent = false;

            object[] p = msg.Payload;
            if (msg.Code != MsgCode.PossessEnded || p == null || p.Length != PayloadLength)
                return false;
            if (!(p[0] is byte playerValue) || !(p[1] is int objValue) || !(p[2] is byte reasonValue) || !(p[3] is bool spentValue))
                return false;

            var decodedPlayer = new PlayerId(playerValue);
            if (!decodedPlayer.IsValid || objValue == 0 || !Enum.IsDefined(typeof(PossessEndReason), reasonValue))
                return false;

            player = decodedPlayer;
            obj = new NetId(objValue);
            reason = (PossessEndReason)reasonValue;
            spent = spentValue;
            return true;
        }
    }
}
