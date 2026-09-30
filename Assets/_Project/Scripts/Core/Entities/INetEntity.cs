namespace CinliMahzen.Core
{
    /// <summary>Anything addressable over the network by <see cref="NetId"/> (Teknik §4.4).</summary>
    public interface INetEntity
    {
        NetId NetId { get; }
    }
}
