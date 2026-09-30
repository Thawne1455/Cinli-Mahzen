namespace CinliMahzen.Core
{
    /// <summary>Per-player record held by <see cref="IPlayerRegistry"/> (Teknik §4.2).</summary>
    public sealed class PlayerInfo
    {
        public PlayerId Id;
        public string Nickname;
        public Role Role;
        public int Score;
    }
}
