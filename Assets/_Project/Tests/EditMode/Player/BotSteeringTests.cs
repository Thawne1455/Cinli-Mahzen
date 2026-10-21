using CinliMahzen.Player;
using NUnit.Framework;
using UnityEngine;

namespace CinliMahzen.Tests.EditMode.Player
{
    /// <summary>TODO 3.12: <see cref="BotSteering"/> — path corners to a local move and a turn.</summary>
    public class BotSteeringTests
    {
        [Test]
        public void YawTo_UsesUnityYaw()
        {
            Assert.AreEqual(0f, BotSteering.YawTo(Vector3.zero, Vector3.forward), 1e-3f);
            Assert.AreEqual(90f, BotSteering.YawTo(Vector3.zero, Vector3.right), 1e-3f);
            Assert.AreEqual(-90f, BotSteering.YawTo(Vector3.zero, Vector3.left), 1e-3f);
        }

        [Test]
        public void LocalMove_IsRelativeToFacing()
        {
            Vector2 ahead = BotSteering.LocalMove(Vector3.zero, 90f, new Vector3(5f, 3f, 0f));
            Assert.AreEqual(0f, ahead.x, 1e-3f);
            Assert.AreEqual(1f, ahead.y, 1e-3f, "facing +X, the target on +X is straight ahead (height ignored)");

            Vector2 right = BotSteering.LocalMove(Vector3.zero, 0f, new Vector3(2f, 0f, 0f));
            Assert.AreEqual(1f, right.x, 1e-3f);
            Assert.AreEqual(Vector2.zero, BotSteering.LocalMove(Vector3.one, 0f, Vector3.one));
        }

        [Test]
        public void Step_SkipsTheStartCorner_AndTurnsAtMostMaxTurn()
        {
            var steering = new BotSteering(8, 0.5f);
            steering.Buffer[0] = Vector3.zero;
            steering.Buffer[1] = new Vector3(0f, 0f, 4f);
            steering.Buffer[2] = new Vector3(4f, 0f, 4f);
            steering.SetPath(3);

            steering.Step(Vector3.zero, 90f, 30f, out Vector2 move, out float yaw);
            Assert.AreEqual(-30f, yaw, 1e-3f, "facing +X, corner 1 is on +Z: turn left, clamped");
            Assert.Less(move.x, 0f, "it strafes toward the corner while turning");
            Assert.IsTrue(steering.HasPath);
        }

        [Test]
        public void Step_AdvancesThroughCorners_ThenStops()
        {
            var steering = new BotSteering(8, 0.5f);
            steering.Buffer[0] = Vector3.zero;
            steering.Buffer[1] = new Vector3(0f, 0f, 4f);
            steering.Buffer[2] = new Vector3(4f, 0f, 4f);
            steering.SetPath(3);

            steering.Step(new Vector3(0f, 0f, 3.9f), 0f, 360f, out _, out float yaw);
            Assert.AreEqual(90f, yaw, 2f, "corner 1 reached: now heading to corner 2 on +X");

            steering.Step(new Vector3(3.9f, 0f, 4f), 90f, 360f, out Vector2 move, out _);
            Assert.AreEqual(Vector2.zero, move);
            Assert.IsFalse(steering.HasPath);
        }

        [Test]
        public void SetPath_ClampsToTheBuffer()
        {
            var steering = new BotSteering(4, 0.5f);
            steering.SetPath(99);
            Assert.AreEqual(4, steering.CornerCount);
            steering.SetPath(0);
            Assert.IsFalse(steering.HasPath);
        }
    }
}
