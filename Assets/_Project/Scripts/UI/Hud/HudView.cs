using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.Core.Net;
using UnityEngine;

namespace CinliMahzen.UI
{
    /// <summary>
    /// The three role HUDs, role reveal, round report and match end (Oyun §13, Teknik §10, TODO 3.11) — <b>placeholder rendering</b> with IMGUI,
    /// like the main menu, until the uGUI/TextMeshPro art pass. Created hidden and persistent by <see cref="HudBootstrap"/>; draws nothing in
    /// Lobby / RoundSetup (so it is invisible in the main menu, where the match stub stays in Lobby).
    /// <para>Data: live values straight from the Core services (<see cref="IHumanStatusQuery"/>, <see cref="IJinnEnergyService"/>,
    /// <see cref="IObjectiveInfo"/>, <see cref="IMatchInfo"/>, <see cref="ICameraRig"/>), change-driven values from <see cref="HudState"/>.
    /// Every text is a <c>Loc</c> key and is rebuilt only when the number behind it changes (<see cref="Cached"/>), not every frame.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] private int fontSize = 18;
        [SerializeField] private int bigFontSize = 40;
        [SerializeField] private float margin = 16f;
        [SerializeField] private float lineHeight = 26f;
        [SerializeField] private float panelWidth = 420f;
        [SerializeField] private int barCells = 10;
        [Tooltip("How long the '+90 s' sunrise bonus stays on screen (s).")]
        [SerializeField] private float timeBonusShowSeconds = 2.5f;

        /// <summary>A text rebuilt only when its (up to three) keys change.</summary>
        private sealed class Cached
        {
            private int _a = int.MinValue;
            private int _b = int.MinValue;
            private int _c = int.MinValue;
            public string Text = string.Empty;

            public bool Changed(int a, int b = 0, int c = 0)
            {
                if (a == _a && b == _b && c == _c)
                    return false;
                _a = a;
                _b = b;
                _c = c;
                return true;
            }
        }

        private readonly Cached _sunrise = new Cached();
        private readonly Cached _bonus = new Cached();
        private readonly Cached _stage = new Cached();
        private readonly Cached _hearts = new Cached();
        private readonly Cached _faints = new Cached();
        private readonly Cached _stamina = new Cached();
        private readonly Cached _lantern = new Cached();
        private readonly Cached _items = new Cached();
        private readonly Cached _dig = new Cached();
        private readonly Cached _prompt = new Cached();
        private readonly Cached _energy = new Cached();
        private readonly Cached _humanStatus = new Cached();
        private readonly Cached _humanDir = new Cached();
        private readonly Cached _inside = new Cached();
        private readonly Cached _teammate = new Cached();
        private readonly Cached _scramble = new Cached();
        private readonly Cached _reveal = new Cached();
        private readonly Cached _report = new Cached();
        private readonly Cached _matchEnd = new Cached();
        private readonly System.Text.StringBuilder _sb = new System.Text.StringBuilder(256);

        private HudState _state;
        private INetBridge _net;
        private IPlayerRegistry _players;
        private GameBalanceConfig _config;
        private GUIStyle _style;
        private GUIStyle _big;
        private GUIStyle _center;

        public HudState State => _state;

        private void Awake()
        {
            _net = GameServices.Net;
            _players = GameServices.Players;
            _config = GameServices.Config;
            if (_net == null || _players == null || _config == null)
            {
                CMLog.Warn("Hud", "GameServices.Net / Players / Config missing — HUD off");
                enabled = false;
                return;
            }
            _state = new HudState(_net, ReadLocalPlayer);
        }

        private void OnEnable()
        {
            _state?.Attach();
        }

        private void OnDisable()
        {
            _state?.Detach();
        }

        private PlayerId ReadLocalPlayer()
        {
            return _players.LocalPlayer;
        }

        private void OnGUI()
        {
            IMatchInfo match = GameServices.Match;
            if (match == null || _state == null)
                return;
            MatchState ms = match.State;
            if (ms == MatchState.Lobby || ms == MatchState.RoundSetup)
                return;
            EnsureStyles();

            PlayerId local = _players.LocalPlayer;
            Role role = _players.GetRole(local);
            double now = _net.Time;

            switch (ms)
            {
                case MatchState.RoleReveal:
                    DrawRoleReveal(role);
                    return;
                case MatchState.RoundEnd:
                    DrawRoundReport(match.RoundIndex);
                    return;
                case MatchState.MatchEnd:
                    DrawMatchEnd();
                    return;
            }

            DrawTopBar(match, now);
            float y = Screen.height - margin - lineHeight * 7f;
            switch (role)
            {
                case Role.Human:
                    DrawHuman(y);
                    break;
                case Role.GoodJinn:
                    DrawGoodJinn(y, local);
                    break;
                case Role.EvilJinn:
                    DrawEvilJinn(y, local, now);
                    break;
            }
            DrawPrompt();
        }

