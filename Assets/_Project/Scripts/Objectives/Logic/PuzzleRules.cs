using CinliMahzen.Core;
using UnityEngine;

namespace CinliMahzen.Objectives
{
    /// <summary>
    /// Authority rules of the three puzzle requests (Teknik §7.3, Oyun §4.2) as pure functions, plain C#.
    /// PuzzleSystemCore gathers the <see cref="PuzzleCheckFacts"/>; these methods only decide. The first failing rule wins, in the
    /// order documented on each method. A returned <see cref="PuzzleDenyReason.None"/> means the request may proceed (the state
    /// change itself, the energy payment and the broadcast are PuzzleSystemCore's job).
    /// </summary>
    public static class PuzzleRules
    {
        /// <summary>Metres added to a range limit to absorb float rounding (same idea as the interaction range check).</summary>
        public const float RangeEpsilon = 0.0001f;

        /// <summary>Folded into the round seed so sabotage rolls never replay the sequence RoundSetupPlanner drew from the same seed.</summary>
        private const int ScrambleSeedSalt = 0x5C4A3B1E;

        /// <summary>
        /// True when <paramref name="actor"/> is within <c>range + tolerance</c> of <paramref name="target"/> (squared-distance compare).
        /// A negative or NaN limit is never in range.
        /// </summary>
        public static bool IsInRange(Vector3 actor, Vector3 target, float range, float tolerance)
        {
            float limit = range + tolerance + RangeEpsilon;
            if (!(limit >= 0f))
            {
                return false;
            }
            return (target - actor).sqrMagnitude <= limit * limit;
        }

        /// <summary>
        /// Human part change. Reads Role, HumanFainted, IsLocked, InRange, HasRequiredItem. Order: WrongRole (not the human),
        /// HumanFainted, Locked, OutOfRange, MissingItem.
        /// </summary>
        public static PuzzleDenyReason CheckOp(in PuzzleCheckFacts f)
        {
            if (f.Role != Role.Human)
            {
                return PuzzleDenyReason.WrongRole;
            }
            if (f.HumanFainted)
            {
                return PuzzleDenyReason.HumanFainted;
            }
            if (f.IsLocked)
            {
                return PuzzleDenyReason.Locked;
            }
            if (!f.InRange)
            {
                return PuzzleDenyReason.OutOfRange;
            }
            if (!f.HasRequiredItem)
            {
                return PuzzleDenyReason.MissingItem;
            }
            return PuzzleDenyReason.None;
        }

        /// <summary>
        /// Evil jinn mass sabotage. Reads Role, JinnsAwake, IsLocked, InRange, RoomLight, InLanternSafeZone, Now,
        /// ScrambleCooldownUntil, Energy, EnergyCost. Order: WrongRole (not an evil jinn), JinnsAsleep, Locked, OutOfRange,
        /// LitRoom, LanternProtected, ScrambleCooldown (<c>Now &lt; ScrambleCooldownUntil</c>; exactly at the end it is allowed),
        /// InsufficientEnergy (<c>Energy &lt; EnergyCost</c>; a cost of 0 or less is free).
        /// </summary>
        public static PuzzleDenyReason CheckScramble(in PuzzleCheckFacts f)
        {
            if (f.Role != Role.EvilJinn)
            {
                return PuzzleDenyReason.WrongRole;
            }
            if (!f.JinnsAwake)
            {
                return PuzzleDenyReason.JinnsAsleep;
            }
            if (f.IsLocked)
            {
                return PuzzleDenyReason.Locked;
            }
            if (!f.InRange)
            {
                return PuzzleDenyReason.OutOfRange;
            }
            if (f.RoomLight == RoomLightState.On)
            {
                return PuzzleDenyReason.LitRoom;
            }
            if (f.InLanternSafeZone)
            {
                return PuzzleDenyReason.LanternProtected;
            }
            if (f.Now < f.ScrambleCooldownUntil)
            {
                return PuzzleDenyReason.ScrambleCooldown;
            }
            if (f.EnergyCost > 0f && !(f.Energy >= f.EnergyCost))
            {
                return PuzzleDenyReason.InsufficientEnergy;
            }
            return PuzzleDenyReason.None;
        }

        /// <summary>
        /// Confirm lever. Reads Role, HumanFainted, IsLocked, InRange, Now, ConfirmCooldownUntil. Order: WrongRole (not the human),
        /// HumanFainted, Locked (already confirmed: no second completion and no penalty), OutOfRange, ConfirmCooldown
        /// (<c>Now &lt; ConfirmCooldownUntil</c>). Whether the puzzle is actually solved is decided afterwards by PuzzleState.TryLock.
        /// </summary>
        public static PuzzleDenyReason CheckConfirm(in PuzzleCheckFacts f)
        {
            if (f.Role != Role.Human)
            {
                return PuzzleDenyReason.WrongRole;
            }
            if (f.HumanFainted)
            {
                return PuzzleDenyReason.HumanFainted;
            }
            if (f.IsLocked)
            {
                return PuzzleDenyReason.Locked;
            }
            if (!f.InRange)
            {
                return PuzzleDenyReason.OutOfRange;
            }
            if (f.Now < f.ConfirmCooldownUntil)
            {
                return PuzzleDenyReason.ConfirmCooldown;
            }
            return PuzzleDenyReason.None;
        }

        /// <summary>
        /// Seed of the authority's sabotage RNG for a round: derived from the round seed (RoundStartedEvt.Seed) but not equal to it.
        /// Always in [1, int.MaxValue). Same round seed → same value on every machine.
        /// </summary>
        public static int ScrambleSeed(int roundSeed)
        {
            return new GameRandom(roundSeed ^ ScrambleSeedSalt).Range(1, int.MaxValue);
        }
    }
}
