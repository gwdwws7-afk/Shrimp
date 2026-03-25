using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ThirdPersonController;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonController.Editor
{
    public static class GrowthEconomyConfigGateValidator
    {
        private const string ValidateMenuPath = "Tools/Productization/P5/Validate Growth Economy Config (CSV)";
        private const string ValidateGateMenuPath = "Tools/Productization/P5/Validate Growth Economy Config (CI Gate)";
        private const string FixMenuPath = "Tools/Productization/P5/Fix Growth Economy Config";
        private const string ReportCsvPath = "Assets/ThirdPersonController/Reports/growth_economy_config_gate_report.csv";
        private const string DefaultConfigAssetPath = "Assets/GameDesign/Data/EconomyConfig_Sample.asset";
        private const int MinDifficultyTableLength = 5;

        private static readonly string[] SearchRoots =
        {
            "Assets/GameDesign/Data",
            "Assets/Resources",
            "Assets/ThirdPersonController/Resources"
        };

        private struct Row
        {
            public string layer;
            public string asset;
            public string key;
            public string status;
            public string value;
            public string note;
        }

        private sealed class Context
        {
            public readonly HashSet<int> chapterIds = new HashSet<int>();
            public readonly HashSet<QuestType> questTypes = new HashSet<QuestType>();
            public readonly HashSet<QuestRewardTier> questTiers = new HashSet<QuestRewardTier>();
        }

        [MenuItem(ValidateMenuPath)]
        public static void Validate()
        {
            Run(applyFix: false, failOnBlocking: false, interactive: true);
        }

        [MenuItem(ValidateGateMenuPath)]
        public static void ValidateCiGate()
        {
            Run(applyFix: false, failOnBlocking: true, interactive: false);
        }

        [MenuItem(FixMenuPath)]
        public static void Fix()
        {
            Run(applyFix: true, failOnBlocking: false, interactive: true);
        }

        public static void ApplyForBatch()
        {
            Run(applyFix: true, failOnBlocking: true, interactive: false);
        }

        public static void ValidateForBatch()
        {
            Run(applyFix: false, failOnBlocking: true, interactive: false);
        }

        private static void Run(bool applyFix, bool failOnBlocking, bool interactive)
        {
            var rows = new List<Row>(128);
            int blocking = 0;
            int fixedCount = 0;
            Context context = CollectContext();

            HashSet<string> configPaths = CollectEconomyConfigPaths();
            if (configPaths.Count == 0)
            {
                if (applyFix)
                {
                    string created = EnsureDefaultConfigAsset();
                    if (!string.IsNullOrWhiteSpace(created))
                    {
                        configPaths = CollectEconomyConfigPaths();
                        fixedCount++;
                        rows.Add(NewRow("Bootstrap", created, "create_default", "Fixed", "created", "Created default EconomyConfig asset."));
                    }
                    else
                    {
                        blocking++;
                        rows.Add(NewRow("Bootstrap", string.Empty, "create_default", "Error", "failed", "Cannot create default EconomyConfig asset."));
                    }
                }
                else
                {
                    blocking++;
                    rows.Add(NewRow("Bootstrap", string.Empty, "economy_config", "Gap", "missing", "No EconomyConfig assets found."));
                }
            }

            var sorted = new List<string>(configPaths);
            sorted.Sort(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < sorted.Count; i++)
            {
                ValidateSingleConfig(sorted[i], context, applyFix, rows, ref blocking, ref fixedCount);
            }

            string reportPath = WriteCsv(rows);
            AssetDatabase.Refresh();
            string summary = $"rows={rows.Count} blocking={blocking} fixed={fixedCount} report={reportPath}";
            Debug.Log($"[GrowthEconomyConfigGate] mode={(applyFix ? "fix" : "validate")} {summary}");
            if (interactive)
            {
                EditorUtility.DisplayDialog("Growth Economy Config Gate", summary, "OK");
            }

            if (failOnBlocking && blocking > 0)
            {
                throw new InvalidOperationException($"[GrowthEconomyConfigGate] gate failed. blocking={blocking} report={reportPath}");
            }
        }

        private static void ValidateSingleConfig(string path, Context context, bool applyFix, List<Row> rows, ref int blocking, ref int fixedCount)
        {
            EconomyConfig config = AssetDatabase.LoadAssetAtPath<EconomyConfig>(path);
            if (config == null)
            {
                blocking++;
                rows.Add(NewRow("Config", path, "load", "Error", "null", "EconomyConfig failed to load."));
                return;
            }

            int localBlocking = 0;
            int localFixed = 0;
            bool changed = false;

            ValidateNonNegative(path, "enemyExpMultiplier", config.enemyExpMultiplier, v => config.enemyExpMultiplier = v, applyFix, rows, ref localBlocking, ref localFixed, ref changed);
            ValidateNonNegative(path, "levelExpMultiplier", config.levelExpMultiplier, v => config.levelExpMultiplier = v, applyFix, rows, ref localBlocking, ref localFixed, ref changed);
            ValidateNonNegative(path, "questExpMultiplier", config.questExpMultiplier, v => config.questExpMultiplier = v, applyFix, rows, ref localBlocking, ref localFixed, ref changed);
            ValidateNonNegative(path, "pearlDropMultiplier", config.pearlDropMultiplier, v => config.pearlDropMultiplier = v, applyFix, rows, ref localBlocking, ref localFixed, ref changed);
            ValidateNonNegative(path, "levelPearlMultiplier", config.levelPearlMultiplier, v => config.levelPearlMultiplier = v, applyFix, rows, ref localBlocking, ref localFixed, ref changed);
            ValidateNonNegative(path, "questPearlMultiplier", config.questPearlMultiplier, v => config.questPearlMultiplier = v, applyFix, rows, ref localBlocking, ref localFixed, ref changed);
            ValidateNonNegative(path, "levelCreditMultiplier", config.levelCreditMultiplier, v => config.levelCreditMultiplier = v, applyFix, rows, ref localBlocking, ref localFixed, ref changed);
            ValidateNonNegative(path, "questCreditMultiplier", config.questCreditMultiplier, v => config.questCreditMultiplier = v, applyFix, rows, ref localBlocking, ref localFixed, ref changed);
            ValidateNonNegative(path, "shopPriceMultiplier", config.shopPriceMultiplier, v => config.shopPriceMultiplier = v, applyFix, rows, ref localBlocking, ref localFixed, ref changed);

            ValidateMin(path, "killsPerTalentPoint", config.killsPerTalentPoint, 1, v => config.killsPerTalentPoint = v, applyFix, rows, ref localBlocking, ref localFixed, ref changed);
            ValidateMin(path, "pointsPerKillMilestone", config.pointsPerKillMilestone, 0, v => config.pointsPerKillMilestone = v, applyFix, rows, ref localBlocking, ref localFixed, ref changed);
            ValidateMin(path, "pointsPerStageClear", config.pointsPerStageClear, 0, v => config.pointsPerStageClear = v, applyFix, rows, ref localBlocking, ref localFixed, ref changed);

            NormalizeTable(path, "levelExpDifficultyMultipliers", config.levelExpDifficultyMultipliers, v => config.levelExpDifficultyMultipliers = v, applyFix, rows, ref localBlocking, ref localFixed, ref changed);
            NormalizeTable(path, "levelPearlDifficultyMultipliers", config.levelPearlDifficultyMultipliers, v => config.levelPearlDifficultyMultipliers = v, applyFix, rows, ref localBlocking, ref localFixed, ref changed);
            NormalizeTable(path, "levelCreditDifficultyMultipliers", config.levelCreditDifficultyMultipliers, v => config.levelCreditDifficultyMultipliers = v, applyFix, rows, ref localBlocking, ref localFixed, ref changed);
            NormalizeTable(path, "questExpDifficultyMultipliers", config.questExpDifficultyMultipliers, v => config.questExpDifficultyMultipliers = v, applyFix, rows, ref localBlocking, ref localFixed, ref changed);
            NormalizeTable(path, "questPearlDifficultyMultipliers", config.questPearlDifficultyMultipliers, v => config.questPearlDifficultyMultipliers = v, applyFix, rows, ref localBlocking, ref localFixed, ref changed);
            NormalizeTable(path, "questCreditDifficultyMultipliers", config.questCreditDifficultyMultipliers, v => config.questCreditDifficultyMultipliers = v, applyFix, rows, ref localBlocking, ref localFixed, ref changed);
            NormalizeTable(path, "dropChanceDifficultyMultipliers", config.dropChanceDifficultyMultipliers, v => config.dropChanceDifficultyMultipliers = v, applyFix, rows, ref localBlocking, ref localFixed, ref changed);
            NormalizeTable(path, "shopPriceDifficultyMultipliers", config.shopPriceDifficultyMultipliers, v => config.shopPriceDifficultyMultipliers = v, applyFix, rows, ref localBlocking, ref localFixed, ref changed);

            EnsureQuestTypeCoverage(path, config, context.questTypes, applyFix, rows, ref localBlocking, ref localFixed, ref changed);
            EnsureQuestTierCoverage(path, config, context.questTiers, applyFix, rows, ref localBlocking, ref localFixed, ref changed);
            EnsureChapterCoverage(path, config, context.chapterIds, applyFix, rows, ref localBlocking, ref localFixed, ref changed);

            if (changed && applyFix)
            {
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
            }

            blocking += localBlocking;
            fixedCount += localFixed;
            rows.Add(NewRow("Summary", path, "config.summary", localBlocking > 0 ? "Gap" : (localFixed > 0 ? "Fixed" : "Ok"), $"blocking={localBlocking};fixed={localFixed}", "done"));
        }

        private static void EnsureQuestTypeCoverage(string path, EconomyConfig config, HashSet<QuestType> required, bool applyFix, List<Row> rows, ref int blocking, ref int fixedCount, ref bool changed)
        {
            if (required == null || required.Count == 0)
            {
                return;
            }

            if (config.questTypeMultipliers == null)
            {
                if (applyFix)
                {
                    config.questTypeMultipliers = new List<QuestTypeRewardMultiplier>();
                    changed = true;
                    fixedCount++;
                }
                else
                {
                    blocking++;
                    rows.Add(NewRow("Coverage", path, "questTypeMultipliers", "Gap", "null", "List is null."));
                    return;
                }
            }

            var existing = new HashSet<QuestType>();
            for (int i = 0; i < config.questTypeMultipliers.Count; i++)
            {
                QuestTypeRewardMultiplier e = config.questTypeMultipliers[i];
                if (e != null)
                {
                    existing.Add(e.questType);
                }
            }

            foreach (QuestType type in required)
            {
                if (existing.Contains(type))
                {
                    continue;
                }

                if (applyFix)
                {
                    config.questTypeMultipliers.Add(new QuestTypeRewardMultiplier { questType = type, expMultiplier = 1f, pearlMultiplier = 1f, creditMultiplier = 1f });
                    fixedCount++;
                    changed = true;
                    rows.Add(NewRow("Coverage", path, $"questType:{type}", "Fixed", "added", "Added default multiplier."));
                }
                else
                {
                    blocking++;
                    rows.Add(NewRow("Coverage", path, $"questType:{type}", "Gap", "missing", "Missing required quest type multiplier."));
                }
            }
        }

        private static void EnsureQuestTierCoverage(string path, EconomyConfig config, HashSet<QuestRewardTier> required, bool applyFix, List<Row> rows, ref int blocking, ref int fixedCount, ref bool changed)
        {
            if (required == null || required.Count == 0)
            {
                return;
            }

            if (config.questTierMultipliers == null)
            {
                if (applyFix)
                {
                    config.questTierMultipliers = new List<QuestTierRewardMultiplier>();
                    changed = true;
                    fixedCount++;
                }
                else
                {
                    blocking++;
                    rows.Add(NewRow("Coverage", path, "questTierMultipliers", "Gap", "null", "List is null."));
                    return;
                }
            }

            var existing = new HashSet<QuestRewardTier>();
            for (int i = 0; i < config.questTierMultipliers.Count; i++)
            {
                QuestTierRewardMultiplier e = config.questTierMultipliers[i];
                if (e != null)
                {
                    existing.Add(e.tier);
                }
            }

            foreach (QuestRewardTier tier in required)
            {
                if (existing.Contains(tier))
                {
                    continue;
                }

                if (applyFix)
                {
                    config.questTierMultipliers.Add(new QuestTierRewardMultiplier { tier = tier, expMultiplier = 1f, pearlMultiplier = 1f, creditMultiplier = 1f });
                    fixedCount++;
                    changed = true;
                    rows.Add(NewRow("Coverage", path, $"questTier:{tier}", "Fixed", "added", "Added default multiplier."));
                }
                else
                {
                    blocking++;
                    rows.Add(NewRow("Coverage", path, $"questTier:{tier}", "Gap", "missing", "Missing required quest tier multiplier."));
                }
            }
        }

        private static void EnsureChapterCoverage(string path, EconomyConfig config, HashSet<int> required, bool applyFix, List<Row> rows, ref int blocking, ref int fixedCount, ref bool changed)
        {
            if (required == null || required.Count == 0)
            {
                return;
            }

            if (config.questChapterMultipliers == null)
            {
                if (applyFix)
                {
                    config.questChapterMultipliers = new List<QuestChapterRewardMultiplier>();
                    changed = true;
                    fixedCount++;
                }
                else
                {
                    blocking++;
                    rows.Add(NewRow("Coverage", path, "questChapterMultipliers", "Gap", "null", "List is null."));
                    return;
                }
            }

            var existing = new HashSet<int>();
            for (int i = 0; i < config.questChapterMultipliers.Count; i++)
            {
                QuestChapterRewardMultiplier e = config.questChapterMultipliers[i];
                if (e != null && e.chapterId > 0)
                {
                    existing.Add(e.chapterId);
                }
            }

            foreach (int chapterId in required)
            {
                if (chapterId <= 0 || existing.Contains(chapterId))
                {
                    continue;
                }

                if (applyFix)
                {
                    config.questChapterMultipliers.Add(new QuestChapterRewardMultiplier { chapterId = chapterId, expMultiplier = 1f, pearlMultiplier = 1f, creditMultiplier = 1f });
                    fixedCount++;
                    changed = true;
                    rows.Add(NewRow("Coverage", path, $"chapter:{chapterId}", "Fixed", "added", "Added default multiplier."));
                }
                else
                {
                    blocking++;
                    rows.Add(NewRow("Coverage", path, $"chapter:{chapterId}", "Gap", "missing", "Missing required chapter multiplier."));
                }
            }
        }

        private static void NormalizeTable(string path, string key, float[] current, Action<float[]> setTable, bool applyFix, List<Row> rows, ref int blocking, ref int fixedCount, ref bool changed)
        {
            bool needsResize = current == null || current.Length < MinDifficultyTableLength;
            bool hasNegative = false;
            if (current != null)
            {
                for (int i = 0; i < current.Length; i++)
                {
                    if (current[i] < 0f)
                    {
                        hasNegative = true;
                        break;
                    }
                }
            }

            if (!needsResize && !hasNegative)
            {
                rows.Add(NewRow("Table", path, key, "Ok", $"len={current.Length}", "table_ok"));
                return;
            }

            if (!applyFix)
            {
                blocking++;
                rows.Add(NewRow("Table", path, key, "Gap", current == null ? "null" : $"len={current.Length}", "Table too short or has negative values."));
                return;
            }

            int targetLen = Math.Max(MinDifficultyTableLength, current != null ? current.Length : 0);
            var next = new float[targetLen];
            for (int i = 0; i < targetLen; i++)
            {
                float v = 1f;
                if (current != null && i < current.Length)
                {
                    v = current[i];
                }

                next[i] = Mathf.Max(0f, v);
            }

            setTable(next);
            changed = true;
            fixedCount++;
            rows.Add(NewRow("Table", path, key, "Fixed", $"len={next.Length}", "Normalized table length and values."));
        }

        private static void ValidateNonNegative(string path, string key, float current, Action<float> setValue, bool applyFix, List<Row> rows, ref int blocking, ref int fixedCount, ref bool changed)
        {
            if (current >= 0f)
            {
                rows.Add(NewRow("Scalar", path, key, "Ok", current.ToString("0.###"), "within_range"));
                return;
            }

            if (!applyFix)
            {
                blocking++;
                rows.Add(NewRow("Scalar", path, key, "Gap", current.ToString("0.###"), "Negative value."));
                return;
            }

            setValue(0f);
            changed = true;
            fixedCount++;
            rows.Add(NewRow("Scalar", path, key, "Fixed", "0", "Clamped to 0."));
        }

        private static void ValidateMin(string path, string key, int current, int minValue, Action<int> setValue, bool applyFix, List<Row> rows, ref int blocking, ref int fixedCount, ref bool changed)
        {
            if (current >= minValue)
            {
                rows.Add(NewRow("Scalar", path, key, "Ok", current.ToString(), "within_range"));
                return;
            }

            if (!applyFix)
            {
                blocking++;
                rows.Add(NewRow("Scalar", path, key, "Gap", current.ToString(), $"Must be >= {minValue}."));
                return;
            }

            setValue(minValue);
            changed = true;
            fixedCount++;
            rows.Add(NewRow("Scalar", path, key, "Fixed", minValue.ToString(), "Raised to minimum."));
        }

        private static Context CollectContext()
        {
            var context = new Context();

            string[] levelGuids = AssetDatabase.FindAssets("t:LevelData", SearchRoots);
            for (int i = 0; i < levelGuids.Length; i++)
            {
                LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(levelGuids[i]));
                if (level != null && level.chapterId > 0)
                {
                    context.chapterIds.Add(level.chapterId);
                }
            }

            string[] questGuids = AssetDatabase.FindAssets("t:QuestDatabase", SearchRoots);
            for (int i = 0; i < questGuids.Length; i++)
            {
                QuestDatabase db = AssetDatabase.LoadAssetAtPath<QuestDatabase>(AssetDatabase.GUIDToAssetPath(questGuids[i]));
                if (db == null || db.quests == null)
                {
                    continue;
                }

                for (int q = 0; q < db.quests.Count; q++)
                {
                    QuestData quest = db.quests[q];
                    if (quest == null)
                    {
                        continue;
                    }

                    context.questTypes.Add(quest.questType);
                    context.questTiers.Add(quest.rewardTier);
                    if (quest.stages == null)
                    {
                        continue;
                    }

                    for (int s = 0; s < quest.stages.Count; s++)
                    {
                        QuestStage stage = quest.stages[s];
                        if (stage != null)
                        {
                            context.questTypes.Add(stage.questType);
                        }
                    }
                }
            }

            return context;
        }

        private static HashSet<string> CollectEconomyConfigPaths()
        {
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string[] guids = AssetDatabase.FindAssets("t:EconomyConfig", SearchRoots);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!string.IsNullOrWhiteSpace(path))
                {
                    paths.Add(path);
                }
            }

            return paths;
        }

        private static string EnsureDefaultConfigAsset()
        {
            if (AssetDatabase.LoadAssetAtPath<EconomyConfig>(DefaultConfigAssetPath) != null)
            {
                return DefaultConfigAssetPath;
            }

            string folder = Path.GetDirectoryName(DefaultConfigAssetPath).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(folder))
            {
                EnsureFolder(folder);
            }

            EconomyConfig config = ScriptableObject.CreateInstance<EconomyConfig>();
            AssetDatabase.CreateAsset(config, DefaultConfigAssetPath);
            AssetDatabase.SaveAssets();
            return DefaultConfigAssetPath;
        }

        private static void EnsureFolder(string path)
        {
            string[] segments = path.Split('/');
            string current = segments[0];
            for (int i = 1; i < segments.Length; i++)
            {
                string next = current + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[i]);
                }

                current = next;
            }
        }

        private static string WriteCsv(List<Row> rows)
        {
            string fullPath = Path.GetFullPath(ReportCsvPath);
            string dir = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var sb = new StringBuilder(4096);
            sb.AppendLine("layer,asset,key,status,value,note");
            for (int i = 0; i < rows.Count; i++)
            {
                Row row = rows[i];
                sb.Append(Escape(row.layer)).Append(',')
                    .Append(Escape(row.asset)).Append(',')
                    .Append(Escape(row.key)).Append(',')
                    .Append(Escape(row.status)).Append(',')
                    .Append(Escape(row.value)).Append(',')
                    .Append(Escape(row.note))
                    .AppendLine();
            }

            File.WriteAllText(fullPath, sb.ToString(), new UTF8Encoding(false));
            return ReportCsvPath;
        }

        private static Row NewRow(string layer, string asset, string key, string status, string value, string note)
        {
            return new Row
            {
                layer = layer ?? string.Empty,
                asset = asset ?? string.Empty,
                key = key ?? string.Empty,
                status = status ?? string.Empty,
                value = value ?? string.Empty,
                note = note ?? string.Empty
            };
        }

        private static string Escape(string value)
        {
            if (value == null)
            {
                value = string.Empty;
            }

            bool needsQuote = value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0;
            if (value.IndexOf('"') >= 0)
            {
                value = value.Replace("\"", "\"\"");
            }

            return needsQuote ? $"\"{value}\"" : value;
        }
    }
}
