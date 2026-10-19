using System;
using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.Core.Events;
using CinliMahzen.Core.Net;

namespace CinliMahzen.Match
{
    /// <summary>
    /// Match statistics for the round report and the titles (Oyun §10, TODO 4.3), plain C#, the scene's <see cref="IMatchStats"/>.
    /// Every input is something <b>every machine</b> receives, so every machine counts the same without a stats message:
    /// <list type="bullet">
    /// <item>WrongConfirm — <see cref="HumanDamagedEvt"/> with cause <see cref="ConfirmFailCauseId"/>, to that round's human;</item>
    /// <item>SlipperHit — <see cref="HumanDamagedEvt"/> with cause <see cref="SlipperCauseId"/>, to the attacker;</item>
    /// <item>Scramble — <see cref="PuzzleScrambledEvt"/>; LampBurst — <see cref="ActionResolvedEvt"/> of action <see cref="BurstActionId"/>;
    /// Ping — <see cref="PingPlacedEvt"/>;</item>
    /// <item>MissedKick — <c>KickResult</c> with no object; Rescue — <c>CologneReviveTriggered</c> and <see cref="SpiritBlockedEvt"/>.</item>
    /// </list>
    /// Round counts reset on every <see cref="RoundStartedEvt"/>, match counts and titles on the first round's. <see cref="ComputeTitles"/>
    /// runs at MatchEnd. The ids must match the data: PuzzleSystemCore.ConfirmFailCauseId and PD_Slipper / PD_Lamp (checked by tests).
    /// </summary>
    public sealed class StatsService : IMatchStats
    {
        public const string ConfirmFailCauseId = "puzzle.confirm_fail";
        public const string SlipperCauseId = "slipper.homing_slipper";
        public const string BurstActionId = "burst";

        private static readonly int KindCount = (int)StatKind.Rescue + 1;

        private readonly INetBridge _net;
        private readonly IPlayerRegistry _players;
        private readonly int[] _round;
        private readonly int[] _match;
        private readonly int _playerSlots;
        private readonly List<MatchTitle> _titles = new List<MatchTitle>(8);

        private readonly Action<NetMsg> _onKick;
        private readonly Action<NetMsg> _onCologne;
        private readonly Action<HumanDamagedEvt> _onDamaged;
        private readonly Action<PuzzleScrambledEvt> _onScrambled;
        private readonly Action<ActionResolvedEvt> _onAction;
        private readonly Action<PingPlacedEvt> _onPing;
        private readonly Action<SpiritBlockedEvt> _onBlocked;
        private readonly Action<RoundStartedEvt> _onRound;

        public StatsService(INetBridge net, IPlayerRegistry players)
        {
            _net = net ?? throw new ArgumentNullException(nameof(net));
            _players = players ?? throw new ArgumentNullException(nameof(players));
            _playerSlots = Math.Max(1, players.Players.Count);
            _round = new int[_playerSlots * KindCount];
            _match = new int[_playerSlots * KindCount];
            _onKick = OnKickResult;
            _onCologne = OnCologneRevive;
            _onDamaged = OnHumanDamaged;
            _onScrambled = OnScrambled;
            _onAction = OnActionResolved;
            _onPing = OnPing;
            _onBlocked = OnSpiritBlocked;
            _onRound = OnRoundStarted;
        }

        public IReadOnlyList<MatchTitle> Titles => _titles;

        public int RoundCount(PlayerId player, StatKind kind)
        {
            int i = Index(player, kind);
            return i >= 0 ? _round[i] : 0;
        }

        public int MatchCount(PlayerId player, StatKind kind)
        {
            int i = Index(player, kind);
            return i >= 0 ? _match[i] : 0;
        }

        public void Attach()
        {
            _net.Register(MsgCode.KickResult, _onKick);
            _net.Register(MsgCode.CologneReviveTriggered, _onCologne);
            EventBus.Subscribe(_onDamaged);
            EventBus.Subscribe(_onScrambled);
            EventBus.Subscribe(_onAction);
            EventBus.Subscribe(_onPing);
            EventBus.Subscribe(_onBlocked);
            EventBus.Subscribe(_onRound);
        }

        public void Detach()
        {
            _net.Unregister(MsgCode.KickResult, _onKick);
            _net.Unregister(MsgCode.CologneReviveTriggered, _onCologne);
            EventBus.Unsubscribe(_onDamaged);
            EventBus.Unsubscribe(_onScrambled);
            EventBus.Unsubscribe(_onAction);
            EventBus.Unsubscribe(_onPing);
            EventBus.Unsubscribe(_onBlocked);
            EventBus.Unsubscribe(_onRound);
        }

        /// <summary>MatchEnd (every machine): fills <see cref="Titles"/> from the match counts.</summary>
        public void ComputeTitles()
        {
            TitleCalculator.Compute(_players.Players, this, _titles);
        }

        /// <summary>Adds one <paramref name="kind"/> to <paramref name="player"/> (round and match). Ignores unknown players.</summary>
        public void Count(PlayerId player, StatKind kind)
        {
            int i = Index(player, kind);
            if (i < 0)
                return;
            _round[i]++;
            _match[i]++;
        }

        private int Index(PlayerId player, StatKind kind)
        {
            if (!player.IsValid || player.Value >= _playerSlots || (int)kind >= KindCount)
                return -1;
            return player.Value * KindCount + (int)kind;
        }

        private PlayerId CurrentHuman()
        {
            IReadOnlyList<PlayerInfo> players = _players.Players;
            for (int i = 0; i < players.Count; i++)
            {
                if (_players.GetRole(players[i].Id) == Role.Human)
                    return players[i].Id;
            }
            return PlayerId.None;
        }

        private void OnHumanDamaged(HumanDamagedEvt evt)
        {
            string cause = evt.Info.CauseId;
            if (cause == ConfirmFailCauseId)
                Count(CurrentHuman(), StatKind.WrongConfirm);
            else if (cause == SlipperCauseId)
                Count(evt.Info.Attacker, StatKind.SlipperHit);
        }

        private void OnScrambled(PuzzleScrambledEvt evt)
        {
            Count(evt.ByJinn, StatKind.Scramble);
        }

        private void OnActionResolved(ActionResolvedEvt evt)
        {
            if (evt.ActionId == BurstActionId)
                Count(evt.Possessor, StatKind.LampBurst);
        }

        private void OnPing(PingPlacedEvt evt)
        {
            Count(evt.Player, StatKind.Ping);
        }

        private void OnKickResult(NetMsg msg)
        {
            if (HumanActionMsgs.TryDecodeKickResult(msg, out PlayerId player, out NetId obj) && !obj.IsValid)
                Count(player, StatKind.MissedKick);
        }

        private void OnCologneRevive(NetMsg msg)
        {
            if (CologneReviveTriggeredMsg.TryDecode(msg, out PlayerId by))
                Count(by, StatKind.Rescue);
        }

        private void OnSpiritBlocked(SpiritBlockedEvt evt)
        {
            Count(evt.GoodJinn, StatKind.Rescue);
        }

        private void OnRoundStarted(RoundStartedEvt evt)
        {
            Array.Clear(_round, 0, _round.Length);
            if (evt.RoundIndex == 0)
            {
                Array.Clear(_match, 0, _match.Length);
                _titles.Clear();
            }
        }
    }
}
