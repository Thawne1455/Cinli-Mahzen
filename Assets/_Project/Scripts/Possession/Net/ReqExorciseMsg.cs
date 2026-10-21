using CinliMahzen.Core;
using CinliMahzen.Core.Net;

namespace CinliMahzen.Possession
{
    /// <summary>
    /// <see cref="MsgCode.ReqExorcise"/> (Oyun §5.2, TODO 3.5). Good jinn → authority: after holding R for <c>ExorciseHoldTime</c>, throw the evil
    /// jinn out of <c>obj</c>. The requester is <see cref="NetMsg.Sender"/>. Payload: { int obj }.
    /// </summary>
    public static class ReqExorciseMsg
    {
        public static NetMsg Encode(NetId obj)
        {
            return new NetMsg { Code = MsgCode.ReqExorcise, Payload = new object[] { obj.Value } };
        }

        public static bool TryDecode(NetMsg msg, out NetId obj)
        {
            obj = NetId.Invalid;
            object[] p = msg.Payload;
            if (msg.Code != MsgCode.ReqExorcise || p == null || p.Length != 1 || !(p[0] is int id) || id == 0)
                return false;
            obj = new NetId(id);
            return true;
        }
    }
}
