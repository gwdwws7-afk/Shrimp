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
    public class BossLevel10GateRegressionTests
    {
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

        [Test]
        public void LevelRuntimeConfigurator_RuntimeBossGateWiring_BindsSingleDefeatHandler()
        {
            GameObject flowGo = new GameObject("RuntimeBossGate_LevelFlow");
            createdObjects.Add(flowGo);
            LevelFlowController levelFlow = flowGo.AddComponent<LevelFlowController>();

            GameObject sequenceGo = new GameObject("RuntimeBossGate_StrongholdSequence");
            createdObjects.Add(sequenceGo);
            StrongholdSequenceController sequence = sequenceGo.AddComponent<StrongholdSequenceController>();
            sequence.autoStartFirst = false;

            GameObject bossSpawnGo = new GameObject("RuntimeBossGate_BossSpawnPoint");
            createdObjects.Add(bossSpawnGo);
            BossSpawnPoint bossSpawnPoint = bossSpawnGo.AddComponent<BossSpawnPoint>();
            bossSpawnPoint.spawnOnStart = true;

            LevelData levelData = ScriptableObject.CreateInstance<LevelData>();
            createdObjects.Add(levelData);
            levelData.levelId = "LEVEL_10";
            levelData.chapterId = 1;
            levelData.overrideBossSettings = true;
            levelData.bossName = "Boss_RuntimeGate";
            levelData.bossMaxHealth = 4200;
            levelData.bossBaseDamage = 38;
            levelData.bossKnockback = 7.2f;
            levelData.bossScaleMultiplier = 2.3f;
            levelData.bossSpawnOffset = new Vector3(0f, 0f, 1f);
            levelData.overrideBossEncounterTuning = true;
            levelData.bossPhase2HealthThreshold = 0.58f;
            levelData.bossPhase3HealthThreshold = 0.26f;
            levelData.bossBreakWindowDuration = 5.5f;
            levelData.bossBreakWindowCooldown = 9.5f;
            levelData.bossBreakWindowDamageMultiplier = 1.85f;
            levelData.bossStaggerMax = 145f;
            levelData.bossStaggerPerDamage = 1.45f;
            levelData.bossAttackInterval = 2.42f;
            levelData.bossDecisionInterval = 0.56f;
            levelData.bossQueuedAttackLimit = 4;
            levelData.bossImmediateRepeatPenalty = 0.24f;
            levelData.bossEnablePostBreakPunishWindow = true;
            levelData.bossPostBreakPunishDuration = 6.25f;
            levelData.bossPostBreakAttackIntervalMultiplier = 0.68f;
            levelData.bossPostBreakDecisionIntervalMultiplier = 0.74f;
            levelData.bossPostBreakChaseSpeedMultiplier = 1.22f;
            levelData.bossEnablePhaseComboChain = true;
            levelData.bossPhase2ComboChance = 0.59f;
            levelData.bossPhase3ComboChance = 0.8f;
            levelData.bossComboStartDelay = 0.05f;
            levelData.bossComboRepeatPenalty = 0.21f;
            levelData.bossEnableInterruptRecoveryGate = true;
            levelData.bossInterruptRecoveryDuration = 0.13f;
            levelData.bossInterruptedAttackCooldownScale = 0.33f;
            levelData.bossEnableTimePressure = true;
            levelData.bossTimePressureDelay = 52f;
            levelData.bossTimePressureRampDuration = 40f;
            levelData.bossMaxTimePressureDamageMultiplier = 1.48f;
            levelData.bossMaxTimePressureSpeedMultiplier = 1.27f;
            levelData.bossEnablePhaseTransitionOpeners = true;
            levelData.bossPhase2TransitionOpenerId = "eel_vortex";
            levelData.bossPhase3TransitionOpenerId = "eel_devour";
            levelData.bossEnablePhaseTransitionOpenerRetry = true;
            levelData.bossPhaseTransitionOpenerRetryDelay = 0.16f;
            levelData.bossPhaseTransitionOpenerMaxRetries = 5;
            levelData.bossEnablePhaseTransitionFollowupChain = true;
            levelData.bossPhase2TransitionFollowupId = "eel_charge";
            levelData.bossPhase3TransitionFollowupId = "eel_chain";
            levelData.bossEnablePhaseTransitionFollowupRetry = true;
            levelData.bossPhaseTransitionFollowupRetryDelay = 0.14f;
            levelData.bossPhaseTransitionFollowupMaxRetries = 4;
            levelData.bossEnablePhase3SpecialPriorityWindow = true;
            levelData.bossPhase3SpecialPriorityDuration = 7.5f;
            levelData.bossPhase3SpecialPriorityWeightMultiplier = 1.9f;
            levelData.bossForceSpecialQueueDuringPhase3Priority = true;

            LevelRuntimeConfigurator runtimeConfigurator = flowGo.AddComponent<LevelRuntimeConfigurator>();
            runtimeConfigurator.autoApplyOnAwake = false;
            runtimeConfigurator.ensureRuntimeWiring = false;
            runtimeConfigurator.applyStrongholds = false;
            runtimeConfigurator.applyQuests = false;
            runtimeConfigurator.applyRewards = false;
            runtimeConfigurator.levelFlow = levelFlow;
            runtimeConfigurator.levelData = levelData;
            runtimeConfigurator.sequenceController = sequence;
            runtimeConfigurator.bossSpawnPoint = bossSpawnPoint;

            runtimeConfigurator.Apply();

            Assert.IsFalse(bossSpawnPoint.spawnOnStart, "Boss should not spawn at scene start for boss-gated levels.");
            Assert.IsTrue(sequence.deferCompletionUntilBoss, "Boss gate should be enabled when boss override is active.");
            Assert.AreSame(bossSpawnPoint, sequence.bossSpawnPoint, "Sequence should be wired to configured boss spawn point.");
            Assert.IsTrue(bossSpawnPoint.overrideEncounterTuning, "Boss encounter tuning override should be enabled.");
            Assert.AreEqual(0.58f, bossSpawnPoint.phase2HealthThreshold, 0.0001f);
            Assert.AreEqual(0.26f, bossSpawnPoint.phase3HealthThreshold, 0.0001f);
            Assert.AreEqual(5.5f, bossSpawnPoint.breakWindowDuration, 0.0001f);
            Assert.AreEqual(9.5f, bossSpawnPoint.breakWindowCooldown, 0.0001f);
            Assert.AreEqual(1.85f, bossSpawnPoint.breakWindowDamageMultiplier, 0.0001f);
            Assert.AreEqual(145f, bossSpawnPoint.staggerMax, 0.0001f);
            Assert.AreEqual(1.45f, bossSpawnPoint.staggerPerDamage, 0.0001f);
            Assert.AreEqual(2.42f, bossSpawnPoint.attackInterval, 0.0001f);
            Assert.AreEqual(0.56f, bossSpawnPoint.decisionInterval, 0.0001f);
            Assert.AreEqual(4, bossSpawnPoint.queuedAttackLimit);
            Assert.AreEqual(0.24f, bossSpawnPoint.immediateRepeatPenalty, 0.0001f);
            Assert.IsTrue(bossSpawnPoint.enablePostBreakPunishWindow);
            Assert.AreEqual(6.25f, bossSpawnPoint.postBreakPunishDuration, 0.0001f);
            Assert.AreEqual(0.68f, bossSpawnPoint.postBreakAttackIntervalMultiplier, 0.0001f);
            Assert.AreEqual(0.74f, bossSpawnPoint.postBreakDecisionIntervalMultiplier, 0.0001f);
            Assert.AreEqual(1.22f, bossSpawnPoint.postBreakChaseSpeedMultiplier, 0.0001f);
            Assert.IsTrue(bossSpawnPoint.enablePhaseComboChain);
            Assert.AreEqual(0.59f, bossSpawnPoint.phase2ComboChance, 0.0001f);
            Assert.AreEqual(0.8f, bossSpawnPoint.phase3ComboChance, 0.0001f);
            Assert.AreEqual(0.05f, bossSpawnPoint.comboStartDelay, 0.0001f);
            Assert.AreEqual(0.21f, bossSpawnPoint.comboRepeatPenalty, 0.0001f);
            Assert.IsTrue(bossSpawnPoint.enableInterruptRecoveryGate);
            Assert.AreEqual(0.13f, bossSpawnPoint.interruptRecoveryDuration, 0.0001f);
            Assert.AreEqual(0.33f, bossSpawnPoint.interruptedAttackCooldownScale, 0.0001f);
            Assert.IsTrue(bossSpawnPoint.enableTimePressure);
            Assert.AreEqual(52f, bossSpawnPoint.timePressureDelay, 0.0001f);
            Assert.AreEqual(40f, bossSpawnPoint.timePressureRampDuration, 0.0001f);
            Assert.AreEqual(1.48f, bossSpawnPoint.maxTimePressureDamageMultiplier, 0.0001f);
            Assert.AreEqual(1.27f, bossSpawnPoint.maxTimePressureSpeedMultiplier, 0.0001f);
            Assert.IsTrue(bossSpawnPoint.enablePhaseTransitionOpeners);
            Assert.AreEqual("eel_vortex", bossSpawnPoint.phase2TransitionOpenerId);
            Assert.AreEqual("eel_devour", bossSpawnPoint.phase3TransitionOpenerId);
            Assert.IsTrue(bossSpawnPoint.enablePhaseTransitionOpenerRetry);
            Assert.AreEqual(0.16f, bossSpawnPoint.phaseTransitionOpenerRetryDelay, 0.0001f);
            Assert.AreEqual(5, bossSpawnPoint.phaseTransitionOpenerMaxRetries);
            Assert.IsTrue(bossSpawnPoint.enablePhaseTransitionFollowupChain);
            Assert.AreEqual("eel_charge", bossSpawnPoint.phase2TransitionFollowupId);
            Assert.AreEqual("eel_chain", bossSpawnPoint.phase3TransitionFollowupId);
            Assert.IsTrue(bossSpawnPoint.enablePhaseTransitionFollowupRetry);
            Assert.AreEqual(0.14f, bossSpawnPoint.phaseTransitionFollowupRetryDelay, 0.0001f);
            Assert.AreEqual(4, bossSpawnPoint.phaseTransitionFollowupMaxRetries);
            Assert.IsTrue(bossSpawnPoint.enablePhase3SpecialPriorityWindow);
            Assert.AreEqual(7.5f, bossSpawnPoint.phase3SpecialPriorityDuration, 0.0001f);
            Assert.AreEqual(1.9f, bossSpawnPoint.phase3SpecialPriorityWeightMultiplier, 0.0001f);
            Assert.IsTrue(bossSpawnPoint.forceSpecialQueueDuringPhase3Priority);
            Assert.AreEqual(1, CountBossDefeatHandlers(bossSpawnPoint, sequence),
                "Runtime boss gate wiring should register exactly one defeat handler.");

            runtimeConfigurator.Apply();
            Assert.AreEqual(1, CountBossDefeatHandlers(bossSpawnPoint, sequence),
                "Repeated Apply calls should not duplicate boss defeat handlers.");
        }

        [UnityTest]
        public IEnumerator Level10Scene_BossGateChain_IsWiredForStrongholdThenBossFlow()
        {
            Scene baselineScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene("Level_10_HiveCore", LoadSceneMode.Additive);
            yield return null;
            yield return null;

            Scene levelScene = SceneManager.GetSceneByName("Level_10_HiveCore");
            Assert.IsTrue(levelScene.IsValid() && levelScene.isLoaded, "Level_10 scene should be loaded.");

            LevelFlowController levelFlow = FindComponentInScene<LevelFlowController>(levelScene);
            Assert.NotNull(levelFlow, "LevelFlowController should exist in Level_10 scene.");
            Assert.NotNull(levelFlow.levelData, "LevelFlowController should resolve level data.");
            Assert.AreEqual("LEVEL_10", levelFlow.levelData.levelId, "Level_10 scene should bind LevelData_Level10.");

            StrongholdSequenceController sequence = FindComponentInScene<StrongholdSequenceController>(levelScene);
            BossSpawnPoint bossSpawnPoint = FindComponentInScene<BossSpawnPoint>(levelScene);

            Assert.NotNull(sequence, "StrongholdSequenceController should exist.");
            Assert.NotNull(bossSpawnPoint, "BossSpawnPoint should exist.");
            Assert.IsTrue(sequence.deferCompletionUntilBoss, "Level_10 should defer completion until boss is defeated.");
            Assert.AreSame(bossSpawnPoint, sequence.bossSpawnPoint, "Stronghold sequence should reference scene boss spawn point.");
            Assert.IsFalse(bossSpawnPoint.spawnOnStart, "Boss should not spawn at scene start in Level_10.");
            Assert.IsTrue(levelFlow.levelData.bossEnablePhaseTransitionOpeners, "Level_10 should keep phase transition opener chain enabled.");
            Assert.AreEqual("eel_charge", levelFlow.levelData.bossPhase2TransitionOpenerId, "Level_10 phase2 opener should use Round4 aggressive opener entry.");
            Assert.AreEqual("eel_devour", levelFlow.levelData.bossPhase3TransitionOpenerId, "Level_10 phase3 opener should stay aligned with Eel phase3 profile.");
            Assert.AreEqual("eel_charge", bossSpawnPoint.phase2TransitionOpenerId, "Runtime boss spawn should receive phase2 opener mapping.");
            Assert.AreEqual("eel_devour", bossSpawnPoint.phase3TransitionOpenerId, "Runtime boss spawn should receive phase3 opener mapping.");
            Assert.IsTrue(levelFlow.levelData.bossEnablePhaseTransitionFollowupChain, "Level_10 should keep followup chain enabled in Round4.");
            Assert.AreEqual("eel_vortex", levelFlow.levelData.bossPhase2TransitionFollowupId, "Level_10 phase2 followup should invert opener chain for variation.");
            Assert.AreEqual("eel_charge", levelFlow.levelData.bossPhase3TransitionFollowupId, "Level_10 phase3 followup should stay distinct from opener.");
            Assert.AreEqual("eel_vortex", bossSpawnPoint.phase2TransitionFollowupId, "Runtime boss spawn should receive phase2 followup mapping.");
            Assert.AreEqual("eel_charge", bossSpawnPoint.phase3TransitionFollowupId, "Runtime boss spawn should receive phase3 followup mapping.");
            Assert.AreEqual(7f, bossSpawnPoint.phase3SpecialPriorityDuration, 0.0001f, "Level_10 Round4 priority window duration should be raised.");
            Assert.AreEqual(1.92f, bossSpawnPoint.phase3SpecialPriorityWeightMultiplier, 0.0001f, "Level_10 Round4 priority weight should be raised.");

            AsyncOperation unload = SceneManager.UnloadSceneAsync(levelScene);
            Assert.NotNull(unload, "Level_10 scene unload operation should be created.");
            while (!unload.isDone)
            {
                yield return null;
            }

            if (baselineScene.IsValid())
            {
                SceneManager.SetActiveScene(baselineScene);
            }
        }

        [UnityTest]
        public IEnumerator Level08And09Scenes_BossGateAndEncounterTuning_AreRuntimeAligned()
        {
            Scene baselineScene = SceneManager.GetActiveScene();
            List<string> errors = new List<string>();

            BossSceneExpectation[] expectations =
            {
                new BossSceneExpectation(
                    sceneName: "Level_08_MoltenRift",
                    levelId: "LEVEL_08",
                    bossName: "Boss_MoltenNarwhal",
                    prototype: BossPrototypeType.Eel,
                    bossMaxHealth: 4600,
                    bossBaseDamage: 36,
                    bossKnockback: 7f,
                    bossScaleMultiplier: 2.3f,
                    phase2Threshold: 0.61f,
                    phase3Threshold: 0.29f,
                    breakDuration: 5f,
                    breakCooldown: 10f,
                    breakDamageMultiplier: 1.78f,
                    staggerMax: 140f,
                    staggerPerDamage: 1.3f,
                    attackInterval: 2.61f,
                    decisionInterval: 0.61f,
                    queuedAttackLimit: 4,
                    immediateRepeatPenalty: 0.26f,
                    phase2TransitionOpenerId: "eel_vortex",
                    phase3TransitionOpenerId: "eel_devour"),
                new BossSceneExpectation(
                    sceneName: "Level_09_StillTideSanctum",
                    levelId: "LEVEL_09",
                    bossName: "Boss_MirrorTidemancer",
                    prototype: BossPrototypeType.Guardian,
                    bossMaxHealth: 4800,
                    bossBaseDamage: 38,
                    bossKnockback: 7.5f,
                    bossScaleMultiplier: 2.35f,
                    phase2Threshold: 0.6f,
                    phase3Threshold: 0.28f,
                    breakDuration: 5.2f,
                    breakCooldown: 9.8f,
                    breakDamageMultiplier: 1.82f,
                    staggerMax: 143f,
                    staggerPerDamage: 1.38f,
                    attackInterval: 2.61f,
                    decisionInterval: 0.61f,
                    queuedAttackLimit: 4,
                    immediateRepeatPenalty: 0.26f,
                    phase2TransitionOpenerId: "guard_spray",
                    phase3TransitionOpenerId: "guard_blade")
            };

            for (int i = 0; i < expectations.Length; i++)
            {
                BossSceneExpectation expectation = expectations[i];
                SceneManager.LoadScene(expectation.sceneName, LoadSceneMode.Additive);
                yield return null;
                yield return null;

                Scene scene = SceneManager.GetSceneByName(expectation.sceneName);
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    errors.Add($"[{expectation.sceneName}] Scene should be loaded.");
                    continue;
                }

                LevelFlowController levelFlow = FindComponentInScene<LevelFlowController>(scene);
                StrongholdSequenceController sequence = FindComponentInScene<StrongholdSequenceController>(scene);
                BossSpawnPoint bossSpawnPoint = FindComponentInScene<BossSpawnPoint>(scene);

                if (levelFlow == null)
                {
                    errors.Add($"[{expectation.sceneName}] Missing LevelFlowController.");
                }
                else
                {
                    if (levelFlow.levelData == null)
                    {
                        errors.Add($"[{expectation.sceneName}] LevelFlow.levelData is null.");
                    }
                    else
                    {
                        if (!string.Equals(levelFlow.levelData.levelId, expectation.levelId, StringComparison.Ordinal))
                        {
                            errors.Add($"[{expectation.sceneName}] LevelData.levelId mismatch. expected={expectation.levelId} actual={levelFlow.levelData.levelId}");
                        }

                        if (!levelFlow.levelData.overrideBossSettings)
                        {
                            errors.Add($"[{expectation.sceneName}] LevelData.overrideBossSettings should be true.");
                        }

                        if (!levelFlow.levelData.bossEnablePhaseTransitionOpeners)
                        {
                            errors.Add($"[{expectation.sceneName}] LevelData.bossEnablePhaseTransitionOpeners should be true.");
                        }

                        if (!string.Equals(levelFlow.levelData.bossPhase2TransitionOpenerId, expectation.phase2TransitionOpenerId, StringComparison.Ordinal))
                        {
                            errors.Add($"[{expectation.sceneName}] LevelData phase2 opener mismatch. expected={expectation.phase2TransitionOpenerId} actual={levelFlow.levelData.bossPhase2TransitionOpenerId}");
                        }

                        if (!string.Equals(levelFlow.levelData.bossPhase3TransitionOpenerId, expectation.phase3TransitionOpenerId, StringComparison.Ordinal))
                        {
                            errors.Add($"[{expectation.sceneName}] LevelData phase3 opener mismatch. expected={expectation.phase3TransitionOpenerId} actual={levelFlow.levelData.bossPhase3TransitionOpenerId}");
                        }
                    }
                }

                if (sequence == null)
                {
                    errors.Add($"[{expectation.sceneName}] Missing StrongholdSequenceController.");
                }
                else
                {
                    if (!sequence.deferCompletionUntilBoss)
                    {
                        errors.Add($"[{expectation.sceneName}] deferCompletionUntilBoss should be true.");
                    }
                }

                if (bossSpawnPoint == null)
                {
                    errors.Add($"[{expectation.sceneName}] Missing BossSpawnPoint.");
                }
                else
                {
                    if (bossSpawnPoint.spawnOnStart)
                    {
                        errors.Add($"[{expectation.sceneName}] BossSpawnPoint.spawnOnStart should be false.");
                    }

                    if (!bossSpawnPoint.overrideEncounterTuning)
                    {
                        errors.Add($"[{expectation.sceneName}] BossSpawnPoint.overrideEncounterTuning should be true.");
                    }

                    if (!string.Equals(bossSpawnPoint.bossName, expectation.bossName, StringComparison.Ordinal))
                    {
                        errors.Add($"[{expectation.sceneName}] bossName mismatch. expected={expectation.bossName} actual={bossSpawnPoint.bossName}");
                    }

                    if (bossSpawnPoint.prototype != expectation.prototype)
                    {
                        errors.Add($"[{expectation.sceneName}] prototype mismatch. expected={expectation.prototype} actual={bossSpawnPoint.prototype}");
                    }

                    if (bossSpawnPoint.maxHealth != expectation.bossMaxHealth)
                    {
                        errors.Add($"[{expectation.sceneName}] maxHealth mismatch. expected={expectation.bossMaxHealth} actual={bossSpawnPoint.maxHealth}");
                    }

                    if (bossSpawnPoint.baseDamage != expectation.bossBaseDamage)
                    {
                        errors.Add($"[{expectation.sceneName}] baseDamage mismatch. expected={expectation.bossBaseDamage} actual={bossSpawnPoint.baseDamage}");
                    }

                    AssertApproximately(expectation.sceneName, "bossKnockback", expectation.bossKnockback, bossSpawnPoint.knockback, errors);
                    AssertApproximately(expectation.sceneName, "bossScaleMultiplier", expectation.bossScaleMultiplier, bossSpawnPoint.scaleMultiplier, errors);
                    AssertApproximately(expectation.sceneName, "bossPhase2HealthThreshold", expectation.phase2Threshold, bossSpawnPoint.phase2HealthThreshold, errors);
                    AssertApproximately(expectation.sceneName, "bossPhase3HealthThreshold", expectation.phase3Threshold, bossSpawnPoint.phase3HealthThreshold, errors);
                    AssertApproximately(expectation.sceneName, "bossBreakWindowDuration", expectation.breakDuration, bossSpawnPoint.breakWindowDuration, errors);
                    AssertApproximately(expectation.sceneName, "bossBreakWindowCooldown", expectation.breakCooldown, bossSpawnPoint.breakWindowCooldown, errors);
                    AssertApproximately(expectation.sceneName, "bossBreakWindowDamageMultiplier", expectation.breakDamageMultiplier, bossSpawnPoint.breakWindowDamageMultiplier, errors);
                    AssertApproximately(expectation.sceneName, "bossStaggerMax", expectation.staggerMax, bossSpawnPoint.staggerMax, errors);
                    AssertApproximately(expectation.sceneName, "bossStaggerPerDamage", expectation.staggerPerDamage, bossSpawnPoint.staggerPerDamage, errors);
                    AssertApproximately(expectation.sceneName, "bossAttackInterval", expectation.attackInterval, bossSpawnPoint.attackInterval, errors);
                    AssertApproximately(expectation.sceneName, "bossDecisionInterval", expectation.decisionInterval, bossSpawnPoint.decisionInterval, errors);
                    AssertApproximately(expectation.sceneName, "bossImmediateRepeatPenalty", expectation.immediateRepeatPenalty, bossSpawnPoint.immediateRepeatPenalty, errors);

                    if (!bossSpawnPoint.enablePhaseTransitionOpeners)
                    {
                        errors.Add($"[{expectation.sceneName}] BossSpawnPoint.enablePhaseTransitionOpeners should be true.");
                    }

                    if (!string.Equals(bossSpawnPoint.phase2TransitionOpenerId, expectation.phase2TransitionOpenerId, StringComparison.Ordinal))
                    {
                        errors.Add($"[{expectation.sceneName}] BossSpawnPoint phase2 opener mismatch. expected={expectation.phase2TransitionOpenerId} actual={bossSpawnPoint.phase2TransitionOpenerId}");
                    }

                    if (!string.Equals(bossSpawnPoint.phase3TransitionOpenerId, expectation.phase3TransitionOpenerId, StringComparison.Ordinal))
                    {
                        errors.Add($"[{expectation.sceneName}] BossSpawnPoint phase3 opener mismatch. expected={expectation.phase3TransitionOpenerId} actual={bossSpawnPoint.phase3TransitionOpenerId}");
                    }

                    if (bossSpawnPoint.queuedAttackLimit != expectation.queuedAttackLimit)
                    {
                        errors.Add($"[{expectation.sceneName}] queuedAttackLimit mismatch. expected={expectation.queuedAttackLimit} actual={bossSpawnPoint.queuedAttackLimit}");
                    }

                    if (sequence != null && !ReferenceEquals(sequence.bossSpawnPoint, bossSpawnPoint))
                    {
                        errors.Add($"[{expectation.sceneName}] sequence.bossSpawnPoint should reference scene BossSpawnPoint.");
                    }

                    if (sequence != null)
                    {
                        int handlers = CountBossDefeatHandlers(bossSpawnPoint, sequence);
                        if (handlers != 1)
                        {
                            errors.Add($"[{expectation.sceneName}] boss defeat handler count mismatch. expected=1 actual={handlers}");
                        }
                    }
                }

                AsyncOperation unload = SceneManager.UnloadSceneAsync(scene);
                if (unload != null)
                {
                    while (!unload.isDone)
                    {
                        yield return null;
                    }
                }
            }

            if (baselineScene.IsValid())
            {
                SceneManager.SetActiveScene(baselineScene);
            }

            if (errors.Count > 0)
            {
                Assert.Fail(string.Join("\n", errors));
            }
        }

        [UnityTest]
        public IEnumerator Level08To10SceneSwitchAndReentry_BossGateBinding_RemainsSingle()
        {
            Scene baselineScene = SceneManager.GetActiveScene();
            List<string> errors = new List<string>();
            string[] route =
            {
                "Level_08_MoltenRift",
                "Level_09_StillTideSanctum",
                "Level_10_HiveCore",
                "Level_08_MoltenRift"
            };

            for (int i = 0; i < route.Length; i++)
            {
                string sceneName = route[i];
                SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
                yield return null;
                yield return null;

                Scene scene = SceneManager.GetSceneByName(sceneName);
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    errors.Add($"[{sceneName}] Scene should be loaded during route step {i + 1}.");
                    continue;
                }

                LevelFlowController levelFlow = FindComponentInScene<LevelFlowController>(scene);
                StrongholdSequenceController sequence = FindComponentInScene<StrongholdSequenceController>(scene);
                BossSpawnPoint bossSpawnPoint = FindComponentInScene<BossSpawnPoint>(scene);

                if (levelFlow == null)
                {
                    errors.Add($"[{sceneName}] Missing LevelFlowController at route step {i + 1}.");
                }
                else if (levelFlow.levelData == null)
                {
                    errors.Add($"[{sceneName}] LevelFlow.levelData is null at route step {i + 1}.");
                }
                else if (!levelFlow.levelData.overrideBossSettings)
                {
                    errors.Add($"[{sceneName}] LevelData.overrideBossSettings should be true at route step {i + 1}.");
                }

                if (sequence == null)
                {
                    errors.Add($"[{sceneName}] Missing StrongholdSequenceController at route step {i + 1}.");
                }
                else if (!sequence.deferCompletionUntilBoss)
                {
                    errors.Add($"[{sceneName}] deferCompletionUntilBoss should be true at route step {i + 1}.");
                }

                if (bossSpawnPoint == null)
                {
                    errors.Add($"[{sceneName}] Missing BossSpawnPoint at route step {i + 1}.");
                }
                else if (bossSpawnPoint.spawnOnStart)
                {
                    errors.Add($"[{sceneName}] BossSpawnPoint.spawnOnStart should be false at route step {i + 1}.");
                }

                if (sequence != null && bossSpawnPoint != null)
                {
                    if (!ReferenceEquals(sequence.bossSpawnPoint, bossSpawnPoint))
                    {
                        errors.Add($"[{sceneName}] sequence.bossSpawnPoint should reference scene BossSpawnPoint at route step {i + 1}.");
                    }

                    int beforeRebind = CountBossDefeatHandlers(bossSpawnPoint, sequence);
                    if (beforeRebind != 1)
                    {
                        errors.Add($"[{sceneName}] handler count before rebind mismatch at route step {i + 1}. expected=1 actual={beforeRebind}");
                    }

                    sequence.ConfigureBossGate(true, bossSpawnPoint);
                    int afterRebind = CountBossDefeatHandlers(bossSpawnPoint, sequence);
                    if (afterRebind != 1)
                    {
                        errors.Add($"[{sceneName}] handler count after first rebind mismatch at route step {i + 1}. expected=1 actual={afterRebind}");
                    }

                    sequence.ConfigureBossGate(true, bossSpawnPoint);
                    int afterSecondRebind = CountBossDefeatHandlers(bossSpawnPoint, sequence);
                    if (afterSecondRebind != 1)
                    {
                        errors.Add($"[{sceneName}] handler count after second rebind mismatch at route step {i + 1}. expected=1 actual={afterSecondRebind}");
                    }
                }

                AsyncOperation unload = SceneManager.UnloadSceneAsync(scene);
                if (unload != null)
                {
                    while (!unload.isDone)
                    {
                        yield return null;
                    }
                }
            }

            if (baselineScene.IsValid())
            {
                SceneManager.SetActiveScene(baselineScene);
            }

            if (errors.Count > 0)
            {
                Assert.Fail(string.Join("\n", errors));
            }
        }

        [UnityTest]
        public IEnumerator Level08To10SceneSwitch_BossDefeatEventStorm_ResolvesSingleCompletion()
        {
            Scene baselineScene = SceneManager.GetActiveScene();
            List<string> errors = new List<string>();
            bool previousLogEnabled = Debug.unityLogger.logEnabled;
            Debug.unityLogger.logEnabled = false;

            SceneCompletionExpectation[] route =
            {
                new SceneCompletionExpectation("Level_08_MoltenRift", 108),
                new SceneCompletionExpectation("Level_09_StillTideSanctum", 109),
                new SceneCompletionExpectation("Level_10_HiveCore", 110)
            };

            try
            {
                for (int i = 0; i < route.Length; i++)
                {
                    SceneCompletionExpectation expectation = route[i];
                    SceneManager.LoadScene(expectation.sceneName, LoadSceneMode.Additive);
                    yield return null;
                    yield return null;

                    Scene scene = SceneManager.GetSceneByName(expectation.sceneName);
                    if (!scene.IsValid() || !scene.isLoaded)
                    {
                        errors.Add($"[{expectation.sceneName}] Scene should be loaded.");
                        continue;
                    }

                    LevelFlowController levelFlow = FindComponentInScene<LevelFlowController>(scene);
                    StrongholdSequenceController sequence = FindComponentInScene<StrongholdSequenceController>(scene);
                    BossSpawnPoint bossSpawnPoint = FindComponentInScene<BossSpawnPoint>(scene);

                    if (levelFlow == null || sequence == null || bossSpawnPoint == null)
                    {
                        errors.Add($"[{expectation.sceneName}] Missing levelFlow/sequence/bossSpawnPoint for completion storm test.");
                    }
                    else
                    {
                        sequence.triggerLevelCompleteOnFinish = false;
                        sequence.triggerVictoryOnFinish = false;

                        bool completedStrongholds = CompleteStrongholdChainForBossGate(sequence, expectation.sceneName, errors);
                        if (completedStrongholds)
                        {
                            bool completedBeforeStorm = GetSequenceBoolField(sequence, "levelCompleted", expectation.sceneName, errors);
                            if (completedBeforeStorm)
                            {
                                errors.Add($"[{expectation.sceneName}] levelCompleted should be false before boss defeat storm.");
                            }

                            InvokeSequenceBossDefeat(sequence, bossSpawnPoint, expectation.sceneName, errors);
                            bool completedAfterFirst = GetSequenceBoolField(sequence, "levelCompleted", expectation.sceneName, errors);
                            if (!completedAfterFirst)
                            {
                                errors.Add($"[{expectation.sceneName}] levelCompleted should be true after first boss defeat signal.");
                            }

                            InvokeSequenceBossDefeat(sequence, bossSpawnPoint, expectation.sceneName, errors);
                            bool completedAfterSecond = GetSequenceBoolField(sequence, "levelCompleted", expectation.sceneName, errors);
                            if (!completedAfterSecond)
                            {
                                errors.Add($"[{expectation.sceneName}] levelCompleted should remain true after second boss defeat signal.");
                            }

                            yield return new WaitForSecondsRealtime(0.05f);
                            InvokeSequenceBossDefeat(sequence, bossSpawnPoint, expectation.sceneName, errors);
                            bool completedAfterThird = GetSequenceBoolField(sequence, "levelCompleted", expectation.sceneName, errors);
                            if (!completedAfterThird)
                            {
                                errors.Add($"[{expectation.sceneName}] levelCompleted should remain true after third boss defeat signal.");
                            }

                            if (levelFlow.levelId != expectation.runtimeLevelId)
                            {
                                errors.Add($"[{expectation.sceneName}] levelFlow.levelId mismatch. expected={expectation.runtimeLevelId} actual={levelFlow.levelId}");
                            }
                        }
                    }

                    AsyncOperation unload = SceneManager.UnloadSceneAsync(scene);
                    if (unload != null)
                    {
                        while (!unload.isDone)
                        {
                            yield return null;
                        }
                    }
                }
            }
            finally
            {
                Debug.unityLogger.logEnabled = previousLogEnabled;
                if (baselineScene.IsValid())
                {
                    SceneManager.SetActiveScene(baselineScene);
                }
            }

            if (errors.Count > 0)
            {
                Assert.Fail(string.Join("\n", errors));
            }
        }

        private static bool GetSequenceBoolField(
            StrongholdSequenceController sequence,
            string fieldName,
            string sceneName,
            List<string> errors)
        {
            if (sequence == null)
            {
                errors.Add($"[{sceneName}] sequence is null in GetSequenceBoolField({fieldName}).");
                return false;
            }

            FieldInfo field = typeof(StrongholdSequenceController).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                errors.Add($"[{sceneName}] sequence field not found: {fieldName}.");
                return false;
            }

            object raw = field.GetValue(sequence);
            if (raw is bool value)
            {
                return value;
            }

            errors.Add($"[{sceneName}] sequence field is not bool: {fieldName}.");
            return false;
        }

        private static void AssertApproximately(string sceneName, string fieldName, float expected, float actual, List<string> errors)
        {
            if (Mathf.Abs(expected - actual) > 0.0001f)
            {
                errors.Add($"[{sceneName}] {fieldName} mismatch. expected={expected} actual={actual}");
            }
        }

        private static bool CompleteStrongholdChainForBossGate(StrongholdSequenceController sequence, string sceneName, List<string> errors)
        {
            if (sequence == null)
            {
                errors.Add($"[{sceneName}] sequence is null in CompleteStrongholdChainForBossGate.");
                return false;
            }

            if (sequence.strongholds == null || sequence.strongholds.Count == 0)
            {
                errors.Add($"[{sceneName}] sequence.strongholds is empty.");
                return false;
            }

            MethodInfo handleStrongholdCompleted = typeof(StrongholdSequenceController).GetMethod(
                "HandleStrongholdCompleted",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (handleStrongholdCompleted == null)
            {
                errors.Add($"[{sceneName}] HandleStrongholdCompleted method not found.");
                return false;
            }

            for (int i = 0; i < sequence.strongholds.Count; i++)
            {
                StrongholdController stronghold = sequence.strongholds[i];
                if (stronghold == null)
                {
                    errors.Add($"[{sceneName}] stronghold[{i}] is null.");
                    return false;
                }

                sequence.ActivateStronghold(i);
                handleStrongholdCompleted.Invoke(sequence, new object[] { stronghold });
            }

            return true;
        }

        private static int CountBossDefeatHandlers(BossSpawnPoint spawnPoint, StrongholdSequenceController sequence)
        {
            Delegate callback = spawnPoint != null ? spawnPoint.OnBossDefeated : null;
            if (callback == null || sequence == null)
            {
                return 0;
            }

            int count = 0;
            Delegate[] delegates = callback.GetInvocationList();
            for (int i = 0; i < delegates.Length; i++)
            {
                Delegate item = delegates[i];
                if (ReferenceEquals(item.Target, sequence) &&
                    string.Equals(item.Method.Name, "HandleBossDefeated", StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

        private static void InvokeSequenceBossDefeat(
            StrongholdSequenceController sequence,
            BossSpawnPoint spawnPoint,
            string sceneName,
            List<string> errors)
        {
            if (sequence == null)
            {
                errors.Add($"[{sceneName}] sequence is null in InvokeSequenceBossDefeat.");
                return;
            }

            MethodInfo handleBossDefeated = typeof(StrongholdSequenceController).GetMethod(
                "HandleBossDefeated",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (handleBossDefeated == null)
            {
                errors.Add($"[{sceneName}] HandleBossDefeated method not found.");
                return;
            }

            handleBossDefeated.Invoke(sequence, new object[] { spawnPoint });
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

        private readonly struct BossSceneExpectation
        {
            public readonly string sceneName;
            public readonly string levelId;
            public readonly string bossName;
            public readonly BossPrototypeType prototype;
            public readonly int bossMaxHealth;
            public readonly int bossBaseDamage;
            public readonly float bossKnockback;
            public readonly float bossScaleMultiplier;
            public readonly float phase2Threshold;
            public readonly float phase3Threshold;
            public readonly float breakDuration;
            public readonly float breakCooldown;
            public readonly float breakDamageMultiplier;
            public readonly float staggerMax;
            public readonly float staggerPerDamage;
            public readonly float attackInterval;
            public readonly float decisionInterval;
            public readonly int queuedAttackLimit;
            public readonly float immediateRepeatPenalty;
            public readonly string phase2TransitionOpenerId;
            public readonly string phase3TransitionOpenerId;

            public BossSceneExpectation(
                string sceneName,
                string levelId,
                string bossName,
                BossPrototypeType prototype,
                int bossMaxHealth,
                int bossBaseDamage,
                float bossKnockback,
                float bossScaleMultiplier,
                float phase2Threshold,
                float phase3Threshold,
                float breakDuration,
                float breakCooldown,
                float breakDamageMultiplier,
                float staggerMax,
                float staggerPerDamage,
                float attackInterval,
                float decisionInterval,
                int queuedAttackLimit,
                float immediateRepeatPenalty,
                string phase2TransitionOpenerId,
                string phase3TransitionOpenerId)
            {
                this.sceneName = sceneName;
                this.levelId = levelId;
                this.bossName = bossName;
                this.prototype = prototype;
                this.bossMaxHealth = bossMaxHealth;
                this.bossBaseDamage = bossBaseDamage;
                this.bossKnockback = bossKnockback;
                this.bossScaleMultiplier = bossScaleMultiplier;
                this.phase2Threshold = phase2Threshold;
                this.phase3Threshold = phase3Threshold;
                this.breakDuration = breakDuration;
                this.breakCooldown = breakCooldown;
                this.breakDamageMultiplier = breakDamageMultiplier;
                this.staggerMax = staggerMax;
                this.staggerPerDamage = staggerPerDamage;
                this.attackInterval = attackInterval;
                this.decisionInterval = decisionInterval;
                this.queuedAttackLimit = queuedAttackLimit;
                this.immediateRepeatPenalty = immediateRepeatPenalty;
                this.phase2TransitionOpenerId = phase2TransitionOpenerId;
                this.phase3TransitionOpenerId = phase3TransitionOpenerId;
            }
        }

        private readonly struct SceneCompletionExpectation
        {
            public readonly string sceneName;
            public readonly int runtimeLevelId;

            public SceneCompletionExpectation(string sceneName, int runtimeLevelId)
            {
                this.sceneName = sceneName;
                this.runtimeLevelId = runtimeLevelId;
            }
        }
    }
}
