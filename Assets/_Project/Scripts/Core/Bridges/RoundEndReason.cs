namespace CinliMahzen.Core
{
    /// <summary>Why a round ended (Teknik §9.1).</summary>
    public enum RoundEndReason : byte
    {
        None = 0,
        Kill = 1,
        Exit = 2,
        Timeout = 3,
    }
}
