using System;
using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.Core.Net;
using NUnit.Framework;

namespace CinliMahzen.Tests.EditMode.A
{
    /// <summary>
    /// Enum int values are persisted in assets and on the wire (PD_*.asset, ID_*.asset, MsgCode).
    /// These tests fail as soon as a value is renumbered; new values must be appended.
    /// </summary>
    public class CoreEnumStabilityTests
    {
        [TestCase(StatusType.None, 0)]
        [TestCase(StatusType.Knockdown, 1)]
        [TestCase(StatusType.Grabbed, 2)]
        [TestCase(StatusType.Drunk, 3)]
        [TestCase(StatusType.Darkness, 4)]
        [TestCase(StatusType.Slowed, 5)]
        [TestCase(StatusType.Stunned, 6)]
        [TestCase(StatusType.CameraSpin180, 7)]
        [TestCase(StatusType.VisionBlurred, 8)]
        public void StatusType_ValueIsStable(StatusType value, int expected)
        {
            Assert.AreEqual(expected, (int)value, value.ToString());
        }

        [TestCase(RoomLightState.Off, 0)]
        [TestCase(RoomLightState.On, 1)]
        public void RoomLightState_ValueIsStable(RoomLightState value, int expected)
        {
            Assert.AreEqual(expected, (int)value, value.ToString());
        }

        [TestCase(LampState.Off, 0)]
        [TestCase(LampState.On, 1)]
        [TestCase(LampState.Broken, 2)]
        public void LampState_ValueIsStable(LampState value, int expected)
        {
            Assert.AreEqual(expected, (int)value, value.ToString());
        }

        [TestCase(ObjectivePhase.Garden, 0)]
        [TestCase(ObjectivePhase.House, 1)]
        [TestCase(ObjectivePhase.Cellar, 2)]
        [TestCase(ObjectivePhase.Done, 3)]
        public void ObjectivePhase_ValueIsStable(ObjectivePhase value, int expected)
        {
            Assert.AreEqual(expected, (int)value, value.ToString());
        }

        [TestCase(RoundEndReason.None, 0)]
        [TestCase(RoundEndReason.Faint, 1)]
        [TestCase(RoundEndReason.TreasureDug, 2)]
        [TestCase(RoundEndReason.Timeout, 3)]
        public void RoundEndReason_ValueIsStable(RoundEndReason value, int expected)
        {
            Assert.AreEqual(expected, (int)value, value.ToString());
        }

        [TestCase(ItemKind.QuestItem, 0)]
        [TestCase(ItemKind.LanternUpgrade, 1)]
        public void ItemKind_ValueIsStable(ItemKind value, int expected)
        {
            Assert.AreEqual(expected, (int)value, value.ToString());
        }

        [Test]
        public void MsgCode_ValuesAreUnique()
        {
            string[] names = Enum.GetNames(typeof(MsgCode));
            var distinctValues = new HashSet<byte>();
            foreach (MsgCode code in Enum.GetValues(typeof(MsgCode)))
                distinctValues.Add((byte)code);

            Assert.AreEqual(names.Length, distinctValues.Count, "every MsgCode name must have its own wire value");
        }

        [Test]
        public void MsgCode_ValuesFallInReservedRanges()
        {
            foreach (MsgCode code in Enum.GetValues(typeof(MsgCode)))
            {
                int v = (int)code;
                bool inRange = (v >= 1 && v <= 29)
                               || (v >= 30 && v <= 59)
                               || (v >= 60 && v <= 99)
                               || (v >= 100 && v <= 139)
                               || (v >= 200 && v <= 254);
                Assert.IsTrue(inRange, $"{code} = {v} is outside the reserved MsgCode ranges (1-29, 30-59, 60-99, 100-139, 200-254)");
            }
        }
    }
}
