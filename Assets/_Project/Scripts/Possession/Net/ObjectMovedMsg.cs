using CinliMahzen.Core;
using CinliMahzen.Core.Net;
using UnityEngine;

namespace CinliMahzen.Possession
{
    /// <summary>
    /// <see cref="MsgCode.ObjectMoved"/> (TODO 1.8). Authority → everyone: <c>obj</c> now stands at <c>position</c> facing <c>yaw</c> degrees
    /// (a hop). Payload: { int obj, Vector3 position, float yaw }.
    /// </summary>
    public static class ObjectMovedMsg
    {
        public static NetMsg Encode(NetId obj, Vector3 position, float yaw)
        {
            return new NetMsg { Code = MsgCode.ObjectMoved, Payload = new object[] { obj.Value, position, yaw } };
        }

        public static bool TryDecode(NetMsg msg, out NetId obj, out Vector3 position, out float yaw)
        {
            obj = NetId.Invalid;
            position = Vector3.zero;
            yaw = 0f;
            object[] p = msg.Payload;
            if (msg.Code != MsgCode.ObjectMoved || p == null || p.Length != 3 || !(p[0] is int id) || id == 0 || !(p[1] is Vector3 pos) || !(p[2] is float y))
                return false;
            obj = new NetId(id);
            position = pos;
            yaw = y;
            return true;
        }
    }
}
