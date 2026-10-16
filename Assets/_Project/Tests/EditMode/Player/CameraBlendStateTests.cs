using CinliMahzen.Player;
using NUnit.Framework;
using UnityEngine;

namespace CinliMahzen.Tests.EditMode.Player
{
    public class CameraBlendStateTests
    {
        // TODO 1.2: camera mode changes take 0.3 s (the rig serializes the value and passes it in).
        private const float Duration = 0.3f;
        private const float Frame = 1f / 60f;
        private const float Epsilon = 1e-4f;

        private static readonly Vector3 FromPosition = new Vector3(0f, 1.65f, 0f);
        private static readonly Vector3 TargetPosition = new Vector3(4f, 3f, -2f);
        private static readonly Quaternion FromRotation = Quaternion.identity;
        private static readonly Quaternion TargetRotation = Quaternion.Euler(0f, 90f, 0f);

        private CameraBlendState _blend;

        [SetUp]
        public void SetUp()
        {
            _blend = new CameraBlendState();
        }

        private static void AssertPosition(Vector3 expected, Vector3 actual)
        {
            Assert.Less(Vector3.Distance(expected, actual), Epsilon, "expected " + expected + " but was " + actual);
        }

        private static void AssertRotation(Quaternion expected, Quaternion actual)
        {
            Assert.Less(Quaternion.Angle(expected, actual), 0.01f, "expected " + expected.eulerAngles + " but was " + actual.eulerAngles);
        }

        [Test]
        public void Idle_ReturnsTheTargetUnchanged()
        {
            _blend.Evaluate(TargetPosition, TargetRotation, Frame, out Vector3 position, out Quaternion rotation);

            Assert.IsFalse(_blend.IsBlending);
            Assert.AreEqual(1f, _blend.Progress);
            AssertPosition(TargetPosition, position);
            AssertRotation(TargetRotation, rotation);
        }

        [Test]
        public void Begin_KeepsTheOldViewUntilTimePasses()
        {
            _blend.Begin(FromPosition, FromRotation, Duration);

            _blend.Evaluate(TargetPosition, TargetRotation, 0f, out Vector3 position, out Quaternion rotation);

            Assert.IsTrue(_blend.IsBlending);
            AssertPosition(FromPosition, position);
            AssertRotation(FromRotation, rotation);
        }

        [Test]
        public void Halfway_IsBetweenTheTwoPoses()
        {
            _blend.Begin(FromPosition, FromRotation, Duration);

            _blend.Evaluate(TargetPosition, TargetRotation, Duration * 0.5f, out Vector3 position, out Quaternion rotation);

            Assert.IsTrue(_blend.IsBlending);
            Assert.AreEqual(0.5f, _blend.Progress, Epsilon);
            // Smoothstep is symmetric, so half the time is half the way.
            AssertPosition(Vector3.Lerp(FromPosition, TargetPosition, 0.5f), position);
            float toFrom = Quaternion.Angle(rotation, FromRotation);
            float toTarget = Quaternion.Angle(rotation, TargetRotation);
            Assert.Greater(toFrom, 1f);
            Assert.Greater(toTarget, 1f);
            Assert.AreEqual(toFrom, toTarget, 0.1f);
        }

        [Test]
        public void AfterTheFullDuration_IsExactlyOnTheTarget_AndStopsBlending()
        {
            _blend.Begin(FromPosition, FromRotation, Duration);

            _blend.Evaluate(TargetPosition, TargetRotation, Duration, out Vector3 position, out Quaternion rotation);

            Assert.IsFalse(_blend.IsBlending);
            Assert.AreEqual(TargetPosition, position);
            Assert.AreEqual(TargetRotation, rotation);
        }

