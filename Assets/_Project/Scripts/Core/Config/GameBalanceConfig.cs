using UnityEngine;

namespace CinliMahzen.Core
{
    /// <summary>
    /// All tunable balance values (Teknik §4.9). One field per name in the Oyun §14 table, same name.
    /// Defaults = GDD initial values; the asset at ScriptableObjects/Config/GameBalanceConfig.asset is the source of truth.
    /// </summary>
    [CreateAssetMenu(menuName = "CinliMahzen/Config/Game Balance Config", fileName = "GameBalanceConfig")]
    public sealed class GameBalanceConfig : ScriptableObject
    {
        [Header("Match")]
        [SerializeField] private float baseRoundDuration = 360f;
        [SerializeField] private float stageTimeBonus = 90f;
        [SerializeField] private float jinnWakeDelay = 10f;
        [SerializeField] private float introCountdown = 5f;
        [SerializeField] private int roundsPerMatch = 4;
        [SerializeField] private int stagesActive = 2;
        [SerializeField] private int puzzlesPerStage = 2;
        [SerializeField] private float roleRevealDuration = 3f;
        [SerializeField] private float roundEndDuration = 8f;

        [Header("Human")]
        [SerializeField] private int humanMaxHp = 3;
        [SerializeField] private float humanInvulnAfterHit = 1.5f;
        [SerializeField] private float faintDuration = 12f;
        [SerializeField] private int faintsToLose = 3;
        [SerializeField] private float knockdownTime = 2f;
        [SerializeField] private float humanWalkSpeed = 3.5f;
        [SerializeField] private float humanSprintSpeed = 5.5f;
        [SerializeField] private float staminaMax = 100f;
        [SerializeField] private float staminaDrain = 20f;
        [SerializeField] private float staminaRegen = 15f;
        [SerializeField] private float staminaRegenDelay = 1f;
        [SerializeField] private float interactRange = 2.2f;
        [SerializeField] private float searchHoldTime = 1f;
        [SerializeField] private float pushHoldTime = 1.5f;
        [SerializeField] private float digTotalTime = 18f;
        [SerializeField] private int digCheckpoints = 3;
        [SerializeField] private int confirmFailDamage = 1;
        [SerializeField] private float confirmFailCooldown = 5f;
        [SerializeField] private float kickRange = 2f;
        [SerializeField] private float kickCooldown = 6f;
        [SerializeField] private float kickStun = 2.5f;

        [Header("Lantern & Items")]
        [SerializeField] private float lanternSafeZoneRadius = 2.5f;
        [SerializeField] private float lanternPulseRange = 8f;
        [SerializeField] private float lanternPulseDuration = 3f;
        [SerializeField] private float lanternPulseCooldown = 12f;
        [SerializeField] private int itemSlotCount = 3;

        [Header("Jinn")]
        [SerializeField] private float evilJinnSpeed = 6.5f;
        [SerializeField] private float goodJinnSpeed = 7f;
        [SerializeField] private float spiritBoostMult = 1.5f;
        [SerializeField] private float energyMax = 100f;
        [SerializeField] private float energyStart = 50f;
        [SerializeField] private float energyRegen = 5f;
        [SerializeField] private float possessRange = 3f;
        [SerializeField] private float possessTime = 1.2f;
        [SerializeField] private float reenterCooldown = 10f;
        [SerializeField] private float litRoomEjectDelay = 2f;
        [SerializeField] private float coldBreathRange = 3f;
        [SerializeField] private float rageRegenMult = 2f;
        [SerializeField] private float rageCooldownMult = 0.6f;

        [Header("Good Jinn")]
        [SerializeField] private float goodEnergyMax = 100f;
        [SerializeField] private float goodEnergyRegen = 8f;
        [SerializeField] private float focusReferenceCost = 2f;
        [SerializeField] private float spiritBumpBlurTime = 2f;
        [SerializeField] private float spiritBumpRange = 1.5f;
        [SerializeField] private float spiritBumpCooldown = 6f;
        [SerializeField] private float spiritBlockEnergyCost = 30f;
        [SerializeField] private float spiritBlockRadius = 1f;
        [SerializeField] private float cologneReviveRange = 3f;
        [SerializeField] private float exorciseRange = 3f;
        [SerializeField] private float exorciseHoldTime = 1.5f;
        [SerializeField] private float exorciseCooldown = 18f;
        [SerializeField] private float exorciseStun = 4f;
        [SerializeField] private float exorcisePossessLock = 4f;
        [SerializeField] private float pingDuration = 8f;
        [SerializeField] private float pingCooldown = 3f;
        [SerializeField] private int pingMaxActive = 2;
        [SerializeField] private float goodSightSpiritRange = 20f;
        [SerializeField] private float goodSightLurkRange = 4f;
        [SerializeField] private float goodSightChargeRange = 25f;
        [SerializeField] private float keySenseRange = 10f;

