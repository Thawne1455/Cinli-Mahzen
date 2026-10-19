namespace CinliMahzen.Core.Net
{
    /// <summary>
    /// Encode/decode helper for <see cref="MsgCode.InventoryChanged"/> (Teknik §5.4, TODO 2.7). Authority → everyone: the full slot list of
    /// <c>player</c> (ItemDefinition ids, "" = empty slot; length = <c>ItemSlotCount</c>). Payload: { byte player, string[] slots }.
    /// Encode copies the slots; decode returns a fresh array.
    /// </summary>
    public static class InventoryChangedMsg
    {
        private const int PayloadLength = 2;

        public static NetMsg Encode(PlayerId player, string[] slots)
        {
            var copy = new string[slots != null ? slots.Length : 0];
            for (int i = 0; i < copy.Length; i++)
                copy[i] = slots[i] ?? string.Empty;
            return new NetMsg { Code = MsgCode.InventoryChanged, Payload = new object[] { player.Value, copy } };
        }

        /// <summary>False (never throws) for another code, a malformed payload, an invalid player, an empty slot list or a null slot.</summary>
        public static bool TryDecode(NetMsg msg, out PlayerId player, out string[] slots)
        {
            player = PlayerId.None;
            slots = null;
            object[] p = msg.Payload;
            if (msg.Code != MsgCode.InventoryChanged || p == null || p.Length != PayloadLength)
                return false;
            if (!(p[0] is byte who) || !new PlayerId(who).IsValid || !(p[1] is string[] wire) || wire.Length == 0)
                return false;
            var copy = new string[wire.Length];
            for (int i = 0; i < wire.Length; i++)
            {
                if (wire[i] == null)
                    return false;
                copy[i] = wire[i];
            }
            player = new PlayerId(who);
            slots = copy;
            return true;
        }
    }
}
