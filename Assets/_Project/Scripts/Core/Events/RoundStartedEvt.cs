// Owner: A
namespace CinliMahzen.Core.Events
{
    /// <summary>Published by Match when a round starts.</summary>
    public readonly struct RoundStartedEvt
    {
        public readonly int RoundIndex;
        public readonly int Seed;

        public RoundStartedEvt(int roundIndex, int seed)
        {
            RoundIndex = roundIndex;
            Seed = seed;
        }
    }
}
