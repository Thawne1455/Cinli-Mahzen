using System;

namespace CinliMahzen.Match
{
    /// <summary>Shared checks for the match message codecs (decode never throws; bad data just fails).</summary>
    internal static class MatchMsgValidation
    {
        /// <summary>True when <paramref name="value"/> is a declared member of the byte-backed enum <typeparamref name="TEnum"/>.</summary>
        internal static bool IsDefinedByte<TEnum>(byte value) where TEnum : struct, Enum
        {
            return Enum.IsDefined(typeof(TEnum), value);
        }

        /// <summary>Rejects NaN and infinities; <c>double.MaxValue</c> ("no timer") is finite and accepted.</summary>
        internal static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
