namespace CinliMahzen.Core.Net
{
    /// <summary>
    /// Encode/decode helper for <see cref="MsgCode.StatusApplied"/> (Teknik §4.3, §4.5). Authority → everyone.
    /// Payload (PUN-serializable): { int targetNetId, byte status, float duration, byte source }.
    /// Lives in CM.Core because several pawn kinds consume it (human: CameraSpin180, spirit: VisionBlurred).
    /// </summary>
    public static class StatusAppliedMsg
    {
        private const int PayloadLength = 4;

        public static NetMsg Encode(NetId target, StatusType status, float duration, PlayerId source)
        {
            return new NetMsg
            {
                Code = MsgCode.StatusApplied,
                Payload = new object[] { target.Value, (byte)status, duration, source.Value },
            };
        }

        /// <summary>
        /// False for another message code, a payload that is too short or has wrong element types,
        /// and a NaN / negative duration. Extra trailing payload elements are ignored.
        /// </summary>
        public static bool TryDecode(NetMsg msg, out NetId target, out StatusType status, out float duration, out PlayerId source)
        {
            target = NetId.Invalid;
            status = StatusType.None;
            duration = 0f;
            source = PlayerId.None;

            object[] p = msg.Payload;
            if (msg.Code != MsgCode.StatusApplied || p == null || p.Length < PayloadLength)
                return false;
            if (!(p[0] is int targetValue) || !(p[1] is byte statusValue) || !(p[2] is float durationValue) || !(p[3] is byte sourceValue))
                return false;
            if (float.IsNaN(durationValue) || durationValue < 0f)
                return false;

            target = new NetId(targetValue);
            status = (StatusType)statusValue;
            duration = durationValue;
            source = new PlayerId(sourceValue);
            return true;
        }
    }
}
