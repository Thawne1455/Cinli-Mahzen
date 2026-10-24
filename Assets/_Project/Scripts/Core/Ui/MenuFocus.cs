using CinliMahzen.Core.Events;
using CinliMahzen.Core.Net;
using UnityEngine;

namespace CinliMahzen.Core
{
    /// <summary>
    /// TODO 4.2: who owns the mouse and keyboard — the game or a menu (pause, settings). Registered by <see cref="GameBootstrap"/>.
    /// Opening frees the cursor (and, for an offline pause, stops game time); closing restores both. The local input source listens to
    /// <see cref="MenuFocusChangedEvt"/> and switches the gameplay maps off while a menu is open. Online, time never stops.
    /// </summary>
    public sealed class MenuFocus
    {
        private CursorLockMode _savedLock;
        private bool _savedVisible;
        private float _savedTimeScale = 1f;
        private bool _frozeTime;

        public bool IsOpen { get; private set; }

        /// <param name="open">True: a menu takes the input.</param>
        /// <param name="freezeTime">Only on open: stop Time.timeScale (offline pause). Ignored online.</param>
        public void SetOpen(bool open, bool freezeTime = false)
        {
            if (open == IsOpen)
                return;
            IsOpen = open;
            if (open)
            {
                _savedLock = Cursor.lockState;
                _savedVisible = Cursor.visible;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                INetBridge net = GameServices.Net;
                _frozeTime = freezeTime && (net == null || !net.IsOnline);
                if (_frozeTime)
                {
                    _savedTimeScale = Time.timeScale;
                    Time.timeScale = 0f;
                }
            }
            else
            {
                Cursor.lockState = _savedLock;
                Cursor.visible = _savedVisible;
                if (_frozeTime)
                    Time.timeScale = _savedTimeScale;
                _frozeTime = false;
            }
            EventBus.Publish(new MenuFocusChangedEvt(open));
        }
    }
}
