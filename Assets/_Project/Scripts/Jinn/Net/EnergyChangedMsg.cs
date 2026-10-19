using CinliMahzen.Core;
using CinliMahzen.Core.Net;

namespace CinliMahzen.Jinn
{
    /// <summary>
    /// <see cref="MsgCode.EnergyChanged"/> (Teknik §6.5, TODO 1.8). Authority → everyone: <c>player</c>'s jinn energy is <c>value</c>
    /// (after a spend at once, during regen at most 4 Hz). Payload: { byte player, float value }.
    /// </summary>
    public static class EnergyChangedMsg
    {
        public static NetMsg Encode(PlayerId player, float value)
        {
            return new NetMsg { Code = MsgCode.EnergyChanged, Payload = new object[] { player.Value, value } };
        }

        public static bool TryDecode(NetMsg msg, out PlayerId player, out float value)
        {
            player = PlayerId.None;
            value = 0f;
            object[] p = msg.Payload;
            if (msg.Code != MsgCode.EnergyChanged || p == null || p.Length != 2 || !(p[0] is byte who) || !(p[1] is float v))
                return false;
            if (!new PlayerId(who).IsValid || float.IsNaN(v) || v < 0f)
                return false;
            player = new PlayerId(who);
            value = v;
            return true;
        }
    }
}
