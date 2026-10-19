using CinliMahzen.Core.Net;

namespace CinliMahzen.Possession
{
    /// <summary>
    /// Encode/decode helper for <see cref="MsgCode.ReqUnpossess"/> (Teknik §6.2). Jinn client → authority: "let me out of the object I am
    /// in" (Space / E while possessing). The requester is <see cref="NetMsg.Sender"/>. Payload: empty.
    /// </summary>
    public static class ReqUnpossessMsg
    {
        public static NetMsg Encode()
        {
            return new NetMsg
            {
                Code = MsgCode.ReqUnpossess,
                Payload = new object[0],
            };
        }

        /// <summary>False (never throws) for another code or a non-empty payload. A null payload is accepted as empty.</summary>
        public static bool TryDecode(NetMsg msg)
        {
            return msg.Code == MsgCode.ReqUnpossess && (msg.Payload == null || msg.Payload.Length == 0);
        }
    }
}
