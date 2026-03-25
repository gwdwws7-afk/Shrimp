using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using ThirdPersonController;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThirdPersonController.Editor
{
    public static class LevelBeatSheetConsistencyValidator
    {
        private const string ValidateMenuPath = "Tools/Level/P0/Validate Level Beat Sheet Consistency (CSV)";
        private const string ValidateGateMenuPath = "Tools/Level/P0/Validate Level Beat Sheet Consistency (CI Gate)";
        private const string SceneFolderPath = "Assets/Scenes";
        private const string ReportCsvPath = "Assets/ThirdPersonController/Reports/level_beat_sheet_consistency_report.csv";
        private const string SummaryMdPath = "Assets/ThirdPersonController/Reports/level_beat_sheet_consistency_summary.md";
        private const string LogPrefix = "[LevelBeatSheetConsistency]";
        private const int MinLevelIndex = 2;
        private const int MaxLevelIndex = 10;
        private static readonly ThresholdConfig Thresholds = ThresholdConfig.FromEnvironment();

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
            // Beat Sheet gate currently uses report-first workflow; fix path is a no-op alias.
            Run(interactive: false, failOnError: true);
        }

        private static void Run(bool interactive, bool failOnError)
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
                string noneMessage =
                    $"{LogPrefix} no LevelData assets found for LEVEL_{MinLevelIndex:D2}~LEVEL_{MaxLevelIndex:D2}.";
                Debug.LogWarning(noneMessage);
                if (interactive)
                {
                    EditorUtility.DisplayDialog("Level Beat Sheet Consistency", noneMessage, "OK");
                }

                return;
            }

            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
            var rows = new List<ValidationRow>(entries.Count);
            try
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    rows.Add(ProcessEntry(entries[i]));
                }
            }
            finally
            {
                if (originalSetup != null && originalSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
                }
            }

            List<string> globalWarnings = BuildGlobalWarnings(rows);

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
            string summaryPath = WriteSummary(rows, errorRows, warningTotal, globalWarnings);
            AssetDatabase.Refresh();

            string summary =
                $"targets={rows.Count} errors={errorRows} warnings={warningTotal} csv={csvPath} summary={summaryPath}";
            Debug.Log($"{LogPrefix} complete | {summary}");

            if (interactive)
            {
                EditorUtility.DisplayDialog("Level Beat Sheet Consistency", summary, "OK");
            }

            if (failOnError && errorRows > 0)
            {
                throw new InvalidOperationException($"{LogPrefix} gate failed. errors={errorRows} csv={csvPath}");
            }
        }

        private static ValidationRow ProcessEntry(LevelEntry entry)
        {
            var row = new ValidationRow
            {
                levelIndex = entry.levelIndex,
                levelId = entry.levelData != null ? entry.levelData.levelId : string.Empty,
                levelAssetPath = entry.levelAssetPath ?? string.Empty,
                sceneName = entry.levelData != null ? entry.levelData.sceneName : string.Empty,
                scenePath = BuildScenePath(entry.levelData),
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
                return BuildRow(row, blockingNotes, warningNotes);
            }

            if (!AssetExists(row.scenePath))
            {
                blockingNotes.Add("Scene asset is missing.");
                return BuildRow(row, blockingNotes, warningNotes);
            }

            Scene scene;
            try
            {
                scene = EditorSceneManager.OpenScene(row.scenePath, OpenSceneMode.Single);
            }
            catch (Exception ex)
            {
                blockingNotes.Add($"OpenScene failed: {ex.Message}");
                return BuildRow(row, blockingNotes, warningNotes);
            }

            StrongholdSequenceController sequence = FindComponentInScene<StrongholdSequenceController>(scene);
            List<StrongholdController> strongholds = FindComponentsInScene<StrongholdController>(scene);
            List<BossSpawnPoint> bossSpawnPoints = FindComponentsInScene<BossSpawnPoint>(scene);

            row.strongholdCount = strongholds.Count;

            if (sequence == null)
            {
                blockingNotes.Add("Missing StrongholdSequenceController.");
                return BuildRow(row, blockingNotes, warningNotes);
            }

            if (strongholds.Count == 0)
            {
                blockingNotes.Add("No StrongholdController found in scene.");
                return BuildRow(row, blockingNotes, warningNotes);
            }

            BeatMetrics metrics = EvaluateMetrics(strongholds, entry.levelIndex);
            row.recommendedPower = Mathf.Max(0, entry.levelData.recommendedPower);
            row.totalWaves = metrics.totalWaves;
            row.totalBaseSpawn = metrics.totalBaseSpawn;
            row.totalEvents = metrics.totalEvents;
            row.chaseEvents = metrics.chaseEvents;
            row.holdEvents = metrics.holdEvents;
            row.protectEvents = metrics.protectEvents;
            row.reinforcementEvents = metrics.reinforcementEvents;

            row.openingWaveCount = metrics.openingWaveCount;
            row.teachingWaveCount = metrics.teachingWaveCount;
            row.peakWaveCount = metrics.peakWaveCount;
            row.openingBaseSpawn = metrics.openingBaseSpawn;
            row.teachingEventWaves = metrics.teachingEventWaves;
            row.teachingChaseOrHoldEvents = metrics.teachingChaseOrHoldEvents;
            row.peakBaseSpawn = metrics.peakBaseSpawn;
            row.peakEventWaves = metrics.peakEventWaves;

            BeatTarget target = BuildTarget(entry.levelIndex, metrics.totalWaves);
            row.targetOpeningSpawn = target.minOpeningSpawn;
            row.targetTeachingEventWaves = target.minTeachingEventWaves;
            row.targetPeakSpawn = target.minPeakSpawn;
            row.targetPeakEventWaves = target.minPeakEventWaves;

            row.openingReady = metrics.openingBaseSpawn >= target.minOpeningSpawn && metrics.openingWaveCount > 0;
            row.teachingReady = metrics.teachingEventWaves >= target.minTeachingEventWaves &&
                                metrics.teachingChaseOrHoldEvents >= target.minTeachingChaseOrHold;
            row.peakReady = metrics.peakBaseSpawn >= target.minPeakSpawn &&
                            metrics.peakEventWaves >= target.minPeakEventWaves;

            row.expectBossGate = entry.levelData.overrideBossSettings;
            row.bossGateReady = !row.expectBossGate ||
                                (sequence.deferCompletionUntilBoss &&
                                 sequence.bossSpawnPoint != null &&
                                 bossSpawnPoints.Contains(sequence.bossSpawnPoint));
            row.settlementReady = sequence.triggerLevelCompleteOnFinish &&
                                  sequence.triggerVictoryOnFinish &&
                                  row.bossGateReady;

            if (!row.openingReady)
            {
                warningNotes.Add(
                    $"opening beat weak (spawn={metrics.openingBaseSpawn}, target={target.minOpeningSpawn}).");
            }

            if (!row.teachingReady)
            {
                warningNotes.Add(
                    $"teaching beat weak (eventWaves={metrics.teachingEventWaves}, chaseOrHold={metrics.teachingChaseOrHoldEvents}).");
            }

            if (!row.peakReady)
            {
                warningNotes.Add(
                    $"peak beat weak (spawn={metrics.peakBaseSpawn}, eventWaves={metrics.peakEventWaves}).");
            }

            if (!row.settlementReady)
            {
                warningNotes.Add("settlement beat wiring is incomplete (completion/boss gate).");
            }

            return BuildRow(row, blockingNotes, warningNotes);
        }

        private static BeatMetrics EvaluateMetrics(List<StrongholdController> strongholds, int levelIndex)
        {
            var metrics = new BeatMetrics();
            List<WaveRef> waveRefs = BuildWaveRefs(strongholds);
            metrics.totalWaves = waveRefs.Count;

            if (waveRefs.Count == 0)
            {
                return metrics;
            }

            WindowRange opening = BuildOpeningWindow(waveRefs.Count);
            WindowRange teaching = BuildTeachingWindow(waveRefs.Count, opening);
            WindowRange peak = BuildPeakWindow(waveRefs.Count, teaching);

            for (int i = 0; i < waveRefs.Count; i++)
            {
                WaveRef waveRef = waveRefs[i];
                metrics.totalBaseSpawn += waveRef.baseSpawn;
                metrics.totalEvents += waveRef.enabledEventCount;
                metrics.chaseEvents += waveRef.chaseEvents;
                metrics.holdEvents += waveRef.holdEvents;
                metrics.protectEvents += waveRef.protectEvents;
                metrics.reinforcementEvents += waveRef.reinforcementEvents;

                if (opening.Contains(i))
                {
                    metrics.openingWaveCount++;
                    metrics.openingBaseSpawn += waveRef.baseSpawn;
                }

                if (teaching.Contains(i))
                {
                    metrics.teachingWaveCount++;
                    metrics.teachingBaseSpawn += waveRef.baseSpawn;
                    if (waveRef.enabledEventCount > 0)
                    {
                        metrics.teachingEventWaves++;
                    }

                    metrics.teachingChaseOrHoldEvents += waveRef.chaseEvents + waveRef.holdEvents;
                }

                if (peak.Contains(i))
                {
                    metrics.peakWaveCount++;
                    metrics.peakBaseSpawn += waveRef.baseSpawn;
                    if (waveRef.enabledEventCount > 0)
                    {
                        metrics.peakEventWaves++;
                    }
                }
            }

            return metrics;
        }

        private static List<WaveRef> BuildWaveRefs(List<StrongholdController> strongholds)
        {
            var result = new List<WaveRef>();
            if (strongholds == null)
            {
                return result;
            }

            for (int s = 0; s < strongholds.Count; s++)
            {
                StrongholdController stronghold = strongholds[s];
                if (stronghold == null || stronghold.waves == null)
                {
                    continue;
                }

                for (int w = 0; w < stronghold.waves.Count; w++)
                {
                    StrongholdWave wave = stronghold.waves[w];
                    if (wave == null)
                    {
                        continue;
                    }

                    int baseSpawn = 0;
                    int enabledEvents = 0;
                    int chase = 0;
                    int hold = 0;
                    int protect = 0;
                    int reinforcement = 0;

                    if (wave.groups != null)
                    {
                        for (int g = 0; g < wave.groups.Count; g++)
                        {
                            WaveSpawnGroup group = wave.groups[g];
                            if (group != null && group.prefab != null && group.count > 0)
                            {
                                baseSpawn += group.count;
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

                            enabledEvents++;
                            switch (waveEvent.eventType)
                            {
                                case WaveEventType.Chase:
                                    chase++;
                                    break;
                                case WaveEventType.HoldPoint:
                                    hold++;
                                    break;
                                case WaveEventType.ProtectTarget:
                                    protect++;
                                    break;
                                case WaveEventType.Reinforcement:
                                default:
                                    reinforcement++;
                                    break;
                            }
                        }
                    }

                    result.Add(new WaveRef
                    {
                        baseSpawn = baseSpawn,
                        enabledEventCount = enabledEvents,
                        chaseEvents = chase,
                        holdEvents = hold,
                        protectEvents = protect,
                        reinforcementEvents = reinforcement
                    });
                }
            }

            return result;
        }

        private static WindowRange BuildOpeningWindow(int totalWaves)
        {
            int count = Mathf.Clamp(Mathf.CeilToInt(totalWaves * 0.25f), 1, Mathf.Max(1, totalWaves));
            count = Mathf.Max(2, count);
            count = Mathf.Min(count, totalWaves);
            return new WindowRange(0, Mathf.Max(0, count - 1));
        }

        private static WindowRange BuildTeachingWindow(int totalWaves, WindowRange opening)
        {
            int start = Mathf.Clamp(opening.End + 1, 0, totalWaves - 1);
            int rawEnd = Mathf.CeilToInt(totalWaves * 0.7f) - 1;
            int end = Mathf.Clamp(rawEnd, start, totalWaves - 1);
            return new WindowRange(start, end);
        }

        private static WindowRange BuildPeakWindow(int totalWaves, WindowRange teaching)
        {
            int count = Mathf.Clamp(Mathf.CeilToInt(totalWaves * 0.3f), 1, totalWaves);
            count = Mathf.Max(2, count);
            int start = Mathf.Max(0, totalWaves - count);
            if (start <= teaching.End)
            {
                start = Mathf.Clamp(teaching.End + 1, 0, totalWaves - 1);
            }

            int end = totalWaves - 1;
            return new WindowRange(start, end);
        }

        private static BeatTarget BuildTarget(int levelIndex, int totalWaves)
        {
            int band = levelIndex <= 4 ? 0 : (levelIndex <= 7 ? 1 : 2);
            int normalizedWaves = Mathf.Max(6, totalWaves);

            return new BeatTarget
            {
                minOpeningSpawn = band == 0 ? 18 : (band == 1 ? 24 : 28),
                minTeachingEventWaves = band == 0 ? 1 : 2,
                minTeachingChaseOrHold = 1,
                minPeakSpawn = normalizedWaves >= 10
                    ? (band == 0 ? 26 : (band == 1 ? 34 : 42))
                    : (band == 0 ? 20 : (band == 1 ? 28 : 34)),
                minPeakEventWaves = band == 0 ? 1 : 2
            };
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
                    levelData = levelData,
                    levelAssetPath = assetPath,
                    levelIndex = levelIndex
                });
            }

            result.Sort((a, b) => a.levelIndex.CompareTo(b.levelIndex));
            return result;
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

            string raw = levelId.Substring(prefix.Length);
            if (int.TryParse(raw, out int parsed))
            {
                return parsed;
            }

            return -1;
        }

        private static bool AssetExists(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return false;
            }

            return AssetDatabase.LoadMainAssetAtPath(assetPath) != null;
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

        private static List<string> BuildGlobalWarnings(List<ValidationRow> rows)
        {
            var warnings = new List<string>();
            if (rows == null || rows.Count == 0)
            {
                return warnings;
            }

            var stableRows = new List<ValidationRow>();
            for (int i = 0; i < rows.Count; i++)
            {
                ValidationRow row = rows[i];
                if (string.Equals(row.status, "Ok", StringComparison.Ordinal))
                {
                    stableRows.Add(row);
                }
            }

            int expectedRows = MaxLevelIndex - MinLevelIndex + 1;
            if (stableRows.Count < expectedRows)
            {
                warnings.Add(
                    $"progression analysis is partial because only {stableRows.Count}/{expectedRows} levels passed local beat checks.");
            }

            if (stableRows.Count == 0)
            {
                return warnings;
            }

            stableRows.Sort((a, b) => a.levelIndex.CompareTo(b.levelIndex));

            int spawnDistinctCount = CountDistinct(stableRows, row => row.totalBaseSpawn);
            int eventDistinctCount = CountDistinct(stableRows, row => row.totalEvents);
            int peakDistinctCount = CountDistinct(stableRows, row => row.peakBaseSpawn);
            if (spawnDistinctCount < Thresholds.minDistinctBaseSpawnValues)
            {
                warnings.Add(
                    $"total base spawn variation is low (distinct values={spawnDistinctCount}, threshold={Thresholds.minDistinctBaseSpawnValues}).");
            }

            if (eventDistinctCount < Thresholds.minDistinctEventValues)
            {
                warnings.Add(
                    $"total event-count variation is low (distinct values={eventDistinctCount}, threshold={Thresholds.minDistinctEventValues}).");
            }

            if (peakDistinctCount < Thresholds.minDistinctPeakSpawnValues)
            {
                warnings.Add(
                    $"peak-window spawn variation is low (distinct values={peakDistinctCount}, threshold={Thresholds.minDistinctPeakSpawnValues}).");
            }

            if (TryComputeBandAverage(stableRows, 2, 4, row => row.totalEvents, out float earlyEventAvg) &&
                TryComputeBandAverage(stableRows, 5, 7, row => row.totalEvents, out float midEventAvg) &&
                TryComputeBandAverage(stableRows, 8, 10, row => row.totalEvents, out float lateEventAvg))
            {
                if (midEventAvg + Thresholds.bandRegressionEpsilon < earlyEventAvg)
                {
                    warnings.Add($"mid-game event pressure regressed (early={earlyEventAvg:0.##}, mid={midEventAvg:0.##}).");
                }

                if (lateEventAvg + Thresholds.bandRegressionEpsilon < midEventAvg)
                {
                    warnings.Add($"late-game event pressure regressed (mid={midEventAvg:0.##}, late={lateEventAvg:0.##}).");
                }
            }

            if (TryComputeBandAverage(stableRows, 2, 4, row => row.peakBaseSpawn, out float earlyPeakSpawnAvg) &&
                TryComputeBandAverage(stableRows, 5, 7, row => row.peakBaseSpawn, out float midPeakSpawnAvg) &&
                TryComputeBandAverage(stableRows, 8, 10, row => row.peakBaseSpawn, out float latePeakSpawnAvg))
            {
                if (midPeakSpawnAvg + Thresholds.bandRegressionEpsilon < earlyPeakSpawnAvg)
                {
                    warnings.Add(
                        $"mid-game peak spawn intensity regressed (early={earlyPeakSpawnAvg:0.##}, mid={midPeakSpawnAvg:0.##}).");
                }

                if (latePeakSpawnAvg + Thresholds.bandRegressionEpsilon < midPeakSpawnAvg)
                {
                    warnings.Add(
                        $"late-game peak spawn intensity regressed (mid={midPeakSpawnAvg:0.##}, late={latePeakSpawnAvg:0.##}).");
                }
            }

            if (TryComputeBandAverage(stableRows, 2, 4, row => row.recommendedPower, out float earlyPowerAvg) &&
                TryComputeBandAverage(stableRows, 5, 7, row => row.recommendedPower, out float midPowerAvg) &&
                TryComputeBandAverage(stableRows, 8, 10, row => row.recommendedPower, out float latePowerAvg) &&
                TryComputeBandAverage(stableRows, 2, 4, row => row.totalEvents, out earlyEventAvg) &&
                TryComputeBandAverage(stableRows, 5, 7, row => row.totalEvents, out midEventAvg) &&
                TryComputeBandAverage(stableRows, 8, 10, row => row.totalEvents, out lateEventAvg) &&
                TryComputeBandAverage(stableRows, 2, 4, row => row.peakBaseSpawn, out earlyPeakSpawnAvg) &&
                TryComputeBandAverage(stableRows, 5, 7, row => row.peakBaseSpawn, out midPeakSpawnAvg) &&
                TryComputeBandAverage(stableRows, 8, 10, row => row.peakBaseSpawn, out latePeakSpawnAvg))
            {
                float earlyToMidPowerGrowth = ComputeGrowthRate(earlyPowerAvg, midPowerAvg);
                float midToLatePowerGrowth = ComputeGrowthRate(midPowerAvg, latePowerAvg);
                float earlyToMidEventGrowth = ComputeGrowthRate(earlyEventAvg, midEventAvg);
                float midToLateEventGrowth = ComputeGrowthRate(midEventAvg, lateEventAvg);
                float earlyToMidPeakGrowth = ComputeGrowthRate(earlyPeakSpawnAvg, midPeakSpawnAvg);
                float midToLatePeakGrowth = ComputeGrowthRate(midPeakSpawnAvg, latePeakSpawnAvg);

                AppendPowerToBeatAlignmentWarnings(
                    "early->mid",
                    earlyToMidPowerGrowth,
                    earlyToMidEventGrowth,
                    earlyToMidPeakGrowth,
                    warnings);
                AppendPowerToBeatAlignmentWarnings(
                    "mid->late",
                    midToLatePowerGrowth,
                    midToLateEventGrowth,
                    midToLatePeakGrowth,
                    warnings);
            }

            var signatureToLevels = new Dictionary<string, List<int>>();
            for (int i = 0; i < stableRows.Count; i++)
            {
                ValidationRow row = stableRows[i];
                string signature =
                    $"{row.strongholdCount}|{row.totalWaves}|{row.totalBaseSpawn}|{row.totalEvents}|{row.chaseEvents}|{row.holdEvents}|{row.protectEvents}|{row.reinforcementEvents}|{row.openingBaseSpawn}|{row.peakBaseSpawn}";
                if (!signatureToLevels.TryGetValue(signature, out List<int> levels))
                {
                    levels = new List<int>();
                    signatureToLevels.Add(signature, levels);
                }

                levels.Add(row.levelIndex);
            }

            int repeatedSignatureWarnings = 0;
            foreach (KeyValuePair<string, List<int>> pair in signatureToLevels)
            {
                List<int> levels = pair.Value;
                if (levels == null || levels.Count < Thresholds.duplicateSignatureMinGroupSize)
                {
                    continue;
                }

                levels.Sort();
                warnings.Add(
                    $"levels {string.Join(",", levels)} share an identical beat signature (content differentiation risk).");
                repeatedSignatureWarnings++;
                if (repeatedSignatureWarnings >= Thresholds.maxDuplicateSignatureWarnings)
                {
                    break;
                }
            }

            return warnings;
        }

        private static void AppendPowerToBeatAlignmentWarnings(
            string bandTransition,
            float powerGrowth,
            float eventGrowth,
            float peakGrowth,
            List<string> warnings)
        {
            if (warnings == null)
            {
                return;
            }

            if (powerGrowth + Thresholds.bandRegressionEpsilon < Thresholds.minPowerGrowthForBeatLinkCheck)
            {
                return;
            }

            if (eventGrowth + Thresholds.bandRegressionEpsilon < Thresholds.minEventGrowthForPowerLink)
            {
                warnings.Add(
                    $"beat/progression link weak ({bandTransition}): recommendedPower grew {powerGrowth * 100f:0.##}% but event pressure grew {eventGrowth * 100f:0.##}% (threshold={Thresholds.minEventGrowthForPowerLink * 100f:0.##}%).");
            }

            if (peakGrowth + Thresholds.bandRegressionEpsilon < Thresholds.minPeakSpawnGrowthForPowerLink)
            {
                warnings.Add(
                    $"beat/progression link weak ({bandTransition}): recommendedPower grew {powerGrowth * 100f:0.##}% but peak spawn grew {peakGrowth * 100f:0.##}% (threshold={Thresholds.minPeakSpawnGrowthForPowerLink * 100f:0.##}%).");
            }
        }

        private static float ComputeGrowthRate(float previousValue, float currentValue)
        {
            if (previousValue <= 0.0001f)
            {
                return 0f;
            }

            return (currentValue - previousValue) / previousValue;
        }

        private static int CountDistinct(List<ValidationRow> rows, Func<ValidationRow, int> selector)
        {
            if (rows == null || rows.Count == 0 || selector == null)
            {
                return 0;
            }

            var values = new HashSet<int>();
            for (int i = 0; i < rows.Count; i++)
            {
                values.Add(selector(rows[i]));
            }

            return values.Count;
        }

        private static bool TryComputeBandAverage(
            List<ValidationRow> rows,
            int minLevelIndex,
            int maxLevelIndex,
            Func<ValidationRow, float> selector,
            out float average)
        {
            average = 0f;
            if (rows == null || rows.Count == 0 || selector == null)
            {
                return false;
            }

            float total = 0f;
            int count = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                ValidationRow row = rows[i];
                if (row.levelIndex < minLevelIndex || row.levelIndex > maxLevelIndex)
                {
                    continue;
                }

                total += selector(row);
                count++;
            }

            if (count <= 0)
            {
                return false;
            }

            average = total / count;
            return true;
        }

        private static string WriteCsv(List<ValidationRow> rows)
        {
            string fullPath = Path.GetFullPath(ReportCsvPath);
            EnsureDirectoryExists(fullPath);

            var csv = new StringBuilder();
            csv.AppendLine(
                "level_index,level_id,recommended_power,level_asset,scene_name,scene_path,status,blocking_errors,warnings,strongholds,waves,base_spawn,total_events,chase_events,hold_events,protect_events,reinforcement_events,opening_waves,opening_base_spawn,teaching_waves,teaching_event_waves,teaching_chase_hold_events,peak_waves,peak_base_spawn,peak_event_waves,target_opening_spawn,target_teaching_event_waves,target_peak_spawn,target_peak_event_waves,opening_ready,teaching_ready,peak_ready,boss_gate_expected,boss_gate_ready,settlement_ready,note");

            for (int i = 0; i < rows.Count; i++)
            {
                ValidationRow row = rows[i];
                csv.Append(row.levelIndex).Append(',')
                    .Append(EscapeCsv(row.levelId)).Append(',')
                    .Append(row.recommendedPower).Append(',')
                    .Append(EscapeCsv(row.levelAssetPath)).Append(',')
                    .Append(EscapeCsv(row.sceneName)).Append(',')
                    .Append(EscapeCsv(row.scenePath)).Append(',')
                    .Append(EscapeCsv(row.status)).Append(',')
                    .Append(row.blockingErrors).Append(',')
                    .Append(row.warnings).Append(',')
                    .Append(row.strongholdCount).Append(',')
                    .Append(row.totalWaves).Append(',')
                    .Append(row.totalBaseSpawn).Append(',')
                    .Append(row.totalEvents).Append(',')
                    .Append(row.chaseEvents).Append(',')
                    .Append(row.holdEvents).Append(',')
                    .Append(row.protectEvents).Append(',')
                    .Append(row.reinforcementEvents).Append(',')
                    .Append(row.openingWaveCount).Append(',')
                    .Append(row.openingBaseSpawn).Append(',')
                    .Append(row.teachingWaveCount).Append(',')
                    .Append(row.teachingEventWaves).Append(',')
                    .Append(row.teachingChaseOrHoldEvents).Append(',')
                    .Append(row.peakWaveCount).Append(',')
                    .Append(row.peakBaseSpawn).Append(',')
                    .Append(row.peakEventWaves).Append(',')
                    .Append(row.targetOpeningSpawn).Append(',')
                    .Append(row.targetTeachingEventWaves).Append(',')
                    .Append(row.targetPeakSpawn).Append(',')
                    .Append(row.targetPeakEventWaves).Append(',')
                    .Append(row.openingReady ? "1" : "0").Append(',')
                    .Append(row.teachingReady ? "1" : "0").Append(',')
                    .Append(row.peakReady ? "1" : "0").Append(',')
                    .Append(row.expectBossGate ? "1" : "0").Append(',')
                    .Append(row.bossGateReady ? "1" : "0").Append(',')
                    .Append(row.settlementReady ? "1" : "0").Append(',')
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
            List<string> globalWarnings)
        {
            string fullPath = Path.GetFullPath(SummaryMdPath);
            EnsureDirectoryExists(fullPath);

            var md = new StringBuilder();
            md.AppendLine("# Level Beat Sheet Consistency Summary");
            md.AppendLine();
            md.AppendLine($"- Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss zzz}");
            md.AppendLine($"- Targets: {rows.Count}");
            md.AppendLine($"- Error Rows: {errorRows}");
            md.AppendLine($"- Warning Count: {warningTotal}");
            md.AppendLine($"- CSV: {ReportCsvPath}");
            md.AppendLine();
            md.AppendLine("## Thresholds");
            md.AppendLine($"- Min Distinct Base Spawn Values: {Thresholds.minDistinctBaseSpawnValues}");
            md.AppendLine($"- Min Distinct Event Values: {Thresholds.minDistinctEventValues}");
            md.AppendLine($"- Min Distinct Peak Spawn Values: {Thresholds.minDistinctPeakSpawnValues}");
            md.AppendLine($"- Duplicate Signature Min Group Size: {Thresholds.duplicateSignatureMinGroupSize}");
            md.AppendLine($"- Duplicate Signature Max Warnings: {Thresholds.maxDuplicateSignatureWarnings}");
            md.AppendLine($"- Min Power Growth For Beat Link Check: {Thresholds.minPowerGrowthForBeatLinkCheck:0.###}");
            md.AppendLine($"- Min Event Growth For Power Link: {Thresholds.minEventGrowthForPowerLink:0.###}");
            md.AppendLine($"- Min Peak Spawn Growth For Power Link: {Thresholds.minPeakSpawnGrowthForPowerLink:0.###}");
            md.AppendLine($"- Band Regression Epsilon: {Thresholds.bandRegressionEpsilon:0.###}");
            md.AppendLine();

            if (globalWarnings != null && globalWarnings.Count > 0)
            {
                md.AppendLine("## Global Warnings");
                for (int i = 0; i < globalWarnings.Count; i++)
                {
                    md.Append("- ")
                        .Append(SafeMarkdownCell(globalWarnings[i]))
                        .AppendLine();
                }

                md.AppendLine();
            }

            md.AppendLine("| Level | Scene | Status | Warnings | Opening | Teaching | Peak | Settlement | Note |");
            md.AppendLine("|---|---|---|---:|---|---|---|---|---|");

            for (int i = 0; i < rows.Count; i++)
            {
                ValidationRow row = rows[i];
                md.Append("| ")
                    .Append(SafeMarkdownCell(row.levelId)).Append(" | ")
                    .Append(SafeMarkdownCell(row.sceneName)).Append(" | ")
                    .Append(SafeMarkdownCell(row.status)).Append(" | ")
                    .Append(row.warnings).Append(" | ")
                    .Append(row.openingReady ? "OK" : "Gap").Append(" | ")
                    .Append(row.teachingReady ? "OK" : "Gap").Append(" | ")
                    .Append(row.peakReady ? "OK" : "Gap").Append(" | ")
                    .Append(row.settlementReady ? "OK" : "Gap").Append(" | ")
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
            public LevelData levelData;
            public string levelAssetPath;
            public int levelIndex;
        }

        private struct ValidationRow
        {
            public int levelIndex;
            public string levelId;
            public int recommendedPower;
            public string levelAssetPath;
            public string sceneName;
            public string scenePath;
            public string status;
            public int blockingErrors;
            public int warnings;
            public int strongholdCount;
            public int totalWaves;
            public int totalBaseSpawn;
            public int totalEvents;
            public int chaseEvents;
            public int holdEvents;
            public int protectEvents;
            public int reinforcementEvents;
            public int openingWaveCount;
            public int openingBaseSpawn;
            public int teachingWaveCount;
            public int teachingEventWaves;
            public int teachingChaseOrHoldEvents;
            public int peakWaveCount;
            public int peakBaseSpawn;
            public int peakEventWaves;
            public int targetOpeningSpawn;
            public int targetTeachingEventWaves;
            public int targetPeakSpawn;
            public int targetPeakEventWaves;
            public bool openingReady;
            public bool teachingReady;
            public bool peakReady;
            public bool expectBossGate;
            public bool bossGateReady;
            public bool settlementReady;
            public string note;
        }

        private struct BeatMetrics
        {
            public int totalWaves;
            public int totalBaseSpawn;
            public int totalEvents;
            public int chaseEvents;
            public int holdEvents;
            public int protectEvents;
            public int reinforcementEvents;

            public int openingWaveCount;
            public int openingBaseSpawn;

            public int teachingWaveCount;
            public int teachingBaseSpawn;
            public int teachingEventWaves;
            public int teachingChaseOrHoldEvents;

            public int peakWaveCount;
            public int peakBaseSpawn;
            public int peakEventWaves;
        }

        private struct BeatTarget
        {
            public int minOpeningSpawn;
            public int minTeachingEventWaves;
            public int minTeachingChaseOrHold;
            public int minPeakSpawn;
            public int minPeakEventWaves;
        }

        private struct WaveRef
        {
            public int baseSpawn;
            public int enabledEventCount;
            public int chaseEvents;
            public int holdEvents;
            public int protectEvents;
            public int reinforcementEvents;
        }

        private readonly struct ThresholdConfig
        {
            public readonly int minDistinctBaseSpawnValues;
            public readonly int minDistinctEventValues;
            public readonly int minDistinctPeakSpawnValues;
            public readonly int duplicateSignatureMinGroupSize;
            public readonly int maxDuplicateSignatureWarnings;
            public readonly float minPowerGrowthForBeatLinkCheck;
            public readonly float minEventGrowthForPowerLink;
            public readonly float minPeakSpawnGrowthForPowerLink;
            public readonly float bandRegressionEpsilon;

            private ThresholdConfig(
                int minDistinctBaseSpawnValues,
                int minDistinctEventValues,
                int minDistinctPeakSpawnValues,
                int duplicateSignatureMinGroupSize,
                int maxDuplicateSignatureWarnings,
                float minPowerGrowthForBeatLinkCheck,
                float minEventGrowthForPowerLink,
                float minPeakSpawnGrowthForPowerLink,
                float bandRegressionEpsilon)
            {
                this.minDistinctBaseSpawnValues = minDistinctBaseSpawnValues;
                this.minDistinctEventValues = minDistinctEventValues;
                this.minDistinctPeakSpawnValues = minDistinctPeakSpawnValues;
                this.duplicateSignatureMinGroupSize = duplicateSignatureMinGroupSize;
                this.maxDuplicateSignatureWarnings = maxDuplicateSignatureWarnings;
                this.minPowerGrowthForBeatLinkCheck = minPowerGrowthForBeatLinkCheck;
                this.minEventGrowthForPowerLink = minEventGrowthForPowerLink;
                this.minPeakSpawnGrowthForPowerLink = minPeakSpawnGrowthForPowerLink;
                this.bandRegressionEpsilon = bandRegressionEpsilon;
            }

            public static ThresholdConfig FromEnvironment()
            {
                return new ThresholdConfig(
                    minDistinctBaseSpawnValues: ReadInt("LEVEL_BEAT_MIN_DISTINCT_BASE_SPAWN", 2, 1, 64),
                    minDistinctEventValues: ReadInt("LEVEL_BEAT_MIN_DISTINCT_EVENTS", 3, 1, 64),
                    minDistinctPeakSpawnValues: ReadInt("LEVEL_BEAT_MIN_DISTINCT_PEAK_SPAWN", 2, 1, 64),
                    duplicateSignatureMinGroupSize: ReadInt("LEVEL_BEAT_DUP_SIGNATURE_MIN_GROUP", 3, 2, 16),
                    maxDuplicateSignatureWarnings: ReadInt("LEVEL_BEAT_DUP_SIGNATURE_MAX_WARNINGS", 3, 1, 16),
                    minPowerGrowthForBeatLinkCheck: ReadFloat("LEVEL_BEAT_MIN_POWER_GROWTH_FOR_LINK", 0.15f, 0f, 1f),
                    minEventGrowthForPowerLink: ReadFloat("LEVEL_BEAT_MIN_EVENT_GROWTH_FOR_LINK", 0.08f, -1f, 1f),
                    minPeakSpawnGrowthForPowerLink: ReadFloat("LEVEL_BEAT_MIN_PEAK_GROWTH_FOR_LINK", 0.05f, -1f, 1f),
                    bandRegressionEpsilon: ReadFloat("LEVEL_BEAT_BAND_REGRESSION_EPSILON", 0.01f, 0f, 0.2f));
            }

            private static int ReadInt(string key, int fallback, int minInclusive, int maxInclusive)
            {
                string raw = Environment.GetEnvironmentVariable(key);
                if (!string.IsNullOrWhiteSpace(raw) &&
                    int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
                {
                    return Mathf.Clamp(parsed, minInclusive, maxInclusive);
                }

                return Mathf.Clamp(fallback, minInclusive, maxInclusive);
            }

            private static float ReadFloat(string key, float fallback, float minInclusive, float maxInclusive)
            {
                string raw = Environment.GetEnvironmentVariable(key);
                if (!string.IsNullOrWhiteSpace(raw) &&
                    float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
                {
                    return Mathf.Clamp(parsed, minInclusive, maxInclusive);
                }

                return Mathf.Clamp(fallback, minInclusive, maxInclusive);
            }
        }

        private readonly struct WindowRange
        {
            public readonly int Start;
            public readonly int End;

            public WindowRange(int start, int end)
            {
                Start = Mathf.Min(start, end);
                End = Mathf.Max(start, end);
            }

            public bool Contains(int index)
            {
                return index >= Start && index <= End;
            }
        }
    }
}
