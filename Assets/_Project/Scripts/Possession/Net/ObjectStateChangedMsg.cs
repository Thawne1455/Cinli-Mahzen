using System;
using CinliMahzen.Core;
using CinliMahzen.Core.Net;

namespace CinliMahzen.Possession
{
    /// <summary>
    /// <see cref="MsgCode.ObjectStateChanged"/> (TODO 1.8). Authority → everyone: an occupied object changed its inside phase on a timer
    /// (Recovering → Lurking: the jinn may act again). Payload: { int obj, byte phase } (<see cref="PossessionPhase"/>, inside phases only).
    /// </summary>
    public static class ObjectStateChangedMsg
    {
        public static NetMsg Encode(NetId obj, PossessionPhase phase)
        {
            return new NetMsg { Code = MsgCode.ObjectStateChanged, Payload = new object[] { obj.Value, (byte)phase } };
        }

        public static bool TryDecode(NetMsg msg, out NetId obj, out PossessionPhase phase)
        {
            obj = NetId.Invalid;
            phase = PossessionPhase.Free;
            object[] p = msg.Payload;
            if (msg.Code != MsgCode.ObjectStateChanged || p == null || p.Length != 2 || !(p[0] is int id) || id == 0 || !(p[1] is byte ph))
                return false;
            if (!Enum.IsDefined(typeof(PossessionPhase), ph) || !PossessionMirror.IsInside((PossessionPhase)ph))
                return false;
            obj = new NetId(id);
            phase = (PossessionPhase)ph;
            return true;
        }
    }
}
