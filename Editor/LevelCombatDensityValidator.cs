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
    public static class LevelCombatDensityValidator
    {
        private const string ValidateMenuPath = "Tools/Level/P0/Validate Level Combat Density (CSV)";
        private const string ValidateGateMenuPath = "Tools/Level/P0/Validate Level Combat Density (CI Gate)";
        private const string FixMenuPath = "Tools/Level/P0/Fix Level Combat Density 02-10";

        private const string SceneFolderPath = "Assets/Scenes";
        private const string ReportCsvPath = "Assets/ThirdPersonController/Reports/level_combat_density_gap_report.csv";
        private const string SummaryMdPath = "Assets/ThirdPersonController/Reports/level_combat_density_gap_summary.md";
        private const string LogPrefix = "[LevelCombatDensity]";
        private const int MinLevelIndex = 2;
        private const int MaxLevelIndex = 10;

        [MenuItem(ValidateMenuPath)]
        public static void Validate()
        {
            Run(applyFix: false, interactive: true, failOnGate: false);
        }

        [MenuItem(ValidateGateMenuPath)]
        public static void ValidateCiGate()
        {
            Run(applyFix: false, interactive: false, failOnGate: true);
        }

        [MenuItem(FixMenuPath)]
        public static void Fix()
        {
            Run(applyFix: true, interactive: true, failOnGate: false);
        }

        public static void ValidateForBatch()
        {
            Run(applyFix: false, interactive: false, failOnGate: true);
        }

        public static void FixForBatch()
        {
            Run(applyFix: true, interactive: false, failOnGate: true);
        }

        private static void Run(bool applyFix, bool interactive, bool failOnGate)
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
                    EditorUtility.DisplayDialog("Level Combat Density", noneMessage, "OK");
                }

                return;
            }

            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
            var rows = new List<ValidationRow>(entries.Count);

            try
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    rows.Add(ProcessEntry(entries[i], applyFix));
                }
            }
            finally
            {
                if (originalSetup != null && originalSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
                }
            }

            int errorSceneCount = 0;
            int gapSceneCount = 0;
            int blockingTotal = 0;
            int gapTotal = 0;
            int fixedTotal = 0;

            for (int i = 0; i < rows.Count; i++)
            {
                ValidationRow row = rows[i];
                if (string.Equals(row.status, "Error", StringComparison.Ordinal))
                {
                    errorSceneCount++;
                }

                if (string.Equals(row.status, "Gap", StringComparison.Ordinal) ||
                    string.Equals(row.status, "Partial", StringComparison.Ordinal))
                {
                    gapSceneCount++;
                }

                blockingTotal += row.blockingErrors;
                gapTotal += row.gapCount;
                fixedTotal += row.fixedCount;
            }

            string csvPath = WriteCsv(rows);
            string summaryPath = WriteSummary(rows, errorSceneCount, gapSceneCount, blockingTotal, gapTotal, fixedTotal, applyFix);
            AssetDatabase.Refresh();

            string summary =
                $"mode={(applyFix ? "fix" : "validate")} targets={rows.Count} errorScenes={errorSceneCount} gapScenes={gapSceneCount} " +
                $"blocking={blockingTotal} gaps={gapTotal} fixed={fixedTotal} csv={csvPath} summary={summaryPath}";
            Debug.Log($"{LogPrefix} complete | {summary}");

            if (interactive)
            {
                EditorUtility.DisplayDialog("Level Combat Density", summary, "OK");
            }

            if (failOnGate && (blockingTotal > 0 || gapTotal > 0))
            {
                throw new InvalidOperationException($"{LogPrefix} gate failed. blocking={blockingTotal} gaps={gapTotal} csv={csvPath}");
            }
        }

        private static ValidationRow ProcessEntry(LevelEntry entry, bool applyFix)
        {
            var blockingNotes = new List<string>();
            var fixNotes = new List<string>();

            string scenePath = BuildScenePath(entry.levelData);
            var row = new ValidationRow
            {
                levelId = entry.levelData != null ? entry.levelData.levelId : string.Empty,
                levelAssetPath = entry.levelAssetPath ?? string.Empty,
                sceneName = entry.levelData != null ? entry.levelData.sceneName : string.Empty,
                scenePath = scenePath ?? string.Empty
            };

            if (entry.levelData == null)
            {
                row.status = "Error";
                row.blockingErrors = 1;
                row.note = "LevelData asset is null.";
                return row;
            }

            if (string.IsNullOrWhiteSpace(scenePath))
            {
                row.status = "Error";
                row.blockingErrors = 1;
                row.note = "LevelData.sceneName is empty.";
                return row;
            }

            if (!AssetExists(scenePath))
            {
                row.status = "Error";
                row.blockingErrors = 1;
                row.note = "Scene asset is missing.";
                return row;
            }

            Scene scene;
            try
            {
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }
            catch (Exception ex)
            {
                row.status = "Error";
                row.blockingErrors = 1;
                row.note = $"OpenScene failed: {ex.Message}";
                return row;
            }

            StrongholdSequenceController sequence = FindComponentInScene<StrongholdSequenceController>(scene);
            List<StrongholdController> strongholds = FindComponentsInScene<StrongholdController>(scene);
            if (sequence == null)
            {
                blockingNotes.Add("Missing StrongholdSequenceController.");
            }

            if (strongholds.Count == 0)
            {
                blockingNotes.Add("No StrongholdController found in scene.");
            }

            DensityMetrics beforeMetrics = EvaluateMetrics(sequence, strongholds);
            DensityTarget target = BuildTarget(entry.levelIndex, beforeMetrics.totalWaves);

            int fixedCount = 0;
            bool sceneChanged = false;

            if (applyFix && blockingNotes.Count == 0)
            {
                fixedCount += ApplySceneFix(entry, sequence, strongholds, target, fixNotes, ref sceneChanged);
                if (sceneChanged)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (EditorSceneManager.SaveScene(scene))
                    {
                        fixedCount++;
                    }
                    else
                    {
                        blockingNotes.Add("SaveScene returned false during fix.");
                    }
                }
            }

            DensityMetrics metrics = (applyFix && sceneChanged)
                ? EvaluateMetrics(sequence, strongholds)
                : beforeMetrics;
            List<string> gapNotes = BuildGapNotes(metrics, target);

            row.fixedCount = fixedCount;
            row.blockingErrors = blockingNotes.Count;
            row.gapCount = gapNotes.Count;
            row.strongholdCount = metrics.strongholdCount;
            row.totalWaves = metrics.totalWaves;
            row.totalGroups = metrics.totalGroups;
            row.totalBaseSpawn = metrics.totalBaseSpawn;
            row.waveEventCoverage = metrics.totalWaves > 0 ? (float)metrics.wavesWithEvents / metrics.totalWaves : 0f;
            row.totalEvents = metrics.totalEvents;
            row.reinforcementEvents = metrics.reinforcementEvents;
            row.chaseEvents = metrics.chaseEvents;
            row.holdEvents = metrics.holdEvents;
            row.protectEvents = metrics.protectEvents;
            row.uniqueEventTypes = metrics.uniqueEventTypes;
            row.completionReady = metrics.completionReady;

            row.targetMinWaves = target.minTotalWaves;
            row.targetMinBaseSpawn = target.minBaseSpawnTotal;
            row.targetMinEventCoverage = target.minEventCoverage;
            row.targetMinUniqueTypes = target.minUniqueEventTypes;
            row.targetMinChase = target.minChaseEvents;
            row.targetMinHold = target.minHoldEvents;
            row.targetMinProtect = target.minProtectEvents;

            if (row.blockingErrors > 0)
            {
                row.status = "Error";
            }
            else if (row.gapCount > 0)
            {
                row.status = fixedCount > 0 ? "Partial" : "Gap";
            }
            else
            {
                row.status = fixedCount > 0 ? "Fixed" : "Ok";
            }

            var notes = new List<string>();
            if (blockingNotes.Count > 0)
            {
                notes.Add("[B] " + string.Join(" [B] ", blockingNotes));
            }

            if (gapNotes.Count > 0)
            {
                notes.Add("[G] " + string.Join(" [G] ", gapNotes));
            }

            if (fixNotes.Count > 0)
            {
                notes.Add("[F] " + string.Join(" [F] ", fixNotes));
            }

            row.note = notes.Count > 0 ? string.Join(" ", notes) : string.Empty;
            return row;
        }

        private static DensityMetrics EvaluateMetrics(
            StrongholdSequenceController sequence,
            List<StrongholdController> strongholds)
        {
            var metrics = new DensityMetrics();
            if (strongholds != null)
            {
                for (int s = 0; s < strongholds.Count; s++)
                {
                    StrongholdController stronghold = strongholds[s];
                    if (stronghold == null)
                    {
                        continue;
                    }

                    metrics.strongholdCount++;
                    List<StrongholdWave> waves = stronghold.waves ?? new List<StrongholdWave>();
                    for (int w = 0; w < waves.Count; w++)
                    {
                        StrongholdWave wave = waves[w];
                        if (wave == null)
                        {
                            continue;
                        }

                        metrics.totalWaves++;
                        bool hasWaveEvent = false;

                        if (wave.groups != null)
                        {
                            for (int g = 0; g < wave.groups.Count; g++)
                            {
                                WaveSpawnGroup group = wave.groups[g];
                                if (group == null)
                                {
                                    continue;
                                }

                                metrics.totalGroups++;
                                if (group.prefab != null && group.count > 0)
                                {
                                    metrics.totalBaseSpawn += group.count;
                                }
                            }
                        }

                        if (wave.events != null)
                        {
                            for (int e = 0; e < wave.events.Count; e++)
                            {
                                WaveEvent waveEvent = wave.events[e];
                                if (waveEvent == null || !waveEvent.enabled)
                                {
                                    continue;
                                }

                                hasWaveEvent = true;
                                metrics.totalEvents++;
                                switch (waveEvent.eventType)
                                {
                                    case WaveEventType.Reinforcement:
                                        metrics.reinforcementEvents++;
                                        break;
                                    case WaveEventType.Chase:
                                        metrics.chaseEvents++;
                                        break;
                                    case WaveEventType.HoldPoint:
                                        metrics.holdEvents++;
                                        break;
                                    case WaveEventType.ProtectTarget:
                                        metrics.protectEvents++;
                                        break;
                                }
                            }
                        }

                        if (hasWaveEvent)
                        {
                            metrics.wavesWithEvents++;
                        }
                    }
                }
            }

            metrics.uniqueEventTypes = 0;
            if (metrics.reinforcementEvents > 0) metrics.uniqueEventTypes++;
            if (metrics.chaseEvents > 0) metrics.uniqueEventTypes++;
            if (metrics.holdEvents > 0) metrics.uniqueEventTypes++;
            if (metrics.protectEvents > 0) metrics.uniqueEventTypes++;

            metrics.completionReady =
                sequence != null &&
                sequence.triggerLevelCompleteOnFinish &&
                sequence.triggerVictoryOnFinish &&
                sequence.strongholds != null &&
                sequence.strongholds.Count > 0;

            return metrics;
        }

        private static DensityTarget BuildTarget(int levelIndex, int totalWaves)
        {
            int band = levelIndex <= 4 ? 0 : (levelIndex <= 7 ? 1 : 2);
            int baselineWaves = Mathf.Max(totalWaves, 8);
            int minSpawnPerWave = 7 + band;
            float minCoverage = band == 0 ? 0.67f : (band == 1 ? 0.78f : 0.89f);

            return new DensityTarget
            {
                minTotalWaves = 8,
                minBaseSpawnTotal = baselineWaves * minSpawnPerWave,
                minEventCoverage = minCoverage,
                minUniqueEventTypes = band == 0 ? 2 : 3,
                minChaseEvents = 1,
                minHoldEvents = levelIndex >= 4 ? 1 : 0,
                minProtectEvents = levelIndex >= 6 ? 1 : 0
            };
        }

        private static List<string> BuildGapNotes(DensityMetrics metrics, DensityTarget target)
        {
            var gaps = new List<string>();
            float coverage = metrics.totalWaves > 0 ? (float)metrics.wavesWithEvents / metrics.totalWaves : 0f;

            if (metrics.totalWaves < target.minTotalWaves)
            {
                gaps.Add($"waves<{target.minTotalWaves} (actual {metrics.totalWaves})");
            }

            if (metrics.totalBaseSpawn < target.minBaseSpawnTotal)
            {
                gaps.Add($"base_spawn<{target.minBaseSpawnTotal} (actual {metrics.totalBaseSpawn})");
            }

            if (coverage + 0.0001f < target.minEventCoverage)
            {
                gaps.Add($"event_coverage<{target.minEventCoverage:0.00} (actual {coverage:0.00})");
            }

            if (metrics.uniqueEventTypes < target.minUniqueEventTypes)
            {
                gaps.Add($"unique_event_types<{target.minUniqueEventTypes} (actual {metrics.uniqueEventTypes})");
            }

            if (metrics.chaseEvents < target.minChaseEvents)
            {
                gaps.Add($"chase_events<{target.minChaseEvents} (actual {metrics.chaseEvents})");
            }

            if (metrics.holdEvents < target.minHoldEvents)
            {
                gaps.Add($"hold_events<{target.minHoldEvents} (actual {metrics.holdEvents})");
            }

            if (metrics.protectEvents < target.minProtectEvents)
            {
                gaps.Add($"protect_events<{target.minProtectEvents} (actual {metrics.protectEvents})");
            }

            if (!metrics.completionReady)
            {
                gaps.Add("completion_trigger_not_ready");
            }

            return gaps;
        }

        private static int ApplySceneFix(
            LevelEntry entry,
            StrongholdSequenceController sequence,
            List<StrongholdController> strongholds,
            DensityTarget target,
            List<string> fixNotes,
            ref bool sceneChanged)
        {
            int ops = 0;
            if (sequence != null)
            {
                if (!sequence.triggerLevelCompleteOnFinish)
                {
                    sequence.triggerLevelCompleteOnFinish = true;
                    sceneChanged = true;
                    ops++;
                }

                if (!sequence.triggerVictoryOnFinish)
                {
                    sequence.triggerVictoryOnFinish = true;
                    sceneChanged = true;
                    ops++;
                }

                if (!sequence.autoStartFirst)
                {
                    sequence.autoStartFirst = true;
                    sceneChanged = true;
                    ops++;
                }

                int expectedLevelId = ResolveRuntimeLevelId(entry.levelData, entry.levelIndex);
                if (expectedLevelId > 0 && sequence.levelId != expectedLevelId)
                {
                    sequence.levelId = expectedLevelId;
                    sceneChanged = true;
                    ops++;
                }
            }

            for (int s = 0; s < strongholds.Count; s++)
            {
                StrongholdController stronghold = strongholds[s];
                if (stronghold == null || stronghold.waves == null || stronghold.waves.Count == 0)
                {
                    continue;
                }

                ops += ApplyStrongholdFix(stronghold, entry.levelIndex, ref sceneChanged);
            }

            ops += EnsureSceneEventCoverage(strongholds, entry.levelIndex, target, ref sceneChanged);

            if (ops > 0)
            {
                fixNotes.Add($"ops={ops}");
            }

            return ops;
        }

        private static int ApplyStrongholdFix(
            StrongholdController stronghold,
            int levelIndex,
            ref bool changed)
        {
            int ops = 0;
            GameObject fallbackPrefab = ResolveFallbackPrefab(stronghold);
            int totalWaves = stronghold.waves != null ? stronghold.waves.Count : 0;
            if (totalWaves <= 0)
            {
                return ops;
            }

            for (int w = 0; w < totalWaves; w++)
            {
                StrongholdWave wave = stronghold.waves[w];
                if (wave == null)
                {
                    continue;
                }

                ops += EnsureWaveSpawnDensity(stronghold, wave, fallbackPrefab, levelIndex, w, totalWaves, ref changed);
                ops += NormalizeWaveEvents(stronghold, wave, fallbackPrefab, levelIndex, w, totalWaves, ref changed);
            }

            if (stronghold.waveCompleteDelay > 1.2f)
            {
                stronghold.waveCompleteDelay = 1f;
                changed = true;
                ops++;
            }

            return ops;
        }

        private static int EnsureWaveSpawnDensity(
            StrongholdController stronghold,
            StrongholdWave wave,
            GameObject fallbackPrefab,
            int levelIndex,
            int waveIndex,
            int totalWaves,
            ref bool changed)
        {
            int ops = 0;
            if (wave.groups == null)
            {
                wave.groups = new List<WaveSpawnGroup>();
                changed = true;
                ops++;
            }

            int targetTotal = ComputeWaveSpawnTarget(levelIndex, waveIndex, totalWaves);
            int currentTotal = 0;
            int firstValidGroupIndex = -1;

            for (int i = 0; i < wave.groups.Count; i++)
            {
                WaveSpawnGroup group = wave.groups[i];
                if (group == null || group.prefab == null)
                {
                    continue;
                }

                if (group.count < 0)
                {
                    group.count = 0;
                    changed = true;
                    ops++;
                }

                if (firstValidGroupIndex < 0)
                {
                    firstValidGroupIndex = i;
                }

                currentTotal += group.count;
            }

            if (firstValidGroupIndex < 0 && fallbackPrefab != null)
            {
                wave.groups.Add(new WaveSpawnGroup
                {
                    prefab = fallbackPrefab,
                    count = targetTotal
                });
                firstValidGroupIndex = wave.groups.Count - 1;
                currentTotal = targetTotal;
                changed = true;
                ops++;
            }

            if (firstValidGroupIndex >= 0 && currentTotal < targetTotal)
            {
                int delta = targetTotal - currentTotal;
                WaveSpawnGroup targetGroup = wave.groups[firstValidGroupIndex];
                targetGroup.count += delta;
                changed = true;
                ops++;
            }

            float intervalTarget = ComputeWaveSpawnInterval(levelIndex, waveIndex, totalWaves);
            if (wave.spawnInterval <= 0f || wave.spawnInterval > intervalTarget + 0.12f)
            {
                wave.spawnInterval = intervalTarget;
                changed = true;
                ops++;
            }

            return ops;
        }

        private static int NormalizeWaveEvents(
            StrongholdController stronghold,
            StrongholdWave wave,
            GameObject fallbackPrefab,
            int levelIndex,
            int waveIndex,
            int totalWaves,
            ref bool changed)
        {
            int ops = 0;
            if (wave.events == null)
            {
                wave.events = new List<WaveEvent>();
                changed = true;
                ops++;
            }

            for (int i = 0; i < wave.events.Count; i++)
            {
                WaveEvent waveEvent = wave.events[i];
                if (waveEvent == null)
                {
                    continue;
                }

                if (!waveEvent.enabled)
                {
                    waveEvent.enabled = true;
                    changed = true;
                    ops++;
                }

                if (waveEvent.triggerDelay < 0f)
                {
                    waveEvent.triggerDelay = 0f;
                    changed = true;
                    ops++;
                }

                float defaultDuration = DefaultEventDuration(waveEvent.eventType, waveIndex);
                if (waveEvent.duration <= 0f)
                {
                    waveEvent.duration = defaultDuration;
                    changed = true;
                    ops++;
                }

                float defaultInterval = DefaultEventInterval(waveEvent.eventType, levelIndex);
                if (waveEvent.spawnInterval <= 0f)
                {
                    waveEvent.spawnInterval = defaultInterval;
                    changed = true;
                    ops++;
                }

                if (waveEvent.spawnRadius <= 0f)
                {
                    waveEvent.spawnRadius = Mathf.Max(3f, stronghold.spawnRadius);
                    changed = true;
                    ops++;
                }

                if (waveEvent.eventType == WaveEventType.HoldPoint)
                {
                    if (waveEvent.holdRadius <= 0f)
                    {
                        waveEvent.holdRadius = Mathf.Max(3f, stronghold.spawnRadius * 0.75f);
                        changed = true;
                        ops++;
                    }

                    if (waveEvent.holdDuration <= 0f)
                    {
                        waveEvent.holdDuration = 6f + waveIndex;
                        changed = true;
                        ops++;
                    }

                    if (waveEvent.holdDecayRate <= 0f)
                    {
                        waveEvent.holdDecayRate = 1f;
                        changed = true;
                        ops++;
                    }

                    if (waveEvent.holdPoint == null && stronghold.center != null)
                    {
                        waveEvent.holdPoint = stronghold.center;
                        changed = true;
                        ops++;
                    }
                }
                else if (waveEvent.eventType == WaveEventType.ProtectTarget)
                {
                    if (!waveEvent.spawnDefenseTarget)
                    {
                        waveEvent.spawnDefenseTarget = true;
                        changed = true;
                        ops++;
                    }

                    if (waveEvent.defenseTargetHealth <= 0)
                    {
                        waveEvent.defenseTargetHealth = 220 + levelIndex * 20 + waveIndex * 10;
                        changed = true;
                        ops++;
                    }

                    if (waveEvent.holdPoint == null && stronghold.center != null)
                    {
                        waveEvent.holdPoint = stronghold.center;
                        changed = true;
                        ops++;
                    }
                }

                if (waveEvent.eventType != WaveEventType.HoldPoint)
                {
                    if (waveEvent.groups == null)
                    {
                        waveEvent.groups = new List<WaveSpawnGroup>();
                        changed = true;
                        ops++;
                    }

                    bool hasValidSpawnGroup = false;
                    for (int g = 0; g < waveEvent.groups.Count; g++)
                    {
                        WaveSpawnGroup group = waveEvent.groups[g];
                        if (group == null || group.prefab == null)
                        {
                            continue;
                        }

                        if (group.count <= 0)
                        {
                            group.count = Mathf.Max(1, ComputeEventSpawnCount(levelIndex, waveIndex, waveEvent.eventType));
                            changed = true;
                            ops++;
                        }

                        hasValidSpawnGroup = true;
                    }

                    if (!hasValidSpawnGroup && fallbackPrefab != null)
                    {
                        waveEvent.groups.Add(new WaveSpawnGroup
                        {
                            prefab = fallbackPrefab,
                            count = ComputeEventSpawnCount(levelIndex, waveIndex, waveEvent.eventType)
                        });
                        changed = true;
                        ops++;
                    }
                }
            }

            return ops;
        }

        private static int EnsureSceneEventCoverage(
            List<StrongholdController> strongholds,
            int levelIndex,
            DensityTarget target,
            ref bool changed)
        {
            List<WaveRef> waveRefs = BuildWaveRefs(strongholds);
            if (waveRefs.Count == 0)
            {
                return 0;
            }

            int ops = 0;
            int wavesWithEvents = 0;
            for (int i = 0; i < waveRefs.Count; i++)
            {
                if (HasEnabledEvent(waveRefs[i].wave))
                {
                    wavesWithEvents++;
                }
            }

            int requiredCoverageWaves = Mathf.CeilToInt(waveRefs.Count * target.minEventCoverage);
            if (requiredCoverageWaves < 1 && waveRefs.Count > 0)
            {
                requiredCoverageWaves = 1;
            }

            for (int i = waveRefs.Count - 1; i >= 0 && wavesWithEvents < requiredCoverageWaves; i--)
            {
                WaveRef waveRef = waveRefs[i];
                if (HasEnabledEvent(waveRef.wave))
                {
                    continue;
                }

                WaveEventType type = PickWaveEventType(levelIndex, waveRef.globalWaveIndex);
                ops += AddAutoEvent(waveRef, levelIndex, type, ref changed);
                wavesWithEvents++;
            }

            EventTypeCounts counts = CountEventTypes(waveRefs);
            ops += EnsureEventTypeMinimum(waveRefs, levelIndex, WaveEventType.Chase, target.minChaseEvents, ref counts, ref changed);
            ops += EnsureEventTypeMinimum(waveRefs, levelIndex, WaveEventType.HoldPoint, target.minHoldEvents, ref counts, ref changed);
            ops += EnsureEventTypeMinimum(waveRefs, levelIndex, WaveEventType.ProtectTarget, target.minProtectEvents, ref counts, ref changed);

            while (counts.UniqueCount < target.minUniqueEventTypes)
            {
                WaveEventType missingType = FindMissingType(counts);
                WaveRef candidate = SelectWaveForEvent(waveRefs, missingType);
                if (candidate.wave == null)
                {
                    break;
                }

                ops += AddAutoEvent(candidate, levelIndex, missingType, ref changed);
                counts = CountEventTypes(waveRefs);
            }

            return ops;
        }

        private static int EnsureEventTypeMinimum(
            List<WaveRef> waveRefs,
            int levelIndex,
            WaveEventType eventType,
            int required,
            ref EventTypeCounts counts,
            ref bool changed)
        {
            if (required <= 0)
            {
                return 0;
            }

            int ops = 0;
            while (GetCountByType(counts, eventType) < required)
            {
                WaveRef candidate = SelectWaveForEvent(waveRefs, eventType);
                if (candidate.wave == null)
                {
                    break;
                }

                ops += AddAutoEvent(candidate, levelIndex, eventType, ref changed);
                counts = CountEventTypes(waveRefs);
            }

            return ops;
        }

        private static int AddAutoEvent(WaveRef waveRef, int levelIndex, WaveEventType eventType, ref bool changed)
        {
            if (waveRef.wave == null)
            {
                return 0;
            }

            if (waveRef.wave.events == null)
            {
                waveRef.wave.events = new List<WaveEvent>();
            }

            WaveEventType resolvedType = eventType;
            bool needsSpawnGroup = resolvedType != WaveEventType.HoldPoint;
            if (needsSpawnGroup && waveRef.fallbackPrefab == null)
            {
                resolvedType = WaveEventType.HoldPoint;
                needsSpawnGroup = false;
            }

            var waveEvent = new WaveEvent
            {
                name = $"Auto_{resolvedType}_{waveRef.globalWaveIndex + 1}",
                eventType = resolvedType,
                enabled = true,
                triggerDelay = waveRef.globalWaveIndex == 0 ? 0f : 0.3f,
                triggerOnRemaining = -1,
                duration = DefaultEventDuration(resolvedType, waveRef.localWaveIndex),
                spawnInterval = DefaultEventInterval(resolvedType, levelIndex),
                spawnRadius = Mathf.Max(3f, waveRef.stronghold != null ? waveRef.stronghold.spawnRadius : 5f),
                useReinforcementPoints = true,
                groups = new List<WaveSpawnGroup>(),
                holdRadius = Mathf.Max(3f, waveRef.stronghold != null ? waveRef.stronghold.spawnRadius * 0.75f : 4f),
                holdDuration = 6f + waveRef.localWaveIndex,
                holdDecayRate = 1f,
                showHoldMarker = true,
                spawnDefenseTarget = resolvedType == WaveEventType.ProtectTarget,
                defenseTargetHealth = 220 + levelIndex * 20 + waveRef.localWaveIndex * 10,
                failOnTargetDestroyed = true,
                assignTargetToSpawnedEnemies = true
            };

            if (waveRef.stronghold != null && waveRef.stronghold.center != null)
            {
                waveEvent.holdPoint = waveRef.stronghold.center;
            }

            if (needsSpawnGroup && waveRef.fallbackPrefab != null)
            {
                waveEvent.groups.Add(new WaveSpawnGroup
                {
                    prefab = waveRef.fallbackPrefab,
                    count = ComputeEventSpawnCount(levelIndex, waveRef.localWaveIndex, resolvedType)
                });
            }

            waveRef.wave.events.Add(waveEvent);
            changed = true;
            return 1;
        }

        private static WaveEventType FindMissingType(EventTypeCounts counts)
        {
            if (counts.Chase <= 0) return WaveEventType.Chase;
            if (counts.HoldPoint <= 0) return WaveEventType.HoldPoint;
            if (counts.ProtectTarget <= 0) return WaveEventType.ProtectTarget;
            if (counts.Reinforcement <= 0) return WaveEventType.Reinforcement;
            return WaveEventType.Reinforcement;
        }

        private static WaveRef SelectWaveForEvent(List<WaveRef> waveRefs, WaveEventType eventType)
        {
            int bestIndex = -1;
            int bestEventCount = int.MaxValue;
            for (int i = waveRefs.Count - 1; i >= 0; i--)
            {
                WaveRef candidate = waveRefs[i];
                if (candidate.wave == null)
                {
                    continue;
                }

                if (eventType != WaveEventType.HoldPoint && candidate.fallbackPrefab == null)
                {
                    continue;
                }

                int eventCount = candidate.wave.events != null ? candidate.wave.events.Count : 0;
                if (eventCount < bestEventCount)
                {
                    bestEventCount = eventCount;
                    bestIndex = i;
                }
            }

            return bestIndex >= 0 ? waveRefs[bestIndex] : default;
        }

        private static WaveEventType PickWaveEventType(int levelIndex, int globalWaveIndex)
        {
            if (levelIndex >= 6 && globalWaveIndex % 5 == 4)
            {
                return WaveEventType.ProtectTarget;
            }

            if (levelIndex >= 4 && globalWaveIndex % 4 == 2)
            {
                return WaveEventType.HoldPoint;
            }

            if (globalWaveIndex % 3 == 1)
            {
                return WaveEventType.Chase;
            }

            return WaveEventType.Reinforcement;
        }

        private static List<WaveRef> BuildWaveRefs(List<StrongholdController> strongholds)
        {
            var result = new List<WaveRef>();
            if (strongholds == null)
            {
                return result;
            }

            int globalIndex = 0;
            for (int s = 0; s < strongholds.Count; s++)
            {
                StrongholdController stronghold = strongholds[s];
                if (stronghold == null || stronghold.waves == null)
                {
                    continue;
                }

                GameObject fallback = ResolveFallbackPrefab(stronghold);
                for (int w = 0; w < stronghold.waves.Count; w++)
                {
                    StrongholdWave wave = stronghold.waves[w];
                    if (wave == null)
                    {
                        continue;
                    }

                    result.Add(new WaveRef
                    {
                        stronghold = stronghold,
                        wave = wave,
                        localWaveIndex = w,
                        globalWaveIndex = globalIndex,
                        fallbackPrefab = ResolveFallbackPrefab(wave, fallback)
                    });
                    globalIndex++;
                }
            }

            return result;
        }

        private static EventTypeCounts CountEventTypes(List<WaveRef> waveRefs)
        {
            var counts = new EventTypeCounts();
            for (int i = 0; i < waveRefs.Count; i++)
            {
                StrongholdWave wave = waveRefs[i].wave;
                if (wave == null || wave.events == null)
                {
                    continue;
                }

                for (int e = 0; e < wave.events.Count; e++)
                {
                    WaveEvent waveEvent = wave.events[e];
                    if (waveEvent == null || !waveEvent.enabled)
                    {
                        continue;
                    }

                    switch (waveEvent.eventType)
                    {
                        case WaveEventType.Reinforcement:
                            counts.Reinforcement++;
                            break;
                        case WaveEventType.Chase:
                            counts.Chase++;
                            break;
                        case WaveEventType.HoldPoint:
                            counts.HoldPoint++;
                            break;
                        case WaveEventType.ProtectTarget:
                            counts.ProtectTarget++;
                            break;
                    }
                }
            }

            return counts;
        }

        private static int GetCountByType(EventTypeCounts counts, WaveEventType eventType)
        {
            switch (eventType)
            {
                case WaveEventType.Reinforcement:
                    return counts.Reinforcement;
                case WaveEventType.Chase:
                    return counts.Chase;
                case WaveEventType.HoldPoint:
                    return counts.HoldPoint;
                case WaveEventType.ProtectTarget:
                    return counts.ProtectTarget;
                default:
                    return 0;
            }
        }

        private static bool HasEnabledEvent(StrongholdWave wave)
        {
            if (wave == null || wave.events == null)
            {
                return false;
            }

            for (int i = 0; i < wave.events.Count; i++)
            {
                WaveEvent waveEvent = wave.events[i];
                if (waveEvent != null && waveEvent.enabled)
                {
                    return true;
                }
            }

            return false;
        }

        private static int ComputeWaveSpawnTarget(int levelIndex, int waveIndex, int totalWaves)
        {
            int band = levelIndex <= 4 ? 0 : (levelIndex <= 7 ? 1 : 2);
            int target = 6 + band;
            if (waveIndex >= Mathf.Max(1, totalWaves - 3))
            {
                target += 2;
            }

            if (waveIndex == totalWaves - 1)
            {
                target += 2;
            }

            return Mathf.Max(3, target);
        }

        private static int ComputeEventSpawnCount(int levelIndex, int waveIndex, WaveEventType eventType)
        {
            int baseCount = levelIndex <= 4 ? 2 : (levelIndex <= 7 ? 3 : 4);
            switch (eventType)
            {
                case WaveEventType.Chase:
                    return Mathf.Clamp(baseCount, 2, 4);
                case WaveEventType.ProtectTarget:
                    return Mathf.Clamp(baseCount + 1, 3, 6);
                case WaveEventType.Reinforcement:
                default:
                    return Mathf.Clamp(baseCount + waveIndex / 3, 2, 6);
            }
        }

        private static float ComputeWaveSpawnInterval(int levelIndex, int waveIndex, int totalWaves)
        {
            float interval = 0.42f - (levelIndex - 2) * 0.015f - waveIndex * 0.008f;
            if (waveIndex >= Mathf.Max(1, totalWaves - 3))
            {
                interval -= 0.02f;
            }

            return Mathf.Clamp(interval, 0.22f, 0.45f);
        }

        private static float DefaultEventDuration(WaveEventType type, int waveIndex)
        {
            switch (type)
            {
                case WaveEventType.Chase:
                    return 6f + waveIndex * 0.8f;
                case WaveEventType.HoldPoint:
                    return 6f + waveIndex * 0.5f;
                case WaveEventType.ProtectTarget:
                    return 8f + waveIndex * 0.8f;
                case WaveEventType.Reinforcement:
                default:
                    return 5f + waveIndex * 0.4f;
            }
        }

        private static float DefaultEventInterval(WaveEventType type, int levelIndex)
        {
            switch (type)
            {
                case WaveEventType.Chase:
                    return 0.9f;
                case WaveEventType.ProtectTarget:
                    return 0.45f;
                case WaveEventType.Reinforcement:
                case WaveEventType.HoldPoint:
                default:
                    return Mathf.Clamp(0.45f - (levelIndex - 2) * 0.01f, 0.28f, 0.45f);
            }
        }

        private static GameObject ResolveFallbackPrefab(StrongholdController stronghold)
        {
            if (stronghold == null || stronghold.waves == null)
            {
                return null;
            }

            for (int w = 0; w < stronghold.waves.Count; w++)
            {
                StrongholdWave wave = stronghold.waves[w];
                if (wave == null || wave.groups == null)
                {
                    continue;
                }

                for (int g = 0; g < wave.groups.Count; g++)
                {
                    WaveSpawnGroup group = wave.groups[g];
                    if (group != null && group.prefab != null)
                    {
                        return group.prefab;
                    }
                }
            }

            return null;
        }

        private static GameObject ResolveFallbackPrefab(StrongholdWave wave, GameObject sceneFallback)
        {
            if (wave != null && wave.groups != null)
            {
                for (int i = 0; i < wave.groups.Count; i++)
                {
                    WaveSpawnGroup group = wave.groups[i];
                    if (group != null && group.prefab != null)
                    {
                        return group.prefab;
                    }
                }
            }

            return sceneFallback;
        }

        private static List<LevelEntry> CollectTargetLevels()
        {
            var entries = new List<LevelEntry>();
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

                entries.Add(new LevelEntry
                {
                    levelData = levelData,
                    levelAssetPath = assetPath,
                    levelIndex = levelIndex
                });
            }

            entries.Sort((a, b) => a.levelIndex.CompareTo(b.levelIndex));
            return entries;
        }

        private static string BuildScenePath(LevelData levelData)
        {
            if (levelData == null || string.IsNullOrWhiteSpace(levelData.sceneName))
            {
                return string.Empty;
            }

            return $"{SceneFolderPath}/{levelData.sceneName.Trim()}.unity";
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

            string numberPart = levelId.Substring(prefix.Length);
            if (int.TryParse(numberPart, out int parsed))
            {
                return parsed;
            }

            return -1;
        }

        private static int ResolveRuntimeLevelId(LevelData levelData, int levelIndex)
        {
            if (levelData == null)
            {
                return 0;
            }

            if (levelData.chapterId > 0 && levelIndex > 0)
            {
                return levelData.chapterId * 100 + levelIndex;
            }

            return 0;
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

        private static string WriteCsv(List<ValidationRow> rows)
        {
            string fullPath = Path.GetFullPath(ReportCsvPath);
            EnsureDirectoryExists(fullPath);

            var csv = new StringBuilder();
            csv.AppendLine(
                "level_id,level_asset,scene_name,scene_path,status,fixed,blocking_errors,gaps,strongholds,waves,groups,base_spawn,wave_event_coverage,total_events,reinforcement_events,chase_events,hold_events,protect_events,unique_event_types,target_min_waves,target_min_spawn,target_min_event_coverage,target_min_unique_types,target_min_chase,target_min_hold,target_min_protect,completion_ready,note");

            for (int i = 0; i < rows.Count; i++)
            {
                ValidationRow row = rows[i];
                csv.Append(EscapeCsv(row.levelId)).Append(',')
                    .Append(EscapeCsv(row.levelAssetPath)).Append(',')
                    .Append(EscapeCsv(row.sceneName)).Append(',')
                    .Append(EscapeCsv(row.scenePath)).Append(',')
                    .Append(EscapeCsv(row.status)).Append(',')
                    .Append(row.fixedCount).Append(',')
                    .Append(row.blockingErrors).Append(',')
                    .Append(row.gapCount).Append(',')
                    .Append(row.strongholdCount).Append(',')
                    .Append(row.totalWaves).Append(',')
                    .Append(row.totalGroups).Append(',')
                    .Append(row.totalBaseSpawn).Append(',')
                    .Append(row.waveEventCoverage.ToString("0.000")).Append(',')
                    .Append(row.totalEvents).Append(',')
                    .Append(row.reinforcementEvents).Append(',')
                    .Append(row.chaseEvents).Append(',')
                    .Append(row.holdEvents).Append(',')
                    .Append(row.protectEvents).Append(',')
                    .Append(row.uniqueEventTypes).Append(',')
                    .Append(row.targetMinWaves).Append(',')
                    .Append(row.targetMinBaseSpawn).Append(',')
                    .Append(row.targetMinEventCoverage.ToString("0.000")).Append(',')
                    .Append(row.targetMinUniqueTypes).Append(',')
                    .Append(row.targetMinChase).Append(',')
                    .Append(row.targetMinHold).Append(',')
                    .Append(row.targetMinProtect).Append(',')
                    .Append(row.completionReady ? "1" : "0").Append(',')
                    .Append(EscapeCsv(row.note))
                    .AppendLine();
            }

            File.WriteAllText(fullPath, csv.ToString(), new UTF8Encoding(false));
            return ReportCsvPath;
        }

        private static string WriteSummary(
            List<ValidationRow> rows,
            int errorSceneCount,
            int gapSceneCount,
            int blockingTotal,
            int gapTotal,
            int fixedTotal,
            bool applyFix)
        {
            string fullPath = Path.GetFullPath(SummaryMdPath);
            EnsureDirectoryExists(fullPath);

            var md = new StringBuilder();
            md.AppendLine("# Level Combat Density Summary");
            md.AppendLine();
            md.AppendLine($"- Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss zzz}");
            md.AppendLine($"- Mode: {(applyFix ? "fix" : "validate")}");
            md.AppendLine($"- Targets: {rows.Count}");
            md.AppendLine($"- Error Scenes: {errorSceneCount}");
            md.AppendLine($"- Gap Scenes: {gapSceneCount}");
            md.AppendLine($"- Blocking Errors: {blockingTotal}");
            md.AppendLine($"- Gaps: {gapTotal}");
            md.AppendLine($"- Fixed: {fixedTotal}");
            md.AppendLine($"- CSV: {ReportCsvPath}");
            md.AppendLine();
            md.AppendLine("| Level | Scene | Status | Fixed | Blocking | Gaps | Waves | BaseSpawn | Coverage | Events | Types | Note |");
            md.AppendLine("|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---|");

            for (int i = 0; i < rows.Count; i++)
            {
                ValidationRow row = rows[i];
                md.Append("| ")
                    .Append(SafeMarkdownCell(row.levelId)).Append(" | ")
                    .Append(SafeMarkdownCell(row.sceneName)).Append(" | ")
                    .Append(SafeMarkdownCell(row.status)).Append(" | ")
                    .Append(row.fixedCount).Append(" | ")
                    .Append(row.blockingErrors).Append(" | ")
                    .Append(row.gapCount).Append(" | ")
                    .Append(row.totalWaves).Append(" | ")
                    .Append(row.totalBaseSpawn).Append(" | ")
                    .Append(row.waveEventCoverage.ToString("0.00")).Append(" | ")
                    .Append(row.totalEvents).Append(" | ")
                    .Append(row.uniqueEventTypes).Append(" | ")
                    .Append(SafeMarkdownCell(TrimForMarkdownTable(row.note, 180))).Append(" |")
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

        private struct LevelEntry
        {
            public LevelData levelData;
            public string levelAssetPath;
            public int levelIndex;
        }

        private struct DensityTarget
        {
            public int minTotalWaves;
            public int minBaseSpawnTotal;
            public float minEventCoverage;
            public int minUniqueEventTypes;
            public int minChaseEvents;
            public int minHoldEvents;
            public int minProtectEvents;
        }

        private struct DensityMetrics
        {
            public int strongholdCount;
            public int totalWaves;
            public int totalGroups;
            public int totalBaseSpawn;
            public int wavesWithEvents;
            public int totalEvents;
            public int reinforcementEvents;
            public int chaseEvents;
            public int holdEvents;
            public int protectEvents;
            public int uniqueEventTypes;
            public bool completionReady;
        }

        private struct ValidationRow
        {
            public string levelId;
            public string levelAssetPath;
            public string sceneName;
            public string scenePath;
            public string status;
            public int fixedCount;
            public int blockingErrors;
            public int gapCount;
            public int strongholdCount;
            public int totalWaves;
            public int totalGroups;
            public int totalBaseSpawn;
            public float waveEventCoverage;
            public int totalEvents;
            public int reinforcementEvents;
            public int chaseEvents;
            public int holdEvents;
            public int protectEvents;
            public int uniqueEventTypes;
            public int targetMinWaves;
            public int targetMinBaseSpawn;
            public float targetMinEventCoverage;
            public int targetMinUniqueTypes;
            public int targetMinChase;
            public int targetMinHold;
            public int targetMinProtect;
            public bool completionReady;
            public string note;
        }

        private struct WaveRef
        {
            public StrongholdController stronghold;
            public StrongholdWave wave;
            public int localWaveIndex;
            public int globalWaveIndex;
            public GameObject fallbackPrefab;
        }

        private struct EventTypeCounts
        {
            public int Reinforcement;
            public int Chase;
            public int HoldPoint;
            public int ProtectTarget;
            public int UniqueCount
            {
                get
                {
                    int count = 0;
                    if (Reinforcement > 0) count++;
                    if (Chase > 0) count++;
                    if (HoldPoint > 0) count++;
                    if (ProtectTarget > 0) count++;
                    return count;
                }
            }
        }
    }
}
