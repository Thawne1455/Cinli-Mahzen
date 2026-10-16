namespace CinliMahzen.Core.Net
{
    /// <summary>
    /// Encode/decode helper for <see cref="MsgCode.InteractResult"/> (Teknik §4.5). Authority → everyone, ok true or false.
    /// Payload (PUN-serializable): { int targetNetId, byte player, bool ok }.
    /// </summary>
    public static class InteractResultMsg
    {
        private const int PayloadLength = 3;

        public static NetMsg Encode(NetId target, PlayerId player, bool ok)
        {
            return new NetMsg
            {
                Code = MsgCode.InteractResult,
                Payload = new object[] { target.Value, player.Value, ok },
            };
        }

        /// <summary>False (never throws) for another code, a null or wrong-length payload, or a wrong element type.</summary>
        public static bool TryDecode(NetMsg msg, out NetId target, out PlayerId player, out bool ok)
        {
            target = NetId.Invalid;
            player = PlayerId.None;
            ok = false;

            object[] p = msg.Payload;
            if (msg.Code != MsgCode.InteractResult || p == null || p.Length != PayloadLength)
                return false;
            if (!(p[0] is int targetValue) || !(p[1] is byte playerValue) || !(p[2] is bool okValue))
                return false;

            target = new NetId(targetValue);
            player = new PlayerId(playerValue);
            ok = okValue;
            return true;
        }
    }
}
