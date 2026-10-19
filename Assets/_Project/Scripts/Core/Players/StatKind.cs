namespace CinliMahzen.Core
{
    /// <summary>Per-player match statistics behind the round report and the match-end titles (Oyun §10, TODO 4.3). Append only.</summary>
    public enum StatKind : byte
    {
        /// <summary>Wrong puzzle confirmations (the human of that round takes the shock).</summary>
        WrongConfirm = 0,
        /// <summary>Mass puzzle sabotages.</summary>
        Scramble = 1,
        /// <summary>Lamps burst from inside.</summary>
        LampBurst = 2,
        /// <summary>Good jinn pings.</summary>
        Ping = 3,
        /// <summary>Kicks that hit nothing possessed.</summary>
        MissedKick = 4,
        /// <summary>Mother's Slipper hits on the human.</summary>
        SlipperHit = 5,
        /// <summary>Cologne revives and Spirit Blocks.</summary>
        Rescue = 6,
    }
}
