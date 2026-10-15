namespace CinliMahzen.Core.Events
{
    /// <summary>Published by Objectives when dig progress changes. Progress is 0..1, Checkpoint is 0..3.</summary>
    public readonly struct DigProgressEvt
    {
        public readonly float Progress;
        public readonly int Checkpoint;

        public DigProgressEvt(float progress, int checkpoint)
        {
            Progress = progress;
            Checkpoint = checkpoint;
        }
    }
}
