using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ThirdPersonController;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ThirdPersonController.Editor
{
    public static class UICrossDeviceReadabilityValidator
    {
        private const string ValidateMenuPath = "Tools/UI/P0/Validate Cross-Device Readability (CSV)";
        private const string ValidateGateMenuPath = "Tools/UI/P0/Validate Cross-Device Readability (CI Gate)";
        private const string SceneFolderPath = "Assets/Scenes";
        private const string ReportCsvPath = "Assets/ThirdPersonController/Reports/ui_cross_device_readability_report.csv";
        private const string SummaryMdPath = "Assets/ThirdPersonController/Reports/ui_cross_device_readability_summary.md";
        private const string LogPrefix = "[UICrossDeviceReadability]";
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

        public static void FixForBatch()
        {
            // Current UI readability gate is report-only, fix path intentionally aliases validate.
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
                    EditorUtility.DisplayDialog("UI Cross-Device Readability", noneMessage, "OK");
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
                ValidationRow row = rows[i];
                if (string.Equals(row.status, "Error", StringComparison.Ordinal))
                {
                    errorRows++;
                }

                warningTotal += row.warnings;
            }

            string csvPath = WriteCsv(rows);
            string summaryPath = WriteSummary(rows, errorRows, warningTotal);
            AssetDatabase.Refresh();

            string summary =
                $"targets={rows.Count} errors={errorRows} warnings={warningTotal} csv={csvPath} summary={summaryPath}";
            Debug.Log($"{LogPrefix} complete | {summary}");

            if (interactive)
            {
                EditorUtility.DisplayDialog("UI Cross-Device Readability", summary, "OK");
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

            List<Canvas> canvases = FindComponentsInScene<Canvas>(scene);
            row.canvasCount = canvases.Count;

            if (canvases.Count == 0)
            {
                blockingNotes.Add("No Canvas found in scene.");
                return BuildRow(row, blockingNotes, warningNotes);
            }

            int scalerCount = 0;
            int scalerScaleWithScreenSizeCount = 0;
            int scalerReadableRefCount = 0;

            for (int i = 0; i < canvases.Count; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas == null)
                {
                    continue;
                }

                CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler == null)
                {
                    continue;
                }

                scalerCount++;

                if (scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
                {
                    scalerScaleWithScreenSizeCount++;
                    if (scaler.referenceResolution.x >= 1280f && scaler.referenceResolution.y >= 720f)
                    {
                        scalerReadableRefCount++;
                    }
                    else
                    {
                        warningNotes.Add(
                            $"Canvas '{canvas.name}' referenceResolution is low ({scaler.referenceResolution.x:0}x{scaler.referenceResolution.y:0}).");
                    }

                    if (scaler.matchWidthOrHeight < 0.15f || scaler.matchWidthOrHeight > 0.85f)
                    {
                        warningNotes.Add(
                            $"Canvas '{canvas.name}' matchWidthOrHeight={scaler.matchWidthOrHeight:0.00} is near edge, cross-device balance may drift.");
                    }
                }
                else
                {
                    warningNotes.Add($"Canvas '{canvas.name}' is not using ScaleWithScreenSize.");
                }
            }

            row.canvasScalerCount = scalerCount;
            row.scaleWithScreenSizeCount = scalerScaleWithScreenSizeCount;
            row.readableReferenceScalerCount = scalerReadableRefCount;

            if (scalerScaleWithScreenSizeCount == 0)
            {
                blockingNotes.Add("No CanvasScaler configured with ScaleWithScreenSize.");
            }

            List<UI_HudHints> hudHints = FindComponentsInScene<UI_HudHints>(scene);
            row.hudHintsCount = hudHints.Count;
            for (int i = 0; i < hudHints.Count; i++)
            {
                UI_HudHints hint = hudHints[i];
                if (hint == null)
                {
                    continue;
                }

                if (hint.width < 200f || hint.width > 420f)
                {
                    warningNotes.Add($"UI_HudHints '{hint.name}' width={hint.width:0.##} out of readability baseline [200,420].");
                }

                if (hint.lineHeight < 16f)
                {
                    warningNotes.Add($"UI_HudHints '{hint.name}' lineHeight={hint.lineHeight:0.##} is too small.");
                }

                if (hint.padding < 8f)
                {
                    warningNotes.Add($"UI_HudHints '{hint.name}' padding={hint.padding:0.##} is too tight.");
                }

                if (hint.hintRefreshInterval > 0.5f)
                {
                    warningNotes.Add($"UI_HudHints '{hint.name}' hintRefreshInterval={hint.hintRefreshInterval:0.##}s is too slow.");
                }
            }

            List<UI_SkillBar> skillBars = FindComponentsInScene<UI_SkillBar>(scene);
            row.skillBarCount = skillBars.Count;
            for (int i = 0; i < skillBars.Count; i++)
            {
                UI_SkillBar skillBar = skillBars[i];
                if (skillBar == null)
                {
                    continue;
                }

                if (skillBar.inputHintRefreshInterval > 0.5f)
                {
                    warningNotes.Add(
                        $"UI_SkillBar '{skillBar.name}' inputHintRefreshInterval={skillBar.inputHintRefreshInterval:0.##}s is too slow.");
                }

                if (skillBar.keyBindings == null || skillBar.keyBindings.Length < 6)
                {
                    warningNotes.Add($"UI_SkillBar '{skillBar.name}' keyBindings length is below 6.");
                }

                if (skillBar.attackInputHintText != null && skillBar.attackInputHintText.fontSize < 13)
                {
                    warningNotes.Add(
                        $"UI_SkillBar '{skillBar.name}' attackInputHintText fontSize={skillBar.attackInputHintText.fontSize} is too small.");
                }
            }

            List<Text> keyTexts = new List<Text>();
            for (int i = 0; i < skillBars.Count; i++)
            {
                UI_SkillBar bar = skillBars[i];
                if (bar == null || bar.skillSlots == null)
                {
                    continue;
                }

                for (int s = 0; s < bar.skillSlots.Length; s++)
                {
                    UI_SkillBar.SkillSlot slot = bar.skillSlots[s];
                    if (slot != null && slot.keyText != null)
                    {
                        keyTexts.Add(slot.keyText);
                    }
                }
            }

            row.keyLabelCount = keyTexts.Count;
            for (int i = 0; i < keyTexts.Count; i++)
            {
                Text keyText = keyTexts[i];
                if (keyText != null && keyText.fontSize < 12)
                {
                    warningNotes.Add($"Skill key label '{keyText.name}' fontSize={keyText.fontSize} is too small.");
                }
            }

            return BuildRow(row, blockingNotes, warningNotes);
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
                "level_id,level_asset,scene_name,scene_path,status,blocking_errors,warnings,canvas_count,canvas_scaler_count,scale_with_screen_size_count,readable_reference_scaler_count,hud_hints_count,skill_bar_count,key_label_count,note");

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
                    .Append(row.canvasCount).Append(',')
                    .Append(row.canvasScalerCount).Append(',')
                    .Append(row.scaleWithScreenSizeCount).Append(',')
                    .Append(row.readableReferenceScalerCount).Append(',')
                    .Append(row.hudHintsCount).Append(',')
                    .Append(row.skillBarCount).Append(',')
                    .Append(row.keyLabelCount).Append(',')
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
            md.AppendLine("# UI Cross-Device Readability Summary");
            md.AppendLine();
            md.AppendLine($"- Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss zzz}");
            md.AppendLine($"- Targets: {rows.Count}");
            md.AppendLine($"- Error Rows: {errorRows}");
            md.AppendLine($"- Warning Count: {warningTotal}");
            md.AppendLine($"- CSV: {ReportCsvPath}");
            md.AppendLine();
            md.AppendLine("| Level | Scene | Status | Warnings | Canvas | Scalers(ScaleWithScreenSize) | HudHints | SkillBars | Note |");
            md.AppendLine("|---|---|---|---:|---:|---:|---:|---:|---|");

            for (int i = 0; i < rows.Count; i++)
            {
                ValidationRow row = rows[i];
                md.Append("| ")
                    .Append(SafeMarkdownCell(row.levelId)).Append(" | ")
                    .Append(SafeMarkdownCell(row.sceneName)).Append(" | ")
                    .Append(SafeMarkdownCell(row.status)).Append(" | ")
                    .Append(row.warnings).Append(" | ")
                    .Append(row.canvasCount).Append(" | ")
                    .Append(row.scaleWithScreenSizeCount).Append(" | ")
                    .Append(row.hudHintsCount).Append(" | ")
                    .Append(row.skillBarCount).Append(" | ")
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
            public string levelId;
            public string levelAssetPath;
            public string sceneName;
            public string scenePath;
            public string status;
            public int blockingErrors;
            public int warnings;
            public int canvasCount;
            public int canvasScalerCount;
            public int scaleWithScreenSizeCount;
            public int readableReferenceScalerCount;
            public int hudHintsCount;
            public int skillBarCount;
            public int keyLabelCount;
            public string note;
        }
    }
}
