using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ThirdPersonController.Tests
{
    public class GrowthEconomyP5RegressionTests
    {
        private readonly List<Object> createdObjects = new List<Object>();
        private EconomyConfig oldEconomyConfig;

        [SetUp]
        public void SetUp()
        {
            oldEconomyConfig = EconomyService.Config;
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
        public void EconomyService_P5QuestAdjust_FallsBackToIdentity_WhenChapterAndStrongholdMissing()
        {
            EconomyConfig config = ScriptableObject.CreateInstance<EconomyConfig>();
            createdObjects.Add(config);
            config.questExpDifficultyMultipliers = new[] { 1f, 1.05f, 1.2f };
            config.levelExpDifficultyMultipliers = new[] { 1f, 1.1f, 1.2f };
            config.questTypeMultipliers = new List<QuestTypeRewardMultiplier>
            {
                new QuestTypeRewardMultiplier
                {
                    questType = QuestType.BossDefeat,
                    expMultiplier = 1.4f,
                    pearlMultiplier = 1f,
                    creditMultiplier = 1f
                }
            };
            config.questTierMultipliers = new List<QuestTierRewardMultiplier>
            {
                new QuestTierRewardMultiplier
                {
                    tier = QuestRewardTier.Challenge,
                    expMultiplier = 1.2f,
                    pearlMultiplier = 1f,
                    creditMultiplier = 1f
                }
            };
            config.questChapterMultipliers = new List<QuestChapterRewardMultiplier>
            {
                new QuestChapterRewardMultiplier
                {
                    chapterId = 2,
                    expMultiplier = 1.3f,
                    pearlMultiplier = 1f,
                    creditMultiplier = 1f
                }
            };
            config.questStrongholdMultipliers = new List<QuestStrongholdRewardMultiplier>
            {
                new QuestStrongholdRewardMultiplier
                {
                    strongholdId = "SH_A",
                    expMultiplier = 1.25f,
                    pearlMultiplier = 1f,
                    creditMultiplier = 1f
                }
            };
            EconomyService.Configure(config);

            int adjusted = EconomyService.AdjustQuestExp(
                baseExp: 100,
                questType: QuestType.BossDefeat,
                questDifficulty: 1,
                levelDifficulty: 2,
                rewardMultiplier: 1f,
                rewardTier: QuestRewardTier.Challenge,
                chapterId: 99,
                strongholdId: "SH_UNKNOWN");

            int expected = Mathf.RoundToInt(100f * 1f * 1.4f * 1.2f * 1f * 1f * 1.05f * 1.2f * 1f);
            Assert.AreEqual(expected, adjusted);
        }

        [Test]
        public void ShopManager_P5Purchase_WhenInventoryAtMax_RefundsWallet()
        {
            EconomyConfig config = ScriptableObject.CreateInstance<EconomyConfig>();
            createdObjects.Add(config);
            config.shopPriceMultiplier = 1f;
            config.shopPriceDifficultyMultipliers = new[] { 1f, 1f, 1f, 1f, 1f };
            EconomyService.Configure(config);

            GameObject root = new GameObject("GrowthEconomyP5_Shop");
            createdObjects.Add(root);

            CurrencyWallet wallet = root.AddComponent<CurrencyWallet>();
            wallet.showMessages = false;
            wallet.SetCredits(200);

            ConsumableCatalog catalog = ScriptableObject.CreateInstance<ConsumableCatalog>();
            createdObjects.Add(catalog);
            catalog.items = new List<ConsumableDefinition>
            {
                new ConsumableDefinition
                {
                    id = "p5_item",
                    displayName = "P5 Item",
                    price = 100,
                    maxStack = 1
                }
            };

            ConsumableInventory inventory = root.AddComponent<ConsumableInventory>();
            inventory.catalog = catalog;
            Assert.IsTrue(inventory.Add("p5_item", 1), "Setup should fill inventory to max stack.");

            ShopManager shop = root.AddComponent<ShopManager>();
            shop.catalog = catalog;
            shop.inventory = inventory;
            shop.wallet = wallet;
            shop.levelDifficulty = 1;
            shop.priceMultiplier = 1f;

            int beforeCredits = wallet.Credits;
            bool purchased = shop.Purchase("p5_item", 1);

            Assert.IsFalse(purchased, "Purchase should fail when target stack is already full.");
            Assert.AreEqual(beforeCredits, wallet.Credits, "Credits must be refunded when inventory add fails.");
        }

        [Test]
        public void QuestSystem_P5CompleteQuest_UsesLastStrongholdId_ForRewardRouting()
        {
            EconomyConfig config = ScriptableObject.CreateInstance<EconomyConfig>();
            createdObjects.Add(config);
            config.questCreditDifficultyMultipliers = new[] { 1f, 1f, 1.1f, 1.2f, 1.3f };
            config.levelCreditDifficultyMultipliers = new[] { 1f, 1f, 1.1f, 1.2f, 1.3f };
            config.questStrongholdMultipliers = new List<QuestStrongholdRewardMultiplier>
            {
                new QuestStrongholdRewardMultiplier
                {
                    strongholdId = "SH_FALLBACK",
                    expMultiplier = 1f,
                    pearlMultiplier = 1f,
                    creditMultiplier = 1.4f
                }
            };
            EconomyService.Configure(config);

            GameObject root = new GameObject("GrowthEconomyP5_Quest");
            createdObjects.Add(root);

            CurrencyWallet wallet = root.AddComponent<CurrencyWallet>();
            wallet.showMessages = false;
            wallet.SetCredits(0);

            QuestSystem questSystem = root.AddComponent<QuestSystem>();
            questSystem.showRewardMessages = false;
            questSystem.autoSaveOnQuestComplete = false;
            questSystem.saveQuestRuntimeState = false;
            questSystem.wallet = wallet;
            questSystem.levelDifficulty = 2;
            questSystem.levelRewardMultiplier = 1f;
            questSystem.levelChapterId = 1;

            QuestData data = new QuestData
            {
                questId = "p5_stronghold_fallback",
                questName = "Fallback Quest",
                questType = QuestType.CompleteStronghold,
                targetStrongholdId = string.Empty,
                difficultyRating = 2,
                rewardTier = QuestRewardTier.Mainline,
                reward = new QuestReward
                {
                    exp = 0,
                    pearls = 0,
                    credits = 100
                }
            };

            QuestProgress progress = new QuestProgress
            {
                data = data,
                status = QuestStatus.InProgress,
                lastStrongholdId = "SH_FALLBACK"
            };
            questSystem.activeQuests.Add(progress);

            MethodInfo completeQuest = typeof(QuestSystem).GetMethod("CompleteQuest", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(completeQuest, "QuestSystem.CompleteQuest should exist.");
            completeQuest.Invoke(questSystem, new object[] { progress });

            int expectedCredits = EconomyService.AdjustQuestCredits(
                baseCredits: data.reward.credits,
                questType: data.questType,
                questDifficulty: data.difficultyRating,
                levelDifficulty: questSystem.levelDifficulty,
                rewardMultiplier: questSystem.levelRewardMultiplier,
                rewardTier: data.rewardTier,
                chapterId: questSystem.levelChapterId,
                strongholdId: "SH_FALLBACK");

            int noFallbackCredits = EconomyService.AdjustQuestCredits(
                baseCredits: data.reward.credits,
                questType: data.questType,
                questDifficulty: data.difficultyRating,
                levelDifficulty: questSystem.levelDifficulty,
                rewardMultiplier: questSystem.levelRewardMultiplier,
                rewardTier: data.rewardTier,
                chapterId: questSystem.levelChapterId,
                strongholdId: string.Empty);

            Assert.AreEqual(QuestStatus.Completed, progress.status);
            Assert.AreEqual(expectedCredits, wallet.Credits);
            Assert.Greater(expectedCredits, noFallbackCredits, "Fallback stronghold multiplier should increase routed credits.");
        }
    }
}
