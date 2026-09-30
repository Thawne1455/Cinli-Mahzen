namespace CinliMahzen.Core
{
    public interface IDamageable
    {
        NetId NetId { get; }
        /// <summary>Authority only.</summary>
        void ApplyDamageAuthority(in DamageInfo info);
    }
}
