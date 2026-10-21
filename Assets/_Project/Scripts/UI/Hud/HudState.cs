using System;
using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.Core.Events;
using CinliMahzen.Core.Net;

namespace CinliMahzen.UI
{
    /// <summary>
    /// What the HUDs show that is not a live query (Oyun §13, Teknik §10, TODO 3.11), plain C#: it listens to the local events and the
    /// broadcast messages every machine receives and keeps the latest values. Live values (HP, energy, timers, dig progress) are read by the
    /// view straight from the Core services; this class only holds what arrives as a change.
    /// Round values reset on <see cref="RoundStartedEvt"/>; the round report (result, per-player score deltas) stays until the next round.
    /// Nothing here changes game state.
    /// </summary>
    public sealed class HudState
    {
        /// <summary>Players tracked per id value (offline 4; ids at or above this are ignored).</summary>
        public const int MaxPlayers = 8;

        private readonly INetBridge _net;
        private readonly Func<PlayerId> _localPlayer;
        private readonly HashSet<int> _solved = new HashSet<int>();
        private readonly PossessionPhase[] _phases = new PossessionPhase[MaxPlayers];
        private readonly int[] _roundDelta = new int[MaxPlayers];
        private string[] _slots = Array.Empty<string>();

        private readonly Action<NetMsg> _onInventory;
        private readonly Action<NetMsg> _onRage;
        private readonly Action<RoundStartedEvt> _onRound;
        private readonly Action<RoundEndedEvt> _onRoundEnded;
        private readonly Action<RoundTimeAddedEvt> _onTimeAdded;
        private readonly Action<PuzzleCompletedEvt> _onPuzzle;
        private readonly Action<PuzzleScrambledEvt> _onScrambled;
        private readonly Action<ReferenceFocusChangedEvt> _onFocus;
        private readonly Action<InteractPromptEvt> _onPrompt;
        private readonly Action<PossessionChangedEvt> _onPossession;
        private readonly Action<ColdBreathChangedEvt> _onColdBreath;
        private readonly Action<ScoreChangedEvt> _onScore;
        private readonly Action<MatchEndedEvt> _onMatchEnded;

        public HudState(INetBridge net, Func<PlayerId> localPlayer)
        {
            _net = net ?? throw new ArgumentNullException(nameof(net));
            _localPlayer = localPlayer ?? throw new ArgumentNullException(nameof(localPlayer));
            _onInventory = OnInventoryMsg;
            _onRage = OnRageMsg;
            _onRound = OnRoundStarted;
            _onRoundEnded = OnRoundEnded;
            _onTimeAdded = OnTimeAdded;
            _onPuzzle = OnPuzzleCompleted;
            _onScrambled = OnPuzzleScrambled;
            _onFocus = OnFocusChanged;
            _onPrompt = OnPrompt;
            _onPossession = OnPossessionChanged;
            _onColdBreath = OnColdBreath;
            _onScore = OnScoreChanged;
            _onMatchEnded = OnMatchEnded;
        }

        // ---- Human ----

        /// <summary>The human's carried items (item ids, one per slot; empty string = free slot).</summary>
        public IReadOnlyList<string> InventorySlots => _slots;

        /// <summary>Incremented whenever <see cref="InventorySlots"/> changes (the view rebuilds its texts only then).</summary>
        public int InventoryVersion { get; private set; }

        public bool ColdBreath { get; private set; }

        public bool PromptVisible { get; private set; }

        public string PromptKey { get; private set; } = string.Empty;

        public float PromptProgress { get; private set; }

        // ---- Round ----

        /// <summary>Distinct puzzles solved this round.</summary>
        public int PuzzlesSolved => _solved.Count;

        /// <summary>Net.Time of the last <c>+StageTimeBonus</c> (the view animates it for a moment); <c>double.MinValue</c> when none.</summary>
        public double TimeBonusAt { get; private set; } = double.MinValue;

        public float TimeBonusSeconds { get; private set; }

        /// <summary>The cellar's Öfke is on (RageStarted).</summary>
        public bool Rage { get; private set; }

        /// <summary>Latest end of a mass-sabotage cooldown this round (Net.Time); <c>double.MinValue</c> when none.</summary>
        public double ScrambleCooldownUntil { get; private set; } = double.MinValue;

        // ---- Jinns ----

        /// <summary>The local good jinn is reading a reference (Spirit Veil focus).</summary>
        public bool LocalFocusing { get; private set; }

        /// <summary>Possession phase of <paramref name="player"/> as last broadcast (Free when unknown).</summary>
        public PossessionPhase PhaseOf(PlayerId player)
        {
            return player.Value < MaxPlayers ? _phases[player.Value] : PossessionPhase.Free;
        }

        // ---- Report / match end ----

        public RoundResult LastResult { get; private set; }

        public RoundEndReason LastReason { get; private set; }

