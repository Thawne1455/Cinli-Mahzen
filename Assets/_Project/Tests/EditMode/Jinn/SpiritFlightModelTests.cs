using CinliMahzen.Core;
using CinliMahzen.Jinn;
using NUnit.Framework;
using UnityEngine;

namespace CinliMahzen.Tests.EditMode.Jinn
{
    /// <summary>Role speeds, boost, vertical, the unit-length cap and the house clamp (Oyun §11/§14, Teknik §6.1).</summary>
    public class SpiritFlightModelTests
    {
        // Oyun §14: EvilJinnSpeed 6.5, GoodJinnSpeed 7, SpiritBoostMult 1.5.
        private const float EvilSpeed = 6.5f;
        private const float GoodSpeed = 7f;
        private const float BoostMult = 1.5f;
        private const float Delta = 1e-4f;

        private SpiritFlightModel _model;

        [SetUp]
        public void SetUp()
        {
            _model = new SpiritFlightModel(EvilSpeed, GoodSpeed, BoostMult);
        }

        private static void AssertVector(Vector3 expected, Vector3 actual)
        {
            Assert.AreEqual(expected.x, actual.x, Delta, "x of " + actual);
            Assert.AreEqual(expected.y, actual.y, Delta, "y of " + actual);
            Assert.AreEqual(expected.z, actual.z, Delta, "z of " + actual);
        }

        private Vector3 Fly(Role role, Vector2 move, float vertical = 0f, bool boost = false)
        {
            return _model.Velocity(role, move, vertical, boost, Vector3.right, Vector3.forward);
        }

        // ---- Speeds ----

        [Test]
        public void SpeedFor_UsesTheRoleSpeed()
        {
            Assert.AreEqual(EvilSpeed, _model.SpeedFor(Role.EvilJinn, false), Delta);
            Assert.AreEqual(GoodSpeed, _model.SpeedFor(Role.GoodJinn, false), Delta);
        }

        [Test]
        public void SpeedFor_BoostMultipliesTheRoleSpeed()
        {
            Assert.AreEqual(EvilSpeed * BoostMult, _model.SpeedFor(Role.EvilJinn, true), Delta);
            Assert.AreEqual(GoodSpeed * BoostMult, _model.SpeedFor(Role.GoodJinn, true), Delta);
        }

        [Test]
        public void SpeedFor_NonJinnRolesDoNotFly()
        {
            Assert.AreEqual(0f, _model.SpeedFor(Role.Human, false));
            Assert.AreEqual(0f, _model.SpeedFor(Role.Human, true));
            Assert.AreEqual(0f, _model.SpeedFor(Role.None, false));
        }

        // ---- Velocity ----

        [Test]
        public void Velocity_Forward_IsRoleSpeedAlongTheView()
        {
            AssertVector(new Vector3(0f, 0f, EvilSpeed), Fly(Role.EvilJinn, new Vector2(0f, 1f)));
            AssertVector(new Vector3(0f, 0f, GoodSpeed), Fly(Role.GoodJinn, new Vector2(0f, 1f)));
        }

        [Test]
        public void Velocity_BackwardAndStrafe_FollowTheViewAxes()
        {
            AssertVector(new Vector3(0f, 0f, -EvilSpeed), Fly(Role.EvilJinn, new Vector2(0f, -1f)));
            AssertVector(new Vector3(EvilSpeed, 0f, 0f), Fly(Role.EvilJinn, new Vector2(1f, 0f)));
            AssertVector(new Vector3(-EvilSpeed, 0f, 0f), Fly(Role.EvilJinn, new Vector2(-1f, 0f)));
        }

        [Test]
        public void Velocity_UsesTheGivenViewAxes_NotTheWorldAxes()
        {
            // Looking along world +X (yaw 90): forward is +X, right is -Z.
            Vector3 velocity = _model.Velocity(Role.EvilJinn, new Vector2(0f, 1f), 0f, false, Vector3.back, Vector3.right);

            AssertVector(new Vector3(EvilSpeed, 0f, 0f), velocity);
        }

        [Test]
        public void Velocity_ForwardFollowsThePitch()
        {
            // Looking 36.87 degrees upward: W flies up and forward along the view.
            var forward = new Vector3(0f, 0.6f, 0.8f);

            Vector3 velocity = _model.Velocity(Role.GoodJinn, new Vector2(0f, 1f), 0f, false, Vector3.right, forward);

            AssertVector(forward * GoodSpeed, velocity);
        }

        [Test]
        public void Velocity_Vertical_IsWorldUpAndDown()
        {
            AssertVector(new Vector3(0f, EvilSpeed, 0f), Fly(Role.EvilJinn, Vector2.zero, 1f));
            AssertVector(new Vector3(0f, -EvilSpeed, 0f), Fly(Role.EvilJinn, Vector2.zero, -1f));

            // Even with a pitched view the vertical keys stay on the world axis.
            Vector3 pitched = _model.Velocity(Role.EvilJinn, Vector2.zero, 1f, false, Vector3.right, new Vector3(0f, 0.6f, 0.8f));
            AssertVector(new Vector3(0f, EvilSpeed, 0f), pitched);
        }

        [Test]
        public void Velocity_VerticalBeyondTheRangeIsClamped()
        {
            AssertVector(new Vector3(0f, EvilSpeed, 0f), Fly(Role.EvilJinn, Vector2.zero, 5f));
            AssertVector(new Vector3(0f, -EvilSpeed, 0f), Fly(Role.EvilJinn, Vector2.zero, -5f));
        }

