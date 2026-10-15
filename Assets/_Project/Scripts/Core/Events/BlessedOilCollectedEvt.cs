namespace CinliMahzen.Core.Events
{
    /// <summary>Published by Player when Blessed Oil is collected and the lantern switches to strong mode.</summary>
    public readonly struct BlessedOilCollectedEvt
    {
        public readonly PlayerId Collector;

        public BlessedOilCollectedEvt(PlayerId collector)
        {
            Collector = collector;
        }
    }
}
