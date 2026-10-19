using System.Collections.Generic;
using System.Text;
using CinliMahzen.Core;

namespace CinliMahzen.World
{
    /// <summary>
    /// Readable multi-line text of a <see cref="RoundPlan"/> with room keys and floors (CinliMahzen/House/Print Round Plan, test
    /// failure messages). Developer output, not player-facing text, so it is not localised.
    /// </summary>
    public static class RoundPlanFormatter
    {
        public static string Format(RoundPlan plan, HouseIndex house, IReadOnlyList<PuzzleDefinition> pool)
        {
            if (plan == null)
            {
                return "(no plan)";
            }

            var text = new StringBuilder();
            text.Append("Round plan seed ").Append(plan.Seed).Append(", hash 0x").Append(plan.ComputeHash().ToString("X16"));
            for (int s = 0; s < plan.StageIndices.Count; s++)
            {
                int stage = plan.StageIndices[s];
                text.Append("\nStage ").Append(stage).Append(':');
                for (int i = 0; i < plan.Puzzles.Count; i++)
                {
                    PlannedPuzzle p = plan.Puzzles[i];
                    if (p.StageIndex != stage)
                    {
                        continue;
                    }
                    text.Append("\n  ").Append(p.Type).Append(" @ ").Append(Room(house, p.RoomId)).Append(" [slot ").Append(p.PuzzleSlotId)
                        .Append("] -> reference @ ").Append(Room(house, p.ReferenceRoomId)).Append(" [slot ").Append(p.ReferenceSlotId).Append(']');
                    text.Append("\n    answer [").Append(string.Join(",", p.Solution)).Append(']');
                    if (RoundPlanRules.HasReferenceRotation(p.Type))
                    {
                        text.Append(", reference rotated ").Append(p.ReferenceRotation).Append(" deg");
                    }
                    if (p.TrapValues.Count > 0)
                    {
                        text.Append(", traps [").Append(string.Join(",", p.TrapValues)).Append(']');
                    }
                    string needs = NeededItem(pool, p);
                    if (needs.Length > 0)
                    {
                        text.Append(", needs ").Append(needs);
                    }
                    text.Append(", reward ").Append(p.HasReward ? p.RewardItemId : "-");
                }
            }
            text.Append("\nItems: ").Append(plan.RequiredItems.Count > 0 ? string.Join(", ", plan.RequiredItems) : "-");
            text.Append("\nHouse key: ").Append(Spot(house, house != null ? house.KeyHideSpots : null, plan.HouseKeySpotId));
            text.Append("\nCologne: ").Append(Spot(house, house != null ? house.CologneSpots : null, plan.CologneSpotId));
            text.Append("\nDig spot: ").Append(plan.HasDigSpot ? Spot(house, house != null ? house.DigSpots : null, plan.DigSpotId) : "none (no cellar yet)");
            return text.ToString();
        }

        private static string NeededItem(IReadOnlyList<PuzzleDefinition> pool, PlannedPuzzle p)
        {
            if (pool == null || p.DefinitionIndex < 0 || p.DefinitionIndex >= pool.Count || pool[p.DefinitionIndex] == null)
            {
                return string.Empty;
            }
            return pool[p.DefinitionIndex].RequiredItemId;
        }

        private static string Room(HouseIndex house, int roomId)
        {
            HouseRoomEntry room = house != null ? house.FindRoom(roomId) : null;
            return room != null ? room.Key + " (floor " + room.Floor + ")" : "room #" + roomId;
        }

        private static string Spot(HouseIndex house, IReadOnlyList<HouseSpotEntry> spots, NetId id)
        {
            if (spots != null)
            {
                for (int i = 0; i < spots.Count; i++)
                {
                    if (spots[i].NetId == id)
                    {
                        return id + " in " + Room(house, house.ResolveRoom(spots[i]));
                    }
                }
            }
            return id + " (not in the house)";
        }
    }
}
