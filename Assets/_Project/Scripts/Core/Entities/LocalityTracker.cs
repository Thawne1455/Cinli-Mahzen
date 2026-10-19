namespace CinliMahzen.Core
{
    /// <summary>
    /// Remembers whether a pawn last saw itself as locally controlled and reports only real changes (plain C#, used by <see cref="PawnBase"/>).
    /// The first observation always counts as a change, so a pawn binds its input and camera exactly once when it starts, then again
    /// whenever hotseat (or an owner change) flips it.
    /// </summary>
    public sealed class LocalityTracker
    {
        private bool _known;
        private bool _local;

        /// <summary>The last value passed to <see cref="TryUpdate"/>; false before the first observation.</summary>
        public bool IsLocal => _local;

        /// <summary>True when nothing has been observed since construction or <see cref="Reset"/>.</summary>
        public bool IsUnknown => !_known;

        /// <summary>Records <paramref name="isLocal"/>. Returns true when it is the first observation or differs from the previous one.</summary>
        public bool TryUpdate(bool isLocal)
        {
            if (_known && _local == isLocal)
                return false;
            _known = true;
            _local = isLocal;
            return true;
        }

        /// <summary>Forgets the last value, so the next <see cref="TryUpdate"/> reports a change (pawn disabled, or its owner/role changed).</summary>
        public void Reset()
        {
            _known = false;
            _local = false;
        }
    }
}
