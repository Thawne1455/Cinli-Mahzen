using CinliMahzen.Core.Net;

namespace CinliMahzen.Player
{
    /// <summary>
    /// <see cref="MsgCode.HumanRevived"/> (authority → all) ⇄ <c>HumanRevivedEvt{byCologne}</c>.
    /// Payload: <c>[0] bool byCologne, [1] int hp</c> (HP the human wakes up with, so clients need not guess).
    /// </summary>
    public static class HumanRevivedMsg
    {
        public const int PayloadLength = 2;

        public static NetMsg Encode(bool byCologne, int hp)
        {
            return new NetMsg
            {
                Code = MsgCode.HumanRevived,
                Payload = new object[] { byCologne, hp },
            };
        }

        /// <summary>False (never throws) on wrong code, null/wrong-length payload or wrong element type.</summary>
        public static bool TryDecode(in NetMsg m, out bool byCologne, out int hp)
        {
            byCologne = false;
            hp = 0;

            object[] payload = m.Payload;
            if (m.Code != MsgCode.HumanRevived || payload == null || payload.Length != PayloadLength)
            {
                return false;
            }

            if (!(payload[0] is bool cologne) || !(payload[1] is int hpValue))
            {
                return false;
            }

            byCologne = cologne;
            hp = hpValue;
            return true;
        }
    }
}
