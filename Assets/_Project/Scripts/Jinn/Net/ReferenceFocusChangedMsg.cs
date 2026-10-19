using CinliMahzen.Core;
using CinliMahzen.Core.Net;

namespace CinliMahzen.Jinn
{
    /// <summary>
    /// Encode/decode helper for <see cref="MsgCode.ReferenceFocusChanged"/> (Teknik §4.3, §6.5) ⇄ <c>ReferenceFocusChangedEvt{jinn, isFocusing}</c>.
    /// Authority → everyone: a good jinn's focus started or stopped (a Spirit Bump also sends "stopped").
    /// Payload (PUN-serializable): { byte player, bool focusing }.
    /// </summary>
    public static class ReferenceFocusChangedMsg
    {
        private const int PayloadLength = 2;

        public static NetMsg Encode(PlayerId player, bool focusing)
        {
            return new NetMsg
            {
                Code = MsgCode.ReferenceFocusChanged,
                Payload = new object[] { player.Value, focusing },
            };
        }

        /// <summary>
        /// False (never throws) for another code, a null or wrong-length payload, a wrong element type, or an invalid player
        /// (<see cref="PlayerId.None"/>).
        /// </summary>
        public static bool TryDecode(NetMsg msg, out PlayerId player, out bool focusing)
        {
            player = PlayerId.None;
            focusing = false;

            object[] p = msg.Payload;
            if (msg.Code != MsgCode.ReferenceFocusChanged || p == null || p.Length != PayloadLength)
                return false;
            if (!(p[0] is byte playerValue) || !(p[1] is bool focusingValue))
                return false;

            var decodedPlayer = new PlayerId(playerValue);
            if (!decodedPlayer.IsValid)
                return false;

            player = decodedPlayer;
            focusing = focusingValue;
            return true;
        }
    }
}
