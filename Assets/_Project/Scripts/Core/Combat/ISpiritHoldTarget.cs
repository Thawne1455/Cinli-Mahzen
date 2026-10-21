namespace CinliMahzen.Core
{
    /// <summary>
    /// Something a jinn in spirit form holds E on (TODO 2.6): the evil jinn's mass sabotage of a puzzle, later the good jinn's lamp repair
    /// (TODO 3.5). CM.Jinn's <c>SpiritInteractor</c> finds it with a raycast on the Interactable layer (<c>GetComponentInParent</c>), counts the
    /// hold and, when it completes, calls <see cref="SendRequest"/> — the target sends its own request message to the authority, whose rules
    /// decide. Lives in Core because CM.Jinn cannot see the modules that implement it (CM.Objectives, CM.World).
    /// </summary>
    public interface ISpiritHoldTarget
    {
        /// <summary>Loc key of the prompt, e.g. "interact.puzzle_scramble".</summary>
        string SpiritPromptKey { get; }

        /// <summary>Seconds E must be held (e.g. <c>ScrambleHoldTime</c>).</summary>
        float SpiritHoldTime { get; }

        /// <summary>How far (m) the jinn's eyes may be (e.g. <c>ScrambleRange</c>).</summary>
        float SpiritRange { get; }

        /// <summary>Local prediction for the prompt (the authority re-checks everything).</summary>
        bool CanSpiritInteract(PlayerId who, Role role);

        /// <summary>The hold completed on this machine: send the request to the authority.</summary>
        void SendRequest(PlayerId who);
    }
}
