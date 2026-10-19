using UnityEngine;

namespace CinliMahzen.World
{
    /// <summary>Where the human starts a round (Teknik 8): exactly one, in the garden facing the front door. Forward (+Z) is the start look direction.</summary>
    public sealed class HumanSpawnMarker : HouseMarker
    {
        protected override Color GizmoColor
        {
            get { return new Color(0.2f, 0.5f, 1f); }
        }
    }
}
