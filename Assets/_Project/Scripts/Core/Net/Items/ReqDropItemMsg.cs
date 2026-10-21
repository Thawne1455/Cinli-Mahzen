namespace CinliMahzen.Core.Net
{
    /// <summary>
    /// Encode/decode helper for <see cref="MsgCode.ReqDropItem"/> (Teknik §5.4, TODO 2.7). Human client → authority: drop the item in inventory
    /// slot <c>slot</c> (G). The requester is <see cref="NetMsg.Sender"/>. Payload: { int slot }.
    /// </summary>
    public static class ReqDropItemMsg
    {
        private const int PayloadLength = 1;

        public static NetMsg Encode(int slot)
        {
            return new NetMsg { Code = MsgCode.ReqDropItem, Payload = new object[] { slot } };
        }

        /// <summary>False (never throws) for another code, a malformed payload or a negative slot.</summary>
        public static bool TryDecode(NetMsg msg, out int slot)
        {
            slot = -1;
            object[] p = msg.Payload;
            if (msg.Code != MsgCode.ReqDropItem || p == null || p.Length != PayloadLength || !(p[0] is int value) || value < 0)
                return false;
            slot = value;
            return true;
        }
    }
}
