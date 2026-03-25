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
    public class BossLevelFlowEndToEndRegressionTests
    {
        private static readonly string[] BossGatedScenes =
        {
            "Level_08_MoltenRift",
            "Level_09_StillTideSanctum",
            "Level_10_HiveCore"
        };

        private static readonly MethodInfo HandleStrongholdCompletedMethod =
            typeof(StrongholdSequenceController).GetMethod(
                "HandleStrongholdCompleted",
                BindingFlags.Instance | BindingFlags.NonPublic);

        [UnityTest, Timeout(400000)]
        public IEnumerator BossGatedScenes_StrongholdThenBoss_TriggersSingleCompletionAndVictory()
        {
            yield return RunBossFlowScenario(simulatePlayerDeathInterruption: false);
        }

        [UnityTest, Timeout(400000)]
        public IEnumerator BossGatedScenes_PlayerDeathBeforeBoss_DoesNotBypassCompletionGate()
        {
            yield return RunBossFlowScenario(simulatePlayerDeathInterruption: true);
        }

        private static IEnumerator RunBossFlowScenario(bool simulatePlayerDeathInterruption)
        {
            Assert.NotNull(HandleStrongholdCompletedMethod,
                "StrongholdSequenceController.HandleStrongholdCompleted should exist for regression simulation.");

            Scene baselineScene = SceneManager.GetActiveScene();
            var errors = new List<string>();

            for (int i = 0; i < BossGatedScenes.Length; i++)
            {
                string sceneName = BossGatedScenes[i];
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
                LevelRuntimeConfigurator runtimeConfigurator = FindComponentInScene<LevelRuntimeConfigurator>(scene);
                StrongholdSequenceController sequence = FindComponentInScene<StrongholdSequenceController>(scene);

                if (levelFlow == null || levelFlow.levelData == null)
                {
                    errors.Add($"[{sceneName}] Missing LevelFlowController/LevelData.");
                    yield return UnloadScene(scene, errors);
                    continue;
                }

                if (!levelFlow.levelData.overrideBossSettings)
                {
                    errors.Add($"[{sceneName}] Expected boss-gated level but overrideBossSettings is false.");
                    yield return UnloadScene(scene, errors);
                    continue;
                }

                if (runtimeConfigurator == null)
                {
                    errors.Add($"[{sceneName}] Missing LevelRuntimeConfigurator.");
                    yield return UnloadScene(scene, errors);
                    continue;
                }

                if (sequence == null)
                {
                    errors.Add($"[{sceneName}] Missing StrongholdSequenceController.");
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
                yield return null;
                yield return null;

                runtimeConfigurator.ensureRuntimeWiring = oldEnsureRuntimeWiring;
                runtimeConfigurator.applyStrongholds = oldApplyStrongholds;
                runtimeConfigurator.applyQuests = oldApplyQuests;
                runtimeConfigurator.applyRewards = oldApplyRewards;

                if (!sequence.deferCompletionUntilBoss)
                {
                    errors.Add($"[{sceneName}] deferCompletionUntilBoss should be true after runtime apply.");
                    yield return UnloadScene(scene, errors);
                    continue;
                }

                if (sequence.bossSpawnPoint == null)
                {
                    errors.Add($"[{sceneName}] sequence.bossSpawnPoint is null after runtime apply.");
                    yield return UnloadScene(scene, errors);
                    continue;
                }

                if (sequence.strongholds == null || sequence.strongholds.Count == 0)
                {
                    errors.Add($"[{sceneName}] sequence.strongholds is empty.");
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
                        errors.Add($"[{sceneName}] Level completed before boss defeat (count={completedCount}).");
                    }

                    if (!simulatePlayerDeathInterruption && gameOverCount != 0)
                    {
                        errors.Add($"[{sceneName}] GameOver fired before boss defeat in normal flow (count={gameOverCount}).");
                    }

                    if (simulatePlayerDeathInterruption)
                    {
                        GameEvents.PlayerDeath();
                        yield return null;

                        if (completedCount != 0)
                        {
                            errors.Add($"[{sceneName}] PlayerDeath interruption bypassed boss completion gate.");
                        }
                    }

                    sequence.bossSpawnPoint.OnBossDefeated?.Invoke(sequence.bossSpawnPoint);
                    yield return null;

                    if (completedCount <= 0)
                    {
                        errors.Add($"[{sceneName}] Boss defeat did not trigger level completion.");
                    }

                    if (gameOverCount <= 0)
                    {
                        errors.Add($"[{sceneName}] Boss defeat did not trigger victory game-over.");
                    }
                    else if (!lastVictory)
                    {
                        errors.Add($"[{sceneName}] Last game-over after boss defeat is not victory.");
                    }

                    int completedAfterFirstDefeat = completedCount;
                    int gameOverAfterFirstDefeat = gameOverCount;

                    sequence.bossSpawnPoint.OnBossDefeated?.Invoke(sequence.bossSpawnPoint);
                    yield return null;

                    if (completedCount != completedAfterFirstDefeat)
                    {
                        errors.Add($"[{sceneName}] Duplicate boss defeat produced duplicate level completion.");
                    }

                    if (gameOverCount != gameOverAfterFirstDefeat)
                    {
                        errors.Add($"[{sceneName}] Duplicate boss defeat produced duplicate game-over.");
                    }
                }
                finally
                {
                    GameEvents.OnLevelCompleted -= onLevelCompleted;
                    GameEvents.OnGameOver -= onGameOver;
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
