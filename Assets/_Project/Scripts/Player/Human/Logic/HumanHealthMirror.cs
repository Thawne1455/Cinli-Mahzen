using System;

namespace CinliMahzen.Player
{
    /// <summary>
    /// What every client (the authority included) knows about the human's health (Teknik §5.3). Plain C#, no rules of its own:
    /// it is rebuilt only from the authority's broadcasts (<c>HumanDamaged</c> / <c>HumanFainted</c> / <c>HumanRevived</c>),
    /// while <see cref="HumanHealthCore"/> decides on the authority. The MonoBehaviour <c>HumanHealth</c> feeds the decoded
    /// messages in and reads <see cref="Hp"/> / <see cref="Faints"/> / <see cref="IsFainted"/> back for
    /// <c>IHumanStatusQuery</c> and Dump State.
    /// </summary>
    public sealed class HumanHealthMirror
    {
        public HumanHealthMirror(int maxHp)
        {
            if (maxHp < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHp), "MaxHp must be at least 1.");
            }

            MaxHp = maxHp;
            Hp = maxHp;
        }

        public int MaxHp { get; }

        /// <summary>Last HP the authority reported. 0 while fainted.</summary>
        public int Hp { get; private set; }

        /// <summary>Faint count of the last <c>HumanFainted</c> message; 0 after <see cref="Reset"/>.</summary>
        public int Faints { get; private set; }

        public bool IsFainted { get; private set; }

        /// <summary><c>HumanDamaged{hpAfter}</c>: HP is the authority's value, clamped to 0..MaxHp.</summary>
        public void ApplyDamaged(int hpAfter)
        {
            Hp = Clamp(hpAfter, 0, MaxHp);
        }

        /// <summary><c>HumanFainted{faintCount}</c>: down on the floor with 0 HP. The count is the authority's, never recomputed here.</summary>
        public void ApplyFainted(int faintCount)
        {
            Hp = 0;
            Faints = faintCount < 0 ? 0 : faintCount;
            IsFainted = true;
        }

        /// <summary>
        /// <c>HumanRevived{byCologne, hp}</c>: standing again with the HP the authority reported; <see cref="Faints"/> stays
        /// (a revive never refunds a faint). Returns whether the human was fainted here — false means this client missed the
        /// faint message; the state is applied anyway because the authority's word wins.
        /// </summary>
        public bool ApplyRevived(int hp)
        {
            bool wasFainted = IsFainted;
            IsFainted = false;
            Hp = Clamp(hp, 0, MaxHp);
            return wasFainted;
        }

        /// <summary>New round: full HP, no faints, standing.</summary>
        public void Reset()
        {
            Hp = MaxHp;
            Faints = 0;
            IsFainted = false;
        }

        private static int Clamp(int value, int min, int max)
        {
            return value < min ? min : (value > max ? max : value);
        }
    }
}
