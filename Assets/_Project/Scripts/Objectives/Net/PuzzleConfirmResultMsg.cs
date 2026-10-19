using CinliMahzen.Core;
using CinliMahzen.Core.Net;

namespace CinliMahzen.Objectives
{
    /// <summary>
    /// Encode/decode helper for <see cref="MsgCode.PuzzleConfirmResult"/> (Teknik §7.3). Authority → everyone, right after a processed confirm:
    /// ok = true (the puzzle was solved and is now locked; <c>PuzzleCompleted</c> follows) or ok = false (wrong answer: shock + lever cooldown).
    /// Payload (PUN-serializable): { int slot, bool ok, byte by }. Refused requests (cooldown, range, ...) send nothing.
    /// </summary>
    public static class PuzzleConfirmResultMsg
    {
        private const int PayloadLength = 3;

        public static NetMsg Encode(int slot, bool ok, PlayerId by)
        {
            return new NetMsg
            {
                Code = MsgCode.PuzzleConfirmResult,
                Payload = new object[] { slot, ok, by.Value },
            };
        }

        /// <summary>False (never throws) for another code, a null or wrong-length payload, a wrong element type or a negative slot.</summary>
        public static bool TryDecode(NetMsg msg, out int slot, out bool ok, out PlayerId by)
        {
            slot = 0;
            ok = false;
            by = PlayerId.None;

            object[] p = msg.Payload;
            if (msg.Code != MsgCode.PuzzleConfirmResult || p == null || p.Length != PayloadLength)
            {
                return false;
            }
            if (!(p[0] is int slotValue) || !(p[1] is bool okValue) || !(p[2] is byte byValue))
            {
                return false;
            }
            if (slotValue < 0)
            {
                return false;
            }

            slot = slotValue;
            ok = okValue;
            by = new PlayerId(byValue);
            return true;
        }
    }
}
