namespace CinliMahzen.Core.Events
{
    /// <summary>Published by Possession when the good jinn's Spirit Block stops a projectile in the cellar.</summary>
    public readonly struct SpiritBlockedEvt
    {
        public readonly PlayerId GoodJinn;
        public readonly NetId ProjectileId;

        public SpiritBlockedEvt(PlayerId goodJinn, NetId projectileId)
        {
            GoodJinn = goodJinn;
            ProjectileId = projectileId;
        }
    }
}
