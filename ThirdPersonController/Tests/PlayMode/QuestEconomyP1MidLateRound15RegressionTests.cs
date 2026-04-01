using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ThirdPersonController.Tests
{
    public class QuestEconomyP1MidLateRound15RegressionTests
    {
        private readonly List<Object> createdObjects = new List<Object>();
        private EconomyConfig oldEconomyConfig;

        [SetUp]
        public void SetUp()
        {
            oldEconomyConfig = EconomyService.Config;
            SaveManager save = SaveManager.Instance;
            if (save != null && save.CurrentData != null)
            {
                save.CurrentData.currentExp = 0;
                save.CurrentData.playerLevel = 1;
                save.CurrentData.credits = 0;
            }
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
        public void QuestSystem_P1MidLateRound15_LongChainFailureRecovery_AccumulatesOnlySuccessfulRewards()
        {
            EconomyConfig config = BuildP1SimulationConfig();
            EconomyService.Configure(config);

            QuestSystem questSystem = CreateQuestRuntime(
                "QuestEconomyP1Round15_LongChain",
                out CurrencyWallet wallet,
                out PlayerExperienceSystem experienceSystem);

            MethodInfo completeQuest = typeof(QuestSystem).GetMethod("CompleteQuest", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(completeQuest, "QuestSystem.CompleteQuest should exist.");

            var nodes = new List<LongChainNode>
            {
                NewLongChainNode("r15_q_01", QuestType.CompleteStronghold, QuestRewardTier.Mainline, 2, 2, 2, "SH_MID", "SH_MID", 130, 95, false),
                NewLongChainNode("r15_q_02", QuestType.Kill, QuestRewardTier.Side, 2, 2, 2, string.Empty, "SH_MID", 150, 110, true),
                NewLongChainNode("r15_q_03", QuestType.BossBreak, QuestRewardTier.Challenge, 3, 3, 3, "SH_MID", "SH_MID", 175, 130, false),
                NewLongChainNode("r15_q_04", QuestType.CompleteStronghold, QuestRewardTier.Side, 3, 3, 3, string.Empty, "SH_MID", 190, 145, true),
                NewLongChainNode("r15_q_05", QuestType.BossBreak, QuestRewardTier.Challenge, 3, 3, 4, "SH_LATE", "SH_LATE", 210, 160, false),
                NewLongChainNode("r15_q_06", QuestType.BossDefeat, QuestRewardTier.Challenge, 4, 4, 4, "SH_LATE", "SH_LATE", 240, 190, false),
                NewLongChainNode("r15_q_07", QuestType.Kill, QuestRewardTier.Side, 4, 4, 4, string.Empty, "SH_LATE", 220, 170, true),
                NewLongChainNode("r15_q_08", QuestType.BossDefeat, QuestRewardTier.Challenge, 5, 6, 6, "SH_LATE", "SH_LATE", 260, 210, false)
            };

            int expectedExp = 0;
            int expectedCredits = 0;
            int failedAttempts = 0;
            int midCreditsTotal = 0;
            int midCount = 0;
            int lateCreditsTotal = 0;
            int lateCount = 0;

            for (int i = 0; i < nodes.Count; i++)
            {
                LongChainNode node = nodes[i];
                questSystem.levelDifficulty = node.levelDifficulty;
                questSystem.levelChapterId = node.chapterId;

                QuestData questData = NewQuestData(
                    node.questId,
                    node.questType,
                    node.rewardTier,
                    node.questDifficulty,
                    node.targetStrongholdId,
                    node.baseExp,
                    node.baseCredits);

                if (node.failFirstAttempt)
                {
                    QuestProgress failedProgress = NewQuestProgress(questData, node.fallbackStrongholdId);
                    questSystem.activeQuests.Add(failedProgress);

                    int expBefore = experienceSystem.currentExp;
                    int creditsBefore = wallet.Credits;

                    questSystem.FailQuest(failedProgress, "Round15 retry simulation");
                    Assert.AreEqual(QuestStatus.Failed, failedProgress.status, $"Failed attempt should enter failed status: {node.questId}");

                    completeQuest.Invoke(questSystem, new object[] { failedProgress });
                    Assert.AreEqual(expBefore, experienceSystem.currentExp, $"Failed attempt should not grant EXP: {node.questId}");
                    Assert.AreEqual(creditsBefore, wallet.Credits, $"Failed attempt should not grant credits: {node.questId}");
                    failedAttempts++;
                }

                QuestProgress successProgress = NewQuestProgress(questData, node.fallbackStrongholdId);
                questSystem.activeQuests.Add(successProgress);

                int expDelta = ExpectedQuestExp(questSystem, questData, node.fallbackStrongholdId);
                int creditDelta = ExpectedQuestCredits(questSystem, questData, node.fallbackStrongholdId);

                completeQuest.Invoke(questSystem, new object[] { successProgress });
                Assert.AreEqual(QuestStatus.Completed, successProgress.status, $"Success path should complete quest: {node.questId}");

                expectedExp += expDelta;
                expectedCredits += creditDelta;

                if (node.chapterId >= 4)
                {
                    lateCreditsTotal += creditDelta;
                    lateCount++;
                }
                else
                {
                    midCreditsTotal += creditDelta;
                    midCount++;
                }
            }

            Assert.Greater(failedAttempts, 0, "Round15 long-chain simulation should include fail->retry samples.");
            Assert.AreEqual(expectedExp, experienceSystem.currentExp, "Long-chain EXP accumulation should match successful completions only.");
            Assert.AreEqual(expectedCredits, wallet.Credits, "Long-chain credit accumulation should match successful completions only.");

            float midAverage = midCount > 0 ? midCreditsTotal / (float)midCount : 0f;
            float lateAverage = lateCount > 0 ? lateCreditsTotal / (float)lateCount : 0f;
            Assert.Greater(lateAverage, midAverage, "Late-chain average credit payout should stay above mid-chain average.");
        }

        [Test]
        public void EconomyService_P1MidLateRound15_LongChainIncomePressure_StaysInPlayableBand()
        {
            EconomyConfig config = BuildP1SimulationConfig();
            EconomyService.Configure(config);

            var nodes = new List<PressureNode>
            {
                NewPressureNode(2, 2, QuestType.CompleteStronghold, QuestRewardTier.Mainline, "SH_MID", 150, 120, 200, 1.05f, 1.08f, 1f),
                NewPressureNode(2, 2, QuestType.Kill, QuestRewardTier.Side, "SH_MID", 170, 130, 220, 1.08f, 1.1f, 1f),
                NewPressureNode(3, 3, QuestType.BossBreak, QuestRewardTier.Challenge, "SH_MID", 185, 145, 245, 1.1f, 1.12f, 1f),
                NewPressureNode(3, 3, QuestType.CompleteStronghold, QuestRewardTier.Side, "SH_MID", 200, 155, 265, 1.1f, 1.12f, 1f),
                NewPressureNode(3, 4, QuestType.BossBreak, QuestRewardTier.Challenge, "SH_LATE", 220, 165, 285, 1.15f, 1.14f, 1f),
                NewPressureNode(4, 4, QuestType.BossDefeat, QuestRewardTier.Challenge, "SH_LATE", 235, 175, 305, 1.2f, 1.16f, 1f),
                NewPressureNode(4, 4, QuestType.Kill, QuestRewardTier.Side, "SH_LATE", 230, 170, 300, 1.12f, 1.14f, 1f),
                NewPressureNode(5, 7, QuestType.BossDefeat, QuestRewardTier.Challenge, "SH_LATE", 250, 190, 330, 1.22f, 1.18f, 1f)
            };

            float earlyIncomeSum = 0f;
            float lateIncomeSum = 0f;
            float earlyPriceSum = 0f;
            float latePriceSum = 0f;

            for (int i = 0; i < nodes.Count; i++)
            {
                PressureNode node = nodes[i];

                int questCredits = EconomyService.AdjustQuestCredits(
                    baseCredits: node.baseQuestCredits,
                    questType: node.questType,
                    questDifficulty: node.questDifficulty,
                    levelDifficulty: node.levelDifficulty,
                    rewardMultiplier: node.questRewardMultiplier,
                    rewardTier: node.rewardTier,
                    chapterId: node.chapterId,
                    strongholdId: node.strongholdId);

                int levelCredits = EconomyService.AdjustLevelCredits(
                    baseCredits: node.baseLevelCredits,
                    difficulty: node.levelDifficulty,
                    levelRewardMultiplier: node.levelRewardMultiplier);

                int income = questCredits + levelCredits;
                int shopPrice = EconomyService.AdjustShopPrice(
                    basePrice: node.baseShopPrice,
                    difficulty: node.levelDifficulty,
                    priceMultiplier: node.shopPriceMultiplier);

                float ratio = shopPrice > 0 ? income / (float)shopPrice : 0f;
                Assert.GreaterOrEqual(ratio, 1f, $"Node {i + 1} income/shop ratio too low.");
                Assert.LessOrEqual(ratio, 4.5f, $"Node {i + 1} income/shop ratio too high.");

                if (i < nodes.Count / 2)
                {
                    earlyIncomeSum += income;
                    earlyPriceSum += shopPrice;
                }
                else
                {
                    lateIncomeSum += income;
                    latePriceSum += shopPrice;
                }
            }

            float earlyIncomeAvg = earlyIncomeSum / (nodes.Count / 2f);
            float lateIncomeAvg = lateIncomeSum / (nodes.Count / 2f);
            float earlyPriceAvg = earlyPriceSum / (nodes.Count / 2f);
            float latePriceAvg = latePriceSum / (nodes.Count / 2f);

            Assert.Greater(lateIncomeAvg, earlyIncomeAvg, "Late-chain average income should rise with progression.");
            Assert.Greater(latePriceAvg, earlyPriceAvg, "Late-chain average shop pressure should rise with progression.");
        }

        [Test]
        public void EconomyService_P1MidLateRound15_DifficultyOverflow_ClampsToConfiguredLateTier()
        {
            EconomyConfig config = BuildP1SimulationConfig();
            EconomyService.Configure(config);

            int capQuestExp = EconomyService.AdjustQuestExp(
                baseExp: 200,
                questType: QuestType.BossDefeat,
                questDifficulty: 4,
                levelDifficulty: 4,
                rewardMultiplier: 1.2f,
                rewardTier: QuestRewardTier.Challenge,
                chapterId: 4,
                strongholdId: "SH_LATE");

            int overflowQuestExp = EconomyService.AdjustQuestExp(
                baseExp: 200,
                questType: QuestType.BossDefeat,
                questDifficulty: 99,
                levelDifficulty: 99,
                rewardMultiplier: 1.2f,
                rewardTier: QuestRewardTier.Challenge,
                chapterId: 4,
                strongholdId: "SH_LATE");

            int capQuestCredits = EconomyService.AdjustQuestCredits(
                baseCredits: 180,
                questType: QuestType.BossDefeat,
                questDifficulty: 4,
                levelDifficulty: 4,
                rewardMultiplier: 1.2f,
                rewardTier: QuestRewardTier.Challenge,
                chapterId: 4,
                strongholdId: "SH_LATE");

            int overflowQuestCredits = EconomyService.AdjustQuestCredits(
                baseCredits: 180,
                questType: QuestType.BossDefeat,
                questDifficulty: 99,
                levelDifficulty: 99,
                rewardMultiplier: 1.2f,
                rewardTier: QuestRewardTier.Challenge,
                chapterId: 4,
                strongholdId: "SH_LATE");

            int capLevelCredits = EconomyService.AdjustLevelCredits(
                baseCredits: 180,
                difficulty: 4,
                levelRewardMultiplier: 1.2f);

            int overflowLevelCredits = EconomyService.AdjustLevelCredits(
                baseCredits: 180,
                difficulty: 99,
                levelRewardMultiplier: 1.2f);

            int capShopPrice = EconomyService.AdjustShopPrice(
                basePrice: 260,
                difficulty: 4,
                priceMultiplier: 1f);

            int overflowShopPrice = EconomyService.AdjustShopPrice(
                basePrice: 260,
                difficulty: 99,
                priceMultiplier: 1f);

            Assert.AreEqual(capQuestExp, overflowQuestExp, "Quest EXP should clamp to the configured max difficulty tier.");
            Assert.AreEqual(capQuestCredits, overflowQuestCredits, "Quest credits should clamp to the configured max difficulty tier.");
            Assert.AreEqual(capLevelCredits, overflowLevelCredits, "Level credits should clamp to the configured max difficulty tier.");
            Assert.AreEqual(capShopPrice, overflowShopPrice, "Shop price should clamp to the configured max difficulty tier.");
        }

        private QuestSystem CreateQuestRuntime(string rootName, out CurrencyWallet wallet, out PlayerExperienceSystem experienceSystem)
        {
            GameObject root = new GameObject(rootName);
            createdObjects.Add(root);

            wallet = root.AddComponent<CurrencyWallet>();
            wallet.showMessages = false;
            wallet.SetCredits(0);

            experienceSystem = root.AddComponent<PlayerExperienceSystem>();
            experienceSystem.logStartupStatus = false;
            experienceSystem.baseExpToNext = 200000;
            experienceSystem.expGrowth = 1f;
            experienceSystem.maxLevel = 99;
            experienceSystem.level = 1;
            experienceSystem.currentExp = 0;

            QuestSystem questSystem = root.AddComponent<QuestSystem>();
            questSystem.showRewardMessages = false;
            questSystem.autoSaveOnQuestComplete = false;
            questSystem.saveQuestRuntimeState = false;
            questSystem.autoStartGuidedQuests = false;
            questSystem.wallet = wallet;
            questSystem.BindExperienceSystem(experienceSystem);
            questSystem.expRewardMultiplier = 1.08f;
            questSystem.levelRewardMultiplier = 1.12f;
            questSystem.levelDifficulty = 3;
            questSystem.levelChapterId = 3;
            return questSystem;
        }

        private static QuestData NewQuestData(
            string questId,
            QuestType questType,
            QuestRewardTier tier,
            int difficultyRating,
            string targetStrongholdId,
            int expReward,
            int creditReward)
        {
            return new QuestData
            {
                questId = questId,
                questName = questId,
                questType = questType,
                rewardTier = tier,
                difficultyRating = difficultyRating,
                targetStrongholdId = targetStrongholdId,
                reward = new QuestReward
                {
                    exp = expReward,
                    pearls = 0,
                    credits = creditReward
                }
            };
        }

        private static QuestProgress NewQuestProgress(QuestData data, string fallbackStrongholdId)
        {
            return new QuestProgress
            {
                data = data,
                status = QuestStatus.InProgress,
                lastStrongholdId = fallbackStrongholdId
            };
        }

        private static int ExpectedQuestExp(QuestSystem questSystem, QuestData data, string fallbackStrongholdId)
        {
            string rewardStrongholdId = ResolveRewardStrongholdId(data, fallbackStrongholdId);
            return EconomyService.AdjustQuestExp(
                baseExp: data.reward.exp,
                questType: data.questType,
                questDifficulty: data.difficultyRating,
                levelDifficulty: questSystem.levelDifficulty,
                rewardMultiplier: questSystem.expRewardMultiplier * questSystem.levelRewardMultiplier,
                rewardTier: data.rewardTier,
                chapterId: questSystem.levelChapterId,
                strongholdId: rewardStrongholdId);
        }

        private static int ExpectedQuestCredits(QuestSystem questSystem, QuestData data, string fallbackStrongholdId)
        {
            string rewardStrongholdId = ResolveRewardStrongholdId(data, fallbackStrongholdId);
            return EconomyService.AdjustQuestCredits(
                baseCredits: data.reward.credits,
                questType: data.questType,
                questDifficulty: data.difficultyRating,
                levelDifficulty: questSystem.levelDifficulty,
                rewardMultiplier: questSystem.levelRewardMultiplier,
                rewardTier: data.rewardTier,
                chapterId: questSystem.levelChapterId,
                strongholdId: rewardStrongholdId);
        }

        private static string ResolveRewardStrongholdId(QuestData data, string fallbackStrongholdId)
        {
            return !string.IsNullOrEmpty(data.targetStrongholdId) ? data.targetStrongholdId : fallbackStrongholdId;
        }

        private EconomyConfig BuildP1SimulationConfig()
        {
            EconomyConfig config = ScriptableObject.CreateInstance<EconomyConfig>();
            createdObjects.Add(config);

            config.levelExpMultiplier = 1.12f;
            config.levelCreditMultiplier = 1.15f;
            config.questExpMultiplier = 1.1f;
            config.questCreditMultiplier = 1.12f;
            config.shopPriceMultiplier = 1.05f;

            config.levelExpDifficultyMultipliers = new[] { 1f, 1f, 1.08f, 1.18f, 1.32f };
            config.levelCreditDifficultyMultipliers = new[] { 1f, 1f, 1.1f, 1.2f, 1.35f };
            config.questExpDifficultyMultipliers = new[] { 1f, 1f, 1.07f, 1.16f, 1.28f };
            config.questCreditDifficultyMultipliers = new[] { 1f, 1f, 1.08f, 1.18f, 1.3f };
            config.shopPriceDifficultyMultipliers = new[] { 1f, 1f, 1.06f, 1.14f, 1.22f };

            config.questTypeMultipliers = new List<QuestTypeRewardMultiplier>
            {
                new QuestTypeRewardMultiplier
                {
                    questType = QuestType.CompleteStronghold,
                    expMultiplier = 1.06f,
                    pearlMultiplier = 1f,
                    creditMultiplier = 1.08f
                },
                new QuestTypeRewardMultiplier
                {
                    questType = QuestType.BossBreak,
                    expMultiplier = 1.14f,
                    pearlMultiplier = 1f,
                    creditMultiplier = 1.18f
                },
                new QuestTypeRewardMultiplier
                {
                    questType = QuestType.BossDefeat,
                    expMultiplier = 1.2f,
                    pearlMultiplier = 1f,
                    creditMultiplier = 1.25f
                }
            };

            config.questTierMultipliers = new List<QuestTierRewardMultiplier>
            {
                new QuestTierRewardMultiplier
                {
                    tier = QuestRewardTier.Mainline,
                    expMultiplier = 1f,
                    pearlMultiplier = 1f,
                    creditMultiplier = 1f
                },
                new QuestTierRewardMultiplier
                {
                    tier = QuestRewardTier.Side,
                    expMultiplier = 1.05f,
                    pearlMultiplier = 1f,
                    creditMultiplier = 1.08f
                },
                new QuestTierRewardMultiplier
                {
                    tier = QuestRewardTier.Challenge,
                    expMultiplier = 1.18f,
                    pearlMultiplier = 1f,
                    creditMultiplier = 1.2f
                }
            };

            config.questChapterMultipliers = new List<QuestChapterRewardMultiplier>
            {
                new QuestChapterRewardMultiplier
                {
                    chapterId = 2,
                    expMultiplier = 1.05f,
                    pearlMultiplier = 1f,
                    creditMultiplier = 1.06f
                },
                new QuestChapterRewardMultiplier
                {
                    chapterId = 3,
                    expMultiplier = 1.13f,
                    pearlMultiplier = 1f,
                    creditMultiplier = 1.15f
                },
                new QuestChapterRewardMultiplier
                {
                    chapterId = 4,
                    expMultiplier = 1.22f,
                    pearlMultiplier = 1f,
                    creditMultiplier = 1.25f
                }
            };

            config.questStrongholdMultipliers = new List<QuestStrongholdRewardMultiplier>
            {
                new QuestStrongholdRewardMultiplier
                {
                    strongholdId = "SH_MID",
                    expMultiplier = 1.06f,
                    pearlMultiplier = 1f,
                    creditMultiplier = 1.08f
                },
                new QuestStrongholdRewardMultiplier
                {
                    strongholdId = "SH_LATE",
                    expMultiplier = 1.12f,
                    pearlMultiplier = 1f,
                    creditMultiplier = 1.15f
                }
            };

            return config;
        }

        private static LongChainNode NewLongChainNode(
            string questId,
            QuestType questType,
            QuestRewardTier rewardTier,
            int questDifficulty,
            int levelDifficulty,
            int chapterId,
            string targetStrongholdId,
            string fallbackStrongholdId,
            int baseExp,
            int baseCredits,
            bool failFirstAttempt)
        {
            return new LongChainNode
            {
                questId = questId,
                questType = questType,
                rewardTier = rewardTier,
                questDifficulty = questDifficulty,
                levelDifficulty = levelDifficulty,
                chapterId = chapterId,
                targetStrongholdId = targetStrongholdId,
                fallbackStrongholdId = fallbackStrongholdId,
                baseExp = baseExp,
                baseCredits = baseCredits,
                failFirstAttempt = failFirstAttempt
            };
        }

        private static PressureNode NewPressureNode(
            int questDifficulty,
            int levelDifficulty,
            QuestType questType,
            QuestRewardTier rewardTier,
            string strongholdId,
            int baseQuestCredits,
            int baseLevelCredits,
            int baseShopPrice,
            float questRewardMultiplier,
            float levelRewardMultiplier,
            float shopPriceMultiplier)
        {
            return new PressureNode
            {
                chapterId = Mathf.Max(2, levelDifficulty),
                questDifficulty = questDifficulty,
                levelDifficulty = levelDifficulty,
                questType = questType,
                rewardTier = rewardTier,
                strongholdId = strongholdId,
                baseQuestCredits = baseQuestCredits,
                baseLevelCredits = baseLevelCredits,
                baseShopPrice = baseShopPrice,
                questRewardMultiplier = questRewardMultiplier,
                levelRewardMultiplier = levelRewardMultiplier,
                shopPriceMultiplier = shopPriceMultiplier
            };
        }

        private struct LongChainNode
        {
            public string questId;
            public QuestType questType;
            public QuestRewardTier rewardTier;
            public int questDifficulty;
            public int levelDifficulty;
            public int chapterId;
            public string targetStrongholdId;
            public string fallbackStrongholdId;
            public int baseExp;
            public int baseCredits;
            public bool failFirstAttempt;
        }

        private struct PressureNode
        {
            public int chapterId;
            public int questDifficulty;
            public int levelDifficulty;
            public QuestType questType;
            public QuestRewardTier rewardTier;
            public string strongholdId;
            public int baseQuestCredits;
            public int baseLevelCredits;
            public int baseShopPrice;
            public float questRewardMultiplier;
            public float levelRewardMultiplier;
            public float shopPriceMultiplier;
        }
    }
}
