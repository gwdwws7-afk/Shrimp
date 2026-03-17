using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ThirdPersonController.Tests
{
    public class LevelSceneGateTests
    {
        private static readonly LevelGateExpectation[] Expectations =
        {
            new LevelGateExpectation("Level_02_WreckedStation", "LEVEL_02", 102, false),
            new LevelGateExpectation("Level_03_ThermalVents", "LEVEL_03", 103, true),
            new LevelGateExpectation("Level_04_CoralGrove", "LEVEL_04", 104, true),
            new LevelGateExpectation("Level_05_SunkenCity", "LEVEL_05", 105, true),
            new LevelGateExpectation("Level_06_BlackTidePipes", "LEVEL_06", 106, true),
            new LevelGateExpectation("Level_07_AbyssHangar", "LEVEL_07", 107, true),
            new LevelGateExpectation("Level_08_MoltenRift", "LEVEL_08", 108, true),
            new LevelGateExpectation("Level_09_StillTideSanctum", "LEVEL_09", 109, true),
            new LevelGateExpectation("Level_10_HiveCore", "LEVEL_10", 110, true)
        };

        [UnityTest, Timeout(300000)]
        public IEnumerator LevelScenes_02To10_CoreWiringGate_Passes()
        {
            Scene baselineScene = SceneManager.GetActiveScene();
            var errors = new List<string>();

            for (int i = 0; i < Expectations.Length; i++)
            {
                LevelGateExpectation expectation = Expectations[i];
                yield return ValidateScene(expectation, errors);
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

        private static IEnumerator ValidateScene(LevelGateExpectation expectation, List<string> errors)
        {
            AsyncOperation load = SceneManager.LoadSceneAsync(expectation.sceneName, LoadSceneMode.Additive);
            if (load == null)
            {
                errors.Add($"[{expectation.sceneName}] LoadSceneAsync returned null.");
                yield break;
            }

            while (!load.isDone)
            {
                yield return null;
            }

            yield return null;
            yield return null;

            Scene scene = SceneManager.GetSceneByName(expectation.sceneName);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                errors.Add($"[{expectation.sceneName}] Scene not loaded after LoadSceneAsync.");
                yield break;
            }

            int buildIndex = SceneUtility.GetBuildIndexByScenePath(scene.path);
            if (buildIndex < 0)
            {
                errors.Add($"[{expectation.sceneName}] Scene path not found in BuildSettings: {scene.path}");
            }

            LevelFlowController levelFlow = FindComponentInScene<LevelFlowController>(scene);
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
                    if (!string.Equals(levelFlow.levelData.levelId, expectation.levelId))
                    {
                        errors.Add(
                            $"[{expectation.sceneName}] LevelData.levelId mismatch. expected={expectation.levelId} actual={levelFlow.levelData.levelId}");
                    }

                    if (!string.Equals(levelFlow.levelData.sceneName, expectation.sceneName))
                    {
                        errors.Add(
                            $"[{expectation.sceneName}] LevelData.sceneName mismatch. expected={expectation.sceneName} actual={levelFlow.levelData.sceneName}");
                    }

                    if (levelFlow.levelData.overrideBossSettings != expectation.expectBossGate)
                    {
                        errors.Add(
                            $"[{expectation.sceneName}] LevelData.overrideBossSettings mismatch. expected={expectation.expectBossGate} actual={levelFlow.levelData.overrideBossSettings}");
                    }
                }

                if (levelFlow.levelId != expectation.runtimeLevelId)
                {
                    errors.Add(
                        $"[{expectation.sceneName}] LevelFlow.levelId mismatch. expected={expectation.runtimeLevelId} actual={levelFlow.levelId}");
                }
            }

            LevelRuntimeConfigurator runtimeConfigurator = FindComponentInScene<LevelRuntimeConfigurator>(scene);
            if (runtimeConfigurator != null && levelFlow != null && runtimeConfigurator.levelFlow != null && runtimeConfigurator.levelFlow != levelFlow)
            {
                errors.Add($"[{expectation.sceneName}] RuntimeConfigurator.levelFlow does not reference scene LevelFlowController.");
            }

            StrongholdSequenceController sequence = FindComponentInScene<StrongholdSequenceController>(scene);
            if (sequence == null)
            {
                errors.Add($"[{expectation.sceneName}] Missing StrongholdSequenceController.");
            }
            else
            {
                if (sequence.strongholds == null || sequence.strongholds.Count == 0)
                {
                    errors.Add($"[{expectation.sceneName}] StrongholdSequence.strongholds is empty.");
                }

                if (sequence.deferCompletionUntilBoss != expectation.expectBossGate)
                {
                    errors.Add(
                        $"[{expectation.sceneName}] deferCompletionUntilBoss mismatch. expected={expectation.expectBossGate} actual={sequence.deferCompletionUntilBoss}");
                }
            }

            BossSpawnPoint bossSpawnPoint = FindComponentInScene<BossSpawnPoint>(scene);
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
            }

            if (sequence != null)
            {
                if (expectation.expectBossGate)
                {
                    if (sequence.bossSpawnPoint != bossSpawnPoint)
                    {
                        errors.Add($"[{expectation.sceneName}] Boss gate enabled but sequence.bossSpawnPoint is not scene BossSpawnPoint.");
                    }
                }
                else if (sequence.bossSpawnPoint != null)
                {
                    errors.Add($"[{expectation.sceneName}] Boss gate disabled but sequence.bossSpawnPoint is not null.");
                }
            }

            AsyncOperation unload = SceneManager.UnloadSceneAsync(scene);
            if (unload == null)
            {
                errors.Add($"[{expectation.sceneName}] UnloadSceneAsync returned null.");
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

        private readonly struct LevelGateExpectation
        {
            public readonly string sceneName;
            public readonly string levelId;
            public readonly int runtimeLevelId;
            public readonly bool expectBossGate;

            public LevelGateExpectation(string sceneName, string levelId, int runtimeLevelId, bool expectBossGate)
            {
                this.sceneName = sceneName;
                this.levelId = levelId;
                this.runtimeLevelId = runtimeLevelId;
                this.expectBossGate = expectBossGate;
            }
        }
    }
}
