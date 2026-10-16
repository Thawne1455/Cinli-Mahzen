using UnityEngine;

namespace CinliMahzen.Player
{
    /// <summary>
    /// Range rule for hold-to-interact (Teknik §4.5), plain C#. The authority accepts a request when the requesting pawn is
    /// within <c>InteractRange + NetRangeTolerance</c> of the target. Compared on squared distance; <see cref="LimitEpsilon"/>
    /// keeps a pawn standing exactly on the limit inside even when the runtime evaluates floats at higher precision.
    /// </summary>
    public static class InteractValidation
    {
        /// <summary>Metres added to the limit to absorb float rounding — far below anything a player can notice.</summary>
        public const float LimitEpsilon = 0.0001f;

        public static bool IsInRange(Vector3 pawnPosition, Vector3 targetPosition, float interactRange, float tolerance)
        {
            float limit = interactRange + tolerance + LimitEpsilon;
            if (!(limit >= 0f))
                return false;
            return (targetPosition - pawnPosition).sqrMagnitude <= limit * limit;
        }
    }
}
