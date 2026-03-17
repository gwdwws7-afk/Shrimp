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
    public static class LevelDataSceneValidator
    {
        private const string ValidateMenuPath = "Tools/Level/P0/Validate LevelData-Scene Links (CSV)";
        private const string ValidateGateMenuPath = "Tools/Level/P0/Validate LevelData-Scene Links (CI Gate)";
        private const string FixMenuPath = "Tools/Level/P0/Fix LevelData-Scene Links";

        private const string TemplateScenePath = "Assets/Scenes/SampleScene_Template.unity";
        private const string SceneFolderPath = "Assets/Scenes";
        private const string ReportCsvPath = "Assets/ThirdPersonController/Reports/level_data_scene_validator_report.csv";
        private const string LogPrefix = "[LevelDataSceneValidator]";

        [MenuItem(ValidateMenuPath)]
        public static void Validate()
        {
            Run(applyFix: false, interactive: true, failOnError: false);
        }

        [MenuItem(ValidateGateMenuPath)]
        public static void ValidateCiGate()
        {
            Run(applyFix: false, interactive: false, failOnError: true);
        }

        [MenuItem(FixMenuPath)]
        public static void Fix()
        {
            Run(applyFix: true, interactive: true, failOnError: false);
        }

        public static void ValidateForBatch()
        {
            Run(applyFix: false, interactive: false, failOnError: true);
        }

        public static void FixForBatch()
        {
            Run(applyFix: true, interactive: false, failOnError: true);
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

            List<LevelEntry> levels = CollectLevelEntries();
            if (levels.Count == 0)
            {
                string noneMessage = $"{LogPrefix} no LevelData assets with LEVEL_* id found.";
                Debug.LogWarning(noneMessage);
                if (interactive)
                {
                    EditorUtility.DisplayDialog("LevelData Scene Validator", noneMessage, "OK");
                }

                return;
            }

            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
            List<EditorBuildSettingsScene> buildSettingsScenes =
                new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            var rows = new List<ValidationRow>();
            int errorCount = 0;
            int fixedCount = 0;
            int createdSceneCount = 0;
            int buildSettingsMutations = 0;

            bool templateExists = AssetExists(TemplateScenePath);

            try
            {
                for (int i = 0; i < levels.Count; i++)
                {
                    LevelEntry entry = levels[i];
                    ValidationRow row = ValidateEntry(
                        entry,
                        applyFix,
                        templateExists,
                        buildSettingsScenes,
                        ref fixedCount,
                        ref createdSceneCount,
                        ref buildSettingsMutations);

                    if (string.Equals(row.status, "Error", StringComparison.Ordinal))
                    {
                        errorCount++;
                    }

                    rows.Add(row);
                }
            }
            finally
            {
                if (originalSetup != null && originalSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
                }
            }

            if (applyFix && buildSettingsMutations > 0)
            {
                EditorBuildSettings.scenes = buildSettingsScenes.ToArray();
            }

            string reportPath = WriteReport(rows);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string mode = applyFix ? "fix" : "validate";
            string summary =
                $"mode={mode} targets={levels.Count} errors={errorCount} fixed={fixedCount} " +
                $"createdScenes={createdSceneCount} buildSettingsMutations={buildSettingsMutations} report={reportPath}";
            Debug.Log($"{LogPrefix} complete | {summary}");

            if (interactive)
            {
                EditorUtility.DisplayDialog("LevelData Scene Validator", summary, "OK");
            }

            if (failOnError && errorCount > 0)
            {
                throw new InvalidOperationException(
                    $"{LogPrefix} {mode} failed with errors={errorCount}. report={reportPath}");
            }
        }

        private static ValidationRow ValidateEntry(
            LevelEntry entry,
            bool applyFix,
            bool templateExists,
            List<EditorBuildSettingsScene> buildSettingsScenes,
            ref int globalFixedCount,
            ref int globalCreatedSceneCount,
            ref int globalBuildSettingsMutations)
        {
            var notes = new List<string>();
            int issues = 0;
            int fixedCount = 0;
            int unresolved = 0;
            bool sceneCreated = false;
            bool sceneExists;
            bool inBuildSettings;
            string scenePath = BuildScenePath(entry.levelData);

            if (string.IsNullOrWhiteSpace(scenePath))
            {
                issues++;
                unresolved++;
                notes.Add("sceneName is empty on LevelData.");
                sceneExists = false;
                inBuildSettings = false;
                return BuildRow(entry, scenePath, sceneExists, inBuildSettings, issues, fixedCount, unresolved, notes);
            }

            sceneExists = AssetExists(scenePath);
            if (!sceneExists)
            {
                issues++;
                if (applyFix && templateExists)
                {
                    if (EnsureParentFolder(scenePath) && AssetDatabase.CopyAsset(TemplateScenePath, scenePath))
                    {
                        sceneExists = true;
                        sceneCreated = true;
                        fixedCount++;
                        notes.Add($"Created scene from template: {TemplateScenePath}");
                    }
                    else
                    {
                        unresolved++;
                        notes.Add($"Failed to create scene from template: {TemplateScenePath}");
                    }
                }
                else
                {
                    unresolved++;
                    if (!templateExists)
                    {
                        notes.Add($"Scene missing and template not found: {TemplateScenePath}");
                    }
                    else
                    {
                        notes.Add("Scene asset missing.");
                    }
                }
            }

            inBuildSettings = IsSceneEnabledInBuildSettings(buildSettingsScenes, scenePath);
            if (!inBuildSettings)
            {
                issues++;
                if (applyFix)
                {
                    if (UpsertBuildSettingsScene(buildSettingsScenes, scenePath))
                    {
                        inBuildSettings = true;
                        fixedCount++;
                        globalBuildSettingsMutations++;
                        notes.Add("Added/enabled scene in BuildSettings.");
                    }
                    else
                    {
                        unresolved++;
                        notes.Add("Failed to add scene in BuildSettings.");
                    }
                }
                else
                {
                    unresolved++;
                    notes.Add("Scene not enabled in BuildSettings.");
                }
            }

            if (sceneExists)
            {
                Scene scene;
                try
                {
                    scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                }
                catch (Exception ex)
                {
                    issues++;
                    unresolved++;
                    notes.Add($"OpenScene failed: {ex.Message}");
                    return BuildRow(entry, scenePath, sceneExists, inBuildSettings, issues, fixedCount, unresolved, notes);
                }

                bool sceneDirty = false;

                LevelFlowController levelFlow = FindComponentInScene<LevelFlowController>(scene);
                if (levelFlow == null)
                {
                    issues++;
                    unresolved++;
                    notes.Add("Missing LevelFlowController.");
                }
                else
                {
                    if (levelFlow.levelData != entry.levelData)
                    {
                        issues++;
                        if (applyFix)
                        {
                            levelFlow.levelData = entry.levelData;
                            sceneDirty = true;
                            fixedCount++;
                            notes.Add("Fixed LevelFlow.levelData reference.");
                        }
                        else
                        {
                            unresolved++;
                            notes.Add("LevelFlow.levelData mismatch.");
                        }
                    }

                    int runtimeLevelId = ResolveRuntimeLevelId(entry.levelData);
                    if (runtimeLevelId > 0 && levelFlow.levelId != runtimeLevelId)
                    {
                        issues++;
                        if (applyFix)
                        {
                            levelFlow.levelId = runtimeLevelId;
                            sceneDirty = true;
                            fixedCount++;
                            notes.Add("Fixed LevelFlow.levelId.");
                        }
                        else
                        {
                            unresolved++;
                            notes.Add($"LevelFlow.levelId mismatch (expected {runtimeLevelId}).");
                        }
                    }
                }

                StrongholdSequenceController sequence = FindComponentInScene<StrongholdSequenceController>(scene);
                if (sequence == null)
                {
                    issues++;
                    unresolved++;
                    notes.Add("Missing StrongholdSequenceController.");
                }

                BossSpawnPoint bossSpawnPoint = FindComponentInScene<BossSpawnPoint>(scene);
                bool expectBossGate = entry.levelData.overrideBossSettings && bossSpawnPoint != null;

                if (entry.levelData.overrideBossSettings && bossSpawnPoint == null)
                {
                    issues++;
                    unresolved++;
                    notes.Add("Boss enabled in LevelData but BossSpawnPoint is missing.");
                }

                if (bossSpawnPoint != null && bossSpawnPoint.spawnOnStart)
                {
                    issues++;
                    if (applyFix)
                    {
                        bossSpawnPoint.spawnOnStart = false;
                        sceneDirty = true;
                        fixedCount++;
                        notes.Add("Fixed BossSpawnPoint.spawnOnStart=false.");
                    }
                    else
                    {
                        unresolved++;
                        notes.Add("BossSpawnPoint.spawnOnStart should be false.");
                    }
                }

                if (sequence != null)
                {
                    if (sequence.deferCompletionUntilBoss != expectBossGate)
                    {
                        issues++;
                        if (applyFix)
                        {
                            sequence.deferCompletionUntilBoss = expectBossGate;
                            sceneDirty = true;
                            fixedCount++;
                            notes.Add("Fixed deferCompletionUntilBoss.");
                        }
                        else
                        {
                            unresolved++;
                            notes.Add($"deferCompletionUntilBoss mismatch (expected {expectBossGate}).");
                        }
                    }

                    BossSpawnPoint expectedRef = expectBossGate ? bossSpawnPoint : null;
                    if (sequence.bossSpawnPoint != expectedRef)
                    {
                        issues++;
                        if (applyFix)
                        {
                            sequence.bossSpawnPoint = expectedRef;
                            sceneDirty = true;
                            fixedCount++;
                            notes.Add("Fixed StrongholdSequence.bossSpawnPoint reference.");
                        }
                        else
                        {
                            unresolved++;
                            notes.Add("StrongholdSequence.bossSpawnPoint mismatch.");
                        }
                    }

                    int runtimeLevelId = ResolveRuntimeLevelId(entry.levelData);
                    if (runtimeLevelId > 0 && sequence.levelId != runtimeLevelId)
                    {
                        issues++;
                        if (applyFix)
                        {
                            sequence.levelId = runtimeLevelId;
                            sceneDirty = true;
                            fixedCount++;
                            notes.Add("Fixed StrongholdSequence.levelId.");
                        }
                        else
                        {
                            unresolved++;
                            notes.Add($"StrongholdSequence.levelId mismatch (expected {runtimeLevelId}).");
                        }
                    }
                }

                if (applyFix && sceneDirty)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene))
                    {
                        unresolved++;
                        notes.Add("SaveScene returned false.");
                    }
                }
            }

            if (sceneCreated)
            {
                globalCreatedSceneCount++;
            }

            globalFixedCount += fixedCount;

            return BuildRow(entry, scenePath, sceneExists, inBuildSettings, issues, fixedCount, unresolved, notes);
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

                int parsedLevelIndex = ParseLevelIndex(levelData.levelId);
                if (parsedLevelIndex <= 0)
                {
                    continue;
                }

                result.Add(new LevelEntry
                {
                    levelData = levelData,
                    levelAssetPath = assetPath,
                    levelIndex = parsedLevelIndex
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

                return string.Compare(a.levelData.levelId, b.levelData.levelId, StringComparison.Ordinal);
            });

            return result;
        }

        private static ValidationRow BuildRow(
            LevelEntry entry,
            string scenePath,
            bool sceneExists,
            bool inBuildSettings,
            int issues,
            int fixedCount,
            int unresolved,
            List<string> notes)
        {
            string status;
            if (unresolved > 0)
            {
                status = "Error";
            }
            else if (fixedCount > 0)
            {
                status = "Fixed";
            }
            else
            {
                status = "Ok";
            }

            return new ValidationRow
            {
                levelId = entry.levelData.levelId ?? string.Empty,
                levelAssetPath = entry.levelAssetPath ?? string.Empty,
                sceneName = entry.levelData.sceneName ?? string.Empty,
                scenePath = scenePath ?? string.Empty,
                sceneExists = sceneExists,
                inBuildSettings = inBuildSettings,
                status = status,
                issues = issues,
                fixedCount = fixedCount,
                unresolved = unresolved,
                note = notes != null && notes.Count > 0 ? string.Join(" ", notes) : string.Empty
            };
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

        private static bool EnsureParentFolder(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return false;
            }

            string folderPath = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return false;
            }

            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return true;
            }

            string[] parts = folderPath.Split('/');
            if (parts.Length == 0 || !string.Equals(parts[0], "Assets", StringComparison.Ordinal))
            {
                return false;
            }

            string current = "Assets";
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }

            return AssetDatabase.IsValidFolder(folderPath);
        }

        private static bool IsSceneEnabledInBuildSettings(List<EditorBuildSettingsScene> scenes, string scenePath)
        {
            if (scenes == null || string.IsNullOrWhiteSpace(scenePath))
            {
                return false;
            }

            for (int i = 0; i < scenes.Count; i++)
            {
                EditorBuildSettingsScene scene = scenes[i];
                if (!string.Equals(scene.path, scenePath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return scene.enabled;
            }

            return false;
        }

        private static bool UpsertBuildSettingsScene(List<EditorBuildSettingsScene> scenes, string scenePath)
        {
            if (scenes == null || string.IsNullOrWhiteSpace(scenePath))
            {
                return false;
            }

            for (int i = 0; i < scenes.Count; i++)
            {
                EditorBuildSettingsScene scene = scenes[i];
                if (!string.Equals(scene.path, scenePath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (scene.enabled)
                {
                    return true;
                }

                scenes[i] = new EditorBuildSettingsScene(scene.path, true);
                return true;
            }

            if (!AssetExists(scenePath))
            {
                return false;
            }

            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            return true;
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

            return AssetDatabase.LoadMainAssetAtPath(assetPath) != null;
        }

        private static string WriteReport(List<ValidationRow> rows)
        {
            string fullPath = Path.GetFullPath(ReportCsvPath);
            string directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var csv = new StringBuilder();
            csv.AppendLine("level_id,level_asset,scene_name,scene_path,scene_exists,in_build_settings,status,issues,fixed,unresolved,note");

            if (rows != null)
            {
                for (int i = 0; i < rows.Count; i++)
                {
                    ValidationRow row = rows[i];
                    csv.Append(EscapeCsv(row.levelId)).Append(',')
                        .Append(EscapeCsv(row.levelAssetPath)).Append(',')
                        .Append(EscapeCsv(row.sceneName)).Append(',')
                        .Append(EscapeCsv(row.scenePath)).Append(',')
                        .Append(row.sceneExists ? "1" : "0").Append(',')
                        .Append(row.inBuildSettings ? "1" : "0").Append(',')
                        .Append(EscapeCsv(row.status)).Append(',')
                        .Append(row.issues).Append(',')
                        .Append(row.fixedCount).Append(',')
                        .Append(row.unresolved).Append(',')
                        .Append(EscapeCsv(row.note))
                        .AppendLine();
                }
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
            public string levelAssetPath;
            public int levelIndex;
        }

        private struct ValidationRow
        {
            public string levelId;
            public string levelAssetPath;
            public string sceneName;
            public string scenePath;
            public bool sceneExists;
            public bool inBuildSettings;
            public string status;
            public int issues;
            public int fixedCount;
            public int unresolved;
            public string note;
        }
    }
}
