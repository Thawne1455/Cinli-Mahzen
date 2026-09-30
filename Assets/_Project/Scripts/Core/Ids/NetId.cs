using System;

namespace CinliMahzen.Core
{
    /// <summary>
    /// Network entity id (Teknik §4.1). 0 = invalid.
    /// Ranges: 1..999 pawns/system, 1000..59999 level objects (deterministic), 60000+ runtime spawns (authority allocates).
    /// </summary>
    public readonly struct NetId : IEquatable<NetId>
    {
        public const int PawnMin = 1;
        public const int LevelMin = 1000;
        public const int RuntimeMin = 60000;

        public readonly int Value;

        public static readonly NetId Invalid = new NetId(0);

        public NetId(int value)
        {
            Value = value;
        }

        public bool IsValid => Value != 0;

        public bool Equals(NetId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is NetId other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => "N" + Value;

        public static bool operator ==(NetId a, NetId b) => a.Value == b.Value;
        public static bool operator !=(NetId a, NetId b) => a.Value != b.Value;
    }
}
