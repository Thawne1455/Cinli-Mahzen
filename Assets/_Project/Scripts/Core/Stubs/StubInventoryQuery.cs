using System.Collections.Generic;

namespace CinliMahzen.Core
{
    /// <summary>Placeholder until the human's Inventory (TODO 2.7): nobody carries anything. Items are settable for tests/sandboxes.</summary>
    public sealed class StubInventoryQuery : IInventoryQuery
    {
        private readonly HashSet<string>[] _items = new HashSet<string>[byte.MaxValue + 1];

        public bool HasItem(PlayerId player, string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return false;
            HashSet<string> set = _items[player.Value];
            return set != null && set.Contains(itemId);
        }

        public void Give(PlayerId player, string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return;
            HashSet<string> set = _items[player.Value];
            if (set == null)
            {
                set = new HashSet<string>();
                _items[player.Value] = set;
            }
            set.Add(itemId);
        }

        public void Take(PlayerId player, string itemId)
        {
            if (itemId == null)
                return;
            _items[player.Value]?.Remove(itemId);
        }

        /// <summary>Empties every player's items.</summary>
        public void Clear()
        {
            for (int i = 0; i < _items.Length; i++)
                _items[i] = null;
        }
    }
}
