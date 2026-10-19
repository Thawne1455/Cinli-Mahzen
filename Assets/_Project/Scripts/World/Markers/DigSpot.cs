using CinliMahzen.Core;
using UnityEngine;

namespace CinliMahzen.World
{
    /// <summary>A candidate dig site in the cellar (Teknik 8, Oyun 4.5); the seed makes one of them the real treasure. The cellar arrives with TODO 3.7, so graybox v0 has none.</summary>
    public sealed class DigSpot : NetHouseMarker, IInteractable
    {
        // TODO 3.7: the human digs with the shovel, one layer per hold (DigTotalTime / DigCheckpoints); a finished layer is kept.
        public string PromptKey => "interact.dig";

        public float HoldTime => GameServices.TryGet(out IObjectiveInteractions objectives) ? objectives.DigLayerTime : 6f;

        public float NoiseOnInteract => 1f;

        public bool CanInteract(PlayerId who, Role role)
        {
            return role == Role.Human && GameServices.TryGet(out IObjectiveInteractions objectives) && objectives.CanDigAt(who, NetId);
        }

        public bool ValidateAuthority(PlayerId who)
        {
            return GameServices.TryGet(out IObjectiveInteractions _);
        }

        public void ExecuteAuthority(PlayerId who)
        {
            Debug.Assert(GameServices.Net.IsAuthority);
            if (GameServices.TryGet(out IObjectiveInteractions objectives))
                objectives.DigAuthority(who, NetId);
        }

        protected override Color GizmoColor
        {
            get { return new Color(0.6f, 0.4f, 0.2f); }
        }
    }
}