        [Test]
        public void SixtyFps_ReachesTheTargetWithinTheDuration()
        {
            _blend.Begin(FromPosition, FromRotation, Duration);

            float elapsed = 0f;
            int frames = 0;
            Vector3 position = FromPosition;
            while (_blend.IsBlending && frames < 100)
            {
                _blend.Evaluate(TargetPosition, TargetRotation, Frame, out position, out Quaternion _);
                elapsed += Frame;
                frames++;
            }

            Assert.IsFalse(_blend.IsBlending, "blend must end");
            Assert.AreEqual(TargetPosition, position);
            Assert.GreaterOrEqual(elapsed, Duration - Epsilon);
            Assert.LessOrEqual(elapsed, Duration + Frame + Epsilon, "ends at the end of the duration (+ one frame of rounding)");
        }

        [Test]
        public void SixtyFps_MovesTowardTheTargetWithoutGoingBack()
        {
            _blend.Begin(FromPosition, FromRotation, Duration);

            float previous = Vector3.Distance(FromPosition, TargetPosition);
            for (int i = 0; i < 100 && _blend.IsBlending; i++)
            {
                _blend.Evaluate(TargetPosition, TargetRotation, Frame, out Vector3 position, out Quaternion _);
                float remaining = Vector3.Distance(position, TargetPosition);
                Assert.LessOrEqual(remaining, previous + Epsilon, "frame " + i);
                previous = remaining;
            }
        }

        [Test]
        public void ZeroDuration_IsNoBlendAtAll()
        {
            _blend.Begin(FromPosition, FromRotation, 0f);
            Assert.IsFalse(_blend.IsBlending);

            _blend.Evaluate(TargetPosition, TargetRotation, Frame, out Vector3 position, out Quaternion rotation);

            AssertPosition(TargetPosition, position);
            AssertRotation(TargetRotation, rotation);
        }

        [Test]
        public void NegativeOrNaNDuration_IsNoBlend()
        {
            _blend.Begin(FromPosition, FromRotation, -1f);
            Assert.IsFalse(_blend.IsBlending);

            _blend.Begin(FromPosition, FromRotation, float.NaN);
            Assert.IsFalse(_blend.IsBlending);
        }

        [Test]
        public void HugeDt_DoesNotOvershootTheTarget()
        {
            _blend.Begin(FromPosition, FromRotation, Duration);

            _blend.Evaluate(TargetPosition, TargetRotation, 5f, out Vector3 position, out Quaternion _);

            Assert.IsFalse(_blend.IsBlending);
            Assert.AreEqual(TargetPosition, position);
        }

        [Test]
        public void NegativeDt_ChangesNothing()
        {
            _blend.Begin(FromPosition, FromRotation, Duration);

            _blend.Evaluate(TargetPosition, TargetRotation, -1f, out Vector3 position, out Quaternion _);

            Assert.IsTrue(_blend.IsBlending);
            AssertPosition(FromPosition, position);
        }

        [Test]
        public void MovingTarget_IsFollowedAndTheEndLandsOnTheNewOne()
        {
            _blend.Begin(FromPosition, FromRotation, Duration);
            _blend.Evaluate(TargetPosition, TargetRotation, Duration * 0.5f, out Vector3 _, out Quaternion _);

            Vector3 movedTarget = TargetPosition + new Vector3(0f, 0f, 10f);
            _blend.Evaluate(movedTarget, TargetRotation, Duration, out Vector3 position, out Quaternion _);

            Assert.AreEqual(movedTarget, position);
        }

        [Test]
        public void BeginWhileBlending_RestartsFromTheNewPose()
        {
            _blend.Begin(FromPosition, FromRotation, Duration);
            _blend.Evaluate(TargetPosition, TargetRotation, Duration * 0.9f, out Vector3 current, out Quaternion currentRotation);

            _blend.Begin(current, currentRotation, Duration);
            _blend.Evaluate(TargetPosition, TargetRotation, 0f, out Vector3 position, out Quaternion _);

            Assert.IsTrue(_blend.IsBlending);
            AssertPosition(current, position);
        }

        [Test]
        public void Cancel_SnapsToTheTarget()
        {
            _blend.Begin(FromPosition, FromRotation, Duration);
            _blend.Cancel();

            _blend.Evaluate(TargetPosition, TargetRotation, Frame, out Vector3 position, out Quaternion _);

            Assert.IsFalse(_blend.IsBlending);
            AssertPosition(TargetPosition, position);
        }
    }
}
