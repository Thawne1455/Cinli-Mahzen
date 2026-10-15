using System;
using CinliMahzen.Core;
using UnityEngine;

// Fields are assigned by Unity serialization (PD_*.asset).
#pragma warning disable CS0649

namespace CinliMahzen.Possession
{
    /// <summary>
    /// One action of a possessable (Tech §6.3, values from Oyun §8). Read-only at runtime;
    /// edited in the PD_*.asset inspector. Data only — the behaviour for each <see cref="Id"/> lives in the
    /// action runner (task 1.8).
    /// </summary>
    [Serializable]
    public sealed class PossessableActionDef
    {
        [Tooltip("throw, homing_slipper, lunge, slide, topple, burst, play_tune, fling, revive_splash (hop is the movement mode: canMove + Hop* fields on the definition)")]
        [SerializeField] private string id;
        [Tooltip("Loc key for the HUD action card, e.g. action.topple")]
        [SerializeField] private string nameKey;
        [Tooltip("DamageInfo.CauseId of harmful actions (stats / faint source), e.g. shelf.topple")]
        [SerializeField] private string causeId;
        [SerializeField] private ActionTrigger trigger;
        [Tooltip("Who may use it: the good jinn only slides, flings / carries harmlessly and revives with cologne")]
        [SerializeField] private ActionUser usableBy = ActionUser.EvilOnly;

        [Header("Timing & cost")]
        [Tooltip("Telegraph / charge time before resolve (s)")]
        [Min(0f)] [SerializeField] private float telegraphTime;
        [Tooltip("Result animation time before the jinn regains control (s). Moving actions add travel time.")]
        [Min(0f)] [SerializeField] private float recoverTime;
        [Tooltip("ToggleArm: spent when the trap triggers, not when armed")]
        [Min(0f)] [SerializeField] private float energyCost;
        [Tooltip("Seconds; scaled by RageCooldownMult during Öfke")]
        [Min(0f)] [SerializeField] private float cooldown;
        [Tooltip("Object becomes Spent after resolving (shelf, cologne splash, breakable small throwables)")]
        [SerializeField] private bool singleUse;

        [Header("Damage")]
        [Min(0)] [SerializeField] private int damage;
        [SerializeField] private DamageFlags flags;
        [Min(0f)] [SerializeField] private float knockdownTime;
        [Min(0f)] [SerializeField] private float grabTime;
        [SerializeField] private StatusType applyStatus;
        [Min(0f)] [SerializeField] private float statusDuration;

        [Header("Shape (object local space)")]
        [SerializeField] private ActionShape shape;
        [SerializeField] private Vector3 shapeSize;
        [SerializeField] private Vector3 shapeOffset;

        [Header("Outer ring (Sphere only, 0 = none)")]
        [Min(0f)] [SerializeField] private float outerRadius;
        [Min(0)] [SerializeField] private int outerDamage;
        [SerializeField] private DamageFlags outerFlags;
        [Min(0f)] [SerializeField] private float outerKnockdownTime;

        [Header("Projectile / moving action")]
        [Tooltip("Projectile or travel speed (m/s)")]
        [Min(0f)] [SerializeField] private float projectileSpeed;
        [Tooltip("Projectile or travel distance (m)")]
        [Min(0f)] [SerializeField] private float projectileRange;
        [Tooltip("Arc apex height (m), 0 = flat gravity-free straight line (Mother's Slipper)")]
        [Min(0f)] [SerializeField] private float projectileArc;

        [Header("Other effects")]
        [Tooltip("Room effect length (s): Old Radio noise suppression (12). 0 = none / until undone by someone else (lamp burst).")]
        [Min(0f)] [SerializeField] private float effectDuration;

        public string Id => id;
        public string NameKey => nameKey;
        public string CauseId => causeId;
        public ActionTrigger Trigger => trigger;
        public ActionUser UsableBy => usableBy;
        public float TelegraphTime => telegraphTime;
        public float RecoverTime => recoverTime;
        public float EnergyCost => energyCost;
        public float Cooldown => cooldown;
        public bool SingleUse => singleUse;
        public int Damage => damage;
        public DamageFlags Flags => flags;
        public float KnockdownTime => knockdownTime;
        public float GrabTime => grabTime;
        public StatusType ApplyStatus => applyStatus;
        public float StatusDuration => statusDuration;
        public ActionShape Shape => shape;
        public Vector3 ShapeSize => shapeSize;
        public Vector3 ShapeOffset => shapeOffset;
        public float OuterRadius => outerRadius;
        public int OuterDamage => outerDamage;
        public DamageFlags OuterFlags => outerFlags;
        public float OuterKnockdownTime => outerKnockdownTime;
        public float ProjectileSpeed => projectileSpeed;
        public float ProjectileRange => projectileRange;
        public float ProjectileArc => projectileArc;
        public float EffectDuration => effectDuration;

        /// <summary>Unity never serializes this class as null; an empty id means "no action".</summary>
        public bool IsDefined => !string.IsNullOrEmpty(id);

        /// <summary>True if a jinn with <paramref name="role"/> may use this action (humans never can).</summary>
        public bool IsUsableBy(Role role)
        {
            switch (role)
            {
                case Role.EvilJinn:
                    return usableBy != ActionUser.GoodOnly;
                case Role.GoodJinn:
                    return usableBy != ActionUser.EvilOnly;
                default:
                    return false;
            }
        }
    }
}
