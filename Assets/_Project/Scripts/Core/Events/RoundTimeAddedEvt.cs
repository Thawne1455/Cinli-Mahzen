namespace CinliMahzen.Core.Events
{
    /// <summary>Published by Match when the round time is extended (front door opened, sunrise counter extended).</summary>
    public readonly struct RoundTimeAddedEvt
    {
        public readonly float BonusSeconds;
        public readonly double NewEndTime;

        public RoundTimeAddedEvt(float bonusSeconds, double newEndTime)
        {
            BonusSeconds = bonusSeconds;
            NewEndTime = newEndTime;
        }
    }
}
