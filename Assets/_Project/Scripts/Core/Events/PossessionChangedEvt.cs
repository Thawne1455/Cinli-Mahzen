// Owner: B (drafted by A in A0.5)
namespace CinliMahzen.Core.Events
{
    /// <summary>Published by Possession when an object's possession state changes.</summary>
    public readonly struct PossessionChangedEvt
    {
        public readonly PlayerId Player;
        public readonly NetId Obj;
        public readonly PossessionPhase State;

        public PossessionChangedEvt(PlayerId player, NetId obj, PossessionPhase state)
        {
            Player = player;
            Obj = obj;
            State = state;
        }
    }
}
