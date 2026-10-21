using CinliMahzen.Core;
using CinliMahzen.Core.Net;

namespace CinliMahzen.Possession
{
    /// <summary>
    /// <see cref="MsgCode.ActionTelegraph"/> (TODO 1.8). Authority → everyone: <c>obj</c> charges action <c>index</c> and resolves at
    /// <c>resolveAt</c> (Net.Time) — the readable warning before every hit (Oyun §12). Payload: { int obj, byte index, double resolveAt }.
    /// </summary>
    public static class ActionTelegraphMsg
    {
        public static NetMsg Encode(NetId obj, int index, double resolveAt)
        {
            return new NetMsg { Code = MsgCode.ActionTelegraph, Payload = new object[] { obj.Value, (byte)index, resolveAt } };
        }

        public static bool TryDecode(NetMsg msg, out NetId obj, out int index, out double resolveAt)
        {
            obj = NetId.Invalid;
            index = -1;
            resolveAt = 0d;
            object[] p = msg.Payload;
            if (msg.Code != MsgCode.ActionTelegraph || p == null || p.Length != 3)
                return false;
            if (!(p[0] is int id) || id == 0 || !(p[1] is byte i) || i > PossessableDefinition.SecondaryIndex || !(p[2] is double at))
                return false;
            obj = new NetId(id);
            index = i;
            resolveAt = at;
            return true;
        }
    }
}
