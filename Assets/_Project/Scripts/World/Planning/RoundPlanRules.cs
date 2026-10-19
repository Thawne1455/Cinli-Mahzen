using System.Collections.Generic;
using CinliMahzen.Core;

namespace CinliMahzen.World
{
    /// <summary>
    /// The rules <see cref="RoundSetupPlanner"/> and <see cref="RoundPlanValidator"/> share (Teknik 7.5), so "fits", "eligible spot"
    /// and "answer shape" have exactly one definition. Plain C#.
    ///
    /// Answer shape per type (Oyun 4.3): Sigil (seal stones) gets a reference rotation of 0/90/180/270 degrees; Wires (charmed
    /// switch) gets <see cref="WiresTrapCount"/> trap motifs that are not in its answer. Every other type has neither.
    /// </summary>
    public static class RoundPlanRules
    {
        /// <summary>Reference rotations are multiples of this (degrees); same value as PuzzleState.RotationStep (a test pins them).</summary>
        public const int RotationStepDegrees = 90;

        /// <summary>0, 90, 180, 270.</summary>
        public const int RotationStepCount = 4;

        /// <summary>Motifs crossed out (X) on the Wires reference (Oyun 4.3).</summary>
        public const int WiresTrapCount = 2;

        public static bool HasReferenceRotation(PuzzleType type)
        {
            return type == PuzzleType.Sigil;
        }

        /// <summary>Trap values the reference of this type shows; 0 = none.</summary>
        public static int TrapCount(PuzzleType type)
        {
            return type == PuzzleType.Wires ? WiresTrapCount : 0;
        }

        public static bool IsValidRotation(int degrees)
        {
            return degrees >= 0 && degrees < RotationStepDegrees * RotationStepCount && degrees % RotationStepDegrees == 0;
        }

        /// <summary>Largest K of the definition (trap values lie in [0, max K), like PuzzleState checks them).</summary>
        public static int MaxValueCount(PuzzleDefinition definition)
        {
            int max = 0;
            IReadOnlyList<int> counts = definition.ValueCounts;
            for (int i = 0; i < counts.Count; i++)
            {
                if (counts[i] > max)
                {
                    max = counts[i];
                }
            }
            return max;
        }

        /// <summary>The definition may be placed on the slot: same size class and the slot lists its type.</summary>
        public static bool Fits(PuzzleDefinition definition, HousePuzzleSlotEntry slot)
        {
            return definition != null && slot != null && definition.Size == slot.Size && slot.Allows(definition.Type);
        }

        /// <summary>The definition's reference may be placed on the slot: same size class and the slot lists its type.</summary>
        public static bool Fits(PuzzleDefinition definition, HouseReferenceSlotEntry slot)
        {
            return definition != null && slot != null && definition.Size == slot.Size && slot.Allows(definition.Type);
        }

        /// <summary>Floor of the room, or null when the house has no such room.</summary>
        public static int? FloorOf(HouseIndex house, int roomId)
        {
            HouseRoomEntry room = house.FindRoom(roomId);
            return room != null ? room.Floor : (int?)null;
        }

        /// <summary>Both rooms exist and lie on different floors.</summary>
        public static bool OnDifferentFloors(HouseIndex house, int roomA, int roomB)
        {
            int? a = FloorOf(house, roomA);
            int? b = FloorOf(house, roomB);
            return a.HasValue && b.HasValue && a.Value != b.Value;
        }

        /// <summary>The house key may hide here: the spot lies in a room of the garden stage (Oyun 4.4).</summary>
        public static bool IsKeyHideCandidate(HouseIndex house, HouseSpotEntry spot)
        {
            int room = house.ResolveRoom(spot);
            return room != HouseIndex.NoRoom && house.StageOfRoom(room) == HouseContract.GardenStage;
        }

        /// <summary>The cologne may spawn here: the spot lies in a room of an active stage (inside the house and reachable this round).</summary>
        public static bool IsCologneCandidate(HouseIndex house, HouseSpotEntry spot, PlannerSettings settings)
        {
            int room = house.ResolveRoom(spot);
            return room != HouseIndex.NoRoom && house.FindRoom(room) != null && settings.IsActiveStage(house.StageOfRoom(room));
        }

        /// <summary>The treasure may be buried here: the spot lies in a cellar room.</summary>
        public static bool IsDigCandidate(HouseIndex house, HouseSpotEntry spot)
        {
            HouseRoomEntry room = house.FindRoom(house.ResolveRoom(spot));
            return room != null && room.Floor == HouseContract.CellarFloor;
        }

        /// <summary>Key hiding places that pass <see cref="IsKeyHideCandidate"/>, in index (NetId) order.</summary>
        public static List<HouseSpotEntry> KeyHideCandidates(HouseIndex house)
        {
            var result = new List<HouseSpotEntry>();
            for (int i = 0; i < house.KeyHideSpots.Count; i++)
            {
                if (IsKeyHideCandidate(house, house.KeyHideSpots[i]))
                {
                    result.Add(house.KeyHideSpots[i]);
                }
            }
            return result;
        }

        /// <summary>Cologne spots that pass <see cref="IsCologneCandidate"/>, in index (NetId) order.</summary>
        public static List<HouseSpotEntry> CologneCandidates(HouseIndex house, PlannerSettings settings)
        {
            var result = new List<HouseSpotEntry>();
            for (int i = 0; i < house.CologneSpots.Count; i++)
            {
                if (IsCologneCandidate(house, house.CologneSpots[i], settings))
                {
                    result.Add(house.CologneSpots[i]);
                }
            }
            return result;
        }

        /// <summary>Dig spots that pass <see cref="IsDigCandidate"/>, in index (NetId) order (empty until the cellar exists, TODO 3.7).</summary>
        public static List<HouseSpotEntry> DigCandidates(HouseIndex house)
        {
            var result = new List<HouseSpotEntry>();
            for (int i = 0; i < house.DigSpots.Count; i++)
            {
                if (IsDigCandidate(house, house.DigSpots[i]))
                {
                    result.Add(house.DigSpots[i]);
                }
            }
            return result;
        }
    }
}
