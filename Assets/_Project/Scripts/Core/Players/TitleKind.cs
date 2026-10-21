namespace CinliMahzen.Core
{
    /// <summary>Match-end titles of Oyun §10 (TODO 4.3); each is earned by the most of one <see cref="StatKind"/>. Append only.</summary>
    public enum TitleKind : byte
    {
        Electrician = 0,   // Elektrikçi — WrongConfirm
        Saboteur = 1,      // Bozguncu — Scramble
        BulbKiller = 2,    // Ampul Katili — LampBurst
        Navigator = 3,     // Navigasyon — Ping
        Kicker = 4,        // Tekmeci Dayı — MissedKick
        SlipperSniper = 5, // Terlik Nişancısı — SlipperHit
        Cologne = 6,       // Kolonyacı — Rescue
    }
}
