using CinliMahzen.Core;

namespace CinliMahzen.Player
{
    /// <summary>
    /// Tuning consumed by <see cref="HumanHealthCore"/>. Built from <c>GameBalanceConfig</c>
    /// (HumanMaxHp, HumanInvulnAfterHit, FaintDuration, FaintsToLose) by the MonoBehaviour side — no literals here.
    /// </summary>
    public readonly struct HumanHealthSettings
    {
        /// <summary><c>HumanMaxHp</c> — also the HP the human wakes up with.</summary>
        public readonly int MaxHp;

        /// <summary><c>HumanInvulnAfterHit</c> (s) — window after an applied hit in which further hits are ignored.</summary>
        public readonly float InvulnAfterHit;

        /// <summary><c>FaintDuration</c> (s) — time on the floor before reviving on its own.</summary>
        public readonly float FaintDuration;

        /// <summary><c>FaintsToLose</c> — faint count at which the jinns win the round.</summary>
        public readonly int FaintsToLose;

        public HumanHealthSettings(int maxHp, float invulnAfterHit, float faintDuration, int faintsToLose)
        {
            MaxHp = maxHp;
            InvulnAfterHit = invulnAfterHit;
            FaintDuration = faintDuration;
            FaintsToLose = faintsToLose;
        }

        public static HumanHealthSettings FromConfig(GameBalanceConfig config)
        {
            if (config == null)
            {
                throw new System.ArgumentNullException(nameof(config));
            }

            return new HumanHealthSettings(
                config.HumanMaxHp,
                config.HumanInvulnAfterHit,
                config.FaintDuration,
                config.FaintsToLose);
        }
    }
}
