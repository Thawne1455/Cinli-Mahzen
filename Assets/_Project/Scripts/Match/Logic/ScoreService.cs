using System;
using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.Core.Events;
using CinliMahzen.Core.Net;
using UnityEngine;

namespace CinliMahzen.Match
{
    /// <summary>
    /// Tiered scoring (Oyun §10, Teknik §9, TODO 3.10), plain C#. Scores are individual: each award goes to every player holding the
    /// rewarded role in that round (seekers = Human + Good Jinn, or the evil jinns), so the role rotation spreads them over the match.
    /// <list type="bullet">
    /// <item><b>Authority</b> listens to the local events every machine sees — <see cref="PuzzleCompletedEvt"/> (+PerPuzzle, seekers),
    /// <see cref="StageCompletedEvt"/> turning the objective phase to Cellar (+BasementOpen once per round, seekers) — and only while the round is Playing. The MatchController calls <see cref="AwardFaintAuthority"/> (before the third faint
    /// ends the round, so that faint still counts) and <see cref="AwardRoundEndAuthority"/> (it alone knows the seconds left). Each award is broadcast as <c>ScoreChanged{player, total, delta, reason}</c>.</item>
    /// <item><b>Every machine</b> applies <c>ScoreChanged</c>: <c>PlayerInfo.Score = total</c> and <see cref="ScoreChangedEvt"/> for the HUD.</item>
    /// </list>
    /// Round counters (puzzles solved, cellar awarded) reset on <see cref="RoundStartedEvt"/>; the totals only through <see cref="ResetMatchAuthority"/>.
    /// </summary>
    public sealed class ScoreService
    {
        private const string Category = "Score";

        private readonly INetBridge _net;
        private readonly IPlayerRegistry _players;
        private readonly Func<IObjectiveInfo> _objectives;
        private readonly Func<MatchState> _matchState;
        private readonly ScoreRules _rules;
        private readonly int[] _totals;
        private readonly HashSet<int> _solvedThisRound = new HashSet<int>();
        private bool _basementAwarded;

        private readonly Action<NetMsg> _onScoreChanged;
        private readonly Action<PuzzleCompletedEvt> _onPuzzle;
        private readonly Action<StageCompletedEvt> _onStage;
        private readonly Action<RoundStartedEvt> _onRound;

        /// <param name="objectives">Current objective info (Phase, PuzzleCount); read when a stage completes or a round ends.</param>
        /// <param name="matchState">The mirror's match state: awards from events count only while Playing.</param>
        public ScoreService(INetBridge net, IPlayerRegistry players, ScoreRules rules, Func<IObjectiveInfo> objectives, Func<MatchState> matchState)
        {
            _net = net ?? throw new ArgumentNullException(nameof(net));
            _players = players ?? throw new ArgumentNullException(nameof(players));
            _objectives = objectives ?? throw new ArgumentNullException(nameof(objectives));
            _matchState = matchState ?? throw new ArgumentNullException(nameof(matchState));
            _rules = rules;
            _totals = new int[Math.Max(1, players.Players.Count)];
            _onScoreChanged = OnScoreChangedMsg;
            _onPuzzle = OnPuzzleCompleted;
            _onStage = OnStageCompleted;
            _onRound = OnRoundStarted;
        }

        public ScoreRules Rules => _rules;

        /// <summary>Puzzles solved this round (distinct slots).</summary>
        public int PuzzlesSolvedThisRound => _solvedThisRound.Count;

        /// <summary>The applied total of <paramref name="player"/> (what every machine was told), 0 for an unknown player.</summary>
        public int ScoreOf(PlayerId player)
        {
            return player.Value < _totals.Length ? _totals[player.Value] : 0;
        }

        public void Attach()
        {
            _net.Register(MsgCode.ScoreChanged, _onScoreChanged);
            EventBus.Subscribe(_onPuzzle);
            EventBus.Subscribe(_onStage);
            EventBus.Subscribe(_onRound);
        }

        public void Detach()
        {
            _net.Unregister(MsgCode.ScoreChanged, _onScoreChanged);
            EventBus.Unsubscribe(_onPuzzle);
            EventBus.Unsubscribe(_onStage);
            EventBus.Unsubscribe(_onRound);
        }

        // ---- Authority ----

        /// <summary>Authority only. The round ended: seekers get TreasureWin + seconds left, or the evil jinns JinnWin + unfinished puzzles × 40.</summary>
        public void AwardRoundEndAuthority(RoundResult result, double secondsLeft)
        {
            Debug.Assert(_net.IsAuthority, "AwardRoundEndAuthority must only run on the authority");
            if (!_net.IsAuthority)
                return;
            switch (result)
            {
                case RoundResult.SeekersWin:
                    AwardRoleAuthority(true, _rules.TreasureAward(secondsLeft), ScoreReason.TreasureWin);
                    break;
                case RoundResult.JinnsWin:
                    int planned = _objectives()?.PuzzleCount ?? 0;
                    AwardRoleAuthority(false, _rules.JinnWinAward(planned - _solvedThisRound.Count), ScoreReason.JinnWin);
                    break;
            }
        }

