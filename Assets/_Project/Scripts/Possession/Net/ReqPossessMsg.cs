using CinliMahzen.Core;
using CinliMahzen.Core.Net;

namespace CinliMahzen.Possession
{
    /// <summary>
    /// Encode/decode helper for <see cref="MsgCode.ReqPossess"/> (Teknik §4.3, §6.2). Jinn client → authority: "let me into this object".
    /// The requester is <see cref="NetMsg.Sender"/>. Payload (PUN-serializable): { int obj }.
    /// </summary>
    public static class ReqPossessMsg
    {
        private const int PayloadLength = 1;

        public static NetMsg Encode(NetId obj)
        {
            return new NetMsg
            {
                Code = MsgCode.ReqPossess,
                Payload = new object[] { obj.Value },
            };
        }

        /// <summary>False (never throws) for another code, a null or wrong-length payload, a wrong element type or an invalid id.</summary>
        public static bool TryDecode(NetMsg msg, out NetId obj)
        {
            obj = NetId.Invalid;
            object[] p = msg.Payload;
            if (msg.Code != MsgCode.ReqPossess || p == null || p.Length != PayloadLength)
                return false;
            if (!(p[0] is int value) || value == 0)
                return false;

            obj = new NetId(value);
            return true;
        }
    }
}
