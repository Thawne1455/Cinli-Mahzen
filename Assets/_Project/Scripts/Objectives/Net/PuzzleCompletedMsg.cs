using CinliMahzen.Core.Net;

namespace CinliMahzen.Objectives
{
    /// <summary>
    /// Encode/decode helper for <see cref="MsgCode.PuzzleCompleted"/> (Teknik §7.3). Authority → everyone when a confirm locked a solved puzzle
    /// (<c>PuzzleCompletedEvt{slot, stage}</c> on every machine). Payload (PUN-serializable): { int slot, int stage }.
    /// </summary>
    public static class PuzzleCompletedMsg
    {
        private const int PayloadLength = 2;

        public static NetMsg Encode(int slot, int stage)
        {
            return new NetMsg
            {
                Code = MsgCode.PuzzleCompleted,
                Payload = new object[] { slot, stage },
            };
        }

        /// <summary>False (never throws) for another code, a null or wrong-length payload, a wrong element type, or a negative slot or stage.</summary>
        public static bool TryDecode(NetMsg msg, out int slot, out int stage)
        {
            slot = 0;
            stage = 0;

            object[] p = msg.Payload;
            if (msg.Code != MsgCode.PuzzleCompleted || p == null || p.Length != PayloadLength)
            {
                return false;
            }
            if (!(p[0] is int slotValue) || !(p[1] is int stageValue))
            {
                return false;
            }
            if (slotValue < 0 || stageValue < 0)
            {
                return false;
            }

            slot = slotValue;
            stage = stageValue;
            return true;
        }
    }
}
