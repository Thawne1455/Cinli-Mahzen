namespace CinliMahzen.Core.Net
{
    /// <summary>
    /// <see cref="MsgCode.CologneReviveTriggered"/> (Teknik §5.3, TODO 1.8). Authority → everyone: the good jinn <c>by</c> broke the Lemon Cologne
    /// over the fainted human within <c>CologneReviveRange</c>. The authority's HumanHealth answers with ReviveWithCologneAuthority
    /// (HumanRevived{byCologne}). Payload: { byte by }.
    /// </summary>
    public static class CologneReviveTriggeredMsg
    {
        public static NetMsg Encode(PlayerId by)
        {
            return new NetMsg { Code = MsgCode.CologneReviveTriggered, Payload = new object[] { by.Value } };
        }

        public static bool TryDecode(NetMsg msg, out PlayerId by)
        {
            by = PlayerId.None;
            object[] p = msg.Payload;
            if (msg.Code != MsgCode.CologneReviveTriggered || p == null || p.Length != 1 || !(p[0] is byte who) || !new PlayerId(who).IsValid)
                return false;
            by = new PlayerId(who);
            return true;
        }
    }
}
