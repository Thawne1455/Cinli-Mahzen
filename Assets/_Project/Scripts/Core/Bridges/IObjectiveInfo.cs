namespace CinliMahzen.Core
{
    /// <summary>Implemented by C (Objectives). Consumed by A (HUD, Match), B (Rage).</summary>
    public interface IObjectiveInfo
    {
        int Fragments { get; }
        bool VaultOpen { get; }
        PlayerId GoldCarrier { get; }
        ObjectivePhase Phase { get; }
    }
}
