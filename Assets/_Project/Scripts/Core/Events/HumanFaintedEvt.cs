namespace CinliMahzen.Core.Events
{
    /// <summary>Published by Player on every client when the human fainted (HP hit 0).</summary>
    public readonly struct HumanFaintedEvt
    {
        public readonly DamageInfo Info;
        public readonly int FaintCount;

        public HumanFaintedEvt(in DamageInfo info, int faintCount)
        {
            Info = info;
            FaintCount = faintCount;
        }
    }
}