        /// <summary>Authority only. The human fainted during Playing: +PerFaint to every evil jinn.</summary>
        public void AwardFaintAuthority()
        {
            Debug.Assert(_net.IsAuthority, "AwardFaintAuthority must only run on the authority");
            if (!_net.IsAuthority)
                return;
            AwardRoleAuthority(false, _rules.PerFaint, ScoreReason.Faint);
        }

        /// <summary>Authority only. A new match: every total back to 0 (broadcast, so every machine follows).</summary>
        public void ResetMatchAuthority()
        {
            Debug.Assert(_net.IsAuthority, "ResetMatchAuthority must only run on the authority");
            if (!_net.IsAuthority)
                return;
            IReadOnlyList<PlayerInfo> players = _players.Players;
            for (int i = 0; i < players.Count; i++)
            {
                PlayerId id = players[i].Id;
                int total = ScoreOf(id);
                _net.Broadcast(ScoreChangedMsg.Encode(id, 0, -total, ScoreReason.Reset));
            }
        }

        private void AwardRoleAuthority(bool seekers, int points, ScoreReason reason)
        {
            if (points == 0)
                return;
            IReadOnlyList<PlayerInfo> players = _players.Players;
            for (int i = 0; i < players.Count; i++)
            {
                PlayerId id = players[i].Id;
                Role role = _players.GetRole(id);
                bool rewarded = seekers ? ScoreRules.IsSeeker(role) : role == Role.EvilJinn;
                if (!rewarded)
                    continue;
                _net.Broadcast(ScoreChangedMsg.Encode(id, ScoreOf(id) + points, points, reason));
            }
        }

        // ---- Local events (every machine; the authority awards) ----

        private void OnPuzzleCompleted(PuzzleCompletedEvt evt)
        {
            if (!_solvedThisRound.Add(evt.PuzzleSlot))
                return;
            if (_net.IsAuthority && _matchState() == MatchState.Playing)
                AwardRoleAuthority(true, _rules.PerPuzzle, ScoreReason.Puzzle);
        }

        private void OnStageCompleted(StageCompletedEvt evt)
        {
            if (_basementAwarded)
                return;
            IObjectiveInfo objectives = _objectives();
            if (objectives == null || objectives.Phase != ObjectivePhase.Cellar)
                return;
            _basementAwarded = true;
            if (_net.IsAuthority && _matchState() == MatchState.Playing)
                AwardRoleAuthority(true, _rules.BasementOpen, ScoreReason.BasementOpened);
        }

        private void OnRoundStarted(RoundStartedEvt evt)
        {
            _solvedThisRound.Clear();
            _basementAwarded = false;
        }

        // ---- Every machine ----

        private void OnScoreChangedMsg(NetMsg msg)
        {
            if (!ScoreChangedMsg.TryDecode(msg, out PlayerId player, out int total, out int delta, out ScoreReason reason))
            {
                CMLog.Warn(Category, "Malformed ScoreChanged ignored");
                return;
            }
            if (player.Value >= _totals.Length)
            {
                CMLog.Warn(Category, "ScoreChanged for unknown " + player + " ignored");
                return;
            }
            _totals[player.Value] = total;
            PlayerInfo info = _players.Get(player);
            if (info != null)
                info.Score = total;
            if (reason != ScoreReason.Reset)
                CMLog.Info(Category, player + " " + (delta >= 0 ? "+" : string.Empty) + delta + " (" + reason + ") = " + total);
            EventBus.Publish(new ScoreChangedEvt(player, total, delta, reason));
        }

        /// <summary>The top scorer, <see cref="PlayerId.None"/> on a tie for first (or no players).</summary>
        public PlayerId Leader(out int topScore)
        {
            topScore = int.MinValue;
            PlayerId leader = PlayerId.None;
            bool tie = false;
            IReadOnlyList<PlayerInfo> players = _players.Players;
            for (int i = 0; i < players.Count; i++)
            {
                int score = ScoreOf(players[i].Id);
                if (score > topScore)
                {
                    topScore = score;
                    leader = players[i].Id;
                    tie = false;
                }
                else if (score == topScore)
                {
                    tie = true;
                }
            }
            if (players.Count == 0)
                topScore = 0;
            return tie ? PlayerId.None : leader;
        }
    }
}
