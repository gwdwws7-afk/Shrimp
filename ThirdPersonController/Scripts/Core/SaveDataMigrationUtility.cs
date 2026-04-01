using System;
using System.Collections.Generic;

namespace ThirdPersonController
{
    /// <summary>
    /// Centralized save-data schema migration utility.
    /// Keeps backward compatibility logic outside SaveManager load flow.
    /// </summary>
    public static class SaveDataMigrationUtility
    {
        public const int LatestSchemaVersion = 2;

        public static bool TryMigrate(GameData data, out string summary)
        {
            summary = "noop";
            if (data == null)
            {
                summary = "data_null";
                return false;
            }

            var notes = new List<string>(16);
            bool changed = false;

            int sourceVersion = data.saveSchemaVersion;
            if (sourceVersion <= 0)
            {
                sourceVersion = 1;
                notes.Add("assume_legacy_v1");
            }

            while (sourceVersion < LatestSchemaVersion)
            {
                switch (sourceVersion)
                {
                    case 1:
                        if (MigrateV1ToV2(data, notes))
                        {
                            changed = true;
                        }
                        sourceVersion = 2;
                        break;
                    default:
                        notes.Add($"unknown_source_{sourceVersion}_force_latest");
                        sourceVersion = LatestSchemaVersion;
                        changed = true;
                        break;
                }
            }

            if (sourceVersion != data.saveSchemaVersion)
            {
                data.saveSchemaVersion = sourceVersion;
                changed = true;
            }

            if (NormalizePostMigrationDefaults(data, notes))
            {
                changed = true;
            }

            summary = notes.Count > 0 ? string.Join(";", notes) : "noop";
            return changed;
        }

        private static bool MigrateV1ToV2(GameData data, List<string> notes)
        {
            bool changed = false;

            if (data.quickConsumableSlots == null)
            {
                data.quickConsumableSlots = new List<string>();
                notes.Add("v1_to_v2_create_quick_slots");
                changed = true;
            }

            while (data.quickConsumableSlots.Count < 3)
            {
                data.quickConsumableSlots.Add(string.Empty);
                changed = true;
            }
            if (changed)
            {
                notes.Add("v1_to_v2_pad_quick_slots_to_3");
            }

            if (string.IsNullOrWhiteSpace(data.activeProgressionRoute))
            {
                data.activeProgressionRoute = "Offense";
                notes.Add("v1_to_v2_default_progression_route");
                changed = true;
            }

            if (!Enum.IsDefined(typeof(LocalizationLanguage), data.localizationLanguage))
            {
                data.localizationLanguage = (int)LocalizationLanguage.SimplifiedChinese;
                notes.Add("v1_to_v2_normalize_localization_language");
                changed = true;
            }

            if (data.questStates == null)
            {
                data.questStates = new List<QuestStateData>();
                notes.Add("v1_to_v2_create_quest_states");
                changed = true;
            }

            if (data.consumables == null)
            {
                data.consumables = new List<ConsumableStack>();
                notes.Add("v1_to_v2_create_consumables");
                changed = true;
            }

            notes.Add("migrate_v1_to_v2");
            return changed;
        }

        private static bool NormalizePostMigrationDefaults(GameData data, List<string> notes)
        {
            bool changed = false;

            if (data.unlockedTalentNodes == null)
            {
                data.unlockedTalentNodes = new List<string>();
                changed = true;
            }

            if (data.ownedPearlIds == null)
            {
                data.ownedPearlIds = new List<string>();
                changed = true;
            }

            if (data.equippedPearlIds == null)
            {
                data.equippedPearlIds = new List<string>();
                changed = true;
            }

            if (data.completedLevels == null)
            {
                data.completedLevels = new List<int>();
                changed = true;
            }

            if (data.consumables == null)
            {
                data.consumables = new List<ConsumableStack>();
                changed = true;
            }

            if (data.quickConsumableSlots == null)
            {
                data.quickConsumableSlots = new List<string>();
                changed = true;
            }

            while (data.quickConsumableSlots.Count < 3)
            {
                data.quickConsumableSlots.Add(string.Empty);
                changed = true;
            }

            if (data.questStates == null)
            {
                data.questStates = new List<QuestStateData>();
                changed = true;
            }

            if (data.claimedProgressionMilestones == null)
            {
                data.claimedProgressionMilestones = new List<string>();
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(data.activeProgressionRoute))
            {
                data.activeProgressionRoute = "Offense";
                changed = true;
            }

            int normalizedFailureStreak = Math.Max(0, data.questFailureStreak);
            if (normalizedFailureStreak != data.questFailureStreak)
            {
                data.questFailureStreak = normalizedFailureStreak;
                changed = true;
            }

            float normalizedFailureDebtExp = Math.Max(0f, data.questFailureDebtExp);
            if (Math.Abs(normalizedFailureDebtExp - data.questFailureDebtExp) > 0.0001f)
            {
                data.questFailureDebtExp = normalizedFailureDebtExp;
                changed = true;
            }

            float normalizedFailureDebtCredits = Math.Max(0f, data.questFailureDebtCredits);
            if (Math.Abs(normalizedFailureDebtCredits - data.questFailureDebtCredits) > 0.0001f)
            {
                data.questFailureDebtCredits = normalizedFailureDebtCredits;
                changed = true;
            }

            int normalizedFailureChapter = Math.Max(0, data.questFailureLastChapterId);
            if (normalizedFailureChapter != data.questFailureLastChapterId)
            {
                data.questFailureLastChapterId = normalizedFailureChapter;
                changed = true;
            }

            if (data.questFailureLastStrongholdId == null)
            {
                data.questFailureLastStrongholdId = string.Empty;
                changed = true;
            }

            if (!Enum.IsDefined(typeof(LocalizationLanguage), data.localizationLanguage))
            {
                data.localizationLanguage = (int)LocalizationLanguage.SimplifiedChinese;
                changed = true;
            }

            if (changed)
            {
                notes.Add("post_migration_normalized_defaults");
            }

            return changed;
        }
    }
}
