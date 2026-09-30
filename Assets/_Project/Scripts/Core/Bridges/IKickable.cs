namespace CinliMahzen.Core
{
    /// <summary>Implemented by B (Possession). Called by A's kick on the authority.</summary>
    public interface IKickable
    {
        NetId NetId { get; }
        void OnKickedAuthority(PlayerId by);
    }
}
