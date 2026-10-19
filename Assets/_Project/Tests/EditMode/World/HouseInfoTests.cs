using CinliMahzen.Core;
using CinliMahzen.World;
using NUnit.Framework;
using UnityEngine;

namespace CinliMahzen.Tests.EditMode.World
{
    /// <summary>TODO 2.2: <see cref="HouseInfo"/> (<see cref="IHouseInfo"/>): spirit bounds and spawn points from the index. Pure C#.</summary>
    public class HouseInfoTests
    {
        [Test]
        public void Bounds_AreTheRoomUnionPlusPaddingOnEverySide()
        {
            HouseIndex index = HouseFixture.Valid().Build();
            Bounds union = index.RoomBounds;

            var info = new HouseInfo(index, new Vector3(2f, 1f, 3f));

            Assert.AreEqual(union.min - new Vector3(2f, 1f, 3f), info.Bounds.min);
            Assert.AreEqual(union.max + new Vector3(2f, 1f, 3f), info.Bounds.max);
        }

        [Test]
        public void Bounds_WithoutPadding_CoverEveryRoomBox()
        {
            HouseFixture fixture = HouseFixture.Valid();
            HouseIndex index = fixture.Build();

            var info = new HouseInfo(index, Vector3.zero);

            for (int r = 0; r < fixture.Rooms.Count; r++)
            {
                for (int b = 0; b < fixture.Rooms[r].Boxes.Count; b++)
                {
                    Bounds box = fixture.Rooms[r].Boxes[b];
                    Vector3 outerMin = info.Bounds.min;
                    Vector3 outerMax = info.Bounds.max;
                    bool covered = outerMin.x <= box.min.x && outerMin.y <= box.min.y && outerMin.z <= box.min.z
                        && outerMax.x >= box.max.x && outerMax.y >= box.max.y && outerMax.z >= box.max.z;
                    Assert.IsTrue(covered, "room " + fixture.Rooms[r].Key);
                }
            }
        }

        [Test]
        public void HumanSpawn_IsTheHumanSpawnMarker()
        {
            HouseFixture fixture = HouseFixture.Valid();

            var info = new HouseInfo(fixture.Build(), Vector3.zero);

            Assert.AreEqual(fixture.HumanSpawns[0], info.HumanSpawn);
        }

        [Test]
        public void JinnSpawns_AreTheThreeJinnSpawnMarkers()
        {
            HouseFixture fixture = HouseFixture.Valid();

            var info = new HouseInfo(fixture.Build(), Vector3.zero);

            Assert.AreEqual(HouseContract.JinnSpawnCount, info.JinnSpawns.Count);
            for (int i = 0; i < fixture.JinnSpawns.Count; i++)
            {
                CollectionAssert.Contains(new System.Collections.Generic.List<Vector3>(info.JinnSpawns), fixture.JinnSpawns[i]);
            }
        }

        [Test]
        public void EmptyHouse_HasZeroBoundsAndOriginSpawn()
        {
            var info = new HouseInfo(new HouseIndexBuilder().Build(), Vector3.zero);

            Assert.AreEqual(Vector3.zero, info.Bounds.size);
            Assert.AreEqual(Vector3.zero, info.HumanSpawn);
            Assert.AreEqual(0, info.JinnSpawns.Count);
        }

        [Test]
        public void HouseInfo_IsTheIHouseInfo()
        {
            Assert.IsInstanceOf<IHouseInfo>(new HouseInfo(HouseFixture.Valid().Build(), Vector3.zero));
        }
    }
}
