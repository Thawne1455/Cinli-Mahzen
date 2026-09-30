// Owner: C (drafted by A in A0.5)
namespace CinliMahzen.Core.Events
{
    /// <summary>Published by Objectives when the gold is picked up or dropped.</summary>
    public readonly struct GoldStateEvt
    {
        public readonly bool Carried;
        public readonly PlayerId Carrier;

        public GoldStateEvt(bool carried, PlayerId carrier)
        {
            Carried = carried;
            Carrier = carrier;
        }
    }
}
