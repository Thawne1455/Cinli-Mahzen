using CinliMahzen.Core;
using UnityEngine;

namespace CinliMahzen.World
{
    /// <summary>A candidate hiding place for the house key in the garden (Teknik 8, Oyun 4.4): pot, bucket, well rim, woodpile. The seed picks one per round.</summary>
    public sealed class KeyHideSpot : NetHouseMarker, IInteractable
    {
        // TODO 3.6: the human searches it (hold SearchHoldTime); the round decides whether the key is here.
        public string PromptKey => "interact.search";

        public float HoldTime
        {
            get
            {
                GameBalanceConfig config = GameServices.Config;
                return config != null ? config.SearchHoldTime : 1f;
            }
        }

        public float NoiseOnInteract => 0.3f;

        public bool CanInteract(PlayerId who, Role role)
        {
            return role == Role.Human && GameServices.TryGet(out IObjectiveInteractions _);
        }

        public bool ValidateAuthority(PlayerId who)
        {
            return GameServices.TryGet(out IObjectiveInteractions _);
        }

        public void ExecuteAuthority(PlayerId who)
        {
            Debug.Assert(GameServices.Net.IsAuthority);
            if (GameServices.TryGet(out IObjectiveInteractions objectives))
                objectives.SearchSpotAuthority(who, NetId);
        }

        protected override Color GizmoColor
        {
            get { return new Color(1f, 0.6f, 0.2f); }
        }
    }
}
