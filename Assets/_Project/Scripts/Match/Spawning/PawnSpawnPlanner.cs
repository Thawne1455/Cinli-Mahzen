using System.Collections.Generic;
using CinliMahzen.Core;
using UnityEngine;

namespace CinliMahzen.Match
{
    /// <summary>
    /// The pure part of the PawnSpawner (TODO 1.3): from the registry's players and the house's spawn points to the list of pawns to create.
    /// <list type="bullet">
    /// <item>Kind by role: Human → <see cref="PawnKind.Human"/>, Good/Evil Jinn → <see cref="PawnKind.Spirit"/>, no role → nothing is spawned.</item>
    /// <item>NetId = <see cref="NetId.PawnMin"/> + the player's slot (P0 → N1 … P3 → N4). It follows the player, not the role, so it stays
    /// the same when the rotation hands the human to somebody else.</item>
    /// <item>Position: the human at <c>IHouseInfo.HumanSpawn</c>; jinns take <c>JinnSpawns</c> in player order, wrapping around when there
    /// are more jinns than points (and falling back to the human spawn when there are none).</item>
    /// </list>
    /// No Unity objects are touched, so it is covered by EditMode tests.
    /// </summary>
    public static class PawnSpawnPlanner
    {
        /// <summary>Highest slot that still gets a pawn NetId (pawn range is 1..999).</summary>
        public static int MaxPlayerSlots => NetId.LevelMin - NetId.PawnMin;

        public static PawnKind KindFor(Role role)
        {
            switch (role)
            {
                case Role.Human:
                    return PawnKind.Human;
                case Role.GoodJinn:
                case Role.EvilJinn:
                    return PawnKind.Spirit;
                default:
                    return PawnKind.None;
            }
        }

        /// <summary>The pawn NetId of the player in slot <paramref name="playerIndex"/>; <see cref="NetId.Invalid"/> outside the pawn range.</summary>
        public static NetId NetIdFor(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= MaxPlayerSlots)
                return NetId.Invalid;
            return new NetId(NetId.PawnMin + playerIndex);
        }

        /// <summary>True when at least one player has a role, i.e. a round's roles have been assigned.</summary>
        public static bool HasAnyRole(IReadOnlyList<PlayerInfo> players)
        {
            if (players == null)
                return false;
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i] != null && KindFor(players[i].Role) != PawnKind.None)
                    return true;
            }
            return false;
        }

        /// <summary>Allocates; call once per spawn, never per frame.</summary>
        public static List<PawnSpawnEntry> Plan(IReadOnlyList<PlayerInfo> players, Vector3 humanSpawn, IReadOnlyList<Vector3> jinnSpawns)
        {
            var plan = new List<PawnSpawnEntry>(players != null ? players.Count : 0);
            if (players == null)
                return plan;

            int jinnCount = jinnSpawns != null ? jinnSpawns.Count : 0;
            int jinnOrdinal = 0;
            for (int i = 0; i < players.Count; i++)
            {
                PlayerInfo info = players[i];
                if (info == null)
                    continue;
                PawnKind kind = KindFor(info.Role);
                if (kind == PawnKind.None)
                    continue;
                NetId id = NetIdFor(i);
                if (!id.IsValid)
                    break;

                Vector3 position = humanSpawn;
                if (kind == PawnKind.Spirit)
                {
                    if (jinnCount > 0)
                        position = jinnSpawns[jinnOrdinal % jinnCount];
                    jinnOrdinal++;
                }
                plan.Add(new PawnSpawnEntry(i, info.Id, info.Role, kind, id, position));
            }
            return plan;
        }
    }
}
