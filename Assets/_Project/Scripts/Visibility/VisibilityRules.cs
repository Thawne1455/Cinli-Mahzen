using CinliMahzen.Core;

namespace CinliMahzen.Visibility
{
    /// <summary>
    /// The distance rules of Oyun §12 (TODO 3.9), plain C#. The camera's culling masks already decide who can see a layer at all; these rules
    /// limit the good jinn's sight of the evil jinns and decide the lurk / charge markers. Values from GameBalanceConfig
    /// (GoodSightSpiritRange 20, GoodSightLurkRange 4, GoodSightChargeRange 25, ColdBreathRange 3).
    /// </summary>
    public readonly struct VisibilityRules
    {
        public readonly float SpiritRange;
        public readonly float LurkRange;
        public readonly float ChargeRange;
        public readonly float ColdBreathRange;

        public VisibilityRules(float spiritRange, float lurkRange, float chargeRange, float coldBreathRange)
        {
            SpiritRange = spiritRange;
            LurkRange = lurkRange;
            ChargeRange = chargeRange;
            ColdBreathRange = coldBreathRange;
        }

        public static VisibilityRules FromConfig(GameBalanceConfig config)
        {
            return new VisibilityRules(config.GoodSightSpiritRange, config.GoodSightLurkRange, config.GoodSightChargeRange, config.ColdBreathRange);
        }

        /// <summary>Does the local <paramref name="viewer"/> see an evil jinn's spirit body <paramref name="distance"/> away? (The human never: culled.)</summary>
        public bool SeesEvilSpirit(Role viewer, float distance)
        {
            switch (viewer)
            {
                case Role.EvilJinn: return true;               // team mate: always
                case Role.GoodJinn: return distance <= SpiritRange;
                default: return false;
            }
        }

        /// <summary>The marker over an object an evil jinn is inside: none, lurking (pale) or charging (red).</summary>
        public LurkMarker MarkerFor(Role viewer, PossessionPhase phase, float distance)
        {
            if (!IsInside(phase))
                return LurkMarker.None;
            bool charging = phase == PossessionPhase.Charging;
            switch (viewer)
            {
                case Role.EvilJinn:
                    return charging ? LurkMarker.Charging : LurkMarker.Lurking;
                case Role.GoodJinn:
                    if (charging)
                        return distance <= ChargeRange ? LurkMarker.Charging : LurkMarker.None;
                    return distance <= LurkRange ? LurkMarker.Lurking : LurkMarker.None;
                default:
                    return LurkMarker.None; // the human sees the telegraph shake (and the lantern pulse), not a marker
            }
        }

        /// <summary>The jinn is inside the object (its spirit body is hidden, a marker may show). Entering still flies the body in.</summary>
        public static bool IsInside(PossessionPhase phase)
        {
            return phase == PossessionPhase.Lurking || phase == PossessionPhase.Charging || phase == PossessionPhase.Recovering;
        }

        /// <summary>The human's cold breath: an evil spirit (not inside an object) within ColdBreathRange.</summary>
        public bool ColdBreath(float distanceToNearestEvilSpirit)
        {
            return distanceToNearestEvilSpirit <= ColdBreathRange;
        }
    }

    /// <summary>What the local viewer sees over a possessed object (TODO 3.9).</summary>
    public enum LurkMarker : byte
    {
        None = 0,
        Lurking = 1,
        Charging = 2,
    }
}
