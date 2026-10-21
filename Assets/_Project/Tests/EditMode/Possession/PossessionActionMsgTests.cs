using CinliMahzen.Core;
using CinliMahzen.Core.Net;
using CinliMahzen.Jinn;
using CinliMahzen.Possession;
using NUnit.Framework;
using UnityEngine;

namespace CinliMahzen.Tests.EditMode.Possession
{
    /// <summary>TODO 1.8 codecs (round trip + rejection) and <see cref="ProjectilePath"/>.</summary>
    public class PossessionActionMsgTests
    {
        private static readonly NetId Obj = new NetId(40003);
        private static readonly PlayerId Evil = new PlayerId(2);

        private static NetMsg With(MsgCode code, params object[] payload) => new NetMsg { Code = code, Payload = payload };

        [Test]
        public void ReqAction_RoundTrip_AndRejects()
        {
            Assert.IsTrue(ReqActionMsg.TryDecode(ReqActionMsg.Encode(1, Vector3.forward), out int index, out Vector3 aim));
            Assert.AreEqual(1, index);
            Assert.AreEqual(Vector3.forward, aim);

            Assert.IsFalse(ReqActionMsg.TryDecode(With(MsgCode.ReqAction, (byte)2, Vector3.forward), out _, out _), "index 2");
            Assert.IsFalse(ReqActionMsg.TryDecode(With(MsgCode.ReqAction, (byte)0, new Vector3(float.NaN, 0f, 0f)), out _, out _), "NaN aim");
            Assert.IsFalse(ReqActionMsg.TryDecode(With(MsgCode.ReqAction, 0, Vector3.forward), out _, out _), "index not a byte");
        }

        [Test]
        public void Telegraph_Resolved_StateChanged_RoundTrip()
        {
            Assert.IsTrue(ActionTelegraphMsg.TryDecode(ActionTelegraphMsg.Encode(Obj, 1, 12.5d), out NetId o1, out int i1, out double at));
            Assert.AreEqual(Obj, o1);
            Assert.AreEqual(1, i1);
            Assert.AreEqual(12.5d, at);

            var end = new Vector3(1f, 0f, 2f);
            Assert.IsTrue(ActionResolvedMsg.TryDecode(ActionResolvedMsg.Encode(Obj, 0, true, true, end, 45f, 20d),
                out NetId o2, out int i2, out bool hit, out bool spent, out Vector3 pos, out float yaw, out double until));
            Assert.AreEqual(Obj, o2);
            Assert.AreEqual(0, i2);
            Assert.IsTrue(hit);
            Assert.IsTrue(spent);
            Assert.AreEqual(end, pos);
            Assert.AreEqual(45f, yaw);
            Assert.AreEqual(20d, until);

            Assert.IsTrue(ObjectStateChangedMsg.TryDecode(ObjectStateChangedMsg.Encode(Obj, PossessionPhase.Lurking), out NetId o3, out PossessionPhase phase));
            Assert.AreEqual(Obj, o3);
            Assert.AreEqual(PossessionPhase.Lurking, phase);
            Assert.IsFalse(ObjectStateChangedMsg.TryDecode(ObjectStateChangedMsg.Encode(Obj, PossessionPhase.Free), out _, out _), "only inside phases");
        }

        [Test]
        public void Denied_RoundTrip_AndRejectsNone()
        {
            Assert.IsTrue(ActionDeniedMsg.TryDecode(ActionDeniedMsg.Encode(Evil, ActionDenyReason.Cooldown), out PlayerId who, out ActionDenyReason reason));
            Assert.AreEqual(Evil, who);
            Assert.AreEqual(ActionDenyReason.Cooldown, reason);
            Assert.IsFalse(ActionDeniedMsg.TryDecode(ActionDeniedMsg.Encode(Evil, ActionDenyReason.None), out _, out _));
            Assert.IsFalse(ActionDeniedMsg.TryDecode(With(MsgCode.ActionDenied, (byte)2, (byte)99), out _, out _));
        }

