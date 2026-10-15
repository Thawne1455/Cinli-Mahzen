namespace CinliMahzen.Core.Events
{
    /// <summary>Published by World when an Old Radio starts or stops playing (playing suppresses noise in its room).</summary>
    public readonly struct RadioStateChangedEvt
    {
        public readonly int Room;
        public readonly bool IsPlaying;

        public RadioStateChangedEvt(int room, bool isPlaying)
        {
            Room = room;
            IsPlaying = isPlaying;
        }
    }
}
