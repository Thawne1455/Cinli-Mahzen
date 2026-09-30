using System;
using System.Collections.Generic;

namespace CinliMahzen.Core
{
    /// <summary>Players and their roles (Teknik §4.2). Implemented by A (PlayerRegistry, A1.5).</summary>
    public interface IPlayerRegistry
    {
        IReadOnlyList<PlayerInfo> Players { get; }
        /// <summary>Changes with F1-F4 in hotseat.</summary>
        PlayerId LocalPlayer { get; }
        Role GetRole(PlayerId id);
        PlayerInfo Get(PlayerId id);
        event Action<PlayerId> LocalPlayerChanged;
        event Action RolesChanged;
    }
}
