using System;
using CinliMahzen.Core;

namespace CinliMahzen.Match
{
    /// <summary>
    /// Role rotation (Oyun §10): in round <c>r</c> the Human is player <c>r</c>, the Good Jinn is player <c>r + 1</c>
    /// and everybody else is an Evil Jinn — all modulo the player count. Pure functions, no state.
    /// Player index = the slot order of the registry (0..playerCount-1), i.e. <c>PlayerId.Value</c> offline.
    /// </summary>
    public static class RoleRotation
    {
        /// <summary>Players in a match (1 Human + 1 Good Jinn + 2 Evil Jinns).</summary>
        public const int DefaultPlayerCount = 4;

        /// <summary>Fewest players that still yield a Human and a Good Jinn.</summary>
        private const int MinPlayerCount = 2;

        /// <summary>Index of the Human in <paramref name="roundIndex"/>, or -1 for an unusable player count.</summary>
        public static int HumanPlayerIndex(int roundIndex, int playerCount = DefaultPlayerCount)
        {
            return playerCount < MinPlayerCount ? -1 : PositiveMod(roundIndex, playerCount);
        }

        /// <summary>Index of the Good Jinn in <paramref name="roundIndex"/>, or -1 for an unusable player count.</summary>
        public static int GoodJinnPlayerIndex(int roundIndex, int playerCount = DefaultPlayerCount)
        {
            return playerCount < MinPlayerCount ? -1 : (PositiveMod(roundIndex, playerCount) + 1) % playerCount;
        }

        /// <summary>
        /// Role of <paramref name="playerIndex"/> in <paramref name="roundIndex"/>;
        /// <see cref="Role.None"/> for an out-of-range player index or an unusable player count.
        /// Negative round indices wrap like positive ones.
        /// </summary>
        public static Role RoleFor(int roundIndex, int playerIndex, int playerCount = DefaultPlayerCount)
        {
            if (playerCount < MinPlayerCount || playerIndex < 0 || playerIndex >= playerCount)
            {
                return Role.None;
            }

            int human = PositiveMod(roundIndex, playerCount);
            if (playerIndex == human)
            {
                return Role.Human;
            }

            return playerIndex == (human + 1) % playerCount ? Role.GoodJinn : Role.EvilJinn;
        }

        /// <summary>All roles of a round in player-index order (allocates; call once per round, never per frame).</summary>
        public static Role[] CreateRoles(int roundIndex, int playerCount = DefaultPlayerCount)
        {
            var roles = new Role[Math.Max(0, playerCount)];
            for (int i = 0; i < roles.Length; i++)
            {
                roles[i] = RoleFor(roundIndex, i, playerCount);
            }

            return roles;
        }

        private static int PositiveMod(int value, int modulus)
        {
            int m = value % modulus;
            return m < 0 ? m + modulus : m;
        }
    }
}
