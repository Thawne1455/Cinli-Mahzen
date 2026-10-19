using UnityEngine;

namespace CinliMahzen.World
{
    /// <summary>
    /// The lockable passage that ends a stage (Teknik 8, Oyun 3.1): stage 0 = front door, 1 = stair gate, 2 = cellar hatch.
    /// Opening it gives the stage time bonus. A <see cref="GateLock"/> on the same GameObject makes it need a key item.
    /// </summary>
    public sealed class StageGate : NetHouseMarker
    {
        [Tooltip("The stage this gate completes.")]
        [SerializeField] private int stageIndex;
        [Tooltip("The door piece that opens and closes. Empty = this transform.")]
        [SerializeField] private Transform door;

        public int StageIndex
        {
            get { return stageIndex; }
        }

        public Transform Door
        {
            get { return door != null ? door : transform; }
        }

        /// <summary>Used by the graybox builder and tests.</summary>
        public void Configure(int newStageIndex, Transform newDoor)
        {
            stageIndex = newStageIndex;
            door = newDoor;
        }

        protected override Color GizmoColor
        {
            get { return Color.white; }
        }
    }
}
