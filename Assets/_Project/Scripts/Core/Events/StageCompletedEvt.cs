namespace CinliMahzen.Core.Events
{
    /// <summary>Published by Objectives when a house stage is completed.</summary>
    public readonly struct StageCompletedEvt
    {
        public readonly int Stage;

        public StageCompletedEvt(int stage)
        {
            Stage = stage;
        }
    }
}
