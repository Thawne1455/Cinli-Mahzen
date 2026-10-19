using CinliMahzen.Core;
using CinliMahzen.Core.Net;
using UnityEngine;

namespace CinliMahzen.Possession
{
    /// <summary>
    /// <see cref="MsgCode.ProjectileSpawned"/> (Teknik §6.4, TODO 1.8). Authority → everyone: <c>obj</c> flies from <c>origin</c> along
    /// <c>direction</c> (unit, horizontal part) at <c>speed</c> m/s for at most <c>range</c> m on a parabola of apex <c>arc</c> m (0 = straight,
    /// gravity-free: Mother's Slipper), starting at <c>t0</c>. Every machine draws the same path (<see cref="ProjectilePath"/>); only the
    /// authority hit-checks it. Payload: { int obj, Vector3 origin, Vector3 direction, float speed, float range, float arc, double t0 }.
    /// </summary>
    public static class ProjectileSpawnedMsg
    {
        public static NetMsg Encode(NetId obj, in ProjectilePath path)
        {
            return new NetMsg
            {
                Code = MsgCode.ProjectileSpawned,
                Payload = new object[] { obj.Value, path.Origin, path.Direction, path.Speed, path.Range, path.Arc, path.StartTime },
            };
        }

        public static bool TryDecode(NetMsg msg, out NetId obj, out ProjectilePath path)
        {
            obj = NetId.Invalid;
            path = default;
            object[] p = msg.Payload;
            if (msg.Code != MsgCode.ProjectileSpawned || p == null || p.Length != 7)
                return false;
            if (!(p[0] is int id) || id == 0 || !(p[1] is Vector3 origin) || !(p[2] is Vector3 dir) || !(p[3] is float speed)
                || !(p[4] is float range) || !(p[5] is float arc) || !(p[6] is double t0))
                return false;
            if (!(speed > 0f) || !(range > 0f) || arc < 0f || dir.sqrMagnitude < 1e-6f)
                return false;
            obj = new NetId(id);
            path = new ProjectilePath(origin, dir, speed, range, arc, t0);
            return true;
        }
    }
}
