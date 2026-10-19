using CinliMahzen.Core;

namespace CinliMahzen.Objectives
{
    /// <summary>
    /// Everything <see cref="PuzzleRules"/> needs to judge one puzzle request, gathered by PuzzleSystemCore on the authority at the
    /// moment the request arrives (nothing is polled per frame). One struct for the three checks; each check reads only the fields
    /// documented on it. Left at default, every field means "not satisfied / empty" — including <see cref="HasRequiredItem"/>, which
    /// the caller must set to true when the puzzle needs no item.
    /// </summary>
    public struct PuzzleCheckFacts
    {
        /// <summary>Requester's role (IPlayerRegistry.GetRole).</summary>
        public Role Role;
        /// <summary>IHumanStatusQuery.IsFainted.</summary>
        public bool HumanFainted;
        /// <summary>IMatchInfo.JinnsAwake.</summary>
        public bool JinnsAwake;
        /// <summary>PuzzleState.IsLocked.</summary>
        public bool IsLocked;
        /// <summary>Requester's pawn is within the check's range (PuzzleRules.IsInRange).</summary>
        public bool InRange;
        /// <summary>True when the puzzle needs no item, or the human carries it.</summary>
        public bool HasRequiredItem;
        /// <summary>IRoomService.LightOf(slot room).</summary>
        public RoomLightState RoomLight;
        /// <summary>IHumanStatusQuery.IsInLanternSafeZone(puzzle position).</summary>
        public bool InLanternSafeZone;
        /// <summary>Net.Time.</summary>
        public double Now;
        /// <summary>PuzzleState.ScrambleCooldownUntil.</summary>
        public double ScrambleCooldownUntil;
        /// <summary>PuzzleSlotRuntime.ConfirmCooldownUntil.</summary>
        public double ConfirmCooldownUntil;
        /// <summary>IJinnEnergyService.Get(requester).</summary>
        public float Energy;
        /// <summary>GameBalanceConfig.ScrambleEnergyCost.</summary>
        public float EnergyCost;
    }
}
