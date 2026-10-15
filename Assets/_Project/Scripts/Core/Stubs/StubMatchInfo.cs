namespace CinliMahzen.Core
{
    /// <summary>Placeholder until Match's MatchStateMachine: always Playing, jinns awake. Settable for tests/sandboxes.</summary>
    public sealed class StubMatchInfo : IMatchInfo
    {
        public MatchState State { get; set; } = MatchState.Playing;
        public int RoundIndex { get; set; }
        public double StateEndTime { get; set; } = double.MaxValue;
        public bool JinnsAwake { get; set; } = true;
        public int Faints { get; set; }
    }
}
