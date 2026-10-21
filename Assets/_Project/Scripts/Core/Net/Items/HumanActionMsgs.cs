using UnityEngine;

namespace CinliMahzen.Core.Net
{
    /// <summary>
    /// The human's lantern pulse and kick (Oyun §5.1, Teknik §5.4, TODO 3.8).
    /// <list type="bullet">
    /// <item><see cref="MsgCode.ReqLanternPulse"/> / <see cref="MsgCode.ReqKick"/> (human → authority): { Vector3 origin, Vector3 direction } — the
    /// human's eye and view direction.</item>
    /// <item><see cref="MsgCode.LanternPulsed"/> (authority → everyone): { byte player, int[] objects, double until } — the possessed objects in the
    /// cone glow until <c>until</c>.</item>
    /// <item><see cref="MsgCode.KickResult"/> (authority → everyone): { byte player, int obj } — obj 0 = the kick hit nothing possessed.</item>
    /// </list>
    /// </summary>
    public static class HumanActionMsgs
    {
        public static NetMsg EncodeAimRequest(MsgCode code, Vector3 origin, Vector3 direction)
        {
            return new NetMsg { Code = code, Payload = new object[] { origin, direction } };
        }

        /// <summary>For <see cref="MsgCode.ReqLanternPulse"/> and <see cref="MsgCode.ReqKick"/>.</summary>
        public static bool TryDecodeAimRequest(NetMsg msg, MsgCode code, out Vector3 origin, out Vector3 direction)
        {
            origin = Vector3.zero;
            direction = Vector3.forward;
            object[] p = msg.Payload;
            if (msg.Code != code || p == null || p.Length != 2 || !(p[0] is Vector3 o) || !(p[1] is Vector3 d) || d.sqrMagnitude < 1e-6f)
                return false;
            if (float.IsNaN(o.x + o.y + o.z + d.x + d.y + d.z))
                return false;
            origin = o;
            direction = d.normalized;
            return true;
        }

        public static NetMsg EncodePulsed(PlayerId player, int[] objects, double until)
        {
            return new NetMsg { Code = MsgCode.LanternPulsed, Payload = new object[] { player.Value, objects ?? new int[0], until } };
        }

        public static bool TryDecodePulsed(NetMsg msg, out PlayerId player, out int[] objects, out double until)
        {
            player = PlayerId.None;
            objects = null;
            until = 0d;
            object[] p = msg.Payload;
            if (msg.Code != MsgCode.LanternPulsed || p == null || p.Length != 3 || !(p[0] is byte who) || !new PlayerId(who).IsValid
                || !(p[1] is int[] ids) || !(p[2] is double u))
                return false;
            player = new PlayerId(who);
            objects = (int[])ids.Clone();
            until = u;
            return true;
        }

        public static NetMsg EncodeKickResult(PlayerId player, NetId obj)
        {
            return new NetMsg { Code = MsgCode.KickResult, Payload = new object[] { player.Value, obj.Value } };
        }

        public static bool TryDecodeKickResult(NetMsg msg, out PlayerId player, out NetId obj)
        {
            player = PlayerId.None;
            obj = NetId.Invalid;
            object[] p = msg.Payload;
            if (msg.Code != MsgCode.KickResult || p == null || p.Length != 2 || !(p[0] is byte who) || !new PlayerId(who).IsValid || !(p[1] is int id))
                return false;
            player = new PlayerId(who);
            obj = new NetId(id);
            return true;
        }
    }
}
