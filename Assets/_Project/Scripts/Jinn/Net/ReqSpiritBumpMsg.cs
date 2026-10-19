using CinliMahzen.Core;
using CinliMahzen.Core.Net;

namespace CinliMahzen.Jinn
{
    /// <summary>
    /// Encode/decode helper for <see cref="MsgCode.ReqSpiritBump"/> (Teknik §4.3, §6.1). Evil jinn's client → authority:
    /// "I flew through the focusing good jinn at speed".
    /// Payload (PUN-serializable): { byte goodJinn }. The requesting evil jinn is the message's Sender, filled by the bridge.
    /// </summary>
    public static class ReqSpiritBumpMsg
    {
        private const int PayloadLength = 1;

        public static NetMsg Encode(PlayerId goodJinn)
        {
            return new NetMsg
            {
                Code = MsgCode.ReqSpiritBump,
                Payload = new object[] { goodJinn.Value },
            };
        }

        /// <summary>
        /// False (never throws) for another code, a null or wrong-length payload, a wrong element type, or an invalid player
        /// (<see cref="PlayerId.None"/>).
        /// </summary>
        public static bool TryDecode(NetMsg msg, out PlayerId goodJinn)
        {
            goodJinn = PlayerId.None;

            object[] p = msg.Payload;
            if (msg.Code != MsgCode.ReqSpiritBump || p == null || p.Length != PayloadLength)
                return false;
            if (!(p[0] is byte goodValue))
                return false;

            var decoded = new PlayerId(goodValue);
            if (!decoded.IsValid)
                return false;

            goodJinn = decoded;
            return true;
        }
    }
}
