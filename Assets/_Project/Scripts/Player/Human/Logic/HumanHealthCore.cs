using System;
using CinliMahzen.Core;

namespace CinliMahzen.Player
{
    /// <summary>
    /// Human HP / invulnerability / faint / revive rules (Teknik §5.3). Plain C#, runs on the authority only
    /// (the MonoBehaviour wrapper asserts <c>Net.IsAuthority</c> and broadcasts the results).
    /// Time is always passed in (<c>GameServices.Net.Time</c>) — never read here.
    ///
    /// Call order per authority frame: <see cref="Tick"/> first (a revive that is due), then any pending
    /// <see cref="ApplyDamage"/> calls — a hit never revives the human by itself.
    /// </summary>
    public sealed class HumanHealthCore
    {
        private readonly HumanHealthSettings _settings;

        public HumanHealthCore(in HumanHealthSettings settings)
        {
            if (settings.MaxHp < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(settings), "MaxHp must be at least 1.");
            }

            if (settings.FaintsToLose < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(settings), "FaintsToLose must be at least 1.");
            }

            _settings = settings;
            Hp = settings.MaxHp;
        }

        public int MaxHp => _settings.MaxHp;

        /// <summary>Current HP. 0 while fainted.</summary>
        public int Hp { get; private set; }

        /// <summary>Faints so far this round. Never decreases (a Cologne revive does not refund it).</summary>
        public int Faints { get; private set; }

        /// <summary>Down on the floor. Stays true after <see cref="HasLost"/>.</summary>
        public bool IsFainted { get; private set; }

        /// <summary><see cref="Faints"/> reached <c>FaintsToLose</c>: the human never revives (until <see cref="ResetForRound"/>).</summary>
        public bool HasLost { get; private set; }

        /// <summary>Net time at which the human revives on its own. <c>+Infinity</c> once lost; 0 when not fainted.</summary>
        public double FaintedUntil { get; private set; }

        /// <summary>Net time until which non-<c>IgnoreInvuln</c> hits are ignored. 0 = none.</summary>
        public double InvulnUntil { get; private set; }

        /// <summary>True strictly before <see cref="InvulnUntil"/>; a hit exactly at that time lands.</summary>
        public bool IsInvulnerable(double now)
        {
            return now < InvulnUntil;
        }

        /// <summary>
        /// Applies one hit. Ignored when fainted/lost, when <c>Amount &lt;= 0</c>, or while invulnerable
        /// (unless <see cref="DamageFlags.IgnoreInvuln"/>). Otherwise HP drops (clamped at 0) and the invulnerability
        /// window restarts; HP 0 means faint (Fainted) or, on the <c>FaintsToLose</c>-th faint, Lost.
        /// </summary>
        public DamageResult ApplyDamage(in DamageInfo info, double now)
        {
            if (HasLost || IsFainted || info.Amount <= 0)
            {
                return Ignored();
            }

            if ((info.Flags & DamageFlags.IgnoreInvuln) == 0 && IsInvulnerable(now))
            {
                return Ignored();
            }

            Hp = Math.Max(0, Hp - info.Amount);
            InvulnUntil = now + _settings.InvulnAfterHit;

            DamageOutcome outcome = DamageOutcome.Damaged;
            if (Hp == 0)
            {
                Faints++;
                IsFainted = true;
                if (Faints >= _settings.FaintsToLose)
                {
                    HasLost = true;
                    FaintedUntil = double.PositiveInfinity;
                    outcome = DamageOutcome.Lost;
                }
                else
                {
                    FaintedUntil = now + _settings.FaintDuration;
                    outcome = DamageOutcome.Fainted;
                }
            }

            bool knockdown = (info.Flags & DamageFlags.Knockdown) != 0;
            return new DamageResult(outcome, Hp, Faints, knockdown, knockdown ? info.KnockdownTime : 0f);
        }

        /// <summary>
        /// Natural revive: true when <c>FaintDuration</c> has elapsed (HP back to max). The caller then broadcasts
        /// <c>HumanRevived{byCologne=false}</c>. Never revives a lost human.
        /// </summary>
        public bool Tick(double now)
        {
            if (!IsFainted || HasLost || now < FaintedUntil)
            {
                return false;
            }

            Revive();
            return true;
        }

        /// <summary>
        /// Lemon Cologne: instant revive with full HP, only while fainted and not lost. The caller then broadcasts
        /// <c>HumanRevived{byCologne=true}</c>. Does not refund <see cref="Faints"/>.
        /// </summary>
        public bool TryReviveWithCologne()
        {
            if (!IsFainted || HasLost)
            {
                return false;
            }

            Revive();
            return true;
        }

        /// <summary>New round: full HP, no faints, no timers, not lost.</summary>
        public void ResetForRound()
        {
            Hp = _settings.MaxHp;
            Faints = 0;
            IsFainted = false;
            HasLost = false;
            FaintedUntil = 0d;
            InvulnUntil = 0d;
        }

        private void Revive()
        {
            IsFainted = false;
            FaintedUntil = 0d;
            Hp = _settings.MaxHp;
        }

        private DamageResult Ignored()
        {
            return new DamageResult(DamageOutcome.Ignored, Hp, Faints, false, 0f);
        }
    }
}
