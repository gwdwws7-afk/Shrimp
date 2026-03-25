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
    public static class BossPhaseAttackConsistencyValidator
    {
        private const string ValidateMenuPath = "Tools/Boss/P1/Validate Phase Attack Consistency (CSV)";
        private const string ValidateGateMenuPath = "Tools/Boss/P1/Validate Phase Attack Consistency (CI Gate)";
        private const string FixMenuPath = "Tools/Boss/P1/Fix Phase Attack Consistency";
        private const string PrefabFolder = "Assets/Prefabs/Bosses";
        private const string SceneFolder = "Assets/Scenes";
        private const string ReportCsvPath = "Assets/ThirdPersonController/Reports/boss_phase_attack_consistency_report.csv";
        private const string LogPrefix = "[BossPhaseAttackConsistency]";
        private const string DefaultEelBossPrefabPath = "Assets/Prefabs/Bosses/BOSS_Eel_Controller.prefab";
        private const string DefaultGuardianBossPrefabPath = "Assets/Prefabs/Bosses/BOSS_Guardian_Controller.prefab";

        private struct ValidationRow
        {
            public string layer;
            public string source;
            public string levelId;
            public string status;
            public int fixedCount;
            public int gapCount;
            public string note;
        }

        private sealed class LevelSceneEntry
        {
            public LevelData levelData;
            public string scenePath;
            public int levelIndex;
        }

        [MenuItem(ValidateMenuPath)]
        public static void Validate()
        {
            Run(applyFix: false, failOnBlocking: false, interactive: true);
        }

        [MenuItem(ValidateGateMenuPath)]
        public static void ValidateCiGate()
        {
            Run(applyFix: false, failOnBlocking: true, interactive: false);
        }

        [MenuItem(FixMenuPath)]
        public static void Fix()
        {
            Run(applyFix: true, failOnBlocking: false, interactive: true);
        }

        public static void ApplyForBatch()
        {
            Run(applyFix: true, failOnBlocking: true, interactive: false);
        }

        public static void ValidateForBatch()
        {
            Run(applyFix: false, failOnBlocking: true, interactive: false);
        }

        private static void Run(bool applyFix, bool failOnBlocking, bool interactive)
        {
            if (interactive && !Application.isBatchMode)
            {
                bool allow = EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                if (!allow)
                {
                    return;
                }
            }

            var rows = new List<ValidationRow>(64);
            int fixedTotal = 0;
            int gapTotal = 0;
            int errorTotal = 0;

            ProcessPrefabLayer(applyFix, rows, ref fixedTotal, ref gapTotal, ref errorTotal);
            ProcessSceneLayer(applyFix, rows, ref fixedTotal, ref gapTotal, ref errorTotal);

            if (applyFix)
            {
                AssetDatabase.SaveAssets();
            }

            string reportPath = WriteCsv(rows);
            AssetDatabase.Refresh();

            string summary =
                $"mode={(applyFix ? "fix" : "validate")} rows={rows.Count} fixed={fixedTotal} " +
                $"gap={gapTotal} error={errorTotal} report={reportPath}";
            Debug.Log($"{LogPrefix} complete | {summary}");

            if (interactive)
            {
                EditorUtility.DisplayDialog("Boss Phase Attack Consistency", summary, "OK");
            }

            if (failOnBlocking && (gapTotal > 0 || errorTotal > 0))
            {
                throw new InvalidOperationException($"{LogPrefix} gate failed. gap={gapTotal} error={errorTotal} report={reportPath}");
            }
        }

        private static void ProcessPrefabLayer(
            bool applyFix,
            List<ValidationRow> rows,
            ref int fixedTotal,
            ref int gapTotal,
            ref int errorTotal)
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder });
            Array.Sort(guids, StringComparer.Ordinal);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrWhiteSpace(path) || !path.EndsWith("_Controller.prefab", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                ValidationRow row = ValidateBossPrefab(path, applyFix);
                rows.Add(row);
                fixedTotal += Mathf.Max(0, row.fixedCount);
                gapTotal += Mathf.Max(0, row.gapCount);
                if (string.Equals(row.status, "Error", StringComparison.Ordinal))
                {
                    errorTotal++;
                }
            }
        }

        private static ValidationRow ValidateBossPrefab(string prefabPath, bool applyFix)
        {
            var row = new ValidationRow
            {
                layer = "Prefab",
                source = prefabPath ?? string.Empty,
                levelId = string.Empty,
                status = "Error",
                fixedCount = 0,
                gapCount = 0,
                note = string.Empty
            };

            if (string.IsNullOrWhiteSpace(prefabPath) || !File.Exists(Path.GetFullPath(prefabPath)))
            {
                row.gapCount = 1;
                row.note = "Prefab path missing.";
                row.status = "Error";
                return row;
            }

            GameObject root = null;
            try
            {
                root = PrefabUtility.LoadPrefabContents(prefabPath);
                if (root == null)
                {
                    row.gapCount = 1;
                    row.note = "LoadPrefabContents returned null.";
                    row.status = "Error";
                    return row;
                }

                BossController boss = root.GetComponent<BossController>();
                if (boss == null)
                {
                    row.gapCount = 1;
                    row.note = "Missing BossController on prefab root.";
                    row.status = "Gap";
                    return row;
                }

                int fixedCount;
                int gapCount;
                List<string> notes;
                bool changed;
                ValidateBossControllerData(boss, applyFix, out fixedCount, out gapCount, out notes, out changed);

                if (applyFix && changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                }

                row.fixedCount = fixedCount;
                row.gapCount = gapCount;
                row.note = notes.Count > 0 ? string.Join(";", notes) : string.Empty;
                row.status = gapCount > 0 ? (applyFix ? "Partial" : "Gap") : (fixedCount > 0 ? "Fixed" : "Ok");
            }
            catch (Exception ex)
            {
                row.gapCount = 1;
                row.status = "Error";
                row.note = $"Exception: {ex.Message}";
            }
            finally
            {
                if (root != null)
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            return row;
        }

        private static void ValidateBossControllerData(
            BossController boss,
            bool applyFix,
            out int fixedCount,
            out int gapCount,
            out List<string> notes,
            out bool changed)
        {
            fixedCount = 0;
            gapCount = 0;
            notes = new List<string>(16);
            changed = false;

            if (boss.phases == null || boss.phases.Count == 0)
            {
                gapCount++;
                notes.Add("phases-empty");
                return;
            }

            if (boss.attacks == null || boss.attacks.Count == 0)
            {
                gapCount++;
                notes.Add("attacks-empty");
                return;
            }

            var attackById = new Dictionary<string, BossAttack>(StringComparer.Ordinal);
            for (int i = 0; i < boss.attacks.Count; i++)
            {
                BossAttack attack = boss.attacks[i];
                if (attack == null)
                {
                    gapCount++;
                    notes.Add($"attack-null-{i}");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(attack.attackId))
                {
                    gapCount++;
                    notes.Add($"attack-id-empty-{i}");
                    continue;
                }

                if (attackById.ContainsKey(attack.attackId))
                {
                    gapCount++;
                    notes.Add($"attack-id-duplicate-{attack.attackId}");
                    continue;
                }

                attackById.Add(attack.attackId, attack);
            }

            float previousThreshold = 1.01f;
            for (int i = 0; i < boss.phases.Count; i++)
            {
                BossPhase phase = boss.phases[i];
                if (phase == null)
                {
                    gapCount++;
                    notes.Add($"phase-null-{i}");
                    continue;
                }

                float threshold = Mathf.Clamp01(phase.healthPercentThreshold);
                if (Mathf.Abs(threshold - phase.healthPercentThreshold) > 0.0001f)
                {
                    if (applyFix)
                    {
                        phase.healthPercentThreshold = threshold;
                        fixedCount++;
                        changed = true;
                    }
                    else
                    {
                        gapCount++;
                        notes.Add($"phase-threshold-out-of-range-{i}");
                    }
                }

                if (threshold > previousThreshold + 0.0001f)
                {
                    gapCount++;
                    notes.Add($"phase-threshold-order-{i}");
                }
                previousThreshold = threshold;

                if (phase.unlockedAttacks == null)
                {
                    if (applyFix)
                    {
                        phase.unlockedAttacks = new List<string>();
                        fixedCount++;
                        changed = true;
                    }
                    else
                    {
                        gapCount++;
                        notes.Add($"phase-unlocked-null-{i}");
                        continue;
                    }
                }

                bool lockByList = phase.unlockSpecialAttacks;
                if (!lockByList && phase.unlockedAttacks.Count > 0)
                {
                    if (applyFix)
                    {
                        phase.unlockedAttacks.Clear();
                        fixedCount++;
                        changed = true;
                    }
                    else
                    {
                        gapCount++;
                        notes.Add($"phase-unlocked-should-empty-{i}");
                    }
                }

                if (!lockByList)
                {
                    continue;
                }

                var eligibleSpecialIds = new List<string>(8);
                for (int attackIndex = 0; attackIndex < boss.attacks.Count; attackIndex++)
                {
                    BossAttack attack = boss.attacks[attackIndex];
                    if (attack == null || !attack.isSpecial || string.IsNullOrWhiteSpace(attack.attackId))
                    {
                        continue;
                    }

                    if (IsAttackAllowedByPhaseRequirement(attack, i))
                    {
                        eligibleSpecialIds.Add(attack.attackId);
                    }
                }

                var seenUnlocked = new HashSet<string>(StringComparer.Ordinal);
                for (int unlockIndex = phase.unlockedAttacks.Count - 1; unlockIndex >= 0; unlockIndex--)
                {
                    string attackId = phase.unlockedAttacks[unlockIndex];
                    if (string.IsNullOrWhiteSpace(attackId))
                    {
                        if (applyFix)
                        {
                            phase.unlockedAttacks.RemoveAt(unlockIndex);
                            fixedCount++;
                            changed = true;
                        }
                        else
                        {
                            gapCount++;
                            notes.Add($"phase-unlocked-empty-id-{i}");
                        }

                        continue;
                    }

                    if (!seenUnlocked.Add(attackId))
                    {
                        if (applyFix)
                        {
                            phase.unlockedAttacks.RemoveAt(unlockIndex);
                            fixedCount++;
                            changed = true;
                        }
                        else
                        {
                            gapCount++;
                            notes.Add($"phase-unlocked-duplicate-{i}-{attackId}");
                        }

                        continue;
                    }

                    if (!attackById.TryGetValue(attackId, out BossAttack linkedAttack))
                    {
                        if (applyFix)
                        {
                            phase.unlockedAttacks.RemoveAt(unlockIndex);
                            fixedCount++;
                            changed = true;
                        }
                        else
                        {
                            gapCount++;
                            notes.Add($"phase-unlocked-missing-attack-{i}-{attackId}");
                        }

                        continue;
                    }

                    if (linkedAttack == null || !linkedAttack.isSpecial || !IsAttackAllowedByPhaseRequirement(linkedAttack, i))
                    {
                        if (applyFix)
                        {
                            phase.unlockedAttacks.RemoveAt(unlockIndex);
                            fixedCount++;
                            changed = true;
                        }
                        else
                        {
                            gapCount++;
                            notes.Add($"phase-unlocked-ineligible-{i}-{attackId}");
                        }
                    }
                }

                if (eligibleSpecialIds.Count > 0)
                {
                    for (int e = 0; e < eligibleSpecialIds.Count; e++)
                    {
                        string requiredId = eligibleSpecialIds[e];
                        if (phase.unlockedAttacks.Contains(requiredId))
                        {
                            continue;
                        }

                        if (applyFix)
                        {
                            phase.unlockedAttacks.Add(requiredId);
                            fixedCount++;
                            changed = true;
                        }
                        else
                        {
                            gapCount++;
                            notes.Add($"phase-unlocked-missing-required-{i}-{requiredId}");
                        }
                    }
                }
            }
        }

        private static bool IsAttackAllowedByPhaseRequirement(BossAttack attack, int phaseIndex)
        {
            if (attack == null)
            {
                return false;
            }

            if (attack.requiresPhase3 && phaseIndex < 2)
            {
                return false;
            }

            if (attack.requiresPhase2 && phaseIndex < 1)
            {
                return false;
            }

            return true;
        }

        private static void ProcessSceneLayer(
            bool applyFix,
            List<ValidationRow> rows,
            ref int fixedTotal,
            ref int gapTotal,
            ref int errorTotal)
        {
            List<LevelSceneEntry> entries = CollectSceneEntries();
            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    ValidationRow row = ValidateSceneEntry(entries[i], applyFix);
                    rows.Add(row);
                    fixedTotal += Mathf.Max(0, row.fixedCount);
                    gapTotal += Mathf.Max(0, row.gapCount);
                    if (string.Equals(row.status, "Error", StringComparison.Ordinal))
                    {
                        errorTotal++;
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
        }

        private static ValidationRow ValidateSceneEntry(LevelSceneEntry entry, bool applyFix)
        {
            var row = new ValidationRow
            {
                layer = "Scene",
                source = entry != null ? entry.scenePath ?? string.Empty : string.Empty,
                levelId = entry != null && entry.levelData != null ? entry.levelData.levelId : string.Empty,
                status = "Error",
                fixedCount = 0,
                gapCount = 0,
                note = string.Empty
            };

            if (entry == null || entry.levelData == null)
            {
                row.gapCount = 1;
                row.note = "LevelData missing.";
                return row;
            }

            if (string.IsNullOrWhiteSpace(entry.scenePath) || !File.Exists(Path.GetFullPath(entry.scenePath)))
            {
                row.gapCount = 1;
                row.note = "Scene missing.";
                row.status = "Error";
                return row;
            }

            var notes = new List<string>(8);
            int fixedCount = 0;
            int gapCount = 0;
            bool sceneDirty = false;

            try
            {
                Scene scene = EditorSceneManager.OpenScene(entry.scenePath, OpenSceneMode.Single);
                List<BossSpawnPoint> spawnPoints = FindComponentsInScene<BossSpawnPoint>(scene);
                if (spawnPoints.Count == 0)
                {
                    row.gapCount = 1;
                    row.status = "Gap";
                    row.note = "Missing BossSpawnPoint.";
                    return row;
                }

                if (spawnPoints.Count > 1)
                {
                    gapCount++;
                    notes.Add("boss-spawnpoint-multiple");
                }

                BossSpawnPoint spawnPoint = spawnPoints[0];
                string expectedPrefabPath = ResolveExpectedPrefabPath(entry.levelData.bossPrototype);
                GameObject expectedPrefab = LoadPrefab(expectedPrefabPath);

                if (spawnPoint.bossPrefab == null)
                {
                    if (applyFix && expectedPrefab != null)
                    {
                        spawnPoint.bossPrefab = expectedPrefab;
                        fixedCount++;
                        sceneDirty = true;
                    }
                    else
                    {
                        gapCount++;
                        notes.Add("boss-prefab-null");
                    }
                }
                else if (expectedPrefab != null && spawnPoint.bossPrefab != expectedPrefab)
                {
                    if (applyFix)
                    {
                        spawnPoint.bossPrefab = expectedPrefab;
                        fixedCount++;
                        sceneDirty = true;
                    }
                    else
                    {
                        gapCount++;
                        notes.Add("boss-prefab-mismatch-prototype");
                    }
                }

                GameObject bossPrefab = spawnPoint.bossPrefab != null ? spawnPoint.bossPrefab : expectedPrefab;
                if (bossPrefab == null)
                {
                    gapCount++;
                    notes.Add("boss-prefab-unresolved");
                }
                else
                {
                    BossController controller = bossPrefab.GetComponent<BossController>();
                    if (controller == null)
                    {
                        gapCount++;
                        notes.Add("boss-prefab-missing-controller");
                    }
                }

                if (applyFix && sceneDirty)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene))
                    {
                        gapCount++;
                        notes.Add("scene-save-failed");
                    }
                }
            }
            catch (Exception ex)
            {
                row.status = "Error";
                row.gapCount = 1;
                row.note = $"Exception: {ex.Message}";
                return row;
            }

            row.fixedCount = fixedCount;
            row.gapCount = gapCount;
            row.note = notes.Count > 0 ? string.Join(";", notes) : string.Empty;
            row.status = gapCount > 0 ? (applyFix ? "Partial" : "Gap") : (fixedCount > 0 ? "Fixed" : "Ok");
            return row;
        }

        private static string ResolveExpectedPrefabPath(BossPrototypeType prototype)
        {
            return prototype == BossPrototypeType.Guardian
                ? DefaultGuardianBossPrefabPath
                : DefaultEelBossPrefabPath;
        }

        private static GameObject LoadPrefab(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private static List<LevelSceneEntry> CollectSceneEntries()
        {
            var result = new List<LevelSceneEntry>();
            string[] guids = AssetDatabase.FindAssets("t:LevelData");
            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrWhiteSpace(assetPath))
                {
                    continue;
                }

                LevelData levelData = AssetDatabase.LoadAssetAtPath<LevelData>(assetPath);
                if (levelData == null || !levelData.overrideBossSettings)
                {
                    continue;
                }

                int levelIndex = ParseLevelIndex(levelData.levelId);
                if (levelIndex <= 0 || string.IsNullOrWhiteSpace(levelData.sceneName))
                {
                    continue;
                }

                string scenePath = $"{SceneFolder}/{levelData.sceneName}.unity";
                result.Add(new LevelSceneEntry
                {
                    levelData = levelData,
                    scenePath = scenePath,
                    levelIndex = levelIndex
                });
            }

            result.Sort((a, b) => a.levelIndex.CompareTo(b.levelIndex));
            return result;
        }

        private static int ParseLevelIndex(string levelId)
        {
            if (string.IsNullOrWhiteSpace(levelId) || !levelId.StartsWith("LEVEL_", StringComparison.OrdinalIgnoreCase))
            {
                return -1;
            }

            string raw = levelId.Substring("LEVEL_".Length);
            if (int.TryParse(raw, out int level))
            {
                return level;
            }

            return -1;
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

        private static string WriteCsv(List<ValidationRow> rows)
        {
            string absolutePath = Path.GetFullPath(ReportCsvPath);
            string directory = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var builder = new StringBuilder();
            builder.AppendLine("layer,source,level_id,status,fixed_count,gap_count,note");
            for (int i = 0; i < rows.Count; i++)
            {
                ValidationRow row = rows[i];
                builder.Append(EscapeCsv(row.layer)).Append(',')
                    .Append(EscapeCsv(row.source)).Append(',')
                    .Append(EscapeCsv(row.levelId)).Append(',')
                    .Append(EscapeCsv(row.status)).Append(',')
                    .Append(row.fixedCount).Append(',')
                    .Append(row.gapCount).Append(',')
                    .Append(EscapeCsv(row.note)).AppendLine();
            }

            File.WriteAllText(absolutePath, builder.ToString(), Encoding.UTF8);
            return ReportCsvPath;
        }

        private static string EscapeCsv(string value)
        {
            string text = value ?? string.Empty;
            if (text.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0)
            {
                return $"\"{text.Replace("\"", "\"\"")}\"";
            }

            return text;
        }
    }
}
