using System;
using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.World;
using NUnit.Framework;
using UnityEngine;

namespace CinliMahzen.Tests.EditMode.World
{
    /// <summary>
    /// TODO 2.2: <see cref="HouseIndex"/> (deterministic NetIds, RoomOf) and <see cref="HouseIndexBuilder"/>. Pure C#.
    /// </summary>
    public class HouseIndexTests
    {
        private static HouseRoomEntry Room(int id, string key, params Bounds[] boxes)
        {
            return new HouseRoomEntry(id, key, 0, false, boxes);
        }

        private static HouseIndex IndexOf(params HouseRoomEntry[] rooms)
        {
            var builder = new HouseIndexBuilder();
            for (int i = 0; i < rooms.Length; i++)
            {
                builder.Add(rooms[i]);
            }
            return builder.Build();
        }

        // ------------------------------------------------------------------------------------------------
        // AssignNetIds
        // ------------------------------------------------------------------------------------------------

        [Test]
        public void AssignNetIds_StartAtLevelMin_InOrdinalPathOrder()
        {
            int[] ids = HouseIndex.AssignNetIds(new[] { "b", "a", "c" });

            Assert.AreEqual(NetId.LevelMin, 1000);
            CollectionAssert.AreEqual(new[] { 1001, 1000, 1002 }, ids);
        }

        [Test]
        public void AssignNetIds_SamePathsInAnyOrder_GiveTheSameIdPerPath()
        {
            string[] paths = { "Floor1/Room_Study/Markers/Lamp_01#LampMarker", "Floor0/Room_Hall/Markers/Lamp_01#LampMarker",
                "Garden/Room_Garden/Markers/KeyHide_03#KeyHideSpot", "Floor0/Room_Hall/Markers/Lamp_01#LightSwitch", "Markers/Stage_1#StageZone" };
            int[] reference = HouseIndex.AssignNetIds(paths);

            var rotated = new List<string>();
            var rotatedIndex = new List<int>();
            for (int shift = 1; shift < paths.Length; shift++)
            {
                rotated.Clear();
                rotatedIndex.Clear();
                for (int i = 0; i < paths.Length; i++)
                {
                    int source = (i + shift) % paths.Length;
                    rotated.Add(paths[source]);
                    rotatedIndex.Add(source);
                }

                int[] ids = HouseIndex.AssignNetIds(rotated);
                for (int i = 0; i < ids.Length; i++)
                {
                    Assert.AreEqual(reference[rotatedIndex[i]], ids[i], "shift " + shift + ", path " + rotated[i]);
                }
            }
        }

        [Test]
        public void AssignNetIds_UseOrdinalNotCultureOrder()
        {
            // Ordinal: 'B' (66) < '_' (95) < 'a' (97). A culture comparison would put "a" before "B".
            int[] ids = HouseIndex.AssignNetIds(new[] { "a", "B", "_" });

            CollectionAssert.AreEqual(new[] { 1002, 1000, 1001 }, ids);
        }

        [Test]
        public void AssignNetIds_AreConsecutiveAndUnique()
        {
            var paths = new List<string>();
            for (int i = 0; i < 200; i++)
            {
                paths.Add("Room_" + ((i * 37) % 200).ToString("000") + "/Lamp");
            }

            int[] ids = HouseIndex.AssignNetIds(paths);

            var sorted = new List<int>(ids);
            sorted.Sort();
            for (int i = 0; i < sorted.Count; i++)
            {
                Assert.AreEqual(NetId.LevelMin + i, sorted[i]);
            }
        }

        [Test]
        public void AssignNetIds_EmptyList_GivesEmptyArray()
        {
            Assert.AreEqual(0, HouseIndex.AssignNetIds(new string[0]).Length);
        }

        [Test]
        public void AssignNetIds_ExactlyMaxCount_EndsAt59999()
        {
            var paths = new string[HouseIndex.MaxNetIdCount];
            for (int i = 0; i < paths.Length; i++)
            {
                paths[i] = "p" + i.ToString("00000");
            }

            int[] ids = HouseIndex.AssignNetIds(paths);

            Assert.AreEqual(59000, HouseIndex.MaxNetIdCount);
            Assert.AreEqual(1000, ids[0]);
            Assert.AreEqual(NetId.RuntimeMin - 1, ids[ids.Length - 1]);
        }

        [Test]
        public void AssignNetIds_OneMoreThanMax_Throws()
        {
            var paths = new string[HouseIndex.MaxNetIdCount + 1];
            for (int i = 0; i < paths.Length; i++)
            {
                paths[i] = "p" + i.ToString("00000");
            }

            Assert.Throws<InvalidOperationException>(() => HouseIndex.AssignNetIds(paths));
        }

