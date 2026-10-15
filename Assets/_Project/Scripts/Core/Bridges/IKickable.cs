namespace CinliMahzen.Core
{
    /// <summary>Implemented by Possession. Called by the human's kick on the authority.</summary>
    public interface IKickable
    {
        NetId NetId { get; }
        void OnKickedAuthority(PlayerId by);
    }
}
