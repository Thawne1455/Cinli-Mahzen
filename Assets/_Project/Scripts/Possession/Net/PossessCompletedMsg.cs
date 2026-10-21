using CinliMahzen.Core;
using CinliMahzen.Core.Net;

namespace CinliMahzen.Possession
{
    /// <summary>
    /// Encode/decode helper for <see cref="MsgCode.PossessCompleted"/> (Teknik §6.2). Authority → everyone: <c>player</c> is now inside
    /// <c>obj</c> (Entering → Lurking; the jinn's camera switches to orbit). Payload: { byte player, int obj }.
    /// </summary>
    public static class PossessCompletedMsg
    {
        private const int PayloadLength = 2;

        public static NetMsg Encode(PlayerId player, NetId obj)
        {
            return new NetMsg
            {
                Code = MsgCode.PossessCompleted,
                Payload = new object[] { player.Value, obj.Value },
            };
        }

        /// <summary>False (never throws) for another code, a malformed payload, an invalid player or an invalid object id.</summary>
        public static bool TryDecode(NetMsg msg, out PlayerId player, out NetId obj)
        {
            player = PlayerId.None;
            obj = NetId.Invalid;

            object[] p = msg.Payload;
            if (msg.Code != MsgCode.PossessCompleted || p == null || p.Length != PayloadLength)
                return false;
            if (!(p[0] is byte playerValue) || !(p[1] is int objValue))
                return false;

            var decodedPlayer = new PlayerId(playerValue);
            if (!decodedPlayer.IsValid || objValue == 0)
                return false;

            player = decodedPlayer;
            obj = new NetId(objValue);
            return true;
        }
    }
}
