namespace CinliMahzen.Core.Net
{
    /// <summary>
    /// <see cref="MsgCode.JinnStunned"/> (Oyun §5.2–5.3, TODO 3.5). Authority → everyone: jinn <c>player</c> is stunned until <c>until</c>
    /// (Net.Time): kicked out of an object by the human (KickStun) or exorcised by the good jinn (ExorciseStun). Its spirit cannot fly meanwhile.
    /// Payload: { byte player, double until }.
    /// </summary>
    public static class JinnStunnedMsg
    {
        public static NetMsg Encode(PlayerId player, double until)
        {
            return new NetMsg { Code = MsgCode.JinnStunned, Payload = new object[] { player.Value, until } };
        }

        public static bool TryDecode(NetMsg msg, out PlayerId player, out double until)
        {
            player = PlayerId.None;
            until = 0d;
            object[] p = msg.Payload;
            if (msg.Code != MsgCode.JinnStunned || p == null || p.Length != 2 || !(p[0] is byte who) || !new PlayerId(who).IsValid || !(p[1] is double u))
                return false;
            player = new PlayerId(who);
            until = u;
            return true;
        }
    }
}
