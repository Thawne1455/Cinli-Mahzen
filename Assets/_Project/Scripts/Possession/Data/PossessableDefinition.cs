using System.Collections.Generic;
using CinliMahzen.Core;
using UnityEngine;

// Fields are assigned by Unity serialization (PD_*.asset).
#pragma warning disable CS0649

namespace CinliMahzen.Possession
{
    /// <summary>
    /// Static data of one possessable type (Tech §6.3, v2.1). One PD_*.asset per row of Oyun §8.
    /// </summary>
    [CreateAssetMenu(menuName = "CinliMahzen/Possessable Definition", fileName = "PD_New")]
    public sealed class PossessableDefinition : ScriptableObject
    {
        public const int PrimaryIndex = 0;
        public const int SecondaryIndex = 1;

        [Tooltip("bottle, candle, plate, slipper, stool, chair, barrel, chest, table, shelf, lamp, radio, cologne, quest_item")]
        [SerializeField] private string id;
        [Tooltip("Loc key of the object name, e.g. poss.shelf")]
        [SerializeField] private string nameKey;

        [Header("Classification")]
        [SerializeField] private PossessableSize size;
        [Tooltip("The good jinn may enter this object (slide, harmless carry / fling, cologne revive)")]
        [SerializeField] private bool allowGoodJinn;
        [Tooltip("Lamps can be entered even in a lit room (the only exception to the light rule)")]
        [SerializeField] private bool isLamp;

        [Header("Actions")]
        [Tooltip("Left click")]
        [SerializeField] private PossessableActionDef primary = new PossessableActionDef();
        [Tooltip("Right click (optional — leave Id empty)")]
        [SerializeField] private PossessableActionDef secondary = new PossessableActionDef();

        [Header("Movement (chair / stool: the 'hop' mode)")]
        [SerializeField] private bool canMove;
        [Min(0f)] [SerializeField] private float hopDistance = 1f;
        [Min(0f)] [SerializeField] private float hopInterval = 0.5f;
        [Min(0f)] [SerializeField] private float hopEnergy = 2f;
        [Tooltip("Hop parabola duration (s)")]
        [Min(0f)] [SerializeField] private float hopAnimTime = 0.25f;
        [Tooltip("Hop parabola apex (m)")]
        [Min(0f)] [SerializeField] private float hopHeight = 0.3f;

        [Header("Aim (A/D)")]
        [Tooltip("0 = no rotation, 35 = ±35°")]
        [Range(0f, 180f)] [SerializeField] private float yawLimitDeg;
        [Min(0f)] [SerializeField] private float yawSpeedDeg = 60f;

        [Header("Possession feel")]
        [Tooltip("Entering telegraph: position noise amplitude (m)")]
        [Min(0f)] [SerializeField] private float enterShakeAmplitude = 0.02f;
        [Tooltip("Entering telegraph: rotation noise amplitude (deg)")]
        [Min(0f)] [SerializeField] private float enterShakeAngleDeg = 2f;
        [Tooltip("Orbit camera distance while possessed (m)")]
        [Min(0f)] [SerializeField] private float cameraDistance = 3.5f;

        public string Id => id;
        public string NameKey => nameKey;
        public PossessableSize Size => size;
        public bool AllowGoodJinn => allowGoodJinn;
        public bool IsLamp => isLamp;
        public PossessableActionDef Primary => primary;
        public PossessableActionDef Secondary => secondary;
        public bool HasSecondary => secondary != null && secondary.IsDefined;
        public int ActionCount => HasSecondary ? 2 : (primary != null && primary.IsDefined ? 1 : 0);
        public bool CanMove => canMove;
        public float HopDistance => hopDistance;
        public float HopInterval => hopInterval;
        public float HopEnergy => hopEnergy;
        public float HopAnimTime => hopAnimTime;
        public float HopHeight => hopHeight;
        public float YawLimitDeg => yawLimitDeg;
        public float YawSpeedDeg => yawSpeedDeg;
        public float EnterShakeAmplitude => enterShakeAmplitude;
        public float EnterShakeAngleDeg => enterShakeAngleDeg;
        public float CameraDistance => cameraDistance;

        /// <summary>0 = primary, 1 = secondary; null if the index has no action.</summary>
        public PossessableActionDef GetAction(int index)
        {
            if (index == PrimaryIndex && primary != null && primary.IsDefined)
            {
                return primary;
            }
            if (index == SecondaryIndex && HasSecondary)
            {
                return secondary;
            }
            return null;
        }

        /// <summary>Appends data problems (empty ids, missing cause keys, inconsistent shapes).</summary>
        public void CollectProblems(List<string> problems)
        {
            if (string.IsNullOrEmpty(id))
            {
                problems.Add(name + ": empty id");
            }
            if (string.IsNullOrEmpty(nameKey))
            {
                problems.Add(name + ": empty nameKey");
            }
            if (primary == null || !primary.IsDefined)
            {
                problems.Add(name + ": primary action missing");
            }
            CollectActionProblems(primary, "primary", problems);
            if (HasSecondary)
            {
                CollectActionProblems(secondary, "secondary", problems);
            }
            if (canMove && (hopDistance <= 0f || hopInterval <= 0f))
            {
                problems.Add(name + ": canMove but hop distance/interval is zero");
            }
            if (isLamp != (size == PossessableSize.Lamp))
            {
                problems.Add(name + ": isLamp must be set exactly when size is Lamp");
            }

            // Good jinn consistency: the flag and the actions' UsableBy must agree, and good actions are harmless.
            bool goodAction = false;
            for (int i = PrimaryIndex; i <= SecondaryIndex; i++)
            {
                PossessableActionDef a = GetAction(i);
                if (a == null || !a.IsUsableBy(Role.GoodJinn))
                {
                    continue;
                }
                goodAction = true;
                if (a.Damage > 0 || a.ApplyStatus != StatusType.None || a.Flags != DamageFlags.None)
                {
                    problems.Add(name + "." + a.Id + ": usable by the good jinn but deals damage / applies a status");
                }
            }
            if (allowGoodJinn && !goodAction)
            {
                problems.Add(name + ": allowGoodJinn but no action is usable by the good jinn");
            }
            if (!allowGoodJinn && goodAction)
            {
                problems.Add(name + ": has a good-jinn action but allowGoodJinn is off");
            }
        }

        private void CollectActionProblems(PossessableActionDef a, string slot, List<string> problems)
        {
            if (a == null || !a.IsDefined)
            {
                return;
            }
            string p = name + "." + slot + " (" + a.Id + ")";
            if (string.IsNullOrEmpty(a.NameKey))
            {
                problems.Add(p + ": empty nameKey");
            }
            if ((a.Damage > 0 || a.ApplyStatus != StatusType.None) && string.IsNullOrEmpty(a.CauseId))
            {
                problems.Add(p + ": harmful action without causeId");
            }
            if (a.Shape == ActionShape.Projectile && (a.ProjectileSpeed <= 0f || a.ProjectileRange <= 0f))
            {
                problems.Add(p + ": projectile without speed/range");
            }
            if (a.Shape == ActionShape.Sphere && a.OuterRadius > 0f && a.OuterRadius <= a.ShapeSize.x)
            {
                problems.Add(p + ": outer radius must exceed inner radius");
            }
            if (a.Trigger == ActionTrigger.HoldToCharge && a.TelegraphTime <= 0f)
            {
                problems.Add(p + ": HoldToCharge needs a charge time");
            }
        }
    }
}
