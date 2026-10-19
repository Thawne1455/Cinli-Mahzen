using CinliMahzen.Core.Net;

namespace CinliMahzen.Objectives
{
    /// <summary>
    /// Encode/decode helper for <see cref="MsgCode.ReqPuzzleScramble"/> (Teknik §7.3). Evil jinn → authority: the 1.5 s hold on a puzzle completed.
    /// Payload (PUN-serializable): { int slot }. The requester is the message's Sender.
    /// </summary>
    public static class ReqPuzzleScrambleMsg
    {
        private const int PayloadLength = 1;

        public static NetMsg Encode(int slot)
        {
            return new NetMsg
            {
                Code = MsgCode.ReqPuzzleScramble,
                Payload = new object[] { slot },
            };
        }

        /// <summary>False (never throws) for another code, a null or wrong-length payload, a wrong element type or a negative slot.</summary>
        public static bool TryDecode(NetMsg msg, out int slot)
        {
            slot = 0;

            object[] p = msg.Payload;
            if (msg.Code != MsgCode.ReqPuzzleScramble || p == null || p.Length != PayloadLength)
            {
                return false;
            }
            if (!(p[0] is int slotValue) || slotValue < 0)
            {
                return false;
            }

            slot = slotValue;
            return true;
        }
    }
}
