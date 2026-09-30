using System;
using UnityEngine;

namespace CinliMahzen.Core
{
    [Serializable]
    public struct LocEntry
    {
        [SerializeField] private string key;
        [SerializeField] private string tr;
        [SerializeField] private string en;

        public LocEntry(string key, string tr, string en)
        {
            this.key = key;
            this.tr = tr;
            this.en = en;
        }

        public string Key => key;
        public string Tr => tr;
        public string En => en;

        public string Get(LocLanguage lang) => lang == LocLanguage.En && !string.IsNullOrEmpty(en) ? en : tr;
    }
}