        // ---------- common ----------

        private void DrawTopBar(IMatchInfo match, double now)
        {
            float x = (Screen.width - panelWidth) * 0.5f;
            if (match.State == MatchState.Playing && match.StateEndTime < double.MaxValue)
            {
                int secs = Mathf.CeilToInt((float)(match.StateEndTime - now));
                if (_sunrise.Changed(secs))
                    _sunrise.Text = Loc.T("hud.sunrise", HudFormat.Clock(secs));
                GUI.Label(new Rect(x, margin, panelWidth, lineHeight), _sunrise.Text, _center);

                if (now - _state.TimeBonusAt <= timeBonusShowSeconds)
                {
                    int bonus = Mathf.RoundToInt(_state.TimeBonusSeconds);
                    if (_bonus.Changed(bonus))
                        _bonus.Text = Loc.T("hud.time_bonus", bonus);
                    GUI.Label(new Rect(x, margin + lineHeight, panelWidth, lineHeight), _bonus.Text, _center);
                }
            }

            IObjectiveInfo obj = GameServices.Get<IObjectiveInfo>();
            if (obj == null)
                return;
            if (_stage.Changed(obj.StageIndex * 64 + obj.StageCount, _state.PuzzlesSolved * 64 + obj.PuzzleCount, (int)obj.Phase))
                _stage.Text = Loc.T("hud.stage", Loc.T(PhaseKey(obj.Phase)), obj.StageIndex + 1, Mathf.Max(1, obj.StageCount), _state.PuzzlesSolved, obj.PuzzleCount);
            GUI.Label(new Rect(x, margin + lineHeight * 2f, panelWidth, lineHeight), _stage.Text, _center);
        }

        private void DrawPrompt()
        {
            if (!_state.PromptVisible || string.IsNullOrEmpty(_state.PromptKey))
                return;
            int cells = HudFormat.BarCells(_state.PromptProgress, barCells);
            if (_prompt.Changed(_state.PromptKey.GetHashCode(), cells))
            {
                string text = Loc.T(_state.PromptKey);
                _prompt.Text = cells > 0 ? text + "  " + HudFormat.Bar(_state.PromptProgress, barCells) : text;
            }
            float x = (Screen.width - panelWidth) * 0.5f;
            GUI.Label(new Rect(x, Screen.height * 0.62f, panelWidth, lineHeight), _prompt.Text, _center);
        }

        private float Line(float y, string text)
        {
            GUI.Label(new Rect(margin, y, panelWidth, lineHeight), text, _style);
            return y + lineHeight;
        }

        private float EnergyLine(float y, PlayerId local, float max)
        {
            IJinnEnergyService energy = GameServices.Get<IJinnEnergyService>();
            float value = energy != null ? energy.Get(local) : 0f;
            int shown = Mathf.FloorToInt(value);
            if (_energy.Changed(shown, Mathf.RoundToInt(max)))
                _energy.Text = Loc.T("hud.energy", shown, Mathf.RoundToInt(max)) + "  " + HudFormat.Bar(max > 0f ? value / max : 0f, barCells);
            return Line(y, _energy.Text);
        }

        // ---------- human ----------

        private void DrawHuman(float y)
        {
            IHumanStatusQuery human = GameServices.Get<IHumanStatusQuery>();
            if (human == null)
                return;
            if (_hearts.Changed(human.Hp, human.MaxHp))
                _hearts.Text = Loc.T("hud.hp") + " " + HudFormat.Hearts(human.Hp, human.MaxHp);
            y = Line(y, _hearts.Text);
            if (_faints.Changed(human.Faints, _config.FaintsToLose))
                _faints.Text = Loc.T("hud.faints", human.Faints, _config.FaintsToLose);
            y = Line(y, _faints.Text);
            int cells = HudFormat.BarCells(human.Stamina01, barCells);
            if (_stamina.Changed(cells))
                _stamina.Text = Loc.T("hud.stamina") + " " + HudFormat.Bar(human.Stamina01, barCells);
            y = Line(y, _stamina.Text);
            bool blessed = human.IsBlessedLanternActive;
            if (_lantern.Changed(blessed ? 1 : 0))
                _lantern.Text = Loc.T(blessed ? "hud.lantern_blessed" : "hud.lantern_weak");
            y = Line(y, _lantern.Text);
            if (_items.Changed(_state.InventoryVersion))
                _items.Text = BuildItems(_state.InventorySlots);
            y = Line(y, _items.Text);

            IObjectiveInfo obj = GameServices.Get<IObjectiveInfo>();
            if (obj != null && obj.Phase == ObjectivePhase.Cellar)
            {
                int digCells = HudFormat.BarCells(obj.DigProgress, barCells);
                if (_dig.Changed(digCells, obj.DigCheckpoint))
                    _dig.Text = Loc.T("hud.dig", obj.DigCheckpoint, _config.DigCheckpoints) + "  " + HudFormat.Bar(obj.DigProgress, barCells);
                Line(y, _dig.Text);
            }

            if (_state.ColdBreath)
            {
                float x = (Screen.width - panelWidth) * 0.5f;
                GUI.Label(new Rect(x, Screen.height * 0.3f, panelWidth, lineHeight), Loc.T("hud.cold_breath"), _center);
            }
        }

