using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ThirdPersonController;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonController.Editor
{
    public static class LevelQuestBeatLinkageGateValidator
    {
        private const string ValidateMenuPath = "Tools/Level/P1/Validate Level Quest Beat Linkage (CSV)";
        private const string ValidateGateMenuPath = "Tools/Level/P1/Validate Level Quest Beat Linkage (CI Gate)";
        private const string SceneFolderPath = "Assets/Scenes";
        private const string ReportCsvPath = "Assets/ThirdPersonController/Reports/level_quest_beat_linkage_report.csv";
        private const string SummaryMdPath = "Assets/ThirdPersonController/Reports/level_quest_beat_linkage_summary.md";
        private const string LogPrefix = "[LevelQuestBeatLinkage]";
        private const int MinLevelIndex = 2;
        private const int MaxLevelIndex = 10;

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
            List<LevelEntry> entries = CollectTargetLevels();
            if (entries.Count == 0)
            {
                string noneMessage =
                    $"{LogPrefix} no LevelData assets found for LEVEL_{MinLevelIndex:D2}~LEVEL_{MaxLevelIndex:D2}.";
                Debug.LogWarning(noneMessage);
                if (interactive)
                {
                    EditorUtility.DisplayDialog("Level Quest Beat Linkage", noneMessage, "OK");
                }

                return;
            }

            QuestLookupContext questLookup = BuildQuestLookup();
            var rows = new List<ValidationRow>(entries.Count);
            for (int i = 0; i < entries.Count; i++)
            {
                rows.Add(ProcessEntry(entries[i], questLookup));
            }

            List<string> globalWarnings = BuildGlobalWarnings(entries, questLookup, rows);

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
            string summaryPath = WriteSummary(rows, errorRows, warningTotal, globalWarnings, questLookup);
            AssetDatabase.Refresh();

            string summary =
                $"targets={rows.Count} errors={errorRows} warnings={warningTotal} csv={csvPath} summary={summaryPath}";
            Debug.Log($"{LogPrefix} complete | {summary}");

            if (interactive)
            {
                EditorUtility.DisplayDialog("Level Quest Beat Linkage", summary, "OK");
            }

            if (failOnError && errorRows > 0)
            {
                throw new InvalidOperationException($"{LogPrefix} gate failed. errors={errorRows} csv={csvPath}");
            }
        }

        private static ValidationRow ProcessEntry(LevelEntry entry, QuestLookupContext questLookup)
        {
            var row = new ValidationRow
            {
                levelIndex = entry.levelIndex,
                levelId = entry.levelData != null ? entry.levelData.levelId : string.Empty,
                levelAssetPath = entry.levelAssetPath ?? string.Empty,
                sceneName = entry.levelData != null ? entry.levelData.sceneName : string.Empty,
                scenePath = BuildScenePath(entry.levelData),
                nextLevelId = entry.levelData != null ? entry.levelData.nextLevelId : string.Empty,
                status = "Error",
                note = string.Empty
            };

            var blockingNotes = new List<string>();
            var warningNotes = new List<string>();

            if (entry.levelData == null)
            {
                blockingNotes.Add("LevelData asset is null.");
                return BuildRow(row, blockingNotes, warningNotes);
            }

            if (string.IsNullOrWhiteSpace(row.scenePath))
            {
                blockingNotes.Add("LevelData.sceneName is empty.");
            }
            else if (!AssetExists(row.scenePath))
            {
                blockingNotes.Add("Scene asset is missing.");
            }

            if (entry.levelIndex < MaxLevelIndex)
            {
                int expectedNext = entry.levelIndex + 1;
                int parsedNext = ParseLevelIndex(entry.levelData.nextLevelId);
                row.nextLevelIndex = parsedNext;
                if (parsedNext != expectedNext)
                {
                    blockingNotes.Add(
                        $"nextLevelId mismatch (expected LEVEL_{expectedNext:D2}, actual '{entry.levelData.nextLevelId}').");
                }
            }
            else if (!string.IsNullOrWhiteSpace(entry.levelData.nextLevelId))
            {
                warningNotes.Add("Final level should normally leave nextLevelId empty.");
            }

            List<QuestConfig> levelQuestConfigs = entry.levelData.quests ?? new List<QuestConfig>();
            row.questConfigCount = levelQuestConfigs.Count;
            if (levelQuestConfigs.Count == 0)
            {
                blockingNotes.Add("LevelData.quests is empty.");
            }

            HashSet<string> levelStrongholdIds = CollectLevelStrongholdIds(entry.levelData);
            row.strongholdCount = levelStrongholdIds.Count;

            HashSet<WaveEventType> levelEventTypes =
                CollectLevelEventTypes(entry.levelData, out int overrideWaveCount, out int overrideEventCount);
            row.overrideWaveCount = overrideWaveCount;
            row.overrideEventCount = overrideEventCount;
            row.eventTypeCount = levelEventTypes.Count;
            row.eventTypes = JoinEventTypes(levelEventTypes);

            if (overrideEventCount <= 0)
            {
                warningNotes.Add("No wave events configured in strongholdOverrides.");
            }

            string expectedQuestPrefix = $"l{entry.levelIndex:D2}_";
            var localQuestIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < levelQuestConfigs.Count; i++)
            {
                QuestConfig config = levelQuestConfigs[i];
                if (config == null)
                {
                    blockingNotes.Add($"LevelData.quests[{i}] is null.");
                    continue;
                }

                bool isRequired = config.required;
                if (isRequired)
                {
                    row.requiredQuestCount++;
                }

                string questId = config.questId != null ? config.questId.Trim() : string.Empty;
                if (string.IsNullOrWhiteSpace(questId))
                {
                    blockingNotes.Add($"LevelData.quests[{i}] has empty questId.");
                    continue;
                }

                if (!localQuestIds.Add(questId))
                {
                    warningNotes.Add($"Duplicate questId '{questId}' in LevelData.quests.");
                }

                if (!questLookup.questById.TryGetValue(questId, out QuestLookup questRef) || questRef.quest == null)
                {
                    if (isRequired)
                    {
                        row.requiredQuestMissing++;
                    }

                    blockingNotes.Add($"Quest '{questId}' not found in QuestDatabase assets.");
                    continue;
                }

                if (!isRequired)
                {
                    continue;
                }

                row.requiredQuestResolved++;
                EvaluateRequiredQuest(
                    entry.levelIndex,
                    questId,
                    questRef,
                    expectedQuestPrefix,
                    levelStrongholdIds,
                    levelEventTypes,
                    row,
                    blockingNotes,
                    warningNotes);
            }

            if (row.requiredQuestCount <= 0)
            {
                blockingNotes.Add("No required quest configured for level.");
            }
            else if (row.requiredQuestResolved < row.requiredQuestCount)
            {
                blockingNotes.Add(
                    $"Required quest resolution incomplete ({row.requiredQuestResolved}/{row.requiredQuestCount}).");
            }

            if (row.requiredQuestResolved > 0 && row.requiredBeatObjectiveCount <= 0)
            {
                warningNotes.Add("Required quest chain has no structural beat objective.");
            }

            if (row.requiredQuestResolved > 0 && row.eventTypeCount > 0 && row.requiredEventObjectiveCount <= 0)
            {
                warningNotes.Add("Level has wave events but required quests do not include CompleteWaveEvent objective.");
            }

            if (entry.levelData.overrideBossSettings)
            {
                if (entry.levelIndex >= 8 && row.requiredBossObjectiveCount <= 0)
                {
                    blockingNotes.Add("Late-game boss level has no required BossBreak/BossDefeat objective.");
                }
                else if (row.requiredBossObjectiveCount <= 0)
                {
                    warningNotes.Add("Boss level has no required BossBreak/BossDefeat objective.");
                }
            }

            return BuildRow(row, blockingNotes, warningNotes);
        }

        private static void EvaluateRequiredQuest(
            int levelIndex,
            string questId,
            QuestLookup questRef,
            string expectedQuestPrefix,
            HashSet<string> levelStrongholdIds,
            HashSet<WaveEventType> levelEventTypes,
            ValidationRow row,
            List<string> blockingNotes,
            List<string> warningNotes)
        {
            QuestData quest = questRef.quest;
            if (quest == null)
            {
                blockingNotes.Add($"Quest '{questId}' data is null.");
                return;
            }

            if (!questId.StartsWith(expectedQuestPrefix, StringComparison.OrdinalIgnoreCase))
            {
                warningNotes.Add(
                    $"Required quest '{questId}' does not match level prefix '{expectedQuestPrefix}'.");
            }

            if (quest.isOptional)
            {
                warningNotes.Add($"Required quest '{questId}' is marked optional in QuestDatabase.");
            }

            if (quest.rewardTier != QuestRewardTier.Mainline)
            {
                warningNotes.Add(
                    $"Required quest '{questId}' should use Mainline reward tier (actual {quest.rewardTier}).");
            }

            bool hasStages = quest.stages != null && quest.stages.Count > 0;
            bool hasBeatObjective = IsBeatObjective(quest.questType);
            bool hasEventObjective = IsWaveEventObjective(quest.questType);
            bool hasBossObjective = IsBossObjective(quest.questType);

            // For staged quests, objective wiring is validated on each stage.
            if (!hasStages)
            {
                ValidateObjectiveBinding(
                    questId,
                    "quest",
                    quest.questType,
                    quest.targetStrongholdId,
                    quest.targetBossId,
                    quest.matchAnyWaveEventType,
                    quest.targetWaveEventType,
                    levelStrongholdIds,
                    levelEventTypes,
                    warningNotes);
            }

            List<QuestStage> stages = quest.stages ?? new List<QuestStage>();
            row.requiredStageCount += stages.Count;
            for (int i = 0; i < stages.Count; i++)
            {
                QuestStage stage = stages[i];
                if (stage == null)
                {
                    warningNotes.Add($"Required quest '{questId}' stage[{i}] is null.");
                    continue;
                }

                string stageLabel = SafeStageLabel(stage, i);

                if (stage.useTimeLimit && stage.timeLimit <= 0f)
                {
                    blockingNotes.Add(
                        $"Required quest '{questId}' stage '{stageLabel}' has useTimeLimit=true but timeLimit<=0.");
                }

                if (string.IsNullOrWhiteSpace(stage.title))
                {
                    warningNotes.Add($"Required quest '{questId}' stage '{stageLabel}' title is empty.");
                }

                if (stage.questType == QuestType.CompleteWaveEvent &&
                    !stage.matchAnyWaveEventType &&
                    !levelEventTypes.Contains(stage.targetWaveEventType))
                {
                    warningNotes.Add(
                        $"Required quest '{questId}' stage '{stageLabel}' targets event type {stage.targetWaveEventType} not present in level overrides.");
                }

                if (stage.questType == QuestType.CompleteStronghold &&
                    string.IsNullOrWhiteSpace(stage.targetStrongholdId))
                {
                    warningNotes.Add(
                        $"Required quest '{questId}' stage '{stageLabel}' is CompleteStronghold but targetStrongholdId is empty.");
                }

                if (!string.IsNullOrWhiteSpace(stage.targetStrongholdId) &&
                    levelStrongholdIds.Count > 0 &&
                    !levelStrongholdIds.Contains(stage.targetStrongholdId))
                {
                    warningNotes.Add(
                        $"Required quest '{questId}' stage '{stageLabel}' references stronghold '{stage.targetStrongholdId}' not listed in LevelData.");
                }

                ValidateObjectiveBinding(
                    questId,
                    $"stage '{stageLabel}'",
                    stage.questType,
                    stage.targetStrongholdId,
                    stage.targetBossId,
                    stage.matchAnyWaveEventType,
                    stage.targetWaveEventType,
                    levelStrongholdIds,
                    levelEventTypes,
                    warningNotes);

                if (IsBeatObjective(stage.questType))
                {
                    hasBeatObjective = true;
                }

                if (IsWaveEventObjective(stage.questType))
                {
                    hasEventObjective = true;
                }

                if (IsBossObjective(stage.questType))
                {
                    hasBossObjective = true;
                }
            }

            if (hasBeatObjective)
            {
                row.requiredBeatObjectiveCount++;
            }

            if (hasEventObjective)
            {
                row.requiredEventObjectiveCount++;
            }

            if (hasBossObjective)
            {
                row.requiredBossObjectiveCount++;
            }

            if (levelIndex >= 8 &&
                hasStages &&
                !hasBossObjective &&
                questId.StartsWith($"l{levelIndex:D2}_", StringComparison.OrdinalIgnoreCase))
            {
                warningNotes.Add($"Required late-game quest '{questId}' has no boss stage objective.");
            }
        }

        private static void ValidateObjectiveBinding(
            string questId,
            string objectiveLabel,
            QuestType objectiveType,
            string targetStrongholdId,
            string targetBossId,
            bool matchAnyWaveEventType,
            WaveEventType targetWaveEventType,
            HashSet<string> levelStrongholdIds,
            HashSet<WaveEventType> levelEventTypes,
            List<string> warningNotes)
        {
            if (objectiveType == QuestType.CompleteStronghold &&
                !string.IsNullOrWhiteSpace(targetStrongholdId) &&
                levelStrongholdIds.Count > 0 &&
                !levelStrongholdIds.Contains(targetStrongholdId))
            {
                warningNotes.Add(
                    $"Required quest '{questId}' {objectiveLabel} references unknown stronghold '{targetStrongholdId}'.");
            }

            if (objectiveType == QuestType.CompleteWaveEvent &&
                !matchAnyWaveEventType &&
                !levelEventTypes.Contains(targetWaveEventType))
            {
                warningNotes.Add(
                    $"Required quest '{questId}' {objectiveLabel} targets event type {targetWaveEventType} not present in level overrides.");
            }

            if (IsBossObjective(objectiveType) && string.IsNullOrWhiteSpace(targetBossId))
            {
                warningNotes.Add($"Required quest '{questId}' {objectiveLabel} is boss objective but targetBossId is empty.");
            }
        }

        private static bool IsBeatObjective(QuestType type)
        {
            return type == QuestType.CompleteWave ||
                   type == QuestType.CompleteStronghold ||
                   type == QuestType.CompleteWaveEvent ||
                   type == QuestType.BossBreak ||
                   type == QuestType.BossDefeat ||
                   type == QuestType.Protect ||
                   type == QuestType.Survive;
        }

        private static bool IsWaveEventObjective(QuestType type)
        {
            return type == QuestType.CompleteWaveEvent;
        }

        private static bool IsBossObjective(QuestType type)
        {
            return type == QuestType.BossBreak || type == QuestType.BossDefeat;
        }

        private static HashSet<string> CollectLevelStrongholdIds(LevelData levelData)
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (levelData == null || levelData.strongholds == null)
            {
                return ids;
            }

            for (int i = 0; i < levelData.strongholds.Count; i++)
            {
                StrongholdConfig config = levelData.strongholds[i];
                if (config == null || string.IsNullOrWhiteSpace(config.strongholdId))
                {
                    continue;
                }

                ids.Add(config.strongholdId.Trim());
            }

            return ids;
        }

        private static HashSet<WaveEventType> CollectLevelEventTypes(
            LevelData levelData,
            out int overrideWaveCount,
            out int overrideEventCount)
        {
            overrideWaveCount = 0;
            overrideEventCount = 0;
            var set = new HashSet<WaveEventType>();
            if (levelData == null || levelData.strongholdOverrides == null)
            {
                return set;
            }

            for (int i = 0; i < levelData.strongholdOverrides.Count; i++)
            {
                StrongholdOverride strongholdOverride = levelData.strongholdOverrides[i];
                if (strongholdOverride == null || strongholdOverride.waves == null)
                {
                    continue;
                }

                for (int waveIndex = 0; waveIndex < strongholdOverride.waves.Count; waveIndex++)
                {
                    StrongholdWaveOverride wave = strongholdOverride.waves[waveIndex];
                    if (wave == null)
                    {
                        continue;
                    }

                    overrideWaveCount++;
                    List<WaveEventOverride> events = wave.events ?? new List<WaveEventOverride>();
                    for (int eventIndex = 0; eventIndex < events.Count; eventIndex++)
                    {
                        WaveEventOverride evt = events[eventIndex];
                        if (evt == null)
                        {
                            continue;
                        }

                        overrideEventCount++;
                        set.Add(evt.eventType);
                    }
                }
            }

            return set;
        }

        private static string JoinEventTypes(HashSet<WaveEventType> eventTypes)
        {
            if (eventTypes == null || eventTypes.Count == 0)
            {
                return string.Empty;
            }

            var ordered = new List<string>(eventTypes.Count);
            foreach (WaveEventType type in eventTypes)
            {
                ordered.Add(type.ToString());
            }

            ordered.Sort(StringComparer.Ordinal);
            return string.Join("|", ordered);
        }

        private static QuestLookupContext BuildQuestLookup()
        {
            var context = new QuestLookupContext
            {
                questById = new Dictionary<string, QuestLookup>(StringComparer.OrdinalIgnoreCase),
                duplicateQuestIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                databaseAssetPaths = new List<string>()
            };

            string[] guids = AssetDatabase.FindAssets("t:QuestDatabase");
            var dbPaths = new List<string>(guids.Length);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!string.IsNullOrWhiteSpace(path))
                {
                    dbPaths.Add(path);
                }
            }

            dbPaths.Sort(StringComparer.Ordinal);
            for (int i = 0; i < dbPaths.Count; i++)
            {
                string dbPath = dbPaths[i];
                QuestDatabase database = AssetDatabase.LoadAssetAtPath<QuestDatabase>(dbPath);
                if (database == null)
                {
                    continue;
                }

                context.databaseAssetPaths.Add(dbPath);
                List<QuestData> quests = database.quests ?? new List<QuestData>();
                for (int questIndex = 0; questIndex < quests.Count; questIndex++)
                {
                    QuestData quest = quests[questIndex];
                    if (quest == null || string.IsNullOrWhiteSpace(quest.questId))
                    {
                        continue;
                    }

                    string questId = quest.questId.Trim();
                    if (context.questById.TryGetValue(questId, out QuestLookup existing))
                    {
                        context.duplicateQuestIds.Add(questId);
                        if (string.CompareOrdinal(dbPath, existing.databaseAssetPath) < 0)
                        {
                            context.questById[questId] = new QuestLookup
                            {
                                quest = quest,
                                databaseAssetPath = dbPath
                            };
                        }

                        continue;
                    }

                    context.questById.Add(questId, new QuestLookup
                    {
                        quest = quest,
                        databaseAssetPath = dbPath
                    });
                }
            }

            return context;
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
                if (levelData == null || !IsFormalLevelId(levelData.levelId))
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
                    levelData = levelData,
                    levelAssetPath = assetPath,
                    levelIndex = levelIndex
                });
            }

            result.Sort((a, b) =>
            {
                int cmp = a.levelIndex.CompareTo(b.levelIndex);
                if (cmp != 0)
                {
                    return cmp;
                }

                return string.Compare(a.levelAssetPath, b.levelAssetPath, StringComparison.Ordinal);
            });
            return result;
        }

        private static List<string> BuildGlobalWarnings(
            List<LevelEntry> entries,
            QuestLookupContext questLookup,
            List<ValidationRow> rows)
        {
            var warnings = new List<string>();

            if (entries.Count < (MaxLevelIndex - MinLevelIndex + 1))
            {
                warnings.Add(
                    $"expected {MaxLevelIndex - MinLevelIndex + 1} levels but found {entries.Count} LevelData assets.");
            }

            if (questLookup.databaseAssetPaths == null || questLookup.databaseAssetPaths.Count == 0)
            {
                warnings.Add("no QuestDatabase assets found.");
            }

            if (questLookup.duplicateQuestIds != null && questLookup.duplicateQuestIds.Count > 0)
            {
                var duplicateIds = new List<string>(questLookup.duplicateQuestIds);
                duplicateIds.Sort(StringComparer.OrdinalIgnoreCase);
                warnings.Add($"duplicate quest ids across QuestDatabase assets: {string.Join(",", duplicateIds)}.");
            }

            int rowsWithWarnings = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].warnings > 0)
                {
                    rowsWithWarnings++;
                }
            }

            if (rowsWithWarnings > 0)
            {
                warnings.Add($"rows with linkage warnings: {rowsWithWarnings}/{rows.Count}.");
            }

            return warnings;
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

        private static string WriteCsv(List<ValidationRow> rows)
        {
            string fullPath = Path.GetFullPath(ReportCsvPath);
            EnsureDirectoryExists(fullPath);

            var csv = new StringBuilder();
            csv.AppendLine(
                "level_index,level_id,level_asset,scene_name,scene_path,status,blocking_errors,warnings,required_quests,resolved_required_quests,missing_required_quests,required_stage_count,required_beat_objectives,required_event_objectives,required_boss_objectives,quest_configs,strongholds,override_waves,override_events,event_type_count,event_types,next_level_id,next_level_index,note");

            for (int i = 0; i < rows.Count; i++)
            {
                ValidationRow row = rows[i];
                csv.Append(row.levelIndex).Append(',')
                    .Append(EscapeCsv(row.levelId)).Append(',')
                    .Append(EscapeCsv(row.levelAssetPath)).Append(',')
                    .Append(EscapeCsv(row.sceneName)).Append(',')
                    .Append(EscapeCsv(row.scenePath)).Append(',')
                    .Append(EscapeCsv(row.status)).Append(',')
                    .Append(row.blockingErrors).Append(',')
                    .Append(row.warnings).Append(',')
                    .Append(row.requiredQuestCount).Append(',')
                    .Append(row.requiredQuestResolved).Append(',')
                    .Append(row.requiredQuestMissing).Append(',')
                    .Append(row.requiredStageCount).Append(',')
                    .Append(row.requiredBeatObjectiveCount).Append(',')
                    .Append(row.requiredEventObjectiveCount).Append(',')
                    .Append(row.requiredBossObjectiveCount).Append(',')
                    .Append(row.questConfigCount).Append(',')
                    .Append(row.strongholdCount).Append(',')
                    .Append(row.overrideWaveCount).Append(',')
                    .Append(row.overrideEventCount).Append(',')
                    .Append(row.eventTypeCount).Append(',')
                    .Append(EscapeCsv(row.eventTypes)).Append(',')
                    .Append(EscapeCsv(row.nextLevelId)).Append(',')
                    .Append(row.nextLevelIndex).Append(',')
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
            QuestLookupContext questLookup)
        {
            string fullPath = Path.GetFullPath(SummaryMdPath);
            EnsureDirectoryExists(fullPath);

            int warnRows = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].warnings > 0)
                {
                    warnRows++;
                }
            }

            var md = new StringBuilder();
            md.AppendLine("# Level Quest Beat Linkage Summary");
            md.AppendLine();
            md.AppendLine($"- Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss zzz}");
            md.AppendLine($"- Target Levels: {rows.Count}");
            md.AppendLine($"- Error Rows: {errorRows}");
            md.AppendLine($"- Warning Rows: {warnRows}");
            md.AppendLine($"- Total Warnings: {warningTotal}");
            md.AppendLine($"- Quest Databases: {questLookup.databaseAssetPaths.Count}");
            md.AppendLine($"- Quest Lookup Entries: {questLookup.questById.Count}");
            md.AppendLine($"- CSV: {ReportCsvPath}");
            md.AppendLine();

            if (globalWarnings != null && globalWarnings.Count > 0)
            {
                md.AppendLine("## Global Warnings");
                for (int i = 0; i < globalWarnings.Count; i++)
                {
                    md.AppendLine($"- {globalWarnings[i]}");
                }

                md.AppendLine();
            }

            md.AppendLine("| Level | Status | Blocking | Warnings | RequiredQuests | Resolved | EventObjectives | BossObjectives | Events | Note |");
            md.AppendLine("|---|---|---:|---:|---:|---:|---:|---:|---|---|");
            for (int i = 0; i < rows.Count; i++)
            {
                ValidationRow row = rows[i];
                md.Append("| ")
                    .Append(SafeMarkdownCell(row.levelId)).Append(" | ")
                    .Append(SafeMarkdownCell(row.status)).Append(" | ")
                    .Append(row.blockingErrors).Append(" | ")
                    .Append(row.warnings).Append(" | ")
                    .Append(row.requiredQuestCount).Append(" | ")
                    .Append(row.requiredQuestResolved).Append(" | ")
                    .Append(row.requiredEventObjectiveCount).Append(" | ")
                    .Append(row.requiredBossObjectiveCount).Append(" | ")
                    .Append(SafeMarkdownCell(row.eventTypes)).Append(" | ")
                    .Append(SafeMarkdownCell(TrimForMarkdownTable(row.note, 180))).Append(" |")
                    .AppendLine();
            }

            File.WriteAllText(fullPath, md.ToString(), new UTF8Encoding(false));
            return SummaryMdPath;
        }

        private static bool IsFormalLevelId(string levelId)
        {
            return !string.IsNullOrWhiteSpace(levelId) &&
                   levelId.StartsWith("LEVEL_", StringComparison.OrdinalIgnoreCase);
        }

        private static int ParseLevelIndex(string levelId)
        {
            if (!IsFormalLevelId(levelId))
            {
                return -1;
            }

            string part = levelId.Substring("LEVEL_".Length);
            return int.TryParse(part, out int parsed) ? parsed : -1;
        }

        private static string BuildScenePath(LevelData levelData)
        {
            if (levelData == null || string.IsNullOrWhiteSpace(levelData.sceneName))
            {
                return string.Empty;
            }

            return $"{SceneFolderPath}/{levelData.sceneName.Trim()}.unity";
        }

        private static string SafeStageLabel(QuestStage stage, int index)
        {
            if (stage == null)
            {
                return $"stage_{index}";
            }

            if (!string.IsNullOrWhiteSpace(stage.stageId))
            {
                return stage.stageId.Trim();
            }

            if (!string.IsNullOrWhiteSpace(stage.title))
            {
                return stage.title.Trim();
            }

            return $"stage_{index}";
        }

        private static bool AssetExists(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return false;
            }

            return File.Exists(Path.GetFullPath(assetPath));
        }

        private static void EnsureDirectoryExists(string filePath)
        {
            string dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            bool mustQuote = value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0;
            if (!mustQuote)
            {
                return value;
            }

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static string TrimForMarkdownTable(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || maxLength <= 0 || value.Length <= maxLength)
            {
                return value ?? string.Empty;
            }

            return value.Substring(0, maxLength) + "...";
        }

        private static string SafeMarkdownCell(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
        }

        private struct LevelEntry
        {
            public LevelData levelData;
            public string levelAssetPath;
            public int levelIndex;
        }

        private struct QuestLookup
        {
            public QuestData quest;
            public string databaseAssetPath;
        }

        private struct QuestLookupContext
        {
            public Dictionary<string, QuestLookup> questById;
            public HashSet<string> duplicateQuestIds;
            public List<string> databaseAssetPaths;
        }

        private sealed class ValidationRow
        {
            public int levelIndex;
            public string levelId;
            public string levelAssetPath;
            public string sceneName;
            public string scenePath;
            public string status;
            public int blockingErrors;
            public int warnings;
            public int requiredQuestCount;
            public int requiredQuestResolved;
            public int requiredQuestMissing;
            public int requiredStageCount;
            public int requiredBeatObjectiveCount;
            public int requiredEventObjectiveCount;
            public int requiredBossObjectiveCount;
            public int questConfigCount;
            public int strongholdCount;
            public int overrideWaveCount;
            public int overrideEventCount;
            public int eventTypeCount;
            public string eventTypes;
            public string nextLevelId;
            public int nextLevelIndex;
            public string note;
        }
    }
}
