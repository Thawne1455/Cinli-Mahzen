using CinliMahzen.Core;

namespace CinliMahzen.Player
{
    /// <summary>
    /// Per-role camera culling masks (Teknik §2 "Culling (lokal role göre CameraRig)"), plain C#. The layer numbers are the fixed
    /// table of Teknik §2 (TagManager layers 6-18); CullingMasksTests checks them against the project's layer names, so a
    /// renumbered layer fails a test instead of silently leaking a hidden object to the wrong role.
    /// Distance rules for evil/good jinn visuals (*) and the reference content (**) are the Visibility module's job, not the mask's.
    /// </summary>
    public static class CullingMasks
    {
        public const int EnvironmentLayer = 6;
        public const int EvilJinnVisualLayer = 11;
        public const int GoodJinnVisualLayer = 12;
        public const int SpiritOnlyLayer = 13;
        public const int JinnOnlyFxLayer = 14;
        public const int XRayOutlineLayer = 18;

        private const int EvilJinnVisualBit = 1 << EvilJinnVisualLayer;
        private const int SpiritOnlyBit = 1 << SpiritOnlyLayer;
        private const int JinnOnlyFxBit = 1 << JinnOnlyFxLayer;
        private const int XRayOutlineBit = 1 << XRayOutlineLayer;

        /// <summary>Everything, minus what only jinns see. Keeps GoodJinnVisual (the human sees the good jinn as a faint glow).</summary>
        private const int HumanMask = ~(EvilJinnVisualBit | SpiritOnlyBit | JinnOnlyFxBit | XRayOutlineBit);

        /// <summary>Everything except JinnOnlyFX (the evil jinns' aim arrows).</summary>
        private const int GoodJinnMask = ~JinnOnlyFxBit;

        /// <summary>Everything.</summary>
        private const int EvilJinnMask = ~0;

        /// <summary>
        /// Layers the local camera renders for <paramref name="role"/>. <see cref="Role.None"/> (no role assigned yet) gets
        /// the human mask, the one that reveals the least.
        /// </summary>
        public static int For(Role role)
        {
            switch (role)
            {
                case Role.GoodJinn:
                    return GoodJinnMask;
                case Role.EvilJinn:
                    return EvilJinnMask;
                default:
                    return HumanMask;
            }
        }
    }
}
