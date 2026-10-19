using CinliMahzen.Core;
using CinliMahzen.Core.Net;
using UnityEngine;

namespace CinliMahzen.Jinn
{
    /// <summary>
    /// <see cref="MsgCode.ReqPing"/> / <see cref="MsgCode.PingPlaced"/> (Oyun §5.2, TODO 3.5). Request (good jinn → authority): { Vector3 point }.
    /// Broadcast (authority → everyone): { byte player, Vector3 point, double until } — a glow at <c>point</c> everybody (the human too) sees until
    /// <c>until</c>.
    /// </summary>
    public static class PingMsg
    {
        public static NetMsg EncodeRequest(Vector3 point)
        {
            return new NetMsg { Code = MsgCode.ReqPing, Payload = new object[] { point } };
        }

        public static bool TryDecodeRequest(NetMsg msg, out Vector3 point)
        {
            point = Vector3.zero;
            object[] p = msg.Payload;
            if (msg.Code != MsgCode.ReqPing || p == null || p.Length != 1 || !(p[0] is Vector3 v) || float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z))
                return false;
            point = v;
            return true;
        }

        public static NetMsg EncodePlaced(PlayerId player, Vector3 point, double until)
        {
            return new NetMsg { Code = MsgCode.PingPlaced, Payload = new object[] { player.Value, point, until } };
        }

        public static bool TryDecodePlaced(NetMsg msg, out PlayerId player, out Vector3 point, out double until)
        {
            player = PlayerId.None;
            point = Vector3.zero;
            until = 0d;
            object[] p = msg.Payload;
            if (msg.Code != MsgCode.PingPlaced || p == null || p.Length != 3 || !(p[0] is byte who) || !new PlayerId(who).IsValid
                || !(p[1] is Vector3 at) || !(p[2] is double u))
                return false;
            player = new PlayerId(who);
            point = at;
            until = u;
            return true;
        }
    }
}
