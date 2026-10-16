namespace CinliMahzen.Core.Events
{
    /// <summary>Published by Player: the local interactor's current prompt (target key, hold progress 0..1). Only on change.</summary>
    public readonly struct InteractPromptEvt
    {
        public readonly string PromptKey;
        public readonly float Progress;
        public readonly bool Visible;

        public InteractPromptEvt(string promptKey, float progress, bool visible)
        {
            PromptKey = promptKey;
            Progress = progress;
            Visible = visible;
        }
    }
}
