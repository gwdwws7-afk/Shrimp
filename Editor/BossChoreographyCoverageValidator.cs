using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using ThirdPersonController;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThirdPersonController.Editor
{
    public static class BossChoreographyCoverageValidator
    {
        private const string ValidateMenuPath = "Tools/Boss/P3/Validate Round5 Choreography Coverage (CSV)";
        private const string ValidateGateMenuPath = "Tools/Boss/P3/Validate Round5 Choreography Coverage (CI Gate)";
        private const string SceneFolderPath = "Assets/Scenes";
        private const string ReportCsvPath = "Assets/ThirdPersonController/Reports/boss_choreography_coverage_report.csv";
        private const string SummaryMdPath = "Assets/ThirdPersonController/Reports/boss_choreography_coverage_summary.md";
        private const string StrictWarningWhitelistCsvPath = "Assets/ThirdPersonController/Reports/boss_choreography_strict_warning_whitelist.csv";
        private const string LogPrefix = "[BossChoreographyCoverage]";
        private const float FloatEpsilon = 0.0001f;
        private const int MinLevelIndex = 8;
        private const int MaxLevelIndex = 10;
        private const string DefaultEelBossPrefabPath = "Assets/Prefabs/Bosses/BOSS_Eel_Controller.prefab";
        private const string DefaultGuardianBossPrefabPath = "Assets/Prefabs/Bosses/BOSS_Guardian_Controller.prefab";

        private static readonly MethodInfo ConfigureBossMethod =
            typeof(LevelRuntimeConfigurator).GetMethod("ConfigureBoss", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo ApplyEncounterTuningMethod =
            typeof(BossSpawnPoint).GetMethod(
                "ApplyEncounterTuning",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(BossController) },
                null);

        private sealed class LevelEntry
        {
            public LevelData levelData;
            public string levelAssetPath;
            public int levelIndex;
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

        private struct ValidationRow
        {
            public string levelId;
            public string levelAssetPath;
            public string sceneName;
            public string scenePath;
            public string status;
            public int blockingErrors;
            public int warnings;
            public bool strictWarningWhitelisted;

            public string runtimeConfiguratorName;
            public string bossSpawnPointName;
            public int bossSpawnPointCount;
            public string bossPrefabPath;

            public bool levelOverrideBossSettings;
            public bool levelOverrideEncounterTuning;
            public bool levelEnablePhaseOpeners;
            public string levelPhase2OpenerId;
            public string levelPhase3OpenerId;
            public bool levelEnablePhaseOpenerRetry;
            public float levelPhaseOpenerRetryDelay;
            public int levelPhaseOpenerMaxRetries;
            public bool levelEnablePhaseFollowupChain;
            public string levelPhase2FollowupId;
            public string levelPhase3FollowupId;
            public bool levelEnablePhaseFollowupRetry;
            public float levelPhaseFollowupRetryDelay;
            public int levelPhaseFollowupMaxRetries;
            public bool levelEnablePhase3PriorityWindow;
            public float levelPhase3PriorityDuration;
            public float levelPhase3PriorityWeight;
            public bool levelForceSpecialQueueInPhase3Priority;

            public bool spawnOverrideEncounterTuning;
            public bool spawnEnablePhaseOpeners;
            public string spawnPhase2OpenerId;
            public string spawnPhase3OpenerId;
            public bool spawnEnablePhaseOpenerRetry;
            public float spawnPhaseOpenerRetryDelay;
            public int spawnPhaseOpenerMaxRetries;
            public bool spawnEnablePhaseFollowupChain;
            public string spawnPhase2FollowupId;
            public string spawnPhase3FollowupId;
            public bool spawnEnablePhaseFollowupRetry;
            public float spawnPhaseFollowupRetryDelay;
            public int spawnPhaseFollowupMaxRetries;
            public bool spawnEnablePhase3PriorityWindow;
            public float spawnPhase3PriorityDuration;
            public float spawnPhase3PriorityWeight;
            public bool spawnForceSpecialQueueInPhase3Priority;

            public bool controllerEnablePhaseOpeners;
            public string controllerPhase2OpenerId;
            public string controllerPhase3OpenerId;
            public bool controllerEnablePhaseOpenerRetry;
            public float controllerPhaseOpenerRetryDelay;
            public int controllerPhaseOpenerMaxRetries;
            public bool controllerEnablePhaseFollowupChain;
            public string controllerPhase2FollowupId;
            public string controllerPhase3FollowupId;
            public bool controllerEnablePhaseFollowupRetry;
            public float controllerPhaseFollowupRetryDelay;
            public int controllerPhaseFollowupMaxRetries;
            public bool controllerEnablePhase3PriorityWindow;
            public float controllerPhase3PriorityDuration;
            public float controllerPhase3PriorityWeight;
            public bool controllerForceSpecialQueueInPhase3Priority;

            public int prefabAttackCount;
            public int prefabSpecialAttackCount;
            public string note;
        }

        [MenuItem(ValidateMenuPath)]
        public static void Validate()
        {
            Run(interactive: true, failOnBlocking: false, strictWarningGate: false);
        }

        [MenuItem(ValidateGateMenuPath)]
        public static void ValidateCiGate()
        {
            Run(interactive: false, failOnBlocking: true, strictWarningGate: true);
        }

        public static void ValidateForBatch()
        {
            Run(interactive: false, failOnBlocking: true, strictWarningGate: true);
        }

        private static void Run(bool interactive, bool failOnBlocking, bool strictWarningGate)
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
                    EditorUtility.DisplayDialog("Boss Choreography Coverage", noneMessage, "OK");
                }

                return;
            }

            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
            var rows = new List<ValidationRow>(entries.Count);
            try
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    rows.Add(ValidateEntry(entries[i]));
                }
            }
            finally
            {
                if (originalSetup != null && originalSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
                }
            }

            HashSet<string> strictWhitelist = null;
            if (strictWarningGate)
            {
                strictWhitelist = LoadStrictWarningWhitelist();
                ApplyStrictWarningGate(rows, strictWhitelist);
            }

            int blockingTotal = 0;
            int warningTotal = 0;
            int errorRows = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                ValidationRow row = rows[i];
                blockingTotal += row.blockingErrors;
                warningTotal += row.warnings;
                if (string.Equals(row.status, "Error", StringComparison.Ordinal))
                {
                    errorRows++;
                }
            }

            int strictWhitelistedRows = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].strictWarningWhitelisted)
                {
                    strictWhitelistedRows++;
                }
            }

            string csvPath = WriteCsv(rows);
            string summaryPath = WriteSummary(
                rows,
                blockingTotal,
                warningTotal,
                strictWarningGate,
                strictWhitelist != null ? strictWhitelist.Count : 0,
                strictWhitelistedRows);
            AssetDatabase.Refresh();

            string summary =
                $"targets={rows.Count} errorRows={errorRows} blocking={blockingTotal} warnings={warningTotal} strictGate={(strictWarningGate ? 1 : 0)} " +
                $"strictWhitelistRows={strictWhitelistedRows} " +
                $"csv={csvPath} summary={summaryPath}";
            Debug.Log($"{LogPrefix} complete | {summary}");

            if (interactive)
            {
                EditorUtility.DisplayDialog("Boss Choreography Coverage", summary, "OK");
            }

            if (failOnBlocking && blockingTotal > 0)
            {
                throw new InvalidOperationException($"{LogPrefix} gate failed. blocking={blockingTotal} csv={csvPath}");
            }
        }

        private static HashSet<string> LoadStrictWarningWhitelist()
        {
            string fullPath = Path.GetFullPath(StrictWarningWhitelistCsvPath);
            var whitelist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(fullPath))
            {
                Debug.Log($"{LogPrefix} strict-warning whitelist missing; treat as empty. csv={StrictWarningWhitelistCsvPath}");
                return whitelist;
            }

            string[] lines;
            try
            {
                lines = File.ReadAllLines(fullPath);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{LogPrefix} strict-warning whitelist read failed; treat as empty. csv={StrictWarningWhitelistCsvPath} error={ex.Message}");
                return whitelist;
            }

            int levelIdColumn = 0;
            int enabledColumn = -1;
            bool schemaResolved = false;
            for (int i = 0; i < lines.Length; i++)
            {
                string rawLine = lines[i];
                string line = rawLine != null ? rawLine.Trim() : string.Empty;
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (line.StartsWith("#", StringComparison.Ordinal) || line.StartsWith("//", StringComparison.Ordinal))
                {
                    continue;
                }

                List<string> cells = SplitCsvLine(rawLine);
                if (!schemaResolved)
                {
                    int maybeLevel = IndexOfColumn(cells, "level_id");
                    int maybeEnabled = IndexOfColumn(cells, "enabled");
                    if (maybeLevel >= 0)
                    {
                        levelIdColumn = maybeLevel;
                        enabledColumn = maybeEnabled;
                        schemaResolved = true;
                        continue;
                    }

                    schemaResolved = true;
                }

                if (enabledColumn >= 0 && enabledColumn < cells.Count)
                {
                    if (!IsTrueLike(cells[enabledColumn]))
                    {
                        continue;
                    }
                }

                if (levelIdColumn < 0 || levelIdColumn >= cells.Count)
                {
                    continue;
                }

                string levelId = NormalizeText(cells[levelIdColumn]);
                if (string.IsNullOrWhiteSpace(levelId))
                {
                    continue;
                }

                whitelist.Add(levelId);
            }

            Debug.Log($"{LogPrefix} strict-warning whitelist loaded | entries={whitelist.Count} csv={StrictWarningWhitelistCsvPath}");
            return whitelist;
        }

        private static void ApplyStrictWarningGate(List<ValidationRow> rows, HashSet<string> strictWhitelist)
        {
            if (rows == null || rows.Count == 0)
            {
                return;
            }

            int escalatedRows = 0;
            int escalatedWarnings = 0;
            int whitelistedRows = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                ValidationRow row = rows[i];
                if (row.warnings <= 0)
                {
                    rows[i] = row;
                    continue;
                }

                bool isWhitelisted = IsStrictWarningWhitelisted(row, strictWhitelist);
                row.strictWarningWhitelisted = isWhitelisted;
                if (isWhitelisted)
                {
                    whitelistedRows++;
                    row.note = AppendNote(row.note, "[W] strict-warning-whitelisted");
                    rows[i] = row;
                    continue;
                }

                escalatedRows++;
                escalatedWarnings += row.warnings;
                row.blockingErrors += row.warnings;
                row.status = "Error";
                row.note = AppendNote(row.note, $"[B] strict-warning-gate escalated warnings={row.warnings}.");
                rows[i] = row;
            }

            Debug.Log(
                $"{LogPrefix} strict-warning gate applied | targets={rows.Count} " +
                $"escalatedRows={escalatedRows} escalatedWarnings={escalatedWarnings} " +
                $"whitelistedRows={whitelistedRows} whitelistEntries={(strictWhitelist != null ? strictWhitelist.Count : 0)}");
        }

        private static bool IsStrictWarningWhitelisted(ValidationRow row, HashSet<string> strictWhitelist)
        {
            if (strictWhitelist == null || strictWhitelist.Count == 0)
            {
                return false;
            }

            return ContainsWhitelistKey(row.levelId, strictWhitelist)
                || ContainsWhitelistKey(row.sceneName, strictWhitelist)
                || ContainsWhitelistKey(row.scenePath, strictWhitelist)
                || ContainsWhitelistKey(row.levelAssetPath, strictWhitelist);
        }

        private static bool ContainsWhitelistKey(string key, HashSet<string> strictWhitelist)
        {
            if (strictWhitelist == null || strictWhitelist.Count == 0)
            {
                return false;
            }

            string normalized = NormalizeText(key);
            return !string.IsNullOrWhiteSpace(normalized) && strictWhitelist.Contains(normalized);
        }

        private static int IndexOfColumn(List<string> columns, string target)
        {
            if (columns == null || columns.Count == 0 || string.IsNullOrWhiteSpace(target))
            {
                return -1;
            }

            for (int i = 0; i < columns.Count; i++)
            {
                string normalized = NormalizeText(columns[i]);
                if (string.Equals(normalized, target, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool IsTrueLike(string value)
        {
            string normalized = NormalizeText(value);
            return string.Equals(normalized, "1", StringComparison.Ordinal)
                || string.Equals(normalized, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "yes", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "y", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "on", StringComparison.OrdinalIgnoreCase);
        }

        private static List<string> SplitCsvLine(string line)
        {
            var cells = new List<string>();
            if (line == null)
            {
                cells.Add(string.Empty);
                return cells;
            }

            var token = new StringBuilder();
            bool inQuotes = false;
            for (int i = 0; i < line.Length; i++)
            {
                char ch = line[i];
                if (ch == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        token.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }

                    continue;
                }

                if (ch == ',' && !inQuotes)
                {
                    cells.Add(token.ToString());
                    token.Length = 0;
                    continue;
                }

                token.Append(ch);
            }

            cells.Add(token.ToString());
            return cells;
        }

        private static string AppendNote(string note, string append)
        {
            string existing = NormalizeText(note);
            string extra = NormalizeText(append);
            if (string.IsNullOrWhiteSpace(existing))
            {
                return extra;
            }

            if (string.IsNullOrWhiteSpace(extra))
            {
                return existing;
            }

            return $"{existing} {extra}";
        }

        private static ValidationRow ValidateEntry(LevelEntry entry)
        {
            var row = new ValidationRow
            {
                levelId = entry.levelData != null ? entry.levelData.levelId : string.Empty,
                levelAssetPath = entry.levelAssetPath ?? string.Empty,
                sceneName = entry.levelData != null ? entry.levelData.sceneName : string.Empty,
                scenePath = BuildScenePath(entry.levelData),
                status = "Error",
                blockingErrors = 0,
                warnings = 0,
                runtimeConfiguratorName = string.Empty,
                bossSpawnPointName = string.Empty,
                bossSpawnPointCount = 0,
                bossPrefabPath = string.Empty,
                note = string.Empty
            };

            var issues = new List<ValidationIssue>(32);
            if (entry.levelData == null)
            {
                issues.Add(ValidationIssue.Blocking("LevelData asset is null."));
                return BuildRow(row, issues);
            }

            row.levelOverrideBossSettings = entry.levelData.overrideBossSettings;
            row.levelOverrideEncounterTuning = entry.levelData.overrideBossEncounterTuning;
            row.levelEnablePhaseOpeners = entry.levelData.bossEnablePhaseTransitionOpeners;
            row.levelPhase2OpenerId = NormalizeText(entry.levelData.bossPhase2TransitionOpenerId);
            row.levelPhase3OpenerId = NormalizeText(entry.levelData.bossPhase3TransitionOpenerId);
            row.levelEnablePhaseOpenerRetry = entry.levelData.bossEnablePhaseTransitionOpenerRetry;
            row.levelPhaseOpenerRetryDelay = entry.levelData.bossPhaseTransitionOpenerRetryDelay;
            row.levelPhaseOpenerMaxRetries = entry.levelData.bossPhaseTransitionOpenerMaxRetries;
            row.levelEnablePhaseFollowupChain = entry.levelData.bossEnablePhaseTransitionFollowupChain;
            row.levelPhase2FollowupId = NormalizeText(entry.levelData.bossPhase2TransitionFollowupId);
            row.levelPhase3FollowupId = NormalizeText(entry.levelData.bossPhase3TransitionFollowupId);
            row.levelEnablePhaseFollowupRetry = entry.levelData.bossEnablePhaseTransitionFollowupRetry;
            row.levelPhaseFollowupRetryDelay = entry.levelData.bossPhaseTransitionFollowupRetryDelay;
            row.levelPhaseFollowupMaxRetries = entry.levelData.bossPhaseTransitionFollowupMaxRetries;
            row.levelEnablePhase3PriorityWindow = entry.levelData.bossEnablePhase3SpecialPriorityWindow;
            row.levelPhase3PriorityDuration = entry.levelData.bossPhase3SpecialPriorityDuration;
            row.levelPhase3PriorityWeight = entry.levelData.bossPhase3SpecialPriorityWeightMultiplier;
            row.levelForceSpecialQueueInPhase3Priority = entry.levelData.bossForceSpecialQueueDuringPhase3Priority;
            ValidateLevelEncounterGrammar(entry.levelData, issues);

            if (!entry.levelData.overrideBossSettings)
            {
                issues.Add(ValidationIssue.Warning("overrideBossSettings=false; choreography mapping is not active."));
                return BuildRow(row, issues);
            }

            if (string.IsNullOrWhiteSpace(row.scenePath))
            {
                issues.Add(ValidationIssue.Blocking("LevelData.sceneName is empty."));
                return BuildRow(row, issues);
            }

            if (!AssetExists(row.scenePath))
            {
                issues.Add(ValidationIssue.Blocking("Scene asset is missing."));
                return BuildRow(row, issues);
            }

            Scene scene;
            try
            {
                scene = EditorSceneManager.OpenScene(row.scenePath, OpenSceneMode.Single);
            }
            catch (Exception ex)
            {
                issues.Add(ValidationIssue.Blocking($"OpenScene failed: {ex.Message}"));
                return BuildRow(row, issues);
            }

            LevelRuntimeConfigurator runtimeConfigurator = FindComponentInScene<LevelRuntimeConfigurator>(scene);
            StrongholdSequenceController sequence = FindComponentInScene<StrongholdSequenceController>(scene);
            List<BossSpawnPoint> spawnPoints = FindComponentsInScene<BossSpawnPoint>(scene);
            row.runtimeConfiguratorName = runtimeConfigurator != null ? runtimeConfigurator.name : string.Empty;
            row.bossSpawnPointCount = spawnPoints.Count;

            if (runtimeConfigurator == null)
            {
                issues.Add(ValidationIssue.Blocking("Missing LevelRuntimeConfigurator in scene."));
            }

            if (spawnPoints.Count == 0)
            {
                issues.Add(ValidationIssue.Blocking("No BossSpawnPoint found in scene."));
                return BuildRow(row, issues);
            }

            BossSpawnPoint selectedSpawn = SelectSpawnPoint(sequence, runtimeConfigurator, spawnPoints);
            if (selectedSpawn == null)
            {
                issues.Add(ValidationIssue.Blocking("Unable to resolve active BossSpawnPoint."));
                return BuildRow(row, issues);
            }

            row.bossSpawnPointName = selectedSpawn.name;

            if (ConfigureBossMethod == null)
            {
                issues.Add(ValidationIssue.Blocking("LevelRuntimeConfigurator.ConfigureBoss reflection binding failed."));
            }
            else if (runtimeConfigurator != null)
            {
                try
                {
                    runtimeConfigurator.levelData = entry.levelData;
                    runtimeConfigurator.bossSpawnPoint = selectedSpawn;
                    ConfigureBossMethod.Invoke(runtimeConfigurator, null);
                }
                catch (TargetInvocationException ex)
                {
                    Exception inner = ex.InnerException ?? ex;
                    issues.Add(ValidationIssue.Blocking($"ConfigureBoss invocation failed: {inner.Message}"));
                }
                catch (Exception ex)
                {
                    issues.Add(ValidationIssue.Blocking($"ConfigureBoss reflection failed: {ex.Message}"));
                }
            }

            row.spawnOverrideEncounterTuning = selectedSpawn.overrideEncounterTuning;
            row.spawnEnablePhaseOpeners = selectedSpawn.enablePhaseTransitionOpeners;
            row.spawnPhase2OpenerId = NormalizeText(selectedSpawn.phase2TransitionOpenerId);
            row.spawnPhase3OpenerId = NormalizeText(selectedSpawn.phase3TransitionOpenerId);
            row.spawnEnablePhaseOpenerRetry = selectedSpawn.enablePhaseTransitionOpenerRetry;
            row.spawnPhaseOpenerRetryDelay = selectedSpawn.phaseTransitionOpenerRetryDelay;
            row.spawnPhaseOpenerMaxRetries = selectedSpawn.phaseTransitionOpenerMaxRetries;
            row.spawnEnablePhaseFollowupChain = selectedSpawn.enablePhaseTransitionFollowupChain;
            row.spawnPhase2FollowupId = NormalizeText(selectedSpawn.phase2TransitionFollowupId);
            row.spawnPhase3FollowupId = NormalizeText(selectedSpawn.phase3TransitionFollowupId);
            row.spawnEnablePhaseFollowupRetry = selectedSpawn.enablePhaseTransitionFollowupRetry;
            row.spawnPhaseFollowupRetryDelay = selectedSpawn.phaseTransitionFollowupRetryDelay;
            row.spawnPhaseFollowupMaxRetries = selectedSpawn.phaseTransitionFollowupMaxRetries;
            row.spawnEnablePhase3PriorityWindow = selectedSpawn.enablePhase3SpecialPriorityWindow;
            row.spawnPhase3PriorityDuration = selectedSpawn.phase3SpecialPriorityDuration;
            row.spawnPhase3PriorityWeight = selectedSpawn.phase3SpecialPriorityWeightMultiplier;
            row.spawnForceSpecialQueueInPhase3Priority = selectedSpawn.forceSpecialQueueDuringPhase3Priority;
            ValidateSpawnEncounterGrammar(selectedSpawn, issues);

            if (!entry.levelData.overrideBossEncounterTuning)
            {
                issues.Add(ValidationIssue.Warning("overrideBossEncounterTuning=false; round4 choreography chain is bypassed."));
            }

            CompareLevelDataToSpawnPoint(entry.levelData, selectedSpawn, issues);

            var probeHost = new GameObject("__BossRound5ControllerProbe__");
            probeHost.hideFlags = HideFlags.HideAndDontSave;
            BossController probeController = probeHost.AddComponent<BossController>();
            try
            {
                if (ApplyEncounterTuningMethod == null)
                {
                    issues.Add(ValidationIssue.Blocking("BossSpawnPoint.ApplyEncounterTuning(BossController) reflection binding failed."));
                }
                else
                {
                    ApplyEncounterTuningMethod.Invoke(selectedSpawn, new object[] { probeController });
                    row.controllerEnablePhaseOpeners = probeController.enablePhaseTransitionOpeners;
                    row.controllerPhase2OpenerId = NormalizeText(probeController.phase2TransitionOpenerId);
                    row.controllerPhase3OpenerId = NormalizeText(probeController.phase3TransitionOpenerId);
                    row.controllerEnablePhaseOpenerRetry = probeController.enablePhaseTransitionOpenerRetry;
                    row.controllerPhaseOpenerRetryDelay = probeController.phaseTransitionOpenerRetryDelay;
                    row.controllerPhaseOpenerMaxRetries = probeController.phaseTransitionOpenerMaxRetries;
                    row.controllerEnablePhaseFollowupChain = probeController.enablePhaseTransitionFollowupChain;
                    row.controllerPhase2FollowupId = NormalizeText(probeController.phase2TransitionFollowupId);
                    row.controllerPhase3FollowupId = NormalizeText(probeController.phase3TransitionFollowupId);
                    row.controllerEnablePhaseFollowupRetry = probeController.enablePhaseTransitionFollowupRetry;
                    row.controllerPhaseFollowupRetryDelay = probeController.phaseTransitionFollowupRetryDelay;
                    row.controllerPhaseFollowupMaxRetries = probeController.phaseTransitionFollowupMaxRetries;
                    row.controllerEnablePhase3PriorityWindow = probeController.enablePhase3SpecialPriorityWindow;
                    row.controllerPhase3PriorityDuration = probeController.phase3SpecialPriorityDuration;
                    row.controllerPhase3PriorityWeight = probeController.phase3SpecialPriorityWeightMultiplier;
                    row.controllerForceSpecialQueueInPhase3Priority = probeController.forceSpecialQueueDuringPhase3Priority;

                    CompareSpawnPointToController(selectedSpawn, probeController, issues);
                    ValidateControllerEncounterGrammar(probeController, issues);
                }
            }
            catch (TargetInvocationException ex)
            {
                Exception inner = ex.InnerException ?? ex;
                issues.Add(ValidationIssue.Blocking($"ApplyEncounterTuning invocation failed: {inner.Message}"));
            }
            catch (Exception ex)
            {
                issues.Add(ValidationIssue.Blocking($"ApplyEncounterTuning reflection failed: {ex.Message}"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(probeHost);
            }

            GameObject bossPrefab = ResolveBossPrefab(selectedSpawn);
            row.bossPrefabPath = bossPrefab != null ? AssetDatabase.GetAssetPath(bossPrefab) : string.Empty;
            ValidatePrefabAttackCoverage(selectedSpawn, bossPrefab, ref row, issues);

            return BuildRow(row, issues);
        }

        private static BossSpawnPoint SelectSpawnPoint(
            StrongholdSequenceController sequence,
            LevelRuntimeConfigurator runtimeConfigurator,
            List<BossSpawnPoint> spawnPoints)
        {
            if (spawnPoints == null || spawnPoints.Count == 0)
            {
                return null;
            }

            if (sequence != null && sequence.bossSpawnPoint != null && spawnPoints.Contains(sequence.bossSpawnPoint))
            {
                return sequence.bossSpawnPoint;
            }

            if (runtimeConfigurator != null &&
                runtimeConfigurator.bossSpawnPoint != null &&
                spawnPoints.Contains(runtimeConfigurator.bossSpawnPoint))
            {
                return runtimeConfigurator.bossSpawnPoint;
            }

            spawnPoints.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
            return spawnPoints[0];
        }

        private static void CompareLevelDataToSpawnPoint(LevelData levelData, BossSpawnPoint spawnPoint, List<ValidationIssue> issues)
        {
            if (levelData == null || spawnPoint == null)
            {
                return;
            }

            if (levelData.overrideBossEncounterTuning && !spawnPoint.overrideEncounterTuning)
            {
                issues.Add(ValidationIssue.Blocking("LevelData overrideBossEncounterTuning=true but BossSpawnPoint.overrideEncounterTuning=false."));
                return;
            }

            if (!levelData.overrideBossEncounterTuning)
            {
                return;
            }

            AddBoolMismatchIssue(
                issues,
                "bossEnablePhaseTransitionOpeners",
                levelData.bossEnablePhaseTransitionOpeners,
                spawnPoint.enablePhaseTransitionOpeners);
            AddStringMismatchIssue(
                issues,
                "bossPhase2TransitionOpenerId",
                levelData.bossPhase2TransitionOpenerId,
                spawnPoint.phase2TransitionOpenerId);
            AddStringMismatchIssue(
                issues,
                "bossPhase3TransitionOpenerId",
                levelData.bossPhase3TransitionOpenerId,
                spawnPoint.phase3TransitionOpenerId);
            AddBoolMismatchIssue(
                issues,
                "bossEnablePhaseTransitionOpenerRetry",
                levelData.bossEnablePhaseTransitionOpenerRetry,
                spawnPoint.enablePhaseTransitionOpenerRetry);
            AddFloatMismatchIssue(
                issues,
                "bossPhaseTransitionOpenerRetryDelay",
                Mathf.Max(0f, levelData.bossPhaseTransitionOpenerRetryDelay),
                Mathf.Max(0f, spawnPoint.phaseTransitionOpenerRetryDelay));
            AddIntMismatchIssue(
                issues,
                "bossPhaseTransitionOpenerMaxRetries",
                Mathf.Max(0, levelData.bossPhaseTransitionOpenerMaxRetries),
                Mathf.Max(0, spawnPoint.phaseTransitionOpenerMaxRetries));
            AddBoolMismatchIssue(
                issues,
                "bossEnablePhaseTransitionFollowupChain",
                levelData.bossEnablePhaseTransitionFollowupChain,
                spawnPoint.enablePhaseTransitionFollowupChain);
            AddStringMismatchIssue(
                issues,
                "bossPhase2TransitionFollowupId",
                levelData.bossPhase2TransitionFollowupId,
                spawnPoint.phase2TransitionFollowupId);
            AddStringMismatchIssue(
                issues,
                "bossPhase3TransitionFollowupId",
                levelData.bossPhase3TransitionFollowupId,
                spawnPoint.phase3TransitionFollowupId);
            AddBoolMismatchIssue(
                issues,
                "bossEnablePhaseTransitionFollowupRetry",
                levelData.bossEnablePhaseTransitionFollowupRetry,
                spawnPoint.enablePhaseTransitionFollowupRetry);
            AddFloatMismatchIssue(
                issues,
                "bossPhaseTransitionFollowupRetryDelay",
                Mathf.Max(0f, levelData.bossPhaseTransitionFollowupRetryDelay),
                Mathf.Max(0f, spawnPoint.phaseTransitionFollowupRetryDelay));
            AddIntMismatchIssue(
                issues,
                "bossPhaseTransitionFollowupMaxRetries",
                Mathf.Max(0, levelData.bossPhaseTransitionFollowupMaxRetries),
                Mathf.Max(0, spawnPoint.phaseTransitionFollowupMaxRetries));
            AddBoolMismatchIssue(
                issues,
                "bossEnablePhase3SpecialPriorityWindow",
                levelData.bossEnablePhase3SpecialPriorityWindow,
                spawnPoint.enablePhase3SpecialPriorityWindow);
            AddFloatMismatchIssue(
                issues,
                "bossPhase3SpecialPriorityDuration",
                levelData.bossPhase3SpecialPriorityDuration,
                spawnPoint.phase3SpecialPriorityDuration);
            AddFloatMismatchIssue(
                issues,
                "bossPhase3SpecialPriorityWeightMultiplier",
                levelData.bossPhase3SpecialPriorityWeightMultiplier,
                spawnPoint.phase3SpecialPriorityWeightMultiplier);
            AddBoolMismatchIssue(
                issues,
                "bossForceSpecialQueueDuringPhase3Priority",
                levelData.bossForceSpecialQueueDuringPhase3Priority,
                spawnPoint.forceSpecialQueueDuringPhase3Priority);
        }

        private static void CompareSpawnPointToController(BossSpawnPoint spawnPoint, BossController controller, List<ValidationIssue> issues)
        {
            if (spawnPoint == null || controller == null)
            {
                return;
            }

            if (!spawnPoint.overrideEncounterTuning)
            {
                return;
            }

            AddBoolMismatchIssue(
                issues,
                "spawn.enablePhaseTransitionOpeners -> controller.enablePhaseTransitionOpeners",
                spawnPoint.enablePhaseTransitionOpeners,
                controller.enablePhaseTransitionOpeners);
            AddStringMismatchIssue(
                issues,
                "spawn.phase2TransitionOpenerId -> controller.phase2TransitionOpenerId",
                spawnPoint.phase2TransitionOpenerId,
                controller.phase2TransitionOpenerId);
            AddStringMismatchIssue(
                issues,
                "spawn.phase3TransitionOpenerId -> controller.phase3TransitionOpenerId",
                spawnPoint.phase3TransitionOpenerId,
                controller.phase3TransitionOpenerId);
            AddBoolMismatchIssue(
                issues,
                "spawn.enablePhaseTransitionOpenerRetry -> controller.enablePhaseTransitionOpenerRetry",
                spawnPoint.enablePhaseTransitionOpenerRetry,
                controller.enablePhaseTransitionOpenerRetry);
            AddFloatMismatchIssue(
                issues,
                "spawn.phaseTransitionOpenerRetryDelay -> controller.phaseTransitionOpenerRetryDelay",
                Mathf.Max(0f, spawnPoint.phaseTransitionOpenerRetryDelay),
                Mathf.Max(0f, controller.phaseTransitionOpenerRetryDelay));
            AddIntMismatchIssue(
                issues,
                "spawn.phaseTransitionOpenerMaxRetries -> controller.phaseTransitionOpenerMaxRetries",
                Mathf.Max(0, spawnPoint.phaseTransitionOpenerMaxRetries),
                Mathf.Max(0, controller.phaseTransitionOpenerMaxRetries));
            AddBoolMismatchIssue(
                issues,
                "spawn.enablePhaseTransitionFollowupChain -> controller.enablePhaseTransitionFollowupChain",
                spawnPoint.enablePhaseTransitionFollowupChain,
                controller.enablePhaseTransitionFollowupChain);
            AddStringMismatchIssue(
                issues,
                "spawn.phase2TransitionFollowupId -> controller.phase2TransitionFollowupId",
                spawnPoint.phase2TransitionFollowupId,
                controller.phase2TransitionFollowupId);
            AddStringMismatchIssue(
                issues,
                "spawn.phase3TransitionFollowupId -> controller.phase3TransitionFollowupId",
                spawnPoint.phase3TransitionFollowupId,
                controller.phase3TransitionFollowupId);
            AddBoolMismatchIssue(
                issues,
                "spawn.enablePhaseTransitionFollowupRetry -> controller.enablePhaseTransitionFollowupRetry",
                spawnPoint.enablePhaseTransitionFollowupRetry,
                controller.enablePhaseTransitionFollowupRetry);
            AddFloatMismatchIssue(
                issues,
                "spawn.phaseTransitionFollowupRetryDelay -> controller.phaseTransitionFollowupRetryDelay",
                Mathf.Max(0f, spawnPoint.phaseTransitionFollowupRetryDelay),
                Mathf.Max(0f, controller.phaseTransitionFollowupRetryDelay));
            AddIntMismatchIssue(
                issues,
                "spawn.phaseTransitionFollowupMaxRetries -> controller.phaseTransitionFollowupMaxRetries",
                Mathf.Max(0, spawnPoint.phaseTransitionFollowupMaxRetries),
                Mathf.Max(0, controller.phaseTransitionFollowupMaxRetries));
            AddBoolMismatchIssue(
                issues,
                "spawn.enablePhase3SpecialPriorityWindow -> controller.enablePhase3SpecialPriorityWindow",
                spawnPoint.enablePhase3SpecialPriorityWindow,
                controller.enablePhase3SpecialPriorityWindow);
            AddFloatMismatchIssue(
                issues,
                "spawn.phase3SpecialPriorityDuration -> controller.phase3SpecialPriorityDuration",
                Mathf.Max(0f, spawnPoint.phase3SpecialPriorityDuration),
                controller.phase3SpecialPriorityDuration);
            AddFloatMismatchIssue(
                issues,
                "spawn.phase3SpecialPriorityWeightMultiplier -> controller.phase3SpecialPriorityWeightMultiplier",
                Mathf.Max(1f, spawnPoint.phase3SpecialPriorityWeightMultiplier),
                controller.phase3SpecialPriorityWeightMultiplier);
            AddBoolMismatchIssue(
                issues,
                "spawn.forceSpecialQueueDuringPhase3Priority -> controller.forceSpecialQueueDuringPhase3Priority",
                spawnPoint.forceSpecialQueueDuringPhase3Priority,
                controller.forceSpecialQueueDuringPhase3Priority);

            if (controller.forceSpecialQueueDuringPhase3Priority && !controller.enablePhase3SpecialPriorityWindow)
            {
                issues.Add(ValidationIssue.Blocking(
                    "controller.forceSpecialQueueDuringPhase3Priority=true but controller.enablePhase3SpecialPriorityWindow=false."));
            }

            if (controller.enablePhaseTransitionOpeners &&
                string.IsNullOrWhiteSpace(controller.phase2TransitionOpenerId) &&
                string.IsNullOrWhiteSpace(controller.phase3TransitionOpenerId))
            {
                issues.Add(ValidationIssue.Warning("Phase openers enabled but both opener ids are empty."));
            }

            if (controller.enablePhaseTransitionOpenerRetry && !controller.enablePhaseTransitionOpeners)
            {
                issues.Add(ValidationIssue.Warning(
                    "Phase opener retry enabled while phase openers are disabled; retry path is currently unreachable."));
            }

            if (controller.enablePhaseTransitionOpenerRetry && controller.phaseTransitionOpenerMaxRetries <= 0)
            {
                issues.Add(ValidationIssue.Warning(
                    "Phase opener retry enabled but max retries <= 0; fallback queue path is effectively disabled."));
            }

            if (controller.enablePhaseTransitionFollowupChain &&
                string.IsNullOrWhiteSpace(controller.phase2TransitionFollowupId) &&
                string.IsNullOrWhiteSpace(controller.phase3TransitionFollowupId))
            {
                issues.Add(ValidationIssue.Warning("Phase followup chain enabled but both followup ids are empty."));
            }

            if (controller.enablePhaseTransitionFollowupRetry && !controller.enablePhaseTransitionFollowupChain)
            {
                issues.Add(ValidationIssue.Warning(
                    "Phase followup retry enabled while followup chain is disabled; retry path is currently unreachable."));
            }

            if (controller.enablePhaseTransitionFollowupRetry && controller.phaseTransitionFollowupMaxRetries <= 0)
            {
                issues.Add(ValidationIssue.Warning(
                    "Phase followup retry enabled but max retries <= 0; followup fallback path is effectively disabled."));
            }
        }

        private static void ValidateLevelEncounterGrammar(LevelData levelData, List<ValidationIssue> issues)
        {
            if (levelData == null || issues == null || !levelData.overrideBossEncounterTuning)
            {
                return;
            }

            ValidateEncounterGrammar(
                source: "LevelData",
                phase2Threshold: levelData.bossPhase2HealthThreshold,
                phase3Threshold: levelData.bossPhase3HealthThreshold,
                breakWindowDuration: levelData.bossBreakWindowDuration,
                breakWindowCooldown: levelData.bossBreakWindowCooldown,
                breakWindowDamageMultiplier: levelData.bossBreakWindowDamageMultiplier,
                attackInterval: levelData.bossAttackInterval,
                decisionInterval: levelData.bossDecisionInterval,
                queuedAttackLimit: levelData.bossQueuedAttackLimit,
                immediateRepeatPenalty: levelData.bossImmediateRepeatPenalty,
                enablePostBreakPunishWindow: levelData.bossEnablePostBreakPunishWindow,
                postBreakPunishDuration: levelData.bossPostBreakPunishDuration,
                postBreakAttackIntervalMultiplier: levelData.bossPostBreakAttackIntervalMultiplier,
                postBreakDecisionIntervalMultiplier: levelData.bossPostBreakDecisionIntervalMultiplier,
                postBreakChaseSpeedMultiplier: levelData.bossPostBreakChaseSpeedMultiplier,
                enableInterruptRecoveryGate: levelData.bossEnableInterruptRecoveryGate,
                interruptRecoveryDuration: levelData.bossInterruptRecoveryDuration,
                interruptedAttackCooldownScale: levelData.bossInterruptedAttackCooldownScale,
                enablePhaseOpeners: levelData.bossEnablePhaseTransitionOpeners,
                phase2OpenerId: levelData.bossPhase2TransitionOpenerId,
                phase3OpenerId: levelData.bossPhase3TransitionOpenerId,
                enablePhaseOpenerRetry: levelData.bossEnablePhaseTransitionOpenerRetry,
                phaseOpenerRetryDelay: levelData.bossPhaseTransitionOpenerRetryDelay,
                phaseOpenerMaxRetries: levelData.bossPhaseTransitionOpenerMaxRetries,
                enablePhaseFollowupChain: levelData.bossEnablePhaseTransitionFollowupChain,
                phase2FollowupId: levelData.bossPhase2TransitionFollowupId,
                phase3FollowupId: levelData.bossPhase3TransitionFollowupId,
                enablePhaseFollowupRetry: levelData.bossEnablePhaseTransitionFollowupRetry,
                phaseFollowupRetryDelay: levelData.bossPhaseTransitionFollowupRetryDelay,
                phaseFollowupMaxRetries: levelData.bossPhaseTransitionFollowupMaxRetries,
                enablePhase3PriorityWindow: levelData.bossEnablePhase3SpecialPriorityWindow,
                phase3PriorityDuration: levelData.bossPhase3SpecialPriorityDuration,
                phase3PriorityWeight: levelData.bossPhase3SpecialPriorityWeightMultiplier,
                enablePhaseComboChain: levelData.bossEnablePhaseComboChain,
                phase2ComboChance: levelData.bossPhase2ComboChance,
                phase3ComboChance: levelData.bossPhase3ComboChance,
                enableTimePressure: levelData.bossEnableTimePressure,
                timePressureDelay: levelData.bossTimePressureDelay,
                timePressureRampDuration: levelData.bossTimePressureRampDuration,
                maxTimePressureDamageMultiplier: levelData.bossMaxTimePressureDamageMultiplier,
                maxTimePressureSpeedMultiplier: levelData.bossMaxTimePressureSpeedMultiplier,
                issues: issues);
        }

        private static void ValidateSpawnEncounterGrammar(BossSpawnPoint spawnPoint, List<ValidationIssue> issues)
        {
            if (spawnPoint == null || issues == null || !spawnPoint.overrideEncounterTuning)
            {
                return;
            }

            ValidateEncounterGrammar(
                source: "BossSpawnPoint",
                phase2Threshold: spawnPoint.phase2HealthThreshold,
                phase3Threshold: spawnPoint.phase3HealthThreshold,
                breakWindowDuration: spawnPoint.breakWindowDuration,
                breakWindowCooldown: spawnPoint.breakWindowCooldown,
                breakWindowDamageMultiplier: spawnPoint.breakWindowDamageMultiplier,
                attackInterval: spawnPoint.attackInterval,
                decisionInterval: spawnPoint.decisionInterval,
                queuedAttackLimit: spawnPoint.queuedAttackLimit,
                immediateRepeatPenalty: spawnPoint.immediateRepeatPenalty,
                enablePostBreakPunishWindow: spawnPoint.enablePostBreakPunishWindow,
                postBreakPunishDuration: spawnPoint.postBreakPunishDuration,
                postBreakAttackIntervalMultiplier: spawnPoint.postBreakAttackIntervalMultiplier,
                postBreakDecisionIntervalMultiplier: spawnPoint.postBreakDecisionIntervalMultiplier,
                postBreakChaseSpeedMultiplier: spawnPoint.postBreakChaseSpeedMultiplier,
                enableInterruptRecoveryGate: spawnPoint.enableInterruptRecoveryGate,
                interruptRecoveryDuration: spawnPoint.interruptRecoveryDuration,
                interruptedAttackCooldownScale: spawnPoint.interruptedAttackCooldownScale,
                enablePhaseOpeners: spawnPoint.enablePhaseTransitionOpeners,
                phase2OpenerId: spawnPoint.phase2TransitionOpenerId,
                phase3OpenerId: spawnPoint.phase3TransitionOpenerId,
                enablePhaseOpenerRetry: spawnPoint.enablePhaseTransitionOpenerRetry,
                phaseOpenerRetryDelay: spawnPoint.phaseTransitionOpenerRetryDelay,
                phaseOpenerMaxRetries: spawnPoint.phaseTransitionOpenerMaxRetries,
                enablePhaseFollowupChain: spawnPoint.enablePhaseTransitionFollowupChain,
                phase2FollowupId: spawnPoint.phase2TransitionFollowupId,
                phase3FollowupId: spawnPoint.phase3TransitionFollowupId,
                enablePhaseFollowupRetry: spawnPoint.enablePhaseTransitionFollowupRetry,
                phaseFollowupRetryDelay: spawnPoint.phaseTransitionFollowupRetryDelay,
                phaseFollowupMaxRetries: spawnPoint.phaseTransitionFollowupMaxRetries,
                enablePhase3PriorityWindow: spawnPoint.enablePhase3SpecialPriorityWindow,
                phase3PriorityDuration: spawnPoint.phase3SpecialPriorityDuration,
                phase3PriorityWeight: spawnPoint.phase3SpecialPriorityWeightMultiplier,
                enablePhaseComboChain: spawnPoint.enablePhaseComboChain,
                phase2ComboChance: spawnPoint.phase2ComboChance,
                phase3ComboChance: spawnPoint.phase3ComboChance,
                enableTimePressure: spawnPoint.enableTimePressure,
                timePressureDelay: spawnPoint.timePressureDelay,
                timePressureRampDuration: spawnPoint.timePressureRampDuration,
                maxTimePressureDamageMultiplier: spawnPoint.maxTimePressureDamageMultiplier,
                maxTimePressureSpeedMultiplier: spawnPoint.maxTimePressureSpeedMultiplier,
                issues: issues);
        }

        private static void ValidateControllerEncounterGrammar(BossController controller, List<ValidationIssue> issues)
        {
            if (controller == null || issues == null)
            {
                return;
            }

            ValidateEncounterGrammar(
                source: "BossController",
                phase2Threshold: ResolvePhaseThreshold(controller, 1),
                phase3Threshold: ResolvePhaseThreshold(controller, 2),
                breakWindowDuration: controller.breakWindowDuration,
                breakWindowCooldown: controller.breakWindowCooldown,
                breakWindowDamageMultiplier: controller.breakWindowDamageMultiplier,
                attackInterval: controller.attackInterval,
                decisionInterval: controller.decisionInterval,
                queuedAttackLimit: controller.queuedAttackLimit,
                immediateRepeatPenalty: controller.immediateRepeatPenalty,
                enablePostBreakPunishWindow: controller.enablePostBreakPunishWindow,
                postBreakPunishDuration: controller.postBreakPunishDuration,
                postBreakAttackIntervalMultiplier: controller.postBreakAttackIntervalMultiplier,
                postBreakDecisionIntervalMultiplier: controller.postBreakDecisionIntervalMultiplier,
                postBreakChaseSpeedMultiplier: controller.postBreakChaseSpeedMultiplier,
                enableInterruptRecoveryGate: controller.enableInterruptRecoveryGate,
                interruptRecoveryDuration: controller.interruptRecoveryDuration,
                interruptedAttackCooldownScale: controller.interruptedAttackCooldownScale,
                enablePhaseOpeners: controller.enablePhaseTransitionOpeners,
                phase2OpenerId: controller.phase2TransitionOpenerId,
                phase3OpenerId: controller.phase3TransitionOpenerId,
                enablePhaseOpenerRetry: controller.enablePhaseTransitionOpenerRetry,
                phaseOpenerRetryDelay: controller.phaseTransitionOpenerRetryDelay,
                phaseOpenerMaxRetries: controller.phaseTransitionOpenerMaxRetries,
                enablePhaseFollowupChain: controller.enablePhaseTransitionFollowupChain,
                phase2FollowupId: controller.phase2TransitionFollowupId,
                phase3FollowupId: controller.phase3TransitionFollowupId,
                enablePhaseFollowupRetry: controller.enablePhaseTransitionFollowupRetry,
                phaseFollowupRetryDelay: controller.phaseTransitionFollowupRetryDelay,
                phaseFollowupMaxRetries: controller.phaseTransitionFollowupMaxRetries,
                enablePhase3PriorityWindow: controller.enablePhase3SpecialPriorityWindow,
                phase3PriorityDuration: controller.phase3SpecialPriorityDuration,
                phase3PriorityWeight: controller.phase3SpecialPriorityWeightMultiplier,
                enablePhaseComboChain: controller.enablePhaseComboChain,
                phase2ComboChance: controller.phase2ComboChance,
                phase3ComboChance: controller.phase3ComboChance,
                enableTimePressure: controller.enableTimePressure,
                timePressureDelay: controller.timePressureDelay,
                timePressureRampDuration: controller.timePressureRampDuration,
                maxTimePressureDamageMultiplier: controller.maxTimePressureDamageMultiplier,
                maxTimePressureSpeedMultiplier: controller.maxTimePressureSpeedMultiplier,
                issues: issues);
        }

        private static void ValidateEncounterGrammar(
            string source,
            float phase2Threshold,
            float phase3Threshold,
            float breakWindowDuration,
            float breakWindowCooldown,
            float breakWindowDamageMultiplier,
            float attackInterval,
            float decisionInterval,
            int queuedAttackLimit,
            float immediateRepeatPenalty,
            bool enablePostBreakPunishWindow,
            float postBreakPunishDuration,
            float postBreakAttackIntervalMultiplier,
            float postBreakDecisionIntervalMultiplier,
            float postBreakChaseSpeedMultiplier,
            bool enableInterruptRecoveryGate,
            float interruptRecoveryDuration,
            float interruptedAttackCooldownScale,
            bool enablePhaseOpeners,
            string phase2OpenerId,
            string phase3OpenerId,
            bool enablePhaseOpenerRetry,
            float phaseOpenerRetryDelay,
            int phaseOpenerMaxRetries,
            bool enablePhaseFollowupChain,
            string phase2FollowupId,
            string phase3FollowupId,
            bool enablePhaseFollowupRetry,
            float phaseFollowupRetryDelay,
            int phaseFollowupMaxRetries,
            bool enablePhase3PriorityWindow,
            float phase3PriorityDuration,
            float phase3PriorityWeight,
            bool enablePhaseComboChain,
            float phase2ComboChance,
            float phase3ComboChance,
            bool enableTimePressure,
            float timePressureDelay,
            float timePressureRampDuration,
            float maxTimePressureDamageMultiplier,
            float maxTimePressureSpeedMultiplier,
            List<ValidationIssue> issues)
        {
            if (phase2Threshold <= 0.1f || phase2Threshold >= 0.95f)
            {
                issues.Add(ValidationIssue.Blocking($"{source}.phase2Threshold out of expected range (0.1,0.95)."));
            }

            if (phase3Threshold <= 0.05f || phase3Threshold >= phase2Threshold - 0.05f)
            {
                issues.Add(ValidationIssue.Blocking($"{source}.phase3Threshold should be >=0.05 and at least 0.05 lower than phase2Threshold."));
            }

            if (breakWindowDuration <= 0f)
            {
                issues.Add(ValidationIssue.Blocking($"{source}.breakWindowDuration must be > 0."));
            }

            if (breakWindowCooldown <= breakWindowDuration)
            {
                issues.Add(ValidationIssue.Blocking($"{source}.breakWindowCooldown must be > breakWindowDuration."));
            }

            if (breakWindowDamageMultiplier < 1f)
            {
                issues.Add(ValidationIssue.Blocking($"{source}.breakWindowDamageMultiplier must be >= 1."));
            }

            if (attackInterval <= 0f)
            {
                issues.Add(ValidationIssue.Blocking($"{source}.attackInterval must be > 0."));
            }

            if (decisionInterval < 0.05f)
            {
                issues.Add(ValidationIssue.Blocking($"{source}.decisionInterval must be >= 0.05."));
            }

            if (queuedAttackLimit < 1)
            {
                issues.Add(ValidationIssue.Blocking($"{source}.queuedAttackLimit must be >= 1."));
            }

            if (immediateRepeatPenalty < 0f || immediateRepeatPenalty > 1f)
            {
                issues.Add(ValidationIssue.Blocking($"{source}.immediateRepeatPenalty must be in [0,1]."));
            }

            if (enablePostBreakPunishWindow)
            {
                if (postBreakPunishDuration <= 0f)
                {
                    issues.Add(ValidationIssue.Blocking($"{source}.postBreakPunishDuration must be > 0 when post-break punish is enabled."));
                }

                if (postBreakAttackIntervalMultiplier < 0.3f || postBreakAttackIntervalMultiplier > 1f)
                {
                    issues.Add(ValidationIssue.Blocking($"{source}.postBreakAttackIntervalMultiplier must be in [0.3,1]."));
                }

                if (postBreakDecisionIntervalMultiplier < 0.3f || postBreakDecisionIntervalMultiplier > 1f)
                {
                    issues.Add(ValidationIssue.Blocking($"{source}.postBreakDecisionIntervalMultiplier must be in [0.3,1]."));
                }

                if (postBreakChaseSpeedMultiplier < 1f)
                {
                    issues.Add(ValidationIssue.Blocking($"{source}.postBreakChaseSpeedMultiplier must be >= 1."));
                }
            }

            if (enableInterruptRecoveryGate)
            {
                if (interruptRecoveryDuration < 0.08f || interruptRecoveryDuration > 2f)
                {
                    issues.Add(ValidationIssue.Blocking($"{source}.interruptRecoveryDuration must be in [0.08,2] when interrupt recovery is enabled."));
                }

                if (interruptedAttackCooldownScale < 0f || interruptedAttackCooldownScale > 1f)
                {
                    issues.Add(ValidationIssue.Blocking($"{source}.interruptedAttackCooldownScale must be in [0,1] when interrupt recovery is enabled."));
                }
            }

            if (enablePhaseOpeners)
            {
                if (string.IsNullOrWhiteSpace(phase2OpenerId))
                {
                    issues.Add(ValidationIssue.Blocking($"{source}.phase2OpenerId is empty while phase openers are enabled."));
                }

                if (string.IsNullOrWhiteSpace(phase3OpenerId))
                {
                    issues.Add(ValidationIssue.Blocking($"{source}.phase3OpenerId is empty while phase openers are enabled."));
                }

                if (enablePhaseOpenerRetry && (phaseOpenerRetryDelay <= 0f || phaseOpenerMaxRetries <= 0))
                {
                    issues.Add(ValidationIssue.Blocking($"{source}.phase opener retry is enabled but retry delay/max retries are invalid."));
                }
            }

            if (enablePhaseFollowupChain)
            {
                if (!enablePhaseOpeners)
                {
                    issues.Add(ValidationIssue.Blocking($"{source}.phase followup chain requires phase openers enabled."));
                }

                if (string.IsNullOrWhiteSpace(phase2FollowupId))
                {
                    issues.Add(ValidationIssue.Blocking($"{source}.phase2FollowupId is empty while phase followup chain is enabled."));
                }

                if (string.IsNullOrWhiteSpace(phase3FollowupId))
                {
                    issues.Add(ValidationIssue.Blocking($"{source}.phase3FollowupId is empty while phase followup chain is enabled."));
                }

                if (enablePhaseFollowupRetry && (phaseFollowupRetryDelay <= 0f || phaseFollowupMaxRetries <= 0))
                {
                    issues.Add(ValidationIssue.Blocking($"{source}.phase followup retry is enabled but retry delay/max retries are invalid."));
                }
            }

            if (enablePhaseFollowupRetry && !enablePhaseFollowupChain)
            {
                issues.Add(ValidationIssue.Blocking($"{source}.phase followup retry is enabled while followup chain is disabled."));
            }

            if (enablePhase3PriorityWindow)
            {
                if (phase3PriorityDuration <= 0f)
                {
                    issues.Add(ValidationIssue.Blocking($"{source}.phase3PriorityDuration must be > 0 when phase3 priority window is enabled."));
                }

                if (phase3PriorityWeight < 1f)
                {
                    issues.Add(ValidationIssue.Blocking($"{source}.phase3PriorityWeight must be >= 1 when phase3 priority window is enabled."));
                }
            }

            if (enablePhaseComboChain &&
                (phase2ComboChance < 0f || phase2ComboChance > 1f || phase3ComboChance < 0f || phase3ComboChance > 1f || phase3ComboChance < phase2ComboChance))
            {
                issues.Add(ValidationIssue.Blocking($"{source}.combo chance settings are invalid (require phase3 >= phase2 and both in [0,1])."));
            }

            if (enableTimePressure)
            {
                if (timePressureDelay < 15f)
                {
                    issues.Add(ValidationIssue.Blocking($"{source}.timePressureDelay must be >= 15 when time pressure is enabled."));
                }

                if (timePressureRampDuration < 1f)
                {
                    issues.Add(ValidationIssue.Blocking($"{source}.timePressureRampDuration must be >= 1 when time pressure is enabled."));
                }

                if (timePressureRampDuration < 20f)
                {
                    issues.Add(ValidationIssue.Blocking($"{source}.timePressureRampDuration must be >= 20 for Round6 pressure pacing."));
                }

                if (maxTimePressureDamageMultiplier < 1f || maxTimePressureSpeedMultiplier < 1f)
                {
                    issues.Add(ValidationIssue.Blocking($"{source}.time pressure max multipliers must be >= 1."));
                }

                if (enablePostBreakPunishWindow && timePressureDelay <= postBreakPunishDuration)
                {
                    issues.Add(ValidationIssue.Blocking(
                        $"{source}.timePressureDelay must be greater than postBreakPunishDuration to preserve counter-window readability."));
                }
            }
        }

        private static float ResolvePhaseThreshold(BossController controller, int phaseIndex)
        {
            if (controller == null || controller.phases == null || phaseIndex < 0 || phaseIndex >= controller.phases.Count)
            {
                return phaseIndex == 1 ? 0.66f : 0.33f;
            }

            BossPhase phase = controller.phases[phaseIndex];
            return phase != null ? phase.healthPercentThreshold : (phaseIndex == 1 ? 0.66f : 0.33f);
        }

        private static void ValidatePrefabAttackCoverage(
            BossSpawnPoint spawnPoint,
            GameObject bossPrefab,
            ref ValidationRow row,
            List<ValidationIssue> issues)
        {
            if (spawnPoint == null)
            {
                return;
            }

            if (bossPrefab == null)
            {
                issues.Add(ValidationIssue.Warning("Boss prefab is missing; skipped opener id coverage check."));
                return;
            }

            BossController prefabController = bossPrefab.GetComponent<BossController>();
            if (prefabController == null)
            {
                issues.Add(ValidationIssue.Warning("Boss prefab has no BossController; skipped opener id coverage check."));
                return;
            }

            var attackIds = new HashSet<string>(StringComparer.Ordinal);
            var attacksById = new Dictionary<string, BossAttack>(StringComparer.Ordinal);
            int specialCount = 0;
            int phase3EligibleSpecialCount = 0;
            if (prefabController.attacks != null)
            {
                for (int i = 0; i < prefabController.attacks.Count; i++)
                {
                    BossAttack attack = prefabController.attacks[i];
                    if (attack == null || string.IsNullOrWhiteSpace(attack.attackId))
                    {
                        continue;
                    }

                    string attackId = attack.attackId.Trim();
                    if (attackIds.Add(attackId))
                    {
                        attacksById[attackId] = attack;
                    }
                    if (attack.isSpecial)
                    {
                        specialCount++;
                        if (IsAttackAllowedByPhaseRequirement(attack, 2))
                        {
                            phase3EligibleSpecialCount++;
                        }
                    }
                }
            }

            row.prefabAttackCount = attackIds.Count;
            row.prefabSpecialAttackCount = specialCount;

            string phase2 = NormalizeText(spawnPoint.phase2TransitionOpenerId);
            string phase3 = NormalizeText(spawnPoint.phase3TransitionOpenerId);
            string phase2Followup = NormalizeText(spawnPoint.phase2TransitionFollowupId);
            string phase3Followup = NormalizeText(spawnPoint.phase3TransitionFollowupId);

            if (spawnPoint.enablePhaseTransitionOpeners)
            {
                if (string.IsNullOrWhiteSpace(phase2))
                {
                    issues.Add(ValidationIssue.Blocking(
                        "Phase transition openers are enabled but phase2TransitionOpenerId is empty."));
                }

                if (string.IsNullOrWhiteSpace(phase3))
                {
                    issues.Add(ValidationIssue.Blocking(
                        "Phase transition openers are enabled but phase3TransitionOpenerId is empty."));
                }

                ValidateOpenerIdExists("phase2TransitionOpenerId", spawnPoint.phase2TransitionOpenerId, attackIds, issues);
                ValidateOpenerIdExists("phase3TransitionOpenerId", spawnPoint.phase3TransitionOpenerId, attackIds, issues);
                ValidateOpenerGrammarForPhase(
                    "phase2TransitionOpenerId",
                    phase2,
                    1,
                    attacksById,
                    issues);
                ValidateOpenerGrammarForPhase(
                    "phase3TransitionOpenerId",
                    phase3,
                    2,
                    attacksById,
                    issues);
            }

            if (spawnPoint.enablePhaseTransitionFollowupChain)
            {
                if (string.IsNullOrWhiteSpace(phase2Followup))
                {
                    issues.Add(ValidationIssue.Blocking(
                        "Phase transition followup chain is enabled but phase2TransitionFollowupId is empty."));
                }

                if (string.IsNullOrWhiteSpace(phase3Followup))
                {
                    issues.Add(ValidationIssue.Blocking(
                        "Phase transition followup chain is enabled but phase3TransitionFollowupId is empty."));
                }

                ValidateOpenerIdExists("phase2TransitionFollowupId", spawnPoint.phase2TransitionFollowupId, attackIds, issues);
                ValidateOpenerIdExists("phase3TransitionFollowupId", spawnPoint.phase3TransitionFollowupId, attackIds, issues);
                ValidateOpenerGrammarForPhase(
                    "phase2TransitionFollowupId",
                    phase2Followup,
                    1,
                    attacksById,
                    issues);
                ValidateOpenerGrammarForPhase(
                    "phase3TransitionFollowupId",
                    phase3Followup,
                    2,
                    attacksById,
                    issues);
            }

            if (!string.IsNullOrWhiteSpace(phase2) &&
                !string.IsNullOrWhiteSpace(phase3) &&
                string.Equals(phase2, phase3, StringComparison.Ordinal))
            {
                issues.Add(ValidationIssue.Blocking("phase2TransitionOpenerId and phase3TransitionOpenerId are identical."));
            }

            if (spawnPoint.enablePhaseTransitionOpenerRetry &&
                spawnPoint.phaseTransitionOpenerMaxRetries > 0 &&
                spawnPoint.phaseTransitionOpenerRetryDelay <= 0f)
            {
                issues.Add(ValidationIssue.Warning(
                    "Phase opener retry is enabled with retries>0 but retry delay<=0; retry spam risk is high."));
            }

            if (spawnPoint.enablePhaseTransitionFollowupRetry &&
                spawnPoint.phaseTransitionFollowupMaxRetries > 0 &&
                spawnPoint.phaseTransitionFollowupRetryDelay <= 0f)
            {
                issues.Add(ValidationIssue.Warning(
                    "Phase followup retry is enabled with retries>0 but retry delay<=0; retry spam risk is high."));
            }

            if (spawnPoint.enablePhase3SpecialPriorityWindow &&
                spawnPoint.phase3SpecialPriorityDuration <= 0f)
            {
                issues.Add(ValidationIssue.Blocking(
                    "Phase3 special priority window is enabled but duration<=0."));
            }

            if (spawnPoint.enablePhase3SpecialPriorityWindow &&
                spawnPoint.phase3SpecialPriorityWeightMultiplier < 1f)
            {
                issues.Add(ValidationIssue.Blocking(
                    "Phase3 special priority weight multiplier is below 1 while priority window is enabled."));
            }

            if (spawnPoint.enablePhase3SpecialPriorityWindow &&
                string.IsNullOrWhiteSpace(phase3))
            {
                issues.Add(ValidationIssue.Blocking(
                    "Phase3 special priority window is enabled but phase3TransitionOpenerId is empty."));
            }

            if (spawnPoint.enablePhase3SpecialPriorityWindow &&
                spawnPoint.forceSpecialQueueDuringPhase3Priority &&
                phase3EligibleSpecialCount == 0)
            {
                issues.Add(ValidationIssue.Blocking(
                    "Force special queue is enabled but boss prefab has no phase3-eligible special attacks."));
            }

            if (spawnPoint.enablePhase3SpecialPriorityWindow &&
                spawnPoint.forceSpecialQueueDuringPhase3Priority &&
                phase3EligibleSpecialCount < 2)
            {
                issues.Add(ValidationIssue.Blocking(
                    "Force special queue in phase3 priority window requires at least 2 phase3-eligible special attacks."));
            }

            if (spawnPoint.enablePhaseTransitionFollowupChain &&
                !spawnPoint.enablePhaseTransitionOpeners)
            {
                issues.Add(ValidationIssue.Blocking(
                    "Phase transition followup chain is enabled while phase openers are disabled."));
            }

            if (!string.IsNullOrWhiteSpace(phase2) &&
                !string.IsNullOrWhiteSpace(phase2Followup) &&
                string.Equals(phase2, phase2Followup, StringComparison.Ordinal))
            {
                issues.Add(ValidationIssue.Blocking("phase2TransitionOpenerId and phase2TransitionFollowupId are identical."));
            }

            if (!string.IsNullOrWhiteSpace(phase3) &&
                !string.IsNullOrWhiteSpace(phase3Followup) &&
                string.Equals(phase3, phase3Followup, StringComparison.Ordinal))
            {
                issues.Add(ValidationIssue.Blocking("phase3TransitionOpenerId and phase3TransitionFollowupId are identical."));
            }

            if (!string.IsNullOrWhiteSpace(phase2Followup) &&
                !string.IsNullOrWhiteSpace(phase3Followup) &&
                string.Equals(phase2Followup, phase3Followup, StringComparison.Ordinal))
            {
                issues.Add(ValidationIssue.Blocking("phase2TransitionFollowupId and phase3TransitionFollowupId are identical."));
            }
        }

        private static void ValidateOpenerIdExists(
            string fieldName,
            string openerId,
            HashSet<string> attackIds,
            List<ValidationIssue> issues)
        {
            string normalized = NormalizeText(openerId);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return;
            }

            if (attackIds == null || !attackIds.Contains(normalized))
            {
                issues.Add(ValidationIssue.Blocking($"{fieldName}='{normalized}' not found in boss prefab attack ids."));
            }
        }

        private static void ValidateOpenerGrammarForPhase(
            string fieldName,
            string openerId,
            int phaseIndex,
            Dictionary<string, BossAttack> attacksById,
            List<ValidationIssue> issues)
        {
            string normalized = NormalizeText(openerId);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return;
            }

            if (attacksById == null || !attacksById.TryGetValue(normalized, out BossAttack attack) || attack == null)
            {
                return;
            }

            if (!attack.isSpecial)
            {
                issues.Add(ValidationIssue.Blocking(
                    $"{fieldName}='{normalized}' points to non-special attack; opener should be a readable special move."));
            }

            if (!IsAttackAllowedByPhaseRequirement(attack, phaseIndex))
            {
                issues.Add(ValidationIssue.Blocking(
                    $"{fieldName}='{normalized}' is not available at phase {phaseIndex + 1}."));
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

        private static GameObject ResolveBossPrefab(BossSpawnPoint spawnPoint)
        {
            if (spawnPoint == null)
            {
                return null;
            }

            if (spawnPoint.bossPrefab != null)
            {
                return spawnPoint.bossPrefab;
            }

            string path = spawnPoint.prototype == BossPrototypeType.Guardian
                ? DefaultGuardianBossPrefabPath
                : DefaultEelBossPrefabPath;
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private static ValidationRow BuildRow(ValidationRow row, List<ValidationIssue> issues)
        {
            int blocking = 0;
            int warnings = 0;
            var notes = new List<string>(issues.Count);
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

            row.status = blocking > 0 ? "Error" : "Ok";
            row.blockingErrors = blocking;
            row.warnings = warnings;
            row.note = notes.Count > 0 ? string.Join(" ", notes) : string.Empty;
            return row;
        }

        private static void AddBoolMismatchIssue(List<ValidationIssue> issues, string fieldName, bool expected, bool actual)
        {
            if (expected != actual)
            {
                issues.Add(ValidationIssue.Blocking(
                    $"{fieldName} mismatch (expected {expected}, actual {actual})."));
            }
        }

        private static void AddStringMismatchIssue(List<ValidationIssue> issues, string fieldName, string expected, string actual)
        {
            string left = NormalizeText(expected);
            string right = NormalizeText(actual);
            if (!string.Equals(left, right, StringComparison.Ordinal))
            {
                issues.Add(ValidationIssue.Blocking(
                    $"{fieldName} mismatch (expected '{left}', actual '{right}')."));
            }
        }

        private static void AddFloatMismatchIssue(List<ValidationIssue> issues, string fieldName, float expected, float actual)
        {
            if (Mathf.Abs(expected - actual) > FloatEpsilon)
            {
                issues.Add(ValidationIssue.Blocking(
                    $"{fieldName} mismatch (expected {expected:0.###}, actual {actual:0.###})."));
            }
        }

        private static void AddIntMismatchIssue(List<ValidationIssue> issues, string fieldName, int expected, int actual)
        {
            if (expected != actual)
            {
                issues.Add(ValidationIssue.Blocking(
                    $"{fieldName} mismatch (expected {expected}, actual {actual})."));
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
                if (levelData == null || !IsFormalLevelId(levelData.levelId))
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

            result.Sort((a, b) =>
            {
                int cmp = a.levelIndex.CompareTo(b.levelIndex);
                if (cmp != 0)
                {
                    return cmp;
                }

                return string.Compare(a.levelAssetPath, b.levelAssetPath, StringComparison.Ordinal);
            });
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

        private static bool IsFormalLevelId(string levelId)
        {
            return !string.IsNullOrWhiteSpace(levelId) &&
                   levelId.StartsWith("LEVEL_", StringComparison.OrdinalIgnoreCase);
        }

        private static int ParseLevelIndex(string levelId)
        {
            if (!IsFormalLevelId(levelId))
            {
                return -1;
            }

            string part = levelId.Substring("LEVEL_".Length);
            return int.TryParse(part, out int parsed) ? parsed : -1;
        }

        private static bool AssetExists(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return false;
            }

            return File.Exists(Path.GetFullPath(assetPath));
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
                "level_id,level_asset,scene_name,scene_path,status,blocking_errors,warnings,strict_warning_whitelisted,runtime_configurator,boss_spawn_point,boss_spawn_points,boss_prefab," +
                "ld_override_boss,ld_override_tuning,ld_openers,ld_phase2_opener,ld_phase3_opener,ld_opener_retry,ld_opener_retry_delay,ld_opener_retry_retries,ld_followup_chain,ld_phase2_followup,ld_phase3_followup,ld_followup_retry,ld_followup_retry_delay,ld_followup_retry_retries,ld_phase3_window,ld_phase3_duration,ld_phase3_weight,ld_force_special," +
                "sp_override_tuning,sp_openers,sp_phase2_opener,sp_phase3_opener,sp_opener_retry,sp_opener_retry_delay,sp_opener_retry_retries,sp_followup_chain,sp_phase2_followup,sp_phase3_followup,sp_followup_retry,sp_followup_retry_delay,sp_followup_retry_retries,sp_phase3_window,sp_phase3_duration,sp_phase3_weight,sp_force_special," +
                "ctrl_openers,ctrl_phase2_opener,ctrl_phase3_opener,ctrl_opener_retry,ctrl_opener_retry_delay,ctrl_opener_retry_retries,ctrl_followup_chain,ctrl_phase2_followup,ctrl_phase3_followup,ctrl_followup_retry,ctrl_followup_retry_delay,ctrl_followup_retry_retries,ctrl_phase3_window,ctrl_phase3_duration,ctrl_phase3_weight,ctrl_force_special," +
                "prefab_attacks,prefab_special_attacks,note");

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
                    .Append(row.strictWarningWhitelisted ? 1 : 0).Append(',')
                    .Append(EscapeCsv(row.runtimeConfiguratorName)).Append(',')
                    .Append(EscapeCsv(row.bossSpawnPointName)).Append(',')
                    .Append(row.bossSpawnPointCount).Append(',')
                    .Append(EscapeCsv(row.bossPrefabPath)).Append(',')
                    .Append(row.levelOverrideBossSettings ? 1 : 0).Append(',')
                    .Append(row.levelOverrideEncounterTuning ? 1 : 0).Append(',')
                    .Append(row.levelEnablePhaseOpeners ? 1 : 0).Append(',')
                    .Append(EscapeCsv(row.levelPhase2OpenerId)).Append(',')
                    .Append(EscapeCsv(row.levelPhase3OpenerId)).Append(',')
                    .Append(row.levelEnablePhaseOpenerRetry ? 1 : 0).Append(',')
                    .Append(row.levelPhaseOpenerRetryDelay.ToString("0.###")).Append(',')
                    .Append(row.levelPhaseOpenerMaxRetries).Append(',')
                    .Append(row.levelEnablePhaseFollowupChain ? 1 : 0).Append(',')
                    .Append(EscapeCsv(row.levelPhase2FollowupId)).Append(',')
                    .Append(EscapeCsv(row.levelPhase3FollowupId)).Append(',')
                    .Append(row.levelEnablePhaseFollowupRetry ? 1 : 0).Append(',')
                    .Append(row.levelPhaseFollowupRetryDelay.ToString("0.###")).Append(',')
                    .Append(row.levelPhaseFollowupMaxRetries).Append(',')
                    .Append(row.levelEnablePhase3PriorityWindow ? 1 : 0).Append(',')
                    .Append(row.levelPhase3PriorityDuration.ToString("0.###")).Append(',')
                    .Append(row.levelPhase3PriorityWeight.ToString("0.###")).Append(',')
                    .Append(row.levelForceSpecialQueueInPhase3Priority ? 1 : 0).Append(',')
                    .Append(row.spawnOverrideEncounterTuning ? 1 : 0).Append(',')
                    .Append(row.spawnEnablePhaseOpeners ? 1 : 0).Append(',')
                    .Append(EscapeCsv(row.spawnPhase2OpenerId)).Append(',')
                    .Append(EscapeCsv(row.spawnPhase3OpenerId)).Append(',')
                    .Append(row.spawnEnablePhaseOpenerRetry ? 1 : 0).Append(',')
                    .Append(row.spawnPhaseOpenerRetryDelay.ToString("0.###")).Append(',')
                    .Append(row.spawnPhaseOpenerMaxRetries).Append(',')
                    .Append(row.spawnEnablePhaseFollowupChain ? 1 : 0).Append(',')
                    .Append(EscapeCsv(row.spawnPhase2FollowupId)).Append(',')
                    .Append(EscapeCsv(row.spawnPhase3FollowupId)).Append(',')
                    .Append(row.spawnEnablePhaseFollowupRetry ? 1 : 0).Append(',')
                    .Append(row.spawnPhaseFollowupRetryDelay.ToString("0.###")).Append(',')
                    .Append(row.spawnPhaseFollowupMaxRetries).Append(',')
                    .Append(row.spawnEnablePhase3PriorityWindow ? 1 : 0).Append(',')
                    .Append(row.spawnPhase3PriorityDuration.ToString("0.###")).Append(',')
                    .Append(row.spawnPhase3PriorityWeight.ToString("0.###")).Append(',')
                    .Append(row.spawnForceSpecialQueueInPhase3Priority ? 1 : 0).Append(',')
                    .Append(row.controllerEnablePhaseOpeners ? 1 : 0).Append(',')
                    .Append(EscapeCsv(row.controllerPhase2OpenerId)).Append(',')
                    .Append(EscapeCsv(row.controllerPhase3OpenerId)).Append(',')
                    .Append(row.controllerEnablePhaseOpenerRetry ? 1 : 0).Append(',')
                    .Append(row.controllerPhaseOpenerRetryDelay.ToString("0.###")).Append(',')
                    .Append(row.controllerPhaseOpenerMaxRetries).Append(',')
                    .Append(row.controllerEnablePhaseFollowupChain ? 1 : 0).Append(',')
                    .Append(EscapeCsv(row.controllerPhase2FollowupId)).Append(',')
                    .Append(EscapeCsv(row.controllerPhase3FollowupId)).Append(',')
                    .Append(row.controllerEnablePhaseFollowupRetry ? 1 : 0).Append(',')
                    .Append(row.controllerPhaseFollowupRetryDelay.ToString("0.###")).Append(',')
                    .Append(row.controllerPhaseFollowupMaxRetries).Append(',')
                    .Append(row.controllerEnablePhase3PriorityWindow ? 1 : 0).Append(',')
                    .Append(row.controllerPhase3PriorityDuration.ToString("0.###")).Append(',')
                    .Append(row.controllerPhase3PriorityWeight.ToString("0.###")).Append(',')
                    .Append(row.controllerForceSpecialQueueInPhase3Priority ? 1 : 0).Append(',')
                    .Append(row.prefabAttackCount).Append(',')
                    .Append(row.prefabSpecialAttackCount).Append(',')
                    .Append(EscapeCsv(row.note))
                    .AppendLine();
            }

            File.WriteAllText(fullPath, csv.ToString(), new UTF8Encoding(false));
            return ReportCsvPath;
        }

        private static string WriteSummary(
            List<ValidationRow> rows,
            int blockingTotal,
            int warningTotal,
            bool strictWarningGate,
            int strictWhitelistEntries,
            int strictWhitelistedRows)
        {
            string fullPath = Path.GetFullPath(SummaryMdPath);
            EnsureDirectoryExists(fullPath);

            int failed = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                if (string.Equals(rows[i].status, "Error", StringComparison.Ordinal))
                {
                    failed++;
                }
            }

            var md = new StringBuilder();
            md.AppendLine("# Boss Choreography Coverage Summary");
            md.AppendLine();
            md.AppendLine($"- Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss zzz}");
            md.AppendLine($"- Targets: {rows.Count}");
            md.AppendLine($"- Failed Rows: {failed}");
            md.AppendLine($"- Blocking Errors: {blockingTotal}");
            md.AppendLine($"- Warnings: {warningTotal}");
            md.AppendLine($"- Strict Warning Gate: {(strictWarningGate ? "On" : "Off")}");
            md.AppendLine($"- Strict Whitelist Entries: {strictWhitelistEntries}");
            md.AppendLine($"- Strict Whitelisted Rows: {strictWhitelistedRows}");
            md.AppendLine($"- Strict Whitelist CSV: {StrictWarningWhitelistCsvPath}");
            md.AppendLine($"- CSV: {ReportCsvPath}");
            md.AppendLine();
            md.AppendLine("| Level | Scene | Status | Blocking | Warnings | StrictWhitelisted | SpawnPoint | PrefabAttackCount | SpecialAttackCount | Note |");
            md.AppendLine("|---|---|---|---:|---:|---|---|---:|---:|---|");

            for (int i = 0; i < rows.Count; i++)
            {
                ValidationRow row = rows[i];
                md.Append("| ")
                    .Append(SafeMarkdownCell(row.levelId)).Append(" | ")
                    .Append(SafeMarkdownCell(row.sceneName)).Append(" | ")
                    .Append(SafeMarkdownCell(row.status)).Append(" | ")
                    .Append(row.blockingErrors).Append(" | ")
                    .Append(row.warnings).Append(" | ")
                    .Append(row.strictWarningWhitelisted ? "Yes" : "No").Append(" | ")
                    .Append(SafeMarkdownCell(row.bossSpawnPointName)).Append(" | ")
                    .Append(row.prefabAttackCount).Append(" | ")
                    .Append(row.prefabSpecialAttackCount).Append(" | ")
                    .Append(SafeMarkdownCell(TrimForMarkdownTable(row.note, 180))).Append(" |")
                    .AppendLine();
            }

            File.WriteAllText(fullPath, md.ToString(), new UTF8Encoding(false));
            return SummaryMdPath;
        }

        private static string NormalizeText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static void EnsureDirectoryExists(string filePath)
        {
            string dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        private static string EscapeCsv(string value)
        {
            string v = value ?? string.Empty;
            if (v.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0)
            {
                return v;
            }

            return "\"" + v.Replace("\"", "\"\"") + "\"";
        }

        private static string TrimForMarkdownTable(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return value ?? string.Empty;
            }

            return value.Substring(0, maxLength) + "...";
        }

        private static string SafeMarkdownCell(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
        }
    }
}
