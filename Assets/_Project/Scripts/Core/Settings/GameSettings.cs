using CinliMahzen.Core.Events;
using UnityEngine;

namespace CinliMahzen.Core
{
    /// <summary>
    /// TODO 4.2: the settings service (registered by <see cref="GameBootstrap"/>). Loads from an <see cref="ISettingsStore"/>, clamps, applies
    /// what the engine owns (volume, language, fullscreen) and saves on every change; the look sensitivity and inverted look are read by
    /// the local input source. Publishes <see cref="SettingsChangedEvt"/>.
    /// </summary>
    public sealed class GameSettings
    {
        public const string KeySensitivity = "cm.settings.sensitivity";
        public const string KeyInvertY = "cm.settings.invert_y";
        public const string KeyVolume = "cm.settings.volume";
        public const string KeyLanguage = "cm.settings.language";
        public const string KeyFullscreen = "cm.settings.fullscreen";

        private readonly ISettingsStore _store;

        public GameSettingsData Current { get; private set; }

        public GameSettings(ISettingsStore store)
        {
            _store = store;
            Current = Load(store);
        }

        /// <summary>Reads the stored settings (missing keys take the defaults), clamped.</summary>
        public static GameSettingsData Load(ISettingsStore store)
        {
            GameSettingsData d = GameSettingsData.Defaults;
            if (store == null)
                return d;
            d.LookSensitivity = store.GetFloat(KeySensitivity, d.LookSensitivity);
            d.InvertY = store.GetInt(KeyInvertY, d.InvertY ? 1 : 0) != 0;
            d.MasterVolume = store.GetFloat(KeyVolume, d.MasterVolume);
            d.Language = (LocLanguage)store.GetInt(KeyLanguage, (int)d.Language);
            d.Fullscreen = store.GetInt(KeyFullscreen, d.Fullscreen ? 1 : 0) != 0;
            return d.Clamped();
        }

        /// <summary>Clamps, stores, applies and announces <paramref name="data"/>. Does nothing when it equals the current settings.</summary>
        public void Set(GameSettingsData data)
        {
            data = data.Clamped();
            if (data.Equals(Current))
                return;
            Current = data;
            if (_store != null)
            {
                _store.SetFloat(KeySensitivity, data.LookSensitivity);
                _store.SetInt(KeyInvertY, data.InvertY ? 1 : 0);
                _store.SetFloat(KeyVolume, data.MasterVolume);
                _store.SetInt(KeyLanguage, (int)data.Language);
                _store.SetInt(KeyFullscreen, data.Fullscreen ? 1 : 0);
                _store.Save();
            }
            Apply();
            EventBus.Publish(new SettingsChangedEvt());
        }

        /// <summary>Pushes the engine-side values: listener volume, Loc language and (in a player build) fullscreen.</summary>
        public void Apply()
        {
            AudioListener.volume = Current.MasterVolume;
            Loc.Language = Current.Language;
            if (!Application.isEditor)
                Screen.fullScreen = Current.Fullscreen;
        }

        /// <summary>The raw look delta scaled by the sensitivity, with Y flipped when inverted.</summary>
        public Vector2 ScaleLook(Vector2 raw)
        {
            GameSettingsData d = Current;
            return new Vector2(raw.x * d.LookSensitivity, (d.InvertY ? -raw.y : raw.y) * d.LookSensitivity);
        }
    }
}
