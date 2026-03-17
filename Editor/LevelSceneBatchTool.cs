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
    public static class LevelSceneBatchTool
    {
        private const string BuildMenuPath = "Tools/Level/P0/Build Level Scenes 02-10";
        private const string ValidateMenuPath = "Tools/Level/P0/Validate Level Scenes 02-10";
        private const string TemplateScenePath = "Assets/Scenes/SampleScene_Template.unity";
        private const string SceneFolderPath = "Assets/Scenes";
        private const string BuildReportCsvPath = "Assets/ThirdPersonController/Reports/level_scene_batch_build_report.csv";
        private const string ValidateReportCsvPath = "Assets/ThirdPersonController/Reports/level_scene_batch_validate_report.csv";
        private const int MinLevelIndex = 2;
        private const int MaxLevelIndex = 10;
        private const string LogPrefix = "[LevelSceneBatch]";

        [MenuItem(BuildMenuPath)]
        public static void BuildLevelScenes()
        {
            RunBuild(interactive: true, failOnError: false);
        }

        [MenuItem(ValidateMenuPath)]
        public static void ValidateLevelScenes()
        {
            RunValidate(interactive: true, failOnError: false);
        }

        public static void BuildLevelScenesForBatch()
        {
            RunBuild(interactive: false, failOnError: true);
        }

        public static void ValidateLevelScenesForBatch()
        {
            RunValidate(interactive: false, failOnError: true);
        }

        private static void RunBuild(bool interactive, bool failOnError)
        {
            if (!AssetExists(TemplateScenePath))
            {
                string missingMessage = $"{LogPrefix} missing template scene: {TemplateScenePath}";
                Debug.LogError(missingMessage);
                if (interactive)
                {
                    EditorUtility.DisplayDialog("Level Scene Batch Build", missingMessage, "OK");
                }

                if (failOnError)
                {
                    throw new InvalidOperationException(missingMessage);
                }

                return;
            }

            if (interactive && !Application.isBatchMode)
            {
                bool allow = EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                if (!allow)
                {
                    return;
                }
            }

            List<LevelEntry> levelEntries = CollectTargetLevels();
            if (levelEntries.Count == 0)
            {
                string noneMessage = $"{LogPrefix} no LevelData assets found for LEVEL_{MinLevelIndex:D2}~LEVEL_{MaxLevelIndex:D2}.";
                Debug.LogWarning(noneMessage);
                if (interactive)
                {
                    EditorUtility.DisplayDialog("Level Scene Batch Build", noneMessage, "OK");
                }

                return;
            }

            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
            var rows = new List<ReportRow>();
            int createdCount = 0;
            int updatedCount = 0;
            int savedCount = 0;
            int errorCount = 0;

            try
            {
                for (int i = 0; i < levelEntries.Count; i++)
                {
                    LevelEntry entry = levelEntries[i];
                    string scenePath = BuildScenePath(entry.levelData);
                    string note = string.Empty;
                    bool created = false;
                    bool updated = false;
                    bool saved = false;
                    string status = "Ok";

                    try
                    {
                        if (string.IsNullOrWhiteSpace(scenePath))
                        {
                            status = "Error";
                            note = "sceneName is empty on LevelData.";
                            errorCount++;
                            rows.Add(CreateRow(entry, scenePath, status, created, updated, saved, note));
                            continue;
                        }

                        if (!AssetExists(scenePath))
                        {
                            bool copied = AssetDatabase.CopyAsset(TemplateScenePath, scenePath);
                            if (!copied)
                            {
                                status = "Error";
                                note = $"Failed to copy template scene to '{scenePath}'.";
                                errorCount++;
                                rows.Add(CreateRow(entry, scenePath, status, created, updated, saved, note));
                                continue;
                            }

                            created = true;
                            createdCount++;
                        }

                        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                        if (!scene.IsValid())
                        {
                            status = "Error";
                            note = "Scene open returned invalid scene.";
                            errorCount++;
                            rows.Add(CreateRow(entry, scenePath, status, created, updated, saved, note));
                            continue;
                        }

                        bool hasErrors;
                        updated = ApplySceneWiring(scene, entry, out hasErrors, out note);

                        if (updated)
                        {
                            EditorSceneManager.MarkSceneDirty(scene);
                            if (EditorSceneManager.SaveScene(scene))
                            {
                                saved = true;
                                savedCount++;
                                updatedCount++;
                            }
                            else
                            {
                                status = "Error";
                                note = AppendNote(note, "SaveScene returned false.");
                                errorCount++;
                            }
                        }

                        if (hasErrors && status != "Error")
                        {
                            status = "Error";
                            errorCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        status = "Error";
                        note = ex.Message;
                        errorCount++;
                    }

                    rows.Add(CreateRow(entry, scenePath, status, created, updated, saved, note));
                }
            }
            finally
            {
                if (originalSetup != null && originalSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
                }
            }

            int buildSettingsAdded = SyncBuildSettings(levelEntries);
            string reportPath = WriteReport(rows, BuildReportCsvPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string summary =
                $"targets={levelEntries.Count} created={createdCount} updated={updatedCount} saved={savedCount} " +
                $"buildSettingsAdded={buildSettingsAdded} errors={errorCount} report={reportPath}";
            Debug.Log($"{LogPrefix} build complete | {summary}");

            if (interactive)
            {
                EditorUtility.DisplayDialog("Level Scene Batch Build", summary, "OK");
            }

            if (failOnError && errorCount > 0)
            {
                throw new InvalidOperationException($"{LogPrefix} build failed with errors={errorCount}. report={reportPath}");
            }
        }

        private static void RunValidate(bool interactive, bool failOnError)
        {
            List<LevelEntry> levelEntries = CollectTargetLevels();
            if (levelEntries.Count == 0)
            {
                string noneMessage = $"{LogPrefix} no LevelData assets found for validation.";
                Debug.LogWarning(noneMessage);
                if (interactive)
                {
                    EditorUtility.DisplayDialog("Level Scene Validation", noneMessage, "OK");
                }

                return;
            }

            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
            var rows = new List<ReportRow>();
            int errorCount = 0;

            try
            {
                for (int i = 0; i < levelEntries.Count; i++)
                {
                    LevelEntry entry = levelEntries[i];
                    string scenePath = BuildScenePath(entry.levelData);
                    string status = "Ok";
                    string note = string.Empty;

                    if (string.IsNullOrWhiteSpace(scenePath))
                    {
                        status = "Error";
                        note = "sceneName is empty on LevelData.";
                        errorCount++;
                        rows.Add(CreateRow(entry, scenePath, status, created: false, updated: false, saved: false, note));
                        continue;
                    }

                    if (!AssetExists(scenePath))
                    {
                        status = "Error";
                        note = "Scene asset not found.";
                        errorCount++;
                        rows.Add(CreateRow(entry, scenePath, status, created: false, updated: false, saved: false, note));
                        continue;
                    }

                    try
                    {
                        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                        if (!scene.IsValid())
                        {
                            status = "Error";
                            note = "Scene open returned invalid scene.";
                            errorCount++;
                            rows.Add(CreateRow(entry, scenePath, status, created: false, updated: false, saved: false, note));
                            continue;
                        }

                        bool hasErrors;
                        ValidateSceneWiring(scene, entry, out hasErrors, out note);
                        if (hasErrors)
                        {
                            status = "Error";
                            errorCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        status = "Error";
                        note = ex.Message;
                        errorCount++;
                    }

                    rows.Add(CreateRow(entry, scenePath, status, created: false, updated: false, saved: false, note));
                }
            }
            finally
            {
                if (originalSetup != null && originalSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
                }
            }

            string reportPath = WriteReport(rows, ValidateReportCsvPath);
            AssetDatabase.Refresh();

            string summary = $"targets={levelEntries.Count} errors={errorCount} report={reportPath}";
            Debug.Log($"{LogPrefix} validation complete | {summary}");

            if (interactive)
            {
                EditorUtility.DisplayDialog("Level Scene Validation", summary, "OK");
            }

            if (failOnError && errorCount > 0)
            {
                throw new InvalidOperationException($"{LogPrefix} validation failed with errors={errorCount}. report={reportPath}");
            }
        }

        private static bool ApplySceneWiring(Scene scene, LevelEntry entry, out bool hasErrors, out string note)
        {
            bool changed = false;
            hasErrors = false;
            var notes = new List<string>();

            LevelFlowController levelFlow = FindComponentInScene<LevelFlowController>(scene);
            StrongholdSequenceController sequence = FindComponentInScene<StrongholdSequenceController>(scene);
            BossSpawnPoint bossSpawnPoint = FindComponentInScene<BossSpawnPoint>(scene);

            if (levelFlow == null)
            {
                hasErrors = true;
                notes.Add("Missing LevelFlowController.");
            }
            else
            {
                if (levelFlow.levelData != entry.levelData)
                {
                    levelFlow.levelData = entry.levelData;
                    changed = true;
                }

                if (entry.runtimeLevelId > 0 && levelFlow.levelId != entry.runtimeLevelId)
                {
                    levelFlow.levelId = entry.runtimeLevelId;
                    changed = true;
                }

                string expectedTitle = string.IsNullOrWhiteSpace(entry.levelData.levelName)
                    ? levelFlow.levelTitle
                    : entry.levelData.levelName;
                if (!string.Equals(levelFlow.levelTitle, expectedTitle, StringComparison.Ordinal))
                {
                    levelFlow.levelTitle = expectedTitle;
                    changed = true;
                }
            }

            if (sequence == null)
            {
                hasErrors = true;
                notes.Add("Missing StrongholdSequenceController.");
            }
            else
            {
                if (entry.runtimeLevelId > 0 && sequence.levelId != entry.runtimeLevelId)
                {
                    sequence.levelId = entry.runtimeLevelId;
                    changed = true;
                }

                bool useBossGate = entry.levelData.overrideBossSettings && bossSpawnPoint != null;
                if (sequence.deferCompletionUntilBoss != useBossGate)
                {
                    sequence.deferCompletionUntilBoss = useBossGate;
                    changed = true;
                }

                BossSpawnPoint expectedBossRef = useBossGate ? bossSpawnPoint : null;
                if (sequence.bossSpawnPoint != expectedBossRef)
                {
                    sequence.bossSpawnPoint = expectedBossRef;
                    changed = true;
                }
            }

            if (bossSpawnPoint == null)
            {
                if (entry.levelData.overrideBossSettings)
                {
                    hasErrors = true;
                    notes.Add("Missing BossSpawnPoint for boss-enabled level.");
                }
                else
                {
                    notes.Add("BossSpawnPoint not found.");
                }
            }
            else
            {
                if (bossSpawnPoint.spawnOnStart)
                {
                    bossSpawnPoint.spawnOnStart = false;
                    changed = true;
                }

                if (entry.levelData.overrideBossSettings)
                {
                    changed |= ApplyBossSettingsFromLevelData(bossSpawnPoint, entry.levelData);
                }
            }

            note = notes.Count > 0 ? string.Join(" ", notes) : string.Empty;
            return changed;
        }

        private static void ValidateSceneWiring(Scene scene, LevelEntry entry, out bool hasErrors, out string note)
        {
            hasErrors = false;
            var notes = new List<string>();

            LevelFlowController levelFlow = FindComponentInScene<LevelFlowController>(scene);
            StrongholdSequenceController sequence = FindComponentInScene<StrongholdSequenceController>(scene);
            BossSpawnPoint bossSpawnPoint = FindComponentInScene<BossSpawnPoint>(scene);

            if (levelFlow == null)
            {
                hasErrors = true;
                notes.Add("Missing LevelFlowController.");
            }
            else
            {
                if (levelFlow.levelData != entry.levelData)
                {
                    hasErrors = true;
                    notes.Add("LevelFlow.levelData mismatch.");
                }
            }

            if (sequence == null)
            {
                hasErrors = true;
                notes.Add("Missing StrongholdSequenceController.");
            }
            else
            {
                bool expectBossGate = entry.levelData.overrideBossSettings && bossSpawnPoint != null;
                if (sequence.deferCompletionUntilBoss != expectBossGate)
                {
                    hasErrors = true;
                    notes.Add($"deferCompletionUntilBoss mismatch (expected={expectBossGate}).");
                }

                BossSpawnPoint expectedRef = expectBossGate ? bossSpawnPoint : null;
                if (sequence.bossSpawnPoint != expectedRef)
                {
                    hasErrors = true;
                    notes.Add("bossSpawnPoint reference mismatch.");
                }
            }

            if (bossSpawnPoint == null)
            {
                if (entry.levelData.overrideBossSettings)
                {
                    hasErrors = true;
                    notes.Add("Missing BossSpawnPoint for boss-enabled level.");
                }
            }
            else if (bossSpawnPoint.spawnOnStart)
            {
                hasErrors = true;
                notes.Add("BossSpawnPoint.spawnOnStart should be false.");
            }

            note = notes.Count > 0 ? string.Join(" ", notes) : string.Empty;
        }

        private static bool ApplyBossSettingsFromLevelData(BossSpawnPoint bossSpawnPoint, LevelData levelData)
        {
            bool changed = false;
            if (bossSpawnPoint.prototype != levelData.bossPrototype)
            {
                bossSpawnPoint.prototype = levelData.bossPrototype;
                changed = true;
            }

            if (!string.IsNullOrWhiteSpace(levelData.bossName) &&
                !string.Equals(bossSpawnPoint.bossName, levelData.bossName, StringComparison.Ordinal))
            {
                bossSpawnPoint.bossName = levelData.bossName;
                changed = true;
            }

            int expectedHealth = Mathf.Max(1, levelData.bossMaxHealth);
            if (bossSpawnPoint.maxHealth != expectedHealth)
            {
                bossSpawnPoint.maxHealth = expectedHealth;
                changed = true;
            }

            int expectedDamage = Mathf.Max(1, levelData.bossBaseDamage);
            if (bossSpawnPoint.baseDamage != expectedDamage)
            {
                bossSpawnPoint.baseDamage = expectedDamage;
                changed = true;
            }

            if (!Mathf.Approximately(bossSpawnPoint.knockback, levelData.bossKnockback))
            {
                bossSpawnPoint.knockback = levelData.bossKnockback;
                changed = true;
            }

            float expectedScale = Mathf.Max(0.1f, levelData.bossScaleMultiplier);
            if (!Mathf.Approximately(bossSpawnPoint.scaleMultiplier, expectedScale))
            {
                bossSpawnPoint.scaleMultiplier = expectedScale;
                changed = true;
            }

            if (bossSpawnPoint.spawnOffset != levelData.bossSpawnOffset)
            {
                bossSpawnPoint.spawnOffset = levelData.bossSpawnOffset;
                changed = true;
            }

            return changed;
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

                int runtimeLevelId = ResolveRuntimeLevelId(levelData, levelIndex);
                entries.Add(new LevelEntry
                {
                    levelData = levelData,
                    levelIndex = levelIndex,
                    runtimeLevelId = runtimeLevelId
                });
            }

            entries.Sort((a, b) => a.levelIndex.CompareTo(b.levelIndex));
            return entries;
        }

        private static int SyncBuildSettings(List<LevelEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return 0;
            }

            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < scenes.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(scenes[i].path))
                {
                    existing.Add(scenes[i].path);
                }
            }

            int added = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                string scenePath = BuildScenePath(entries[i].levelData);
                if (string.IsNullOrWhiteSpace(scenePath))
                {
                    continue;
                }

                if (!AssetExists(scenePath))
                {
                    continue;
                }

                if (existing.Contains(scenePath))
                {
                    continue;
                }

                scenes.Add(new EditorBuildSettingsScene(scenePath, true));
                existing.Add(scenePath);
                added++;
            }

            if (added > 0)
            {
                EditorBuildSettings.scenes = scenes.ToArray();
            }

            return added;
        }

        private static string WriteReport(List<ReportRow> rows, string reportAssetPath)
        {
            string fullPath = Path.GetFullPath(reportAssetPath);
            string directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var csv = new StringBuilder();
            csv.AppendLine("level_id,scene_name,scene_path,status,created,updated,saved,note");
            for (int i = 0; i < rows.Count; i++)
            {
                ReportRow row = rows[i];
                csv.Append(EscapeCsv(row.levelId)).Append(',')
                    .Append(EscapeCsv(row.sceneName)).Append(',')
                    .Append(EscapeCsv(row.scenePath)).Append(',')
                    .Append(EscapeCsv(row.status)).Append(',')
                    .Append(row.created ? "1" : "0").Append(',')
                    .Append(row.updated ? "1" : "0").Append(',')
                    .Append(row.saved ? "1" : "0").Append(',')
                    .Append(EscapeCsv(row.note))
                    .AppendLine();
            }

            File.WriteAllText(fullPath, csv.ToString(), new UTF8Encoding(false));
            return reportAssetPath;
        }

        private static string BuildScenePath(LevelData levelData)
        {
            if (levelData == null || string.IsNullOrWhiteSpace(levelData.sceneName))
            {
                return string.Empty;
            }

            string sceneName = levelData.sceneName.Trim();
            return $"{SceneFolderPath}/{sceneName}.unity";
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

        private static int ResolveRuntimeLevelId(LevelData levelData, int fallbackIndex)
        {
            if (levelData == null)
            {
                return 0;
            }

            int levelIndex = fallbackIndex > 0 ? fallbackIndex : ParseLevelIndex(levelData.levelId);
            if (levelData.chapterId > 0 && levelIndex > 0)
            {
                return levelData.chapterId * 100 + levelIndex;
            }

            return 0;
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

        private static bool AssetExists(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return false;
            }

            UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            return asset != null;
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

        private static string AppendNote(string current, string next)
        {
            if (string.IsNullOrWhiteSpace(current))
            {
                return next ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(next))
            {
                return current;
            }

            return current + " " + next;
        }

        private static ReportRow CreateRow(
            LevelEntry entry,
            string scenePath,
            string status,
            bool created,
            bool updated,
            bool saved,
            string note)
        {
            return new ReportRow
            {
                levelId = entry.levelData != null ? entry.levelData.levelId : string.Empty,
                sceneName = entry.levelData != null ? entry.levelData.sceneName : string.Empty,
                scenePath = scenePath ?? string.Empty,
                status = status ?? string.Empty,
                created = created,
                updated = updated,
                saved = saved,
                note = note ?? string.Empty
            };
        }

        private struct LevelEntry
        {
            public LevelData levelData;
            public int levelIndex;
            public int runtimeLevelId;
        }

        private struct ReportRow
        {
            public string levelId;
            public string sceneName;
            public string scenePath;
            public string status;
            public bool created;
            public bool updated;
            public bool saved;
            public string note;
        }
    }
}
