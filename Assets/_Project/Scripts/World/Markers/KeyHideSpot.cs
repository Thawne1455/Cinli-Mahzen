using UnityEngine;

namespace CinliMahzen.World
{
    /// <summary>A candidate hiding place for the house key in the garden (Teknik 8, Oyun 4.4): pot, bucket, well rim, woodpile. The seed picks one per round.</summary>
    public sealed class KeyHideSpot : NetHouseMarker
    {
        protected override Color GizmoColor
        {
            get { return new Color(1f, 0.6f, 0.2f); }
        }
    }
}
