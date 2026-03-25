using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ThirdPersonController.Tests
{
    public class BossPhaseGrammarRegressionTests
    {
        private static readonly string[] BossScenes =
        {
            "Level_08_MoltenRift",
            "Level_09_StillTideSanctum",
            "Level_10_HiveCore"
        };

        private static readonly MethodInfo ApplyEncounterTuningMethod =
            typeof(BossSpawnPoint).GetMethod(
                "ApplyEncounterTuning",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(BossController) },
                null);

        private readonly List<UnityEngine.Object> createdObjects = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                UnityEngine.Object obj = createdObjects[i];
                if (obj != null)
                {
                    UnityEngine.Object.DestroyImmediate(obj);
                }
            }

            createdObjects.Clear();
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator BossScenes_PhaseGrammar_IsRuntimeValid()
        {
            Scene baselineScene = SceneManager.GetActiveScene();
            var errors = new List<string>();

            for (int i = 0; i < BossScenes.Length; i++)
            {
                string sceneName = BossScenes[i];
                AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                if (load == null)
                {
                    errors.Add($"[{sceneName}] LoadSceneAsync returned null.");
                    continue;
                }

                while (!load.isDone)
                {
                    yield return null;
                }

                Scene scene = SceneManager.GetSceneByName(sceneName);
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    errors.Add($"[{sceneName}] Scene not loaded.");
                    continue;
                }

                SceneManager.SetActiveScene(scene);
                yield return null;
                yield return null;

                LevelFlowController levelFlow = FindComponentInScene<LevelFlowController>(scene);
                BossSpawnPoint bossSpawnPoint = FindComponentInScene<BossSpawnPoint>(scene);

                if (levelFlow == null || levelFlow.levelData == null)
                {
                    errors.Add($"[{sceneName}] Missing LevelFlowController/LevelData.");
                }
                else
                {
                    ValidateLevelDataGrammar(sceneName, levelFlow.levelData, errors);
                }

                if (bossSpawnPoint == null)
                {
                    errors.Add($"[{sceneName}] Missing BossSpawnPoint.");
                }
                else
                {
                    ValidateSpawnPointGrammar(sceneName, bossSpawnPoint, errors);
                }

                if (levelFlow != null &&
                    levelFlow.levelData != null &&
                    levelFlow.levelData.overrideBossEncounterTuning &&
                    bossSpawnPoint != null &&
                    bossSpawnPoint.overrideEncounterTuning)
                {
                    AssertApproximately(sceneName, "phase2Threshold", levelFlow.levelData.bossPhase2HealthThreshold, bossSpawnPoint.phase2HealthThreshold, errors);
                    AssertApproximately(sceneName, "phase3Threshold", levelFlow.levelData.bossPhase3HealthThreshold, bossSpawnPoint.phase3HealthThreshold, errors);
                    if (levelFlow.levelData.bossEnablePhaseTransitionOpeners && bossSpawnPoint.enablePhaseTransitionOpeners)
                    {
                        if (!string.Equals(levelFlow.levelData.bossPhase2TransitionOpenerId, bossSpawnPoint.phase2TransitionOpenerId, StringComparison.Ordinal))
                        {
                            errors.Add($"[{sceneName}] phase2 opener mismatch LevelData={levelFlow.levelData.bossPhase2TransitionOpenerId} Spawn={bossSpawnPoint.phase2TransitionOpenerId}");
                        }

                        if (!string.Equals(levelFlow.levelData.bossPhase3TransitionOpenerId, bossSpawnPoint.phase3TransitionOpenerId, StringComparison.Ordinal))
                        {
                            errors.Add($"[{sceneName}] phase3 opener mismatch LevelData={levelFlow.levelData.bossPhase3TransitionOpenerId} Spawn={bossSpawnPoint.phase3TransitionOpenerId}");
                        }
                    }
                }

                yield return UnloadScene(scene, errors);
            }

            if (baselineScene.IsValid() && baselineScene.isLoaded)
            {
                SceneManager.SetActiveScene(baselineScene);
            }

            if (errors.Count > 0)
            {
                Assert.Fail(string.Join("\n", errors));
            }
        }

        [Test]
        public void BossSpawnPoint_ApplyEncounterTuning_ClampsPhaseGrammarToSafeRange()
        {
            Assert.NotNull(ApplyEncounterTuningMethod, "BossSpawnPoint.ApplyEncounterTuning(BossController) should exist.");

            GameObject spawnGo = new GameObject("BossPhaseGrammar_Spawn");
            createdObjects.Add(spawnGo);
            BossSpawnPoint spawn = spawnGo.AddComponent<BossSpawnPoint>();
            spawn.overrideEncounterTuning = true;
            spawn.phase2HealthThreshold = 0.2f;
            spawn.phase3HealthThreshold = 0.3f;
            spawn.breakWindowDuration = 4f;
            spawn.breakWindowCooldown = 9f;
            spawn.breakWindowDamageMultiplier = 1.5f;
            spawn.attackInterval = 0.01f;
            spawn.decisionInterval = 0.01f;
            spawn.queuedAttackLimit = 0;
            spawn.immediateRepeatPenalty = -0.2f;
            spawn.enablePhaseTransitionOpeners = true;
            spawn.phase2TransitionOpenerId = "eel_vortex";
            spawn.phase3TransitionOpenerId = "eel_devour";
            spawn.enablePhaseTransitionOpenerRetry = true;
            spawn.phaseTransitionOpenerRetryDelay = 0.1f;
            spawn.phaseTransitionOpenerMaxRetries = 2;
            spawn.enablePhaseTransitionFollowupChain = true;
            spawn.phase2TransitionFollowupId = "eel_charge";
            spawn.phase3TransitionFollowupId = "eel_chain";
            spawn.enablePhaseTransitionFollowupRetry = true;
            spawn.phaseTransitionFollowupRetryDelay = -0.2f;
            spawn.phaseTransitionFollowupMaxRetries = -3;
            spawn.enablePhaseComboChain = true;
            spawn.phase2ComboChance = -0.5f;
            spawn.phase3ComboChance = 1.3f;

            GameObject probeGo = new GameObject("BossPhaseGrammar_ControllerProbe");
            createdObjects.Add(probeGo);
            BossController probeController = probeGo.AddComponent<BossController>();

            ApplyEncounterTuningMethod.Invoke(spawn, new object[] { probeController });

            float phase2 = ResolvePhaseThreshold(probeController, 1, fallback: 0.66f);
            float phase3 = ResolvePhaseThreshold(probeController, 2, fallback: 0.33f);
            Assert.Greater(phase2, phase3, "Phase2 threshold should be greater than phase3 threshold after tuning.");
            Assert.GreaterOrEqual(phase2, 0.1f);
            Assert.GreaterOrEqual(phase3, 0.05f);
            Assert.LessOrEqual(probeController.immediateRepeatPenalty, 1f);
            Assert.GreaterOrEqual(probeController.immediateRepeatPenalty, 0f);
            Assert.GreaterOrEqual(probeController.queuedAttackLimit, 1);
            Assert.GreaterOrEqual(probeController.decisionInterval, 0.05f);
            Assert.GreaterOrEqual(probeController.phase2ComboChance, 0f);
            Assert.LessOrEqual(probeController.phase2ComboChance, 1f);
            Assert.GreaterOrEqual(probeController.phase3ComboChance, 0f);
            Assert.LessOrEqual(probeController.phase3ComboChance, 1f);
            Assert.IsTrue(probeController.enablePhaseTransitionFollowupChain);
            Assert.AreEqual("eel_charge", probeController.phase2TransitionFollowupId);
            Assert.AreEqual("eel_chain", probeController.phase3TransitionFollowupId);
            Assert.IsTrue(probeController.enablePhaseTransitionFollowupRetry);
            Assert.GreaterOrEqual(probeController.phaseTransitionFollowupRetryDelay, 0f);
            Assert.GreaterOrEqual(probeController.phaseTransitionFollowupMaxRetries, 0);
        }

        [Test]
        public void LevelRuntimeConfigurator_Apply_BossGrammarFieldsAreSynchronized()
        {
            GameObject flowGo = new GameObject("BossPhaseGrammar_RuntimeFlow");
            createdObjects.Add(flowGo);
            LevelFlowController levelFlow = flowGo.AddComponent<LevelFlowController>();

            GameObject sequenceGo = new GameObject("BossPhaseGrammar_RuntimeSequence");
            createdObjects.Add(sequenceGo);
            StrongholdSequenceController sequence = sequenceGo.AddComponent<StrongholdSequenceController>();
            sequence.autoStartFirst = false;

            GameObject spawnGo = new GameObject("BossPhaseGrammar_RuntimeSpawn");
            createdObjects.Add(spawnGo);
            BossSpawnPoint spawnPoint = spawnGo.AddComponent<BossSpawnPoint>();
            spawnPoint.spawnOnStart = true;

            LevelData levelData = ScriptableObject.CreateInstance<LevelData>();
            createdObjects.Add(levelData);
            levelData.levelId = "LEVEL_10";
            levelData.overrideBossSettings = true;
            levelData.overrideBossEncounterTuning = true;
            levelData.bossPhase2HealthThreshold = 0.6f;
            levelData.bossPhase3HealthThreshold = 0.28f;
            levelData.bossBreakWindowDuration = 5.1f;
            levelData.bossBreakWindowCooldown = 9.6f;
            levelData.bossBreakWindowDamageMultiplier = 1.78f;
            levelData.bossAttackInterval = 2.4f;
            levelData.bossDecisionInterval = 0.57f;
            levelData.bossQueuedAttackLimit = 4;
            levelData.bossImmediateRepeatPenalty = 0.25f;
            levelData.bossEnablePhaseTransitionOpeners = true;
            levelData.bossPhase2TransitionOpenerId = "eel_vortex";
            levelData.bossPhase3TransitionOpenerId = "eel_devour";
            levelData.bossEnablePhaseTransitionOpenerRetry = true;
            levelData.bossPhaseTransitionOpenerRetryDelay = 0.15f;
            levelData.bossPhaseTransitionOpenerMaxRetries = 4;
            levelData.bossEnablePhaseTransitionFollowupChain = true;
            levelData.bossPhase2TransitionFollowupId = "eel_charge";
            levelData.bossPhase3TransitionFollowupId = "eel_chain";
            levelData.bossEnablePhaseTransitionFollowupRetry = true;
            levelData.bossPhaseTransitionFollowupRetryDelay = 0.12f;
            levelData.bossPhaseTransitionFollowupMaxRetries = 3;
            levelData.bossEnablePhase3SpecialPriorityWindow = true;
            levelData.bossPhase3SpecialPriorityDuration = 6.5f;
            levelData.bossPhase3SpecialPriorityWeightMultiplier = 1.8f;
            levelData.bossForceSpecialQueueDuringPhase3Priority = true;

            LevelRuntimeConfigurator configurator = flowGo.AddComponent<LevelRuntimeConfigurator>();
            configurator.autoApplyOnAwake = false;
            configurator.ensureRuntimeWiring = false;
            configurator.applyStrongholds = false;
            configurator.applyQuests = false;
            configurator.applyRewards = false;
            configurator.levelFlow = levelFlow;
            configurator.levelData = levelData;
            configurator.sequenceController = sequence;
            configurator.bossSpawnPoint = spawnPoint;

            configurator.Apply();

            Assert.IsFalse(spawnPoint.spawnOnStart, "Boss gate should disable spawnOnStart for staged encounter flow.");
            Assert.IsTrue(spawnPoint.overrideEncounterTuning);
            Assert.AreEqual(levelData.bossPhase2HealthThreshold, spawnPoint.phase2HealthThreshold, 0.0001f);
            Assert.AreEqual(levelData.bossPhase3HealthThreshold, spawnPoint.phase3HealthThreshold, 0.0001f);
            Assert.AreEqual(levelData.bossBreakWindowDuration, spawnPoint.breakWindowDuration, 0.0001f);
            Assert.AreEqual(levelData.bossBreakWindowCooldown, spawnPoint.breakWindowCooldown, 0.0001f);
            Assert.AreEqual(levelData.bossAttackInterval, spawnPoint.attackInterval, 0.0001f);
            Assert.AreEqual(levelData.bossDecisionInterval, spawnPoint.decisionInterval, 0.0001f);
            Assert.AreEqual(levelData.bossQueuedAttackLimit, spawnPoint.queuedAttackLimit);
            Assert.AreEqual(levelData.bossImmediateRepeatPenalty, spawnPoint.immediateRepeatPenalty, 0.0001f);
            Assert.IsTrue(spawnPoint.enablePhaseTransitionOpeners);
            Assert.AreEqual(levelData.bossPhase2TransitionOpenerId, spawnPoint.phase2TransitionOpenerId);
            Assert.AreEqual(levelData.bossPhase3TransitionOpenerId, spawnPoint.phase3TransitionOpenerId);
            Assert.IsTrue(spawnPoint.enablePhaseTransitionOpenerRetry);
            Assert.AreEqual(levelData.bossPhaseTransitionOpenerRetryDelay, spawnPoint.phaseTransitionOpenerRetryDelay, 0.0001f);
            Assert.AreEqual(levelData.bossPhaseTransitionOpenerMaxRetries, spawnPoint.phaseTransitionOpenerMaxRetries);
            Assert.IsTrue(spawnPoint.enablePhaseTransitionFollowupChain);
            Assert.AreEqual(levelData.bossPhase2TransitionFollowupId, spawnPoint.phase2TransitionFollowupId);
            Assert.AreEqual(levelData.bossPhase3TransitionFollowupId, spawnPoint.phase3TransitionFollowupId);
            Assert.IsTrue(spawnPoint.enablePhaseTransitionFollowupRetry);
            Assert.AreEqual(levelData.bossPhaseTransitionFollowupRetryDelay, spawnPoint.phaseTransitionFollowupRetryDelay, 0.0001f);
            Assert.AreEqual(levelData.bossPhaseTransitionFollowupMaxRetries, spawnPoint.phaseTransitionFollowupMaxRetries);
            Assert.IsTrue(spawnPoint.enablePhase3SpecialPriorityWindow);
            Assert.AreEqual(levelData.bossPhase3SpecialPriorityDuration, spawnPoint.phase3SpecialPriorityDuration, 0.0001f);
            Assert.AreEqual(levelData.bossPhase3SpecialPriorityWeightMultiplier, spawnPoint.phase3SpecialPriorityWeightMultiplier, 0.0001f);
            Assert.IsTrue(spawnPoint.forceSpecialQueueDuringPhase3Priority);
        }

        private static void ValidateLevelDataGrammar(string sceneName, LevelData levelData, List<string> errors)
        {
            if (levelData == null)
            {
                errors.Add($"[{sceneName}] LevelData is null.");
                return;
            }

            if (!levelData.overrideBossSettings)
            {
                errors.Add($"[{sceneName}] LevelData.overrideBossSettings should be true.");
                return;
            }

            if (!levelData.overrideBossEncounterTuning)
            {
                errors.Add($"[{sceneName}] LevelData.overrideBossEncounterTuning should be true.");
                return;
            }

            ValidateGrammarCore(sceneName, "LevelData",
                levelData.bossPhase2HealthThreshold,
                levelData.bossPhase3HealthThreshold,
                levelData.bossBreakWindowDuration,
                levelData.bossBreakWindowCooldown,
                levelData.bossBreakWindowDamageMultiplier,
                levelData.bossAttackInterval,
                levelData.bossDecisionInterval,
                levelData.bossQueuedAttackLimit,
                levelData.bossImmediateRepeatPenalty,
                levelData.bossEnablePhaseTransitionOpeners,
                levelData.bossPhase2TransitionOpenerId,
                levelData.bossPhase3TransitionOpenerId,
                levelData.bossEnablePhaseTransitionOpenerRetry,
                levelData.bossPhaseTransitionOpenerRetryDelay,
                levelData.bossPhaseTransitionOpenerMaxRetries,
                levelData.bossEnablePhaseTransitionFollowupChain,
                levelData.bossPhase2TransitionFollowupId,
                levelData.bossPhase3TransitionFollowupId,
                levelData.bossEnablePhaseTransitionFollowupRetry,
                levelData.bossPhaseTransitionFollowupRetryDelay,
                levelData.bossPhaseTransitionFollowupMaxRetries,
                levelData.bossEnablePhase3SpecialPriorityWindow,
                levelData.bossPhase3SpecialPriorityDuration,
                levelData.bossPhase3SpecialPriorityWeightMultiplier,
                levelData.bossEnablePhaseComboChain,
                levelData.bossPhase2ComboChance,
                levelData.bossPhase3ComboChance,
                levelData.bossEnableTimePressure,
                levelData.bossTimePressureRampDuration,
                levelData.bossMaxTimePressureDamageMultiplier,
                levelData.bossMaxTimePressureSpeedMultiplier,
                errors);
        }

        private static void ValidateSpawnPointGrammar(string sceneName, BossSpawnPoint spawnPoint, List<string> errors)
        {
            if (spawnPoint == null)
            {
                errors.Add($"[{sceneName}] BossSpawnPoint is null.");
                return;
            }

            if (!spawnPoint.overrideEncounterTuning)
            {
                errors.Add($"[{sceneName}] BossSpawnPoint.overrideEncounterTuning should be true.");
                return;
            }

            ValidateGrammarCore(sceneName, "BossSpawnPoint",
                spawnPoint.phase2HealthThreshold,
                spawnPoint.phase3HealthThreshold,
                spawnPoint.breakWindowDuration,
                spawnPoint.breakWindowCooldown,
                spawnPoint.breakWindowDamageMultiplier,
                spawnPoint.attackInterval,
                spawnPoint.decisionInterval,
                spawnPoint.queuedAttackLimit,
                spawnPoint.immediateRepeatPenalty,
                spawnPoint.enablePhaseTransitionOpeners,
                spawnPoint.phase2TransitionOpenerId,
                spawnPoint.phase3TransitionOpenerId,
                spawnPoint.enablePhaseTransitionOpenerRetry,
                spawnPoint.phaseTransitionOpenerRetryDelay,
                spawnPoint.phaseTransitionOpenerMaxRetries,
                spawnPoint.enablePhaseTransitionFollowupChain,
                spawnPoint.phase2TransitionFollowupId,
                spawnPoint.phase3TransitionFollowupId,
                spawnPoint.enablePhaseTransitionFollowupRetry,
                spawnPoint.phaseTransitionFollowupRetryDelay,
                spawnPoint.phaseTransitionFollowupMaxRetries,
                spawnPoint.enablePhase3SpecialPriorityWindow,
                spawnPoint.phase3SpecialPriorityDuration,
                spawnPoint.phase3SpecialPriorityWeightMultiplier,
                spawnPoint.enablePhaseComboChain,
                spawnPoint.phase2ComboChance,
                spawnPoint.phase3ComboChance,
                spawnPoint.enableTimePressure,
                spawnPoint.timePressureRampDuration,
                spawnPoint.maxTimePressureDamageMultiplier,
                spawnPoint.maxTimePressureSpeedMultiplier,
                errors);
        }

        private static void ValidateGrammarCore(
            string sceneName,
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
            bool enableOpeners,
            string phase2OpenerId,
            string phase3OpenerId,
            bool enableOpenerRetry,
            float openerRetryDelay,
            int openerMaxRetries,
            bool enableFollowupChain,
            string phase2FollowupId,
            string phase3FollowupId,
            bool enableFollowupRetry,
            float followupRetryDelay,
            int followupMaxRetries,
            bool enablePhase3PriorityWindow,
            float phase3PriorityDuration,
            float phase3PriorityWeight,
            bool enableComboChain,
            float phase2ComboChance,
            float phase3ComboChance,
            bool enableTimePressure,
            float timePressureRampDuration,
            float maxTimePressureDamageMultiplier,
            float maxTimePressureSpeedMultiplier,
            List<string> errors)
        {
            if (phase2Threshold <= 0.1f || phase2Threshold >= 0.95f)
            {
                errors.Add($"[{sceneName}] {source}.phase2Threshold out of range (0.1,0.95).");
            }

            if (phase3Threshold <= 0.05f || phase3Threshold >= phase2Threshold - 0.05f)
            {
                errors.Add($"[{sceneName}] {source}.phase3Threshold should be >=0.05 and at least 0.05 below phase2Threshold.");
            }

            if (breakWindowDuration <= 0f || breakWindowCooldown <= breakWindowDuration || breakWindowDamageMultiplier < 1f)
            {
                errors.Add($"[{sceneName}] {source}.break window grammar invalid.");
            }

            if (attackInterval <= 0f || decisionInterval < 0.05f || queuedAttackLimit < 1)
            {
                errors.Add($"[{sceneName}] {source}.attack cadence grammar invalid.");
            }

            if (immediateRepeatPenalty < 0f || immediateRepeatPenalty > 1f)
            {
                errors.Add($"[{sceneName}] {source}.immediateRepeatPenalty must be in [0,1].");
            }

            if (enableOpeners)
            {
                if (string.IsNullOrWhiteSpace(phase2OpenerId) || string.IsNullOrWhiteSpace(phase3OpenerId))
                {
                    errors.Add($"[{sceneName}] {source}.phase opener ids must both be set when openers are enabled.");
                }

                if (!string.IsNullOrWhiteSpace(phase2OpenerId) &&
                    !string.IsNullOrWhiteSpace(phase3OpenerId) &&
                    string.Equals(phase2OpenerId.Trim(), phase3OpenerId.Trim(), StringComparison.Ordinal))
                {
                    errors.Add($"[{sceneName}] {source}.phase2OpenerId and phase3OpenerId must be different.");
                }

                if (enableOpenerRetry && (openerRetryDelay <= 0f || openerMaxRetries <= 0))
                {
                    errors.Add($"[{sceneName}] {source}.phase opener retry grammar invalid.");
                }
            }

            if (enableFollowupChain)
            {
                if (!enableOpeners)
                {
                    errors.Add($"[{sceneName}] {source}.followup chain requires phase openers enabled.");
                }

                if (string.IsNullOrWhiteSpace(phase2FollowupId) || string.IsNullOrWhiteSpace(phase3FollowupId))
                {
                    errors.Add($"[{sceneName}] {source}.phase followup ids must both be set when followup chain is enabled.");
                }

                if (enableFollowupRetry && (followupRetryDelay <= 0f || followupMaxRetries <= 0))
                {
                    errors.Add($"[{sceneName}] {source}.phase followup retry grammar invalid.");
                }

                if (!string.IsNullOrWhiteSpace(phase2OpenerId) &&
                    !string.IsNullOrWhiteSpace(phase2FollowupId) &&
                    string.Equals(phase2OpenerId.Trim(), phase2FollowupId.Trim(), StringComparison.Ordinal))
                {
                    errors.Add($"[{sceneName}] {source}.phase2OpenerId and phase2FollowupId must be different.");
                }

                if (!string.IsNullOrWhiteSpace(phase3OpenerId) &&
                    !string.IsNullOrWhiteSpace(phase3FollowupId) &&
                    string.Equals(phase3OpenerId.Trim(), phase3FollowupId.Trim(), StringComparison.Ordinal))
                {
                    errors.Add($"[{sceneName}] {source}.phase3OpenerId and phase3FollowupId must be different.");
                }

                if (!string.IsNullOrWhiteSpace(phase2FollowupId) &&
                    !string.IsNullOrWhiteSpace(phase3FollowupId) &&
                    string.Equals(phase2FollowupId.Trim(), phase3FollowupId.Trim(), StringComparison.Ordinal))
                {
                    errors.Add($"[{sceneName}] {source}.phase2FollowupId and phase3FollowupId must be different.");
                }
            }

            if (enableFollowupRetry && !enableFollowupChain)
            {
                errors.Add($"[{sceneName}] {source}.phase followup retry requires followup chain enabled.");
            }

            if (enablePhase3PriorityWindow && (phase3PriorityDuration <= 0f || phase3PriorityWeight < 1f))
            {
                errors.Add($"[{sceneName}] {source}.phase3 priority window grammar invalid.");
            }

            if (enableComboChain &&
                (phase2ComboChance < 0f || phase2ComboChance > 1f || phase3ComboChance < 0f || phase3ComboChance > 1f || phase3ComboChance < phase2ComboChance))
            {
                errors.Add($"[{sceneName}] {source}.combo chance grammar invalid.");
            }

            if (enableTimePressure &&
                (timePressureRampDuration < 1f || maxTimePressureDamageMultiplier < 1f || maxTimePressureSpeedMultiplier < 1f))
            {
                errors.Add($"[{sceneName}] {source}.time pressure grammar invalid.");
            }
        }

        private static void AssertApproximately(string sceneName, string key, float expected, float actual, List<string> errors)
        {
            if (Mathf.Abs(expected - actual) > 0.0001f)
            {
                errors.Add($"[{sceneName}] {key} mismatch expected={expected:0.###} actual={actual:0.###}");
            }
        }

        private static float ResolvePhaseThreshold(BossController controller, int index, float fallback)
        {
            if (controller == null || controller.phases == null || index < 0 || index >= controller.phases.Count)
            {
                return fallback;
            }

            BossPhase phase = controller.phases[index];
            return phase != null ? phase.healthPercentThreshold : fallback;
        }

        private static IEnumerator UnloadScene(Scene scene, List<string> errors)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                yield break;
            }

            AsyncOperation unload = SceneManager.UnloadSceneAsync(scene);
            if (unload == null)
            {
                errors.Add($"[{scene.name}] UnloadSceneAsync returned null.");
                yield break;
            }

            while (!unload.isDone)
            {
                yield return null;
            }
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
    }
}
