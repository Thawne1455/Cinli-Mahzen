using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.Core.Events;
using CinliMahzen.Player;
using CinliMahzen.UI;
using NUnit.Framework;
using UnityEngine;

namespace CinliMahzen.Tests.EditMode.UI
{
    /// <summary>TODO 4.2: menu navigation (<see cref="MenuModel"/>) and the settings service (<see cref="GameSettings"/>).</summary>
    public class MenuAndSettingsTests
    {
        private sealed class MemoryStore : ISettingsStore
        {
            public readonly Dictionary<string, float> Floats = new Dictionary<string, float>();
            public readonly Dictionary<string, int> Ints = new Dictionary<string, int>();
            public int Saves;

            public bool HasKey(string key) => Floats.ContainsKey(key) || Ints.ContainsKey(key);
            public float GetFloat(string key, float fallback) => Floats.TryGetValue(key, out float v) ? v : fallback;
            public int GetInt(string key, int fallback) => Ints.TryGetValue(key, out int v) ? v : fallback;
            public void SetFloat(string key, float value) => Floats[key] = value;
            public void SetInt(string key, int value) => Ints[key] = value;
            public void Save() => Saves++;
        }

        private LocLanguage _language;
        private float _volume;

        [SetUp]
        public void SetUp()
        {
            EventBus.ClearAll();
            _language = Loc.Language;
            _volume = AudioListener.volume;
        }

        [TearDown]
        public void TearDown()
        {
            Loc.Language = _language;
            AudioListener.volume = _volume;
            EventBus.ClearAll();
        }

        // ---------- menu ----------

        [Test]
        public void PauseMenu_EscOpens_EscInSettingsGoesBack_EscAgainCloses()
        {
            var menu = new MenuModel(true);
            Assert.IsFalse(menu.IsOpen);

            menu.Escape();
            Assert.IsTrue(menu.IsOpen);
            Assert.AreEqual(MenuScreen.Root, menu.Screen);

            menu.ShowSettings();
            menu.Escape();
            Assert.IsTrue(menu.IsOpen, "Esc leaves the settings first");
            Assert.AreEqual(MenuScreen.Root, menu.Screen);

            menu.Escape();
            Assert.IsFalse(menu.IsOpen);
        }

        [Test]
        public void MainMenu_NeverCloses_AndReopensOnItsRootPage()
        {
            var menu = new MenuModel(false);
            Assert.IsTrue(menu.IsOpen);

            menu.Escape();
            Assert.IsTrue(menu.IsOpen);
            menu.ShowSettings();
            menu.Close();
            Assert.IsTrue(menu.IsOpen);
            Assert.AreEqual(MenuScreen.Root, menu.Screen);
        }

        [Test]
        public void Settings_CannotBeShownWhileClosed()
        {
            var menu = new MenuModel(true);
            menu.ShowSettings();
            Assert.AreEqual(MenuScreen.Root, menu.Screen);
        }

        // ---------- settings ----------

        [Test]
        public void Settings_StartFromTheDefaults_WhenNothingIsStored()
        {
            var settings = new GameSettings(new MemoryStore());

            Assert.AreEqual(GameSettingsData.Defaults, settings.Current);
        }

        [Test]
        public void Settings_AreClamped_Stored_Applied_AndAnnounced()
        {
            var store = new MemoryStore();
            var settings = new GameSettings(store);
            int events = 0;
            EventBus.Subscribe<SettingsChangedEvt>(_ => events++);

            GameSettingsData d = settings.Current;
            d.LookSensitivity = 99f;
            d.MasterVolume = -1f;
            d.Language = LocLanguage.En;
            d.InvertY = true;
            settings.Set(d);

            Assert.AreEqual(GameSettingsData.MaxSensitivity, settings.Current.LookSensitivity);
            Assert.AreEqual(0f, settings.Current.MasterVolume);
            Assert.AreEqual(LocLanguage.En, Loc.Language, "applied");
            Assert.AreEqual(0f, AudioListener.volume, 1e-5f);
            Assert.AreEqual(1, events);
            Assert.AreEqual(1, store.Saves);

            var reloaded = new GameSettings(store);
            Assert.AreEqual(settings.Current, reloaded.Current, "a new session reads the same settings");

            settings.Set(settings.Current);
            Assert.AreEqual(1, events, "an unchanged value does not announce or save again");
        }

        [Test]
        public void Settings_GarbageInTheStore_IsRepaired()
        {
            var store = new MemoryStore();
            store.SetFloat(GameSettings.KeySensitivity, float.NaN);
            store.SetInt(GameSettings.KeyLanguage, 42);

            GameSettingsData d = GameSettings.Load(store);

            Assert.AreEqual(GameSettingsData.DefaultSensitivity, d.LookSensitivity);
            Assert.AreEqual(LocLanguage.Tr, d.Language);
        }

        [Test]
        public void LookScaling_UsesTheSensitivity_AndFlipsYWhenInverted()
        {
            var settings = new GameSettings(new MemoryStore());
            GameSettingsData d = settings.Current;
            d.LookSensitivity = 2f;
            d.InvertY = true;
            settings.Set(d);

            Vector2 scaled = settings.ScaleLook(new Vector2(1f, 3f));

            Assert.AreEqual(2f, scaled.x, 1e-5f);
            Assert.AreEqual(-6f, scaled.y, 1e-5f);
        }

        [Test]
        public void AnOpenMenu_SwitchesTheGameplayMapsOff_AndClosingItRestoresThem()
        {
            var source = new LocalInputSource(() => new PlayerId(0));
            try
            {
                ((ILocalInputProvider)source).AcquireLocal(Role.Human);
                Assert.AreEqual(InputMode.Human, source.Mode);

                EventBus.Publish(new MenuFocusChangedEvt(true));
                Assert.AreEqual(InputMode.None, source.Mode);
                ((ILocalInputProvider)source).AcquireLocal(Role.Human);
                Assert.AreEqual(InputMode.None, source.Mode, "a role change under the menu waits for it to close");

                EventBus.Publish(new MenuFocusChangedEvt(false));
                Assert.AreEqual(InputMode.Human, source.Mode);
            }
            finally
            {
                source.Dispose();
            }
        }

        [Test]
        public void MenuFocus_AnnouncesOpenAndClose_OnlyOnChange()
        {
            var focus = new MenuFocus();
            var seen = new List<bool>();
            EventBus.Subscribe<MenuFocusChangedEvt>(e => seen.Add(e.IsOpen));
            float timeScale = Time.timeScale;

            focus.SetOpen(true);
            focus.SetOpen(true);
            focus.SetOpen(false);

            CollectionAssert.AreEqual(new[] { true, false }, seen);
            Assert.AreEqual(timeScale, Time.timeScale, "no freeze asked: time untouched");
        }
    }
}
