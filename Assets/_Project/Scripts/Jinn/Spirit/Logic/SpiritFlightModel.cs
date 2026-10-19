using System;
using CinliMahzen.Core;
using UnityEngine;

namespace CinliMahzen.Jinn
{
    /// <summary>
    /// Flight maths of the spirit form (Teknik §6.1, Oyun §11), plain C#: input → velocity, and the house clamp. Kinematic only —
    /// the caller moves the transform by <c>velocity * dt</c>; nothing here touches physics, so spirits pass through walls.
    /// Speeds come from <c>GameBalanceConfig</c> (<c>EvilJinnSpeed</c>, <c>GoodJinnSpeed</c>, <c>SpiritBoostMult</c>); there are no literals here.
    /// </summary>
    public sealed class SpiritFlightModel
    {
        private readonly float _evilSpeed;
        private readonly float _goodSpeed;
        private readonly float _boostMultiplier;

        /// <param name="evilSpeed"><c>EvilJinnSpeed</c> (m/s).</param>
        /// <param name="goodSpeed"><c>GoodJinnSpeed</c> (m/s).</param>
        /// <param name="boostMultiplier"><c>SpiritBoostMult</c> — speed factor while Shift (Boost) is held.</param>
        public SpiritFlightModel(float evilSpeed, float goodSpeed, float boostMultiplier)
        {
            _evilSpeed = evilSpeed;
            _goodSpeed = goodSpeed;
            _boostMultiplier = boostMultiplier;
        }

        public static SpiritFlightModel FromConfig(GameBalanceConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            return new SpiritFlightModel(config.EvilJinnSpeed, config.GoodJinnSpeed, config.SpiritBoostMult);
        }

        /// <summary>
        /// Flight speed (m/s) of <paramref name="role"/>, multiplied by the boost factor while <paramref name="boosting"/>.
        /// Only the two jinn roles fly: any other role gets 0.
        /// </summary>
        public float SpeedFor(Role role, bool boosting)
        {
            float speed;
            switch (role)
            {
                case Role.EvilJinn:
                    speed = _evilSpeed;
                    break;
                case Role.GoodJinn:
                    speed = _goodSpeed;
                    break;
                default:
                    return 0f;
            }
            return boosting ? speed * _boostMultiplier : speed;
        }

        /// <summary>
        /// World-space velocity for one frame of input. <paramref name="move"/> is WASD (x = strafe right, y = forward), <paramref name="vertical"/>
        /// is Ctrl/Space (-1..+1, world up). <paramref name="right"/> and <paramref name="forward"/> are the unit axes of the view: forward is
        /// the look direction (pitch included), so W flies where the jinn looks. The combined direction never exceeds length 1, so a
        /// diagonal is not faster than a straight line; a half-pushed stick stays proportionally slower.
        /// Non-finite input counts as no input.
        /// </summary>
        public Vector3 Velocity(Role role, Vector2 move, float vertical, bool boosting, Vector3 right, Vector3 forward)
        {
            float speed = SpeedFor(role, boosting);
            if (!(speed > 0f))
                return Vector3.zero;

            float x = Sanitize(move.x);
            float z = Sanitize(move.y);
            float y = Mathf.Clamp(Sanitize(vertical), -1f, 1f);

            Vector3 direction = right * x + forward * z + Vector3.up * y;
            float sqrLength = direction.sqrMagnitude;
            if (sqrLength > 1f)
                direction /= Mathf.Sqrt(sqrLength);
            return direction * speed;
        }

        /// <summary>The nearest point to <paramref name="position"/> inside <paramref name="bounds"/> (<c>IHouseInfo.Bounds</c>), per axis.</summary>
        public static Vector3 ClampToBounds(Vector3 position, Bounds bounds)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            return new Vector3(
                Mathf.Clamp(position.x, min.x, max.x),
                Mathf.Clamp(position.y, min.y, max.y),
                Mathf.Clamp(position.z, min.z, max.z));
        }

        private static float Sanitize(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? 0f : value;
        }
    }
}
