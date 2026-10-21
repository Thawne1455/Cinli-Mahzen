namespace CinliMahzen.Core.Net
{
    /// <summary>
    /// <see cref="MsgCode.BlessedOilCollected"/> (Oyun §4.4, TODO 3.6). Authority → everyone: <c>player</c> took the Blessed Oil from the tool shed
    /// point <c>point</c>; the lantern becomes strong (Player, TODO 3.8, listens to <c>BlessedOilCollectedEvt</c>). Payload: { int point, byte player }.
    /// </summary>
    public static class BlessedOilCollectedMsg
    {
        public static NetMsg Encode(NetId point, PlayerId player)
        {
            return new NetMsg { Code = MsgCode.BlessedOilCollected, Payload = new object[] { point.Value, player.Value } };
        }

        public static bool TryDecode(NetMsg msg, out NetId point, out PlayerId player)
        {
            point = NetId.Invalid;
            player = PlayerId.None;
            object[] p = msg.Payload;
            if (msg.Code != MsgCode.BlessedOilCollected || p == null || p.Length != 2 || !(p[0] is int id) || id == 0 || !(p[1] is byte who)
                || !new PlayerId(who).IsValid)
                return false;
            point = new NetId(id);
            player = new PlayerId(who);
            return true;
        }
    }
}
