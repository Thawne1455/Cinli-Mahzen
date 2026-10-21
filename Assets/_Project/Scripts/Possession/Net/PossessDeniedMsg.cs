using System;
using CinliMahzen.Core;
using CinliMahzen.Core.Net;

namespace CinliMahzen.Possession
{
    /// <summary>
    /// Encode/decode helper for <see cref="MsgCode.PossessDenied"/> (Teknik §6.2). Authority → everyone (only the requester reacts): the
    /// possession request of <c>player</c> for <c>obj</c> was refused with <c>reason</c> (HUD hint, e.g. "the room is lit").
    /// Payload: { byte player, int obj, byte reason }. <c>obj</c> may be 0 when the request named no valid object.
    /// </summary>
    public static class PossessDeniedMsg
    {
        private const int PayloadLength = 3;

        public static NetMsg Encode(PlayerId player, NetId obj, PossessDenyReason reason)
        {
            return new NetMsg
            {
                Code = MsgCode.PossessDenied,
                Payload = new object[] { player.Value, obj.Value, (byte)reason },
            };
        }

        /// <summary>False (never throws) for another code, a malformed payload, an invalid player, an unknown reason or <see cref="PossessDenyReason.None"/>.</summary>
        public static bool TryDecode(NetMsg msg, out PlayerId player, out NetId obj, out PossessDenyReason reason)
        {
            player = PlayerId.None;
            obj = NetId.Invalid;
            reason = PossessDenyReason.None;

            object[] p = msg.Payload;
            if (msg.Code != MsgCode.PossessDenied || p == null || p.Length != PayloadLength)
                return false;
            if (!(p[0] is byte playerValue) || !(p[1] is int objValue) || !(p[2] is byte reasonValue))
                return false;

            var decodedPlayer = new PlayerId(playerValue);
            if (!decodedPlayer.IsValid || reasonValue == 0 || !Enum.IsDefined(typeof(PossessDenyReason), reasonValue))
                return false;

            player = decodedPlayer;
            obj = new NetId(objValue);
            reason = (PossessDenyReason)reasonValue;
            return true;
        }
    }
}
