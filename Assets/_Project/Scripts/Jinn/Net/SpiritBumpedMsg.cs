using CinliMahzen.Core;
using CinliMahzen.Core.Net;

namespace CinliMahzen.Jinn
{
    /// <summary>
    /// Encode/decode helper for <see cref="MsgCode.SpiritBumped"/> (Teknik §4.3, §6.1) ⇄ <c>SpiritBumpedEvt{evilJinn, goodJinn}</c>.
    /// Authority → everyone: the bump was accepted (the good jinn's focus is cut by a separate <c>ReferenceFocusChanged</c>).
    /// Payload (PUN-serializable): { byte evilJinn, byte goodJinn }.
    /// </summary>
    public static class SpiritBumpedMsg
    {
        private const int PayloadLength = 2;

        public static NetMsg Encode(PlayerId evilJinn, PlayerId goodJinn)
        {
            return new NetMsg
            {
                Code = MsgCode.SpiritBumped,
                Payload = new object[] { evilJinn.Value, goodJinn.Value },
            };
        }

        /// <summary>
        /// False (never throws) for another code, a null or wrong-length payload, a wrong element type, or an invalid player
        /// (<see cref="PlayerId.None"/>) on either side.
        /// </summary>
        public static bool TryDecode(NetMsg msg, out PlayerId evilJinn, out PlayerId goodJinn)
        {
            evilJinn = PlayerId.None;
            goodJinn = PlayerId.None;

            object[] p = msg.Payload;
            if (msg.Code != MsgCode.SpiritBumped || p == null || p.Length != PayloadLength)
                return false;
            if (!(p[0] is byte evilValue) || !(p[1] is byte goodValue))
                return false;

            var decodedEvil = new PlayerId(evilValue);
            var decodedGood = new PlayerId(goodValue);
            if (!decodedEvil.IsValid || !decodedGood.IsValid)
                return false;

            evilJinn = decodedEvil;
            goodJinn = decodedGood;
            return true;
        }
    }
}
