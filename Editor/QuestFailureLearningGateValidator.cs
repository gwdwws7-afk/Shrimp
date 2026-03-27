using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ThirdPersonController;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonController.Editor
{
    public static class QuestFailureLearningGateValidator
    {
        private const string ValidateMenuPath = "Tools/Quest/P1/Validate Failure Learning Coverage (CSV)";
        private const string ValidateGateMenuPath = "Tools/Quest/P1/Validate Failure Learning Coverage (CI Gate)";
        private const string ReportCsvPath = "Assets/ThirdPersonController/Reports/quest_failure_learning_gate_report.csv";
        private const string SummaryMdPath = "Assets/ThirdPersonController/Reports/quest_failure_learning_gate_summary.md";
        private const string LogPrefix = "[QuestFailureLearningGate]";

        private static readonly string[] PlaceholderTokens =
        {
            "todo",
            "tbd",
            "placeholder",
            "example",
            "sample",
            "xxx",
            "??",
            "占位",
            "示例",
            "待补"
        };

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

        private static void Run(bool interactive, bool failOnError)
        {
            List<QuestDatabase> databases = CollectQuestDatabases();
            if (databases.Count == 0)
            {
                string noneMessage = $"{LogPrefix} no QuestDatabase assets found.";
                Debug.LogWarning(noneMessage);
                if (interactive)
                {
                    EditorUtility.DisplayDialog("Quest Failure Learning Gate", noneMessage, "OK");
                }

                return;
            }

            var rows = new List<ValidationRow>(128);
            var questIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int stageTimedRules = 0;
            int questsWithFailureRule = 0;
            int questsWithLearningText = 0;
            int questsWithFollowup = 0;

            for (int dbIndex = 0; dbIndex < databases.Count; dbIndex++)
            {
                QuestDatabase database = databases[dbIndex];
                string databasePath = AssetDatabase.GetAssetPath(database);
                if (database == null)
                {
                    rows.Add(new ValidationRow
                    {
                        databaseAssetPath = databasePath ?? string.Empty,
                        questId = string.Empty,
                        questName = string.Empty,
                        status = "Error",
                        blockingErrors = 1,
                        warnings = 0,
                        stageCount = 0,
                        hasFailureRule = 0,
                        hasLearningText = 0,
                        hasFollowup = 0,
                        rewardTier = string.Empty,
                        note = "QuestDatabase is null."
                    });
                    continue;
                }

                List<QuestData> quests = database.quests ?? new List<QuestData>();
                if (quests.Count == 0)
                {
                    rows.Add(new ValidationRow
                    {
                        databaseAssetPath = databasePath,
                        questId = string.Empty,
                        questName = database.name,
                        status = "Error",
                        blockingErrors = 1,
                        warnings = 0,
                        stageCount = 0,
                        hasFailureRule = 0,
                        hasLearningText = 0,
                        hasFollowup = 0,
                        rewardTier = string.Empty,
                        note = "QuestDatabase has no quest entries."
                    });
                    continue;
                }

                for (int i = 0; i < quests.Count; i++)
                {
                    ValidationRow row = ProcessQuest(
                        quests[i],
                        databasePath,
                        questIds,
                        ref stageTimedRules,
                        ref questsWithFailureRule,
                        ref questsWithLearningText,
                        ref questsWithFollowup);
                    rows.Add(row);
                }
            }

            int errorRows = 0;
            int warningTotal = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                if (string.Equals(rows[i].status, "Error", StringComparison.Ordinal))
                {
                    errorRows++;
                }

                warningTotal += rows[i].warnings;
            }

            string csvPath = WriteCsv(rows);
            string summaryPath = WriteSummary(
                rows,
                errorRows,
                warningTotal,
                stageTimedRules,
                questsWithFailureRule,
                questsWithLearningText,
                questsWithFollowup);
            AssetDatabase.Refresh();

            string summary =
                $"rows={rows.Count} errors={errorRows} warnings={warningTotal} csv={csvPath} summary={summaryPath}";
            Debug.Log($"{LogPrefix} complete | {summary}");

            if (interactive)
            {
                EditorUtility.DisplayDialog("Quest Failure Learning Gate", summary, "OK");
            }

            if (failOnError && errorRows > 0)
            {
                throw new InvalidOperationException($"{LogPrefix} gate failed. errors={errorRows} csv={csvPath}");
            }
        }

        private static ValidationRow ProcessQuest(
            QuestData quest,
            string databasePath,
            HashSet<string> questIds,
            ref int stageTimedRules,
            ref int questsWithFailureRule,
            ref int questsWithLearningText,
            ref int questsWithFollowup)
        {
            var row = new ValidationRow
            {
                databaseAssetPath = databasePath ?? string.Empty,
                questId = quest != null ? quest.questId : string.Empty,
                questName = quest != null ? quest.questName : string.Empty,
                status = "Error",
                blockingErrors = 0,
                warnings = 0,
                stageCount = 0,
                hasFailureRule = 0,
                hasLearningText = 0,
                hasFollowup = 0,
                rewardTier = quest != null ? quest.rewardTier.ToString() : string.Empty,
                note = string.Empty
            };

            var blockingNotes = new List<string>();
            var warningNotes = new List<string>();

            if (quest == null)
            {
                blockingNotes.Add("QuestData is null.");
                return BuildRow(row, blockingNotes, warningNotes);
            }

            if (string.IsNullOrWhiteSpace(quest.questId))
            {
                blockingNotes.Add("questId is empty.");
            }
            else if (!questIds.Add(quest.questId))
            {
                blockingNotes.Add($"Duplicate questId '{quest.questId}'.");
            }

            if (string.IsNullOrWhiteSpace(quest.questName))
            {
                warningNotes.Add("questName is empty.");
            }

            bool hasLearningText = IsMeaningfulText(quest.description);
            bool hasFollowup = HasFollowup(quest.nextQuestIds);
            bool hasFailureRule = HasQuestFailureRule(quest);

            List<QuestStage> stages = quest.stages ?? new List<QuestStage>();
            row.stageCount = stages.Count;

            int stageRules = 0;
            var stageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < stages.Count; i++)
            {
                QuestStage stage = stages[i];
                if (stage == null)
                {
                    warningNotes.Add($"Stage[{i}] is null.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(stage.stageId))
                {
                    warningNotes.Add($"Stage[{i}] stageId is empty.");
                }
                else if (!stageIds.Add(stage.stageId))
                {
                    warningNotes.Add($"Duplicate stageId '{stage.stageId}'.");
                }

                if (stage.useTimeLimit)
                {
                    stageRules++;
                    if (stage.timeLimit <= 0f)
                    {
                        blockingNotes.Add($"Stage '{SafeStageLabel(stage, i)}' useTimeLimit=true but timeLimit<=0.");
                    }
                }

                if (!IsMeaningfulText(stage.description))
                {
                    warningNotes.Add($"Stage '{SafeStageLabel(stage, i)}' description is empty/placeholder.");
                }
                else
                {
                    hasLearningText = true;
                }

                bool countDrivenType =
                    stage.questType != QuestType.Survive &&
                    stage.questType != QuestType.Protect &&
                    stage.questType != QuestType.Reach;
                if (countDrivenType && stage.targetCount <= 0)
                {
                    warningNotes.Add($"Stage '{SafeStageLabel(stage, i)}' targetCount<=0 for count-driven quest type.");
                }
            }

            if (stageRules > 0)
            {
                stageTimedRules += stageRules;
            }

            if (!hasFailureRule && stageRules > 0)
            {
                hasFailureRule = true;
            }

            if (quest.timeLimit < 0f)
            {
                blockingNotes.Add("timeLimit must be >= 0.");
            }

            if (quest.reward == null)
            {
                warningNotes.Add("reward is null.");
            }
            else if (quest.reward.exp < 0 || quest.reward.pearls < 0 || quest.reward.credits < 0)
            {
                blockingNotes.Add("reward values contain negative numbers.");
            }

            if (hasFailureRule)
            {
                row.hasFailureRule = 1;
                questsWithFailureRule++;
            }
            else if (!quest.isOptional && quest.rewardTier == QuestRewardTier.Mainline)
            {
                warningNotes.Add("Mainline quest has no explicit failure rule.");
            }

            if (hasLearningText)
            {
                row.hasLearningText = 1;
                questsWithLearningText++;
            }
            else
            {
                warningNotes.Add("Quest description is empty/placeholder.");
            }

            if (hasFollowup)
            {
                row.hasFollowup = 1;
                questsWithFollowup++;
            }
            else if (!quest.isOptional &&
                     quest.rewardTier == QuestRewardTier.Mainline &&
                     quest.autoStartNextQuests &&
                     !IsFlowDrivenMainlineQuest(quest))
            {
                warningNotes.Add("No follow-up quest linkage configured.");
            }

            if (hasFailureRule && !hasLearningText)
            {
                warningNotes.Add("Has failure rule but no usable learning text.");
            }

            if (quest.failOnDefenseTargetDestroyed && !HasProtectDefenseObjective(quest, stages))
            {
                warningNotes.Add("failOnDefenseTargetDestroyed is true but no Protect/ProtectTarget objective is configured.");
            }

            if (stages.Count == 0)
            {
                bool timerDriven = quest.questType == QuestType.Survive || quest.questType == QuestType.Protect;
                if (!timerDriven && quest.targetCount <= 0)
                {
                    warningNotes.Add("Single-stage quest targetCount<=0.");
                }
            }

            return BuildRow(row, blockingNotes, warningNotes);
        }

        private static bool HasQuestFailureRule(QuestData quest)
        {
            if (quest == null)
            {
                return false;
            }

            if (quest.timeLimit > 0f ||
                quest.failOnPlayerDeath ||
                quest.failOnGameOver ||
                quest.failOnDefenseTargetDestroyed)
            {
                return true;
            }

            if (quest.stages == null || quest.stages.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < quest.stages.Count; i++)
            {
                QuestStage stage = quest.stages[i];
                if (stage != null && stage.useTimeLimit && stage.timeLimit > 0f)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsMeaningfulText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string trimmed = text.Trim();
            if (trimmed.Length < 8)
            {
                return false;
            }

            string lowered = trimmed.ToLowerInvariant();
            for (int i = 0; i < PlaceholderTokens.Length; i++)
            {
                if (lowered.Contains(PlaceholderTokens[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasFollowup(List<string> nextQuestIds)
        {
            if (nextQuestIds == null || nextQuestIds.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < nextQuestIds.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(nextQuestIds[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsFlowDrivenMainlineQuest(QuestData quest)
        {
            if (quest == null || quest.rewardTier != QuestRewardTier.Mainline || string.IsNullOrWhiteSpace(quest.questId))
            {
                return false;
            }

            string id = quest.questId.Trim();
            if (id.Length < 4)
            {
                return false;
            }

            return (id[0] == 'l' || id[0] == 'L') &&
                   char.IsDigit(id[1]) &&
                   char.IsDigit(id[2]) &&
                   id[3] == '_';
        }

        private static bool HasProtectDefenseObjective(QuestData quest, List<QuestStage> stages)
        {
            if (quest != null)
            {
                if (quest.questType == QuestType.Protect)
                {
                    return true;
                }

                if (quest.questType == QuestType.CompleteWaveEvent &&
                    !quest.matchAnyWaveEventType &&
                    quest.targetWaveEventType == WaveEventType.ProtectTarget)
                {
                    return true;
                }
            }

            if (stages == null || stages.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < stages.Count; i++)
            {
                QuestStage stage = stages[i];
                if (stage == null)
                {
                    continue;
                }

                if (stage.questType == QuestType.Protect)
                {
                    return true;
                }

                if (stage.questType == QuestType.CompleteWaveEvent &&
                    !stage.matchAnyWaveEventType &&
                    stage.targetWaveEventType == WaveEventType.ProtectTarget)
                {
                    return true;
                }
            }

            return false;
        }

        private static string SafeStageLabel(QuestStage stage, int index)
        {
            if (stage == null)
            {
                return $"Stage[{index}]";
            }

            if (!string.IsNullOrWhiteSpace(stage.stageId))
            {
                return stage.stageId;
            }

            if (!string.IsNullOrWhiteSpace(stage.title))
            {
                return stage.title;
            }

            return $"Stage[{index}]";
        }

        private static ValidationRow BuildRow(ValidationRow row, List<string> blockingNotes, List<string> warningNotes)
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

        private static List<QuestDatabase> CollectQuestDatabases()
        {
            var result = new List<QuestDatabase>();
            string[] guids = AssetDatabase.FindAssets("t:QuestDatabase");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                QuestDatabase db = AssetDatabase.LoadAssetAtPath<QuestDatabase>(path);
                if (db != null)
                {
                    result.Add(db);
                }
            }

            result.Sort((a, b) =>
            {
                string pa = a != null ? AssetDatabase.GetAssetPath(a) : string.Empty;
                string pb = b != null ? AssetDatabase.GetAssetPath(b) : string.Empty;
                return string.Compare(pa, pb, StringComparison.OrdinalIgnoreCase);
            });
            return result;
        }

        private static string WriteCsv(List<ValidationRow> rows)
        {
            string fullPath = Path.GetFullPath(ReportCsvPath);
            EnsureDirectoryExists(fullPath);

            var csv = new StringBuilder();
            csv.AppendLine(
                "database_asset,quest_id,quest_name,reward_tier,status,blocking_errors,warnings,stage_count,has_failure_rule,has_learning_text,has_followup,note");
            for (int i = 0; i < rows.Count; i++)
            {
                ValidationRow row = rows[i];
                csv.Append(EscapeCsv(row.databaseAssetPath)).Append(',')
                    .Append(EscapeCsv(row.questId)).Append(',')
                    .Append(EscapeCsv(row.questName)).Append(',')
                    .Append(EscapeCsv(row.rewardTier)).Append(',')
                    .Append(EscapeCsv(row.status)).Append(',')
                    .Append(row.blockingErrors).Append(',')
                    .Append(row.warnings).Append(',')
                    .Append(row.stageCount).Append(',')
                    .Append(row.hasFailureRule).Append(',')
                    .Append(row.hasLearningText).Append(',')
                    .Append(row.hasFollowup).Append(',')
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
            int stageTimedRules,
            int questsWithFailureRule,
            int questsWithLearningText,
            int questsWithFollowup)
        {
            string fullPath = Path.GetFullPath(SummaryMdPath);
            EnsureDirectoryExists(fullPath);

            int totalRows = rows.Count;
            float failureCoverage = totalRows > 0 ? (float)questsWithFailureRule / totalRows : 0f;
            float learningCoverage = totalRows > 0 ? (float)questsWithLearningText / totalRows : 0f;
            float followupCoverage = totalRows > 0 ? (float)questsWithFollowup / totalRows : 0f;

            var md = new StringBuilder();
            md.AppendLine("# Quest Failure Learning Gate Summary");
            md.AppendLine();
            md.AppendLine($"- Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss zzz}");
            md.AppendLine($"- Rows: {totalRows}");
            md.AppendLine($"- Error Rows: {errorRows}");
            md.AppendLine($"- Warning Count: {warningTotal}");
            md.AppendLine($"- Quest Failure Coverage: {(failureCoverage * 100f):0.0}%");
            md.AppendLine($"- Learning Text Coverage: {(learningCoverage * 100f):0.0}%");
            md.AppendLine($"- Follow-up Link Coverage: {(followupCoverage * 100f):0.0}%");
            md.AppendLine($"- Stage Timed Rule Count: {stageTimedRules}");
            md.AppendLine($"- CSV: {ReportCsvPath}");
            md.AppendLine();
            md.AppendLine("| Quest ID | Name | Tier | Status | Warnings | Failure Rule | Learning Text | Follow-up | Note |");
            md.AppendLine("|---|---|---|---|---:|---:|---:|---:|---|");

            for (int i = 0; i < rows.Count; i++)
            {
                ValidationRow row = rows[i];
                md.Append("| ")
                    .Append(SafeMarkdownCell(row.questId)).Append(" | ")
                    .Append(SafeMarkdownCell(row.questName)).Append(" | ")
                    .Append(SafeMarkdownCell(row.rewardTier)).Append(" | ")
                    .Append(SafeMarkdownCell(row.status)).Append(" | ")
                    .Append(row.warnings).Append(" | ")
                    .Append(row.hasFailureRule).Append(" | ")
                    .Append(row.hasLearningText).Append(" | ")
                    .Append(row.hasFollowup).Append(" | ")
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

        private struct ValidationRow
        {
            public string databaseAssetPath;
            public string questId;
            public string questName;
            public string rewardTier;
            public string status;
            public int blockingErrors;
            public int warnings;
            public int stageCount;
            public int hasFailureRule;
            public int hasLearningText;
            public int hasFollowup;
            public string note;
        }
    }
}
