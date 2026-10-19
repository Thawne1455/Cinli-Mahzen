using CinliMahzen.Core.Net;

namespace CinliMahzen.Objectives
{
    /// <summary>
    /// Encode/decode helper for <see cref="MsgCode.ReqPuzzleConfirm"/> (Teknik §7.3). Human → authority: the confirm lever was pulled.
    /// Payload (PUN-serializable): { int slot }. The requester is the message's Sender.
    /// </summary>
    public static class ReqPuzzleConfirmMsg
    {
        private const int PayloadLength = 1;

        public static NetMsg Encode(int slot)
        {
            return new NetMsg
            {
                Code = MsgCode.ReqPuzzleConfirm,
                Payload = new object[] { slot },
            };
        }

        /// <summary>False (never throws) for another code, a null or wrong-length payload, a wrong element type or a negative slot.</summary>
        public static bool TryDecode(NetMsg msg, out int slot)
        {
            slot = 0;

            object[] p = msg.Payload;
            if (msg.Code != MsgCode.ReqPuzzleConfirm || p == null || p.Length != PayloadLength)
            {
                return false;
            }
            if (!(p[0] is int slotValue) || slotValue < 0)
            {
                return false;
            }

            slot = slotValue;
            return true;
        }
    }
}
