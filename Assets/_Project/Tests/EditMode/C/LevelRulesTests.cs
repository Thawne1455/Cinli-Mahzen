using System.Collections.Generic;
using CinliMahzen.World;
using NUnit.Framework;

namespace CinliMahzen.Tests.EditMode
{
    public class LevelRulesTests
    {
        private static LevelPlan Good() => LevelPlanner.Plan(7, new LevelGenSettings());

        [Test]
        public void ValidPlan_HasNoErrors()
        {
            var p = Good();
            Assert.IsEmpty(LevelRules.Validate(p.Rooms, p.Markers, new LevelGenSettings()));
        }

        [TestCase(MarkerKind.HumanSpawn)]
        [TestCase(MarkerKind.JinnSpawn)]
        [TestCase(MarkerKind.Exit)]
        [TestCase(MarkerKind.Vault)]
        [TestCase(MarkerKind.GoldSpawn)]
        [TestCase(MarkerKind.LightSocket)]
        [TestCase(MarkerKind.ContainerSocket)]
        [TestCase(MarkerKind.PossessableSocket)]
        public void MissingMarkerKind_IsReported(MarkerKind kind)
        {
            var p = Good();
            p.Markers.RemoveAll(m => m.Kind == kind);
            Assert.IsNotEmpty(LevelRules.Validate(p.Rooms, p.Markers, new LevelGenSettings()));
        }

        [TestCase(PuzzleType.RuneStones)]
        [TestCase(PuzzleType.RuneHint)]
        [TestCase(PuzzleType.FootprintStart)]
        [TestCase(PuzzleType.DigSpot)]
        public void MissingPuzzle_IsReported(PuzzleType type)
        {
            var p = Good();
            p.Markers.RemoveAll(m => m.Kind == MarkerKind.PuzzleSocket && m.Puzzle == type);
            Assert.IsNotEmpty(LevelRules.Validate(p.Rooms, p.Markers, new LevelGenSettings()));
        }

        [Test]
        public void ExitTooCloseToVault_IsReported()
        {
            var p = Good();
            var s = new LevelGenSettings { MinExitPathFromVault = 500f };
            Assert.IsTrue(LevelRules.Validate(p.Rooms, p.Markers, s).Exists(e => e.Contains("from vault")));
        }

        [Test]
        public void JinnTooClose_IsReported()
        {
            var p = Good();
            var human = p.Markers.Find(m => m.Kind == MarkerKind.HumanSpawn);
            var jinn = p.Markers.Find(m => m.Kind == MarkerKind.JinnSpawn);
            jinn.X = human.X; jinn.Z = human.Z;
            Assert.IsTrue(LevelRules.Validate(p.Rooms, p.Markers, new LevelGenSettings()).Exists(e => e.Contains("from human")));
        }

        [Test]
        public void SecondVaultEntrance_IsReported()
        {
            var p = Good();
            int vault = p.Rooms.FindIndex(r => (r.Tags & RoomTag.Vault) != 0);
            int other = p.Rooms.FindIndex(r => r.RoomId != vault && !p.Rooms[vault].Neighbors.Contains(r.RoomId));
            p.Rooms[vault].Neighbors.Add(other);
            p.Rooms[other].Neighbors.Add(vault);
            Assert.IsTrue(LevelRules.Validate(p.Rooms, p.Markers, new LevelGenSettings()).Exists(e => e.Contains("entrances")));
        }

        [Test]
        public void TreeGraph_HasNoLoop()
        {
            var rooms = new List<PlannedRoom>
            {
                new PlannedRoom { RoomId = 0, Neighbors = { 1 } },
                new PlannedRoom { RoomId = 1, Neighbors = { 0 } },
            };
            Assert.IsTrue(LevelRules.Validate(rooms, new List<PlannedMarker>(), new LevelGenSettings()).Exists(e => e.Contains("loop")));
        }
    }
}
