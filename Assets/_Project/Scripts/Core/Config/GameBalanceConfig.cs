using UnityEngine;

namespace CinliMahzen.Core
{
    /// <summary>
    /// All tunable balance values (Teknik §4.9). One field per row of Oyun §12, same name.
    /// Defaults = GDD initial values; the asset at ScriptableObjects/Config/GameBalanceConfig.asset is the source of truth.
    /// Loot chances live in C's LootTable asset, not here.
    /// </summary>
    [CreateAssetMenu(menuName = "CinliMahzen/Config/Game Balance Config", fileName = "GameBalanceConfig")]
    public sealed class GameBalanceConfig : ScriptableObject
    {
        [Header("Match")]
        [SerializeField] private float roundDuration = 600f;
        [SerializeField] private float jinnWakeDelay = 20f;
        [SerializeField] private float introCountdown = 5f;
        [SerializeField] private int roundsPerMatch = 4;
        [SerializeField] private int keyFragmentsRequired = 3;

        [Header("Human")]
        [SerializeField] private int humanMaxHp = 3;
        [SerializeField] private float humanInvulnAfterHit = 1.5f;
        [SerializeField] private float humanWalkSpeed = 3.5f;
        [SerializeField] private float humanSprintSpeed = 5.5f;
        [SerializeField] private float humanCarrySpeed = 2.4f;
        [SerializeField] private float staminaMax = 100f;
        [SerializeField] private float staminaDrain = 20f;
        [SerializeField] private float staminaRegen = 15f;
        [SerializeField] private float staminaRegenDelay = 1f;
        [SerializeField] private float interactRange = 2.2f;
        [SerializeField] private float searchHoldTime = 1f;
        [SerializeField] private float digHoldTime = 3f;
        [SerializeField] private float pickupGoldTime = 1f;
        [SerializeField] private float kickRange = 2f;
        [SerializeField] private float kickCooldown = 6f;
        [SerializeField] private float kickStun = 2.5f;
        [SerializeField] private float lanternPulseRange = 8f;
        [SerializeField] private float lanternPulseDuration = 3f;
        [SerializeField] private float lanternPulseCooldown = 12f;

        [Header("Items")]
        [SerializeField] private float saltRadius = 3f;
        [SerializeField] private float saltDuration = 25f;
        [SerializeField] private int saltStartCount = 0;
        [SerializeField] private int nazarStartCount = 0;
        [SerializeField] private int itemSlotCount = 2;

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
        [SerializeField] private float exorciseRange = 3f;
        [SerializeField] private float exorciseHoldTime = 1.5f;
        [SerializeField] private float exorciseCooldown = 18f;
        [SerializeField] private float exorciseStun = 4f;
        [SerializeField] private float exorcisePossessLock = 4f;
        [SerializeField] private float blessAfterExorcise = 15f;
        [SerializeField] private float pingDuration = 8f;
        [SerializeField] private float pingCooldown = 3f;
        [SerializeField] private int pingMaxActive = 2;
        [SerializeField] private float blessDuration = 20f;
        [SerializeField] private float blessCooldown = 25f;
        [SerializeField] private int blessMaxActive = 1;
        [SerializeField] private float goodSightSpiritRange = 20f;
        [SerializeField] private float goodSightLurkRange = 4f;
        [SerializeField] private float goodSightChargeRange = 25f;
        [SerializeField] private float heatTrailDuration = 5f;
        [SerializeField] private float noisePingRange = 15f;
        [SerializeField] private float coldBreathRange = 3f;
        [SerializeField] private float rageRegenMult = 2f;
        [SerializeField] private float rageCooldownMult = 0.6f;

        [Header("Network")]
        [SerializeField] private float netRangeTolerance = 0.5f;

        /// <summary>Round length (s).</summary>
        public float RoundDuration => roundDuration;
        /// <summary>Jinns cannot act for this long after Playing starts (s).</summary>
        public float JinnWakeDelay => jinnWakeDelay;
        /// <summary>Intro countdown (s).</summary>
        public float IntroCountdown => introCountdown;
        public int RoundsPerMatch => roundsPerMatch;
        public int KeyFragmentsRequired => keyFragmentsRequired;
        public int HumanMaxHp => humanMaxHp;
        /// <summary>Invulnerability after a hit (s).</summary>
        public float HumanInvulnAfterHit => humanInvulnAfterHit;
        /// <summary>m/s</summary>
        public float HumanWalkSpeed => humanWalkSpeed;
        /// <summary>m/s</summary>
        public float HumanSprintSpeed => humanSprintSpeed;
        /// <summary>m/s, cannot sprint while carrying.</summary>
        public float HumanCarrySpeed => humanCarrySpeed;
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
        public float DigHoldTime => digHoldTime;
        /// <summary>s</summary>
        public float PickupGoldTime => pickupGoldTime;
        /// <summary>m</summary>
        public float KickRange => kickRange;
        /// <summary>s</summary>
        public float KickCooldown => kickCooldown;
        /// <summary>Jinn stun on a successful kick (s).</summary>
        public float KickStun => kickStun;
        /// <summary>m</summary>
        public float LanternPulseRange => lanternPulseRange;
        /// <summary>s</summary>
        public float LanternPulseDuration => lanternPulseDuration;
        /// <summary>s</summary>
        public float LanternPulseCooldown => lanternPulseCooldown;
        /// <summary>m</summary>
        public float SaltRadius => saltRadius;
        /// <summary>s</summary>
        public float SaltDuration => saltDuration;
        /// <summary>Salt is found in containers.</summary>
        public int SaltStartCount => saltStartCount;
        /// <summary>Nazar is found in containers.</summary>
        public int NazarStartCount => nazarStartCount;
        public int ItemSlotCount => itemSlotCount;
        /// <summary>m/s</summary>
        public float EvilJinnSpeed => evilJinnSpeed;
        /// <summary>m/s</summary>
        public float GoodJinnSpeed => goodJinnSpeed;
        public float SpiritBoostMult => spiritBoostMult;
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
        public float BlessAfterExorcise => blessAfterExorcise;
        /// <summary>s</summary>
        public float PingDuration => pingDuration;
        /// <summary>s</summary>
        public float PingCooldown => pingCooldown;
        public int PingMaxActive => pingMaxActive;
        /// <summary>s</summary>
        public float BlessDuration => blessDuration;
        /// <summary>s</summary>
        public float BlessCooldown => blessCooldown;
        public int BlessMaxActive => blessMaxActive;
        /// <summary>m</summary>
        public float GoodSightSpiritRange => goodSightSpiritRange;
        /// <summary>m</summary>
        public float GoodSightLurkRange => goodSightLurkRange;
        /// <summary>m</summary>
        public float GoodSightChargeRange => goodSightChargeRange;
        /// <summary>s</summary>
        public float HeatTrailDuration => heatTrailDuration;
        /// <summary>m</summary>
        public float NoisePingRange => noisePingRange;
        /// <summary>m</summary>
        public float ColdBreathRange => coldBreathRange;
        public float RageRegenMult => rageRegenMult;
        public float RageCooldownMult => rageCooldownMult;
        /// <summary>Slack the authority grants on range checks (possess, interact, exorcise) — CONTRACT_CHANGES 2026-09-30.</summary>
        public float NetRangeTolerance => netRangeTolerance;
    }
}
