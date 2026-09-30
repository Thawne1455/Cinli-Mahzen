using UnityEngine;

namespace CinliMahzen.Core
{
    /// <summary>Damage request/result payload (Teknik §4.5).</summary>
    public struct DamageInfo
    {
        /// <summary>HumanMaxHp (3) kills instantly.</summary>
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
        /// <summary>Used when Flags has Grab.</summary>
        public float GrabTime;
        public Vector3 Point;
        public Vector3 Direction;
    }
}
