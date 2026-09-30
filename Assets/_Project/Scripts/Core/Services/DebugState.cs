using System;
using System.Collections.Generic;
using System.Text;

namespace CinliMahzen.Core
{
    /// <summary>
    /// Sections for "CinliMahzen/Debug/Dump State To Console" (Teknik §12.2). Every module may register a section
    /// that returns a JSON value (object/array/string), e.g. DebugState.Register("possession", BuildPossessionJson).
    /// </summary>
    public static class DebugState
    {
        private static readonly List<KeyValuePair<string, Func<string>>> Sections = new List<KeyValuePair<string, Func<string>>>();

        public static void Register(string key, Func<string> jsonValue)
        {
            Unregister(key);
            Sections.Add(new KeyValuePair<string, Func<string>>(key, jsonValue));
        }

        public static void Unregister(string key)
        {
            for (int i = Sections.Count - 1; i >= 0; i--)
            {
                if (Sections[i].Key == key)
                    Sections.RemoveAt(i);
            }
        }

        public static string BuildJson()
        {
            var sb = new StringBuilder(1024);
            sb.Append('{');
            for (int i = 0; i < Sections.Count; i++)
            {
                if (i > 0)
                    sb.Append(',');
                sb.Append('"').Append(Sections[i].Key).Append("\":");
                string value;
                try
                {
                    value = Sections[i].Value();
                }
                catch (Exception e)
                {
                    value = Quote("error: " + e.Message);
                }
                sb.Append(string.IsNullOrEmpty(value) ? "null" : value);
            }
            sb.Append('}');
            return sb.ToString();
        }

        /// <summary>JSON string literal helper.</summary>
        public static string Quote(string s)
        {
            if (s == null)
                return "null";
            return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Clear()
        {
            Sections.Clear();
        }
    }
}
