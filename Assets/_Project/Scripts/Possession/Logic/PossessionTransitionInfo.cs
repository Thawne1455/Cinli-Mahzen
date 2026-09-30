using CinliMahzen.Core;

namespace CinliMahzen.Possession
{
    /// <summary>One timer-driven transition collected by <see cref="PossessionArbiterCore.Tick"/>.</summary>
    public readonly struct PossessionTransitionInfo
    {
        public readonly NetId Object;
        public readonly PlayerId Player;
        public readonly PossessableTransition Kind;

        public PossessionTransitionInfo(NetId obj, PlayerId player, PossessableTransition kind)
        {
            Object = obj;
            Player = player;
            Kind = kind;
        }
    }
}
