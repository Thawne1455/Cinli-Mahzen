using CinliMahzen.Core.Net;

namespace CinliMahzen.Jinn
{
    /// <summary>
    /// Encode/decode helper for <see cref="MsgCode.ReqFocusReference"/> (Teknik §4.3, §6.5). Good jinn's client → authority:
    /// the Spirit Veil focus (right mouse button) started or stopped.
    /// Payload (PUN-serializable): { bool focusing }. The requester is the message's Sender, filled by the bridge.
    /// </summary>
    public static class ReqFocusReferenceMsg
    {
        private const int PayloadLength = 1;

        public static NetMsg Encode(bool focusing)
        {
            return new NetMsg
            {
                Code = MsgCode.ReqFocusReference,
                Payload = new object[] { focusing },
            };
        }

        /// <summary>False (never throws) for another code, a null or wrong-length payload, or a wrong element type.</summary>
        public static bool TryDecode(NetMsg msg, out bool focusing)
        {
            focusing = false;

            object[] p = msg.Payload;
            if (msg.Code != MsgCode.ReqFocusReference || p == null || p.Length != PayloadLength)
                return false;
            if (!(p[0] is bool focusingValue))
                return false;

            focusing = focusingValue;
            return true;
        }
    }
}
