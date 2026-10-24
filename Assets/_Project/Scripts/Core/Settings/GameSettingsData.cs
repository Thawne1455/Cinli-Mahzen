namespace CinliMahzen.Core
{
    /// <summary>
    /// TODO 4.2: the player's own settings (not balance values, so they are not in GameBalanceConfig): look sensitivity, inverted look,
    /// master volume, language and fullscreen. A plain value; <see cref="Clamped"/> keeps every field inside its range.
    /// </summary>
    public struct GameSettingsData
    {
        public const float MinSensitivity = 0.25f;
        public const float MaxSensitivity = 3f;
        public const float DefaultSensitivity = 1f;
        public const float DefaultVolume = 0.8f;

        /// <summary>Multiplies the raw look input of every controller (human, spirit, possessed).</summary>
        public float LookSensitivity;

        public bool InvertY;

        /// <summary>0..1, the AudioListener volume.</summary>
        public float MasterVolume;

        public LocLanguage Language;

        public bool Fullscreen;

        public static GameSettingsData Defaults
        {
            get
            {
                return new GameSettingsData
                {
                    LookSensitivity = DefaultSensitivity,
                    InvertY = false,
                    MasterVolume = DefaultVolume,
                    Language = LocLanguage.Tr,
                    Fullscreen = true,
                };
            }
        }

        /// <summary>A copy with every field in range (NaN becomes the default).</summary>
        public GameSettingsData Clamped()
        {
            GameSettingsData c = this;
            c.LookSensitivity = float.IsNaN(LookSensitivity) ? DefaultSensitivity : Clamp(LookSensitivity, MinSensitivity, MaxSensitivity);
            c.MasterVolume = float.IsNaN(MasterVolume) ? DefaultVolume : Clamp(MasterVolume, 0f, 1f);
            if (c.Language != LocLanguage.Tr && c.Language != LocLanguage.En)
                c.Language = LocLanguage.Tr;
            return c;
        }

        private static float Clamp(float v, float min, float max)
        {
            return v < min ? min : (v > max ? max : v);
        }
    }
}
