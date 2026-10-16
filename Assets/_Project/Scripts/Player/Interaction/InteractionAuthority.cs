using System;
using CinliMahzen.Core;
using CinliMahzen.Core.Net;
using UnityEngine;

namespace CinliMahzen.Player
{
    /// <summary>
    /// Authority side of hold-to-interact (Teknik §4.5). ReqInteract → range check (InteractRange + NetRangeTolerance) →
    /// ValidateAuthority → ExecuteAuthority → InteractResult broadcast (ok true or false). Requests only reach the authority;
    /// a non-authority client ignores them.
    /// </summary>
    public sealed class InteractionAuthority
    {
        private readonly INetBridge _net;
        private readonly IEntityRegistry _entities;
        private readonly GameBalanceConfig _config;
        private readonly Func<PlayerId, Vector3?> _pawnPosition;

        /// <param name="pawnPosition">Position of a player's pawn. Defaults to a scene search by owner, run only when a request arrives.</param>
        public InteractionAuthority(INetBridge net, IEntityRegistry entities, GameBalanceConfig config, Func<PlayerId, Vector3?> pawnPosition = null)
        {
            _net = net;
            _entities = entities;
            _config = config;
            _pawnPosition = pawnPosition ?? new Func<PlayerId, Vector3?>(FindPawnPosition);
        }

        public void Attach()
        {
            _net.Register(MsgCode.ReqInteract, OnReqInteract);
        }

        public void Detach()
        {
            _net.Unregister(MsgCode.ReqInteract, OnReqInteract);
        }

        private void OnReqInteract(NetMsg msg)
        {
            if (!_net.IsAuthority)
                return;
            if (!ReqInteractMsg.TryDecode(msg, out NetId target))
            {
                CMLog.Warn("Interact", "Malformed ReqInteract ignored");
                return;
            }
            ProcessRequestAuthority(msg.Sender, target);
        }

        /// <summary>Authority only. Broadcasts InteractResult exactly once, ok or not. Returns ok.</summary>
        public bool ProcessRequestAuthority(PlayerId who, NetId target)
        {
            Debug.Assert(_net.IsAuthority, "ProcessRequestAuthority must only run on the authority");

            bool ok = ResolveRequest(who, target);
            if (!ok)
                CMLog.Info("Interact", "Rejected " + target + " from " + who);
            _net.Broadcast(InteractResultMsg.Encode(target, who, ok));
            return ok;
        }

        private bool ResolveRequest(PlayerId who, NetId target)
        {
            if (!target.IsValid || _entities == null || _config == null)
                return false;
            if (!_entities.TryGet<IInteractable>(target, out IInteractable interactable))
                return false;
            if (!(interactable is Component component))
                return false;

            Vector3? pawn = _pawnPosition(who);
            if (!pawn.HasValue)
                return false;
            if (!InteractValidation.IsInRange(pawn.Value, component.transform.position, _config.InteractRange, _config.NetRangeTolerance))
                return false;
            if (!interactable.ValidateAuthority(who))
                return false;

            interactable.ExecuteAuthority(who);
            return true;
        }

        /// <summary>Scene search over PawnBase by owner. Runs on a request only, never per frame.</summary>
        private static Vector3? FindPawnPosition(PlayerId who)
        {
            PawnBase[] pawns = UnityEngine.Object.FindObjectsByType<PawnBase>(FindObjectsSortMode.None);
            for (int i = 0; i < pawns.Length; i++)
            {
                if (pawns[i].Owner == who)
                    return pawns[i].transform.position;
            }
            return null;
        }
    }
}
