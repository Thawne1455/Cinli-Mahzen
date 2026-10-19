using System.Collections.Generic;
using CinliMahzen.Core;

namespace CinliMahzen.Jinn
{
    /// <summary>
    /// Who is focusing a puzzle reference right now (Ruh Perdesi, Teknik §6.5), plain C#. The authority owns the instance
    /// (<see cref="SpiritAuthority"/>): it sets and clears the flag, then broadcasts <c>ReferenceFocusChanged</c>; clients only keep a
    /// per-pawn mirror of the broadcast. One flag per <see cref="PlayerId"/>; an invalid id (<see cref="PlayerId.None"/>) is never focusing.
    /// </summary>
    public sealed class ReferenceFocusState
    {
        private readonly bool[] _focusing = new bool[PlayerId.None.Value];
        private int _count;

        /// <summary>Number of players focusing.</summary>
        public int Count => _count;

        public bool IsFocusing(PlayerId player)
        {
            return player.IsValid && _focusing[player.Value];
        }

        /// <summary>
        /// Sets the flag. Returns true when it changed — the caller broadcasts only then — and false when it already had that value
        /// or <paramref name="player"/> is invalid.
        /// </summary>
        public bool TrySet(PlayerId player, bool focusing)
        {
            if (!player.IsValid || _focusing[player.Value] == focusing)
                return false;
            _focusing[player.Value] = focusing;
            _count += focusing ? 1 : -1;
            return true;
        }

        /// <summary>
        /// Clears every flag (new round). The players that were focusing are appended to <paramref name="cleared"/> (may be null) so the
        /// authority can broadcast their change. Returns how many were cleared.
        /// </summary>
        public int ClearAll(List<PlayerId> cleared)
        {
            if (_count == 0)
                return 0;

            int n = 0;
            for (int i = 0; i < _focusing.Length; i++)
            {
                if (!_focusing[i])
                    continue;
                _focusing[i] = false;
                cleared?.Add(new PlayerId((byte)i));
                n++;
            }
            _count = 0;
            return n;
        }
    }
}
