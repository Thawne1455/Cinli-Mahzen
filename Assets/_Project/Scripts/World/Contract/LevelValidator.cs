using System.Collections.Generic;
using CinliMahzen.Core;
using UnityEngine;

namespace CinliMahzen.World
{
    /// <summary>Unity adapter over LevelRules (Spec 8.5): converts a LevelLayout to plain data and runs the rules.</summary>
    public static class LevelValidator
    {
        public static List<string> Validate(LevelLayout layout, LevelGenSettings settings)
        {
            var rooms = new List<PlannedRoom>(layout.Rooms.Count);
            for (int i = 0; i < layout.Rooms.Count; i++)
            {
                var r = layout.Rooms[i];
                rooms.Add(new PlannedRoom
                {
                    RoomId = r.RoomId,
                    MinX = r.Bounds.min.x, MaxX = r.Bounds.max.x,
                    MinZ = r.Bounds.min.z, MaxZ = r.Bounds.max.z,
                    Tags = r.Tags,
                    Neighbors = r.Neighbors,
                });
            }
            var markers = new List<PlannedMarker>(layout.Markers.Count);
            for (int i = 0; i < layout.Markers.Count; i++)
            {
                var m = layout.Markers[i];
                var pm = new PlannedMarker { Kind = m.Kind, RoomId = m.RoomId, X = m.transform.position.x, Y = m.transform.position.y, Z = m.transform.position.z };
                if (m is PossessableSocket ps) { pm.Category = ps.Category; pm.WallFacing = ps.WallFacing; }
                else if (m is PuzzleSocket pz) pm.Puzzle = pz.PuzzleType;
                markers.Add(pm);
            }
            return LevelRules.Validate(rooms, markers, settings);
        }

        public static bool ValidateAndLog(LevelLayout layout, LevelGenSettings settings)
        {
            var errors = Validate(layout, settings);
            for (int i = 0; i < errors.Count; i++) CMLog.Error("Level", errors[i]);
            if (errors.Count == 0) CMLog.Info("Level", "Validation OK (seed " + layout.Seed + ", " + layout.Rooms.Count + " rooms)");
            return errors.Count == 0;
        }
    }
}
