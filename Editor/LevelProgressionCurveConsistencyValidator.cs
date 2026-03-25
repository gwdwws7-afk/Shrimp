using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ThirdPersonController;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonController.Editor
{
    public static class LevelProgressionCurveConsistencyValidator
    {
        private const string ValidateMenuPath = "Tools/Level/P1/Validate Level Progression Curve Consistency (CSV)";
        private const string ValidateGateMenuPath = "Tools/Level/P1/Validate Level Progression Curve Consistency (CI Gate)";
        private const string ReportCsvPath = "Assets/ThirdPersonController/Reports/level_progression_curve_consistency_report.csv";
        private const string SummaryMdPath = "Assets/ThirdPersonController/Reports/level_progression_curve_consistency_summary.md";
        private const string LogPrefix = "[LevelProgressionCurveConsistency]";
        private const int MinLevelIndex = 2;
        private const int MaxLevelIndex = 10;
        private const float FloatEpsilon = 0.0001f;

        [MenuItem(ValidateMenuPath)]
        public static void Validate()
        {
            Run(interactive: true, failOnError: false);
        }

        [MenuItem(ValidateGateMenuPath)]
        public static void ValidateCiGate()
        {
            Run(interactive: false, failOnError: true);
        }

        public static void ValidateForBatch()
        {
            Run(interactive: false, failOnError: true);
        }

        public static void FixForBatch()
        {
            // Current curve gate is report-only. Keep fix path as validate alias.
            Run(interactive: false, failOnError: true);
        }

        private static void Run(bool interactive, bool failOnError)
        {
            List<LevelEntry> entries = CollectTargetLevels();
            if (entries.Count == 0)
            {
                string noneMessage =
                    $"{LogPrefix} no LevelData assets found for LEVEL_{MinLevelIndex:D2}~LEVEL_{MaxLevelIndex:D2}.";
                Debug.LogWarning(noneMessage);
                if (interactive)
                {
                    EditorUtility.DisplayDialog("Level Progression Curve Consistency", noneMessage, "OK");
                }

                return;
            }

            var levelByIndex = new Dictionary<int, LevelEntry>();
            var duplicatedIndexes = new HashSet<int>();
            for (int i = 0; i < entries.Count; i++)
            {
                LevelEntry entry = entries[i];
                if (levelByIndex.ContainsKey(entry.levelIndex))
                {
                    duplicatedIndexes.Add(entry.levelIndex);
                    if (string.CompareOrdinal(entry.levelAssetPath, levelByIndex[entry.levelIndex].levelAssetPath) < 0)
                    {
                        levelByIndex[entry.levelIndex] = entry;
                    }
                }
                else
                {
                    levelByIndex.Add(entry.levelIndex, entry);
                }
            }

            var rows = new List<ValidationRow>(MaxLevelIndex - MinLevelIndex + 1);
            int transitionCount = 0;
            int increasingPowerTransitions = 0;
            int increasingExpTransitions = 0;
            int increasingCreditTransitions = 0;
            bool hasPreviousStableRow = false;
            ValidationRow previousStableRow = default;

            for (int index = MinLevelIndex; index <= MaxLevelIndex; index++)
            {
                ValidationRow row = ProcessIndex(index, levelByIndex, hasPreviousStableRow, previousStableRow);
                rows.Add(row);

                if (!row.hasData || row.blockingErrors > 0)
                {
                    continue;
                }

                if (hasPreviousStableRow)
                {
                    transitionCount++;
                    if (row.recommendedPower > previousStableRow.recommendedPower)
                    {
                        increasingPowerTransitions++;
                    }

                    if (row.baseExp > previousStableRow.baseExp)
                    {
                        increasingExpTransitions++;
                    }

                    if (row.baseCredits > previousStableRow.baseCredits)
                    {
                        increasingCreditTransitions++;
                    }
                }

                hasPreviousStableRow = true;
                previousStableRow = row;
            }

            var globalWarnings = BuildGlobalWarnings(
                rows,
                duplicatedIndexes,
                transitionCount,
                increasingPowerTransitions,
                increasingExpTransitions,
                increasingCreditTransitions);

            int errorRows = 0;
            int warningTotal = globalWarnings.Count;
            for (int i = 0; i < rows.Count; i++)
            {
                ValidationRow row = rows[i];
                if (string.Equals(row.status, "Error", StringComparison.Ordinal))
                {
                    errorRows++;
                }

                warningTotal += row.warnings;
            }

            string csvPath = WriteCsv(rows);
            string summaryPath = WriteSummary(
                rows,
                errorRows,
                warningTotal,
                globalWarnings,
                transitionCount,
                increasingPowerTransitions,
                increasingExpTransitions,
                increasingCreditTransitions);
            AssetDatabase.Refresh();

            string summary =
                $"targets={rows.Count} errors={errorRows} warnings={warningTotal} csv={csvPath} summary={summaryPath}";
            Debug.Log($"{LogPrefix} complete | {summary}");

            if (interactive)
            {
                EditorUtility.DisplayDialog("Level Progression Curve Consistency", summary, "OK");
            }

            if (failOnError && errorRows > 0)
            {
                throw new InvalidOperationException($"{LogPrefix} gate failed. errors={errorRows} csv={csvPath}");
            }
        }

        private static ValidationRow ProcessIndex(
            int levelIndex,
            Dictionary<int, LevelEntry> levelByIndex,
            bool hasPreviousStableRow,
            ValidationRow previousStableRow)
        {
            if (!levelByIndex.TryGetValue(levelIndex, out LevelEntry entry))
            {
                return new ValidationRow
                {
                    levelIndex = levelIndex,
                    levelId = $"LEVEL_{levelIndex:D2}",
                    levelAssetPath = string.Empty,
                    status = "Error",
                    blockingErrors = 1,
                    warnings = 0,
                    expectedRecommendedLevel = hasPreviousStableRow ? previousStableRow.recommendedLevel + 1 : levelIndex,
                    note = "Missing LevelData asset for expected level index.",
                    hasData = false
                };
            }

            LevelData data = entry.levelData;
            var row = new ValidationRow
            {
                levelIndex = levelIndex,
                levelId = data != null ? data.levelId : string.Empty,
                levelAssetPath = entry.levelAssetPath ?? string.Empty,
                status = "Error",
                hasData = data != null
            };

            var blockingNotes = new List<string>();
            var warningNotes = new List<string>();

            if (data == null)
            {
                blockingNotes.Add("LevelData is null.");
                return BuildRow(row, blockingNotes, warningNotes);
            }

            int parsedLevelFromId = ParseLevelIndex(data.levelId);
            row.recommendedLevel = data.recommendedLevel;
            row.recommendedPower = data.recommendedPower;
            row.baseExp = data.baseExp;
            row.baseCredits = data.baseCredits;
            row.levelRewardMultiplier = data.levelRewardMultiplier;
            row.questRewardMultiplier = data.questRewardMultiplier;
            row.dropChanceMultiplier = data.dropChanceMultiplier;
            row.overrideBossSettings = data.overrideBossSettings;

            if (parsedLevelFromId != levelIndex)
            {
                blockingNotes.Add($"levelId mismatch (expected LEVEL_{levelIndex:D2}, actual '{data.levelId}').");
            }

            if (data.recommendedLevel <= 0)
            {
                blockingNotes.Add("recommendedLevel must be > 0.");
            }

            if (data.recommendedPower <= 0)
            {
                blockingNotes.Add("recommendedPower must be > 0.");
            }

            if (data.baseExp < 0)
            {
                blockingNotes.Add("baseExp must be >= 0.");
            }

            if (data.baseCredits < 0)
            {
                blockingNotes.Add("baseCredits must be >= 0.");
            }

            if (data.levelRewardMultiplier <= 0f)
            {
                blockingNotes.Add("levelRewardMultiplier must be > 0.");
            }

            if (data.questRewardMultiplier <= 0f)
            {
                blockingNotes.Add("questRewardMultiplier must be > 0.");
            }

            if (data.dropChanceMultiplier <= 0f)
            {
                blockingNotes.Add("dropChanceMultiplier must be > 0.");
            }

            if (hasPreviousStableRow)
            {
                row.expectedRecommendedLevel = previousStableRow.recommendedLevel + 1;
                row.recommendedLevelDelta = row.recommendedLevel - previousStableRow.recommendedLevel;
                row.recommendedPowerDelta = row.recommendedPower - previousStableRow.recommendedPower;
                row.baseExpDelta = row.baseExp - previousStableRow.baseExp;
                row.baseCreditsDelta = row.baseCredits - previousStableRow.baseCredits;
                row.levelRewardMultiplierDelta = row.levelRewardMultiplier - previousStableRow.levelRewardMultiplier;
                row.questRewardMultiplierDelta = row.questRewardMultiplier - previousStableRow.questRewardMultiplier;
                row.dropChanceMultiplierDelta = row.dropChanceMultiplier - previousStableRow.dropChanceMultiplier;

                if (row.recommendedLevel < row.expectedRecommendedLevel)
                {
                    warningNotes.Add(
                        $"recommendedLevel is behind curve (expected >= {row.expectedRecommendedLevel}, actual {row.recommendedLevel}).");
                }

                if (row.recommendedPower < previousStableRow.recommendedPower)
                {
                    warningNotes.Add(
                        $"recommendedPower regressed ({previousStableRow.recommendedPower} -> {row.recommendedPower}).");
                }
                else if (row.recommendedPower == previousStableRow.recommendedPower)
                {
                    warningNotes.Add($"recommendedPower plateau detected at {row.recommendedPower}.");
                }

                if (row.baseExp < previousStableRow.baseExp)
                {
                    warningNotes.Add($"baseExp regressed ({previousStableRow.baseExp} -> {row.baseExp}).");
                }
                else if (row.baseExp == previousStableRow.baseExp)
                {
                    warningNotes.Add($"baseExp plateau detected at {row.baseExp}.");
                }

                if (row.baseCredits < previousStableRow.baseCredits)
                {
                    warningNotes.Add($"baseCredits regressed ({previousStableRow.baseCredits} -> {row.baseCredits}).");
                }

                if (row.levelRewardMultiplier + FloatEpsilon < previousStableRow.levelRewardMultiplier)
                {
                    warningNotes.Add(
                        $"levelRewardMultiplier regressed ({previousStableRow.levelRewardMultiplier:0.###} -> {row.levelRewardMultiplier:0.###}).");
                }

                if (row.questRewardMultiplier + FloatEpsilon < previousStableRow.questRewardMultiplier)
                {
                    warningNotes.Add(
                        $"questRewardMultiplier regressed ({previousStableRow.questRewardMultiplier:0.###} -> {row.questRewardMultiplier:0.###}).");
                }

                if (row.dropChanceMultiplier + FloatEpsilon < previousStableRow.dropChanceMultiplier)
                {
                    warningNotes.Add(
                        $"dropChanceMultiplier regressed ({previousStableRow.dropChanceMultiplier:0.###} -> {row.dropChanceMultiplier:0.###}).");
                }

                if (previousStableRow.recommendedPower > 0 && row.recommendedPower > previousStableRow.recommendedPower)
                {
                    float powerGrowth = (float)(row.recommendedPower - previousStableRow.recommendedPower) /
                                        previousStableRow.recommendedPower;
                    if (powerGrowth < 0.03f)
                    {
                        warningNotes.Add(
                            $"recommendedPower growth is too flat ({powerGrowth * 100f:0.0}% over previous level).");
                    }
                    else if (powerGrowth > 0.35f)
                    {
                        warningNotes.Add(
                            $"recommendedPower growth is too steep ({powerGrowth * 100f:0.0}% over previous level).");
                    }
                }

                if (previousStableRow.baseExp > 0 && row.baseExp > previousStableRow.baseExp)
                {
                    float expGrowth = (float)(row.baseExp - previousStableRow.baseExp) / previousStableRow.baseExp;
                    if (expGrowth < 0.02f)
                    {
                        warningNotes.Add($"baseExp growth is too flat ({expGrowth * 100f:0.0}% over previous level).");
                    }
                    else if (expGrowth > 0.30f)
                    {
                        warningNotes.Add($"baseExp growth is too steep ({expGrowth * 100f:0.0}% over previous level).");
                    }
                }
            }
            else
            {
                row.expectedRecommendedLevel = Mathf.Max(levelIndex, row.recommendedLevel);
            }

            if (levelIndex >= 8 && !row.overrideBossSettings)
            {
                warningNotes.Add("late-game level should enable overrideBossSettings.");
            }

            return BuildRow(row, blockingNotes, warningNotes);
        }

        private static ValidationRow BuildRow(
            ValidationRow row,
            List<string> blockingNotes,
            List<string> warningNotes)
        {
            int blockingCount = blockingNotes != null ? blockingNotes.Count : 0;
            int warningCount = warningNotes != null ? warningNotes.Count : 0;

            row.blockingErrors = blockingCount;
            row.warnings = warningCount;
            row.status = blockingCount > 0 ? "Error" : "Ok";

            var notes = new List<string>();
            if (blockingCount > 0)
            {
                notes.Add("[B] " + string.Join(" [B] ", blockingNotes));
            }

            if (warningCount > 0)
            {
                notes.Add("[W] " + string.Join(" [W] ", warningNotes));
            }

            row.note = notes.Count > 0 ? string.Join(" ", notes) : string.Empty;
            return row;
        }

        private static List<string> BuildGlobalWarnings(
            List<ValidationRow> rows,
            HashSet<int> duplicatedIndexes,
            int transitionCount,
            int increasingPowerTransitions,
            int increasingExpTransitions,
            int increasingCreditTransitions)
        {
            var warnings = new List<string>();

            if (duplicatedIndexes != null && duplicatedIndexes.Count > 0)
            {
                var ordered = new List<int>(duplicatedIndexes);
                ordered.Sort();
                warnings.Add($"duplicate LevelData assets detected for indexes: {string.Join(",", ordered)}.");
            }

            int dataRows = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].hasData)
                {
                    dataRows++;
                }
            }

            if (dataRows < (MaxLevelIndex - MinLevelIndex + 1))
            {
                warnings.Add(
                    $"expected {MaxLevelIndex - MinLevelIndex + 1} levels but only {dataRows} have LevelData.");
            }

            if (transitionCount <= 0)
            {
                warnings.Add("insufficient stable level sequence to evaluate progression transitions.");
                return warnings;
            }

            int minIncreasingTransitions = Mathf.CeilToInt(transitionCount * 0.7f);
            if (increasingPowerTransitions < minIncreasingTransitions)
            {
                warnings.Add(
                    $"recommendedPower increases only {increasingPowerTransitions}/{transitionCount} transitions.");
            }

            if (increasingExpTransitions < minIncreasingTransitions)
            {
                warnings.Add(
                    $"baseExp increases only {increasingExpTransitions}/{transitionCount} transitions.");
            }

            if (increasingCreditTransitions < minIncreasingTransitions)
            {
                warnings.Add(
                    $"baseCredits increases only {increasingCreditTransitions}/{transitionCount} transitions.");
            }

            return warnings;
        }

        private static List<LevelEntry> CollectTargetLevels()
        {
            var result = new List<LevelEntry>();
            string[] guids = AssetDatabase.FindAssets("t:LevelData");
            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrWhiteSpace(assetPath))
                {
                    continue;
                }

                LevelData levelData = AssetDatabase.LoadAssetAtPath<LevelData>(assetPath);
                if (levelData == null)
                {
                    continue;
                }

                int levelIndex = ParseLevelIndex(levelData.levelId);
                if (levelIndex < MinLevelIndex || levelIndex > MaxLevelIndex)
                {
                    continue;
                }

                result.Add(new LevelEntry
                {
                    levelIndex = levelIndex,
                    levelAssetPath = assetPath,
                    levelData = levelData
                });
            }

            result.Sort((a, b) => a.levelIndex.CompareTo(b.levelIndex));
            return result;
        }

        private static int ParseLevelIndex(string levelId)
        {
            if (string.IsNullOrWhiteSpace(levelId))
            {
                return -1;
            }

            const string prefix = "LEVEL_";
            if (!levelId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return -1;
            }

            string raw = levelId.Substring(prefix.Length);
            if (int.TryParse(raw, out int parsed))
            {
                return parsed;
            }

            return -1;
        }

        private static string WriteCsv(List<ValidationRow> rows)
        {
            string fullPath = Path.GetFullPath(ReportCsvPath);
            EnsureDirectoryExists(fullPath);

            var csv = new StringBuilder();
            csv.AppendLine(
                "level_index,level_id,level_asset,status,has_data,blocking_errors,warnings,recommended_level,expected_recommended_level,recommended_level_delta,recommended_power,recommended_power_delta,base_exp,base_exp_delta,base_credits,base_credits_delta,level_reward_multiplier,level_reward_multiplier_delta,quest_reward_multiplier,quest_reward_multiplier_delta,drop_chance_multiplier,drop_chance_multiplier_delta,boss_override,note");

            for (int i = 0; i < rows.Count; i++)
            {
                ValidationRow row = rows[i];
                csv.Append(row.levelIndex).Append(',')
                    .Append(EscapeCsv(row.levelId)).Append(',')
                    .Append(EscapeCsv(row.levelAssetPath)).Append(',')
                    .Append(EscapeCsv(row.status)).Append(',')
                    .Append(row.hasData ? "1" : "0").Append(',')
                    .Append(row.blockingErrors).Append(',')
                    .Append(row.warnings).Append(',')
                    .Append(row.recommendedLevel).Append(',')
                    .Append(row.expectedRecommendedLevel).Append(',')
                    .Append(row.recommendedLevelDelta).Append(',')
                    .Append(row.recommendedPower).Append(',')
                    .Append(row.recommendedPowerDelta).Append(',')
                    .Append(row.baseExp).Append(',')
                    .Append(row.baseExpDelta).Append(',')
                    .Append(row.baseCredits).Append(',')
                    .Append(row.baseCreditsDelta).Append(',')
                    .Append(row.levelRewardMultiplier.ToString("0.###")).Append(',')
                    .Append(row.levelRewardMultiplierDelta.ToString("0.###")).Append(',')
                    .Append(row.questRewardMultiplier.ToString("0.###")).Append(',')
                    .Append(row.questRewardMultiplierDelta.ToString("0.###")).Append(',')
                    .Append(row.dropChanceMultiplier.ToString("0.###")).Append(',')
                    .Append(row.dropChanceMultiplierDelta.ToString("0.###")).Append(',')
                    .Append(row.overrideBossSettings ? "1" : "0").Append(',')
                    .Append(EscapeCsv(row.note))
                    .AppendLine();
            }

            File.WriteAllText(fullPath, csv.ToString(), new UTF8Encoding(false));
            return ReportCsvPath;
        }

        private static string WriteSummary(
            List<ValidationRow> rows,
            int errorRows,
            int warningTotal,
            List<string> globalWarnings,
            int transitionCount,
            int increasingPowerTransitions,
            int increasingExpTransitions,
            int increasingCreditTransitions)
        {
            string fullPath = Path.GetFullPath(SummaryMdPath);
            EnsureDirectoryExists(fullPath);

            var md = new StringBuilder();
            md.AppendLine("# Level Progression Curve Consistency Summary");
            md.AppendLine();
            md.AppendLine($"- Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss zzz}");
            md.AppendLine($"- Targets: {rows.Count}");
            md.AppendLine($"- Error Rows: {errorRows}");
            md.AppendLine($"- Warning Count: {warningTotal}");
            md.AppendLine($"- Stable Transitions: {transitionCount}");
            md.AppendLine($"- Power Increases: {increasingPowerTransitions}/{transitionCount}");
            md.AppendLine($"- Exp Increases: {increasingExpTransitions}/{transitionCount}");
            md.AppendLine($"- Credit Increases: {increasingCreditTransitions}/{transitionCount}");
            md.AppendLine($"- CSV: {ReportCsvPath}");

            if (globalWarnings != null && globalWarnings.Count > 0)
            {
                md.AppendLine();
                md.AppendLine("## Global Warnings");
                for (int i = 0; i < globalWarnings.Count; i++)
                {
                    md.AppendLine($"- {globalWarnings[i]}");
                }
            }

            md.AppendLine();
            md.AppendLine("| Level | Status | Warnings | RecLv | RecPower | BaseExp | BaseCredits | LvMul | QuestMul | DropMul | Boss | Note |");
            md.AppendLine("|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|");
            for (int i = 0; i < rows.Count; i++)
            {
                ValidationRow row = rows[i];
                md.Append("| LEVEL_")
                    .Append(row.levelIndex.ToString("D2")).Append(" | ")
                    .Append(SafeMarkdownCell(row.status)).Append(" | ")
                    .Append(row.warnings).Append(" | ")
                    .Append(row.recommendedLevel).Append(" | ")
                    .Append(row.recommendedPower).Append(" | ")
                    .Append(row.baseExp).Append(" | ")
                    .Append(row.baseCredits).Append(" | ")
                    .Append(row.levelRewardMultiplier.ToString("0.###")).Append(" | ")
                    .Append(row.questRewardMultiplier.ToString("0.###")).Append(" | ")
                    .Append(row.dropChanceMultiplier.ToString("0.###")).Append(" | ")
                    .Append(row.overrideBossSettings ? "Y" : "N").Append(" | ")
                    .Append(SafeMarkdownCell(TrimForMarkdownTable(row.note, 160))).Append(" |")
                    .AppendLine();
            }

            File.WriteAllText(fullPath, md.ToString(), new UTF8Encoding(false));
            return SummaryMdPath;
        }

        private static string EscapeCsv(string value)
        {
            string safe = value ?? string.Empty;
            bool needsQuotes = safe.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0;
            if (!needsQuotes)
            {
                return safe;
            }

            return "\"" + safe.Replace("\"", "\"\"") + "\"";
        }

        private static void EnsureDirectoryExists(string fullPath)
        {
            string dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        private static string SafeMarkdownCell(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
        }

        private static string TrimForMarkdownTable(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return value ?? string.Empty;
            }

            return value.Substring(0, Math.Max(0, maxLength - 3)) + "...";
        }

        private struct LevelEntry
        {
            public int levelIndex;
            public string levelAssetPath;
            public LevelData levelData;
        }

        private struct ValidationRow
        {
            public int levelIndex;
            public string levelId;
            public string levelAssetPath;
            public string status;
            public bool hasData;
            public int blockingErrors;
            public int warnings;
            public int recommendedLevel;
            public int expectedRecommendedLevel;
            public int recommendedLevelDelta;
            public int recommendedPower;
            public int recommendedPowerDelta;
            public int baseExp;
            public int baseExpDelta;
            public int baseCredits;
            public int baseCreditsDelta;
            public float levelRewardMultiplier;
            public float levelRewardMultiplierDelta;
            public float questRewardMultiplier;
            public float questRewardMultiplierDelta;
            public float dropChanceMultiplier;
            public float dropChanceMultiplierDelta;
            public bool overrideBossSettings;
            public string note;
        }
    }
}
