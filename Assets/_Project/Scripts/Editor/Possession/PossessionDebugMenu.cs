using CinliMahzen.Core;
using CinliMahzen.Core.Net;
using CinliMahzen.Jinn;
using CinliMahzen.Possession;
using UnityEditor;
using UnityEngine;

namespace CinliMahzen.Editor
{
    /// <summary>
    /// CinliMahzen/Debug/* entries of possession (TODO 1.7) — MCP <c>execute_menu_item</c> equivalents of looking at an object and pressing
    /// E / Space. Play mode only. The requests go through the bridge like real input, so the authority's rules decide (see the
    /// "[Possession]" log and the "possession" section of Dump State).
    /// </summary>
    public static class PossessionDebugMenu
    {
        private const string Root = "CinliMahzen/Debug/";

        [MenuItem(Root + "Possess Nearest (local jinn)")]
        public static void PossessNearest()
        {
            if (!TryGetLocalJinn(out SpiritPawn pawn))
                return;
            var input = pawn.GetComponent<JinnPossessInput>();
            if (input == null)
            {
                Debug.LogError("[Possession] " + pawn.name + " has no JinnPossessInput (rebuild P_Spirit: CinliMahzen/Setup/Build Spirit Prefab)");
                return;
            }
            if (!input.DebugRequestNearest())
                Debug.LogWarning("[Possession] No possessable within PossessRange of " + pawn.name);
        }

        [MenuItem(Root + "Unpossess (local jinn)")]
        public static void Unpossess()
        {
            INetBridge net = GameServices.Net;
            if (net == null)
                return;
            net.SendToAuthority(ReqUnpossessMsg.Encode());
        }

        [MenuItem(Root + "Kick Possessed Objects")]
        public static void KickAll()
        {
            PossessionSystem system = PossessionSystem.Instance;
            if (system == null || system.Authority == null || !GameServices.Net.IsAuthority)
            {
                Debug.LogError("[Possession] No PossessionSystem on the authority");
                return;
            }
            foreach (Possessable p in Possessable.All)
            {
                if (system.Mirror.IsPossessed(p.NetId))
                    system.Authority.KickAuthority(p.NetId, PlayerId.None);
            }
        }

        [MenuItem(Root + "Possess Nearest (local jinn)", true)]
        [MenuItem(Root + "Unpossess (local jinn)", true)]
        [MenuItem(Root + "Kick Possessed Objects", true)]
        private static bool PlayingOnly() => EditorApplication.isPlaying;

        private static bool TryGetLocalJinn(out SpiritPawn pawn)
        {
            pawn = null;
            foreach (SpiritPawn candidate in Object.FindObjectsByType<SpiritPawn>(FindObjectsSortMode.None))
            {
                if (candidate.IsLocalView)
                {
                    pawn = candidate;
                    return true;
                }
            }
            Debug.LogError("[Possession] The local player is not a jinn (F2-F4 / Switch To Player 2..4 first)");
            return false;
        }
    }
}
