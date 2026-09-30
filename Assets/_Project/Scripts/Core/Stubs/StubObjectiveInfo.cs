namespace CinliMahzen.Core
{
    /// <summary>Placeholder until C's ObjectiveState: Explore phase, nothing collected. Settable for tests/sandboxes.</summary>
    public sealed class StubObjectiveInfo : IObjectiveInfo
    {
        public int Fragments { get; set; }
        public bool VaultOpen { get; set; }
        public PlayerId GoldCarrier { get; set; } = PlayerId.None;
        public ObjectivePhase Phase { get; set; } = ObjectivePhase.Explore;
    }
}
