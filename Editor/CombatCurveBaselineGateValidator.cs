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
    public static class CombatCurveBaselineGateValidator
    {
        private const string ValidateMenuPath = "Tools/Combat/P1/Validate Combat Curve Baseline (CSV)";
        private const string ValidateGateMenuPath = "Tools/Combat/P1/Validate Combat Curve Baseline (CI Gate)";
        private const string SceneFolderPath = "Assets/Scenes";
        private const string ReportCsvPath = "Assets/ThirdPersonController/Reports/combat_curve_baseline_gate_report.csv";
        private const string SummaryMdPath = "Assets/ThirdPersonController/Reports/combat_curve_baseline_gate_summary.md";
        private const string LogPrefix = "[CombatCurveBaselineGate]";
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
                    EditorUtility.DisplayDialog("Combat Curve Baseline Gate", noneMessage, "OK");
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
                EditorUtility.DisplayDialog("Combat Curve Baseline Gate", summary, "OK");
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
                status = "Error"
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

            List<PlayerCombat> combats = FindComponentsInScene<PlayerCombat>(scene);
            List<StaminaSystem> staminaSystems = FindComponentsInScene<StaminaSystem>(scene);
            List<SkillManager> skillManagers = FindComponentsInScene<SkillManager>(scene);

            row.playerCombatCount = combats.Count;
            row.staminaSystemCount = staminaSystems.Count;
            row.skillManagerCount = skillManagers.Count;

            if (combats.Count == 0)
            {
                blockingNotes.Add("No PlayerCombat found in scene.");
            }

            if (staminaSystems.Count == 0)
            {
                blockingNotes.Add("No StaminaSystem found in scene.");
            }

            if (blockingNotes.Count > 0)
            {
                return BuildRow(row, blockingNotes, warningNotes);
            }

            PlayerCombat combat = combats[0];
            StaminaSystem stamina = staminaSystems[0];

            row.attackDamage = combat.attackDamage;
            row.tier1Multiplier = combat.tier1DamageMultiplier;
            row.tier2Multiplier = combat.tier2DamageMultiplier;
            row.tier3Multiplier = combat.tier3DamageMultiplier;
            row.berserkMultiplier = combat.berserkDamageMultiplier;
            row.comboWindow = combat.comboWindowTime;
            row.comboReset = combat.comboResetTime;
            row.berserkThreshold = combat.berserkThreshold;

            row.maxStamina = stamina.maxStamina;
            row.heavyAttackCost = stamina.heavyAttackCost;
            row.dodgeCost = stamina.dodgeCost;
            row.recoveryRate = stamina.recoveryRate;
            row.exhaustionDuration = stamina.exhaustionDuration;

            if (combats.Count > 1)
            {
                warningNotes.Add($"Found {combats.Count} PlayerCombat components; using first one as baseline.");
            }

            if (staminaSystems.Count > 1)
            {
                warningNotes.Add($"Found {staminaSystems.Count} StaminaSystem components; using first one as baseline.");
            }

            if (skillManagers.Count > 1)
            {
                warningNotes.Add($"Found {skillManagers.Count} SkillManager components; aggregating all skill slots.");
            }

            EvaluateComboCurve(row, warningNotes);
            EvaluateStaminaPressure(ref row, warningNotes);
            CollectSkillMetrics(skillManagers, ref row, warningNotes);

            return BuildRow(row, blockingNotes, warningNotes);
        }

        private static void EvaluateComboCurve(ValidationRow row, List<string> warningNotes)
        {
            if (row.attackDamage <= 0)
            {
                warningNotes.Add("attackDamage <= 0.");
            }

            if (row.tier1Multiplier < 1f)
            {
                warningNotes.Add($"tier1DamageMultiplier={row.tier1Multiplier:0.###} < 1.");
            }

            if (row.tier2Multiplier <= row.tier1Multiplier)
            {
                warningNotes.Add(
                    $"tier2DamageMultiplier={row.tier2Multiplier:0.###} should be greater than tier1={row.tier1Multiplier:0.###}.");
            }

            if (row.tier3Multiplier <= row.tier2Multiplier)
            {
                warningNotes.Add(
                    $"tier3DamageMultiplier={row.tier3Multiplier:0.###} should be greater than tier2={row.tier2Multiplier:0.###}.");
            }

            if (row.berserkMultiplier < row.tier3Multiplier)
            {
                warningNotes.Add(
                    $"berserkDamageMultiplier={row.berserkMultiplier:0.###} should not be lower than tier3={row.tier3Multiplier:0.###}.");
            }

            if (row.comboWindow <= 0f)
            {
                warningNotes.Add("comboWindowTime <= 0.");
            }
            else if (row.comboWindow > row.comboReset)
            {
                warningNotes.Add(
                    $"comboWindowTime={row.comboWindow:0.###} exceeds comboResetTime={row.comboReset:0.###}.");
            }

            if (row.comboReset < 0.6f || row.comboReset > 2.2f)
            {
                warningNotes.Add($"comboResetTime={row.comboReset:0.###} out of baseline range [0.6,2.2].");
            }

            if (row.berserkThreshold < 20)
            {
                warningNotes.Add($"berserkThreshold={row.berserkThreshold} is too low.");
            }
        }

        private static void EvaluateStaminaPressure(ref ValidationRow row, List<string> warningNotes)
        {
            if (row.maxStamina <= 0f)
            {
                warningNotes.Add("maxStamina <= 0.");
                return;
            }

            if (row.heavyAttackCost <= 0f)
            {
                warningNotes.Add("heavyAttackCost <= 0.");
            }

            if (row.dodgeCost <= 0f)
            {
                warningNotes.Add("dodgeCost <= 0.");
            }

            if (row.heavyAttackCost <= row.dodgeCost)
            {
                warningNotes.Add(
                    $"heavyAttackCost={row.heavyAttackCost:0.###} should generally exceed dodgeCost={row.dodgeCost:0.###}.");
            }

            float heavyPressure = row.heavyAttackCost / row.maxStamina;
            row.heavyCostPercent = heavyPressure;
            if (heavyPressure < 0.08f || heavyPressure > 0.45f)
            {
                warningNotes.Add($"heavyAttackCost pressure={heavyPressure * 100f:0.0}% out of baseline range [8%,45%].");
            }

            if (row.recoveryRate < 5f || row.recoveryRate > 45f)
            {
                warningNotes.Add($"recoveryRate={row.recoveryRate:0.###} out of baseline range [5,45].");
            }

            if (row.exhaustionDuration < 0.5f || row.exhaustionDuration > 6f)
            {
                warningNotes.Add($"exhaustionDuration={row.exhaustionDuration:0.###} out of baseline range [0.5,6].");
            }
        }

        private static void CollectSkillMetrics(List<SkillManager> skillManagers, ref ValidationRow row, List<string> warningNotes)
        {
            var uniqueSkills = new List<SkillBase>();
            var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < skillManagers.Count; i++)
            {
                SkillManager manager = skillManagers[i];
                if (manager == null || manager.skills == null || manager.skills.Length == 0)
                {
                    continue;
                }

                for (int s = 0; s < manager.skills.Length; s++)
                {
                    SkillBase skill = manager.skills[s];
                    if (skill == null)
                    {
                        continue;
                    }

                    string path = AssetDatabase.GetAssetPath(skill);
                    string key = string.IsNullOrWhiteSpace(path)
                        ? $"{skill.name}#{skill.GetInstanceID()}"
                        : path;
                    if (!seenKeys.Add(key))
                    {
                        continue;
                    }

                    uniqueSkills.Add(skill);
                }
            }

            if (uniqueSkills.Count == 0)
            {
                List<SkillBase> globalSkills = CollectGlobalSkillAssets();
                for (int i = 0; i < globalSkills.Count; i++)
                {
                    SkillBase skill = globalSkills[i];
                    if (skill == null)
                    {
                        continue;
                    }

                    string path = AssetDatabase.GetAssetPath(skill);
                    string key = string.IsNullOrWhiteSpace(path)
                        ? $"{skill.name}#{skill.GetInstanceID()}"
                        : path;
                    if (seenKeys.Add(key))
                    {
                        uniqueSkills.Add(skill);
                    }
                }
            }

            row.skillCount = uniqueSkills.Count;
            if (row.skillCount <= 0)
            {
                warningNotes.Add("No skill assets found for curve sampling.");
                return;
            }

            float damageTotal = 0f;
            float minCooldown = float.MaxValue;
            float maxCooldown = float.MinValue;
            int maxDamage = 0;
            float maxSkillCost = 0f;

            for (int i = 0; i < uniqueSkills.Count; i++)
            {
                SkillBase skill = uniqueSkills[i];
                if (skill == null)
                {
                    continue;
                }

                int damage = Mathf.Max(0, skill.damage);
                float cooldown = Mathf.Max(0f, skill.cooldown);
                float cost = Mathf.Max(0f, skill.staminaCost);

                damageTotal += damage;
                maxDamage = Math.Max(maxDamage, damage);
                maxSkillCost = Mathf.Max(maxSkillCost, cost);

                if (cooldown < minCooldown)
                {
                    minCooldown = cooldown;
                }

                if (cooldown > maxCooldown)
                {
                    maxCooldown = cooldown;
                }

                if (cooldown <= 0f)
                {
                    warningNotes.Add($"Skill '{skill.skillName}' cooldown <= 0.");
                }
            }

            row.averageSkillDamage = row.skillCount > 0 ? damageTotal / row.skillCount : 0f;
            row.maxSkillDamage = maxDamage;
            row.minSkillCooldown = minCooldown == float.MaxValue ? 0f : minCooldown;
            row.maxSkillCooldown = maxCooldown == float.MinValue ? 0f : maxCooldown;
            row.maxSkillStaminaCost = maxSkillCost;

            if (row.attackDamage > 0)
            {
                if (row.maxSkillDamage < row.attackDamage * 1.35f)
                {
                    warningNotes.Add(
                        $"maxSkillDamage={row.maxSkillDamage} is low vs attackDamage={row.attackDamage} (expected >= 1.35x).");
                }

                if (row.averageSkillDamage < row.attackDamage)
                {
                    warningNotes.Add(
                        $"averageSkillDamage={row.averageSkillDamage:0.##} is below base attackDamage={row.attackDamage}.");
                }
            }

            if (row.maxStamina > 0f && row.maxSkillStaminaCost > row.maxStamina * 0.75f)
            {
                warningNotes.Add(
                    $"max skill staminaCost={row.maxSkillStaminaCost:0.##} exceeds 75% of maxStamina={row.maxStamina:0.##}.");
            }
        }

        private static List<SkillBase> CollectGlobalSkillAssets()
        {
            var result = new List<SkillBase>();
            string[] guids = AssetDatabase.FindAssets("t:SkillBase");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                SkillBase skill = AssetDatabase.LoadAssetAtPath<SkillBase>(path);
                if (skill != null)
                {
                    result.Add(skill);
                }
            }

            result.Sort((a, b) =>
            {
                string pa = a != null ? AssetDatabase.GetAssetPath(a) : string.Empty;
                string pb = b != null ? AssetDatabase.GetAssetPath(b) : string.Empty;
                return string.Compare(pa, pb, StringComparison.OrdinalIgnoreCase);
            });
            return result;
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
                "level_id,level_asset,scene_name,scene_path,status,blocking_errors,warnings,player_combat_count,stamina_system_count,skill_manager_count,attack_damage,tier1_multiplier,tier2_multiplier,tier3_multiplier,berserk_multiplier,combo_window,combo_reset,berserk_threshold,max_stamina,heavy_attack_cost,dodge_cost,heavy_cost_percent,recovery_rate,exhaustion_duration,skill_count,average_skill_damage,max_skill_damage,min_skill_cooldown,max_skill_cooldown,max_skill_stamina_cost,note");

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
                    .Append(row.playerCombatCount).Append(',')
                    .Append(row.staminaSystemCount).Append(',')
                    .Append(row.skillManagerCount).Append(',')
                    .Append(row.attackDamage).Append(',')
                    .Append(row.tier1Multiplier.ToString("0.###")).Append(',')
                    .Append(row.tier2Multiplier.ToString("0.###")).Append(',')
                    .Append(row.tier3Multiplier.ToString("0.###")).Append(',')
                    .Append(row.berserkMultiplier.ToString("0.###")).Append(',')
                    .Append(row.comboWindow.ToString("0.###")).Append(',')
                    .Append(row.comboReset.ToString("0.###")).Append(',')
                    .Append(row.berserkThreshold).Append(',')
                    .Append(row.maxStamina.ToString("0.###")).Append(',')
                    .Append(row.heavyAttackCost.ToString("0.###")).Append(',')
                    .Append(row.dodgeCost.ToString("0.###")).Append(',')
                    .Append(row.heavyCostPercent.ToString("0.###")).Append(',')
                    .Append(row.recoveryRate.ToString("0.###")).Append(',')
                    .Append(row.exhaustionDuration.ToString("0.###")).Append(',')
                    .Append(row.skillCount).Append(',')
                    .Append(row.averageSkillDamage.ToString("0.###")).Append(',')
                    .Append(row.maxSkillDamage).Append(',')
                    .Append(row.minSkillCooldown.ToString("0.###")).Append(',')
                    .Append(row.maxSkillCooldown.ToString("0.###")).Append(',')
                    .Append(row.maxSkillStaminaCost.ToString("0.###")).Append(',')
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
            md.AppendLine("# Combat Curve Baseline Gate Summary");
            md.AppendLine();
            md.AppendLine($"- Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss zzz}");
            md.AppendLine($"- Targets: {rows.Count}");
            md.AppendLine($"- Error Rows: {errorRows}");
            md.AppendLine($"- Warning Count: {warningTotal}");
            md.AppendLine($"- CSV: {ReportCsvPath}");
            md.AppendLine();
            md.AppendLine("| Level | Scene | Status | Warnings | Attack | T1/T2/T3/Berserk | Stamina(H/D/Max) | Skills | Note |");
            md.AppendLine("|---|---|---|---:|---:|---|---|---:|---|");

            for (int i = 0; i < rows.Count; i++)
            {
                ValidationRow row = rows[i];
                md.Append("| ")
                    .Append(SafeMarkdownCell(row.levelId)).Append(" | ")
                    .Append(SafeMarkdownCell(row.sceneName)).Append(" | ")
                    .Append(SafeMarkdownCell(row.status)).Append(" | ")
                    .Append(row.warnings).Append(" | ")
                    .Append(row.attackDamage).Append(" | ")
                    .Append(row.tier1Multiplier.ToString("0.##")).Append(" / ")
                    .Append(row.tier2Multiplier.ToString("0.##")).Append(" / ")
                    .Append(row.tier3Multiplier.ToString("0.##")).Append(" / ")
                    .Append(row.berserkMultiplier.ToString("0.##")).Append(" | ")
                    .Append(row.heavyAttackCost.ToString("0.#")).Append(" / ")
                    .Append(row.dodgeCost.ToString("0.#")).Append(" / ")
                    .Append(row.maxStamina.ToString("0.#")).Append(" | ")
                    .Append(row.skillCount).Append(" | ")
                    .Append(SafeMarkdownCell(TrimForMarkdownTable(row.note, 150))).Append(" |")
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
            public int playerCombatCount;
            public int staminaSystemCount;
            public int skillManagerCount;
            public int attackDamage;
            public float tier1Multiplier;
            public float tier2Multiplier;
            public float tier3Multiplier;
            public float berserkMultiplier;
            public float comboWindow;
            public float comboReset;
            public int berserkThreshold;
            public float maxStamina;
            public float heavyAttackCost;
            public float dodgeCost;
            public float heavyCostPercent;
            public float recoveryRate;
            public float exhaustionDuration;
            public int skillCount;
            public float averageSkillDamage;
            public int maxSkillDamage;
            public float minSkillCooldown;
            public float maxSkillCooldown;
            public float maxSkillStaminaCost;
            public string note;
        }
    }
}
