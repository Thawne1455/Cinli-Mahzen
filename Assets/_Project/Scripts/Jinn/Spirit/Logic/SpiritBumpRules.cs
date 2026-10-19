using CinliMahzen.Core;

namespace CinliMahzen.Jinn
{
    /// <summary>
    /// Spirit Bump rules (Teknik §6.1, Oyun §14), pure functions. Two sides of the same rule:
    /// <see cref="CanRequest"/> is the local pre-check that decides whether the evil jinn's client sends <c>ReqSpiritBump</c>;
    /// <see cref="Validate"/> is the authority's check of that request. The authority cannot see the Shift key, so Boost is only
    /// part of the client check. Distances are compared with <see cref="LimitEpsilon"/> slack so a jinn standing exactly on the limit
    /// counts as inside, even when the runtime evaluates floats at higher precision.
    /// </summary>
    public static class SpiritBumpRules
    {
        /// <summary>Metres added to the range limit to absorb float rounding — far below anything a player can notice.</summary>
        public const float LimitEpsilon = 0.0001f;

        /// <summary>
        /// Client pre-check: an evil jinn, Boost held, the target is a good jinn that is focusing, the local cooldown is over
        /// (<paramref name="now"/> &gt;= <paramref name="cooldownUntil"/>) and the distance is within <paramref name="range"/>.
        /// </summary>
        public static bool CanRequest(bool isEvilJinn, bool boosting, bool targetIsFocusingGoodJinn, float distance, float range, double now, double cooldownUntil)
        {
            if (!isEvilJinn || !boosting || !targetIsFocusingGoodJinn)
                return false;
            if (now < cooldownUntil)
                return false;
            return IsWithin(distance, range, 0f);
        }

        /// <summary>
        /// Authority check, first failure wins in this order: requester role, target role, target focusing, requester cooldown, distance
        /// (<c>SpiritBumpRange + NetRangeTolerance</c>). Returns <see cref="SpiritBumpDenyReason.None"/> when the bump is accepted.
        /// A NaN or negative <paramref name="distance"/> (unknown position) is out of range.
        /// </summary>
        public static SpiritBumpDenyReason Validate(
            Role requesterRole, Role targetRole, bool targetFocusing, float distance, float range, float tolerance, double now, double cooldownUntil)
        {
            if (requesterRole != Role.EvilJinn)
                return SpiritBumpDenyReason.RequesterNotEvil;
            if (targetRole != Role.GoodJinn)
                return SpiritBumpDenyReason.TargetNotGood;
            if (!targetFocusing)
                return SpiritBumpDenyReason.TargetNotFocusing;
            if (now < cooldownUntil)
                return SpiritBumpDenyReason.Cooldown;
            if (!IsWithin(distance, range, tolerance))
                return SpiritBumpDenyReason.OutOfRange;
            return SpiritBumpDenyReason.None;
        }

        private static bool IsWithin(float distance, float range, float tolerance)
        {
            // Written so that NaN (distance or limit) fails both comparisons and is rejected.
            float limit = range + tolerance + LimitEpsilon;
            return distance >= 0f && distance <= limit;
        }
    }
}
