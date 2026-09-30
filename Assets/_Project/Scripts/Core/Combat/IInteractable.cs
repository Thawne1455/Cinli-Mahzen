namespace CinliMahzen.Core
{
    /// <summary>
    /// Hold-to-interact target (Teknik §4.5). Flow: Interactor → ReqInteract(netId) → authority
    /// ValidateAuthority + range check (InteractRange + NetRangeTolerance) → ExecuteAuthority → owner broadcasts result.
    /// </summary>
    public interface IInteractable
    {
        NetId NetId { get; }
        /// <summary>Loc key, e.g. "interact.search".</summary>
        string PromptKey { get; }
        /// <summary>0 = instant.</summary>
        float HoldTime { get; }
        /// <summary>Local prediction for UI.</summary>
        bool CanInteract(PlayerId who, Role role);
        /// <summary>Re-checked on the authority.</summary>
        bool ValidateAuthority(PlayerId who);
        /// <summary>Runs on the authority; the owner broadcasts the result.</summary>
        void ExecuteAuthority(PlayerId who);
        /// <summary>0..1, for the noise system.</summary>
        float NoiseOnInteract { get; }
    }
}
