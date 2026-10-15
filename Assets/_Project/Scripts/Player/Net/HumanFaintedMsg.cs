using CinliMahzen.Core;
using CinliMahzen.Core.Net;

namespace CinliMahzen.Player
{
    /// <summary>
    /// <see cref="MsgCode.HumanFainted"/> (authority → all) ⇄ <c>HumanFaintedEvt{info, faintCount}</c>.
    /// Carries the full <see cref="DamageInfo"/> (not just a summary) so every client rebuilds the event exactly:
    /// the autopsy text needs <c>CauseId</c>, Stats/Score need <c>Attacker</c>. Costs a few dozen bytes once per faint.
    /// Payload: <c>[0] int faintCount, [1..8] DamageInfo</c> (see <see cref="DamageInfoCodec"/>).
    /// </summary>
    public static class HumanFaintedMsg
    {
        public const int PayloadLength = 1 + DamageInfoCodec.FieldCount;

        public static NetMsg Encode(in DamageInfo info, int faintCount)
        {
            var payload = new object[PayloadLength];
            payload[0] = faintCount;
            DamageInfoCodec.Write(info, payload, 1);
            return new NetMsg { Code = MsgCode.HumanFainted, Payload = payload };
        }

        /// <summary>False (never throws) on wrong code, null/wrong-length payload or wrong element type.</summary>
        public static bool TryDecode(in NetMsg m, out DamageInfo info, out int faintCount)
        {
            info = default;
            faintCount = 0;

            object[] payload = m.Payload;
            if (m.Code != MsgCode.HumanFainted || payload == null || payload.Length != PayloadLength)
            {
                return false;
            }

            if (!(payload[0] is int count) || !DamageInfoCodec.TryRead(payload, 1, out DamageInfo decoded))
            {
                return false;
            }

            info = decoded;
            faintCount = count;
            return true;
        }
    }
}
