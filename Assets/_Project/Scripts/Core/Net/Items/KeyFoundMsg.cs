namespace CinliMahzen.Core.Net
{
    /// <summary>
    /// <see cref="MsgCode.KeyFound"/> (Oyun §4.4, TODO 3.6). Authority → everyone: <c>player</c> found the house key in the hiding place
    /// <c>spot</c> (its glow goes out; the key is in the inventory, InventoryChanged follows). Payload: { int spot, byte player }.
    /// </summary>
    public static class KeyFoundMsg
    {
        public static NetMsg Encode(NetId spot, PlayerId player)
        {
            return new NetMsg { Code = MsgCode.KeyFound, Payload = new object[] { spot.Value, player.Value } };
        }

        public static bool TryDecode(NetMsg msg, out NetId spot, out PlayerId player)
        {
            spot = NetId.Invalid;
            player = PlayerId.None;
            object[] p = msg.Payload;
            if (msg.Code != MsgCode.KeyFound || p == null || p.Length != 2 || !(p[0] is int id) || id == 0 || !(p[1] is byte who) || !new PlayerId(who).IsValid)
                return false;
            spot = new NetId(id);
            player = new PlayerId(who);
            return true;
        }
    }
}