        [Test]
        public void Projectile_Spawned_Impact_RoundTrip()
        {
            var path = new ProjectilePath(Vector3.up, Vector3.forward, 10f, 8f, 1f, 3d);
            Assert.IsTrue(ProjectileSpawnedMsg.TryDecode(ProjectileSpawnedMsg.Encode(Obj, path), out NetId obj, out ProjectilePath decoded));
            Assert.AreEqual(Obj, obj);
            Assert.AreEqual(path.Origin, decoded.Origin);
            Assert.AreEqual(path.Direction, decoded.Direction);
            Assert.AreEqual(10f, decoded.Speed);
            Assert.AreEqual(8f, decoded.Range);
            Assert.AreEqual(1f, decoded.Arc);
            Assert.AreEqual(3d, decoded.StartTime);
            Assert.IsFalse(ProjectileSpawnedMsg.TryDecode(With(MsgCode.ProjectileSpawned, 5, Vector3.zero, Vector3.zero, 1f, 1f, 0f, 0d), out _, out _), "no direction");

            Assert.IsTrue(ProjectileImpactMsg.TryDecode(ProjectileImpactMsg.Encode(Obj, Vector3.one, true), out NetId o2, out Vector3 point, out bool hit));
            Assert.AreEqual(Obj, o2);
            Assert.AreEqual(Vector3.one, point);
            Assert.IsTrue(hit);
        }

        [Test]
        public void Move_Moved_Radio_Cologne_Energy_RoundTrip()
        {
            Assert.IsTrue(ReqPossessedMoveMsg.TryDecode(ReqPossessedMoveMsg.Encode(Vector3.right), out Vector3 dir));
            Assert.AreEqual(Vector3.right, dir);

            Assert.IsTrue(ObjectMovedMsg.TryDecode(ObjectMovedMsg.Encode(Obj, Vector3.one, 90f), out NetId obj, out Vector3 pos, out float yaw));
            Assert.AreEqual(Obj, obj);
            Assert.AreEqual(Vector3.one, pos);
            Assert.AreEqual(90f, yaw);

            Assert.IsTrue(RadioStateChangedMsg.TryDecode(RadioStateChangedMsg.Encode(4, 30d), out int room, out double until));
            Assert.AreEqual(4, room);
            Assert.AreEqual(30d, until);
            Assert.IsFalse(RadioStateChangedMsg.TryDecode(With(MsgCode.RadioStateChanged, -1, 1d), out _, out _));

            Assert.IsTrue(CologneReviveTriggeredMsg.TryDecode(CologneReviveTriggeredMsg.Encode(new PlayerId(1)), out PlayerId by));
            Assert.AreEqual(new PlayerId(1), by);

            Assert.IsTrue(EnergyChangedMsg.TryDecode(EnergyChangedMsg.Encode(Evil, 42.5f), out PlayerId who, out float value));
            Assert.AreEqual(Evil, who);
            Assert.AreEqual(42.5f, value);
            Assert.IsFalse(EnergyChangedMsg.TryDecode(With(MsgCode.EnergyChanged, (byte)2, -1f), out _, out _), "negative energy");
        }

        [Test]
        public void ProjectilePath_StraightAndArced()
        {
            var straight = new ProjectilePath(Vector3.zero, new Vector3(0f, 0f, 2f), 10f, 10f, 0f, 0d);
            Assert.AreEqual(Vector3.forward, straight.Direction, "normalised");
            Assert.AreEqual(1f, straight.Duration);
            Assert.AreEqual(new Vector3(0f, 0f, 5f), straight.PositionAt(0.5d));
            Assert.AreEqual(new Vector3(0f, 0f, 10f), straight.PositionAt(3d), "clamped at the range");
            Assert.IsTrue(straight.IsFinished(1d));

            var arced = new ProjectilePath(Vector3.zero, Vector3.forward, 10f, 10f, 1.5f, 0d);
            Assert.AreEqual(1.5f, arced.PositionAt(0.5d).y, 1e-5f, "apex at mid-range");
            Assert.AreEqual(0f, arced.PositionAt(1d).y, 1e-5f, "back at launch height at the end");
            Assert.AreEqual(Vector3.zero, arced.PositionAt(-1d), "before launch");
        }
    }
}