        /// <summary>Score gained by <paramref name="player"/> this round (round report).</summary>
        public int RoundDeltaOf(PlayerId player)
        {
            return player.Value < MaxPlayers ? _roundDelta[player.Value] : 0;
        }

        public bool MatchEnded { get; private set; }

        public PlayerId Winner { get; private set; } = PlayerId.None;

        public int TopScore { get; private set; }

        public void Attach()
        {
            _net.Register(MsgCode.InventoryChanged, _onInventory);
            _net.Register(MsgCode.RageStarted, _onRage);
            EventBus.Subscribe(_onRound);
            EventBus.Subscribe(_onRoundEnded);
            EventBus.Subscribe(_onTimeAdded);
            EventBus.Subscribe(_onPuzzle);
            EventBus.Subscribe(_onScrambled);
            EventBus.Subscribe(_onFocus);
            EventBus.Subscribe(_onPrompt);
            EventBus.Subscribe(_onPossession);
            EventBus.Subscribe(_onColdBreath);
            EventBus.Subscribe(_onScore);
            EventBus.Subscribe(_onMatchEnded);
        }

        public void Detach()
        {
            _net.Unregister(MsgCode.InventoryChanged, _onInventory);
            _net.Unregister(MsgCode.RageStarted, _onRage);
            EventBus.Unsubscribe(_onRound);
            EventBus.Unsubscribe(_onRoundEnded);
            EventBus.Unsubscribe(_onTimeAdded);
            EventBus.Unsubscribe(_onPuzzle);
            EventBus.Unsubscribe(_onScrambled);
            EventBus.Unsubscribe(_onFocus);
            EventBus.Unsubscribe(_onPrompt);
            EventBus.Unsubscribe(_onPossession);
            EventBus.Unsubscribe(_onColdBreath);
            EventBus.Unsubscribe(_onScore);
            EventBus.Unsubscribe(_onMatchEnded);
        }

        private void OnInventoryMsg(NetMsg msg)
        {
            if (!InventoryChangedMsg.TryDecode(msg, out _, out string[] slots))
                return;
            _slots = slots ?? Array.Empty<string>();
            InventoryVersion++;
        }

        private void OnRageMsg(NetMsg msg)
        {
            if (CellarMsgs.TryDecodeRage(msg, out bool active))
                Rage = active;
        }

        private void OnRoundStarted(RoundStartedEvt evt)
        {
            _solved.Clear();
            Array.Clear(_phases, 0, _phases.Length);
            Array.Clear(_roundDelta, 0, _roundDelta.Length);
            _slots = Array.Empty<string>();
            InventoryVersion++;
            Rage = false;
            ScrambleCooldownUntil = double.MinValue;
            TimeBonusAt = double.MinValue;
            LocalFocusing = false;
            ColdBreath = false;
            PromptVisible = false;
            LastResult = RoundResult.None;
            LastReason = RoundEndReason.None;
            if (evt.RoundIndex == 0)
            {
                MatchEnded = false;
                Winner = PlayerId.None;
                TopScore = 0;
            }
        }

        private void OnRoundEnded(RoundEndedEvt evt)
        {
            LastResult = evt.Result;
            LastReason = evt.Reason;
        }

        private void OnTimeAdded(RoundTimeAddedEvt evt)
        {
            TimeBonusAt = _net.Time;
            TimeBonusSeconds = evt.BonusSeconds;
        }

        private void OnPuzzleCompleted(PuzzleCompletedEvt evt)
        {
            _solved.Add(evt.PuzzleSlot);
        }

        private void OnPuzzleScrambled(PuzzleScrambledEvt evt)
        {
            if (evt.CooldownUntil > ScrambleCooldownUntil)
                ScrambleCooldownUntil = evt.CooldownUntil;
        }

        private void OnFocusChanged(ReferenceFocusChangedEvt evt)
        {
            if (evt.Jinn == _localPlayer())
                LocalFocusing = evt.IsFocusing;
        }

        private void OnPrompt(InteractPromptEvt evt)
        {
            PromptVisible = evt.Visible;
            PromptKey = evt.PromptKey ?? string.Empty;
            PromptProgress = evt.Progress;
        }

        private void OnPossessionChanged(PossessionChangedEvt evt)
        {
            if (evt.Player.Value < MaxPlayers)
                _phases[evt.Player.Value] = evt.State;
        }

        private void OnColdBreath(ColdBreathChangedEvt evt)
        {
            ColdBreath = evt.Active;
        }

        private void OnScoreChanged(ScoreChangedEvt evt)
        {
            if (evt.Reason == ScoreReason.Reset || evt.Player.Value >= MaxPlayers)
                return;
            _roundDelta[evt.Player.Value] += evt.Delta;
        }

        private void OnMatchEnded(MatchEndedEvt evt)
        {
            MatchEnded = true;
            Winner = evt.Winner;
            TopScore = evt.TopScore;
        }
    }
}
