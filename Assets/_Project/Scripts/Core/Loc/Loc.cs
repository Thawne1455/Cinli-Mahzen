using System.Collections.Generic;
using UnityEngine;

namespace CinliMahzen.Core
{
    /// <summary>
    /// Localisation lookup. Every user-visible string goes through <see cref="T(string)"/>.
    /// Lazily loads every <see cref="LocTable"/> under Resources/Loc on first use.
    /// Missing key → "#key" (+ one warning per key).
    /// </summary>
    public static class Loc
    {
        public const string ResourcesFolder = "Loc";

        private static readonly Dictionary<string, LocEntry> Entries = new Dictionary<string, LocEntry>();
        private static readonly HashSet<string> WarnedMissing = new HashSet<string>();
        private static bool _loaded;

        public static LocLanguage Language { get; set; } = LocLanguage.Tr;

        public static string T(string key)
        {
            if (!_loaded)
                LoadFromResources();
            if (key != null && Entries.TryGetValue(key, out LocEntry e))
                return e.Get(Language);
            if (key != null && WarnedMissing.Add(key))
                CMLog.Warn("Loc", "Missing key: " + key);
            return "#" + key;
        }

        /// <summary>string.Format over the translated text. Allocates — not for Update.</summary>
        public static string T(string key, params object[] args)
        {
            return string.Format(T(key), args);
        }

        public static bool Has(string key)
        {
            if (!_loaded)
                LoadFromResources();
            return key != null && Entries.ContainsKey(key);
        }

        /// <summary>Replaces the loaded tables with exactly these (tests, tools).</summary>
        public static void UseTables(params LocTable[] tables)
        {
            Clear();
            _loaded = true;
            for (int i = 0; i < tables.Length; i++)
                AddTable(tables[i]);
        }

        public static void AddTable(LocTable table)
        {
            if (table == null)
                return;
            IReadOnlyList<LocEntry> list = table.Entries;
            for (int i = 0; i < list.Count; i++)
            {
                LocEntry e = list[i];
                if (string.IsNullOrEmpty(e.Key))
                    continue;
                if (Entries.ContainsKey(e.Key))
                    CMLog.Warn("Loc", "Duplicate key '" + e.Key + "' in " + table.name + " (first one wins)");
                else
                    Entries.Add(e.Key, e);
            }
        }

        /// <summary>Forgets everything; the next lookup reloads from Resources.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Clear()
        {
            Entries.Clear();
            WarnedMissing.Clear();
            _loaded = false;
        }

        private static void LoadFromResources()
        {
            _loaded = true;
            LocTable[] tables = Resources.LoadAll<LocTable>(ResourcesFolder);
            for (int i = 0; i < tables.Length; i++)
                AddTable(tables[i]);
        }
    }
}
