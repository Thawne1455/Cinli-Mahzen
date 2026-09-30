using UnityEngine;

namespace CinliMahzen.Core
{
    /// <summary>
    /// Boot-time references, loaded from Resources/CM_BootSettings (auto-bootstrap has no scene to hold references).
    /// </summary>
    [CreateAssetMenu(menuName = "CinliMahzen/Config/Boot Settings", fileName = "CM_BootSettings")]
    public sealed class BootSettings : ScriptableObject
    {
        [SerializeField] private GameBalanceConfig config;
        [SerializeField] private bool autoBootstrap = true;

        public GameBalanceConfig Config => config;
        /// <summary>False = scenes must boot manually (M4 online flow).</summary>
        public bool AutoBootstrap => autoBootstrap;
    }
}
