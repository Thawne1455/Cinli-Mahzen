using CinliMahzen.Core;
using UnityEngine;

namespace CinliMahzen.UI
{
    /// <summary>
    /// TODO 4.2: the settings page, drawn with IMGUI inside a menu (placeholder art like the HUD). Every change goes straight to
    /// <see cref="GameSettings.Set"/> (stored and applied at once). Returns true when "Back" was pressed.
    /// </summary>
    public static class SettingsPanel
    {
        public static bool Draw(Rect area, GameSettings settings, float rowHeight)
        {
            GUILayout.BeginArea(area);
            GUILayout.Label(Loc.T("settings.title"));
            GUILayout.Space(rowHeight * 0.3f);
            if (settings == null)
            {
                GUILayout.Label(Loc.T("settings.unavailable"));
            }
            else
            {
                GameSettingsData d = settings.Current;

                GUILayout.Label(Loc.T("settings.sensitivity", d.LookSensitivity.ToString("0.00")));
                d.LookSensitivity = GUILayout.HorizontalSlider(d.LookSensitivity, GameSettingsData.MinSensitivity, GameSettingsData.MaxSensitivity);
                GUILayout.Space(rowHeight * 0.3f);

                d.InvertY = GUILayout.Toggle(d.InvertY, Loc.T("settings.invert_y"));
                GUILayout.Space(rowHeight * 0.3f);

                GUILayout.Label(Loc.T("settings.volume", Mathf.RoundToInt(d.MasterVolume * 100f)));
                d.MasterVolume = GUILayout.HorizontalSlider(d.MasterVolume, 0f, 1f);
                GUILayout.Space(rowHeight * 0.3f);

                GUILayout.BeginHorizontal();
                GUILayout.Label(Loc.T("settings.language"));
                if (GUILayout.Toggle(d.Language == LocLanguage.Tr, Loc.T("settings.lang_tr"), GUI.skin.button))
                    d.Language = LocLanguage.Tr;
                if (GUILayout.Toggle(d.Language == LocLanguage.En, Loc.T("settings.lang_en"), GUI.skin.button))
                    d.Language = LocLanguage.En;
                GUILayout.EndHorizontal();
                GUILayout.Space(rowHeight * 0.3f);

                d.Fullscreen = GUILayout.Toggle(d.Fullscreen, Loc.T("settings.fullscreen"));
                GUILayout.Space(rowHeight * 0.3f);

                if (GUILayout.Button(Loc.T("settings.reset"), GUILayout.Height(rowHeight)))
                    d = GameSettingsData.Defaults;

                settings.Set(d);
            }

            GUILayout.Space(rowHeight * 0.5f);
            bool back = GUILayout.Button(Loc.T("menu.back"), GUILayout.Height(rowHeight));
            GUILayout.EndArea();
            return back;
        }
    }
}