        [Header("Sabotage & Lights")]
        [SerializeField] private float scrambleEnergyCost = 25f;
        [SerializeField] private float scrambleHoldTime = 1.5f;
        [SerializeField] private float scrambleRange = 3f;
        [SerializeField] private float scramblePuzzleCooldown = 15f;
        [SerializeField] private float lampLightRange = 8f;
        [SerializeField] private float repairHoldTime = 2f;

        [Header("Score")]
        [SerializeField] private int scorePerPuzzle = 50;
        [SerializeField] private int scoreBasementOpen = 50;
        [SerializeField] private int scoreTreasureWin = 150;
        [SerializeField] private int scorePerFaint = 50;
        [SerializeField] private int jinnWinPoints = 200;
        [SerializeField] private int scorePerUnfinishedPuzzle = 40;

        [Header("Network")]
        [SerializeField] private float netRangeTolerance = 0.5f;

        // ---- Match ----

        /// <summary>Starting round length (s).</summary>
        public float BaseRoundDuration => baseRoundDuration;
        /// <summary>Time added to the round clock each time a stage gate opens (s).</summary>
        public float StageTimeBonus => stageTimeBonus;
        /// <summary>Jinns cannot act for this long after Playing starts (s).</summary>
        public float JinnWakeDelay => jinnWakeDelay;
        /// <summary>Intro countdown (s).</summary>
        public float IntroCountdown => introCountdown;
        public int RoundsPerMatch => roundsPerMatch;
        /// <summary>Stages in play (MVP: 2; full game: 3).</summary>
        public int StagesActive => stagesActive;
        public int PuzzlesPerStage => puzzlesPerStage;
        /// <summary>s</summary>
        public float RoleRevealDuration => roleRevealDuration;
        /// <summary>s</summary>
        public float RoundEndDuration => roundEndDuration;

        // ---- Human ----

        public int HumanMaxHp => humanMaxHp;
        /// <summary>Invulnerability after a hit (s).</summary>
        public float HumanInvulnAfterHit => humanInvulnAfterHit;
        /// <summary>Time spent fainted before reviving on its own (s).</summary>
        public float FaintDuration => faintDuration;
        /// <summary>Faint count at which the jinns win the round.</summary>
        public int FaintsToLose => faintsToLose;
        /// <summary>Knocked-down time after a toppling shelf (s).</summary>
        public float KnockdownTime => knockdownTime;
        /// <summary>m/s</summary>
        public float HumanWalkSpeed => humanWalkSpeed;
        /// <summary>m/s</summary>
        public float HumanSprintSpeed => humanSprintSpeed;
        public float StaminaMax => staminaMax;
        /// <summary>Per second while sprinting.</summary>
        public float StaminaDrain => staminaDrain;
        /// <summary>Per second.</summary>
        public float StaminaRegen => staminaRegen;
        /// <summary>Seconds before regen starts.</summary>
        public float StaminaRegenDelay => staminaRegenDelay;
        /// <summary>m</summary>
        public float InteractRange => interactRange;
        /// <summary>s</summary>
        public float SearchHoldTime => searchHoldTime;
        /// <summary>s</summary>
        public float PushHoldTime => pushHoldTime;
        /// <summary>Total cellar dig time (s).</summary>
        public float DigTotalTime => digTotalTime;
        /// <summary>Dig layers; progress is saved safely at each layer boundary.</summary>
        public int DigCheckpoints => digCheckpoints;
        /// <summary>HP lost on a failed confirmation.</summary>
        public int ConfirmFailDamage => confirmFailDamage;
        /// <summary>s</summary>
        public float ConfirmFailCooldown => confirmFailCooldown;
        /// <summary>m</summary>
        public float KickRange => kickRange;
        /// <summary>s</summary>
        public float KickCooldown => kickCooldown;
        /// <summary>Jinn stun on a successful kick (s).</summary>
        public float KickStun => kickStun;

        // ---- Lantern & Items ----

        /// <summary>Radius around a lit lantern in which spirit-form sabotage is blocked (m).</summary>
        public float LanternSafeZoneRadius => lanternSafeZoneRadius;
        /// <summary>m</summary>
        public float LanternPulseRange => lanternPulseRange;
        /// <summary>s</summary>
        public float LanternPulseDuration => lanternPulseDuration;
        /// <summary>s</summary>
        public float LanternPulseCooldown => lanternPulseCooldown;
        /// <summary>Quest-item slots (the fixed lantern is not counted).</summary>
        public int ItemSlotCount => itemSlotCount;

        // ---- Jinn ----

