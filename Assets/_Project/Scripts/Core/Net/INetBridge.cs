using System;

namespace CinliMahzen.Core.Net
{
    /// <summary>
    /// Network abstraction (Teknik §4.3). Every state change: Request → authority validates → Broadcast → everyone applies.
    /// Offline: OfflineNetBridge (same frame). M4: PunNetBridge.
    /// </summary>
    public interface INetBridge
    {
        bool IsOnline { get; }
        /// <summary>Offline: true. Online: PhotonNetwork.IsMasterClient.</summary>
        bool IsAuthority { get; }
        PlayerId LocalPlayer { get; }
        /// <summary>Offline: Time.timeAsDouble. Online: PhotonNetwork.Time.</summary>
        double Time { get; }

        void SendToAuthority(NetMsg msg);
        /// <summary>Authority only; otherwise logs an error.</summary>
        void Broadcast(NetMsg msg);
        void Register(MsgCode code, Action<NetMsg> handler);
        void Unregister(MsgCode code, Action<NetMsg> handler);
    }
}
