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
    public class BossChaosStressRegressionTests
    {
        private static readonly string[] BossScenes03To10 =
        {
            "Level_03_ThermalVents",
            "Level_04_CoralGrove",
            "Level_05_SunkenCity",
            "Level_06_BlackTidePipes",
            "Level_07_AbyssHangar",
            "Level_08_MoltenRift",
            "Level_09_StillTideSanctum",
            "Level_10_HiveCore"
        };

        private static readonly MethodInfo HandleStrongholdCompletedMethod =
            typeof(StrongholdSequenceController).GetMethod(
                "HandleStrongholdCompleted",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo HandleBossDefeatedMethod =
            typeof(StrongholdSequenceController).GetMethod(
                "HandleBossDefeated",
                BindingFlags.Instance | BindingFlags.NonPublic);

        [UnityTest, Timeout(1200000)]
        public IEnumerator BossScenes03To10_LowFpsJitter_RebindStormAndInterruptions_BossGateRemainsStable()
        {
            Assert.NotNull(HandleStrongholdCompletedMethod, "HandleStrongholdCompleted method should exist.");
            Assert.NotNull(HandleBossDefeatedMethod, "HandleBossDefeated method should exist.");

            Scene baselineScene = SceneManager.GetActiveScene();
            float originalTimeScale = Time.timeScale;
            bool previousLogEnabled = Debug.unityLogger.logEnabled;
            Debug.unityLogger.logEnabled = false;

            var errors = new List<string>();
            try
            {
                for (int i = 0; i < BossScenes03To10.Length; i++)
                {
                    string sceneName = BossScenes03To10[i];
                    SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
                    yield return null;
                    yield return null;

                    Scene scene = SceneManager.GetSceneByName(sceneName);
                    if (!scene.IsValid() || !scene.isLoaded)
                    {
                        errors.Add($"[{sceneName}] Scene should be loaded.");
                        continue;
                    }

                    LevelFlowController levelFlow = FindComponentInScene<LevelFlowController>(scene);
                    StrongholdSequenceController sequence = FindComponentInScene<StrongholdSequenceController>(scene);
                    BossSpawnPoint bossSpawnPoint = FindComponentInScene<BossSpawnPoint>(scene);

                    if (levelFlow == null || levelFlow.levelData == null || sequence == null || bossSpawnPoint == null)
                    {
                        errors.Add($"[{sceneName}] Missing levelFlow/levelData/sequence/bossSpawnPoint.");
                        Time.timeScale = 1f;
                        yield return UnloadScene(scene, errors);
                        continue;
                    }

                    if (!levelFlow.levelData.overrideBossSettings)
                    {
                        errors.Add($"[{sceneName}] overrideBossSettings should be true for chaos route.");
                    }

                    sequence.triggerLevelCompleteOnFinish = false;
                    sequence.triggerVictoryOnFinish = false;

                    if (!sequence.deferCompletionUntilBoss)
                    {
                        errors.Add($"[{sceneName}] deferCompletionUntilBoss should be true.");
                    }

                    if (sequence.strongholds == null || sequence.strongholds.Count == 0)
                    {
                        errors.Add($"[{sceneName}] sequence.strongholds is empty.");
                    }
                    else
                    {
                        List<StrongholdController> stableStrongholds = new List<StrongholdController>(sequence.strongholds);
                        Time.timeScale = (i % 2 == 0) ? 0.38f : 0.56f;

                        for (int storm = 0; storm < 20; storm++)
                        {
                            sequence.ConfigureStrongholds(new List<StrongholdController>(stableStrongholds));
                            sequence.ConfigureBossGate(true, bossSpawnPoint);

                            int bossHandlers = CountBossDefeatHandlers(bossSpawnPoint, sequence);
                            if (bossHandlers != 1)
                            {
                                errors.Add(
                                    $"[{sceneName}] boss handler mismatch after storm={storm + 1}. expected=1 actual={bossHandlers}");
                            }

                            for (int s = 0; s < stableStrongholds.Count; s++)
                            {
                                StrongholdController stronghold = stableStrongholds[s];
                                int handlers = CountStrongholdCompletedHandlers(stronghold, sequence);
                                if (handlers != 1)
                                {
                                    errors.Add(
                                        $"[{sceneName}] stronghold[{s}] handler mismatch after storm={storm + 1}. expected=1 actual={handlers}");
                                }
                            }
                        }

                        yield return new WaitForSecondsRealtime(0.12f);
                        bool completedStrongholds = CompleteStrongholdChainForBossGate(sequence, sceneName, errors);
                        if (completedStrongholds)
                        {
                            bool completedBeforeDefeat = GetSequenceBoolField(sequence, "levelCompleted", sceneName, errors);
                            if (completedBeforeDefeat)
                            {
                                errors.Add($"[{sceneName}] levelCompleted should be false before boss defeat.");
                            }

                            bool triggerInterruption = (i % 3) != 1;
                            if (triggerInterruption)
                            {
                                GameEvents.PlayerDeath();
                                yield return null;
                                bool completedAfterInterruption = GetSequenceBoolField(sequence, "levelCompleted", sceneName, errors);
                                if (completedAfterInterruption)
                                {
                                    errors.Add($"[{sceneName}] PlayerDeath interruption bypassed boss completion gate.");
                                }
                            }

                            InvokeSequenceBossDefeat(sequence, bossSpawnPoint, sceneName, errors);
                            bool completedAfterFirst = GetSequenceBoolField(sequence, "levelCompleted", sceneName, errors);
                            if (!completedAfterFirst)
                            {
                                errors.Add($"[{sceneName}] levelCompleted should be true after first boss defeat.");
                            }

                            InvokeSequenceBossDefeat(sequence, bossSpawnPoint, sceneName, errors);
                            bool completedAfterSecond = GetSequenceBoolField(sequence, "levelCompleted", sceneName, errors);
                            if (!completedAfterSecond)
                            {
                                errors.Add($"[{sceneName}] levelCompleted should remain true after repeated boss defeat.");
                            }
                        }
                    }

                    Time.timeScale = 1f;
                    yield return UnloadScene(scene, errors);
                }
            }
            finally
            {
                Debug.unityLogger.logEnabled = previousLogEnabled;
                Time.timeScale = originalTimeScale;
                if (baselineScene.IsValid() && baselineScene.isLoaded)
                {
                    SceneManager.SetActiveScene(baselineScene);
                }
            }

            if (errors.Count > 0)
            {
                Assert.Fail(string.Join("\n", errors));
            }
        }

        [UnityTest, Timeout(1200000)]
        public IEnumerator BossScenes03To10_ReentryShuffle_BossDefeatEventStorm_RemainsSingleCompletionState()
        {
            Assert.NotNull(HandleStrongholdCompletedMethod, "HandleStrongholdCompleted method should exist.");
            Assert.NotNull(HandleBossDefeatedMethod, "HandleBossDefeated method should exist.");

            Scene baselineScene = SceneManager.GetActiveScene();
            bool previousLogEnabled = Debug.unityLogger.logEnabled;
            Debug.unityLogger.logEnabled = false;
            var errors = new List<string>();

            string[] route =
            {
                "Level_03_ThermalVents",
                "Level_05_SunkenCity",
                "Level_07_AbyssHangar",
                "Level_04_CoralGrove",
                "Level_06_BlackTidePipes",
                "Level_08_MoltenRift",
                "Level_10_HiveCore",
                "Level_09_StillTideSanctum",
                "Level_03_ThermalVents"
            };

            try
            {
                for (int i = 0; i < route.Length; i++)
                {
                    string sceneName = route[i];
                    SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
                    yield return null;
                    yield return null;

                    Scene scene = SceneManager.GetSceneByName(sceneName);
                    if (!scene.IsValid() || !scene.isLoaded)
                    {
                        errors.Add($"[{sceneName}] Scene should be loaded at route step {i + 1}.");
                        continue;
                    }

                    LevelFlowController levelFlow = FindComponentInScene<LevelFlowController>(scene);
                    StrongholdSequenceController sequence = FindComponentInScene<StrongholdSequenceController>(scene);
                    BossSpawnPoint bossSpawnPoint = FindComponentInScene<BossSpawnPoint>(scene);

                    if (levelFlow == null || levelFlow.levelData == null || sequence == null || bossSpawnPoint == null)
                    {
                        errors.Add($"[{sceneName}] Missing levelFlow/levelData/sequence/bossSpawnPoint at route step {i + 1}.");
                        yield return UnloadScene(scene, errors);
                        continue;
                    }

                    if (!levelFlow.levelData.overrideBossSettings)
                    {
                        errors.Add($"[{sceneName}] overrideBossSettings should be true at route step {i + 1}.");
                    }

                    sequence.triggerLevelCompleteOnFinish = false;
                    sequence.triggerVictoryOnFinish = false;

                    bool completedStrongholds = CompleteStrongholdChainForBossGate(sequence, sceneName, errors);
                    if (completedStrongholds)
                    {
                        bool completedBeforeStorm = GetSequenceBoolField(sequence, "levelCompleted", sceneName, errors);
                        if (completedBeforeStorm)
                        {
                            errors.Add($"[{sceneName}] levelCompleted should be false before boss defeat storm.");
                        }

                        for (int storm = 0; storm < 4; storm++)
                        {
                            InvokeSequenceBossDefeat(sequence, bossSpawnPoint, sceneName, errors);
                            yield return null;
                            bool completedAfterSignal = GetSequenceBoolField(sequence, "levelCompleted", sceneName, errors);
                            if (!completedAfterSignal)
                            {
                                errors.Add($"[{sceneName}] levelCompleted should remain true during boss defeat storm (signal={storm + 1}).");
                            }
                        }
                    }

                    yield return UnloadScene(scene, errors);
                }
            }
            finally
            {
                Debug.unityLogger.logEnabled = previousLogEnabled;
                if (baselineScene.IsValid() && baselineScene.isLoaded)
                {
                    SceneManager.SetActiveScene(baselineScene);
                }
            }

            if (errors.Count > 0)
            {
                Assert.Fail(string.Join("\n", errors));
            }
        }

        private static bool CompleteStrongholdChainForBossGate(
            StrongholdSequenceController sequence,
            string sceneName,
            List<string> errors)
        {
            if (sequence == null)
            {
                errors.Add($"[{sceneName}] sequence is null.");
                return false;
            }

            if (sequence.strongholds == null || sequence.strongholds.Count == 0)
            {
                errors.Add($"[{sceneName}] sequence.strongholds is empty.");
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
                HandleStrongholdCompletedMethod.Invoke(sequence, new object[] { stronghold });
            }

            return true;
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

        private static int CountStrongholdCompletedHandlers(StrongholdController stronghold, StrongholdSequenceController sequence)
        {
            if (stronghold == null || sequence == null)
            {
                return 0;
            }

            FieldInfo field = typeof(StrongholdController).GetField(
                "OnStrongholdCompleted",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                return 0;
            }

            Delegate callback = field.GetValue(stronghold) as Delegate;
            if (callback == null)
            {
                return 0;
            }

            int count = 0;
            Delegate[] delegates = callback.GetInvocationList();
            for (int i = 0; i < delegates.Length; i++)
            {
                Delegate item = delegates[i];
                if (ReferenceEquals(item.Target, sequence) &&
                    string.Equals(item.Method.Name, "HandleStrongholdCompleted", StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
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

            if (HandleBossDefeatedMethod == null)
            {
                errors.Add($"[{sceneName}] HandleBossDefeated method not found.");
                return;
            }

            HandleBossDefeatedMethod.Invoke(sequence, new object[] { spawnPoint });
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
