using CinliMahzen.Core;
using UnityEngine;

namespace CinliMahzen.Player
{
    /// <summary>
    /// Shared <see cref="DamageInfo"/> wire layout for <see cref="HumanDamagedMsg"/> and <see cref="HumanFaintedMsg"/>.
    /// Writes/reads <see cref="FieldCount"/> consecutive payload slots starting at an offset. Only PUN-serializable types:
    /// <code>
    /// +0 int     Amount
    /// +1 byte    Attacker (PlayerId.Value, 255 = None)
    /// +2 int     SourceObject (NetId.Value, 0 = invalid)
    /// +3 string  CauseId (null is sent as "" and read back as "")
    /// +4 byte    Flags (DamageFlags)
    /// +5 float   KnockdownTime
    /// +6 Vector3 Point
    /// +7 Vector3 Direction
    /// </code>
    /// </summary>
    public static class DamageInfoCodec
    {
        public const int FieldCount = 8;

        /// <summary>Writes <paramref name="info"/> into <c>payload[offset .. offset + FieldCount)</c>. The range must fit.</summary>
        public static void Write(in DamageInfo info, object[] payload, int offset)
        {
            payload[offset] = info.Amount;
            payload[offset + 1] = info.Attacker.Value;
            payload[offset + 2] = info.SourceObject.Value;
            payload[offset + 3] = info.CauseId ?? string.Empty;
            payload[offset + 4] = (byte)info.Flags;
            payload[offset + 5] = info.KnockdownTime;
            payload[offset + 6] = info.Point;
            payload[offset + 7] = info.Direction;
        }

        /// <summary>Reads a <see cref="DamageInfo"/>; false (never throws) on null/short payload or any wrong element type.</summary>
        public static bool TryRead(object[] payload, int offset, out DamageInfo info)
        {
            info = default;
            if (payload == null || offset < 0 || offset > payload.Length - FieldCount)
            {
                return false;
            }

            if (!(payload[offset] is int amount)
                || !(payload[offset + 1] is byte attacker)
                || !(payload[offset + 2] is int sourceObject)
                || !(payload[offset + 3] is string causeId)
                || !(payload[offset + 4] is byte flags)
                || !(payload[offset + 5] is float knockdownTime)
                || !(payload[offset + 6] is Vector3 point)
                || !(payload[offset + 7] is Vector3 direction))
            {
                return false;
            }

            info = new DamageInfo
            {
                Amount = amount,
                Attacker = new PlayerId(attacker),
                SourceObject = new NetId(sourceObject),
                CauseId = causeId,
                Flags = (DamageFlags)flags,
                KnockdownTime = knockdownTime,
                Point = point,
                Direction = direction,
            };
            return true;
        }
    }
}
