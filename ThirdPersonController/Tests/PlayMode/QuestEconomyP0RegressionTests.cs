using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ThirdPersonController.Tests
{
    public class QuestEconomyP0RegressionTests
    {
        private readonly List<Object> createdObjects = new List<Object>();
        private EconomyConfig oldEconomyConfig;

        [SetUp]
        public void SetUp()
        {
            oldEconomyConfig = EconomyService.Config;
            SaveManager save = SaveManager.Instance;
            Assert.NotNull(save);
            Assert.NotNull(save.CurrentData);
            save.CurrentData.currentExp = 0;
            save.CurrentData.playerLevel = 1;
            save.CurrentData.credits = 0;
            save.CurrentData.questStates = new List<QuestStateData>();
            save.CurrentData.consumables = new List<ConsumableStack>();
            save.CurrentData.claimedProgressionMilestones = new List<string>();
            save.CurrentData.unlockedPearlSlots = 3;
            save.CurrentData.maxPearlRarityUnlocked = (int)PearlRarity.Uncommon;
            save.CurrentData.pearlDropRateMultiplier = 1f;
        }

        [TearDown]
        public void TearDown()
        {
            EconomyService.Configure(oldEconomyConfig);

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
        public void EconomyService_P0QuestMultipliers_ApplyTypeTierChapterStrongholdAndDifficulty()
        {
            EconomyConfig config = BuildP0EconomyConfig();
            EconomyService.Configure(config);

            const int baseExp = 100;
            const int basePearls = 4;
            const int baseCredits = 80;
            const float externalRewardMultiplier = 1.25f;

            int exp = EconomyService.AdjustQuestExp(
                baseExp,
                QuestType.BossDefeat,
                questDifficulty: 2,
                levelDifficulty: 3,
                rewardMultiplier: externalRewardMultiplier,
                rewardTier: QuestRewardTier.Challenge,
                chapterId: 3,
                strongholdId: "SH_A");

            int pearls = EconomyService.AdjustQuestPearls(
                basePearls,
                QuestType.BossDefeat,
                questDifficulty: 2,
                levelDifficulty: 3,
                rewardMultiplier: externalRewardMultiplier,
                rewardTier: QuestRewardTier.Challenge,
                chapterId: 3,
                strongholdId: "SH_A");

            int credits = EconomyService.AdjustQuestCredits(
                baseCredits,
                QuestType.BossDefeat,
                questDifficulty: 2,
                levelDifficulty: 3,
                rewardMultiplier: externalRewardMultiplier,
                rewardTier: QuestRewardTier.Challenge,
                chapterId: 3,
                strongholdId: "SH_A");

            float expectedExpMultiplier =
                1f * 1.3f * 1.2f * 1.1f * 1.05f * 1.2f * 1.1f * externalRewardMultiplier;
            float expectedPearlMultiplier =
                1f * 1.2f * 1.15f * 1.08f * 1.04f * 1.1f * 1.05f * externalRewardMultiplier;
            float expectedCreditMultiplier =
                1f * 1.25f * 1.1f * 1.06f * 1.03f * 1.15f * 1.12f * externalRewardMultiplier;

            Assert.AreEqual(Mathf.RoundToInt(baseExp * expectedExpMultiplier), exp);
            Assert.AreEqual(Mathf.RoundToInt(basePearls * expectedPearlMultiplier), pearls);
            Assert.AreEqual(Mathf.RoundToInt(baseCredits * expectedCreditMultiplier), credits);
        }

        [Test]
        public void QuestSystem_P0CompleteQuest_AppliesEconomyRewardsToExperienceAndWallet()
        {
            EconomyConfig config = BuildP0EconomyConfig();
            EconomyService.Configure(config);

            GameObject root = new GameObject("QuestEconomy_P0Complete");
            createdObjects.Add(root);

            CurrencyWallet wallet = root.AddComponent<CurrencyWallet>();
            wallet.showMessages = false;
            wallet.SetCredits(0);

            PlayerExperienceSystem experienceSystem = root.AddComponent<PlayerExperienceSystem>();
            experienceSystem.baseExpToNext = 100000;
            experienceSystem.expGrowth = 1f;
            experienceSystem.level = 1;
            experienceSystem.currentExp = 0;

            QuestSystem questSystem = root.AddComponent<QuestSystem>();
            questSystem.showRewardMessages = false;
            questSystem.autoSaveOnQuestComplete = false;
            questSystem.saveQuestRuntimeState = false;
            questSystem.wallet = wallet;
            questSystem.BindExperienceSystem(experienceSystem);
            questSystem.expRewardMultiplier = 1.1f;
            questSystem.pearlRewardMultiplier = 1f;
            questSystem.levelRewardMultiplier = 1.2f;
            questSystem.levelDifficulty = 3;
            questSystem.levelChapterId = 3;

            QuestData questData = new QuestData
            {
                questId = "p0_quest_complete",
                questName = "P0 Quest",
                questType = QuestType.BossDefeat,
                difficultyRating = 2,
                rewardTier = QuestRewardTier.Challenge,
                targetStrongholdId = "SH_A",
                reward = new QuestReward
                {
                    exp = 100,
                    pearls = 0,
                    credits = 80
                }
            };

            QuestProgress progress = new QuestProgress
            {
                data = questData,
                status = QuestStatus.InProgress,
                lastStrongholdId = "SH_A"
            };
            questSystem.activeQuests.Add(progress);

            MethodInfo completeQuest = typeof(QuestSystem).GetMethod("CompleteQuest", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(completeQuest, "QuestSystem.CompleteQuest should exist.");
            completeQuest.Invoke(questSystem, new object[] { progress });

            int expectedExp = EconomyService.AdjustQuestExp(
                questData.reward.exp,
                questData.questType,
                questData.difficultyRating,
                questSystem.levelDifficulty,
                questSystem.expRewardMultiplier * questSystem.levelRewardMultiplier,
                questData.rewardTier,
                questSystem.levelChapterId,
                questData.targetStrongholdId);

            int expectedCredits = EconomyService.AdjustQuestCredits(
                questData.reward.credits,
                questData.questType,
                questData.difficultyRating,
                questSystem.levelDifficulty,
                questSystem.levelRewardMultiplier,
                questData.rewardTier,
                questSystem.levelChapterId,
                questData.targetStrongholdId);

            Assert.AreEqual(QuestStatus.Completed, progress.status);
            Assert.AreEqual(expectedExp, experienceSystem.currentExp, "Quest completion should route adjusted EXP into main progression chain.");
            Assert.AreEqual(expectedCredits, wallet.Credits, "Quest completion should route adjusted credits into wallet.");
        }

        [Test]
        public void QuestSystem_P0RestoreState_RestoresStageClampProgressAndTimerConsistency()
        {
            SaveManager save = SaveManager.Instance;
            Assert.NotNull(save);
            save.CurrentData.questStates = new List<QuestStateData>
            {
                new QuestStateData
                {
                    questId = "p0_restore_reach",
                    status = (int)QuestStatus.InProgress,
                    currentProgress = 3,
                    stageIndex = 9,
                    stageElapsedTime = 14f,
                    totalElapsedTime = 28f,
                    isTimerActive = true,
                    lastStrongholdId = "SH_A"
                }
            };

            GameObject root = new GameObject("QuestEconomy_P0Restore");
            createdObjects.Add(root);

            QuestSystem questSystem = root.AddComponent<QuestSystem>();
            questSystem.logQuestStateSync = false;
            questSystem.saveQuestRuntimeState = false;
            questSystem.availableQuests = new List<QuestData>
            {
                new QuestData
                {
                    questId = "p0_restore_reach",
                    questName = "Reach Restore",
                    stages = new List<QuestStage>
                    {
                        new QuestStage
                        {
                            stageId = "stage_reach",
                            questType = QuestType.Reach,
                            targetCount = 1,
                            targetLocationId = "checkpoint_a"
                        }
                    }
                }
            };

            bool restored = questSystem.RestoreQuestRuntimeStateFromSave(notifyListeners: false, addMissingQuests: true);
            Assert.IsTrue(restored, "RestoreQuestRuntimeStateFromSave should succeed when save state is present.");
            Assert.AreEqual(1, questSystem.activeQuests.Count);

            QuestProgress restoredQuest = questSystem.activeQuests[0];
            Assert.AreEqual("p0_restore_reach", restoredQuest.data.questId);
            Assert.AreEqual(QuestStatus.InProgress, restoredQuest.status);
            Assert.AreEqual(3, restoredQuest.currentProgress);
            Assert.AreEqual(0, restoredQuest.stageIndex, "In-progress stage index should clamp to current stage range.");
            Assert.AreEqual(14f, restoredQuest.stageElapsedTime, 0.0001f);
            Assert.AreEqual(28f, restoredQuest.totalElapsedTime, 0.0001f);
            Assert.IsTrue(restoredQuest.isTimerActive, "Reach-type stage should keep timer active after restore.");
            Assert.AreEqual("SH_A", restoredQuest.lastStrongholdId);
        }

        [Test]
        public void EconomyService_P0LevelAndShopMultipliers_ApplyDifficultyAndExternalMultiplier()
        {
            EconomyConfig config = BuildP0EconomyConfig();
            config.levelExpMultiplier = 1.15f;
            config.levelPearlMultiplier = 1.1f;
            config.levelCreditMultiplier = 1.2f;
            config.shopPriceMultiplier = 1.08f;
            config.dropChanceDifficultyMultipliers = new[] { 1f, 1.02f, 1.09f, 1.18f };
            config.shopPriceDifficultyMultipliers = new[] { 1f, 1.03f, 1.1f, 1.22f };
            EconomyService.Configure(config);

            int levelExp = EconomyService.AdjustLevelExp(200, difficulty: 3, levelRewardMultiplier: 1.25f);
            int levelPearls = EconomyService.AdjustLevelPearls(5, difficulty: 2, levelRewardMultiplier: 1.2f);
            int levelCredits = EconomyService.AdjustLevelCredits(120, difficulty: 3, levelRewardMultiplier: 1.15f);
            int shopPrice = EconomyService.AdjustShopPrice(90, difficulty: 2, priceMultiplier: 1.3f);
            float dropChanceMultiplier = EconomyService.GetDropChanceMultiplier(3);

            Assert.AreEqual(Mathf.RoundToInt(200 * 1.15f * 1.1f * 1.25f), levelExp);
            Assert.AreEqual(Mathf.RoundToInt(5 * 1.1f * 1.02f * 1.2f), levelPearls);
            Assert.AreEqual(Mathf.RoundToInt(120 * 1.2f * 1.12f * 1.15f), levelCredits);
            Assert.AreEqual(Mathf.RoundToInt(90 * 1.08f * 1.1f * 1.3f), shopPrice);
            Assert.AreEqual(1.18f, dropChanceMultiplier, 0.0001f);
        }

        [Test]
        public void LevelRewardSystem_P0HandleLevelCompleted_AppliesAdjustedRewardsOnce()
        {
            EconomyConfig config = BuildP0EconomyConfig();
            config.levelExpMultiplier = 1.1f;
            config.levelCreditMultiplier = 1.15f;
            EconomyService.Configure(config);

            GameObject root = new GameObject("QuestEconomy_P0LevelRewards");
            createdObjects.Add(root);

            CurrencyWallet wallet = root.AddComponent<CurrencyWallet>();
            wallet.showMessages = false;
            wallet.SetCredits(0);

            PlayerExperienceSystem experienceSystem = root.AddComponent<PlayerExperienceSystem>();
            experienceSystem.baseExpToNext = 100000;
            experienceSystem.expGrowth = 1f;
            experienceSystem.level = 1;
            experienceSystem.currentExp = 0;

            LevelData levelData = BuildLevelData(
                levelId: "LEVEL_03",
                chapterId: 1,
                difficulty: LevelDifficulty.Nightmare,
                baseExp: 180,
                basePearls: 0,
                baseCredits: 90);

            LevelRewardSystem rewardSystem = root.AddComponent<LevelRewardSystem>();
            rewardSystem.levelData = levelData;
            rewardSystem.showMessages = false;
            rewardSystem.autoSaveOnReward = false;
            rewardSystem.expRewardMultiplier = 1.1f;
            rewardSystem.pearlRewardMultiplier = 1f;
            rewardSystem.levelRewardMultiplier = 1.25f;
            rewardSystem.levelDifficulty = 3;
            rewardSystem.experienceSystem = experienceSystem;
            rewardSystem.wallet = wallet;

            MethodInfo handleLevelCompleted = typeof(LevelRewardSystem).GetMethod("HandleLevelCompleted", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(handleLevelCompleted);

            int expectedExp = EconomyService.AdjustLevelExp(
                levelData.baseExp,
                rewardSystem.levelDifficulty,
                rewardSystem.expRewardMultiplier * rewardSystem.levelRewardMultiplier);
            int expectedCredits = EconomyService.AdjustLevelCredits(
                levelData.baseCredits,
                rewardSystem.levelDifficulty,
                rewardSystem.levelRewardMultiplier);

            handleLevelCompleted.Invoke(rewardSystem, new object[] { 103 });
            Assert.AreEqual(expectedExp, experienceSystem.currentExp);
            Assert.AreEqual(expectedCredits, wallet.Credits);

            handleLevelCompleted.Invoke(rewardSystem, new object[] { 103 });
            Assert.AreEqual(expectedExp, experienceSystem.currentExp, "Reward should not be granted twice for the same level completion event.");
            Assert.AreEqual(expectedCredits, wallet.Credits, "Credits should remain unchanged after duplicate completion event.");
        }

        [Test]
        public void ShopManager_P0Purchase_WhenInventoryMissing_DoesNotConsumeCredits()
        {
            EconomyConfig config = BuildP0EconomyConfig();
            config.shopPriceMultiplier = 1.1f;
            config.shopPriceDifficultyMultipliers = new[] { 1f, 1.05f, 1.2f, 1.3f };
            EconomyService.Configure(config);

            GameObject root = new GameObject("QuestEconomy_P0Shop");
            createdObjects.Add(root);

            CurrencyWallet wallet = root.AddComponent<CurrencyWallet>();
            wallet.showMessages = false;
            wallet.SetCredits(500);

            ConsumableCatalog catalog = ScriptableObject.CreateInstance<ConsumableCatalog>();
            createdObjects.Add(catalog);
            catalog.items = new List<ConsumableDefinition>
            {
                new ConsumableDefinition
                {
                    id = "p0_shop_item",
                    displayName = "P0 Shop Item",
                    price = 100,
                    maxStack = 10
                }
            };

            ShopManager shop = root.AddComponent<ShopManager>();
            shop.catalog = catalog;
            shop.wallet = wallet;
            shop.inventory = null;
            shop.levelDifficulty = 2;
            shop.priceMultiplier = 1.25f;

            int expectedPrice = EconomyService.AdjustShopPrice(100, 2, 1.25f);
            bool purchased = shop.Purchase("p0_shop_item", 1);

            Assert.IsFalse(purchased, "Purchase should fail when inventory is missing.");
            Assert.AreEqual(500, wallet.Credits, "Credits must remain unchanged when purchase cannot be fulfilled.");
            Assert.Greater(expectedPrice, 0);
        }

        [Test]
        public void LongTermProgressionSystem_P0MilestoneClaim_AppliesRewardsAndPersistsToSave()
        {
            SaveManager save = SaveManager.Instance;
            Assert.NotNull(save);
            Assert.NotNull(save.CurrentData);
            save.CurrentData.claimedProgressionMilestones = new List<string>();
            save.CurrentData.unlockedPearlSlots = 3;
            save.CurrentData.maxPearlRarityUnlocked = (int)PearlRarity.Uncommon;
            save.CurrentData.pearlDropRateMultiplier = 1f;
            save.CurrentData.activeProgressionRoute = ProgressionRoute.Offense.ToString();
            save.CurrentData.totalKills = 12;
            save.CurrentData.pearlsCollected = 8;
            save.CurrentData.longestCombo = 6;

            GameObject root = new GameObject("QuestEconomy_P0LongTermProgression");
            createdObjects.Add(root);

            TalentTree talentTree = root.AddComponent<TalentTree>();
            talentTree.availablePoints = 0;

            PearlEquipment equipment = root.AddComponent<PearlEquipment>();
            equipment.slotCount = 3;
            equipment.EnsureSlotCount();

            PearlDropManager dropManager = root.AddComponent<PearlDropManager>();
            dropManager.useProgressionCaps = true;
            dropManager.maxRarityCap = PearlRarity.Uncommon;
            dropManager.dropChanceMultiplier = 1f;

            ProgressionMilestoneData milestoneData = ScriptableObject.CreateInstance<ProgressionMilestoneData>();
            createdObjects.Add(milestoneData);
            milestoneData.milestones = new List<ProgressionMilestone>
            {
                new ProgressionMilestone
                {
                    id = "p0_progression_milestone",
                    title = "P0 Progression",
                    route = ProgressionRoute.Offense,
                    requiredTotalKills = 10,
                    requiredPearlsCollected = 5,
                    requiredLongestCombo = 3,
                    grantTalentPoints = 2,
                    grantPearlSlots = 1,
                    unlockMaxRarity = PearlRarity.Epic,
                    dropRateMultiplier = 1.2f
                }
            };

            LongTermProgressionSystem progression = root.AddComponent<LongTermProgressionSystem>();
            progression.milestoneData = milestoneData;
            progression.saveManager = save;
            progression.statisticsManager = null;
            progression.talentTree = talentTree;
            progression.equipment = equipment;
            progression.dropManager = dropManager;
            progression.autoApplyOnStart = false;
            progression.autoSaveOnMilestone = false;
            progression.showMessages = false;
            progression.activeRoute = ProgressionRoute.Offense;

            progression.EvaluateMilestones();

            Assert.IsTrue(save.CurrentData.claimedProgressionMilestones.Contains("p0_progression_milestone"));
            Assert.AreEqual(2, talentTree.availablePoints);
            Assert.AreEqual(4, save.CurrentData.unlockedPearlSlots);
            Assert.AreEqual(4, equipment.slotCount);
            Assert.AreEqual((int)PearlRarity.Epic, save.CurrentData.maxPearlRarityUnlocked);
            Assert.AreEqual(1.2f, save.CurrentData.pearlDropRateMultiplier, 0.0001f);
            Assert.AreEqual(PearlRarity.Epic, dropManager.maxRarityCap);
            Assert.AreEqual(1.2f, dropManager.dropChanceMultiplier, 0.0001f);

            progression.EvaluateMilestones();
            Assert.AreEqual(1, save.CurrentData.claimedProgressionMilestones.Count, "Milestone reward should not be applied twice.");
            Assert.AreEqual(2, talentTree.availablePoints, "Talent points should not increase on duplicate evaluate.");
        }

        private LevelData BuildLevelData(
            string levelId,
            int chapterId,
            LevelDifficulty difficulty,
            int baseExp,
            int basePearls,
            int baseCredits)
        {
            LevelData data = ScriptableObject.CreateInstance<LevelData>();
            createdObjects.Add(data);
            data.levelId = levelId;
            data.chapterId = chapterId;
            data.difficulty = difficulty;
            data.baseExp = baseExp;
            data.basePearls = basePearls;
            data.baseCredits = baseCredits;
            return data;
        }

        private EconomyConfig BuildP0EconomyConfig()
        {
            EconomyConfig config = ScriptableObject.CreateInstance<EconomyConfig>();
            createdObjects.Add(config);

            config.questExpMultiplier = 1f;
            config.questPearlMultiplier = 1f;
            config.questCreditMultiplier = 1f;

            config.questExpDifficultyMultipliers = new[] { 1f, 1.05f, 1.2f, 1.3f };
            config.levelExpDifficultyMultipliers = new[] { 1f, 1.02f, 1.05f, 1.1f };
            config.questPearlDifficultyMultipliers = new[] { 1f, 1.03f, 1.1f, 1.2f };
            config.levelPearlDifficultyMultipliers = new[] { 1f, 1.01f, 1.02f, 1.05f };
            config.questCreditDifficultyMultipliers = new[] { 1f, 1.08f, 1.15f, 1.22f };
            config.levelCreditDifficultyMultipliers = new[] { 1f, 1.04f, 1.08f, 1.12f };

            config.questTypeMultipliers = new List<QuestTypeRewardMultiplier>
            {
                new QuestTypeRewardMultiplier
                {
                    questType = QuestType.BossDefeat,
                    expMultiplier = 1.3f,
                    pearlMultiplier = 1.2f,
                    creditMultiplier = 1.25f
                }
            };
            config.questTierMultipliers = new List<QuestTierRewardMultiplier>
            {
                new QuestTierRewardMultiplier
                {
                    tier = QuestRewardTier.Challenge,
                    expMultiplier = 1.2f,
                    pearlMultiplier = 1.15f,
                    creditMultiplier = 1.1f
                }
            };
            config.questChapterMultipliers = new List<QuestChapterRewardMultiplier>
            {
                new QuestChapterRewardMultiplier
                {
                    chapterId = 3,
                    expMultiplier = 1.1f,
                    pearlMultiplier = 1.08f,
                    creditMultiplier = 1.06f
                }
            };
            config.questStrongholdMultipliers = new List<QuestStrongholdRewardMultiplier>
            {
                new QuestStrongholdRewardMultiplier
                {
                    strongholdId = "SH_A",
                    expMultiplier = 1.05f,
                    pearlMultiplier = 1.04f,
                    creditMultiplier = 1.03f
                }
            };

            return config;
        }
    }
}
