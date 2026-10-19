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
    /// </list>
    /// </summary>
    public sealed class PuzzlePartDisplay : MonoBehaviour
    {
        public enum Mode
        {
            RotateRoll = 0,
            ActivateChild = 1,
        }

        [SerializeField] private Mode mode = Mode.RotateRoll;
        [Tooltip("What changes with the value. Empty = this transform.")]
        [SerializeField] private Transform visual;
        [Tooltip("RotateRoll: degrees per value step (Portraits: 90).")]
        [SerializeField] private float degreesPerValue = 90f;

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

                case Mode.ActivateChild:
                    for (int i = 0; i < v.childCount; i++)
                        v.GetChild(i).gameObject.SetActive(i == value);
                    break;
            }
        }
    }
}
