using System;

namespace CinliMahzen.Core
{
    /// <summary>Player slot id: 0..3 in offline hotseat, mapped from Photon ActorNumber in M4 (Teknik §4.1).</summary>
    public readonly struct PlayerId : IEquatable<PlayerId>
    {
        public readonly byte Value;

        public static readonly PlayerId None = new PlayerId(255);

        public PlayerId(byte value)
        {
            Value = value;
        }

        public bool IsValid => Value != 255;

        public bool Equals(PlayerId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is PlayerId other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => IsValid ? "P" + Value : "P-";

        public static bool operator ==(PlayerId a, PlayerId b) => a.Value == b.Value;
        public static bool operator !=(PlayerId a, PlayerId b) => a.Value != b.Value;
    }
}