        [Test]
        public void Velocity_Boost_MultipliesTheSpeed()
        {
            AssertVector(new Vector3(0f, 0f, EvilSpeed * BoostMult), Fly(Role.EvilJinn, new Vector2(0f, 1f), 0f, true));
            AssertVector(new Vector3(0f, GoodSpeed * BoostMult, 0f), Fly(Role.GoodJinn, Vector2.zero, 1f, true));
        }

        [Test]
        public void Velocity_Diagonal_IsNotFasterThanStraight()
        {
            Vector3 diagonal = Fly(Role.EvilJinn, new Vector2(1f, 1f));
            Vector3 all3 = Fly(Role.EvilJinn, new Vector2(1f, 1f), 1f, true);

            Assert.AreEqual(EvilSpeed, diagonal.magnitude, Delta);
            Assert.AreEqual(diagonal.x, diagonal.z, Delta, "an equal push on both axes stays a 45 degree line");
            Assert.AreEqual(EvilSpeed * BoostMult, all3.magnitude, Delta);
        }

        [Test]
        public void Velocity_HalfPush_StaysProportionallySlower()
        {
            AssertVector(new Vector3(0f, 0f, EvilSpeed * 0.5f), Fly(Role.EvilJinn, new Vector2(0f, 0.5f)));
        }

        [Test]
        public void Velocity_NoInput_IsZero()
        {
            AssertVector(Vector3.zero, Fly(Role.EvilJinn, Vector2.zero));
            AssertVector(Vector3.zero, Fly(Role.GoodJinn, Vector2.zero, 0f, true));
        }

        [Test]
        public void Velocity_NonJinnRole_IsZero()
        {
            AssertVector(Vector3.zero, Fly(Role.Human, new Vector2(0f, 1f), 1f, true));
            AssertVector(Vector3.zero, Fly(Role.None, new Vector2(1f, 1f)));
        }

        [Test]
        public void Velocity_NonFiniteInput_CountsAsNoInput()
        {
            AssertVector(Vector3.zero, Fly(Role.EvilJinn, new Vector2(float.NaN, float.NaN), float.NaN));
            AssertVector(Vector3.zero, Fly(Role.EvilJinn, new Vector2(float.PositiveInfinity, 0f), float.NegativeInfinity));
        }

        // ---- Bounds ----

        private static Bounds House()
        {
            // Center (0, 5, 0), size 40 x 10 x 40 -> x,z in [-20, 20], y in [0, 10] (the StubHouseInfo box).
            return new Bounds(new Vector3(0f, 5f, 0f), new Vector3(40f, 10f, 40f));
        }

        [Test]
        public void ClampToBounds_InsideStaysWhereItIs()
        {
            var inside = new Vector3(3f, 2f, -7f);

            AssertVector(inside, SpiritFlightModel.ClampToBounds(inside, House()));
        }

        [Test]
        public void ClampToBounds_OutsideGoesToTheNearestEdge_PerAxis()
        {
            AssertVector(new Vector3(20f, 2f, 0f), SpiritFlightModel.ClampToBounds(new Vector3(35f, 2f, 0f), House()));
            AssertVector(new Vector3(-20f, 2f, 0f), SpiritFlightModel.ClampToBounds(new Vector3(-35f, 2f, 0f), House()));
            AssertVector(new Vector3(0f, 10f, 0f), SpiritFlightModel.ClampToBounds(new Vector3(0f, 14f, 0f), House()));
            AssertVector(new Vector3(0f, 0f, 0f), SpiritFlightModel.ClampToBounds(new Vector3(0f, -3f, 0f), House()));
            AssertVector(new Vector3(0f, 2f, 20f), SpiritFlightModel.ClampToBounds(new Vector3(0f, 2f, 99f), House()));
        }

        [Test]
        public void ClampToBounds_CornerClampsEveryAxis()
        {
            AssertVector(new Vector3(20f, 10f, -20f), SpiritFlightModel.ClampToBounds(new Vector3(50f, 50f, -50f), House()));
        }

        [Test]
        public void ClampToBounds_OnTheEdgeIsInside()
        {
            var edge = new Vector3(20f, 10f, -20f);

            AssertVector(edge, SpiritFlightModel.ClampToBounds(edge, House()));
        }

        [Test]
        public void FlyingOutward_EndsOnTheBoundary_AndStaysThere()
        {
            Vector3 position = new Vector3(19f, 5f, 0f);
            Vector3 velocity = Fly(Role.EvilJinn, new Vector2(1f, 0f), 0f, true);

            for (int i = 0; i < 100; i++)
                position = SpiritFlightModel.ClampToBounds(position + velocity * 0.1f, House());

            AssertVector(new Vector3(20f, 5f, 0f), position);
        }

        // ---- Config ----

        [Test]
        public void FromConfig_TakesTheGddSpeedsAndBoost()
        {
            GameBalanceConfig config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            try
            {
                SpiritFlightModel model = SpiritFlightModel.FromConfig(config);

                Assert.AreEqual(config.EvilJinnSpeed, model.SpeedFor(Role.EvilJinn, false), Delta);
                Assert.AreEqual(config.GoodJinnSpeed, model.SpeedFor(Role.GoodJinn, false), Delta);
                Assert.AreEqual(config.EvilJinnSpeed * config.SpiritBoostMult, model.SpeedFor(Role.EvilJinn, true), Delta);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void FromConfig_NullThrows()
        {
            Assert.Throws<System.ArgumentNullException>(() => SpiritFlightModel.FromConfig(null));
        }
    }
}
