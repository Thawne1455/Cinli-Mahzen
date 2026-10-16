using System;
using CinliMahzen.Core;
using CinliMahzen.Core.Net;

namespace CinliMahzen.Match
{
    /// <summary>
    /// <c>RoundSetup</c> (Teknik §4.3) — authority → all when a round enters RoundSetup.
    /// Payload: <c>{ int seed, byte[] roles }</c>; <c>roles[i]</c> is the <see cref="Role"/> of player index <c>i</c>
    /// (see <see cref="RoleRotation"/>). Encode copies the roles; decode returns a fresh array. Decoding never throws.
    /// </summary>
    public static class RoundSetupMsg
    {
        private const int PayloadLength = 2;

        public static NetMsg Encode(int seed, Role[] roles)
        {
            if (roles == null)
            {
                throw new ArgumentNullException(nameof(roles));
            }

            var wire = new byte[roles.Length];
            for (int i = 0; i < roles.Length; i++)
            {
                wire[i] = (byte)roles[i];
            }

            return new NetMsg
            {
                Code = MsgCode.RoundSetup,
                Payload = new object[] { seed, wire },
            };
        }

        /// <summary>False for a wrong code, missing/short payload, wrong element types, an empty role list or an unknown role value.</summary>
        public static bool TryDecode(in NetMsg msg, out int seed, out Role[] roles)
        {
            seed = 0;
            roles = null;

            object[] p = msg.Payload;
            if (msg.Code != MsgCode.RoundSetup || p == null || p.Length < PayloadLength)
            {
                return false;
            }

            if (!(p[0] is int rawSeed) || !(p[1] is byte[] wire) || wire.Length == 0)
            {
                return false;
            }

            var decoded = new Role[wire.Length];
            for (int i = 0; i < wire.Length; i++)
            {
                if (!MatchMsgValidation.IsDefinedByte<Role>(wire[i]))
                {
                    return false;
                }

                decoded[i] = (Role)wire[i];
            }

            seed = rawSeed;
            roles = decoded;
            return true;
        }
    }
}