        private string BuildItems(IReadOnlyList<string> slots)
        {
            _sb.Clear();
            _sb.Append(Loc.T("hud.items")).Append(':');
            if (slots.Count == 0)
                _sb.Append(' ').Append(Loc.T("hud.empty_slot"));
            for (int i = 0; i < slots.Count; i++)
            {
                _sb.Append(i == 0 ? " " : " · ");
                _sb.Append(string.IsNullOrEmpty(slots[i]) ? Loc.T("hud.empty_slot") : Loc.T("item." + slots[i]));
            }
            return _sb.ToString();
        }

        // ---------- good jinn ----------

        private void DrawGoodJinn(float y, PlayerId local)
        {
            y = EnergyLine(y, local, _config.GoodEnergyMax);
            if (_state.LocalFocusing)
                y = Line(y, Loc.T("hud.focus"));

            IHumanStatusQuery human = GameServices.Get<IHumanStatusQuery>();
            if (human == null)
                return;
            if (_humanStatus.Changed(human.Hp * 64 + human.MaxHp, human.Faints, human.IsFainted ? 1 : 0))
            {
                _humanStatus.Text = human.IsFainted
                    ? Loc.T("hud.human_fainted", human.Faints, _config.FaintsToLose)
                    : Loc.T("hud.human_status", HudFormat.Hearts(human.Hp, human.MaxHp), human.Faints, _config.FaintsToLose);
            }
            y = Line(y, _humanStatus.Text);

            ICameraRig rig = GameServices.Get<ICameraRig>();
            Camera cam = rig != null ? rig.Camera : null;
            if (cam != null)
            {
                Transform view = cam.transform;
                Vector3 to = human.Position - view.position;
                Vector3 flat = new Vector3(to.x, 0f, to.z);
                Vector3 forward = new Vector3(view.forward.x, 0f, view.forward.z);
                int meters = Mathf.RoundToInt(to.magnitude);
                char arrow = flat.sqrMagnitude > 0.01f && forward.sqrMagnitude > 0.0001f
                    ? HudFormat.Arrow(Vector3.SignedAngle(forward, flat, Vector3.up))
                    : '•';
                if (_humanDir.Changed(meters, arrow))
                    _humanDir.Text = Loc.T("hud.human_dir", meters, arrow);
                Line(y, _humanDir.Text);
            }
        }

        // ---------- evil jinn ----------

        private void DrawEvilJinn(float y, PlayerId local, double now)
        {
            y = EnergyLine(y, local, _config.EnergyMax);

            PossessionPhase phase = _state.PhaseOf(local);
            if (_inside.Changed((int)phase))
                _inside.Text = InsideText(phase);
            if (_inside.Text.Length > 0)
                y = Line(y, _inside.Text);

            PlayerId mate = FindTeammate(local);
            if (mate.IsValid)
            {
                PossessionPhase matePhase = _state.PhaseOf(mate);
                bool mateInside = matePhase == PossessionPhase.Lurking || matePhase == PossessionPhase.Charging || matePhase == PossessionPhase.Recovering;
                if (_teammate.Changed(mate.Value, mateInside ? 1 : 0, matePhase == PossessionPhase.Charging ? 1 : 0))
                    _teammate.Text = Loc.T("hud.teammate", mate.ToString(), Loc.T(mateInside ? "hud.teammate_inside" : "hud.teammate_free"));
                y = Line(y, _teammate.Text);
            }

            int cooldown = Mathf.CeilToInt((float)(_state.ScrambleCooldownUntil - now));
            if (cooldown < 0)
                cooldown = 0;
            if (_scramble.Changed(cooldown))
                _scramble.Text = cooldown > 0 ? Loc.T("hud.scramble_cd", cooldown) : Loc.T("hud.scramble_ready");
            y = Line(y, _scramble.Text);

            IHumanStatusQuery human = GameServices.Get<IHumanStatusQuery>();
            if (human != null && human.IsBlessedLanternActive)
                y = Line(y, Loc.T("hud.lantern_guard"));
            if (_state.Rage)
            {
                float x = (Screen.width - panelWidth) * 0.5f;
                GUI.Label(new Rect(x, margin + lineHeight * 3.5f, panelWidth, lineHeight * 1.6f), Loc.T("hud.rage"), _big);
            }
        }

