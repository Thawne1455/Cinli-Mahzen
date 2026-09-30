// Owner: A
namespace CinliMahzen.Core.Events
{
    /// <summary>Published by Player on every client when the human died.</summary>
    public readonly struct HumanDiedEvt
    {
        public readonly DamageInfo Info;

        public HumanDiedEvt(in DamageInfo info)
        {
            Info = info;
        }
    }
}
