using System.Text;
using CinliMahzen.Core;
using CinliMahzen.Core.Net;
using UnityEngine;

namespace CinliMahzen.DebugTools
{
    /// <summary>
    /// F9 debug overlay (Teknik §12.1): FPS, local player/role, match state, remaining time, objective, last 10 NetMsgs.
    /// Developer-only text (not localised). Text is rebuilt 4×/s into a reused StringBuilder.
    /// </summary>
    public sealed class DebugOverlay : MonoBehaviour
    {
        private const float RefreshInterval = 0.25f;

        private readonly StringBuilder _sb = new StringBuilder(1024);
        private string _text = string.Empty;
        private float _nextRefresh;
        private float _fpsAccum;
        private int _fpsFrames;
        private float _fps;
        private GUIStyle _style;

        public static bool Visible { get; set; }

        private void Update()
        {
            _fpsAccum += Time.unscaledDeltaTime;
            _fpsFrames++;
            if (Time.unscaledTime < _nextRefresh)
                return;
            _nextRefresh = Time.unscaledTime + RefreshInterval;
            _fps = _fpsAccum > 0f ? _fpsFrames / _fpsAccum : 0f;
            _fpsAccum = 0f;
            _fpsFrames = 0;
            if (Visible)
                Rebuild();
        }

        private void Rebuild()
        {
            _sb.Clear();
            _sb.Append("FPS ").Append(Mathf.RoundToInt(_fps)).Append("   timeScale x").Append(Time.timeScale).Append('\n');

            INetBridge net = GameServices.Net;
            PlayerId local = net != null ? net.LocalPlayer : PlayerId.None;
            IPlayerRegistry players = GameServices.Players;
            Role role = players != null ? players.GetRole(local) : Role.None;
            _sb.Append("Local ").Append(local.ToString()).Append("  role ").Append(role.ToString())
               .Append(net == null ? "  [no net]" : net.IsAuthority ? "  [authority]" : "  [client]").Append('\n');

            IMatchInfo match = GameServices.Match;
            if (match != null)
            {
                _sb.Append("Match ").Append(match.State.ToString()).Append("  round ").Append(match.RoundIndex)
                   .Append("  jinnsAwake ").Append(match.JinnsAwake ? "yes" : "no");
                if (net != null && match.StateEndTime < double.MaxValue)
                    _sb.Append("  left ").Append(Mathf.Max(0f, (float)(match.StateEndTime - net.Time)).ToString("0.0")).Append('s');
                _sb.Append(match is StubMatchInfo ? "  (stub)" : string.Empty).Append('\n');
            }

            IObjectiveInfo obj = GameServices.Get<IObjectiveInfo>();
            if (obj != null)
            {
                _sb.Append("Objective ").Append(obj.Phase.ToString()).Append("  fragments ").Append(obj.Fragments)
                   .Append("  vault ").Append(obj.VaultOpen ? "open" : "closed")
                   .Append(obj is StubObjectiveInfo ? "  (stub)" : string.Empty).Append('\n');
            }

            _sb.Append("Energy / possession: TODO (B)\n");
            _sb.Append("-- last NetMsg --\n");
            for (int i = 0; i < NetDebugLog.Count; i++)
            {
                NetDebugLog.Entry e = NetDebugLog.Get(i);
                _sb.Append(e.IsBroadcast ? "B " : "R ").Append(e.Code.ToString()).Append("  ").Append(e.Sender.ToString())
                   .Append("  t=").Append(e.SentTime.ToString("0.00")).Append('\n');
            }
            _text = _sb.ToString();
        }

        private void OnGUI()
        {
            if (!Visible)
                return;
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 13, richText = false };
                _style.normal.textColor = Color.white;
                Rebuild();
            }
            GUI.Box(new Rect(10f, 10f, 460f, 300f), _text, _style);
        }
    }
}
