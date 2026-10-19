using System;
using CinliMahzen.Core;
using CinliMahzen.Core.Net;

namespace CinliMahzen.Possession
{
    /// <summary>
    /// <see cref="MsgCode.ActionDenied"/> (TODO 1.8). Authority → everyone (the requester's HUD reacts): <c>player</c>'s action or hop was
    /// refused with <c>reason</c>. Payload: { byte player, byte reason }.
    /// </summary>
    public static class ActionDeniedMsg
    {
        public static NetMsg Encode(PlayerId player, ActionDenyReason reason)
        {
            return new NetMsg { Code = MsgCode.ActionDenied, Payload = new object[] { player.Value, (byte)reason } };
        }

        public static bool TryDecode(NetMsg msg, out PlayerId player, out ActionDenyReason reason)
        {
            player = PlayerId.None;
            reason = ActionDenyReason.None;
            object[] p = msg.Payload;
            if (msg.Code != MsgCode.ActionDenied || p == null || p.Length != 2 || !(p[0] is byte who) || !(p[1] is byte r))
                return false;
            if (!new PlayerId(who).IsValid || r == 0 || !Enum.IsDefined(typeof(ActionDenyReason), r))
                return false;
            player = new PlayerId(who);
            reason = (ActionDenyReason)r;
            return true;
        }
    }
}
