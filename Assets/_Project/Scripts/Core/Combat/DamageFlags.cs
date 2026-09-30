using System;

namespace CinliMahzen.Core
{
    /// <summary>Damage modifiers (Teknik §4.5). PD_*.asset files store the int values — never change them.</summary>
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
