namespace CinliMahzen.Core
{
    /// <summary>
    /// Debug entry points of the objectives (TODO 2.7) for CM.DebugTools, which cannot see CM.Objectives. Registered with
    /// <c>GameServices.Register</c> by the scene's round setup; there is no stub, so look it up with <c>GameServices.TryGet</c>.
    /// </summary>
    public interface IObjectiveDebugCommands
    {
        /// <summary>Authority only (F11): opens the gate of the current stage as if its puzzles / key were done. False when nothing is left to open.</summary>
        bool CompleteCurrentStageAuthority();
    }
}
