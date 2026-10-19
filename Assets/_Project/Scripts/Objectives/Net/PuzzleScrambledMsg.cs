using System;
using CinliMahzen.Core;
using CinliMahzen.Core.Net;

namespace CinliMahzen.Objectives
{
    /// <summary>
    /// Encode/decode helper for <see cref="MsgCode.PuzzleScrambled"/> (Teknik §7.3). Authority → everyone after an accepted mass sabotage.
    /// Payload (PUN-serializable): { int slot, int[] values, byte by, double cooldownUntil }. <c>values</c> is the new (unsolved)
    /// arrangement; <c>cooldownUntil</c> is the absolute Net.Time before which the puzzle cannot be scrambled again.
    /// The array is copied on both sides.
    /// </summary>
    public static class PuzzleScrambledMsg
    {
        private const int PayloadLength = 4;

        /// <exception cref="ArgumentNullException"><paramref name="values"/> is null.</exception>
        public static NetMsg Encode(int slot, int[] values, PlayerId by, double cooldownUntil)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }
            return new NetMsg
            {
                Code = MsgCode.PuzzleScrambled,
                Payload = new object[] { slot, (int[])values.Clone(), by.Value, cooldownUntil },
            };
        }

        /// <summary>
        /// False (never throws) for another code, a null or wrong-length payload, a wrong element type, a negative slot,
        /// a null / empty value list, and a NaN or infinite cooldown.
        /// </summary>
        public static bool TryDecode(NetMsg msg, out int slot, out int[] values, out PlayerId by, out double cooldownUntil)
        {
            slot = 0;
            values = null;
            by = PlayerId.None;
            cooldownUntil = 0d;

            object[] p = msg.Payload;
            if (msg.Code != MsgCode.PuzzleScrambled || p == null || p.Length != PayloadLength)
            {
                return false;
            }
            if (!(p[0] is int slotValue) || !(p[1] is int[] valueList) || !(p[2] is byte byValue) || !(p[3] is double cooldownValue))
            {
                return false;
            }
            if (slotValue < 0 || valueList.Length == 0 || double.IsNaN(cooldownValue) || double.IsInfinity(cooldownValue))
            {
                return false;
            }

            slot = slotValue;
            values = (int[])valueList.Clone();
            by = new PlayerId(byValue);
            cooldownUntil = cooldownValue;
            return true;
        }
    }
}
