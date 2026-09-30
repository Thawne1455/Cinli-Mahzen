// Owner: A
namespace CinliMahzen.Core.Events
{
    /// <summary>Published by Match when a round ends.</summary>
    public readonly struct RoundEndedEvt
    {
        public readonly RoundResult Result;
        public readonly RoundEndReason Reason;

        public RoundEndedEvt(RoundResult result, RoundEndReason reason)
        {
            Result = result;
            Reason = reason;
        }
    }
}
