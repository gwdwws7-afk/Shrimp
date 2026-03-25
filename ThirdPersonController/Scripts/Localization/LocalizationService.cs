using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    public class LocalizationService : Singleton<LocalizationService>
    {
        private const string LanguagePrefKey = "ThirdPersonController.Localization.Language";

        [Header("Localization")]
        public LocalizationLanguage defaultLanguage = LocalizationLanguage.SimplifiedChinese;
        public string tableResourcePath = "Localization/DefaultLocalizationTable";
        public LocalizationTable table;
        public bool logMissingLocalization = true;

        public LocalizationLanguage CurrentLanguage { get; private set; }
        public event Action<LocalizationLanguage> OnLanguageChanged;
        public event Action<string, LocalizationLanguage, string> OnMissingLocalization;

        private readonly HashSet<string> reportedMissingKeys = new HashSet<string>(StringComparer.Ordinal);

        protected override void OnAwake()
        {
            LoadTableIfNeeded();
            CurrentLanguage = LoadLanguage();
        }

        public void SetLanguage(LocalizationLanguage language)
        {
            if (CurrentLanguage == language)
            {
                return;
            }

            CurrentLanguage = language;
            PlayerPrefs.SetInt(LanguagePrefKey, (int)language);
            PlayerPrefs.Save();
            OnLanguageChanged?.Invoke(CurrentLanguage);
        }

        public void SetTable(LocalizationTable newTable)
        {
            table = newTable;
        }

        public string Get(string key, string fallback = "")
        {
            LoadTableIfNeeded();

            if (table != null && table.TryGet(key, out LocalizationEntry entry))
            {
                string text = entry.Get(CurrentLanguage);
                if (!string.IsNullOrEmpty(text))
                {
                    return text;
                }

                if (CurrentLanguage != LocalizationLanguage.English)
                {
                    string englishFallback = entry.Get(LocalizationLanguage.English);
                    if (!string.IsNullOrEmpty(englishFallback))
                    {
                        ReportMissingLocalization(key, CurrentLanguage, "MissingCurrentLanguage_UseEnglishFallback");
                        return englishFallback;
                    }
                }

                ReportMissingLocalization(key, CurrentLanguage, "MissingLocalizedText");
            }
            else
            {
                ReportMissingLocalization(key, CurrentLanguage, "MissingKey");
            }

            if (!string.IsNullOrEmpty(fallback))
            {
                return fallback;
            }

            return string.IsNullOrEmpty(key) ? string.Empty : key;
        }

        public void ClearMissingLocalizationAudit()
        {
            reportedMissingKeys.Clear();
        }

        private void LoadTableIfNeeded()
        {
            if (table != null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(tableResourcePath))
            {
                table = Resources.Load<LocalizationTable>(tableResourcePath);
            }
        }

        private LocalizationLanguage LoadLanguage()
        {
            if (!PlayerPrefs.HasKey(LanguagePrefKey))
            {
                return defaultLanguage;
            }

            int raw = PlayerPrefs.GetInt(LanguagePrefKey, (int)defaultLanguage);
            if (Enum.IsDefined(typeof(LocalizationLanguage), raw))
            {
                return (LocalizationLanguage)raw;
            }

            return defaultLanguage;
        }

        private void ReportMissingLocalization(string key, LocalizationLanguage language, string reason)
        {
            string safeKey = string.IsNullOrEmpty(key) ? "<empty>" : key;
            string dedupeKey = $"{safeKey}|{language}|{reason}";
            if (!reportedMissingKeys.Add(dedupeKey))
            {
                return;
            }

            OnMissingLocalization?.Invoke(safeKey, language, reason);
            if (logMissingLocalization)
            {
                Debug.LogWarning($"[Localization] Missing localization for key '{safeKey}' ({language}) reason={reason}");
            }
        }
    }
}
