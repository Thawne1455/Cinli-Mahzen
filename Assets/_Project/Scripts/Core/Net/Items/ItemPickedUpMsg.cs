namespace CinliMahzen.Core.Net
{
    /// <summary>
    /// Encode/decode helper for <see cref="MsgCode.ItemPickedUp"/> (Teknik §5.4, TODO 2.7). Authority → everyone: <c>player</c> took the ground
    /// item <c>item</c>; every machine removes the pickup (the inventory follows with <c>InventoryChanged</c>). Payload: { int item, byte player }.
    /// </summary>
    public static class ItemPickedUpMsg
    {
        private const int PayloadLength = 2;

        public static NetMsg Encode(NetId item, PlayerId player)
        {
            return new NetMsg { Code = MsgCode.ItemPickedUp, Payload = new object[] { item.Value, player.Value } };
        }

        /// <summary>False (never throws) for another code, a malformed payload, an invalid id or an invalid player.</summary>
        public static bool TryDecode(NetMsg msg, out NetId item, out PlayerId player)
        {
            item = NetId.Invalid;
            player = PlayerId.None;
            object[] p = msg.Payload;
            if (msg.Code != MsgCode.ItemPickedUp || p == null || p.Length != PayloadLength)
                return false;
            if (!(p[0] is int id) || id == 0 || !(p[1] is byte who) || !new PlayerId(who).IsValid)
                return false;
            item = new NetId(id);
            player = new PlayerId(who);
            return true;
        }
    }
}
