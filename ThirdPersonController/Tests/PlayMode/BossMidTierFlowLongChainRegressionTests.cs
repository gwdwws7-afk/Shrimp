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
    public class BossMidTierFlowLongChainRegressionTests
    {
        private static readonly string[] MidTierBossScenes =
        {
            "Level_03_ThermalVents",
            "Level_04_CoralGrove",
            "Level_05_SunkenCity",
            "Level_06_BlackTidePipes",
            "Level_07_AbyssHangar"
        };

        private static readonly MethodInfo HandleStrongholdCompletedMethod =
            typeof(StrongholdSequenceController).GetMethod(
                "HandleStrongholdCompleted",
                BindingFlags.Instance | BindingFlags.NonPublic);

        [UnityTest, Timeout(700000)]
        public IEnumerator Level03To07_BossGateLongChain_ForwardSweep_TriggersSingleCompletionAndVictory()
        {
            yield return RunLongChainScenario(simulatePlayerDeathInterruption: false, includeReverseReentryPass: false);
        }

        [UnityTest, Timeout(1000000)]
        public IEnumerator Level03To07_BossGateLongChain_ReentryWithInterruption_RemainsStableAndNoDuplicateHandlers()
        {
            yield return RunLongChainScenario(simulatePlayerDeathInterruption: true, includeReverseReentryPass: true);
        }

        private static IEnumerator RunLongChainScenario(bool simulatePlayerDeathInterruption, bool includeReverseReentryPass)
        {
            Assert.NotNull(
                HandleStrongholdCompletedMethod,
                "StrongholdSequenceController.HandleStrongholdCompleted should exist for long-chain regression simulation.");

            Scene baselineScene = SceneManager.GetActiveScene();
            var errors = new List<string>();
            List<string[]> scenePasses = BuildScenePasses(includeReverseReentryPass);

            for (int passIndex = 0; passIndex < scenePasses.Count; passIndex++)
            {
                string[] passScenes = scenePasses[passIndex];
                for (int sceneIndex = 0; sceneIndex < passScenes.Length; sceneIndex++)
                {
                    string sceneName = passScenes[sceneIndex];
                    AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                    if (load == null)
                    {
                        errors.Add($"[pass={passIndex + 1}:{sceneName}] LoadSceneAsync returned null.");
                        continue;
                    }

                    while (!load.isDone)
                    {
                        yield return null;
                    }

                    Scene scene = SceneManager.GetSceneByName(sceneName);
                    if (!scene.IsValid() || !scene.isLoaded)
                    {
                        errors.Add($"[pass={passIndex + 1}:{sceneName}] Scene not loaded.");
                        continue;
                    }

                    SceneManager.SetActiveScene(scene);
                    yield return null;
                    yield return null;

                    LevelFlowController levelFlow = FindComponentInScene<LevelFlowController>(scene);
                    LevelRuntimeConfigurator runtimeConfigurator = FindComponentInScene<LevelRuntimeConfigurator>(scene);
                    StrongholdSequenceController sequence = FindComponentInScene<StrongholdSequenceController>(scene);

                    if (levelFlow == null || levelFlow.levelData == null)
                    {
                        errors.Add($"[pass={passIndex + 1}:{sceneName}] Missing LevelFlowController/LevelData.");
                        yield return UnloadScene(scene, errors);
                        continue;
                    }

                    if (!levelFlow.levelData.overrideBossSettings)
                    {
                        errors.Add($"[pass={passIndex + 1}:{sceneName}] Expected boss-gated level but overrideBossSettings is false.");
                        yield return UnloadScene(scene, errors);
                        continue;
                    }

                    if (runtimeConfigurator == null)
                    {
                        errors.Add($"[pass={passIndex + 1}:{sceneName}] Missing LevelRuntimeConfigurator.");
                        yield return UnloadScene(scene, errors);
                        continue;
                    }

                    if (sequence == null)
                    {
                        errors.Add($"[pass={passIndex + 1}:{sceneName}] Missing StrongholdSequenceController.");
                        yield return UnloadScene(scene, errors);
                        continue;
                    }

                    bool oldEnsureRuntimeWiring = runtimeConfigurator.ensureRuntimeWiring;
                    bool oldApplyStrongholds = runtimeConfigurator.applyStrongholds;
                    bool oldApplyQuests = runtimeConfigurator.applyQuests;
                    bool oldApplyRewards = runtimeConfigurator.applyRewards;

                    runtimeConfigurator.ensureRuntimeWiring = false;
                    runtimeConfigurator.applyStrongholds = true;
                    runtimeConfigurator.applyQuests = false;
                    runtimeConfigurator.applyRewards = false;

                    runtimeConfigurator.Apply();
                    runtimeConfigurator.Apply();
                    yield return null;
                    yield return null;

                    runtimeConfigurator.ensureRuntimeWiring = oldEnsureRuntimeWiring;
                    runtimeConfigurator.applyStrongholds = oldApplyStrongholds;
                    runtimeConfigurator.applyQuests = oldApplyQuests;
                    runtimeConfigurator.applyRewards = oldApplyRewards;

                    if (!sequence.deferCompletionUntilBoss)
                    {
                        errors.Add($"[pass={passIndex + 1}:{sceneName}] deferCompletionUntilBoss should be true after runtime apply.");
                        yield return UnloadScene(scene, errors);
                        continue;
                    }

                    if (sequence.bossSpawnPoint == null)
                    {
                        errors.Add($"[pass={passIndex + 1}:{sceneName}] sequence.bossSpawnPoint is null after runtime apply.");
                        yield return UnloadScene(scene, errors);
                        continue;
                    }

                    int bossHandlers = CountBossDefeatHandlers(sequence.bossSpawnPoint, sequence);
                    if (bossHandlers != 1)
                    {
                        errors.Add(
                            $"[pass={passIndex + 1}:{sceneName}] expected exactly one HandleBossDefeated handler, actual={bossHandlers}.");
                    }

                    if (sequence.strongholds == null || sequence.strongholds.Count == 0)
                    {
                        errors.Add($"[pass={passIndex + 1}:{sceneName}] sequence.strongholds is empty.");
                        yield return UnloadScene(scene, errors);
                        continue;
                    }

                    int completedCount = 0;
                    int gameOverCount = 0;
                    bool lastVictory = false;
                    Action<int> onLevelCompleted = _ => completedCount++;
                    Action<bool> onGameOver = victory =>
                    {
                        gameOverCount++;
                        lastVictory = victory;
                    };

                    GameEvents.OnLevelCompleted += onLevelCompleted;
                    GameEvents.OnGameOver += onGameOver;

                    try
                    {
                        SimulateStrongholdChain(sequence);

                        if (completedCount != 0)
                        {
                            errors.Add($"[pass={passIndex + 1}:{sceneName}] Level completed before boss defeat (count={completedCount}).");
                        }

                        bool interruptThisScene = simulatePlayerDeathInterruption && ((sceneIndex + passIndex) % 2 == 0);
                        if (interruptThisScene)
                        {
                            GameEvents.PlayerDeath();
                            yield return null;
                            if (completedCount != 0)
                            {
                                errors.Add($"[pass={passIndex + 1}:{sceneName}] PlayerDeath interruption bypassed boss completion gate.");
                            }
                        }

                        sequence.bossSpawnPoint.OnBossDefeated?.Invoke(sequence.bossSpawnPoint);
                        yield return null;

                        if (completedCount <= 0)
                        {
                            errors.Add($"[pass={passIndex + 1}:{sceneName}] Boss defeat did not trigger level completion.");
                        }

                        if (gameOverCount <= 0)
                        {
                            errors.Add($"[pass={passIndex + 1}:{sceneName}] Boss defeat did not trigger victory game-over.");
                        }
                        else if (!lastVictory)
                        {
                            errors.Add($"[pass={passIndex + 1}:{sceneName}] Last game-over after boss defeat is not victory.");
                        }

                        int completedAfterFirstDefeat = completedCount;
                        int gameOverAfterFirstDefeat = gameOverCount;

                        sequence.bossSpawnPoint.OnBossDefeated?.Invoke(sequence.bossSpawnPoint);
                        yield return null;

                        if (completedCount != completedAfterFirstDefeat)
                        {
                            errors.Add($"[pass={passIndex + 1}:{sceneName}] Duplicate boss defeat produced duplicate level completion.");
                        }

                        if (gameOverCount != gameOverAfterFirstDefeat)
                        {
                            errors.Add($"[pass={passIndex + 1}:{sceneName}] Duplicate boss defeat produced duplicate game-over.");
                        }
                    }
                    finally
                    {
                        GameEvents.OnLevelCompleted -= onLevelCompleted;
                        GameEvents.OnGameOver -= onGameOver;
                    }

                    yield return UnloadScene(scene, errors);
                }
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

        private static List<string[]> BuildScenePasses(bool includeReverseReentryPass)
        {
            var passes = new List<string[]>();
            string[] forward = new string[MidTierBossScenes.Length];
            Array.Copy(MidTierBossScenes, forward, MidTierBossScenes.Length);
            passes.Add(forward);

            if (includeReverseReentryPass)
            {
                string[] reverse = new string[MidTierBossScenes.Length];
                for (int i = 0; i < MidTierBossScenes.Length; i++)
                {
                    reverse[i] = MidTierBossScenes[MidTierBossScenes.Length - 1 - i];
                }

                passes.Add(reverse);
            }

            return passes;
        }

        private static void SimulateStrongholdChain(StrongholdSequenceController sequence)
        {
            if (sequence == null || sequence.strongholds == null)
            {
                return;
            }

            for (int i = 0; i < sequence.strongholds.Count; i++)
            {
                StrongholdController stronghold = sequence.strongholds[i];
                if (stronghold == null)
                {
                    continue;
                }

                sequence.ActivateStronghold(i);
                HandleStrongholdCompletedMethod.Invoke(sequence, new object[] { stronghold });
            }
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
