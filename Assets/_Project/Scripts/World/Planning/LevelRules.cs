using System;
using System.Collections.Generic;

namespace CinliMahzen.World
{
    /// <summary>Unity-free validation rules (Spec 8.2 / 8.5). Returns human-readable errors; empty = valid.</summary>
    public static class LevelRules
    {
        public static List<string> Validate(IReadOnlyList<PlannedRoom> rooms, IReadOnlyList<PlannedMarker> markers, LevelGenSettings s)
        {
            var errors = new List<string>();
            if (rooms == null || rooms.Count == 0) { errors.Add("No rooms"); return errors; }

            int human = 0, jinn = 0, exit = 0, vault = 0, gold = 0, poss = 0, cont = 0;
            int runeStones = 0, runeHint = 0, footprint = 0, dig = 0;
            var lightsPerRoom = new int[rooms.Count];
            PlannedMarker humanM = null, exitM = null, vaultM = null, goldM = null;
            var jinns = new List<PlannedMarker>();
            var stones = new List<PlannedMarker>();
            var hints = new List<PlannedMarker>();

            for (int i = 0; i < markers.Count; i++)
            {
                var m = markers[i];
                if (m.RoomId < 0 || m.RoomId >= rooms.Count) { errors.Add(m.Kind + " has invalid roomId " + m.RoomId); continue; }
                switch (m.Kind)
                {
                    case MarkerKind.HumanSpawn: human++; humanM = m; break;
                    case MarkerKind.JinnSpawn: jinn++; jinns.Add(m); break;
                    case MarkerKind.Exit: exit++; exitM = m; break;
                    case MarkerKind.Vault: vault++; vaultM = m; break;
                    case MarkerKind.GoldSpawn: gold++; goldM = m; break;
                    case MarkerKind.PossessableSocket: poss++; break;
                    case MarkerKind.ContainerSocket: cont++; break;
                    case MarkerKind.LightSocket: lightsPerRoom[m.RoomId]++; break;
                    case MarkerKind.PuzzleSocket:
                        if (m.Puzzle == PuzzleType.RuneStones) { runeStones++; stones.Add(m); }
                        else if (m.Puzzle == PuzzleType.RuneHint) { runeHint++; hints.Add(m); }
                        else if (m.Puzzle == PuzzleType.FootprintStart) footprint++;
                        else dig++;
                        break;
                }
            }

            if (human != 1) errors.Add("HumanSpawnMarker count " + human + " (expected 1)");
            if (jinn != 3) errors.Add("JinnSpawnMarker count " + jinn + " (expected 3)");
            if (exit != 1) errors.Add("ExitMarker count " + exit + " (expected 1)");
            if (vault != 1) errors.Add("VaultMarker count " + vault + " (expected 1)");
            if (gold != 1) errors.Add("GoldSpawnMarker count " + gold + " (expected 1)");
            if (poss < s.MinPossessableSockets || poss > s.MaxPossessableSockets)
                errors.Add("PossessableSocket count " + poss + " (expected " + s.MinPossessableSockets + "-" + s.MaxPossessableSockets + ")");
            if (cont < s.MinContainerSockets || cont > s.MaxContainerSockets)
                errors.Add("ContainerSocket count " + cont + " (expected " + s.MinContainerSockets + "-" + s.MaxContainerSockets + ")");
            if (runeStones < 1) errors.Add("No PuzzleSocket RuneStones");
            if (runeHint < 1) errors.Add("No PuzzleSocket RuneHint");
            if (footprint < 1) errors.Add("No PuzzleSocket FootprintStart");
            if (dig < 1) errors.Add("No PuzzleSocket DigSpot");
            for (int r = 0; r < rooms.Count; r++)
                if (lightsPerRoom[r] < 1) errors.Add("Room " + r + " has no LightSocket");

            // graph
            var fromStart = LevelGraph.Hops(rooms, 0);
            for (int r = 0; r < rooms.Count; r++)
                if (fromStart[r] < 0) { errors.Add("Room " + r + " is unreachable"); break; }
            if (LevelGraph.EdgeCount(rooms) < rooms.Count) errors.Add("No loop in room graph");

            if (humanM != null && (rooms[humanM.RoomId].Tags & RoomTag.Start) == 0) errors.Add("HumanSpawn is not in the Start room");
            if (vaultM != null)
            {
                var vr = rooms[vaultM.RoomId];
                if ((vr.Tags & RoomTag.Vault) == 0) errors.Add("VaultMarker room lacks Vault tag");
                if (vr.Neighbors.Count != 1) errors.Add("Vault room has " + vr.Neighbors.Count + " entrances (expected 1)");
                if (goldM != null && goldM.RoomId != vaultM.RoomId) errors.Add("GoldSpawn is not inside the vault room");
                if (exitM != null)
                {
                    float d = LevelGraph.PathDistances(rooms, vaultM.RoomId)[exitM.RoomId];
                    if (d < s.MinExitPathFromVault) errors.Add("Exit is " + d.ToString("F1") + " m from vault (min " + s.MinExitPathFromVault + ")");
                }
                if (stones.Count > 0 && hints.Count > 0)
                {
                    var hops = LevelGraph.Hops(rooms, stones[0].RoomId);
                    if (hops[hints[0].RoomId] < s.MinRuneRoomHops)
                        errors.Add("RuneHint is " + hops[hints[0].RoomId] + " rooms from RuneStones (min " + s.MinRuneRoomHops + ")");
                }
            }
            if (humanM != null)
                for (int j = 0; j < jinns.Count; j++)
                {
                    float dx = jinns[j].X - humanM.X, dz = jinns[j].Z - humanM.Z;
                    float d = (float)Math.Sqrt(dx * dx + dz * dz);
                    if (d < s.MinJinnDistanceFromHuman) errors.Add("JinnSpawn " + j + " is " + d.ToString("F1") + " m from human (min " + s.MinJinnDistanceFromHuman + ")");
                }
            return errors;
        }
    }
}
