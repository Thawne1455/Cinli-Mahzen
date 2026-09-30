namespace CinliMahzen.Core.Net
{
    /// <summary>Ring buffer of the last dispatched NetMsgs, for the debug overlay (Teknik §12.1). Allocation-free.</summary>
    public static class NetDebugLog
    {
        public const int Capacity = 10;

        public struct Entry
        {
            public MsgCode Code;
            public PlayerId Sender;
            public double SentTime;
            public bool IsBroadcast;
        }

        private static readonly Entry[] Buffer = new Entry[Capacity];
        private static int _next;

        public static int Count { get; private set; }

        public static void Record(in NetMsg msg, bool isBroadcast)
        {
            Buffer[_next] = new Entry { Code = msg.Code, Sender = msg.Sender, SentTime = msg.SentTime, IsBroadcast = isBroadcast };
            _next = (_next + 1) % Capacity;
            if (Count < Capacity)
                Count++;
        }

        /// <summary>0 = newest.</summary>
        public static Entry Get(int index)
        {
            int i = (_next - 1 - index + Capacity * 2) % Capacity;
            return Buffer[i];
        }

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Clear()
        {
            _next = 0;
            Count = 0;
        }
    }
}
