using System;

namespace CinliMahzen.Core
{
    /// <summary>Damage modifiers (Teknik §4.5). PD_*.asset files store the int values — never change them.
    /// Grab and Lethal are v1 leftovers kept for value stability; v2.1 never applies them (no death — HP 0 means fainting).</summary>
    [Flags]
    public enum DamageFlags : byte
    {
        None = 0,
        Knockdown = 1,
        Grab = 2,
        Lethal = 4,
        IgnoreInvuln = 8,
    }
}
