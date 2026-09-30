// Owner: C (drafted by A in A0.5)
namespace CinliMahzen.Core.Events
{
    /// <summary>Published by Objectives when the objective phase changes.</summary>
    public readonly struct PhaseChangedEvt
    {
        public readonly ObjectivePhase Phase;

        public PhaseChangedEvt(ObjectivePhase phase)
        {
            Phase = phase;
        }
    }
}
