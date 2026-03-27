using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ThirdPersonController;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThirdPersonController.Editor
{
    public static class BossFlowCouplingValidator
    {
        private const string ValidateMenuPath = "Tools/Boss/P1/Validate Level 03-10 Boss Flow Coupling (CSV)";
        private const string ValidateGateMenuPath = "Tools/Boss/P1/Validate Level 03-10 Boss Flow Coupling (CI Gate)";
        private const string SceneFolderPath = "Assets/Scenes";
        private const string ReportCsvPath = "Assets/ThirdPersonController/Reports/boss_level_flow_coupling_report.csv";
        private const string SummaryMdPath = "Assets/ThirdPersonController/Reports/boss_level_flow_coupling_summary.md";
        private const string LogPrefix = "[BossFlowCoupling]";
        private const int MinLevelIndex = 3;
        private const int MaxLevelIndex = 10;

        [MenuItem(ValidateMenuPath)]
        public static void Validate()
        {
            Run(interactive: true, failOnBlocking: false);
        }

        [MenuItem(ValidateGateMenuPath)]
        public static void ValidateCiGate()
        {
            Run(interactive: false, failOnBlocking: true);
        }

        public static void ValidateForBatch()
        {
            Run(interactive: false, failOnBlocking: true);
        }

        private static void Run(bool interactive, bool failOnBlocking)
        {
            if (interactive && !Application.isBatchMode)
            {
                bool allow = EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                if (!allow)
                {
                    return;
                }
            }

            List<LevelEntry> entries = CollectTargetLevels();
            if (entries.Count == 0)
            {
                string noneMessage = $"{LogPrefix} no LevelData assets found for LEVEL_{MinLevelIndex:D2}~LEVEL_{MaxLevelIndex:D2}.";
                Debug.LogWarning(noneMessage);
                if (interactive)
                {
                    EditorUtility.DisplayDialog("Boss Flow Coupling", noneMessage, "OK");
                }

                return;
            }

            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
            var rows = new List<ValidationRow>(entries.Count);
            try
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    rows.Add(ValidateEntry(entries[i]));
                }
            }
            finally
            {
                if (originalSetup != null && originalSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
                }
            }

            int blockingTotal = 0;
            int warningTotal = 0;
            int errorSceneCount = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                ValidationRow row = rows[i];
                blockingTotal += row.blockingErrors;
                warningTotal += row.warnings;
                if (string.Equals(row.status, "Error", StringComparison.Ordinal))
                {
                    errorSceneCount++;
                }
            }

            string csvPath = WriteCsv(rows);
            string summaryPath = WriteSummary(rows, blockingTotal, warningTotal);
            AssetDatabase.Refresh();

            string summary =
                $"targets={rows.Count} errorScenes={errorSceneCount} blocking={blockingTotal} warnings={warningTotal} " +
                $"csv={csvPath} summary={summaryPath}";
            Debug.Log($"{LogPrefix} complete | {summary}");

            if (interactive)
            {
                EditorUtility.DisplayDialog("Boss Flow Coupling", summary, "OK");
            }

            if (failOnBlocking && blockingTotal > 0)
            {
                throw new InvalidOperationException($"{LogPrefix} gate failed with blocking={blockingTotal}. csv={csvPath}");
            }
        }

        private static ValidationRow ValidateEntry(LevelEntry entry)
        {
            var issues = new List<ValidationIssue>(32);
            int strongholdCount = 0;
            int bossSpawnPointCount = 0;
            int requiredQuestCount = 0;
            int questInLevelCount = 0;
            int questCouplingScore = 0;
            int bossObjectiveCount = 0;

            if (entry.levelData == null)
            {
                issues.Add(ValidationIssue.Blocking("LevelData asset is null."));
                return BuildRow(entry, string.Empty, strongholdCount, bossSpawnPointCount, requiredQuestCount, questInLevelCount, questCouplingScore, bossObjectiveCount, issues);
            }

            string scenePath = BuildScenePath(entry.levelData);
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                issues.Add(ValidationIssue.Blocking("LevelData.sceneName is empty."));
                return BuildRow(entry, scenePath, strongholdCount, bossSpawnPointCount, requiredQuestCount, questInLevelCount, questCouplingScore, bossObjectiveCount, issues);
            }

            if (!AssetExists(scenePath))
            {
                issues.Add(ValidationIssue.Blocking("Scene asset is missing."));
                return BuildRow(entry, scenePath, strongholdCount, bossSpawnPointCount, requiredQuestCount, questInLevelCount, questCouplingScore, bossObjectiveCount, issues);
            }

            Scene scene;
            try
            {
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }
            catch (Exception ex)
            {
                issues.Add(ValidationIssue.Blocking($"OpenScene failed: {ex.Message}"));
                return BuildRow(entry, scenePath, strongholdCount, bossSpawnPointCount, requiredQuestCount, questInLevelCount, questCouplingScore, bossObjectiveCount, issues);
            }

            bool expectBossGate = entry.levelData.overrideBossSettings;
            int expectedRuntimeLevelId = ResolveRuntimeLevelId(entry.levelData, entry.levelIndex);

            LevelFlowController levelFlow = FindComponentInScene<LevelFlowController>(scene);
            LevelRuntimeConfigurator runtimeConfigurator = FindComponentInScene<LevelRuntimeConfigurator>(scene);
            StrongholdSequenceController sequence = FindComponentInScene<StrongholdSequenceController>(scene);
            List<StrongholdController> strongholds = FindComponentsInScene<StrongholdController>(scene);
            List<BossSpawnPoint> bossSpawnPoints = FindComponentsInScene<BossSpawnPoint>(scene);

            strongholdCount = strongholds.Count;
            bossSpawnPointCount = bossSpawnPoints.Count;

            var strongholdIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < strongholds.Count; i++)
            {
                StrongholdController stronghold = strongholds[i];
                if (stronghold == null)
                {
                    continue;
                }

                string id = stronghold.StrongholdId;
                if (!string.IsNullOrWhiteSpace(id))
                {
                    strongholdIds.Add(id);
                }
            }

            var bossIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < bossSpawnPoints.Count; i++)
            {
                BossSpawnPoint spawnPoint = bossSpawnPoints[i];
                if (spawnPoint == null)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(spawnPoint.bossName))
                {
                    bossIds.Add(spawnPoint.bossName);
                }

                if (!string.IsNullOrWhiteSpace(spawnPoint.name))
                {
                    bossIds.Add(spawnPoint.name);
                }
            }

            if (levelFlow == null)
            {
                issues.Add(ValidationIssue.Blocking("Missing LevelFlowController."));
            }
            else
            {
                if (levelFlow.levelData == null)
                {
                    issues.Add(ValidationIssue.Blocking("LevelFlow.levelData is null."));
                }
                else if (levelFlow.levelData != entry.levelData)
                {
                    issues.Add(ValidationIssue.Blocking("LevelFlow.levelData does not reference current LevelData."));
                }

                if (expectedRuntimeLevelId > 0 && levelFlow.levelId != expectedRuntimeLevelId)
                {
                    issues.Add(ValidationIssue.Blocking(
                        $"LevelFlow.levelId mismatch (expected {expectedRuntimeLevelId}, actual {levelFlow.levelId})."));
                }
            }

            if (runtimeConfigurator == null)
            {
                issues.Add(ValidationIssue.Warning("Missing LevelRuntimeConfigurator (runtime auto-create fallback path)."));
            }
            else
            {
                if (runtimeConfigurator.levelData != null && runtimeConfigurator.levelData != entry.levelData)
                {
                    issues.Add(ValidationIssue.Blocking("RuntimeConfigurator.levelData references a different LevelData."));
                }

                if (levelFlow != null && runtimeConfigurator.levelFlow != null && runtimeConfigurator.levelFlow != levelFlow)
                {
                    issues.Add(ValidationIssue.Blocking("RuntimeConfigurator.levelFlow references a different LevelFlowController."));
                }

                if (sequence != null && runtimeConfigurator.sequenceController != null && runtimeConfigurator.sequenceController != sequence)
                {
                    issues.Add(ValidationIssue.Warning("RuntimeConfigurator.sequenceController does not match scene StrongholdSequenceController."));
                }
            }

            if (sequence == null)
            {
                issues.Add(ValidationIssue.Blocking("Missing StrongholdSequenceController."));
            }
            else
            {
                if (sequence.strongholds == null || sequence.strongholds.Count == 0)
                {
                    issues.Add(ValidationIssue.Blocking("StrongholdSequence.strongholds is empty."));
                }
                else
                {
                    for (int i = 0; i < sequence.strongholds.Count; i++)
                    {
                        StrongholdController linked = sequence.strongholds[i];
                        if (linked == null)
                        {
                            issues.Add(ValidationIssue.Blocking($"StrongholdSequence.strongholds[{i}] is null."));
                        }
                        else if (!strongholds.Contains(linked))
                        {
                            issues.Add(ValidationIssue.Warning($"StrongholdSequence.strongholds[{i}] points outside current scene stronghold set."));
                        }
                    }
                }

                if (!sequence.triggerLevelCompleteOnFinish)
                {
                    issues.Add(ValidationIssue.Blocking("StrongholdSequence.triggerLevelCompleteOnFinish should be true."));
                }

                if (!sequence.triggerVictoryOnFinish)
                {
                    issues.Add(ValidationIssue.Warning("StrongholdSequence.triggerVictoryOnFinish is false."));
                }

                if (expectedRuntimeLevelId > 0 && sequence.levelId != expectedRuntimeLevelId)
                {
                    issues.Add(ValidationIssue.Blocking(
                        $"StrongholdSequence.levelId mismatch (expected {expectedRuntimeLevelId}, actual {sequence.levelId})."));
                }
            }

            if (expectBossGate)
            {
                if (bossSpawnPoints.Count == 0)
                {
                    issues.Add(ValidationIssue.Blocking("Boss gate enabled but no BossSpawnPoint found in scene."));
                }
                else if (bossSpawnPoints.Count > 1)
                {
                    issues.Add(ValidationIssue.Warning("Multiple BossSpawnPoint found; ensure intentional encounter ownership."));
                }

                BossSpawnPoint linkedBoss = sequence != null ? sequence.bossSpawnPoint : null;
                if (linkedBoss == null)
                {
                    issues.Add(ValidationIssue.Blocking("Boss gate enabled but StrongholdSequence.bossSpawnPoint is null."));
                }
                else if (!bossSpawnPoints.Contains(linkedBoss))
                {
                    issues.Add(ValidationIssue.Blocking("StrongholdSequence.bossSpawnPoint points outside current scene."));
                }

                if (sequence != null && !sequence.deferCompletionUntilBoss)
                {
                    issues.Add(ValidationIssue.Blocking("Boss gate enabled but deferCompletionUntilBoss is false."));
                }

                if (linkedBoss != null && linkedBoss.spawnOnStart)
                {
                    issues.Add(ValidationIssue.Blocking("BossSpawnPoint.spawnOnStart should be false for boss-gated flow."));
                }

                if (runtimeConfigurator != null && runtimeConfigurator.bossSpawnPoint != null &&
                    !bossSpawnPoints.Contains(runtimeConfigurator.bossSpawnPoint))
                {
                    issues.Add(ValidationIssue.Blocking("RuntimeConfigurator.bossSpawnPoint points outside current scene."));
                }
            }
            else
            {
                if (sequence != null && sequence.deferCompletionUntilBoss)
                {
                    issues.Add(ValidationIssue.Warning("deferCompletionUntilBoss is true while LevelData.overrideBossSettings is false."));
                }
            }

            List<QuestConfig> questConfigs = entry.levelData.quests ?? new List<QuestConfig>();
            questInLevelCount = questConfigs.Count;
            for (int i = 0; i < questConfigs.Count; i++)
            {
                QuestConfig config = questConfigs[i];
                if (config != null && config.required)
                {
                    requiredQuestCount++;
                }
            }

            if (questInLevelCount == 0)
            {
                issues.Add(ValidationIssue.Warning("LevelData.quests is empty."));
            }
            else
            {
                QuestDatabase questDatabase = ResolveQuestDatabase(levelFlow, runtimeConfigurator, out bool usedFallback);
                if (questDatabase == null)
                {
                    issues.Add(ValidationIssue.Blocking("QuestDatabase is missing for level quest binding."));
                }
                else
                {
                    if (usedFallback)
                    {
                        issues.Add(ValidationIssue.Warning("QuestDatabase resolved by asset fallback; scene wiring does not reference it directly."));
                    }

                    bool hasBossObjectiveInRequiredQuest = false;
                    for (int i = 0; i < questConfigs.Count; i++)
                    {
                        QuestConfig config = questConfigs[i];
                        if (config == null || string.IsNullOrWhiteSpace(config.questId))
                        {
                            issues.Add(ValidationIssue.Blocking($"LevelData.quests[{i}] has empty questId."));
                            continue;
                        }

                        QuestData questData = questDatabase.GetQuestById(config.questId);
                        if (questData == null)
                        {
                            issues.Add(ValidationIssue.Blocking($"QuestDatabase missing quest id '{config.questId}'."));
                            continue;
                        }

                        int localCouplingScore = 0;
                        int localBossObjectiveCount = 0;
                        bool hasRequiredObjective = false;
                        AnalyzeQuestCoupling(
                            config.questId,
                            questData,
                            strongholdIds,
                            bossIds,
                            issues,
                            ref localCouplingScore,
                            ref localBossObjectiveCount,
                            ref hasRequiredObjective);

                        questCouplingScore += localCouplingScore;
                        bossObjectiveCount += localBossObjectiveCount;

                        if (config.required)
                        {
                            if (questData.isOptional)
                            {
                                issues.Add(ValidationIssue.Warning($"Required quest '{config.questId}' is marked optional in QuestDatabase."));
                            }

                            if (!hasRequiredObjective)
                            {
                                issues.Add(ValidationIssue.Warning(
                                    $"Required quest '{config.questId}' has no structural objective (CompleteStronghold/CompleteWave/CompleteWaveEvent/BossBreak/BossDefeat)."));
                            }

                            if (localBossObjectiveCount > 0)
                            {
                                hasBossObjectiveInRequiredQuest = true;
                            }
                        }
                    }

                    if (expectBossGate && requiredQuestCount > 0 && !hasBossObjectiveInRequiredQuest)
                    {
                        issues.Add(ValidationIssue.Blocking("Boss-gated level has no explicit BossBreak/BossDefeat objective in required quest chain."));
                    }
                }
            }

            return BuildRow(
                entry,
                scenePath,
                strongholdCount,
                bossSpawnPointCount,
                requiredQuestCount,
                questInLevelCount,
                questCouplingScore,
                bossObjectiveCount,
                issues);
        }

        private static void AnalyzeQuestCoupling(
            string questId,
            QuestData questData,
            HashSet<string> strongholdIds,
            HashSet<string> bossIds,
            List<ValidationIssue> issues,
            ref int couplingScore,
            ref int bossObjectiveCount,
            ref bool hasRequiredObjective)
        {
            if (questData == null)
            {
                return;
            }

            if (questData.stages != null && questData.stages.Count > 0)
            {
                for (int i = 0; i < questData.stages.Count; i++)
                {
                    QuestStage stage = questData.stages[i];
                    if (stage == null)
                    {
                        issues.Add(ValidationIssue.Warning($"Quest '{questId}' stage[{i}] is null."));
                        continue;
                    }

                    AnalyzeObjective(
                        questId,
                        $"stage[{i}]",
                        stage.questType,
                        stage.targetStrongholdId,
                        stage.targetBossId,
                        stage.matchAnyWaveEventType,
                        stage.targetWaveEventType,
                        strongholdIds,
                        bossIds,
                        issues,
                        ref couplingScore,
                        ref bossObjectiveCount,
                        ref hasRequiredObjective);
                }
            }
            else
            {
                AnalyzeObjective(
                    questId,
                    "root",
                    questData.questType,
                    questData.targetStrongholdId,
                    questData.targetBossId,
                    questData.matchAnyWaveEventType,
                    questData.targetWaveEventType,
                    strongholdIds,
                    bossIds,
                    issues,
                    ref couplingScore,
                    ref bossObjectiveCount,
                    ref hasRequiredObjective);
            }
        }

        private static void AnalyzeObjective(
            string questId,
            string label,
            QuestType questType,
            string targetStrongholdId,
            string targetBossId,
            bool matchAnyWaveEventType,
            WaveEventType targetWaveEventType,
            HashSet<string> strongholdIds,
            HashSet<string> bossIds,
            List<ValidationIssue> issues,
            ref int couplingScore,
            ref int bossObjectiveCount,
            ref bool hasRequiredObjective)
        {
            switch (questType)
            {
                case QuestType.CompleteStronghold:
                    hasRequiredObjective = true;
                    couplingScore++;
                    if (string.IsNullOrWhiteSpace(targetStrongholdId))
                    {
                        issues.Add(ValidationIssue.Warning($"Quest '{questId}' {label} CompleteStronghold has empty targetStrongholdId."));
                    }
                    else if (!strongholdIds.Contains(targetStrongholdId))
                    {
                        issues.Add(ValidationIssue.Blocking(
                            $"Quest '{questId}' {label} targets missing stronghold '{targetStrongholdId}'."));
                    }

                    break;

                case QuestType.CompleteWave:
                    hasRequiredObjective = true;
                    couplingScore++;
                    if (string.IsNullOrWhiteSpace(targetStrongholdId))
                    {
                        issues.Add(ValidationIssue.Warning($"Quest '{questId}' {label} CompleteWave has empty targetStrongholdId (uses any stronghold)."));
                    }
                    else if (!strongholdIds.Contains(targetStrongholdId))
                    {
                        issues.Add(ValidationIssue.Blocking(
                            $"Quest '{questId}' {label} targets missing stronghold '{targetStrongholdId}'."));
                    }

                    break;

                case QuestType.CompleteWaveEvent:
                    hasRequiredObjective = true;
                    couplingScore++;
                    if (string.IsNullOrWhiteSpace(targetStrongholdId))
                    {
                        issues.Add(ValidationIssue.Warning($"Quest '{questId}' {label} CompleteWaveEvent has empty targetStrongholdId."));
                    }
                    else if (!strongholdIds.Contains(targetStrongholdId))
                    {
                        issues.Add(ValidationIssue.Blocking(
                            $"Quest '{questId}' {label} targets missing stronghold '{targetStrongholdId}'."));
                    }

                    if (!matchAnyWaveEventType)
                    {
                        couplingScore++;
                        if (targetWaveEventType == WaveEventType.Reinforcement)
                        {
                            issues.Add(ValidationIssue.Warning(
                                $"Quest '{questId}' {label} restricts to Reinforcement only; verify design intent."));
                        }
                    }

                    break;

                case QuestType.BossBreak:
                case QuestType.BossDefeat:
                    hasRequiredObjective = true;
                    couplingScore += 2;
                    bossObjectiveCount++;
                    if (!string.IsNullOrWhiteSpace(targetBossId) && !bossIds.Contains(targetBossId))
                    {
                        issues.Add(ValidationIssue.Warning(
                            $"Quest '{questId}' {label} targets boss '{targetBossId}' not found in scene boss ids."));
                    }

                    break;
            }
        }
        private static QuestDatabase ResolveQuestDatabase(
            LevelFlowController levelFlow,
            LevelRuntimeConfigurator runtimeConfigurator,
            out bool usedFallback)
        {
            usedFallback = false;

            if (levelFlow != null && levelFlow.questDatabase != null)
            {
                return levelFlow.questDatabase;
            }

            if (runtimeConfigurator != null && runtimeConfigurator.questDatabase != null)
            {
                return runtimeConfigurator.questDatabase;
            }

            string[] guids = AssetDatabase.FindAssets("t:QuestDatabase");
            QuestDatabase fallback = null;
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                QuestDatabase candidate = AssetDatabase.LoadAssetAtPath<QuestDatabase>(path);
                if (candidate == null)
                {
                    continue;
                }

                if (fallback != null)
                {
                    return null;
                }

                fallback = candidate;
            }

            if (fallback != null)
            {
                usedFallback = true;
            }

            return fallback;
        }

        private static ValidationRow BuildRow(
            LevelEntry entry,
            string scenePath,
            int strongholdCount,
            int bossSpawnPointCount,
            int requiredQuestCount,
            int questInLevelCount,
            int questCouplingScore,
            int bossObjectiveCount,
            List<ValidationIssue> issues)
        {
            int blocking = 0;
            int warnings = 0;
            var notes = new List<string>();
            for (int i = 0; i < issues.Count; i++)
            {
                ValidationIssue issue = issues[i];
                if (issue.isBlocking)
                {
                    blocking++;
                    notes.Add("[B] " + issue.message);
                }
                else
                {
                    warnings++;
                    notes.Add("[W] " + issue.message);
                }
            }

            string status = blocking > 0 ? "Error" : "Ok";
            return new ValidationRow
            {
                levelId = entry.levelData != null ? entry.levelData.levelId : string.Empty,
                levelAssetPath = entry.levelAssetPath ?? string.Empty,
                sceneName = entry.levelData != null ? entry.levelData.sceneName : string.Empty,
                scenePath = scenePath ?? string.Empty,
                status = status,
                blockingErrors = blocking,
                warnings = warnings,
                strongholdCount = strongholdCount,
                bossSpawnPointCount = bossSpawnPointCount,
                requiredQuestCount = requiredQuestCount,
                questInLevelCount = questInLevelCount,
                questCouplingScore = questCouplingScore,
                bossObjectiveCount = bossObjectiveCount,
                note = notes.Count > 0 ? string.Join(" ", notes) : string.Empty
            };
        }

        private static int ResolveRuntimeLevelId(LevelData levelData, int parsedLevelIndex)
        {
            if (levelData == null || levelData.chapterId <= 0 || parsedLevelIndex <= 0)
            {
                return 0;
            }

            return levelData.chapterId * 100 + parsedLevelIndex;
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

        private static string WriteCsv(List<ValidationRow> rows)
        {
            string fullPath = Path.GetFullPath(ReportCsvPath);
            EnsureDirectoryExists(fullPath);

            var csv = new StringBuilder();
            csv.AppendLine("level_id,level_asset,scene_name,scene_path,status,blocking_errors,warnings,strongholds,boss_spawn_points,required_quests,level_quests,quest_coupling_score,boss_objective_count,note");
            for (int i = 0; i < rows.Count; i++)
            {
                ValidationRow row = rows[i];
                csv.Append(EscapeCsv(row.levelId)).Append(',')
                    .Append(EscapeCsv(row.levelAssetPath)).Append(',')
                    .Append(EscapeCsv(row.sceneName)).Append(',')
                    .Append(EscapeCsv(row.scenePath)).Append(',')
                    .Append(EscapeCsv(row.status)).Append(',')
                    .Append(row.blockingErrors).Append(',')
                    .Append(row.warnings).Append(',')
                    .Append(row.strongholdCount).Append(',')
                    .Append(row.bossSpawnPointCount).Append(',')
                    .Append(row.requiredQuestCount).Append(',')
                    .Append(row.questInLevelCount).Append(',')
                    .Append(row.questCouplingScore).Append(',')
                    .Append(row.bossObjectiveCount).Append(',')
                    .Append(EscapeCsv(row.note))
                    .AppendLine();
            }

            File.WriteAllText(fullPath, csv.ToString(), new UTF8Encoding(false));
            return ReportCsvPath;
        }

        private static string WriteSummary(List<ValidationRow> rows, int blockingTotal, int warningTotal)
        {
            string fullPath = Path.GetFullPath(SummaryMdPath);
            EnsureDirectoryExists(fullPath);

            int errorScenes = 0;
            int warnScenes = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                ValidationRow row = rows[i];
                if (string.Equals(row.status, "Error", StringComparison.Ordinal))
                {
                    errorScenes++;
                }
                else if (row.warnings > 0)
                {
                    warnScenes++;
                }
            }

            var md = new StringBuilder();
            md.AppendLine("# Boss Flow Coupling Summary");
            md.AppendLine();
            md.AppendLine($"- Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss zzz}");
            md.AppendLine($"- Targets: {rows.Count}");
            md.AppendLine($"- Error Scenes: {errorScenes}");
            md.AppendLine($"- Warning Scenes: {warnScenes}");
            md.AppendLine($"- Blocking Errors: {blockingTotal}");
            md.AppendLine($"- Warnings: {warningTotal}");
            md.AppendLine($"- CSV: {ReportCsvPath}");
            md.AppendLine();
            md.AppendLine("| Level | Scene | Status | Blocking | Warnings | Strongholds | BossSpawn | RequiredQuests | CouplingScore | Note |");
            md.AppendLine("|---|---|---|---:|---:|---:|---:|---:|---:|---|");

            for (int i = 0; i < rows.Count; i++)
            {
                ValidationRow row = rows[i];
                md.Append("| ")
                    .Append(SafeMarkdownCell(row.levelId)).Append(" | ")
                    .Append(SafeMarkdownCell(row.sceneName)).Append(" | ")
                    .Append(SafeMarkdownCell(row.status)).Append(" | ")
                    .Append(row.blockingErrors).Append(" | ")
                    .Append(row.warnings).Append(" | ")
                    .Append(row.strongholdCount).Append(" | ")
                    .Append(row.bossSpawnPointCount).Append(" | ")
                    .Append(row.requiredQuestCount).Append(" | ")
                    .Append(row.questCouplingScore).Append(" | ")
                    .Append(SafeMarkdownCell(TrimForMarkdownTable(row.note, 180))).Append(" |")
                    .AppendLine();
            }

            File.WriteAllText(fullPath, md.ToString(), new UTF8Encoding(false));
            return SummaryMdPath;
        }

        private static string BuildScenePath(LevelData levelData)
        {
            if (levelData == null || string.IsNullOrWhiteSpace(levelData.sceneName))
            {
                return string.Empty;
            }

            return $"{SceneFolderPath}/{levelData.sceneName.Trim()}.unity";
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

        private static bool AssetExists(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return false;
            }

            return File.Exists(Path.GetFullPath(assetPath));
        }

        private static T FindComponentInScene<T>(Scene scene) where T : Component
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return null;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (root == null)
                {
                    continue;
                }

                T component = root.GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        private static List<T> FindComponentsInScene<T>(Scene scene) where T : Component
        {
            var result = new List<T>();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return result;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (root == null)
                {
                    continue;
                }

                T[] components = root.GetComponentsInChildren<T>(true);
                for (int j = 0; j < components.Length; j++)
                {
                    T component = components[j];
                    if (component != null)
                    {
                        result.Add(component);
                    }
                }
            }

            return result;
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

        private struct ValidationIssue
        {
            public bool isBlocking;
            public string message;

            public static ValidationIssue Blocking(string message)
            {
                return new ValidationIssue
                {
                    isBlocking = true,
                    message = message ?? string.Empty
                };
            }

            public static ValidationIssue Warning(string message)
            {
                return new ValidationIssue
                {
                    isBlocking = false,
                    message = message ?? string.Empty
                };
            }
        }

        private struct ValidationRow
        {
            public string levelId;
            public string levelAssetPath;
            public string sceneName;
            public string scenePath;
            public string status;
            public int blockingErrors;
            public int warnings;
            public int strongholdCount;
            public int bossSpawnPointCount;
            public int requiredQuestCount;
            public int questInLevelCount;
            public int questCouplingScore;
            public int bossObjectiveCount;
            public string note;
        }
    }
}
