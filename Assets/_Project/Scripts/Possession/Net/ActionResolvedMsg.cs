using CinliMahzen.Core;
using CinliMahzen.Core.Net;
using UnityEngine;

namespace CinliMahzen.Possession
{
    /// <summary>
    /// <see cref="MsgCode.ActionResolved"/> (TODO 1.8). Authority → everyone: <c>obj</c> resolved action <c>index</c>; <c>hit</c> = the human was
    /// hit (a projectile reports its hit later with ProjectileImpact); the object ends at <c>endPosition</c> / <c>endYaw</c> (moving
    /// actions; unchanged otherwise) and recovers until <c>recoverUntil</c>; <c>spent</c> = it is used up when the jinn leaves.
    /// Payload: { int obj, byte index, bool hit, bool spent, Vector3 endPosition, float endYaw, double recoverUntil }.
    /// </summary>
    public static class ActionResolvedMsg
    {
        public static NetMsg Encode(NetId obj, int index, bool hit, bool spent, Vector3 endPosition, float endYaw, double recoverUntil)
        {
            return new NetMsg
            {
                Code = MsgCode.ActionResolved,
                Payload = new object[] { obj.Value, (byte)index, hit, spent, endPosition, endYaw, recoverUntil },
            };
        }

        public static bool TryDecode(NetMsg msg, out NetId obj, out int index, out bool hit, out bool spent, out Vector3 endPosition,
            out float endYaw, out double recoverUntil)
        {
            obj = NetId.Invalid;
            index = -1;
            hit = false;
            spent = false;
            endPosition = Vector3.zero;
            endYaw = 0f;
            recoverUntil = 0d;
            object[] p = msg.Payload;
            if (msg.Code != MsgCode.ActionResolved || p == null || p.Length != 7)
                return false;
            if (!(p[0] is int id) || id == 0 || !(p[1] is byte i) || i > PossessableDefinition.SecondaryIndex || !(p[2] is bool h)
                || !(p[3] is bool s) || !(p[4] is Vector3 pos) || !(p[5] is float yaw) || !(p[6] is double until))
                return false;
            obj = new NetId(id);
            index = i;
            hit = h;
            spent = s;
            endPosition = pos;
            endYaw = yaw;
            recoverUntil = until;
            return true;
        }
    }
}
