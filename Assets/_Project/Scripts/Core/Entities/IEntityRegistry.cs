namespace CinliMahzen.Core
{
    /// <summary>NetId → entity lookup (Teknik §4.4). Implemented by A (EntityRegistry, A0.6).</summary>
    public interface IEntityRegistry
    {
        void Register(INetEntity e);
        void Unregister(INetEntity e);
        bool TryGet<T>(NetId id, out T entity) where T : class;
        /// <summary>Authority only; ids start at <see cref="NetId.RuntimeMin"/>.</summary>
        NetId AllocateRuntimeId();
    }
}
