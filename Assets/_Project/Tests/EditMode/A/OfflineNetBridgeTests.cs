using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.Core.Net;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CinliMahzen.Tests.EditMode.A
{
    public class OfflineNetBridgeTests
    {
        private double _now;
        private PlayerId _local;
        private OfflineNetBridge _net;

        [SetUp]
        public void SetUp()
        {
            _now = 12.5d;
            _local = new PlayerId(2);
            _net = new OfflineNetBridge(() => _now, () => _local);
        }

        [Test]
        public void Defaults_OfflineAuthority()
        {
            Assert.IsFalse(_net.IsOnline);
            Assert.IsTrue(_net.IsAuthority);
            Assert.AreEqual(_local, _net.LocalPlayer);
            Assert.AreEqual(12.5d, _net.Time);
        }

        [Test]
        public void SendToAuthority_RoundTrip_SameFrame_FillsSenderAndTime()
        {
            NetMsg got = default;
            int calls = 0;
            _net.Register(MsgCode.ReqPossess, m => { got = m; calls++; });

            _net.SendToAuthority(new NetMsg { Code = MsgCode.ReqPossess, Payload = new object[] { 1234, 2.5f, "x" } });

            Assert.AreEqual(1, calls, "handler runs synchronously");
            Assert.AreEqual(MsgCode.ReqPossess, got.Code);
            Assert.AreEqual(_local, got.Sender);
            Assert.AreEqual(12.5d, got.SentTime);
            Assert.AreEqual(1234, (int)got.Payload[0]);
            Assert.AreEqual(2.5f, (float)got.Payload[1]);
            Assert.AreEqual("x", (string)got.Payload[2]);
        }

        [Test]
        public void RequestThenBroadcast_FullAuthorityLoop()
        {
            var results = new List<int>();
            _net.Register(MsgCode.ReqInteract, req =>
            {
                Assert.IsTrue(_net.IsAuthority);
                _net.Broadcast(new NetMsg { Code = MsgCode.InteractResult, Payload = new object[] { (int)req.Payload[0] + 1 } });
            });
            _net.Register(MsgCode.InteractResult, res => results.Add((int)res.Payload[0]));
            _net.Register(MsgCode.InteractResult, res => results.Add(-(int)res.Payload[0]));

            _net.SendToAuthority(new NetMsg { Code = MsgCode.ReqInteract, Payload = new object[] { 41 } });

            CollectionAssert.AreEqual(new[] { 42, -42 }, results);
        }

        [Test]
        public void Broadcast_FromNonAuthority_LogsErrorAndDrops()
        {
            int calls = 0;
            _net.Register(MsgCode.HumanDamaged, _ => calls++);
            _net.IsAuthority = false;

            LogAssert.Expect(LogType.Error, "[Net] Broadcast(HumanDamaged) called on a non-authority client — ignored");
            _net.Broadcast(new NetMsg { Code = MsgCode.HumanDamaged });

            Assert.AreEqual(0, calls);
        }

        [Test]
        public void OnlyMatchingCode_IsDispatched()
        {
            int a = 0, b = 0;
            _net.Register(MsgCode.KickResult, _ => a++);
            _net.Register(MsgCode.ReqKick, _ => b++);
            _net.Broadcast(new NetMsg { Code = MsgCode.KickResult });
            Assert.AreEqual(1, a);
            Assert.AreEqual(0, b);
        }

        [Test]
        public void Unregister_StopsDelivery_AndDuplicatesIgnored()
        {
            int calls = 0;
            System.Action<NetMsg> h = _ => calls++;
            _net.Register(MsgCode.PingPlaced, h);
            _net.Register(MsgCode.PingPlaced, h);
            _net.Broadcast(new NetMsg { Code = MsgCode.PingPlaced });
            Assert.AreEqual(1, calls, "duplicate registration ignored");

            _net.Unregister(MsgCode.PingPlaced, h);
            _net.Broadcast(new NetMsg { Code = MsgCode.PingPlaced });
            Assert.AreEqual(1, calls);
        }

        [Test]
        public void UnregisterDuringDispatch_IsSafe()
        {
            int first = 0, second = 0;
            System.Action<NetMsg> h2 = _ => second++;
            System.Action<NetMsg> h1 = null;
            h1 = _ =>
            {
                first++;
                _net.Unregister(MsgCode.LevelBuilt, h1);
                _net.Unregister(MsgCode.LevelBuilt, h2);
            };
            _net.Register(MsgCode.LevelBuilt, h1);
            _net.Register(MsgCode.LevelBuilt, h2);

            _net.Broadcast(new NetMsg { Code = MsgCode.LevelBuilt });
            _net.Broadcast(new NetMsg { Code = MsgCode.LevelBuilt });

            Assert.AreEqual(1, first);
            Assert.AreEqual(0, second, "removed before its turn");
        }

        [Test]
        public void HandlerException_DoesNotStopOthers()
        {
            int calls = 0;
            _net.Register(MsgCode.DebugCommand, _ => throw new System.InvalidOperationException("boom"));
            _net.Register(MsgCode.DebugCommand, _ => calls++);

            LogAssert.Expect(LogType.Exception, "InvalidOperationException: boom");
            _net.Broadcast(new NetMsg { Code = MsgCode.DebugCommand });

            Assert.AreEqual(1, calls);
        }
    }
}
