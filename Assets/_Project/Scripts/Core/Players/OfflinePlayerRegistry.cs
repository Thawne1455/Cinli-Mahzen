using System;
using System.Collections.Generic;
using CinliMahzen.Core.Events;

namespace CinliMahzen.Core
{
    /// <summary>
    /// Offline / hotseat player registry (Teknik §4.2, TODO 1.3). Four fixed slots P0..P3 (nicknames "P1".."P4") whose roles
    /// are assigned from outside: at boot by DebugTools' HotseatBootstrap (round 0 of the rotation), later by the MatchController
    /// (TODO 1.10 integration) once a round starts. The local player is the one currently being played (F1-F4 in hotseat);
    /// <see cref="Net.OfflineNetBridge"/> reads <see cref="LocalPlayer"/> from here, so <c>PawnBase.IsLocallyControlled</c> follows it.
    /// Plain C#, no Unity objects: the only side effects are the two events and <see cref="LocalRoleChangedEvt"/> on the EventBus.
    /// <see cref="SetLocalPlayer"/> and <see cref="SetRoles"/> are deliberately not on <see cref="IPlayerRegistry"/>: the first is hotseat-only,
    /// the second is decided together with the online registry (1.10 / M5).
    /// </summary>
    public sealed class OfflinePlayerRegistry : IPlayerRegistry
    {
        public const int DefaultPlayerCount = 4;

        private readonly PlayerInfo[] _players;
        private PlayerId _local = new PlayerId(0);

        public OfflinePlayerRegistry(int playerCount = DefaultPlayerCount)
        {
            if (playerCount < 1 || playerCount >= PlayerId.None.Value)
                throw new ArgumentOutOfRangeException(nameof(playerCount), playerCount, "Player count must be 1..254");

            _players = new PlayerInfo[playerCount];
            for (int i = 0; i < playerCount; i++)
            {
                _players[i] = new PlayerInfo
                {
                    Id = new PlayerId((byte)i),
                    Nickname = "P" + (i + 1),
                    Role = Role.None,
                    Score = 0,
                };
            }
        }

        public IReadOnlyList<PlayerInfo> Players => _players;

        public PlayerId LocalPlayer => _local;

        public event Action<PlayerId> LocalPlayerChanged;

        public event Action RolesChanged;

        /// <summary>The role of <paramref name="id"/>, or <see cref="Role.None"/> for an unknown player.</summary>
        public Role GetRole(PlayerId id)
        {
            return IsKnown(id) ? _players[id.Value].Role : Role.None;
        }

        /// <summary>The record of <paramref name="id"/>, or null for an unknown player.</summary>
        public PlayerInfo Get(PlayerId id)
        {
            return IsKnown(id) ? _players[id.Value] : null;
        }

        /// <summary>
        /// Makes <paramref name="id"/> the local (played) player. Raises <see cref="LocalPlayerChanged"/> and, when the local role
        /// differs from the previous one, publishes <see cref="LocalRoleChangedEvt"/> (camera culling, input map). Selecting the
        /// current player again raises nothing. Returns false (and changes nothing) for an unknown player.
        /// </summary>
        public bool SetLocalPlayer(PlayerId id)
        {
            if (!IsKnown(id))
                return false;
            if (id == _local)
                return true;

            Role before = GetRole(_local);
            _local = id;
            RaiseLocalPlayerChanged(id);

            Role after = GetRole(id);
            if (after != before)
                EventBus.Publish(new LocalRoleChangedEvt(after));
            return true;
        }

        /// <summary>
        /// Assigns every player's role (index = player slot). Always raises <see cref="RolesChanged"/> (a new round re-spawns the pawns even
        /// when the roles happen to repeat) and publishes <see cref="LocalRoleChangedEvt"/> only when the local player's role changed.
        /// Returns false (and changes nothing) when the array is null, has the wrong length or holds an undefined role.
        /// </summary>
        public bool SetRoles(Role[] byPlayerIndex)
        {
            if (byPlayerIndex == null || byPlayerIndex.Length != _players.Length)
                return false;
            for (int i = 0; i < byPlayerIndex.Length; i++)
            {
                if (byPlayerIndex[i] > Role.EvilJinn)
                    return false;
            }

            Role before = GetRole(_local);
            for (int i = 0; i < byPlayerIndex.Length; i++)
                _players[i].Role = byPlayerIndex[i];
            RaiseRolesChanged();

            Role after = GetRole(_local);
            if (after != before)
                EventBus.Publish(new LocalRoleChangedEvt(after));
            return true;
        }

        private bool IsKnown(PlayerId id)
        {
            return id.IsValid && id.Value < _players.Length;
        }

        // One failing subscriber must not stop the others (a half-switched hotseat is worse than a logged exception).
        private void RaiseLocalPlayerChanged(PlayerId id)
        {
            Action<PlayerId> handlers = LocalPlayerChanged;
            if (handlers == null)
                return;
            foreach (Delegate d in handlers.GetInvocationList())
            {
                try
                {
                    ((Action<PlayerId>)d)(id);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                }
            }
        }

        private void RaiseRolesChanged()
        {
            Action handlers = RolesChanged;
            if (handlers == null)
                return;
            foreach (Delegate d in handlers.GetInvocationList())
            {
                try
                {
                    ((Action)d)();
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                }
            }
        }
    }
}
