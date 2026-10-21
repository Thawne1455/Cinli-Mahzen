namespace CinliMahzen.Core.Net
{
    /// <summary>
    /// Encode/decode helper for <see cref="MsgCode.GateOpened"/> (Teknik §7.3, TODO 2.7). Authority → everyone: the gate that ends
    /// <c>stage</c> is open (every puzzle of the stage is done, or its key was used). Every machine opens the door and publishes
    /// <c>StageCompletedEvt</c>; the authority's MatchController adds <c>StageTimeBonus</c>. Payload: { int stage }.
    /// </summary>
    public static class GateOpenedMsg
    {
        private const int PayloadLength = 1;

        public static NetMsg Encode(int stage)
        {
            return new NetMsg { Code = MsgCode.GateOpened, Payload = new object[] { stage } };
        }

        /// <summary>False (never throws) for another code, a malformed payload or a negative stage.</summary>
        public static bool TryDecode(NetMsg msg, out int stage)
        {
            stage = -1;
            object[] p = msg.Payload;
            if (msg.Code != MsgCode.GateOpened || p == null || p.Length != PayloadLength || !(p[0] is int value) || value < 0)
                return false;
            stage = value;
            return true;
        }
    }
}
