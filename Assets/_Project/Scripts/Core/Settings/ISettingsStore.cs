namespace CinliMahzen.Core
{
    /// <summary>Key/value storage of <see cref="GameSettings"/> (PlayerPrefs at runtime, a dictionary in tests).</summary>
    public interface ISettingsStore
    {
        bool HasKey(string key);
        float GetFloat(string key, float fallback);
        int GetInt(string key, int fallback);
        void SetFloat(string key, float value);
        void SetInt(string key, int value);
        void Save();
    }
}