        private static string InsideText(PossessionPhase phase)
        {
            switch (phase)
            {
                case PossessionPhase.Entering: return Loc.T("hud.inside_entering");
                case PossessionPhase.Lurking: return Loc.T("hud.inside_lurk");
                case PossessionPhase.Charging: return Loc.T("hud.inside_charge");
                case PossessionPhase.Recovering: return Loc.T("hud.inside_recover");
                default: return string.Empty;
            }
        }

        private PlayerId FindTeammate(PlayerId local)
        {
            IReadOnlyList<PlayerInfo> players = _players.Players;
            for (int i = 0; i < players.Count; i++)
            {
                PlayerId id = players[i].Id;
                if (id != local && _players.GetRole(id) == Role.EvilJinn)
                    return id;
            }
            return PlayerId.None;
        }

        // ---------- screens ----------

        private void DrawRoleReveal(Role role)
        {
            if (_reveal.Changed((int)role))
                _reveal.Text = Loc.T("hud.role_reveal", Loc.T(HudFormat.RoleKey(role)));
            GUI.Label(new Rect(0f, Screen.height * 0.4f, Screen.width, lineHeight * 2f), _reveal.Text, _big);
        }

        private void DrawRoundReport(int roundIndex)
        {
            if (_report.Changed(roundIndex, (int)_state.LastResult * 16 + (int)_state.LastReason, ScoreHash()))
            {
                _sb.Clear();
                _sb.Append(Loc.T("hud.round_end", roundIndex + 1)).Append('\n')
                   .Append(Loc.T(HudFormat.ResultKey(_state.LastResult))).Append(" — ").Append(Loc.T(HudFormat.ReasonKey(_state.LastReason))).Append("\n\n");
                AppendScores(true);
                _report.Text = _sb.ToString();
            }
            DrawPanel(_report.Text);
        }

        private void DrawMatchEnd()
        {
            if (_matchEnd.Changed(_state.Winner.Value, _state.TopScore, ScoreHash()))
            {
                _sb.Clear();
                _sb.Append(Loc.T("hud.match_end")).Append('\n');
                _sb.Append(_state.Winner.IsValid ? Loc.T("hud.winner", _state.Winner.ToString(), _state.TopScore) : Loc.T("hud.tie", _state.TopScore)).Append("\n\n");
                AppendScores(false);
                _sb.Append('\n').Append(Loc.T("hud.restart_hint"));
                _matchEnd.Text = _sb.ToString();
            }
            DrawPanel(_matchEnd.Text);
        }

        private void AppendScores(bool withRoundDelta)
        {
            IReadOnlyList<PlayerInfo> players = _players.Players;
            for (int i = 0; i < players.Count; i++)
            {
                PlayerInfo p = players[i];
                string role = Loc.T(HudFormat.RoleKey(_players.GetRole(p.Id)));
                _sb.Append(withRoundDelta
                    ? Loc.T("hud.score_row_delta", p.Nickname, role, p.Score, _state.RoundDeltaOf(p.Id))
                    : Loc.T("hud.score_row", p.Nickname, p.Score)).Append('\n');
            }
        }

        private int ScoreHash()
        {
            int hash = 17;
            IReadOnlyList<PlayerInfo> players = _players.Players;
            for (int i = 0; i < players.Count; i++)
                hash = hash * 31 + players[i].Score;
            return hash;
        }

        private void DrawPanel(string text)
        {
            float w = Mathf.Min(Screen.width - margin * 2f, panelWidth * 1.4f);
            float h = Mathf.Min(Screen.height - margin * 2f, lineHeight * 12f);
            var rect = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            GUI.Box(rect, GUIContent.none);
            GUI.Label(new Rect(rect.x + margin, rect.y + margin, rect.width - margin * 2f, rect.height - margin * 2f), text, _style);
        }

        private void EnsureStyles()
        {
            if (_style != null)
                return;
            _style = new GUIStyle(GUI.skin.label) { fontSize = fontSize, wordWrap = true };
            _center = new GUIStyle(_style) { alignment = TextAnchor.MiddleCenter };
            _big = new GUIStyle(_center) { fontSize = bigFontSize, fontStyle = FontStyle.Bold };
        }

        private static string PhaseKey(ObjectivePhase phase)
        {
            switch (phase)
            {
                case ObjectivePhase.Garden: return "hud.phase_garden";
                case ObjectivePhase.Cellar: return "hud.phase_cellar";
                case ObjectivePhase.Done: return "hud.phase_done";
                default: return "hud.phase_house";
            }
        }
    }
}
