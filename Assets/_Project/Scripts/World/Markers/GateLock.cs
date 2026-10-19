using UnityEngine;

namespace CinliMahzen.World
{
    /// <summary>
    /// Makes the <see cref="StageGate"/> on the same GameObject need a key item (Teknik 8): the front door needs the house key,
    /// the cellar hatch will need the cellar key. A gate without a lock opens when its stage puzzles are solved.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(StageGate))]
    public sealed class GateLock : HouseMarker
    {
        [Tooltip("ItemDefinition.Id the human must carry, e.g. \"house_key\".")]
        [SerializeField] private string requiredItemId = string.Empty;

        public string RequiredItemId
        {
            get { return requiredItemId ?? string.Empty; }
        }

        /// <summary>Used by the graybox builder and tests.</summary>
        public void Configure(string newRequiredItemId)
        {
            requiredItemId = newRequiredItemId ?? string.Empty;
        }

        protected override Color GizmoColor
        {
            get { return new Color(1f, 0.5f, 0.1f); }
        }
    }
}
