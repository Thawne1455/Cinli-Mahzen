using CinliMahzen.Core;
using CinliMahzen.Core.Net;
using UnityEngine;

namespace CinliMahzen.Possession
{
    /// <summary>
    /// <see cref="MsgCode.ProjectileImpact"/> (TODO 1.8). Authority → everyone: the flying <c>obj</c> stopped at <c>point</c>;
    /// <c>hitHuman</c> = it hit the human. Payload: { int obj, Vector3 point, bool hitHuman }.
    /// </summary>
    public static class ProjectileImpactMsg
    {
        public static NetMsg Encode(NetId obj, Vector3 point, bool hitHuman)
        {
            return new NetMsg { Code = MsgCode.ProjectileImpact, Payload = new object[] { obj.Value, point, hitHuman } };
        }

        public static bool TryDecode(NetMsg msg, out NetId obj, out Vector3 point, out bool hitHuman)
        {
            obj = NetId.Invalid;
            point = Vector3.zero;
            hitHuman = false;
            object[] p = msg.Payload;
            if (msg.Code != MsgCode.ProjectileImpact || p == null || p.Length != 3 || !(p[0] is int id) || id == 0
                || !(p[1] is Vector3 at) || !(p[2] is bool hit))
                return false;
            obj = new NetId(id);
            point = at;
            hitHuman = hit;
            return true;
        }
    }
}
