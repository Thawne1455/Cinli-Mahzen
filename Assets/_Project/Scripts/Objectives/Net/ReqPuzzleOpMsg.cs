using CinliMahzen.Core;
using CinliMahzen.Core.Net;

namespace CinliMahzen.Objectives
{
    /// <summary>
    /// Encode/decode helper for <see cref="MsgCode.ReqPuzzleOp"/> (Teknik §7.3). Human (any client) → authority: change one part.
    /// Payload (PUN-serializable): { int slot, byte opKind, int part, int value, int partB }. The requester is the message's Sender.
    /// <c>opKind</c> is exactly one <see cref="PuzzleOpKind"/> flag; the decoder rebuilds the <see cref="PuzzleOp"/> through its factories,
    /// so the fields the kind does not use come back as 0. Range / role / item checks are the authority's, not the codec's.
    /// </summary>
    public static class ReqPuzzleOpMsg
    {
        private const int PayloadLength = 5;

        public static NetMsg Encode(int slot, in PuzzleOp op)
        {
            return new NetMsg
            {
                Code = MsgCode.ReqPuzzleOp,
                Payload = new object[] { slot, (byte)op.Kind, op.Part, op.Value, op.PartB },
            };
        }

        /// <summary>
        /// False (never throws) for another code, a null or wrong-length payload, a wrong element type, a negative slot, or an op kind
        /// that is not exactly Cycle, Set or Swap (None, combined and unknown flags included).
        /// </summary>
        public static bool TryDecode(NetMsg msg, out int slot, out PuzzleOp op)
        {
            slot = 0;
            op = default;

            object[] p = msg.Payload;
            if (msg.Code != MsgCode.ReqPuzzleOp || p == null || p.Length != PayloadLength)
            {
                return false;
            }
            if (!(p[0] is int slotValue) || !(p[1] is byte kind) || !(p[2] is int part) || !(p[3] is int value) || !(p[4] is int partB))
            {
                return false;
            }
            if (slotValue < 0)
            {
                return false;
            }

            PuzzleOp decoded;
            switch ((PuzzleOpKind)kind)
            {
                case PuzzleOpKind.Cycle:
                    decoded = PuzzleOp.Cycle(part, value);
                    break;
                case PuzzleOpKind.Set:
                    decoded = PuzzleOp.Set(part, value);
                    break;
                case PuzzleOpKind.Swap:
                    decoded = PuzzleOp.Swap(part, partB);
                    break;
                default:
                    return false;
            }

            slot = slotValue;
            op = decoded;
            return true;
        }
    }
}