        [Test]
        public void AssignNetIds_DuplicatePath_Throws()
        {
            var e = Assert.Throws<ArgumentException>(() => HouseIndex.AssignNetIds(new[] { "a", "b", "a" }));
            StringAssert.Contains("Duplicate", e.Message);
        }

        [Test]
        public void AssignNetIds_NullOrEmptyPath_Throws()
        {
            Assert.Throws<ArgumentException>(() => HouseIndex.AssignNetIds(new[] { "a", "" }));
            Assert.Throws<ArgumentException>(() => HouseIndex.AssignNetIds(new[] { "a", null }));
            Assert.Throws<ArgumentNullException>(() => HouseIndex.AssignNetIds(null));
        }

        // ------------------------------------------------------------------------------------------------
        // HouseIndexBuilder
        // ------------------------------------------------------------------------------------------------

        [Test]
        public void Builder_AssignsNetIdsFromPaths_WhateverTheInsertionOrder()
        {
            HouseSpotEntry lampB = new HouseSpotEntry("B#LampMarker", HouseSpotKind.Lamp, 1, "", Vector3.zero);
            HouseSpotEntry lampA = new HouseSpotEntry("A#LampMarker", HouseSpotKind.Lamp, 1, "", Vector3.zero);
            HouseGateEntry gate = new HouseGateEntry("C#StageGate", 0, "", Vector3.zero);
            new HouseIndexBuilder().Add(lampB).Add(gate).Add(lampA).Build();

            HouseSpotEntry lampB2 = new HouseSpotEntry("B#LampMarker", HouseSpotKind.Lamp, 1, "", Vector3.zero);
            HouseSpotEntry lampA2 = new HouseSpotEntry("A#LampMarker", HouseSpotKind.Lamp, 1, "", Vector3.zero);
            HouseGateEntry gate2 = new HouseGateEntry("C#StageGate", 0, "", Vector3.zero);
            new HouseIndexBuilder().Add(lampA2).Add(lampB2).Add(gate2).Build();

            Assert.AreEqual(new NetId(1000), lampA.NetId);
            Assert.AreEqual(new NetId(1001), lampB.NetId);
            Assert.AreEqual(new NetId(1002), gate.NetId);
            Assert.AreEqual(lampA.NetId, lampA2.NetId);
            Assert.AreEqual(lampB.NetId, lampB2.NetId);
            Assert.AreEqual(gate.NetId, gate2.NetId);
        }

        [Test]
        public void Builder_ListsComeOutSortedByNetId()
        {
            var builder = new HouseIndexBuilder();
            builder.Add(new HouseSpotEntry("Z#LampMarker", HouseSpotKind.Lamp, 1, "", Vector3.zero));
            builder.Add(new HouseSpotEntry("M#LampMarker", HouseSpotKind.Lamp, 1, "", Vector3.zero));
            builder.Add(new HouseSpotEntry("A#LampMarker", HouseSpotKind.Lamp, 1, "", Vector3.zero));

            HouseIndex index = builder.Build();

            Assert.AreEqual("A#LampMarker", index.Lamps[0].Path);
            Assert.AreEqual("M#LampMarker", index.Lamps[1].Path);
            Assert.AreEqual("Z#LampMarker", index.Lamps[2].Path);
        }

        [Test]
        public void Builder_DuplicatePathAcrossKinds_FailsWithMessage()
        {
            var builder = new HouseIndexBuilder();
            builder.Add(new HouseSpotEntry("X#Marker", HouseSpotKind.Lamp, 1, "", Vector3.zero));
            builder.Add(new HouseSpotEntry("X#Marker", HouseSpotKind.LightSwitch, 1, "", Vector3.zero));

            HouseIndex index;
            string error;
            bool ok = builder.TryBuild(out index, out error);

            Assert.IsFalse(ok);
            Assert.IsNull(index);
            StringAssert.Contains("X#Marker", error);
            Assert.Throws<InvalidOperationException>(() => builder.Build());
        }

        [Test]
        public void Builder_ExplicitNetId_IsKept()
        {
            var lamp = new HouseSpotEntry("A#LampMarker", HouseSpotKind.Lamp, 1, "", Vector3.zero, new NetId(4242));
            var other = new HouseSpotEntry("B#LampMarker", HouseSpotKind.Lamp, 1, "", Vector3.zero);

            new HouseIndexBuilder().Add(lamp).Add(other).Build();

            Assert.AreEqual(new NetId(4242), lamp.NetId);
            Assert.AreEqual(new NetId(1000), other.NetId);
        }

