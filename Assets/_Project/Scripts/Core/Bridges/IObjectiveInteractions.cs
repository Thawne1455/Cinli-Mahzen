namespace CinliMahzen.Core
{
    /// <summary>
    /// What the house's interactive markers do in the round (TODO 3.6), for CM.World, which cannot see CM.Objectives. Registered by the scene's
    /// round setup (<c>GameServices.Register</c>); there is no stub, so markers look it up with <c>GameServices.TryGet</c>. Authority only.
    /// </summary>
    public interface IObjectiveInteractions
    {
        /// <summary>The human searched the key hiding place <paramref name="spot"/> (KeyHideSpot NetId).</summary>
        void SearchSpotAuthority(PlayerId who, NetId spot);

        /// <summary>The human used the locked gate <paramref name="gate"/> (StageGate NetId with a GateLock).</summary>
        void UseGateAuthority(PlayerId who, NetId gate);

        /// <summary>The human took the tool shed item at <paramref name="point"/> (ToolShedItemPoint NetId).</summary>
        void CollectToolShedItemAuthority(PlayerId who, NetId point);

        /// <summary>Local prediction for prompts: the item at <paramref name="point"/> is still there.</summary>
        bool IsToolShedItemAvailable(NetId point);

        /// <summary>Local prediction for prompts: the gate is still closed.</summary>
        bool IsGateClosed(NetId gate);
    }
}
