using CinliMahzen.Core;
using CinliMahzen.Core.Net;

namespace CinliMahzen.Player
{
    /// <summary>
    /// <see cref="MsgCode.HumanDamaged"/> (authority → all) ⇄ <c>HumanDamagedEvt{info, hpAfter}</c>.
    /// Payload: <c>[0] int hpAfter, [1..8] DamageInfo</c> (see <see cref="DamageInfoCodec"/>).
    /// </summary>
    public static class HumanDamagedMsg
    {
        public const int PayloadLength = 1 + DamageInfoCodec.FieldCount;

        public static NetMsg Encode(in DamageInfo info, int hpAfter)
        {
            var payload = new object[PayloadLength];
            payload[0] = hpAfter;
            DamageInfoCodec.Write(info, payload, 1);
            return new NetMsg { Code = MsgCode.HumanDamaged, Payload = payload };
        }

        /// <summary>False (never throws) on wrong code, null/wrong-length payload or wrong element type.</summary>
        public static bool TryDecode(in NetMsg m, out DamageInfo info, out int hpAfter)
        {
            info = default;
            hpAfter = 0;

            object[] payload = m.Payload;
            if (m.Code != MsgCode.HumanDamaged || payload == null || payload.Length != PayloadLength)
            {
                return false;
            }

            if (!(payload[0] is int hp) || !DamageInfoCodec.TryRead(payload, 1, out DamageInfo decoded))
            {
                return false;
            }

            info = decoded;
            hpAfter = hp;
            return true;
        }
    }
}
