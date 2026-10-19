using UnityEngine;

namespace CinliMahzen.Objectives
{
    /// <summary>
    /// Shows one part value of a puzzle (TODO 2.5), on the interactive puzzle (<see cref="PuzzlePart"/>) and on its reference
    /// (<see cref="PuzzleReference"/>) alike, so both always draw a value the same way.
    /// <list type="bullet">
    /// <item><see cref="Mode.RotateRoll"/>: <see cref="visual"/> turns around its forward axis by value × <see cref="degreesPerValue"/>
    /// (Portraits: 0 Straight, 1 Right, 2 Upside down, 3 Left).</item>
    /// <item><see cref="Mode.ActivateChild"/>: only child number <c>value</c> of <see cref="visual"/> is active (motif plugs, painted lines).</item>
    /// <item><see cref="Mode.RotateYaw"/> / <see cref="Mode.RotatePitch"/> (TODO 3.1): <see cref="visual"/> turns around its local up / right axis by
    /// <see cref="baseAngle"/> + value × <see cref="degreesPerValue"/> (the statue's head faces one of 6 objects, its staff tilts up / middle / down).</item>
    /// </list>
    /// </summary>
    public sealed class PuzzlePartDisplay : MonoBehaviour
    {
        public enum Mode
        {
            RotateRoll = 0,
            ActivateChild = 1,
            RotateYaw = 2,
            RotatePitch = 3,
        }

        [SerializeField] private Mode mode = Mode.RotateRoll;
        [Tooltip("What changes with the value. Empty = this transform.")]
        [SerializeField] private Transform visual;
        [Tooltip("RotateRoll: degrees per value step (Portraits: 90).")]
        [SerializeField] private float degreesPerValue = 90f;
        [Tooltip("RotateYaw / RotatePitch: angle of value 0 (degrees).")]
        [SerializeField] private float baseAngle;

        private Quaternion _rest;
        private bool _restKnown;
        private int _shown = -1;

        private Transform Visual => visual != null ? visual : transform;

        /// <summary>The value drawn last (-1 before the first <see cref="Show"/>).</summary>
        public int Shown => _shown;

        public void Show(int value)
        {
            if (value == _shown)
                return;
            _shown = value;

            Transform v = Visual;
            switch (mode)
            {
                case Mode.RotateRoll:
                    if (!_restKnown)
                    {
                        _rest = v.localRotation;
                        _restKnown = true;
                    }
                    // Negative roll = clockwise as seen by someone facing the frame: value 1 = "Right".
                    v.localRotation = _rest * Quaternion.Euler(0f, 0f, -value * degreesPerValue);
                    break;

                case Mode.RotateYaw:
                case Mode.RotatePitch:
                    if (!_restKnown)
                    {
                        _rest = v.localRotation;
                        _restKnown = true;
                    }
                    float angle = baseAngle + value * degreesPerValue;
                    v.localRotation = _rest * (mode == Mode.RotateYaw ? Quaternion.Euler(0f, angle, 0f) : Quaternion.Euler(angle, 0f, 0f));
                    break;

                case Mode.ActivateChild:
                    for (int i = 0; i < v.childCount; i++)
                        v.GetChild(i).gameObject.SetActive(i == value);
                    break;
            }
        }
    }
}
