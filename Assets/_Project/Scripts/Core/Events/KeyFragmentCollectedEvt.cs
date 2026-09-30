// Owner: C (drafted by A in A0.5)
namespace CinliMahzen.Core.Events
{
    /// <summary>Published by Objectives when a key fragment was collected.</summary>
    public readonly struct KeyFragmentCollectedEvt
    {
        public readonly int Count;

        public KeyFragmentCollectedEvt(int count)
        {
            Count = count;
        }
    }
}