        [Test]
        public void Builder_SortsRoomsAndStages_AndSpawnsByPath()
        {
            var builder = new HouseIndexBuilder();
            builder.Add(Room(5, "five", HouseFixture.Box(0, 0, 0, 1, 1, 1)));
            builder.Add(Room(2, "two", HouseFixture.Box(2, 0, 0, 3, 1, 1)));
            builder.Add(new HouseStageEntry(2, new int[0]));
            builder.Add(new HouseStageEntry(0, new int[0]));
            builder.AddJinnSpawn("b", new Vector3(2f, 0f, 0f));
            builder.AddJinnSpawn("a", new Vector3(1f, 0f, 0f));

            HouseIndex index = builder.Build();

            Assert.AreEqual(2, index.Rooms[0].Id);
            Assert.AreEqual(5, index.Rooms[1].Id);
            Assert.AreEqual(0, index.Stages[0].Index);
            Assert.AreEqual(2, index.Stages[1].Index);
            Assert.AreEqual(1f, index.JinnSpawns[0].x);
            Assert.AreEqual(2f, index.JinnSpawns[1].x);
        }

        // ------------------------------------------------------------------------------------------------
        // RoomOf
        // ------------------------------------------------------------------------------------------------

        [Test]
        public void RoomOf_InsideABox_ReturnsItsRoomId()
        {
            HouseIndex index = IndexOf(Room(7, "hall", HouseFixture.Box(0, 0, 0, 10, 4, 10)));

            Assert.AreEqual(7, index.RoomOf(new Vector3(5f, 2f, 5f)));
        }

        [Test]
        public void RoomOf_Outside_ReturnsMinusOne()
        {
            HouseIndex index = IndexOf(Room(7, "hall", HouseFixture.Box(0, 0, 0, 10, 4, 10)));

            Assert.AreEqual(-1, HouseIndex.NoRoom);
            Assert.AreEqual(-1, index.RoomOf(new Vector3(-0.1f, 2f, 5f)));
            Assert.AreEqual(-1, index.RoomOf(new Vector3(5f, 4.5f, 5f)), "above the box");
            Assert.AreEqual(-1, index.RoomOf(new Vector3(5f, -0.5f, 5f)), "below the box");
            Assert.AreEqual(-1, index.RoomOf(new Vector3(5f, 2f, 12f)));
        }

        [Test]
        public void RoomOf_NoRooms_ReturnsMinusOne()
        {
            Assert.AreEqual(-1, new HouseIndexBuilder().Build().RoomOf(Vector3.zero));
        }

        [Test]
        public void RoomOf_BoxesAreHalfOpen_NeighboursNeverShareAPoint()
        {
            HouseIndex index = IndexOf(
                Room(1, "west", HouseFixture.Box(0, 0, 0, 10, 4, 10)),
                Room(2, "east", HouseFixture.Box(10, 0, 0, 20, 4, 10)));

            Assert.AreEqual(1, index.RoomOf(new Vector3(0f, 0f, 0f)), "min is inclusive");
            Assert.AreEqual(2, index.RoomOf(new Vector3(10f, 1f, 5f)), "shared face belongs to the box that starts there");
            Assert.AreEqual(-1, index.RoomOf(new Vector3(20f, 1f, 5f)), "max is exclusive");
        }

        [Test]
        public void RoomOf_StackedFloors_SplitAtTheFloorHeight()
        {
            HouseIndex index = IndexOf(
                Room(1, "ground", HouseFixture.Box(0, 0, 0, 10, 4, 10)),
                Room(2, "first", HouseFixture.Box(0, 4, 0, 10, 8, 10)));

            Assert.AreEqual(1, index.RoomOf(new Vector3(5f, 3.99f, 5f)));
            Assert.AreEqual(2, index.RoomOf(new Vector3(5f, 4f, 5f)));
            Assert.AreEqual(2, index.RoomOf(new Vector3(5f, 4.05f, 5f)));
        }

        [Test]
        public void RoomOf_LShapedRoom_UsesEveryBox()
        {
            // L: a 4 x 12 strip along the west plus a 8 x 4 arm to the east at the south.
            HouseRoomEntry kitchen = Room(3, "kitchen",
                HouseFixture.Box(0, 0, 0, 4, 4, 12),
                HouseFixture.Box(4, 0, 0, 12, 4, 4));
            HouseIndex index = IndexOf(kitchen);

            Assert.AreEqual(3, index.RoomOf(new Vector3(2f, 1f, 10f)), "strip");
            Assert.AreEqual(3, index.RoomOf(new Vector3(10f, 1f, 2f)), "arm");
            Assert.AreEqual(-1, index.RoomOf(new Vector3(10f, 1f, 10f)), "the notch of the L is outside");
        }

