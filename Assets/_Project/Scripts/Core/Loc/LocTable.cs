using System.Collections.Generic;
using UnityEngine;

namespace CinliMahzen.Core
{
    /// <summary>
    /// One agent's localisation table (tr/en columns). Each agent owns its own asset under
    /// Assets/_Project/Resources/Loc/LocTable_&lt;Agent&gt;.asset; <see cref="Loc"/> loads all of them.
    /// </summary>
    [CreateAssetMenu(menuName = "CinliMahzen/Loc/Loc Table", fileName = "LocTable_X")]
    public sealed class LocTable : ScriptableObject
    {
        [SerializeField] private List<LocEntry> entries = new List<LocEntry>();

        public IReadOnlyList<LocEntry> Entries => entries;

        /// <summary>Editor/test helper. Replaces an existing key.</summary>
        public void Set(string key, string tr, string en)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Key == key)
                {
                    entries[i] = new LocEntry(key, tr, en);
                    return;
                }
            }
            entries.Add(new LocEntry(key, tr, en));
        }
    }
}
