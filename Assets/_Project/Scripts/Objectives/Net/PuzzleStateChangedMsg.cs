using System;
using CinliMahzen.Core;
using CinliMahzen.Core.Net;

namespace CinliMahzen.Objectives
{
    /// <summary>
    /// Encode/decode helper for <see cref="MsgCode.PuzzleStateChanged"/> (Teknik §7.3). Authority → everyone after an accepted human op.
    /// Payload (PUN-serializable): { int slot, int[] values, byte by }. <c>values</c> is the full part list after the change
    /// (PuzzleState.ApplySnapshot on every client). The array is copied on both sides, so neither caller shares memory with the message.
    /// </summary>
    public static class PuzzleStateChangedMsg
    {
        private const int PayloadLength = 3;

        /// <exception cref="ArgumentNullException"><paramref name="values"/> is null.</exception>
        public static NetMsg Encode(int slot, int[] values, PlayerId by)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }
            return new NetMsg
            {
                Code = MsgCode.PuzzleStateChanged,
                Payload = new object[] { slot, (int[])values.Clone(), by.Value },
            };
        }

        /// <summary>False (never throws) for another code, a null or wrong-length payload, a wrong element type, a negative slot, or a null / empty value list.</summary>
        public static bool TryDecode(NetMsg msg, out int slot, out int[] values, out PlayerId by)
        {
            slot = 0;
            values = null;
            by = PlayerId.None;

            object[] p = msg.Payload;
            if (msg.Code != MsgCode.PuzzleStateChanged || p == null || p.Length != PayloadLength)
            {
                return false;
            }
            if (!(p[0] is int slotValue) || !(p[1] is int[] valueList) || !(p[2] is byte byValue))
            {
                return false;
            }
            if (slotValue < 0 || valueList.Length == 0)
            {
                return false;
            }

            slot = slotValue;
            values = (int[])valueList.Clone();
            by = new PlayerId(byValue);
            return true;
        }
    }
}
