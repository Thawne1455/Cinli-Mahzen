using CinliMahzen.Player;
using NUnit.Framework;
using UnityEngine;

namespace CinliMahzen.Tests.EditMode.Player
{
    public class OrbitSolverTests
    {
        private const float Desired = 4f;
        private const float Skin = 0.1f;
        private const float MinDistance = 0.5f;
        private const float Epsilon = 1e-4f;

        private static readonly Vector3 Pivot = new Vector3(1f, 2f, 3f);

        private static void AssertVector(Vector3 expected, Vector3 actual)
        {
            Assert.Less(Vector3.Distance(expected, actual), Epsilon, "expected " + expected + " but was " + actual);
        }

        private static float Solved(float hitDistance, float desired = Desired)
        {
            OrbitSolver.Solve(Pivot, 30f, 20f, desired, hitDistance, Skin, MinDistance, out Vector3 position, out Quaternion _);
            return Vector3.Distance(Pivot, position);
        }

        // ---- free view ----

        [Test]
        public void Free_PutsTheCameraTheFullDistanceBehindThePivot()
        {
            OrbitSolver.Solve(Pivot, 0f, 0f, Desired, OrbitSolver.NoHit, Skin, MinDistance, out Vector3 position, out Quaternion rotation);

            AssertVector(Pivot + new Vector3(0f, 0f, -Desired), position);
            AssertVector(Vector3.forward, rotation * Vector3.forward);
        }

        [Test]
        public void Yaw_OrbitsAroundTheVerticalAxis()
        {
            OrbitSolver.Solve(Pivot, 90f, 0f, Desired, OrbitSolver.NoHit, Skin, MinDistance, out Vector3 position, out Quaternion rotation);

            AssertVector(Pivot + new Vector3(-Desired, 0f, 0f), position);
            AssertVector(Vector3.right, rotation * Vector3.forward);
        }

        [Test]
        public void PositivePitch_LiftsTheCameraAboveThePivot()
        {
            OrbitSolver.Solve(Pivot, 0f, 90f, Desired, OrbitSolver.NoHit, Skin, MinDistance, out Vector3 position, out Quaternion rotation);

            AssertVector(Pivot + new Vector3(0f, Desired, 0f), position);
            AssertVector(Vector3.down, rotation * Vector3.forward);
        }

        [Test]
        public void Free_CameraAlwaysLooksAtThePivot()
        {
            OrbitSolver.Solve(Pivot, 123f, 35f, Desired, OrbitSolver.NoHit, Skin, MinDistance, out Vector3 position, out Quaternion rotation);

            AssertVector((Pivot - position).normalized, rotation * Vector3.forward);
            Assert.AreEqual(Desired, Vector3.Distance(Pivot, position), Epsilon);
        }

        [Test]
        public void BackDirection_IsAUnitVectorOppositeTheLookDirection()
        {
            Vector3 back = OrbitSolver.BackDirection(40f, 25f);
            Quaternion look = Quaternion.Euler(25f, 40f, 0f);

            Assert.AreEqual(1f, back.magnitude, Epsilon);
            AssertVector(-(look * Vector3.forward), back);
        }

        // ---- walls ----

        [Test]
        public void WallInTheWay_ShortensTheDistance_ByTheSkin()
        {
            Assert.AreEqual(2f - Skin, Solved(2f), Epsilon);
        }

        [Test]
        public void WallBeyondTheDesiredDistance_ChangesNothing()
        {
            Assert.AreEqual(Desired, Solved(Desired + 5f), Epsilon);
        }

        [Test]
        public void WallJustBeyondTheDesiredDistance_StillShortens_SoTheSkinIsKept()
        {
            Assert.AreEqual(Desired - Skin / 2f, Solved(Desired + Skin / 2f), Epsilon);
            Assert.AreEqual(Desired - Skin, Solved(Desired), Epsilon);
        }

        [Test]
        public void WallRightAtThePivot_KeepsTheMinimumDistance()
        {
            Assert.AreEqual(MinDistance, Solved(0.1f), Epsilon);
            Assert.AreEqual(MinDistance, Solved(0f), Epsilon);
        }

        [Test]
        public void NegativeHitDistance_KeepsTheMinimumDistance()
        {
            Assert.AreEqual(MinDistance, Solved(-1f), Epsilon);
        }

        [Test]
        public void MinimumNeverExceedsTheDesiredDistance()
        {
            // A caller that asks for a closer camera than the minimum still gets what it asked for.
            Assert.AreEqual(0.3f, Solved(0.1f, 0.3f), Epsilon);
        }

        [Test]
        public void NaNHit_IsTreatedAsFree()
        {
            Assert.AreEqual(Desired, Solved(float.NaN), Epsilon);
        }

        [Test]
        public void ShortenedCamera_StaysOnTheSameRay_AndKeepsLookingAtThePivot()
        {
            OrbitSolver.Solve(Pivot, 50f, 25f, Desired, OrbitSolver.NoHit, Skin, MinDistance, out Vector3 freePosition, out Quaternion freeRotation);
            OrbitSolver.Solve(Pivot, 50f, 25f, Desired, 1.5f, Skin, MinDistance, out Vector3 shortPosition, out Quaternion shortRotation);

            AssertVector((freePosition - Pivot).normalized, (shortPosition - Pivot).normalized);
            Assert.AreEqual(freeRotation, shortRotation);
            Assert.AreEqual(1.5f - Skin, Vector3.Distance(Pivot, shortPosition), Epsilon);
        }

        // ---- pitch ----

        [Test]
        public void ClampPitch_StaysInsideTheLimits()
        {
            Assert.AreEqual(-10f, OrbitSolver.ClampPitch(-45f, -10f, 70f));
            Assert.AreEqual(70f, OrbitSolver.ClampPitch(85f, -10f, 70f));
            Assert.AreEqual(25f, OrbitSolver.ClampPitch(25f, -10f, 70f));
        }
    }
}
