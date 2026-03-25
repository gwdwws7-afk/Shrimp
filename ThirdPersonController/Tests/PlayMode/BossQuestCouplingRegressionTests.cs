using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ThirdPersonController.Tests
{
    public class BossQuestCouplingRegressionTests
    {
        private static readonly string[] BossGatedScenes =
        {
            "Level_08_MoltenRift",
            "Level_09_StillTideSanctum",
            "Level_10_HiveCore"
        };

        [UnityTest, Timeout(300000)]
        public IEnumerator BossGatedLevels_RequiredQuestChain_ContainsBossObjective()
        {
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

                LevelRuntimeConfigurator runtime = FindComponentInScene<LevelRuntimeConfigurator>(scene);
                if (runtime != null)
                {
                    runtime.Apply();
                    yield return null;
                }

                QuestSystem questSystem = runtime != null ? runtime.questSystem : null;
                if (questSystem == null)
                {
                    questSystem = Object.FindObjectOfType<QuestSystem>();
                }
                if (questSystem == null || questSystem.availableQuests == null)
                {
                    errors.Add($"[{sceneName}] QuestSystem/availableQuests missing after runtime apply.");
                    yield return UnloadScene(scene, errors);
                    continue;
                }

                List<QuestConfig> questConfigs = levelFlow.levelData.quests ?? new List<QuestConfig>();
                int requiredQuestCount = 0;
                bool hasBossObjectiveInRequiredQuest = false;

                for (int q = 0; q < questConfigs.Count; q++)
                {
                    QuestConfig config = questConfigs[q];
                    if (config == null || !config.required || string.IsNullOrEmpty(config.questId))
                    {
                        continue;
                    }

                    requiredQuestCount++;
                    QuestData boundQuest = FindQuestById(questSystem.availableQuests, config.questId);
                    if (boundQuest == null)
                    {
                        errors.Add($"[{sceneName}] Required quest '{config.questId}' is not present in runtime quest chain.");
                        continue;
                    }

                    if (HasBoundBossObjective(boundQuest))
                    {
                        hasBossObjectiveInRequiredQuest = true;
                    }
                }

                if (requiredQuestCount <= 0)
                {
                    errors.Add($"[{sceneName}] No required quests configured.");
                }
                else if (!hasBossObjectiveInRequiredQuest)
                {
                    errors.Add($"[{sceneName}] Required quest chain has no BossBreak/BossDefeat objective with targetBossId.");
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

        private static QuestData FindQuestById(List<QuestData> quests, string questId)
        {
            if (quests == null || string.IsNullOrEmpty(questId))
            {
                return null;
            }

            for (int i = 0; i < quests.Count; i++)
            {
                QuestData quest = quests[i];
                if (quest != null && quest.questId == questId)
                {
                    return quest;
                }
            }

            return null;
        }

        private static bool HasBoundBossObjective(QuestData quest)
        {
            if (quest == null)
            {
                return false;
            }

            if (quest.stages != null && quest.stages.Count > 0)
            {
                for (int i = 0; i < quest.stages.Count; i++)
                {
                    QuestStage stage = quest.stages[i];
                    if (stage == null)
                    {
                        continue;
                    }

                    if ((stage.questType == QuestType.BossBreak || stage.questType == QuestType.BossDefeat) &&
                        !string.IsNullOrWhiteSpace(stage.targetBossId))
                    {
                        return true;
                    }
                }

                return false;
            }

            return (quest.questType == QuestType.BossBreak || quest.questType == QuestType.BossDefeat) &&
                   !string.IsNullOrWhiteSpace(quest.targetBossId);
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