        [Test]
        public void RoomOf_NestedBoxes_TheSmallerRoomWins()
        {
            HouseIndex index = IndexOf(
                Room(1, "big", HouseFixture.Box(0, 0, 0, 20, 4, 20)),
                Room(2, "closet", HouseFixture.Box(2, 0, 2, 4, 4, 4)));

            Assert.AreEqual(2, index.RoomOf(new Vector3(3f, 1f, 3f)), "inside the nested box");
            Assert.AreEqual(1, index.RoomOf(new Vector3(10f, 1f, 10f)), "outside the nested box");
        }

        [Test]
        public void RoomOf_NestedBoxes_SmallerWinsEvenWithTheHigherId()
        {
            HouseIndex index = IndexOf(
                Room(9, "closet", HouseFixture.Box(2, 0, 2, 4, 4, 4)),
                Room(1, "big", HouseFixture.Box(0, 0, 0, 20, 4, 20)));

            Assert.AreEqual(9, index.RoomOf(new Vector3(3f, 1f, 3f)));
        }

        [Test]
        public void RoomOf_EqualVolumes_TheLowerRoomIdWins()
        {
            HouseIndex index = IndexOf(
                Room(8, "b", HouseFixture.Box(0, 0, 0, 10, 4, 10)),
                Room(3, "a", HouseFixture.Box(0, 0, 0, 10, 4, 10)));

            Assert.AreEqual(3, index.RoomOf(new Vector3(5f, 1f, 5f)));
        }

        [Test]
        public void RoomOf_RoomWithSeveralBoxes_UsesTheSmallestContainingBox()
        {
            // Room 1 has a big box (volume 1600) and a small one (16) inside it; room 2 is in between (64). Where the small
            // box of room 1 applies it beats room 2; where only the big box applies, room 2 wins.
            HouseIndex index = IndexOf(
                Room(1, "r1", HouseFixture.Box(0, 0, 0, 20, 4, 20), HouseFixture.Box(1, 0, 1, 3, 4, 3)),
                Room(2, "r2", HouseFixture.Box(0, 0, 0, 4, 4, 4)));

            Assert.AreEqual(1, index.RoomOf(new Vector3(2f, 1f, 2f)));
            Assert.AreEqual(2, index.RoomOf(new Vector3(3.5f, 1f, 3.5f)));
        }

        [Test]
        public void StageOfRoom_AndFindHelpers_ReadTheIndex()
        {
            var builder = new HouseIndexBuilder();
            builder.Add(Room(4, "hall", HouseFixture.Box(0, 0, 0, 1, 1, 1)));
            builder.Add(new HouseStageEntry(1, new[] { 4 }));
            HouseIndex index = builder.Build();

            Assert.AreEqual(1, index.StageOfRoom(4));
            Assert.AreEqual(-1, index.StageOfRoom(99));
            Assert.IsNotNull(index.FindRoom(4));
            Assert.IsNull(index.FindRoom(5));
            Assert.IsNotNull(index.FindStage(1));
            Assert.IsNull(index.FindStage(0));
        }

        [Test]
        public void ResolveRoom_UsesTheExplicitRoomOtherwiseThePosition()
        {
            HouseIndex index = IndexOf(Room(6, "garden", HouseFixture.Box(0, 0, 0, 10, 4, 10)));
            var explicitSpot = new HouseSpotEntry("a", HouseSpotKind.Lamp, 99, "", new Vector3(5f, 1f, 5f));
            var positional = new HouseSpotEntry("b", HouseSpotKind.KeyHide, HouseIndex.NoRoom, "", new Vector3(5f, 1f, 5f));
            var nowhere = new HouseSpotEntry("c", HouseSpotKind.KeyHide, HouseIndex.NoRoom, "", new Vector3(50f, 1f, 5f));

            Assert.AreEqual(99, index.ResolveRoom(explicitSpot));
            Assert.AreEqual(6, index.ResolveRoom(positional));
            Assert.AreEqual(-1, index.ResolveRoom(nowhere));
        }

        [Test]
        public void RoomBounds_IsTheUnionOfAllBoxes()
        {
            HouseIndex index = IndexOf(
                Room(1, "a", HouseFixture.Box(0, 0, 0, 10, 4, 10)),
                Room(2, "b", HouseFixture.Box(10, 4, -5, 20, 8, 5)));

            Assert.AreEqual(new Vector3(0f, 0f, -5f), index.RoomBounds.min);
            Assert.AreEqual(new Vector3(20f, 8f, 10f), index.RoomBounds.max);
        }
    }
}
