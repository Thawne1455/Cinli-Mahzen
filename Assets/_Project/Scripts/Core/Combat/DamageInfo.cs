using UnityEngine;

namespace CinliMahzen.Core
{
    /// <summary>Damage request/result payload (Teknik §4.5). No death in v2.1: HP 0 means the human faints.</summary>
    public struct DamageInfo
    {
        public int Amount;
        /// <summary>Evil jinn that caused it.</summary>
        public PlayerId Attacker;
        /// <summary>Shelf, barrel, ...</summary>
        public NetId SourceObject;
        /// <summary>E.g. "shelf.topple" → autopsy Loc key.</summary>
        public string CauseId;
        public DamageFlags Flags;
        /// <summary>Used when Flags has Knockdown.</summary>
        public float KnockdownTime;
        public Vector3 Point;
        public Vector3 Direction;
    }
}
