using CinliMahzen.Core;
using CinliMahzen.Core.Net;
using UnityEngine;

namespace CinliMahzen.Possession
{
    /// <summary>
    /// <see cref="MsgCode.SpiritBlockedProjectile"/> (Teknik §6.4, TODO 3.7). Authority → everyone: in the cellar the good jinn <c>goodJinn</c> took
    /// the flying <c>obj</c> on its spirit body at <c>point</c> (no damage to the human; it paid SpiritBlockEnergyCost). Payload: { byte goodJinn, int obj, Vector3 point }.
    /// </summary>
    public static class SpiritBlockedMsg
    {
        public static NetMsg Encode(PlayerId goodJinn, NetId obj, Vector3 point)
        {
            return new NetMsg { Code = MsgCode.SpiritBlockedProjectile, Payload = new object[] { goodJinn.Value, obj.Value, point } };
        }

        public static bool TryDecode(NetMsg msg, out PlayerId goodJinn, out NetId obj, out Vector3 point)
        {
            goodJinn = PlayerId.None;
            obj = NetId.Invalid;
            point = Vector3.zero;
            object[] p = msg.Payload;
            if (msg.Code != MsgCode.SpiritBlockedProjectile || p == null || p.Length != 3 || !(p[0] is byte who) || !new PlayerId(who).IsValid
                || !(p[1] is int id) || id == 0 || !(p[2] is Vector3 at))
                return false;
            goodJinn = new PlayerId(who);
            obj = new NetId(id);
            point = at;
            return true;
        }
    }
}
