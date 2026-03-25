using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ThirdPersonController.Tests
{
    public class LevelRuntimeConfiguratorQuestCloneRegressionTests
    {
        private readonly List<Object> createdObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                Object obj = createdObjects[i];
                if (obj != null)
                {
                    Object.DestroyImmediate(obj);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void CloneQuest_PreservesStageAndRootTargetFields()
        {
            LevelRuntimeConfigurator runtime = CreateRuntimeConfigurator("CloneQuest_Runtime");

            QuestData source = new QuestData
            {
                questId = "q_clone_root",
                questType = QuestType.CompleteWaveEvent,
                targetStrongholdId = "Stronghold_02",
                targetBossId = "Boss_HiveCore",
                matchAnyWaveEventType = false,
                targetWaveEventType = WaveEventType.ProtectTarget,
                nextQuestIds = new List<string> { "q_next_01" },
                reward = new QuestReward
                {
                    exp = 100,
                    pearls = 2,
                    credits = 30,
                    itemIds = new List<string> { "itm_a" }
                },
                rewardTier = QuestRewardTier.Challenge,
                stages = new List<QuestStage>
                {
                    new QuestStage
                    {
                        stageId = "stage_01",
                        questType = QuestType.CompleteStronghold,
                        targetStrongholdId = "Stronghold_01",
                        targetBossId = "Boss_HiveCore",
                        matchAnyWaveEventType = false,
                        targetWaveEventType = WaveEventType.HoldPoint
                    }
                }
            };

            QuestData clone = InvokeCloneQuest(runtime, source);

            Assert.NotNull(clone);
            Assert.AreNotSame(source, clone);
            Assert.AreEqual(source.targetStrongholdId, clone.targetStrongholdId);
            Assert.AreEqual(source.targetBossId, clone.targetBossId);
            Assert.AreEqual(source.matchAnyWaveEventType, clone.matchAnyWaveEventType);
            Assert.AreEqual(source.targetWaveEventType, clone.targetWaveEventType);
            Assert.AreEqual(source.rewardTier, clone.rewardTier);

            Assert.NotNull(clone.stages);
            Assert.AreEqual(1, clone.stages.Count);
            Assert.AreEqual("Stronghold_01", clone.stages[0].targetStrongholdId);
            Assert.AreEqual("Boss_HiveCore", clone.stages[0].targetBossId);
            Assert.IsFalse(clone.stages[0].matchAnyWaveEventType);
            Assert.AreEqual(WaveEventType.HoldPoint, clone.stages[0].targetWaveEventType);

            Assert.NotNull(clone.nextQuestIds);
            Assert.AreEqual(1, clone.nextQuestIds.Count);
            Assert.AreNotSame(source.nextQuestIds, clone.nextQuestIds);
            Assert.NotNull(clone.reward);
            Assert.NotNull(clone.reward.itemIds);
            Assert.AreNotSame(source.reward.itemIds, clone.reward.itemIds);
        }

        [Test]
        public void ConfigureQuests_ClonedQuestRetainsStageTargetFields()
        {
            LevelRuntimeConfigurator runtime = CreateRuntimeConfigurator("ConfigureQuests_Runtime");

            QuestData templateQuest = new QuestData
            {
                questId = "l10_hive_core",
                questName = "Hive Core Breach",
                questType = QuestType.CompleteWaveEvent,
                matchAnyWaveEventType = false,
                targetWaveEventType = WaveEventType.ProtectTarget,
                stages = new List<QuestStage>
                {
                    new QuestStage
                    {
                        stageId = "l10_hold_breaker",
                        questType = QuestType.CompleteWaveEvent,
                        targetStrongholdId = "Stronghold_01",
                        targetBossId = "Boss_HiveCore",
                        matchAnyWaveEventType = false,
                        targetWaveEventType = WaveEventType.ProtectTarget
                    }
                }
            };

            LevelData levelData = ScriptableObject.CreateInstance<LevelData>();
            createdObjects.Add(levelData);
            levelData.levelId = "LEVEL_10";
            levelData.quests = new List<QuestConfig>
            {
                new QuestConfig { questId = "l10_hive_core", required = true, order = 0 }
            };

            QuestDatabase questDatabase = ScriptableObject.CreateInstance<QuestDatabase>();
            createdObjects.Add(questDatabase);
            questDatabase.quests = new List<QuestData> { templateQuest };

            QuestSystem questSystem = runtime.gameObject.AddComponent<QuestSystem>();
            runtime.levelData = levelData;
            runtime.questDatabase = questDatabase;
            runtime.questSystem = questSystem;

            InvokeConfigureQuests(runtime);

            Assert.NotNull(questSystem.availableQuests);
            Assert.AreEqual(1, questSystem.availableQuests.Count);

            QuestData configuredQuest = questSystem.availableQuests[0];
            Assert.NotNull(configuredQuest);
            Assert.AreNotSame(templateQuest, configuredQuest);
            Assert.IsFalse(configuredQuest.isOptional);
            Assert.NotNull(configuredQuest.stages);
            Assert.AreEqual(1, configuredQuest.stages.Count);
            Assert.AreEqual("Stronghold_01", configuredQuest.stages[0].targetStrongholdId);
            Assert.AreEqual("Boss_HiveCore", configuredQuest.stages[0].targetBossId);
            Assert.IsFalse(configuredQuest.stages[0].matchAnyWaveEventType);
            Assert.AreEqual(WaveEventType.ProtectTarget, configuredQuest.stages[0].targetWaveEventType);
        }

        private LevelRuntimeConfigurator CreateRuntimeConfigurator(string name)
        {
            GameObject go = new GameObject(name);
            createdObjects.Add(go);
            return go.AddComponent<LevelRuntimeConfigurator>();
        }

        private static QuestData InvokeCloneQuest(LevelRuntimeConfigurator runtime, QuestData source)
        {
            MethodInfo method = typeof(LevelRuntimeConfigurator).GetMethod(
                "CloneQuest",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(method, "CloneQuest reflection lookup failed.");
            object clone = method.Invoke(runtime, new object[] { source });
            return clone as QuestData;
        }

        private static void InvokeConfigureQuests(LevelRuntimeConfigurator runtime)
        {
            MethodInfo method = typeof(LevelRuntimeConfigurator).GetMethod(
                "ConfigureQuests",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(method, "ConfigureQuests reflection lookup failed.");
            method.Invoke(runtime, null);
        }
    }
}
