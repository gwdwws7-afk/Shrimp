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
    public static class WaveEventRotationGateValidator
    {
        private const string ValidateMenuPath = "Tools/Level/P1/Validate Wave Event Rotation (CSV)";
        private const string ValidateGateMenuPath = "Tools/Level/P1/Validate Wave Event Rotation (CI Gate)";
        private const string SceneFolderPath = "Assets/Scenes";
        private const string ReportCsvPath = "Assets/ThirdPersonController/Reports/wave_event_rotation_gate_report.csv";
        private const string SummaryMdPath = "Assets/ThirdPersonController/Reports/wave_event_rotation_gate_summary.md";
        private const string LogPrefix = "[WaveEventRotationGate]";
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
                    EditorUtility.DisplayDialog("Wave Event Rotation Gate", noneMessage, "OK");
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
            string summaryPath = WriteSummary(rows, errorRows, warningTotal);
            AssetDatabase.Refresh();

            string summary =
                $"targets={rows.Count} errors={errorRows} warnings={warningTotal} csv={csvPath} summary={summaryPath}";
            Debug.Log($"{LogPrefix} complete | {summary}");

            if (interactive)
            {
                EditorUtility.DisplayDialog("Wave Event Rotation Gate", summary, "OK");
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
                levelId = entry.levelData != null ? entry.levelData.levelId : string.Empty,
                levelAssetPath = entry.levelAssetPath ?? string.Empty,
                sceneName = entry.levelData != null ? entry.levelData.sceneName : string.Empty,
                scenePath = BuildScenePath(entry.levelData),
                status = "Error",
                note = string.Empty
            };

            var blockingNotes = new List<string>();
            var warningNotes = new List<string>();
            var uniqueTypes = new HashSet<WaveEventType>();

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

            List<StrongholdController> strongholds = FindComponentsInScene<StrongholdController>(scene);
            row.strongholdCount = strongholds.Count;
            if (strongholds.Count == 0)
            {
                blockingNotes.Add("No StrongholdController found in scene.");
                return BuildRow(row, blockingNotes, warningNotes);
            }

            int wavesWithoutEvents = 0;
            int reinforcementCount = 0;
            int chaseCount = 0;
            int holdCount = 0;
            int protectCount = 0;
            Dictionary<string, Dictionary<int, StrongholdWaveOverride>> overrideLookup =
                BuildWaveOverrideLookup(entry.levelData);

            for (int s = 0; s < strongholds.Count; s++)
            {
                StrongholdController stronghold = strongholds[s];
                if (stronghold == null)
                {
                    continue;
                }

                List<StrongholdWave> waves = stronghold.waves ?? new List<StrongholdWave>();
                if (waves.Count == 0)
                {
                    blockingNotes.Add($"Stronghold '{stronghold.name}' has no waves.");
                    continue;
                }

                bool hasPrevType = false;
                WaveEventType prevType = WaveEventType.Reinforcement;
                int streak = 0;
                bool hasAnyEventInStronghold = false;
                var strongholdTypes = new HashSet<WaveEventType>();

                for (int w = 0; w < waves.Count; w++)
                {
                    StrongholdWave wave = waves[w];
                    if (wave == null)
                    {
                        continue;
                    }

                    row.totalWaves++;

                    WaveEventType primaryType;
                    StrongholdWaveOverride waveOverride = GetWaveOverride(overrideLookup, stronghold.StrongholdId, w);
                    bool foundType = TryGetPrimaryEnabledEventType(wave, waveOverride, out primaryType);
                    if (!foundType)
                    {
                        wavesWithoutEvents++;
                        continue;
                    }

                    hasAnyEventInStronghold = true;
                    row.wavesWithEvents++;
                    uniqueTypes.Add(primaryType);
                    strongholdTypes.Add(primaryType);
                    CountType(primaryType, ref reinforcementCount, ref chaseCount, ref holdCount, ref protectCount);

                    if (!hasPrevType)
                    {
                        hasPrevType = true;
                        prevType = primaryType;
                        streak = 1;
                        continue;
                    }

                    row.rotationChains++;
                    if (primaryType == prevType)
                    {
                        streak++;
                    }
                    else
                    {
                        row.rotationBreaks++;
                        streak = 1;
                    }

                    if (streak > row.longestSameTypeStreak)
                    {
                        row.longestSameTypeStreak = streak;
                    }

                    prevType = primaryType;
                }

                if (!hasAnyEventInStronghold)
                {
                    warningNotes.Add($"Stronghold '{stronghold.name}' has no enabled wave events.");
                }
                else if (strongholdTypes.Count <= 1 && waves.Count >= 3)
                {
                    warningNotes.Add($"Stronghold '{stronghold.name}' event types are not rotating (single type only).");
                }
            }

            row.uniqueEventTypes = uniqueTypes.Count;
            row.reinforcementEvents = reinforcementCount;
            row.chaseEvents = chaseCount;
            row.holdEvents = holdCount;
            row.protectEvents = protectCount;

            if (row.totalWaves <= 0)
            {
                blockingNotes.Add("No waves found across strongholds.");
                return BuildRow(row, blockingNotes, warningNotes);
            }

            row.eventCoverage = row.totalWaves > 0 ? (float)row.wavesWithEvents / row.totalWaves : 0f;
            if (row.eventCoverage < 0.55f)
            {
                warningNotes.Add($"Wave event coverage is low ({row.eventCoverage * 100f:0.0}%).");
            }

            if (row.uniqueEventTypes < 2)
            {
                warningNotes.Add("Scene has fewer than 2 unique wave event types.");
            }

            if (row.longestSameTypeStreak >= 4)
            {
                warningNotes.Add($"Longest same-type streak is {row.longestSameTypeStreak}, rotation fatigue risk.");
            }

            if (row.rotationChains > 0)
            {
                float breakRate = (float)row.rotationBreaks / row.rotationChains;
                row.rotationBreakRate = breakRate;
                if (breakRate < 0.35f)
                {
                    warningNotes.Add($"Rotation break rate is low ({breakRate * 100f:0.0}%).");
                }
            }
            else
            {
                row.rotationBreakRate = 0f;
            }

            if (wavesWithoutEvents > 0 && row.totalWaves > 0)
            {
                float missingRatio = (float)wavesWithoutEvents / row.totalWaves;
                if (missingRatio > 0.25f)
                {
                    warningNotes.Add($"Waves without enabled events are high: {wavesWithoutEvents}/{row.totalWaves}.");
                }
            }

            if (row.chaseEvents <= 0 || row.holdEvents <= 0 || row.protectEvents <= 0)
            {
                warningNotes.Add("Missing one or more tactical event types (Chase/Hold/Protect).");
            }

            return BuildRow(row, blockingNotes, warningNotes);
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

        private static Dictionary<string, Dictionary<int, StrongholdWaveOverride>> BuildWaveOverrideLookup(LevelData levelData)
        {
            var result = new Dictionary<string, Dictionary<int, StrongholdWaveOverride>>(StringComparer.OrdinalIgnoreCase);
            if (levelData == null || levelData.strongholdOverrides == null || levelData.strongholdOverrides.Count == 0)
            {
                return result;
            }

            for (int i = 0; i < levelData.strongholdOverrides.Count; i++)
            {
                StrongholdOverride strongholdOverride = levelData.strongholdOverrides[i];
                if (strongholdOverride == null || string.IsNullOrWhiteSpace(strongholdOverride.strongholdId))
                {
                    continue;
                }

                if (!result.TryGetValue(strongholdOverride.strongholdId, out Dictionary<int, StrongholdWaveOverride> perWave))
                {
                    perWave = new Dictionary<int, StrongholdWaveOverride>();
                    result[strongholdOverride.strongholdId] = perWave;
                }

                if (strongholdOverride.waves == null)
                {
                    continue;
                }

                for (int w = 0; w < strongholdOverride.waves.Count; w++)
                {
                    StrongholdWaveOverride waveOverride = strongholdOverride.waves[w];
                    if (waveOverride == null || waveOverride.waveIndex < 0)
                    {
                        continue;
                    }

                    perWave[waveOverride.waveIndex] = waveOverride;
                }
            }

            return result;
        }

        private static StrongholdWaveOverride GetWaveOverride(
            Dictionary<string, Dictionary<int, StrongholdWaveOverride>> overrideLookup,
            string strongholdId,
            int waveIndex)
        {
            if (overrideLookup == null || overrideLookup.Count == 0 || string.IsNullOrWhiteSpace(strongholdId) || waveIndex < 0)
            {
                return null;
            }

            if (!overrideLookup.TryGetValue(strongholdId, out Dictionary<int, StrongholdWaveOverride> perWave) || perWave == null)
            {
                return null;
            }

            return perWave.TryGetValue(waveIndex, out StrongholdWaveOverride waveOverride) ? waveOverride : null;
        }

        private static bool TryGetPrimaryEnabledEventType(
            StrongholdWave wave,
            StrongholdWaveOverride waveOverride,
            out WaveEventType type)
        {
            type = WaveEventType.Reinforcement;
            if (waveOverride != null && waveOverride.replaceEvents)
            {
                return TryGetPrimaryOverrideEventType(waveOverride.events, out type);
            }

            if (TryGetPrimarySceneEventType(wave, out type))
            {
                return true;
            }

            if (waveOverride != null && !waveOverride.replaceEvents)
            {
                return TryGetPrimaryOverrideEventType(waveOverride.events, out type);
            }

            return false;
        }

        private static bool TryGetPrimarySceneEventType(StrongholdWave wave, out WaveEventType type)
        {
            type = WaveEventType.Reinforcement;
            if (wave == null || wave.events == null || wave.events.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < wave.events.Count; i++)
            {
                WaveEvent ev = wave.events[i];
                if (ev == null || !ev.enabled)
                {
                    continue;
                }

                type = ev.eventType;
                return true;
            }

            return false;
        }

        private static bool TryGetPrimaryOverrideEventType(List<WaveEventOverride> events, out WaveEventType type)
        {
            type = WaveEventType.Reinforcement;
            if (events == null || events.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < events.Count; i++)
            {
                WaveEventOverride ev = events[i];
                if (ev == null)
                {
                    continue;
                }

                type = ev.eventType;
                return true;
            }

            return false;
        }

        private static void CountType(
            WaveEventType type,
            ref int reinforcementCount,
            ref int chaseCount,
            ref int holdCount,
            ref int protectCount)
        {
            switch (type)
            {
                case WaveEventType.Reinforcement:
                    reinforcementCount++;
                    break;
                case WaveEventType.Chase:
                    chaseCount++;
                    break;
                case WaveEventType.HoldPoint:
                    holdCount++;
                    break;
                case WaveEventType.ProtectTarget:
                    protectCount++;
                    break;
            }
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
            return int.TryParse(raw, out int parsed) ? parsed : -1;
        }

        private static bool AssetExists(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return false;
            }

            return AssetDatabase.LoadMainAssetAtPath(assetPath) != null;
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
                "level_id,level_asset,scene_name,scene_path,status,blocking_errors,warnings,stronghold_count,total_waves,waves_with_events,event_coverage,unique_event_types,rotation_chains,rotation_breaks,rotation_break_rate,longest_same_type_streak,reinforcement_events,chase_events,hold_events,protect_events,note");

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
                    .Append(row.totalWaves).Append(',')
                    .Append(row.wavesWithEvents).Append(',')
                    .Append(row.eventCoverage.ToString("0.###")).Append(',')
                    .Append(row.uniqueEventTypes).Append(',')
                    .Append(row.rotationChains).Append(',')
                    .Append(row.rotationBreaks).Append(',')
                    .Append(row.rotationBreakRate.ToString("0.###")).Append(',')
                    .Append(row.longestSameTypeStreak).Append(',')
                    .Append(row.reinforcementEvents).Append(',')
                    .Append(row.chaseEvents).Append(',')
                    .Append(row.holdEvents).Append(',')
                    .Append(row.protectEvents).Append(',')
                    .Append(EscapeCsv(row.note))
                    .AppendLine();
            }

            File.WriteAllText(fullPath, csv.ToString(), new UTF8Encoding(false));
            return ReportCsvPath;
        }

        private static string WriteSummary(List<ValidationRow> rows, int errorRows, int warningTotal)
        {
            string fullPath = Path.GetFullPath(SummaryMdPath);
            EnsureDirectoryExists(fullPath);

            var md = new StringBuilder();
            md.AppendLine("# Wave Event Rotation Gate Summary");
            md.AppendLine();
            md.AppendLine($"- Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss zzz}");
            md.AppendLine($"- Targets: {rows.Count}");
            md.AppendLine($"- Error Rows: {errorRows}");
            md.AppendLine($"- Warning Count: {warningTotal}");
            md.AppendLine($"- CSV: {ReportCsvPath}");
            md.AppendLine();
            md.AppendLine("| Level | Scene | Status | Warnings | Waves | Coverage | Unique Types | Break Rate | Longest Streak | Note |");
            md.AppendLine("|---|---|---|---:|---:|---:|---:|---:|---:|---|");

            for (int i = 0; i < rows.Count; i++)
            {
                ValidationRow row = rows[i];
                md.Append("| ")
                    .Append(SafeMarkdownCell(row.levelId)).Append(" | ")
                    .Append(SafeMarkdownCell(row.sceneName)).Append(" | ")
                    .Append(SafeMarkdownCell(row.status)).Append(" | ")
                    .Append(row.warnings).Append(" | ")
                    .Append(row.totalWaves).Append(" | ")
                    .Append((row.eventCoverage * 100f).ToString("0.0")).Append("% | ")
                    .Append(row.uniqueEventTypes).Append(" | ")
                    .Append((row.rotationBreakRate * 100f).ToString("0.0")).Append("% | ")
                    .Append(row.longestSameTypeStreak).Append(" | ")
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
            public int blockingErrors;
            public int warnings;
            public int strongholdCount;
            public int totalWaves;
            public int wavesWithEvents;
            public float eventCoverage;
            public int uniqueEventTypes;
            public int rotationChains;
            public int rotationBreaks;
            public float rotationBreakRate;
            public int longestSameTypeStreak;
            public int reinforcementEvents;
            public int chaseEvents;
            public int holdEvents;
            public int protectEvents;
            public string note;
        }
    }
}
