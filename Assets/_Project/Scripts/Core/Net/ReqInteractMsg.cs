namespace CinliMahzen.Core.Net
{
    /// <summary>
    /// Encode/decode helper for <see cref="MsgCode.ReqInteract"/> (Teknik §4.5). Owner → authority: a hold completed.
    /// Payload (PUN-serializable): { int targetNetId }. The requester is the message's Sender, filled by the bridge.
    /// </summary>
    public static class ReqInteractMsg
    {
        private const int PayloadLength = 1;

        public static NetMsg Encode(NetId target)
        {
            return new NetMsg
            {
                Code = MsgCode.ReqInteract,
                Payload = new object[] { target.Value },
            };
        }

        /// <summary>False (never throws) for another code, a null or wrong-length payload, or a wrong element type.</summary>
        public static bool TryDecode(NetMsg msg, out NetId target)
        {
            target = NetId.Invalid;

            object[] p = msg.Payload;
            if (msg.Code != MsgCode.ReqInteract || p == null || p.Length != PayloadLength)
                return false;
            if (!(p[0] is int targetValue))
                return false;

            target = new NetId(targetValue);
            return true;
        }
    }
}
