using System;
using CinliMahzen.Core;
using CinliMahzen.Core.Net;

namespace CinliMahzen.World
{
    /// <summary>
    /// <see cref="MsgCode.LightsChanged"/> (Teknik §7, TODO 3.4). Authority → everyone: lamp <c>lamp</c> (a LampMarker's NetId) is now
    /// <c>state</c> (Off / On / Broken). Every machine updates the lamp, its room's light and the light visual. Payload: { int lamp, byte state }.
    /// </summary>
    public static class LightsChangedMsg
    {
        public static NetMsg Encode(NetId lamp, LampState state)
        {
            return new NetMsg { Code = MsgCode.LightsChanged, Payload = new object[] { lamp.Value, (byte)state } };
        }

        public static bool TryDecode(NetMsg msg, out NetId lamp, out LampState state)
        {
            lamp = NetId.Invalid;
            state = LampState.Off;
            object[] p = msg.Payload;
            if (msg.Code != MsgCode.LightsChanged || p == null || p.Length != 2 || !(p[0] is int id) || id == 0 || !(p[1] is byte s))
                return false;
            if (!Enum.IsDefined(typeof(LampState), s))
                return false;
            lamp = new NetId(id);
            state = (LampState)s;
            return true;
        }
    }
}
