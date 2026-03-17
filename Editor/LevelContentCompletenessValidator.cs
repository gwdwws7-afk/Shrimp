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
    public static class LevelContentCompletenessValidator
    {
        private const string ValidateMenuPath = "Tools/Level/P0/Validate Level Content Completeness (CSV)";
        private const string ValidateGateMenuPath = "Tools/Level/P0/Validate Level Content Completeness (CI Gate)";
        private const string FixMenuPath = "Tools/Level/P0/Fix Level Content Completeness";
        private const string SceneFolderPath = "Assets/Scenes";
        private const string ReportCsvPath = "Assets/ThirdPersonController/Reports/level_content_completeness_report.csv";
        private const string SummaryMdPath = "Assets/ThirdPersonController/Reports/level_content_completeness_summary.md";
        private const string LogPrefix = "[LevelContentCompleteness]";

        [MenuItem(ValidateMenuPath)]
        public static void Validate()
        {
            Run(applyFix: false, interactive: true, failOnBlocking: false);
        }

        [MenuItem(ValidateGateMenuPath)]
        public static void ValidateCiGate()
        {
            Run(applyFix: false, interactive: false, failOnBlocking: true);
        }

        [MenuItem(FixMenuPath)]
        public static void Fix()
        {
            Run(applyFix: true, interactive: true, failOnBlocking: false);
        }

        public static void ValidateForBatch()
        {
            Run(applyFix: false, interactive: false, failOnBlocking: true);
        }

        public static void FixForBatch()
        {
            Run(applyFix: true, interactive: false, failOnBlocking: true);
        }

        private static void Run(bool applyFix, bool interactive, bool failOnBlocking)
        {
            if (interactive && !Application.isBatchMode)
            {
                bool allow = EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                if (!allow)
                {
                    return;
                }
            }

            List<LevelEntry> entries = CollectLevelEntries();
            if (entries.Count == 0)
            {
                string noneMessage = $"{LogPrefix} no formal LevelData assets found.";
                Debug.LogWarning(noneMessage);
                if (interactive)
                {
                    EditorUtility.DisplayDialog("Level Content Completeness", noneMessage, "OK");
                }

                return;
            }

            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
            var rows = new List<ValidationRow>(entries.Count);

            try
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    rows.Add(ValidateEntry(entries[i], applyFix));
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
            int fixedTotal = 0;
            int errorSceneCount = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                ValidationRow row = rows[i];
                blockingTotal += row.blockingErrors;
                warningTotal += row.warnings;
                fixedTotal += row.fixedCount;
                if (string.Equals(row.status, "Error", StringComparison.Ordinal))
                {
                    errorSceneCount++;
                }
            }

            string csvPath = WriteCsv(rows);
            string mdPath = WriteSummary(rows, blockingTotal, warningTotal);
            AssetDatabase.Refresh();

            string summary =
                $"mode={(applyFix ? "fix" : "validate")} targets={rows.Count} errorScenes={errorSceneCount} " +
                $"blocking={blockingTotal} warnings={warningTotal} fixed={fixedTotal} " +
                $"csv={csvPath} summary={mdPath}";
            Debug.Log($"{LogPrefix} complete | {summary}");

            if (interactive)
            {
                EditorUtility.DisplayDialog("Level Content Completeness", summary, "OK");
            }

            if (failOnBlocking && blockingTotal > 0)
            {
                throw new InvalidOperationException(
                    $"{LogPrefix} gate failed with blocking={blockingTotal}. csv={csvPath}");
            }
        }

        private static ValidationRow ValidateEntry(LevelEntry entry, bool applyFix)
        {
            var issues = new List<ValidationIssue>();
            int strongholdCount = 0;
            int waveCount = 0;
            string playerAnchor = string.Empty;
            int buildIndex = -1;
            int fixedCount = 0;
            bool sceneDirty = false;

            if (entry.levelData == null)
            {
                issues.Add(ValidationIssue.Blocking("LevelData asset is null."));
                return BuildRow(entry, string.Empty, buildIndex, strongholdCount, waveCount, playerAnchor, fixedCount, issues);
            }

            string scenePath = BuildScenePath(entry.levelData);
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                issues.Add(ValidationIssue.Blocking("LevelData.sceneName is empty."));
                return BuildRow(entry, scenePath, buildIndex, strongholdCount, waveCount, playerAnchor, fixedCount, issues);
            }

            if (!AssetExists(scenePath))
            {
                issues.Add(ValidationIssue.Blocking("Scene asset is missing."));
                return BuildRow(entry, scenePath, buildIndex, strongholdCount, waveCount, playerAnchor, fixedCount, issues);
            }

            bool sceneInBuildSettings = TryGetEnabledBuildIndex(scenePath, out buildIndex, out bool presentButDisabled);
            if (!sceneInBuildSettings)
            {
                if (presentButDisabled)
                {
                    issues.Add(ValidationIssue.Blocking("Scene exists in BuildSettings but is disabled."));
                }
                else
                {
                    issues.Add(ValidationIssue.Blocking("Scene not found in BuildSettings."));
                }
            }

            Scene scene;
            try
            {
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }
            catch (Exception ex)
            {
                issues.Add(ValidationIssue.Blocking($"OpenScene failed: {ex.Message}"));
                return BuildRow(entry, scenePath, buildIndex, strongholdCount, waveCount, playerAnchor, fixedCount, issues);
            }

            LevelFlowController levelFlow = FindComponentInScene<LevelFlowController>(scene);
            LevelRuntimeConfigurator runtimeConfigurator = FindComponentInScene<LevelRuntimeConfigurator>(scene);
            StrongholdSequenceController sequence = FindComponentInScene<StrongholdSequenceController>(scene);
            BossSpawnPoint bossSpawnPoint = FindComponentInScene<BossSpawnPoint>(scene);
            List<StrongholdController> allStrongholds = FindComponentsInScene<StrongholdController>(scene);
            strongholdCount = allStrongholds.Count;

            bool hasPlayerCombat = FindComponentInScene<PlayerCombat>(scene) != null;
            bool hasPlayerHealth = FindComponentInScene<PlayerHealth>(scene) != null;
            bool hasPlayerInput = FindComponentInScene<PlayerInputHandler>(scene) != null;
            bool hasPlayerTag = FindTaggedObjectInScene(scene, "Player") != null;

            if (hasPlayerCombat)
            {
                playerAnchor = "PlayerCombat";
            }
            else if (hasPlayerHealth)
            {
                playerAnchor = "PlayerHealth";
            }
            else if (hasPlayerTag)
            {
                playerAnchor = "Tag:Player";
            }
            else if (hasPlayerInput)
            {
                playerAnchor = "PlayerInputHandler";
            }

            if (string.IsNullOrWhiteSpace(playerAnchor))
            {
                issues.Add(ValidationIssue.Blocking("No player anchor found (PlayerCombat/PlayerHealth/Tag:Player)."));
            }

            int expectedRuntimeLevelId = ResolveRuntimeLevelId(entry.levelData);

            if (runtimeConfigurator == null && applyFix && levelFlow != null)
            {
                runtimeConfigurator = levelFlow.GetComponent<LevelRuntimeConfigurator>();
                if (runtimeConfigurator == null)
                {
                    runtimeConfigurator = levelFlow.gameObject.AddComponent<LevelRuntimeConfigurator>();
                    sceneDirty = true;
                    fixedCount++;
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
                    issues.Add(ValidationIssue.Blocking("LevelFlow.levelData does not reference current LevelData asset."));
                }

                if (expectedRuntimeLevelId > 0 && levelFlow.levelId != expectedRuntimeLevelId)
                {
                    issues.Add(ValidationIssue.Blocking(
                        $"LevelFlow.levelId mismatch (expected {expectedRuntimeLevelId}, actual {levelFlow.levelId})."));
                }

                if (runtimeConfigurator != null && runtimeConfigurator.levelFlow != null &&
                    runtimeConfigurator.levelFlow != levelFlow)
                {
                    if (applyFix)
                    {
                        runtimeConfigurator.levelFlow = levelFlow;
                        sceneDirty = true;
                        fixedCount++;
                    }
                    else
                    {
                        issues.Add(ValidationIssue.Blocking("RuntimeConfigurator.levelFlow points to a different LevelFlowController."));
                    }
                }

                if (levelFlow.runtimeConfigurator == null)
                {
                    if (applyFix && runtimeConfigurator != null)
                    {
                        levelFlow.runtimeConfigurator = runtimeConfigurator;
                        sceneDirty = true;
                        fixedCount++;
                    }
                    else
                    {
                        issues.Add(ValidationIssue.Warning("LevelFlow.runtimeConfigurator is null (runtime auto-create fallback in use)."));
                    }
                }
                else if (runtimeConfigurator != null && levelFlow.runtimeConfigurator != runtimeConfigurator)
                {
                    if (applyFix)
                    {
                        levelFlow.runtimeConfigurator = runtimeConfigurator;
                        sceneDirty = true;
                        fixedCount++;
                    }
                    else
                    {
                        issues.Add(ValidationIssue.Warning("LevelFlow.runtimeConfigurator does not match scene RuntimeConfigurator."));
                    }
                }

                if (entry.levelData.quests != null && entry.levelData.quests.Count > 0 && levelFlow.questDatabase == null)
                {
                    issues.Add(ValidationIssue.Warning("LevelData has quests but LevelFlow.questDatabase is null."));
                }
            }

            if (runtimeConfigurator == null)
            {
                issues.Add(ValidationIssue.Warning("Missing LevelRuntimeConfigurator (runtime fallback path will auto-add one)."));
            }
            else
            {
                if (runtimeConfigurator.levelData != null && runtimeConfigurator.levelData != entry.levelData)
                {
                    if (applyFix)
                    {
                        runtimeConfigurator.levelData = entry.levelData;
                        sceneDirty = true;
                        fixedCount++;
                    }
                    else
                    {
                        issues.Add(ValidationIssue.Blocking("RuntimeConfigurator.levelData references a different LevelData."));
                    }
                }
                else if (runtimeConfigurator.levelData == null && applyFix)
                {
                    runtimeConfigurator.levelData = entry.levelData;
                    sceneDirty = true;
                    fixedCount++;
                }

                if (levelFlow != null && runtimeConfigurator.levelFlow == null && applyFix)
                {
                    runtimeConfigurator.levelFlow = levelFlow;
                    sceneDirty = true;
                    fixedCount++;
                }

                if (sequence != null && runtimeConfigurator.sequenceController != null &&
                    runtimeConfigurator.sequenceController != sequence)
                {
                    if (applyFix)
                    {
                        runtimeConfigurator.sequenceController = sequence;
                        sceneDirty = true;
                        fixedCount++;
                    }
                    else
                    {
                        issues.Add(ValidationIssue.Warning("RuntimeConfigurator.sequenceController does not match scene StrongholdSequenceController."));
                    }
                }
                else if (sequence != null && runtimeConfigurator.sequenceController == null && applyFix)
                {
                    runtimeConfigurator.sequenceController = sequence;
                    sceneDirty = true;
                    fixedCount++;
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
                        StrongholdController linkedStronghold = sequence.strongholds[i];
                        if (linkedStronghold == null)
                        {
                            issues.Add(ValidationIssue.Blocking($"StrongholdSequence.strongholds[{i}] is null."));
                        }
                        else if (!allStrongholds.Contains(linkedStronghold))
                        {
                            issues.Add(ValidationIssue.Warning(
                                $"StrongholdSequence.strongholds[{i}] is not part of scene stronghold set."));
                        }
                    }
                }

                if (!sequence.triggerLevelCompleteOnFinish)
                {
                    issues.Add(ValidationIssue.Warning("StrongholdSequence.triggerLevelCompleteOnFinish is false."));
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

            if (allStrongholds.Count == 0)
            {
                issues.Add(ValidationIssue.Blocking("No StrongholdController found in scene."));
            }

            var strongholdById = new Dictionary<string, StrongholdController>(StringComparer.Ordinal);
            for (int i = 0; i < allStrongholds.Count; i++)
            {
                StrongholdController stronghold = allStrongholds[i];
                if (stronghold == null)
                {
                    continue;
                }

                string strongholdId = stronghold.StrongholdId;
                if (string.IsNullOrWhiteSpace(strongholdId))
                {
                    issues.Add(ValidationIssue.Blocking($"Stronghold '{stronghold.name}' has empty StrongholdId."));
                }
                else if (strongholdById.ContainsKey(strongholdId))
                {
                    issues.Add(ValidationIssue.Blocking($"Duplicate stronghold id in scene: {strongholdId}."));
                }
                else
                {
                    strongholdById.Add(strongholdId, stronghold);
                }

                if (stronghold.waves == null || stronghold.waves.Count == 0)
                {
                    issues.Add(ValidationIssue.Blocking($"Stronghold '{stronghold.name}' has no waves."));
                    continue;
                }

                bool hasSpawnPoints = stronghold.spawnPoints != null && stronghold.spawnPoints.Count > 0;
                if (!hasSpawnPoints)
                {
                    issues.Add(ValidationIssue.Warning($"Stronghold '{stronghold.name}' has no explicit spawn points."));
                }

                for (int w = 0; w < stronghold.waves.Count; w++)
                {
                    StrongholdWave wave = stronghold.waves[w];
                    if (wave == null)
                    {
                        issues.Add(ValidationIssue.Blocking(
                            $"Stronghold '{stronghold.name}' wave[{w}] is null."));
                        continue;
                    }

                    waveCount++;
                    bool hasValidGroup = false;
                    if (wave.groups != null)
                    {
                        for (int g = 0; g < wave.groups.Count; g++)
                        {
                            WaveSpawnGroup group = wave.groups[g];
                            if (group != null && group.prefab != null && group.count > 0)
                            {
                                hasValidGroup = true;
                                break;
                            }
                        }
                    }

                    if (!hasValidGroup)
                    {
                        issues.Add(ValidationIssue.Warning(
                            $"Stronghold '{stronghold.name}' wave[{w}] has no valid spawn group prefab."));
                    }
                }
            }

            if (entry.levelData.strongholds == null || entry.levelData.strongholds.Count == 0)
            {
                issues.Add(ValidationIssue.Warning("LevelData.strongholds is empty."));
            }
            else
            {
                for (int i = 0; i < entry.levelData.strongholds.Count; i++)
                {
                    StrongholdConfig config = entry.levelData.strongholds[i];
                    if (config == null || string.IsNullOrWhiteSpace(config.strongholdId))
                    {
                        issues.Add(ValidationIssue.Blocking($"LevelData.strongholds[{i}] has empty strongholdId."));
                        continue;
                    }

                    bool required = config.required;
                    bool existsInScene = strongholdById.ContainsKey(config.strongholdId);
                    if (required && !existsInScene)
                    {
                        issues.Add(ValidationIssue.Blocking(
                            $"Required LevelData stronghold '{config.strongholdId}' not found in scene."));
                    }
                    else if (!required && !existsInScene)
                    {
                        issues.Add(ValidationIssue.Warning(
                            $"Optional LevelData stronghold '{config.strongholdId}' not found in scene."));
                    }
                }
            }

            bool expectBossGate = entry.levelData.overrideBossSettings && bossSpawnPoint != null;
            if (entry.levelData.overrideBossSettings && bossSpawnPoint == null)
            {
                issues.Add(ValidationIssue.Blocking("LevelData requires boss gate but BossSpawnPoint is missing."));
            }

            if (bossSpawnPoint != null && bossSpawnPoint.spawnOnStart)
            {
                issues.Add(ValidationIssue.Blocking("BossSpawnPoint.spawnOnStart should be false."));
            }

            if (sequence != null)
            {
                if (sequence.deferCompletionUntilBoss != expectBossGate)
                {
                    issues.Add(ValidationIssue.Blocking(
                        $"StrongholdSequence.deferCompletionUntilBoss mismatch (expected {expectBossGate})."));
                }

                BossSpawnPoint expectedBossRef = expectBossGate ? bossSpawnPoint : null;
                if (sequence.bossSpawnPoint != expectedBossRef)
                {
                    issues.Add(ValidationIssue.Blocking("StrongholdSequence.bossSpawnPoint mismatch."));
                }
            }

            if (entry.levelData.quests != null && entry.levelData.quests.Count > 0)
            {
                QuestDatabase questDatabase = null;
                if (levelFlow != null && levelFlow.questDatabase != null)
                {
                    questDatabase = levelFlow.questDatabase;
                }
                else if (runtimeConfigurator != null && runtimeConfigurator.questDatabase != null)
                {
                    questDatabase = runtimeConfigurator.questDatabase;
                }

                if (questDatabase == null)
                {
                    issues.Add(ValidationIssue.Warning("LevelData has quests but no QuestDatabase reference found in scene wiring."));
                }
                else
                {
                    for (int i = 0; i < entry.levelData.quests.Count; i++)
                    {
                        QuestConfig quest = entry.levelData.quests[i];
                        if (quest == null || string.IsNullOrWhiteSpace(quest.questId))
                        {
                            continue;
                        }

                        if (questDatabase.GetQuestById(quest.questId) == null)
                        {
                            issues.Add(ValidationIssue.Warning(
                                $"QuestDatabase missing quest id '{quest.questId}'."));
                        }
                    }
                }
            }

            if (applyFix && sceneDirty)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                if (EditorSceneManager.SaveScene(scene))
                {
                    fixedCount++;
                }
                else
                {
                    issues.Add(ValidationIssue.Blocking("SaveScene returned false during fix."));
                }
            }

            return BuildRow(entry, scenePath, buildIndex, strongholdCount, waveCount, playerAnchor, fixedCount, issues);
        }

        private static ValidationRow BuildRow(
            LevelEntry entry,
            string scenePath,
            int buildIndex,
            int strongholdCount,
            int waveCount,
            string playerAnchor,
            int fixedCount,
            List<ValidationIssue> issues)
        {
            int blocking = 0;
            int warnings = 0;
            var notes = new List<string>();
            if (issues != null)
            {
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
            }

            string status = blocking > 0 ? "Error" : (fixedCount > 0 ? "Fixed" : "Ok");
            return new ValidationRow
            {
                levelId = entry.levelData != null ? entry.levelData.levelId : string.Empty,
                levelAssetPath = entry.levelAssetPath ?? string.Empty,
                sceneName = entry.levelData != null ? entry.levelData.sceneName : string.Empty,
                scenePath = scenePath ?? string.Empty,
                status = status,
                buildIndex = buildIndex,
                fixedCount = fixedCount,
                blockingErrors = blocking,
                warnings = warnings,
                strongholdCount = strongholdCount,
                waveCount = waveCount,
                playerAnchor = playerAnchor ?? string.Empty,
                note = notes.Count > 0 ? string.Join(" ", notes) : string.Empty
            };
        }

        private static List<LevelEntry> CollectLevelEntries()
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

                string fileName = Path.GetFileNameWithoutExtension(assetPath);
                if (string.IsNullOrWhiteSpace(fileName) ||
                    !fileName.StartsWith("LevelData_Level", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!IsFormalLevelId(levelData.levelId))
                {
                    continue;
                }

                int parsedIndex = ParseLevelIndex(levelData.levelId);
                if (parsedIndex <= 0)
                {
                    continue;
                }

                result.Add(new LevelEntry
                {
                    levelData = levelData,
                    levelAssetPath = assetPath,
                    levelIndex = parsedIndex
                });
            }

            result.Sort((a, b) =>
            {
                int left = a.levelIndex > 0 ? a.levelIndex : int.MaxValue;
                int right = b.levelIndex > 0 ? b.levelIndex : int.MaxValue;
                int cmp = left.CompareTo(right);
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
            csv.AppendLine("level_id,level_asset,scene_name,scene_path,status,build_index,fixed,blocking_errors,warnings,strongholds,total_waves,player_anchor,note");

            for (int i = 0; i < rows.Count; i++)
            {
                ValidationRow row = rows[i];
                csv.Append(EscapeCsv(row.levelId)).Append(',')
                    .Append(EscapeCsv(row.levelAssetPath)).Append(',')
                    .Append(EscapeCsv(row.sceneName)).Append(',')
                    .Append(EscapeCsv(row.scenePath)).Append(',')
                    .Append(EscapeCsv(row.status)).Append(',')
                    .Append(row.buildIndex).Append(',')
                    .Append(row.fixedCount).Append(',')
                    .Append(row.blockingErrors).Append(',')
                    .Append(row.warnings).Append(',')
                    .Append(row.strongholdCount).Append(',')
                    .Append(row.waveCount).Append(',')
                    .Append(EscapeCsv(row.playerAnchor)).Append(',')
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

            int errorSceneCount = 0;
            int fixedTotal = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                if (string.Equals(rows[i].status, "Error", StringComparison.Ordinal))
                {
                    errorSceneCount++;
                }

                fixedTotal += rows[i].fixedCount;
            }

            var md = new StringBuilder();
            md.AppendLine("# Level Content Completeness Summary");
            md.AppendLine();
            md.AppendLine($"- Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss zzz}");
            md.AppendLine($"- Targets: {rows.Count}");
            md.AppendLine($"- Error Scenes: {errorSceneCount}");
            md.AppendLine($"- Fixed: {fixedTotal}");
            md.AppendLine($"- Blocking Errors: {blockingTotal}");
            md.AppendLine($"- Warnings: {warningTotal}");
            md.AppendLine($"- CSV: {ReportCsvPath}");
            md.AppendLine();
            md.AppendLine("| Level | Scene | Status | Fixed | Blocking | Warnings | Strongholds | Waves | Player | Note |");
            md.AppendLine("|---|---|---|---:|---:|---:|---:|---:|---|---|");

            for (int i = 0; i < rows.Count; i++)
            {
                ValidationRow row = rows[i];
                string shortNote = TrimForMarkdownTable(row.note, 180);
                md.Append("| ")
                    .Append(SafeMarkdownCell(row.levelId)).Append(" | ")
                    .Append(SafeMarkdownCell(row.sceneName)).Append(" | ")
                    .Append(SafeMarkdownCell(row.status)).Append(" | ")
                    .Append(row.fixedCount).Append(" | ")
                    .Append(row.blockingErrors).Append(" | ")
                    .Append(row.warnings).Append(" | ")
                    .Append(row.strongholdCount).Append(" | ")
                    .Append(row.waveCount).Append(" | ")
                    .Append(SafeMarkdownCell(row.playerAnchor)).Append(" | ")
                    .Append(SafeMarkdownCell(shortNote)).Append(" |")
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

        private static int ResolveRuntimeLevelId(LevelData levelData)
        {
            if (levelData == null)
            {
                return 0;
            }

            int levelIndex = ParseLevelIndex(levelData.levelId);
            if (levelData.chapterId > 0 && levelIndex > 0)
            {
                return levelData.chapterId * 100 + levelIndex;
            }

            return 0;
        }

        private static bool IsFormalLevelId(string levelId)
        {
            if (string.IsNullOrWhiteSpace(levelId))
            {
                return false;
            }

            return levelId.StartsWith("LEVEL_", StringComparison.OrdinalIgnoreCase);
        }

        private static int ParseLevelIndex(string levelId)
        {
            if (!IsFormalLevelId(levelId))
            {
                return -1;
            }

            string numberPart = levelId.Substring("LEVEL_".Length);
            if (int.TryParse(numberPart, out int parsed))
            {
                return parsed;
            }

            return -1;
        }

        private static bool TryGetEnabledBuildIndex(string scenePath, out int buildIndex, out bool presentButDisabled)
        {
            buildIndex = -1;
            presentButDisabled = false;
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                return false;
            }

            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                EditorBuildSettingsScene scene = scenes[i];
                if (!string.Equals(scene.path, scenePath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (scene.enabled)
                {
                    buildIndex = i;
                    return true;
                }

                presentButDisabled = true;
            }

            return false;
        }

        private static bool AssetExists(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return false;
            }

            UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            return asset != null;
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

        private static GameObject FindTaggedObjectInScene(Scene scene, string tag)
        {
            if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrWhiteSpace(tag))
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

                Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
                for (int t = 0; t < transforms.Length; t++)
                {
                    Transform tr = transforms[t];
                    if (tr == null || tr.gameObject == null)
                    {
                        continue;
                    }

                    try
                    {
                        if (tr.gameObject.CompareTag(tag))
                        {
                            return tr.gameObject;
                        }
                    }
                    catch (UnityException)
                    {
                        return null;
                    }
                }
            }

            return null;
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

        private static string TrimForMarkdownTable(string value, int maxChars)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            if (value.Length <= maxChars)
            {
                return value;
            }

            return value.Substring(0, Math.Max(0, maxChars - 3)) + "...";
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

        private struct ValidationRow
        {
            public string levelId;
            public string levelAssetPath;
            public string sceneName;
            public string scenePath;
            public string status;
            public int buildIndex;
            public int fixedCount;
            public int blockingErrors;
            public int warnings;
            public int strongholdCount;
            public int waveCount;
            public string playerAnchor;
            public string note;
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
    }
}