        /// <summary>m/s</summary>
        public float EvilJinnSpeed => evilJinnSpeed;
        /// <summary>m/s</summary>
        public float GoodJinnSpeed => goodJinnSpeed;
        public float SpiritBoostMult => spiritBoostMult;
        /// <summary>Evil jinn energy cap.</summary>
        public float EnergyMax => energyMax;
        public float EnergyStart => energyStart;
        /// <summary>Per second.</summary>
        public float EnergyRegen => energyRegen;
        /// <summary>m</summary>
        public float PossessRange => possessRange;
        /// <summary>s</summary>
        public float PossessTime => possessTime;
        /// <summary>Same jinn, same object (s).</summary>
        public float ReenterCooldown => reenterCooldown;
        /// <summary>Delay before a jinn inside an object is ejected when its room is lit (s).</summary>
        public float LitRoomEjectDelay => litRoomEjectDelay;
        /// <summary>m</summary>
        public float ColdBreathRange => coldBreathRange;
        /// <summary>Evil jinn energy regen multiplier once digging starts.</summary>
        public float RageRegenMult => rageRegenMult;
        /// <summary>Evil jinn cooldown multiplier once digging starts.</summary>
        public float RageCooldownMult => rageCooldownMult;

        // ---- Good Jinn ----

        /// <summary>Good jinn energy cap.</summary>
        public float GoodEnergyMax => goodEnergyMax;
        /// <summary>Per second.</summary>
        public float GoodEnergyRegen => goodEnergyRegen;
        /// <summary>Energy per second while focusing a reference.</summary>
        public float FocusReferenceCost => focusReferenceCost;
        /// <summary>Good jinn vision blur after a spirit bump (s).</summary>
        public float SpiritBumpBlurTime => spiritBumpBlurTime;
        /// <summary>Fast-flying evil jinn must pass within this distance of a focusing good jinn to bump it (m).</summary>
        public float SpiritBumpRange => spiritBumpRange;
        /// <summary>s</summary>
        public float SpiritBumpCooldown => spiritBumpCooldown;
        /// <summary>Energy the good jinn pays to body-block a thrown item in the cellar.</summary>
        public float SpiritBlockEnergyCost => spiritBlockEnergyCost;
        /// <summary>Spirit Shield collision radius (m).</summary>
        public float SpiritBlockRadius => spiritBlockRadius;
        /// <summary>Good jinn must be this close to a fainted human to revive with cologne (m).</summary>
        public float CologneReviveRange => cologneReviveRange;
        /// <summary>m</summary>
        public float ExorciseRange => exorciseRange;
        /// <summary>s</summary>
        public float ExorciseHoldTime => exorciseHoldTime;
        /// <summary>s</summary>
        public float ExorciseCooldown => exorciseCooldown;
        /// <summary>s</summary>
        public float ExorciseStun => exorciseStun;
        /// <summary>s</summary>
        public float ExorcisePossessLock => exorcisePossessLock;
        /// <summary>s</summary>
        public float PingDuration => pingDuration;
        /// <summary>s</summary>
        public float PingCooldown => pingCooldown;
        public int PingMaxActive => pingMaxActive;
        /// <summary>m</summary>
        public float GoodSightSpiritRange => goodSightSpiritRange;
        /// <summary>m</summary>
        public float GoodSightLurkRange => goodSightLurkRange;
        /// <summary>m</summary>
        public float GoodSightChargeRange => goodSightChargeRange;
        /// <summary>Range at which the good jinn sees the key's hiding place (m).</summary>
        public float KeySenseRange => keySenseRange;

        // ---- Sabotage & Lights ----

        /// <summary>Energy cost of scrambling a puzzle.</summary>
        public float ScrambleEnergyCost => scrambleEnergyCost;
        /// <summary>s</summary>
        public float ScrambleHoldTime => scrambleHoldTime;
        /// <summary>m</summary>
        public float ScrambleRange => scrambleRange;
        /// <summary>A scrambled puzzle cannot be scrambled again for this long (s).</summary>
        public float ScramblePuzzleCooldown => scramblePuzzleCooldown;
        /// <summary>Range at which the good jinn lights or repairs a lamp (m).</summary>
        public float LampLightRange => lampLightRange;
        /// <summary>s</summary>
        public float RepairHoldTime => repairHoldTime;

        // ---- Score ----

        /// <summary>Seeker score per solved puzzle.</summary>
        public int ScorePerPuzzle => scorePerPuzzle;
        public int ScoreBasementOpen => scoreBasementOpen;
        public int ScoreTreasureWin => scoreTreasureWin;
        /// <summary>Jinn score per human faint.</summary>
        public int ScorePerFaint => scorePerFaint;
        /// <summary>Jinn score for winning the round.</summary>
        public int JinnWinPoints => jinnWinPoints;
        /// <summary>Jinn score per puzzle left unfinished at round end.</summary>
        public int ScorePerUnfinishedPuzzle => scorePerUnfinishedPuzzle;

        // ---- Network ----

        /// <summary>Slack the authority grants on range checks (possess, interact, exorcise).</summary>
        public float NetRangeTolerance => netRangeTolerance;
    }
}
