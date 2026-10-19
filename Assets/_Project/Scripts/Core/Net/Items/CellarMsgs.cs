namespace CinliMahzen.Core.Net
{
    /// <summary>
    /// Cellar finale codecs (Oyun §4.5, TODO 3.7). All authority → everyone.
    /// <list type="bullet">
    /// <item><see cref="MsgCode.RageStarted"/> { bool active }: Öfke — evil jinns regenerate ×RageRegenMult, cooldowns ×RageCooldownMult.</item>
    /// <item><see cref="MsgCode.DigCheckpointReached"/> { int spot, int layer }: a 6 s dig layer is done (1..DigCheckpoints) and kept.</item>
    /// <item><see cref="MsgCode.TreasureDug"/> { byte player }: the last layer is done — the seekers win (TreasureDugEvt).</item>
    /// </list>
    /// </summary>
    public static class CellarMsgs
    {
        public static NetMsg EncodeRage(bool active)
        {
            return new NetMsg { Code = MsgCode.RageStarted, Payload = new object[] { active } };
        }

        public static bool TryDecodeRage(NetMsg msg, out bool active)
        {
            active = false;
            object[] p = msg.Payload;
            if (msg.Code != MsgCode.RageStarted || p == null || p.Length != 1 || !(p[0] is bool a))
                return false;
            active = a;
            return true;
        }

        public static NetMsg EncodeCheckpoint(NetId spot, int layer)
        {
            return new NetMsg { Code = MsgCode.DigCheckpointReached, Payload = new object[] { spot.Value, layer } };
        }

        public static bool TryDecodeCheckpoint(NetMsg msg, out NetId spot, out int layer)
        {
            spot = NetId.Invalid;
            layer = 0;
            object[] p = msg.Payload;
            if (msg.Code != MsgCode.DigCheckpointReached || p == null || p.Length != 2 || !(p[0] is int id) || id == 0 || !(p[1] is int l) || l <= 0)
                return false;
            spot = new NetId(id);
            layer = l;
            return true;
        }

        public static NetMsg EncodeTreasure(PlayerId digger)
        {
            return new NetMsg { Code = MsgCode.TreasureDug, Payload = new object[] { digger.Value } };
        }

        public static bool TryDecodeTreasure(NetMsg msg, out PlayerId digger)
        {
            digger = PlayerId.None;
            object[] p = msg.Payload;
            if (msg.Code != MsgCode.TreasureDug || p == null || p.Length != 1 || !(p[0] is byte who) || !new PlayerId(who).IsValid)
                return false;
            digger = new PlayerId(who);
            return true;
        }
    }
}
