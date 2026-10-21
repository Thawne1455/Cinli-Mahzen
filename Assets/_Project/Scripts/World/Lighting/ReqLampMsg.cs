using CinliMahzen.Core;
using CinliMahzen.Core.Net;

namespace CinliMahzen.World
{
    /// <summary>
    /// <see cref="MsgCode.ReqLampLight"/> / <see cref="MsgCode.ReqLampRepair"/> (Teknik §6.5, TODO 3.5). Good jinn → authority: light the Off lamp
    /// <c>lamp</c> (E, at once) or repair the Broken one (E held <c>RepairHoldTime</c>). The requester is <see cref="NetMsg.Sender"/>.
    /// Payload: { int lamp } (a LampMarker NetId).
    /// </summary>
    public static class ReqLampMsg
    {
        public static NetMsg Encode(NetId lamp, bool repair)
        {
            return new NetMsg { Code = repair ? MsgCode.ReqLampRepair : MsgCode.ReqLampLight, Payload = new object[] { lamp.Value } };
        }

        public static bool TryDecode(NetMsg msg, out NetId lamp, out bool repair)
        {
            lamp = NetId.Invalid;
            repair = msg.Code == MsgCode.ReqLampRepair;
            object[] p = msg.Payload;
            if ((msg.Code != MsgCode.ReqLampLight && msg.Code != MsgCode.ReqLampRepair) || p == null || p.Length != 1 || !(p[0] is int id) || id == 0)
                return false;
            lamp = new NetId(id);
            return true;
        }
    }
}
