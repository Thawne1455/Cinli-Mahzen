using CinliMahzen.Core.Net;
using UnityEngine;

namespace CinliMahzen.Possession
{
    /// <summary>
    /// <see cref="MsgCode.PossessedMove"/> (Teknik §6.3 hop, TODO 1.8). Jinn inside a movable object (chair, stool) → authority: hop once along
    /// <c>direction</c> (world, horizontal). Payload: { Vector3 direction }.
    /// </summary>
    public static class ReqPossessedMoveMsg
    {
        public static NetMsg Encode(Vector3 direction)
        {
            return new NetMsg { Code = MsgCode.PossessedMove, Payload = new object[] { direction } };
        }

        public static bool TryDecode(NetMsg msg, out Vector3 direction)
        {
            direction = Vector3.zero;
            object[] p = msg.Payload;
            if (msg.Code != MsgCode.PossessedMove || p == null || p.Length != 1 || !(p[0] is Vector3 d) || !ReqActionMsg.IsFinite(d))
                return false;
            direction = d;
            return true;
        }
    }
}
