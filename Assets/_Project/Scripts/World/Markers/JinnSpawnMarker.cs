using UnityEngine;

namespace CinliMahzen.World
{
    /// <summary>Where a jinn spirit starts a round (Teknik 8): exactly three (good jinn + 2 evil jinns), in different rooms inside the house. Height is the spirit's flying height.</summary>
    public sealed class JinnSpawnMarker : HouseMarker
    {
        protected override Color GizmoColor
        {
            get { return new Color(1f, 0.2f, 0.2f); }
        }
    }
}
