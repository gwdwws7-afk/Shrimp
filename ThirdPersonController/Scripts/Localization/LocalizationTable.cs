using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    [CreateAssetMenu(
        fileName = "LocalizationTable",
        menuName = "ThirdPersonController/Localization/Localization Table")]
    public class LocalizationTable : ScriptableObject
    {
        public List<LocalizationEntry> entries = new List<LocalizationEntry>();

        private Dictionary<string, LocalizationEntry> lookup;

        public bool TryGet(string key, out LocalizationEntry entry)
        {
            entry = null;
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            EnsureLookup();
            return lookup.TryGetValue(key, out entry);
        }

        public void RebuildLookup()
        {
            lookup = null;
            EnsureLookup();
        }

        private void EnsureLookup()
        {
            if (lookup != null)
            {
                return;
            }

            lookup = new Dictionary<string, LocalizationEntry>(StringComparer.Ordinal);
            for (int i = 0; i < entries.Count; i++)
            {
                LocalizationEntry entry = entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.key))
                {
                    continue;
                }

                if (!lookup.ContainsKey(entry.key))
                {
                    lookup.Add(entry.key, entry);
                }
            }
        }
    }
}
