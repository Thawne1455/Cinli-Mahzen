using System;
using System.Collections.Generic;
using CinliMahzen.Core;

namespace CinliMahzen.Objectives
{
    /// <summary>
    /// Quest-item slots per player (Teknik §5.4, TODO 2.7), plain C#. <c>ItemSlotCount</c> (3) slots of ItemDefinition ids, "" = empty. Only
    /// the human carries items in practice, but the state is keyed by player so a hotseat switch or a role change cannot mix them up.
    /// The same item id is never held twice. The authority mutates it; every machine mirrors it from <c>InventoryChanged</c>.
    /// </summary>
    public sealed class InventoryState
    {
        private readonly int _slotCount;
        private readonly Dictionary<PlayerId, string[]> _slots = new Dictionary<PlayerId, string[]>();

        public InventoryState(int slotCount)
        {
            if (slotCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(slotCount), "an inventory needs at least one slot");
            _slotCount = slotCount;
        }

        public int SlotCount => _slotCount;

        /// <summary>Item in <paramref name="slot"/> of <paramref name="player"/>, "" when empty or out of range.</summary>
        public string Get(PlayerId player, int slot)
        {
            if (slot < 0 || slot >= _slotCount || !_slots.TryGetValue(player, out string[] slots))
                return string.Empty;
            return slots[slot];
        }

        public bool Has(PlayerId player, string itemId)
        {
            return IndexOf(player, itemId) >= 0;
        }

        /// <summary>Slot index holding <paramref name="itemId"/>, or -1 (null / "" is never held).</summary>
        public int IndexOf(PlayerId player, string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || !_slots.TryGetValue(player, out string[] slots))
                return -1;
            for (int i = 0; i < slots.Length; i++)
            {
                if (string.Equals(slots[i], itemId, StringComparison.Ordinal))
                    return i;
            }
            return -1;
        }

        public bool IsFull(PlayerId player)
        {
            return FirstEmpty(player) < 0;
        }

        /// <summary>Puts the item in the first empty slot. Returns the slot, or -1 when full, already held or the id is empty.</summary>
        public int TryAdd(PlayerId player, string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || Has(player, itemId))
                return -1;
            int slot = FirstEmpty(player);
            if (slot < 0)
                return -1;
            SlotsOf(player)[slot] = itemId;
            return slot;
        }

        /// <summary>Empties <paramref name="slot"/> and returns its item; false when the slot is empty or out of range.</summary>
        public bool TryRemoveAt(PlayerId player, int slot, out string itemId)
        {
            itemId = Get(player, slot);
            if (itemId.Length == 0)
                return false;
            _slots[player][slot] = string.Empty;
            return true;
        }

        /// <summary>The highest non-empty slot (the item picked up last when slots fill in order), or -1.</summary>
        public int LastFilledSlot(PlayerId player)
        {
            if (!_slots.TryGetValue(player, out string[] slots))
                return -1;
            for (int i = slots.Length - 1; i >= 0; i--)
            {
                if (slots[i].Length > 0)
                    return i;
            }
            return -1;
        }

        /// <summary>A copy of the slots (length <see cref="SlotCount"/>).</summary>
        public string[] Snapshot(PlayerId player)
        {
            var copy = new string[_slotCount];
            for (int i = 0; i < _slotCount; i++)
                copy[i] = Get(player, i);
            return copy;
        }

        /// <summary>Replaces the slots with the authority's list. False (nothing changes) when the length is wrong or a slot is null.</summary>
        public bool Apply(PlayerId player, string[] slots)
        {
            if (slots == null || slots.Length != _slotCount)
                return false;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                    return false;
            }
            string[] own = SlotsOf(player);
            for (int i = 0; i < slots.Length; i++)
                own[i] = slots[i];
            return true;
        }

        /// <summary>Everyone's slots empty (new round).</summary>
        public void Clear()
        {
            _slots.Clear();
        }

        private int FirstEmpty(PlayerId player)
        {
            string[] slots = SlotsOf(player);
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].Length == 0)
                    return i;
            }
            return -1;
        }

        private string[] SlotsOf(PlayerId player)
        {
            if (!_slots.TryGetValue(player, out string[] slots))
            {
                slots = new string[_slotCount];
                for (int i = 0; i < _slotCount; i++)
                    slots[i] = string.Empty;
                _slots.Add(player, slots);
            }
            return slots;
        }
    }
}
