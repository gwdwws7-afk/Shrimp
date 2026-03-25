using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ThirdPersonController.Tests
{
    public class BossSceneLocalBindingRegressionTests
    {
        private static readonly string[] BossScenes =
        {
            "Level_03_ThermalVents",
            "Level_04_CoralGrove"
        };

        [UnityTest, Timeout(300000)]
        public IEnumerator RuntimeConfigurator_BindsBossSpawnPoint_FromOwnScene_WhenMultipleBossScenesLoaded()
        {
            var loadedScenes = new List<Scene>();
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
                    errors.Add($"[{sceneName}] Scene not loaded after LoadSceneAsync.");
                    continue;
                }

                loadedScenes.Add(scene);
            }

            yield return null;
            yield return null;

            for (int i = 0; i < loadedScenes.Count; i++)
            {
                Scene scene = loadedScenes[i];
                string sceneName = scene.name;

                LevelRuntimeConfigurator runtime = FindComponentInScene<LevelRuntimeConfigurator>(scene);
                StrongholdSequenceController sequence = FindComponentInScene<StrongholdSequenceController>(scene);
                List<BossSpawnPoint> bossSpawnPoints = FindComponentsInScene<BossSpawnPoint>(scene);

                if (runtime == null)
                {
                    errors.Add($"[{sceneName}] Missing LevelRuntimeConfigurator.");
                    continue;
                }

                if (sequence == null)
                {
                    errors.Add($"[{sceneName}] Missing StrongholdSequenceController.");
                    continue;
                }

                if (bossSpawnPoints.Count == 0)
                {
                    errors.Add($"[{sceneName}] Missing BossSpawnPoint.");
                    continue;
                }

                if (runtime.levelData == null || !runtime.levelData.overrideBossSettings)
                {
                    continue;
                }

                sequence.ConfigureBossGate(true, null);
                runtime.bossSpawnPoint = null;
                runtime.sequenceController = sequence;

                bool oldEnsureRuntimeWiring = runtime.ensureRuntimeWiring;
                bool oldApplyStrongholds = runtime.applyStrongholds;
                bool oldApplyQuests = runtime.applyQuests;
                bool oldApplyRewards = runtime.applyRewards;

                runtime.ensureRuntimeWiring = false;
                runtime.applyStrongholds = false;
                runtime.applyQuests = false;
                runtime.applyRewards = false;

                runtime.Apply();
                yield return null;

                runtime.ensureRuntimeWiring = oldEnsureRuntimeWiring;
                runtime.applyStrongholds = oldApplyStrongholds;
                runtime.applyQuests = oldApplyQuests;
                runtime.applyRewards = oldApplyRewards;

                if (!sequence.deferCompletionUntilBoss)
                {
                    errors.Add($"[{sceneName}] deferCompletionUntilBoss should be true after Apply.");
                }

                if (sequence.bossSpawnPoint == null)
                {
                    errors.Add($"[{sceneName}] sequence.bossSpawnPoint should not be null after Apply.");
                    continue;
                }

                if (!bossSpawnPoints.Contains(sequence.bossSpawnPoint))
                {
                    errors.Add($"[{sceneName}] sequence.bossSpawnPoint references object outside this scene.");
                }
            }

            for (int i = loadedScenes.Count - 1; i >= 0; i--)
            {
                Scene scene = loadedScenes[i];
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    continue;
                }

                AsyncOperation unload = SceneManager.UnloadSceneAsync(scene);
                if (unload == null)
                {
                    errors.Add($"[{scene.name}] UnloadSceneAsync returned null.");
                    continue;
                }

                while (!unload.isDone)
                {
                    yield return null;
                }
            }

            if (errors.Count > 0)
            {
                Assert.Fail(string.Join("\n", errors));
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
    }
}
