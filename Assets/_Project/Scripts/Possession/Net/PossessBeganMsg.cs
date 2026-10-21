using CinliMahzen.Core;
using CinliMahzen.Core.Net;

namespace CinliMahzen.Possession
{
    /// <summary>
    /// Encode/decode helper for <see cref="MsgCode.PossessBegan"/> (Teknik §6.2). Authority → everyone: <c>player</c> started entering
    /// <c>obj</c>; it finishes at <c>enterEndTime</c> (Net.Time; the object shakes until then). Payload: { byte player, int obj, double enterEndTime }.
    /// </summary>
    public static class PossessBeganMsg
    {
        private const int PayloadLength = 3;

        public static NetMsg Encode(PlayerId player, NetId obj, double enterEndTime)
        {
            return new NetMsg
            {
                Code = MsgCode.PossessBegan,
                Payload = new object[] { player.Value, obj.Value, enterEndTime },
            };
        }

        /// <summary>False (never throws) for another code, a malformed payload, an invalid player or an invalid object id.</summary>
        public static bool TryDecode(NetMsg msg, out PlayerId player, out NetId obj, out double enterEndTime)
        {
            player = PlayerId.None;
            obj = NetId.Invalid;
            enterEndTime = 0d;

            object[] p = msg.Payload;
            if (msg.Code != MsgCode.PossessBegan || p == null || p.Length != PayloadLength)
                return false;
            if (!(p[0] is byte playerValue) || !(p[1] is int objValue) || !(p[2] is double end))
                return false;

            var decodedPlayer = new PlayerId(playerValue);
            if (!decodedPlayer.IsValid || objValue == 0)
                return false;

            player = decodedPlayer;
            obj = new NetId(objValue);
            enterEndTime = end;
            return true;
        }
    }
}
