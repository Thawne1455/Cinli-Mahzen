using System;
using CinliMahzen.Core;
using CinliMahzen.World;
using NUnit.Framework;
using UnityEngine;

namespace CinliMahzen.Tests.EditMode.World
{
    /// <summary>TODO 2.2: <see cref="RoomService"/> (<see cref="IRoomService"/>) over a small index. Pure C#; the clock is a field the tests move.</summary>
    public class RoomServiceTests
    {
        private double _now;
        private RoomService _service;

        [SetUp]
        public void SetUp()
        {
            _now = 100d;
            _service = new RoomService(HouseFixture.Valid().Build(), () => _now);
        }

        [Test]
        public void RoomService_IsTheIRoomService()
        {
            Assert.IsInstanceOf<IRoomService>(_service);
        }

        [Test]
        public void RoomOf_ComesFromTheRoomBoxes()
        {
            Assert.AreEqual(HouseFixture.HallId, _service.RoomOf(HouseFixture.Inside(HouseFixture.HallId, 5f, 1f, 5f)));
            Assert.AreEqual(HouseFixture.StudyId, _service.RoomOf(HouseFixture.Inside(HouseFixture.StudyId, 5f, 1f, 5f)));
            Assert.AreEqual(HouseFixture.GardenId, _service.RoomOf(HouseFixture.Inside(HouseFixture.GardenId, 5f, 1f, 5f)));
            Assert.AreEqual(-1, _service.RoomOf(new Vector3(100f, 1f, 100f)));
        }

        [Test]
        public void LightOf_DefaultsToOff_ForEveryRoom()
        {
            Assert.AreEqual(RoomLightState.Off, _service.LightOf(HouseFixture.HallId));
            Assert.AreEqual(RoomLightState.Off, _service.LightOf(HouseFixture.GardenId));
            Assert.AreEqual(RoomLightState.Off, _service.LightOf(HouseFixture.StudyId));
        }

        [Test]
        public void LightOf_OutsideAndUnknownRooms_IsAlwaysOff()
        {
            Assert.AreEqual(RoomLightState.Off, _service.LightOf(-1));
            Assert.AreEqual(RoomLightState.Off, _service.LightOf(999));
        }

        [Test]
        public void SetRoomLight_ChangesOnlyThatRoom()
        {
            Assert.IsTrue(_service.SetRoomLight(HouseFixture.HallId, RoomLightState.On));

            Assert.AreEqual(RoomLightState.On, _service.LightOf(HouseFixture.HallId));
            Assert.AreEqual(RoomLightState.Off, _service.LightOf(HouseFixture.KitchenId));

            Assert.IsTrue(_service.SetRoomLight(HouseFixture.HallId, RoomLightState.Off));
            Assert.AreEqual(RoomLightState.Off, _service.LightOf(HouseFixture.HallId));
        }

        [Test]
        public void SetRoomLight_OutsideOrUnknownRoom_IsRejectedAndStaysOff()
        {
            Assert.IsFalse(_service.SetRoomLight(-1, RoomLightState.On));
            Assert.IsFalse(_service.SetRoomLight(999, RoomLightState.On));

            Assert.AreEqual(RoomLightState.Off, _service.LightOf(-1));
            Assert.AreEqual(RoomLightState.Off, _service.LightOf(999));
        }

        [Test]
        public void IsNoiseSuppressed_DefaultsToFalse()
        {
            Assert.IsFalse(_service.IsNoiseSuppressed(HouseFixture.HallId));
            Assert.IsFalse(_service.IsNoiseSuppressed(-1));
            Assert.IsFalse(_service.IsNoiseSuppressed(999));
        }

        [Test]
        public void IsNoiseSuppressed_IsTrueUntilTheEndTime()
        {
            Assert.IsTrue(_service.SuppressNoiseUntil(HouseFixture.HallId, 112d));

            _now = 100d;
            Assert.IsTrue(_service.IsNoiseSuppressed(HouseFixture.HallId));
            _now = 111.99d;
            Assert.IsTrue(_service.IsNoiseSuppressed(HouseFixture.HallId));
            _now = 112d;
            Assert.IsFalse(_service.IsNoiseSuppressed(HouseFixture.HallId), "the end time itself is already over");
            _now = 500d;
            Assert.IsFalse(_service.IsNoiseSuppressed(HouseFixture.HallId));
        }

        [Test]
        public void IsNoiseSuppressed_IsPerRoom()
        {
            _service.SuppressNoiseUntil(HouseFixture.HallId, 112d);

            Assert.IsTrue(_service.IsNoiseSuppressed(HouseFixture.HallId));
            Assert.IsFalse(_service.IsNoiseSuppressed(HouseFixture.KitchenId));
        }

        [Test]
        public void SuppressNoiseUntil_ReplacesTheEndTime_ZeroEndsItEarly()
        {
            _service.SuppressNoiseUntil(HouseFixture.HallId, 112d);
            _service.SuppressNoiseUntil(HouseFixture.HallId, 130d);
            _now = 120d;
            Assert.IsTrue(_service.IsNoiseSuppressed(HouseFixture.HallId));

            _service.SuppressNoiseUntil(HouseFixture.HallId, 0d);
            Assert.IsFalse(_service.IsNoiseSuppressed(HouseFixture.HallId));
        }

        [Test]
        public void SuppressNoiseUntil_UnknownRoom_IsRejected()
        {
            Assert.IsFalse(_service.SuppressNoiseUntil(-1, 200d));
            Assert.IsFalse(_service.SuppressNoiseUntil(999, 200d));
            Assert.IsFalse(_service.IsNoiseSuppressed(-1));
        }

        [Test]
        public void ResetState_TurnsEveryLightOffAndEndsEveryMute()
        {
            _service.SetRoomLight(HouseFixture.HallId, RoomLightState.On);
            _service.SetRoomLight(HouseFixture.StudyId, RoomLightState.On);
            _service.SuppressNoiseUntil(HouseFixture.KitchenId, 500d);

            _service.ResetState();

            Assert.AreEqual(RoomLightState.Off, _service.LightOf(HouseFixture.HallId));
            Assert.AreEqual(RoomLightState.Off, _service.LightOf(HouseFixture.StudyId));
            Assert.IsFalse(_service.IsNoiseSuppressed(HouseFixture.KitchenId));
        }

        [Test]
        public void Constructor_RejectsNullArguments()
        {
            HouseIndex index = HouseFixture.Valid().Build();

            Assert.Throws<ArgumentNullException>(() => new RoomService(null, () => 0d));
            Assert.Throws<ArgumentNullException>(() => new RoomService(index, null));
        }
    }
}
