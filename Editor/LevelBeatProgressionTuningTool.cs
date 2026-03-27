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
    public static class LevelBeatProgressionTuningTool
    {
        private const string ApplyMenuPath = "Tools/Level/P1/Apply Beat Progression Peak Spawn Tuning";
        private const string ValidateMenuPath = "Tools/Level/P1/Validate Beat Progression Peak Spawn Tuning";
        private const string ReportCsvPath = "Assets/ThirdPersonController/Reports/level_beat_progression_tuning_report.csv";
        private const string LogPrefix = "[LevelBeatProgressionTuning]";
        private const string SceneFolderPath = "Assets/Scenes";
        private const int MinLevelIndex = 2;
        private const int MaxLevelIndex = 10;

        private static readonly Dictionary<int, int> TargetPeakSpawnByLevel = new Dictionary<int, int>
        {
            { 2, 66 },
            { 3, 70 },
            { 4, 74 },
            { 5, 80 },
            { 6, 86 },
            { 7, 92 },
            { 8, 100 },
            { 9, 108 },
            { 10, 116 }
        };

        [MenuItem(ApplyMenuPath)]
        public static void Apply()
        {
            Run(applyFix: true, interactive: true, failOnError: false);
        }

        [MenuItem(ValidateMenuPath)]
        public static void Validate()
        {
            Run(applyFix: false, interactive: true, failOnError: false);
        }

        public static void ApplyForBatch()
        {
            Run(applyFix: true, interactive: false, failOnError: true);
        }

        public static void ValidateForBatch()
        {
            Run(applyFix: false, interactive: false, failOnError: true);
        }

        private static void Run(bool applyFix, bool interactive, bool failOnError)
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
                    EditorUtility.DisplayDialog("Beat Progression Tuning", noneMessage, "OK");
                }

                return;
            }

            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
            var rows = new List<ReportRow>(entries.Count);
            int changedSceneCount = 0;
            int errorRows = 0;
            int adjustedTotal = 0;

            try
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    ReportRow row = ProcessEntry(entries[i], applyFix);
                    rows.Add(row);
                    adjustedTotal += Mathf.Abs(row.appliedDelta);
                    if (row.changed)
                    {
                        changedSceneCount++;
                    }

                    if (string.Equals(row.status, "Error", StringComparison.Ordinal))
                    {
                        errorRows++;
                    }
                }
            }
            finally
            {
                if (originalSetup != null && originalSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
                }
            }

            string reportPath = WriteCsv(rows);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string summary =
                $"mode={(applyFix ? "apply" : "validate")} targets={rows.Count} changed={changedSceneCount} adjusted={adjustedTotal} errors={errorRows} csv={reportPath}";
            Debug.Log($"{LogPrefix} complete | {summary}");

            if (interactive)
            {
                EditorUtility.DisplayDialog("Beat Progression Tuning", summary, "OK");
            }

            if (failOnError && errorRows > 0)
            {
                throw new InvalidOperationException($"{LogPrefix} failed with errors={errorRows}. csv={reportPath}");
            }
        }

        private static ReportRow ProcessEntry(LevelEntry entry, bool applyFix)
        {
            var row = new ReportRow
            {
                levelIndex = entry.levelIndex,
                levelId = entry.levelData != null ? entry.levelData.levelId : string.Empty,
                scenePath = BuildScenePath(entry.levelData),
                status = "Error",
                note = string.Empty
            };

            if (entry.levelData == null)
            {
                row.note = "LevelData is null.";
                return row;
            }

            if (!TargetPeakSpawnByLevel.TryGetValue(entry.levelIndex, out int targetPeakSpawn))
            {
                row.status = "Ok";
                row.note = "No target configured for this level.";
                return row;
            }

            row.targetPeakSpawn = targetPeakSpawn;

            if (string.IsNullOrWhiteSpace(row.scenePath))
            {
                row.note = "sceneName is empty.";
                return row;
            }

            if (!AssetExists(row.scenePath))
            {
                row.note = "Scene asset is missing.";
                return row;
            }

            Scene scene;
            try
            {
                scene = EditorSceneManager.OpenScene(row.scenePath, OpenSceneMode.Single);
            }
            catch (Exception ex)
            {
                row.note = $"OpenScene failed: {ex.Message}";
                return row;
            }

            List<StrongholdController> strongholds = FindComponentsInScene<StrongholdController>(scene);
            List<WaveRef> waveRefs = BuildWaveRefs(strongholds);
            row.totalWaves = waveRefs.Count;

            if (waveRefs.Count == 0)
            {
                row.note = "No waves found in scene.";
                return row;
            }

            WindowRange opening = BuildOpeningWindow(waveRefs.Count);
            WindowRange teaching = BuildTeachingWindow(waveRefs.Count, opening);
            WindowRange peak = BuildPeakWindow(waveRefs.Count, teaching);

            int beforePeakSpawn = SumPeakSpawn(waveRefs, peak);
            row.beforePeakSpawn = beforePeakSpawn;
            row.peakWindow = $"{peak.Start}-{peak.End}";
            row.desiredDelta = targetPeakSpawn - beforePeakSpawn;

            if (!applyFix)
            {
                row.afterPeakSpawn = beforePeakSpawn;
                row.appliedDelta = 0;
                row.status = beforePeakSpawn == targetPeakSpawn ? "Ok" : "Gap";
                row.note = beforePeakSpawn == targetPeakSpawn
                    ? "Peak spawn matches target."
                    : $"Peak spawn mismatch. target={targetPeakSpawn} actual={beforePeakSpawn}";
                return row;
            }

            int appliedDelta = ApplyDeltaToPeakWaves(waveRefs, peak, row.desiredDelta);
            int afterPeakSpawn = SumPeakSpawn(waveRefs, peak);

            row.appliedDelta = appliedDelta;
            row.afterPeakSpawn = afterPeakSpawn;
            row.changed = appliedDelta != 0;

            if (row.changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                {
                    row.status = "Error";
                    row.note = "SaveScene returned false.";
                    return row;
                }
            }

            int remainingDelta = targetPeakSpawn - afterPeakSpawn;
            if (remainingDelta != 0)
            {
                row.status = "Error";
                row.note =
                    $"Could not reach target peak spawn. target={targetPeakSpawn} actual={afterPeakSpawn} remainingDelta={remainingDelta}";
                return row;
            }

            row.status = "Ok";
            row.note = row.changed
                ? $"Applied peak spawn tuning. target={targetPeakSpawn} before={beforePeakSpawn} after={afterPeakSpawn}"
                : "Already matches target.";
            return row;
        }

        private static int ApplyDeltaToPeakWaves(List<WaveRef> waveRefs, WindowRange peakWindow, int desiredDelta)
        {
            if (desiredDelta == 0 || waveRefs == null || waveRefs.Count == 0)
            {
                return 0;
            }

            var targetGroups = new List<WaveSpawnGroup>();
            for (int i = 0; i < waveRefs.Count; i++)
            {
                if (!peakWindow.Contains(i))
                {
                    continue;
                }

                List<WaveSpawnGroup> groups = waveRefs[i].groups;
                if (groups == null)
                {
                    continue;
                }

                for (int g = 0; g < groups.Count; g++)
                {
                    WaveSpawnGroup group = groups[g];
                    if (group != null && group.prefab != null)
                    {
                        targetGroups.Add(group);
                    }
                }
            }

            if (targetGroups.Count == 0)
            {
                return 0;
            }

            if (desiredDelta > 0)
            {
                int cursor = 0;
                int remaining = desiredDelta;
                while (remaining > 0)
                {
                    WaveSpawnGroup group = targetGroups[cursor % targetGroups.Count];
                    group.count = Mathf.Max(1, group.count + 1);
                    remaining--;
                    cursor++;
                }

                return desiredDelta;
            }

            int requestedReduction = -desiredDelta;
            int reduced = 0;
            bool anyReduced = true;
            while (reduced < requestedReduction && anyReduced)
            {
                anyReduced = false;
                for (int i = 0; i < targetGroups.Count; i++)
                {
                    if (reduced >= requestedReduction)
                    {
                        break;
                    }

                    WaveSpawnGroup group = targetGroups[i];
                    if (group.count > 1)
                    {
                        group.count--;
                        reduced++;
                        anyReduced = true;
                    }
                }
            }

            return -reduced;
        }

        private static int SumPeakSpawn(List<WaveRef> waveRefs, WindowRange peakWindow)
        {
            if (waveRefs == null || waveRefs.Count == 0)
            {
                return 0;
            }

            int total = 0;
            for (int i = 0; i < waveRefs.Count; i++)
            {
                if (peakWindow.Contains(i))
                {
                    total += SumGroupSpawn(waveRefs[i].groups);
                }
            }

            return total;
        }

        private static int SumGroupSpawn(List<WaveSpawnGroup> groups)
        {
            if (groups == null || groups.Count == 0)
            {
                return 0;
            }

            int total = 0;
            for (int i = 0; i < groups.Count; i++)
            {
                WaveSpawnGroup group = groups[i];
                if (group != null && group.prefab != null && group.count > 0)
                {
                    total += group.count;
                }
            }

            return total;
        }

        private static List<WaveRef> BuildWaveRefs(List<StrongholdController> strongholds)
        {
            var refs = new List<WaveRef>();
            if (strongholds == null)
            {
                return refs;
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
                    List<WaveSpawnGroup> groups = wave.groups;
                    if (groups != null)
                    {
                        for (int g = 0; g < groups.Count; g++)
                        {
                            WaveSpawnGroup group = groups[g];
                            if (group != null && group.prefab != null && group.count > 0)
                            {
                                baseSpawn += group.count;
                            }
                        }
                    }

                    refs.Add(new WaveRef
                    {
                        baseSpawn = baseSpawn,
                        groups = groups
                    });
                }
            }

            return refs;
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

            return new WindowRange(start, totalWaves - 1);
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
                for (int c = 0; c < components.Length; c++)
                {
                    T component = components[c];
                    if (component != null)
                    {
                        result.Add(component);
                    }
                }
            }

            return result;
        }

        private static string WriteCsv(List<ReportRow> rows)
        {
            string fullPath = Path.GetFullPath(ReportCsvPath);
            string directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var csv = new StringBuilder();
            csv.AppendLine(
                "level_index,level_id,scene_path,status,total_waves,peak_window,target_peak_spawn,before_peak_spawn,desired_delta,applied_delta,after_peak_spawn,changed,note");

            for (int i = 0; i < rows.Count; i++)
            {
                ReportRow row = rows[i];
                csv.Append(row.levelIndex).Append(',')
                    .Append(EscapeCsv(row.levelId)).Append(',')
                    .Append(EscapeCsv(row.scenePath)).Append(',')
                    .Append(EscapeCsv(row.status)).Append(',')
                    .Append(row.totalWaves).Append(',')
                    .Append(EscapeCsv(row.peakWindow)).Append(',')
                    .Append(row.targetPeakSpawn).Append(',')
                    .Append(row.beforePeakSpawn).Append(',')
                    .Append(row.desiredDelta).Append(',')
                    .Append(row.appliedDelta).Append(',')
                    .Append(row.afterPeakSpawn).Append(',')
                    .Append(row.changed ? "1" : "0").Append(',')
                    .Append(EscapeCsv(row.note))
                    .AppendLine();
            }

            File.WriteAllText(fullPath, csv.ToString(), new UTF8Encoding(false));
            return ReportCsvPath;
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

        private struct LevelEntry
        {
            public LevelData levelData;
            public int levelIndex;
        }

        private struct WaveRef
        {
            public int baseSpawn;
            public List<WaveSpawnGroup> groups;
        }

        private struct ReportRow
        {
            public int levelIndex;
            public string levelId;
            public string scenePath;
            public string status;
            public int totalWaves;
            public string peakWindow;
            public int targetPeakSpawn;
            public int beforePeakSpawn;
            public int desiredDelta;
            public int appliedDelta;
            public int afterPeakSpawn;
            public bool changed;
            public string note;
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
