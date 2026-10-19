using CinliMahzen.Core;
using CinliMahzen.Core.Net;
using UnityEngine;

namespace CinliMahzen.Possession
{
    /// <summary>
    /// <see cref="MsgCode.ReqAction"/> (Teknik §6.4, TODO 1.8). Jinn inside an object → authority: use action <c>index</c> (0 primary, 1
    /// secondary) aimed along <c>aim</c> (the jinn's orbit camera forward). The object is the one the sender is inside. Payload: { byte index, Vector3 aim }.
    /// </summary>
    public static class ReqActionMsg
    {
        public static NetMsg Encode(int index, Vector3 aim)
        {
            return new NetMsg { Code = MsgCode.ReqAction, Payload = new object[] { (byte)index, aim } };
        }

        /// <summary>False (never throws) for another code, a malformed payload, an index above 1 or a non-finite aim.</summary>
        public static bool TryDecode(NetMsg msg, out int index, out Vector3 aim)
        {
            index = -1;
            aim = Vector3.zero;
            object[] p = msg.Payload;
            if (msg.Code != MsgCode.ReqAction || p == null || p.Length != 2 || !(p[0] is byte i) || !(p[1] is Vector3 a))
                return false;
            if (i > PossessableDefinition.SecondaryIndex || !IsFinite(a))
                return false;
            index = i;
            aim = a;
            return true;
        }

        internal static bool IsFinite(Vector3 v)
        {
            return !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) || float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z));
        }
    }
}
