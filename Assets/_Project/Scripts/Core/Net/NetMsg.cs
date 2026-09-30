namespace CinliMahzen.Core.Net
{
    /// <summary>
    /// Network message (Teknik §4.3). Payload may only contain PUN-serializable types:
    /// byte, bool, short, int, long, float, double, string, Vector2/3, Quaternion, byte[], int[], float[], object[].
    /// </summary>
    public struct NetMsg
    {
        public MsgCode Code;
        /// <summary>Filled by the bridge.</summary>
        public PlayerId Sender;
        /// <summary>Filled by the bridge.</summary>
        public double SentTime;
        public object[] Payload;
    }
}
