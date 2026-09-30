using System;
using System.Collections.Generic;

namespace CinliMahzen.Core.Net
{
    /// <summary>
    /// Offline / hotseat bridge (Teknik §4.3, TODO A0.6). This machine is always the authority:
    /// <see cref="SendToAuthority"/> and <see cref="Broadcast"/> invoke the registered handlers synchronously, in the same frame.
    /// Handlers registered/unregistered during dispatch are safe (new ones start with the next message).
    /// </summary>
    public sealed class OfflineNetBridge : INetBridge
    {
        private readonly List<Action<NetMsg>>[] _handlers = new List<Action<NetMsg>>[256];
        private readonly bool[] _dirty = new bool[256];
        private readonly Func<double> _clock;
        private readonly Func<PlayerId> _localPlayer;
        private int _dispatchDepth;

        /// <param name="clock">Defaults to UnityEngine.Time.timeAsDouble.</param>
        /// <param name="localPlayer">Defaults to GameServices.Players.LocalPlayer, or P0 when no registry exists yet.</param>
        public OfflineNetBridge(Func<double> clock = null, Func<PlayerId> localPlayer = null)
        {
            _clock = clock ?? (() => UnityEngine.Time.timeAsDouble);
            _localPlayer = localPlayer ?? DefaultLocalPlayer;
        }

        public bool IsOnline => false;

        /// <summary>Always true offline. Settable only so tests can exercise the non-authority paths.</summary>
        public bool IsAuthority { get; set; } = true;

        public PlayerId LocalPlayer => _localPlayer();

        public double Time => _clock();

        public void SendToAuthority(NetMsg msg)
        {
            msg.Sender = LocalPlayer;
            msg.SentTime = Time;
            Dispatch(msg);
        }

        public void Broadcast(NetMsg msg)
        {
            if (!IsAuthority)
            {
                CMLog.Error("Net", "Broadcast(" + msg.Code + ") called on a non-authority client — ignored");
                return;
            }
            msg.Sender = LocalPlayer;
            msg.SentTime = Time;
            Dispatch(msg);
        }

        public void Register(MsgCode code, Action<NetMsg> handler)
        {
            if (handler == null)
                return;
            List<Action<NetMsg>> list = _handlers[(byte)code];
            if (list == null)
            {
                list = new List<Action<NetMsg>>(4);
                _handlers[(byte)code] = list;
            }
            if (!list.Contains(handler))
                list.Add(handler);
        }

        public void Unregister(MsgCode code, Action<NetMsg> handler)
        {
            List<Action<NetMsg>> list = _handlers[(byte)code];
            if (list == null || handler == null)
                return;
            int i = list.IndexOf(handler);
            if (i < 0)
                return;
            if (_dispatchDepth > 0)
            {
                list[i] = null;
                _dirty[(byte)code] = true;
            }
            else
            {
                list.RemoveAt(i);
            }
        }

        /// <summary>Removes every handler (scene change / tests).</summary>
        public void ClearHandlers()
        {
            for (int i = 0; i < _handlers.Length; i++)
                _handlers[i]?.Clear();
        }

        private void Dispatch(in NetMsg msg)
        {
            byte code = (byte)msg.Code;
            List<Action<NetMsg>> list = _handlers[code];
            if (list == null || list.Count == 0)
                return;

            _dispatchDepth++;
            int count = list.Count;
            for (int i = 0; i < count; i++)
            {
                Action<NetMsg> h = list[i];
                if (h == null)
                    continue;
                try
                {
                    h(msg);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                }
            }
            _dispatchDepth--;

            if (_dispatchDepth == 0 && _dirty[code])
            {
                list.RemoveAll(h => h == null);
                _dirty[code] = false;
            }
        }

        private static PlayerId DefaultLocalPlayer()
        {
            IPlayerRegistry players = GameServices.Players;
            return players != null ? players.LocalPlayer : new PlayerId(0);
        }
    }
}
