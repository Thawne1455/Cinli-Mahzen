using UnityEngine;

namespace CinliMahzen.Core.Net
{
    /// <summary>
    /// Encode/decode helper for <see cref="MsgCode.ItemSpawned"/> and <see cref="MsgCode.ItemDropped"/> (Teknik §5.4, §7.3, TODO 2.7): a quest
    /// item lies on the ground at <c>position</c> as the runtime entity <c>item</c> (a puzzle reward, or dropped by the human). Authority →
    /// everyone; every machine spawns the same pickup. Payload: { int item, string itemId, Vector3 position }.
    /// </summary>
    public static class ItemSpawnedMsg
    {
        private const int PayloadLength = 3;

        /// <param name="dropped">True: sent as <see cref="MsgCode.ItemDropped"/> (the human let go of it); otherwise <see cref="MsgCode.ItemSpawned"/>.</param>
        public static NetMsg Encode(NetId item, string itemId, Vector3 position, bool dropped = false)
        {
            return new NetMsg
            {
                Code = dropped ? MsgCode.ItemDropped : MsgCode.ItemSpawned,
                Payload = new object[] { item.Value, itemId ?? string.Empty, position },
            };
        }

        /// <summary>False (never throws) for another code, a malformed payload, an invalid id or an empty item id.</summary>
        public static bool TryDecode(NetMsg msg, out NetId item, out string itemId, out Vector3 position, out bool dropped)
        {
            item = NetId.Invalid;
            itemId = string.Empty;
            position = Vector3.zero;
            dropped = msg.Code == MsgCode.ItemDropped;

            object[] p = msg.Payload;
            if ((msg.Code != MsgCode.ItemSpawned && msg.Code != MsgCode.ItemDropped) || p == null || p.Length != PayloadLength)
                return false;
            if (!(p[0] is int id) || id == 0 || !(p[1] is string key) || key.Length == 0 || !(p[2] is Vector3 pos))
                return false;

            item = new NetId(id);
            itemId = key;
            position = pos;
            return true;
        }
    }
}
