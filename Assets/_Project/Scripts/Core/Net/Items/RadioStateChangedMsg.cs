namespace CinliMahzen.Core.Net
{
    /// <summary>
    /// <see cref="MsgCode.RadioStateChanged"/> (Teknik §6.3, TODO 1.8). Authority → everyone: an Old Radio plays in <c>room</c> until
    /// <c>until</c> (Net.Time): sabotage creaks and telegraph sounds are muted there (IRoomService.IsNoiseSuppressed). World applies it.
    /// Payload: { int room, double until }.
    /// </summary>
    public static class RadioStateChangedMsg
    {
        public static NetMsg Encode(int room, double until)
        {
            return new NetMsg { Code = MsgCode.RadioStateChanged, Payload = new object[] { room, until } };
        }

        public static bool TryDecode(NetMsg msg, out int room, out double until)
        {
            room = -1;
            until = 0d;
            object[] p = msg.Payload;
            if (msg.Code != MsgCode.RadioStateChanged || p == null || p.Length != 2 || !(p[0] is int r) || r < 0 || !(p[1] is double u))
                return false;
            room = r;
            until = u;
            return true;
        }
    }
}
