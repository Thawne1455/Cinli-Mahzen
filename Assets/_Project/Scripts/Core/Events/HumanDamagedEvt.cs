// Owner: A
namespace CinliMahzen.Core.Events
{
    /// <summary>Published by Player on every client when the authority confirmed damage.</summary>
    public readonly struct HumanDamagedEvt
    {
        public readonly DamageInfo Info;
        public readonly int HpAfter;

        public HumanDamagedEvt(in DamageInfo info, int hpAfter)
        {
            Info = info;
            HpAfter = hpAfter;
        }
    }
}
