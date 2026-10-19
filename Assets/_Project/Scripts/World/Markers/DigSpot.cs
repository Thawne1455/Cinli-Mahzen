using UnityEngine;

namespace CinliMahzen.World
{
    /// <summary>A candidate dig site in the cellar (Teknik 8, Oyun 4.5); the seed makes one of them the real treasure. The cellar arrives with TODO 3.7, so graybox v0 has none.</summary>
    public sealed class DigSpot : NetHouseMarker
    {
        protected override Color GizmoColor
        {
            get { return new Color(0.6f, 0.4f, 0.2f); }
        }
    }
}
