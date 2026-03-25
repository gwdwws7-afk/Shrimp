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
    public static class BossEncounterProfileCoverageValidator
    {
        private const string ValidateMenuPath = "Tools/Boss/P3/Validate Encounter Profile Coverage (CSV)";
        private const string ValidateGateMenuPath = "Tools/Boss/P3/Validate Encounter Profile Coverage (CI Gate)";
        private const string FixMenuPath = "Tools/Boss/P3/Fix Encounter Profile Coverage";

        private const string SceneFolderPath = "Assets/Scenes";
        private const string ReportCsvPath = "Assets/ThirdPersonController/Reports/boss_encounter_profile_coverage_report.csv";
        private const string SummaryMdPath = "Assets/ThirdPersonController/Reports/boss_encounter_profile_coverage_summary.md";
        private const string LogPrefix = "[BossEncounterProfileCoverage]";

        private const string DefaultProfileFolderPath = "Assets/ThirdPersonController/ScriptableObjects/Boss";
        private const string DefaultEelProfilePath = DefaultProfileFolderPath + "/BossEncounterProfile_Eel_Default.asset";
        private const string DefaultGuardianProfilePath = DefaultProfileFolderPath + "/BossEncounterProfile_Guardian_Default.asset";

        private const int MinLevelIndex = 1;
        private const int MaxLevelIndex = 10;

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
            public int expectBossGate;
            public int blockingErrors;
            public int gaps;
            public int warnings;
            public int fixedCount;
            public int bossSpawnPointCount;
            public int profileAssignedCount;
            public int profileMissingCount;
            public int applyProfileEnabledCount;
            public int missingPrefabCount;
            public string profileAssets;
            public string note;
        }

        private struct DefaultProfiles
        {
            public BossEncounterProfile eel;
            public BossEncounterProfile guardian;
            public int createdCount;
            public List<string> setupNotes;
        }

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
                string noneMessage = $"{LogPrefix} no formal LevelData assets found for LEVEL_{MinLevelIndex:D2}~LEVEL_{MaxLevelIndex:D2}.";
                Debug.LogWarning(noneMessage);
                if (interactive)
                {
                    EditorUtility.DisplayDialog("Boss Encounter Profile Coverage", noneMessage, "OK");
                }

                return;
            }

            DefaultProfiles defaults = ResolveDefaultProfiles(applyFix);

            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
            var rows = new List<ValidationRow>(entries.Count);
            try
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    rows.Add(ValidateEntry(entries[i], applyFix, defaults));
                }
            }
            finally
            {
                if (originalSetup != null && originalSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
                }
            }

            int blockingTotal = 0;
            int gapTotal = 0;
            int warningTotal = 0;
            int fixedTotal = defaults.createdCount;
            int errorScenes = 0;
            int gapScenes = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                ValidationRow row = rows[i];
                blockingTotal += row.blockingErrors;
                gapTotal += row.gaps;
                warningTotal += row.warnings;
                fixedTotal += row.fixedCount;
                if (string.Equals(row.status, "Error", StringComparison.Ordinal))
                {
                    errorScenes++;
                }
                else if (string.Equals(row.status, "Gap", StringComparison.Ordinal) ||
                         string.Equals(row.status, "Partial", StringComparison.Ordinal))
                {
                    gapScenes++;
                }
            }

            if (applyFix)
            {
                AssetDatabase.SaveAssets();
            }

            string csvPath = WriteCsv(rows);
            string summaryPath = WriteSummary(rows, blockingTotal, gapTotal, warningTotal, fixedTotal, defaults.setupNotes);
            AssetDatabase.Refresh();

            string summary =
                $"mode={(applyFix ? "fix" : "validate")} targets={rows.Count} errorScenes={errorScenes} gapScenes={gapScenes} " +
                $"blocking={blockingTotal} gaps={gapTotal} warnings={warningTotal} fixed={fixedTotal} " +
                $"csv={csvPath} summary={summaryPath}";
            Debug.Log($"{LogPrefix} complete | {summary}");

            if (interactive)
            {
                EditorUtility.DisplayDialog("Boss Encounter Profile Coverage", summary, "OK");
            }

            if (failOnGate && (blockingTotal > 0 || gapTotal > 0))
            {
                throw new InvalidOperationException($"{LogPrefix} gate failed. blocking={blockingTotal} gaps={gapTotal} csv={csvPath}");
            }
        }

        private static ValidationRow ValidateEntry(LevelEntry entry, bool applyFix, DefaultProfiles defaults)
        {
            var blockingNotes = new List<string>();
            var gapNotes = new List<string>();
            var warningNotes = new List<string>();
            var fixNotes = new List<string>();
            var profileAssets = new HashSet<string>(StringComparer.Ordinal);
            var validatedProfiles = new HashSet<BossEncounterProfile>();

            string scenePath = BuildScenePath(entry.levelData);
            var row = new ValidationRow
            {
                levelId = entry.levelData != null ? entry.levelData.levelId : string.Empty,
                levelAssetPath = entry.levelAssetPath ?? string.Empty,
                sceneName = entry.levelData != null ? entry.levelData.sceneName : string.Empty,
                scenePath = scenePath ?? string.Empty,
                expectBossGate = entry.levelData != null && entry.levelData.overrideBossSettings ? 1 : 0
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

            bool expectBossGate = entry.levelData.overrideBossSettings;
            List<BossSpawnPoint> spawnPoints = FindComponentsInScene<BossSpawnPoint>(scene);
            row.bossSpawnPointCount = spawnPoints.Count;

            if (expectBossGate && spawnPoints.Count == 0)
            {
                blockingNotes.Add("LevelData.overrideBossSettings is true but no BossSpawnPoint exists in scene.");
            }

            bool sceneDirty = false;
            for (int i = 0; i < spawnPoints.Count; i++)
            {
                BossSpawnPoint spawnPoint = spawnPoints[i];
                if (spawnPoint == null)
                {
                    continue;
                }

                string spawnLabel = string.IsNullOrWhiteSpace(spawnPoint.name) ? $"BossSpawnPoint[{i}]" : spawnPoint.name;

                if (spawnPoint.bossPrefab == null)
                {
                    row.missingPrefabCount++;
                    warningNotes.Add($"{spawnLabel}: bossPrefab is null.");
                }

                if (expectBossGate && spawnPoint.applyEncounterProfile)
                {
                    if (applyFix)
                    {
                        spawnPoint.applyEncounterProfile = false;
                        sceneDirty = true;
                        row.fixedCount++;
                        fixNotes.Add($"{spawnLabel}: applyEncounterProfile set false (preserve LevelData-tuned boss values).");
                    }
                    else
                    {
                        warningNotes.Add($"{spawnLabel}: applyEncounterProfile=true may override LevelData boss tuning at spawn.");
                    }
                }

                if (spawnPoint.encounterProfile == null && expectBossGate)
                {
                    if (applyFix)
                    {
                        BossEncounterProfile fallback = ResolveDefaultProfileForSpawnPoint(spawnPoint, defaults);
                        if (fallback != null)
                        {
                            spawnPoint.encounterProfile = fallback;
                            sceneDirty = true;
                            row.fixedCount++;
                            fixNotes.Add(
                                $"{spawnLabel}: assigned encounterProfile -> {NormalizeAssetPath(AssetDatabase.GetAssetPath(fallback))}.");
                        }
                        else
                        {
                            gapNotes.Add($"{spawnLabel}: missing encounterProfile and no default profile asset available.");
                        }
                    }
                    else
                    {
                        gapNotes.Add($"{spawnLabel}: encounterProfile is null.");
                    }
                }

                if (spawnPoint.encounterProfile != null)
                {
                    row.profileAssignedCount++;
                    string profileAssetPath = NormalizeAssetPath(AssetDatabase.GetAssetPath(spawnPoint.encounterProfile));
                    if (string.IsNullOrWhiteSpace(profileAssetPath))
                    {
                        profileAssetPath = spawnPoint.encounterProfile.name;
                    }

                    profileAssets.Add(profileAssetPath);

                    if (validatedProfiles.Add(spawnPoint.encounterProfile))
                    {
                        ValidateProfileIdentityCard(
                            spawnPoint.encounterProfile,
                            spawnPoint.prototype,
                            applyFix,
                            gapNotes,
                            fixNotes,
                            ref row.fixedCount);
                    }
                }
                else if (expectBossGate)
                {
                    row.profileMissingCount++;
                }

                if (spawnPoint.applyEncounterProfile)
                {
                    row.applyProfileEnabledCount++;
                }
            }

            if (applyFix && sceneDirty)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                if (EditorSceneManager.SaveScene(scene))
                {
                    row.fixedCount++;
                }
                else
                {
                    blockingNotes.Add("SaveScene returned false after applying fixes.");
                }
            }

            row.profileAssets = profileAssets.Count > 0 ? string.Join(";", profileAssets) : string.Empty;
            row.blockingErrors = blockingNotes.Count;
            row.gaps = gapNotes.Count;
            row.warnings = warningNotes.Count;

            if (row.blockingErrors > 0)
            {
                row.status = "Error";
            }
            else if (row.gaps > 0)
            {
                row.status = row.fixedCount > 0 ? "Partial" : "Gap";
            }
            else
            {
                row.status = row.fixedCount > 0 ? "Fixed" : "Ok";
            }

            var noteParts = new List<string>();
            if (blockingNotes.Count > 0)
            {
                noteParts.Add("[B] " + string.Join(" [B] ", blockingNotes));
            }

            if (gapNotes.Count > 0)
            {
                noteParts.Add("[G] " + string.Join(" [G] ", gapNotes));
            }

            if (warningNotes.Count > 0)
            {
                noteParts.Add("[W] " + string.Join(" [W] ", warningNotes));
            }

            if (fixNotes.Count > 0)
            {
                noteParts.Add("[F] " + string.Join(" [F] ", fixNotes));
            }

            row.note = noteParts.Count > 0 ? string.Join(" ", noteParts) : string.Empty;
            return row;
        }

        private static DefaultProfiles ResolveDefaultProfiles(bool applyFix)
        {
            var result = new DefaultProfiles
            {
                eel = AssetDatabase.LoadAssetAtPath<BossEncounterProfile>(DefaultEelProfilePath),
                guardian = AssetDatabase.LoadAssetAtPath<BossEncounterProfile>(DefaultGuardianProfilePath),
                createdCount = 0,
                setupNotes = new List<string>()
            };

            if (!applyFix)
            {
                if (result.eel == null)
                {
                    result.setupNotes.Add($"Default profile missing: {DefaultEelProfilePath}");
                }

                if (result.guardian == null)
                {
                    result.setupNotes.Add($"Default profile missing: {DefaultGuardianProfilePath}");
                }

                return result;
            }

            EnsureFolderExists(DefaultProfileFolderPath);

            if (result.eel == null)
            {
                result.eel = CreateDefaultProfileAsset(DefaultEelProfilePath, BossPrototypeType.Eel, out string note);
                if (result.eel != null)
                {
                    result.createdCount++;
                }

                if (!string.IsNullOrWhiteSpace(note))
                {
                    result.setupNotes.Add(note);
                }
            }

            if (result.guardian == null)
            {
                result.guardian = CreateDefaultProfileAsset(DefaultGuardianProfilePath, BossPrototypeType.Guardian, out string note);
                if (result.guardian != null)
                {
                    result.createdCount++;
                }

                if (!string.IsNullOrWhiteSpace(note))
                {
                    result.setupNotes.Add(note);
                }
            }

            if (result.createdCount > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            if (result.eel == null)
            {
                result.eel = AssetDatabase.LoadAssetAtPath<BossEncounterProfile>(DefaultEelProfilePath);
            }

            if (result.guardian == null)
            {
                result.guardian = AssetDatabase.LoadAssetAtPath<BossEncounterProfile>(DefaultGuardianProfilePath);
            }

            if (result.eel == null)
            {
                result.setupNotes.Add($"Failed to provision default profile: {DefaultEelProfilePath}");
            }

            if (result.guardian == null)
            {
                result.setupNotes.Add($"Failed to provision default profile: {DefaultGuardianProfilePath}");
            }

            return result;
        }

        private static BossEncounterProfile CreateDefaultProfileAsset(
            string assetPath,
            BossPrototypeType prototype,
            out string note)
        {
            note = string.Empty;
            try
            {
                BossEncounterProfile profile = ScriptableObject.CreateInstance<BossEncounterProfile>();
                ConfigureDefaultProfile(profile, prototype);
                AssetDatabase.CreateAsset(profile, assetPath);
                EditorUtility.SetDirty(profile);
                note = $"Created default profile: {assetPath}";
                return profile;
            }
            catch (Exception ex)
            {
                note = $"Create profile failed: {assetPath} | {ex.Message}";
                return null;
            }
        }

        private static void ConfigureDefaultProfile(BossEncounterProfile profile, BossPrototypeType prototype)
        {
            if (profile == null)
            {
                return;
            }

            bool guardian = prototype == BossPrototypeType.Guardian;
            profile.bossDisplayName = guardian ? "Guardian Boss" : "Eel Boss";
            profile.bossIdentityId = guardian ? "guardian_default" : "eel_default";
            profile.roleFantasy = guardian
                ? "Pressure front-line space with heavy denial swings and punish late dodges."
                : "Control space with mobility pressure and force reactive repositioning.";
            profile.counterPlayHint = guardian
                ? "Bait heavy shield swings, then punish recovery before special chain."
                : "Dodge chain-rush endpoint and retaliate during punish window.";
            profile.failLearningHint = guardian
                ? "Failure usually comes from over-committing into armored openers."
                : "Failure usually comes from missing chain-rush direction read.";
            profile.expectedPhaseCount = 3;
            profile.expectedSkillCount = 4;
            profile.definesBreakRule = true;
            profile.overrideSpawnStats = false;
            profile.maxHealth = guardian ? 4200 : 3800;
            profile.expReward = guardian ? 520 : 480;
            profile.baseDamage = guardian ? 36 : 32;
            profile.knockback = guardian ? 7.5f : 6.8f;

            profile.overrideEncounterTuning = false;
            profile.phase2HealthThreshold = guardian ? 0.64f : 0.66f;
            profile.phase3HealthThreshold = guardian ? 0.31f : 0.33f;
            profile.breakWindowDuration = guardian ? 3.8f : 4.2f;
            profile.breakWindowCooldown = guardian ? 11f : 12f;
            profile.breakWindowDamageMultiplier = guardian ? 1.7f : 1.6f;
            profile.staggerMax = guardian ? 145f : 128f;
            profile.staggerPerDamage = guardian ? 0.95f : 1.0f;
            profile.attackInterval = guardian ? 2.9f : 3.15f;
            profile.decisionInterval = guardian ? 0.7f : 0.78f;
            profile.queuedAttackLimit = guardian ? 4 : 3;
            profile.immediateRepeatPenalty = guardian ? 0.4f : 0.34f;
            profile.enablePostBreakPunishWindow = true;
            profile.postBreakPunishDuration = guardian ? 5.8f : 5.2f;
            profile.postBreakAttackIntervalMultiplier = guardian ? 0.72f : 0.75f;
            profile.postBreakDecisionIntervalMultiplier = guardian ? 0.76f : 0.82f;
            profile.postBreakChaseSpeedMultiplier = guardian ? 1.18f : 1.15f;
            profile.enablePhaseComboChain = true;
            profile.phase2ComboChance = guardian ? 0.5f : 0.45f;
            profile.phase3ComboChance = guardian ? 0.7f : 0.65f;
            profile.comboStartDelay = guardian ? 0.06f : 0.08f;
            profile.comboRepeatPenalty = guardian ? 0.38f : 0.35f;
            profile.enableInterruptRecoveryGate = true;
            profile.interruptRecoveryDuration = guardian ? 0.18f : 0.2f;
            profile.interruptedAttackCooldownScale = guardian ? 0.42f : 0.45f;
            profile.enableTimePressure = true;
            profile.timePressureDelay = guardian ? 70f : 75f;
            profile.timePressureRampDuration = guardian ? 55f : 60f;
            profile.maxTimePressureDamageMultiplier = guardian ? 1.4f : 1.35f;
            profile.maxTimePressureSpeedMultiplier = guardian ? 1.23f : 1.2f;
            profile.enablePhaseTransitionOpeners = true;
            profile.phase2TransitionOpenerId = guardian ? "guard_spray" : "eel_vortex";
            profile.phase3TransitionOpenerId = guardian ? "guard_blade" : "eel_devour";
            profile.enablePhaseTransitionOpenerRetry = true;
            profile.phaseTransitionOpenerRetryDelay = 0.12f;
            profile.phaseTransitionOpenerMaxRetries = 3;
            profile.enablePhaseTransitionFollowupChain = true;
            profile.phase2TransitionFollowupId = guardian ? "guard_overload" : "eel_charge";
            profile.phase3TransitionFollowupId = guardian ? "guard_spray" : "eel_vortex";
            profile.enablePhaseTransitionFollowupRetry = true;
            profile.phaseTransitionFollowupRetryDelay = 0.12f;
            profile.phaseTransitionFollowupMaxRetries = 2;
            profile.enablePhase3SpecialPriorityWindow = true;
            profile.phase3SpecialPriorityDuration = guardian ? 6.5f : 6f;
            profile.phase3SpecialPriorityWeightMultiplier = guardian ? 1.8f : 1.7f;
            profile.forceSpecialQueueDuringPhase3Priority = true;
        }

        private static void ValidateProfileIdentityCard(
            BossEncounterProfile profile,
            BossPrototypeType prototype,
            bool applyFix,
            List<string> gapNotes,
            List<string> fixNotes,
            ref int fixedCount)
        {
            if (profile == null)
            {
                return;
            }

            bool guardian = prototype == BossPrototypeType.Guardian;
            string defaultDisplayName = guardian ? "Guardian Boss" : "Eel Boss";
            string defaultIdentityId = guardian ? "guardian_identity" : "eel_identity";
            string defaultRole = guardian
                ? "Anchor the arena with heavy denial and punish mistimed aggression."
                : "Maintain mobility pressure and force directional defensive reads.";
            string defaultCounterHint = guardian
                ? "Counter by baiting armored openers and punishing long recoveries."
                : "Counter by dodging chain-rush endpoints and retaliating in punish windows.";
            string defaultFailHint = guardian
                ? "Typical failure: over-commit into armored strike sequence."
                : "Typical failure: late dodge on chain-rush follow-up.";

            bool changed = false;
            string profileLabel = profile.name;

            if (string.IsNullOrWhiteSpace(profile.bossDisplayName))
            {
                if (applyFix)
                {
                    profile.bossDisplayName = defaultDisplayName;
                    changed = true;
                    fixedCount++;
                    fixNotes.Add($"{profileLabel}: bossDisplayName auto-filled.");
                }
                else
                {
                    gapNotes.Add($"{profileLabel}: bossDisplayName is empty.");
                }
            }

            if (string.IsNullOrWhiteSpace(profile.bossIdentityId))
            {
                if (applyFix)
                {
                    profile.bossIdentityId = defaultIdentityId;
                    changed = true;
                    fixedCount++;
                    fixNotes.Add($"{profileLabel}: bossIdentityId auto-filled.");
                }
                else
                {
                    gapNotes.Add($"{profileLabel}: bossIdentityId is empty.");
                }
            }

            if (string.IsNullOrWhiteSpace(profile.roleFantasy))
            {
                if (applyFix)
                {
                    profile.roleFantasy = defaultRole;
                    changed = true;
                    fixedCount++;
                    fixNotes.Add($"{profileLabel}: roleFantasy auto-filled.");
                }
                else
                {
                    gapNotes.Add($"{profileLabel}: roleFantasy is empty.");
                }
            }

            if (string.IsNullOrWhiteSpace(profile.counterPlayHint))
            {
                if (applyFix)
                {
                    profile.counterPlayHint = defaultCounterHint;
                    changed = true;
                    fixedCount++;
                    fixNotes.Add($"{profileLabel}: counterPlayHint auto-filled.");
                }
                else
                {
                    gapNotes.Add($"{profileLabel}: counterPlayHint is empty.");
                }
            }

            if (string.IsNullOrWhiteSpace(profile.failLearningHint))
            {
                if (applyFix)
                {
                    profile.failLearningHint = defaultFailHint;
                    changed = true;
                    fixedCount++;
                    fixNotes.Add($"{profileLabel}: failLearningHint auto-filled.");
                }
                else
                {
                    gapNotes.Add($"{profileLabel}: failLearningHint is empty.");
                }
            }

            if (profile.expectedPhaseCount < 2)
            {
                if (applyFix)
                {
                    profile.expectedPhaseCount = 2;
                    changed = true;
                    fixedCount++;
                    fixNotes.Add($"{profileLabel}: expectedPhaseCount clamped to 2.");
                }
                else
                {
                    gapNotes.Add($"{profileLabel}: expectedPhaseCount < 2.");
                }
            }

            if (profile.expectedSkillCount < 3)
            {
                if (applyFix)
                {
                    profile.expectedSkillCount = 3;
                    changed = true;
                    fixedCount++;
                    fixNotes.Add($"{profileLabel}: expectedSkillCount clamped to 3.");
                }
                else
                {
                    gapNotes.Add($"{profileLabel}: expectedSkillCount < 3.");
                }
            }

            if (!profile.definesBreakRule)
            {
                if (applyFix)
                {
                    profile.definesBreakRule = true;
                    changed = true;
                    fixedCount++;
                    fixNotes.Add($"{profileLabel}: definesBreakRule set true.");
                }
                else
                {
                    gapNotes.Add($"{profileLabel}: definesBreakRule is false.");
                }
            }

            if (applyFix && changed)
            {
                EditorUtility.SetDirty(profile);
            }
        }

        private static BossEncounterProfile ResolveDefaultProfileForSpawnPoint(
            BossSpawnPoint spawnPoint,
            DefaultProfiles defaults)
        {
            BossEncounterProfile eel = defaults.eel;
            if (eel == null)
            {
                eel = AssetDatabase.LoadAssetAtPath<BossEncounterProfile>(DefaultEelProfilePath);
            }

            BossEncounterProfile guardian = defaults.guardian;
            if (guardian == null)
            {
                guardian = AssetDatabase.LoadAssetAtPath<BossEncounterProfile>(DefaultGuardianProfilePath);
            }

            if (spawnPoint != null && spawnPoint.prototype == BossPrototypeType.Guardian)
            {
                return guardian != null ? guardian : eel;
            }

            return eel != null ? eel : guardian;
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

                string fileName = Path.GetFileNameWithoutExtension(assetPath);
                if (string.IsNullOrWhiteSpace(fileName) ||
                    !fileName.StartsWith("LevelData_Level", StringComparison.OrdinalIgnoreCase))
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

        private static string WriteCsv(List<ValidationRow> rows)
        {
            string fullPath = Path.GetFullPath(ReportCsvPath);
            EnsureFileDirectoryExists(fullPath);

            var csv = new StringBuilder();
            csv.AppendLine("level_id,level_asset,scene_name,scene_path,status,boss_gate,blocking_errors,gaps,warnings,fixed,boss_spawn_points,profiles_assigned,profiles_missing,apply_profile_enabled,missing_prefab,profile_assets,note");
            for (int i = 0; i < rows.Count; i++)
            {
                ValidationRow row = rows[i];
                csv.Append(EscapeCsv(row.levelId)).Append(',')
                    .Append(EscapeCsv(row.levelAssetPath)).Append(',')
                    .Append(EscapeCsv(row.sceneName)).Append(',')
                    .Append(EscapeCsv(row.scenePath)).Append(',')
                    .Append(EscapeCsv(row.status)).Append(',')
                    .Append(row.expectBossGate).Append(',')
                    .Append(row.blockingErrors).Append(',')
                    .Append(row.gaps).Append(',')
                    .Append(row.warnings).Append(',')
                    .Append(row.fixedCount).Append(',')
                    .Append(row.bossSpawnPointCount).Append(',')
                    .Append(row.profileAssignedCount).Append(',')
                    .Append(row.profileMissingCount).Append(',')
                    .Append(row.applyProfileEnabledCount).Append(',')
                    .Append(row.missingPrefabCount).Append(',')
                    .Append(EscapeCsv(row.profileAssets)).Append(',')
                    .Append(EscapeCsv(row.note))
                    .AppendLine();
            }

            File.WriteAllText(fullPath, csv.ToString(), new UTF8Encoding(false));
            return ReportCsvPath;
        }

        private static string WriteSummary(
            List<ValidationRow> rows,
            int blockingTotal,
            int gapTotal,
            int warningTotal,
            int fixedTotal,
            List<string> setupNotes)
        {
            string fullPath = Path.GetFullPath(SummaryMdPath);
            EnsureFileDirectoryExists(fullPath);

            int errorScenes = 0;
            int gapScenes = 0;
            int fixedScenes = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                ValidationRow row = rows[i];
                if (string.Equals(row.status, "Error", StringComparison.Ordinal))
                {
                    errorScenes++;
                }
                else if (string.Equals(row.status, "Gap", StringComparison.Ordinal) ||
                         string.Equals(row.status, "Partial", StringComparison.Ordinal))
                {
                    gapScenes++;
                }
                else if (string.Equals(row.status, "Fixed", StringComparison.Ordinal))
                {
                    fixedScenes++;
                }
            }

            var md = new StringBuilder();
            md.AppendLine("# Boss Encounter Profile Coverage Summary");
            md.AppendLine();
            md.AppendLine($"- Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss zzz}");
            md.AppendLine($"- Targets: {rows.Count}");
            md.AppendLine($"- Error Scenes: {errorScenes}");
            md.AppendLine($"- Gap Scenes: {gapScenes}");
            md.AppendLine($"- Fixed Scenes: {fixedScenes}");
            md.AppendLine($"- Blocking Errors: {blockingTotal}");
            md.AppendLine($"- Gaps: {gapTotal}");
            md.AppendLine($"- Warnings: {warningTotal}");
            md.AppendLine($"- Fixed Count: {fixedTotal}");
            md.AppendLine($"- CSV: {ReportCsvPath}");
            if (setupNotes != null && setupNotes.Count > 0)
            {
                md.AppendLine();
                md.AppendLine("- Setup Notes:");
                for (int i = 0; i < setupNotes.Count; i++)
                {
                    md.AppendLine($"  - {setupNotes[i]}");
                }
            }

            md.AppendLine();
            md.AppendLine("| Level | Scene | Status | BossGate | Blocking | Gaps | Warnings | Fixed | SpawnPoints | MissingProfiles | ApplyEnabled | Note |");
            md.AppendLine("|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---|");
            for (int i = 0; i < rows.Count; i++)
            {
                ValidationRow row = rows[i];
                md.Append("| ")
                    .Append(SafeMarkdownCell(row.levelId)).Append(" | ")
                    .Append(SafeMarkdownCell(row.sceneName)).Append(" | ")
                    .Append(SafeMarkdownCell(row.status)).Append(" | ")
                    .Append(row.expectBossGate).Append(" | ")
                    .Append(row.blockingErrors).Append(" | ")
                    .Append(row.gaps).Append(" | ")
                    .Append(row.warnings).Append(" | ")
                    .Append(row.fixedCount).Append(" | ")
                    .Append(row.bossSpawnPointCount).Append(" | ")
                    .Append(row.profileMissingCount).Append(" | ")
                    .Append(row.applyProfileEnabledCount).Append(" | ")
                    .Append(SafeMarkdownCell(TrimForMarkdownTable(row.note, 180))).Append(" |")
                    .AppendLine();
            }

            File.WriteAllText(fullPath, md.ToString(), new UTF8Encoding(false));
            return SummaryMdPath;
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

        private static string BuildScenePath(LevelData levelData)
        {
            if (levelData == null || string.IsNullOrWhiteSpace(levelData.sceneName))
            {
                return string.Empty;
            }

            return $"{SceneFolderPath}/{levelData.sceneName.Trim()}.unity";
        }

        private static bool AssetExists(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return false;
            }

            return File.Exists(Path.GetFullPath(assetPath));
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

        private static void EnsureFolderExists(string folderAssetPath)
        {
            if (string.IsNullOrWhiteSpace(folderAssetPath) || AssetDatabase.IsValidFolder(folderAssetPath))
            {
                return;
            }

            string normalized = NormalizeAssetPath(folderAssetPath);
            string[] parts = normalized.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return;
            }

            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static void EnsureFileDirectoryExists(string filePath)
        {
            string dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        private static string NormalizeAssetPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            return path.Replace('\\', '/');
        }

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            bool mustQuote = value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0;
            if (!mustQuote)
            {
                return value;
            }

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static string TrimForMarkdownTable(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || maxLength <= 0 || value.Length <= maxLength)
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
