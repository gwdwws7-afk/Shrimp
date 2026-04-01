using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ThirdPersonController.Tests
{
    public class QuestFailureEconomyRound17RegressionTests
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
        public void QuestSystem_Round17_FailurePath_DoesNotAutoChain_RecoveryCompletionAutoChainsFollowup()
        {
            EconomyConfig config = BuildP1SimulationConfig();
            EconomyService.Configure(config);

            QuestSystem questSystem = CreateQuestRuntime(
                "QuestFailureEconomy_Round17_AutoChain",
                out CurrencyWallet wallet,
                out PlayerExperienceSystem experienceSystem);
            questSystem.autoStartGuidedQuests = true;
            questSystem.levelDifficulty = 4;
            questSystem.levelChapterId = 4;

            MethodInfo completeQuest = typeof(QuestSystem).GetMethod("CompleteQuest", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(completeQuest, "QuestSystem.CompleteQuest should exist.");

            QuestData recoveryQuest = NewQuestData(
                questId: "r17_recovery",
                questType: QuestType.CompleteStronghold,
                rewardTier: QuestRewardTier.Side,
                difficultyRating: 4,
                targetStrongholdId: "SH_LATE",
                expReward: 190,
                creditReward: 150,
                description: "Recover momentum by securing the route with low-risk objectives.");
            recoveryQuest.autoStartNextQuests = true;
            recoveryQuest.nextQuestIds = new List<string> { "r17_followup" };

            QuestData followupQuest = NewQuestData(
                questId: "r17_followup",
                questType: QuestType.BossBreak,
                rewardTier: QuestRewardTier.Challenge,
                difficultyRating: 4,
                targetStrongholdId: "SH_LATE",
                expReward: 230,
                creditReward: 180,
                description: "Push advantage once baseline economy has been restored.");
            followupQuest.autoStartNextQuests = false;

            QuestData failedQuest = NewQuestData(
                questId: "r17_failed",
                questType: QuestType.Protect,
                rewardTier: QuestRewardTier.Mainline,
                difficultyRating: 4,
                targetStrongholdId: "SH_LATE",
                expReward: 200,
                creditReward: 170,
                description: "Failure learning: prioritize survival and objective safety before aggression.");
            failedQuest.failOnPlayerDeath = true;
            failedQuest.autoStartNextQuests = true;
            failedQuest.nextQuestIds = new List<string> { "r17_recovery" };

            questSystem.availableQuests = new List<QuestData> { failedQuest, recoveryQuest, followupQuest };

            questSystem.StartQuest(failedQuest);
            QuestProgress failedProgress = FindQuestProgress(questSystem, "r17_failed", QuestStatus.InProgress);
            Assert.NotNull(failedProgress, "Failed quest should be active after StartQuest.");

            int expBeforeFail = experienceSystem.currentExp;
            int creditsBeforeFail = wallet.Credits;
            questSystem.FailQuest(failedProgress, "Round17 fail-path simulation");
            Assert.AreEqual(QuestStatus.Failed, failedProgress.status, "Fail path should move quest to Failed.");

            completeQuest.Invoke(questSystem, new object[] { failedProgress });
            Assert.AreEqual(expBeforeFail, experienceSystem.currentExp, "Fail path should not grant EXP.");
            Assert.AreEqual(creditsBeforeFail, wallet.Credits, "Fail path should not grant credits.");
            Assert.IsNull(FindQuestProgress(questSystem, "r17_recovery", QuestStatus.InProgress), "Failing should not auto-chain next quest.");

            questSystem.StartQuest(recoveryQuest);
            QuestProgress recoveryProgress = FindQuestProgress(questSystem, "r17_recovery", QuestStatus.InProgress);
            Assert.NotNull(recoveryProgress, "Recovery quest should be manually startable after failure.");

            int expectedRecoveryExp = ExpectedQuestExp(questSystem, recoveryQuest, "SH_LATE");
            int expectedRecoveryCredits = ExpectedQuestCredits(questSystem, recoveryQuest, "SH_LATE");

            completeQuest.Invoke(questSystem, new object[] { recoveryProgress });
            Assert.AreEqual(QuestStatus.Completed, recoveryProgress.status, "Recovery quest should complete.");
            Assert.AreEqual(expBeforeFail + expectedRecoveryExp, experienceSystem.currentExp, "Only recovery completion should grant EXP.");
            Assert.AreEqual(creditsBeforeFail + expectedRecoveryCredits, wallet.Credits, "Only recovery completion should grant credits.");

            QuestProgress followupProgress = FindQuestProgress(questSystem, "r17_followup", QuestStatus.InProgress);
            Assert.NotNull(followupProgress, "Recovery completion should auto-chain the follow-up quest.");
            Assert.AreEqual(1, CountQuestInstances(questSystem, "r17_followup"), "Follow-up quest should be auto-started exactly once.");

            completeQuest.Invoke(questSystem, new object[] { recoveryProgress });
            Assert.AreEqual(expBeforeFail + expectedRecoveryExp, experienceSystem.currentExp, "Re-completing recovery quest should not duplicate EXP.");
            Assert.AreEqual(creditsBeforeFail + expectedRecoveryCredits, wallet.Credits, "Re-completing recovery quest should not duplicate credits.");
            Assert.AreEqual(1, CountQuestInstances(questSystem, "r17_followup"), "Re-completion should not duplicate follow-up quest instance.");
        }

        [Test]
        public void EconomyService_Round17_FailureDebtAndRecoveryWindow_MidLateBand_IsControlled()
        {
            EconomyConfig config = BuildP1SimulationConfig();
            EconomyService.Configure(config);

            var scenarios = new List<DebtScenario>
            {
                NewDebtScenario(chapterId: 3, levelDifficulty: 3, strongholdId: "SH_MID", recoveryBaseCredits: 160, challengeBaseCredits: 220, recoveryLevelCredits: 140, challengeLevelCredits: 160, shopBasePrice: 300),
                NewDebtScenario(chapterId: 4, levelDifficulty: 4, strongholdId: "SH_LATE", recoveryBaseCredits: 170, challengeBaseCredits: 240, recoveryLevelCredits: 160, challengeLevelCredits: 180, shopBasePrice: 430),
                NewDebtScenario(chapterId: 4, levelDifficulty: 6, strongholdId: "SH_LATE", recoveryBaseCredits: 185, challengeBaseCredits: 260, recoveryLevelCredits: 180, challengeLevelCredits: 200, shopBasePrice: 480)
            };

            float firstRecoveryIncome = 0f;
            float lastRecoveryIncome = 0f;

            for (int i = 0; i < scenarios.Count; i++)
            {
                DebtScenario scenario = scenarios[i];

                int recoveryQuestCredits = EconomyService.AdjustQuestCredits(
                    baseCredits: scenario.recoveryBaseCredits,
                    questType: QuestType.CompleteStronghold,
                    questDifficulty: scenario.levelDifficulty,
                    levelDifficulty: scenario.levelDifficulty,
                    rewardMultiplier: 1.08f,
                    rewardTier: QuestRewardTier.Side,
                    chapterId: scenario.chapterId,
                    strongholdId: scenario.strongholdId);

                int challengeQuestCredits = EconomyService.AdjustQuestCredits(
                    baseCredits: scenario.challengeBaseCredits,
                    questType: QuestType.BossDefeat,
                    questDifficulty: scenario.levelDifficulty,
                    levelDifficulty: scenario.levelDifficulty,
                    rewardMultiplier: 1.2f,
                    rewardTier: QuestRewardTier.Challenge,
                    chapterId: scenario.chapterId,
                    strongholdId: scenario.strongholdId);

                int recoveryLevelCredits = EconomyService.AdjustLevelCredits(
                    baseCredits: scenario.recoveryLevelCredits,
                    difficulty: scenario.levelDifficulty,
                    levelRewardMultiplier: 1.1f);

                int challengeLevelCredits = EconomyService.AdjustLevelCredits(
                    baseCredits: scenario.challengeLevelCredits,
                    difficulty: scenario.levelDifficulty,
                    levelRewardMultiplier: 1.15f);

                float recoveryIncome = recoveryQuestCredits + recoveryLevelCredits;
                float challengeIncome = challengeQuestCredits + challengeLevelCredits;
                float debt = challengeIncome - recoveryIncome;

                int shopPrice = EconomyService.AdjustShopPrice(
                    basePrice: scenario.shopBasePrice,
                    difficulty: scenario.levelDifficulty,
                    priceMultiplier: 1f);

                float recoveryShopRatio = shopPrice > 0 ? recoveryIncome / shopPrice : 0f;
                float debtChallengeRatio = challengeIncome > 0f ? debt / challengeIncome : 0f;
                float debtShopRatio = shopPrice > 0 ? debt / shopPrice : 0f;

                Assert.Greater(challengeIncome, recoveryIncome, $"Challenge route should yield more income in scenario {i + 1}.");
                Assert.Greater(debt, 0f, $"Failure debt should be positive in scenario {i + 1}.");
                Assert.GreaterOrEqual(recoveryShopRatio, 1f, $"Recovery route should still afford one core shop purchase in scenario {i + 1}.");
                Assert.LessOrEqual(recoveryShopRatio, 2.4f, $"Recovery route should not over-inflate economy in scenario {i + 1}.");
                Assert.GreaterOrEqual(debtChallengeRatio, 0.2f, $"Debt ratio too small in scenario {i + 1}; failure cost is not meaningful.");
                Assert.LessOrEqual(debtChallengeRatio, 0.65f, $"Debt ratio too large in scenario {i + 1}; recovery becomes too punishing.");
                Assert.LessOrEqual(debtShopRatio, 1.6f, $"Debt should stay within a recoverable shop-pressure window in scenario {i + 1}.");

                if (i == 0)
                {
                    firstRecoveryIncome = recoveryIncome;
                }

                if (i == scenarios.Count - 1)
                {
                    lastRecoveryIncome = recoveryIncome;
                }
            }

            Assert.Greater(lastRecoveryIncome, firstRecoveryIncome, "Late recovery baseline should still grow versus mid-game.");
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
            experienceSystem.baseExpToNext = 300000;
            experienceSystem.expGrowth = 1f;
            experienceSystem.maxLevel = 99;
            experienceSystem.level = 1;
            experienceSystem.currentExp = 0;

            QuestSystem questSystem = root.AddComponent<QuestSystem>();
            questSystem.showRewardMessages = false;
            questSystem.autoSaveOnQuestComplete = false;
            questSystem.saveQuestRuntimeState = false;
            questSystem.autoStartGuidedQuests = true;
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
            QuestRewardTier rewardTier,
            int difficultyRating,
            string targetStrongholdId,
            int expReward,
            int creditReward,
            string description)
        {
            return new QuestData
            {
                questId = questId,
                questName = questId,
                description = description,
                questType = questType,
                rewardTier = rewardTier,
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

        private static QuestProgress FindQuestProgress(QuestSystem questSystem, string questId, QuestStatus status)
        {
            if (questSystem == null || string.IsNullOrEmpty(questId) || questSystem.activeQuests == null)
            {
                return null;
            }

            for (int i = 0; i < questSystem.activeQuests.Count; i++)
            {
                QuestProgress progress = questSystem.activeQuests[i];
                if (progress == null || progress.data == null)
                {
                    continue;
                }

                if (progress.data.questId == questId && progress.status == status)
                {
                    return progress;
                }
            }

            return null;
        }

        private static int CountQuestInstances(QuestSystem questSystem, string questId)
        {
            if (questSystem == null || string.IsNullOrEmpty(questId) || questSystem.activeQuests == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < questSystem.activeQuests.Count; i++)
            {
                QuestProgress progress = questSystem.activeQuests[i];
                if (progress != null && progress.data != null && progress.data.questId == questId)
                {
                    count++;
                }
            }

            return count;
        }

        private static int ExpectedQuestExp(QuestSystem questSystem, QuestData data, string fallbackStrongholdId)
        {
            string rewardStrongholdId = !string.IsNullOrEmpty(data.targetStrongholdId) ? data.targetStrongholdId : fallbackStrongholdId;
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
            string rewardStrongholdId = !string.IsNullOrEmpty(data.targetStrongholdId) ? data.targetStrongholdId : fallbackStrongholdId;
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

        private static DebtScenario NewDebtScenario(
            int chapterId,
            int levelDifficulty,
            string strongholdId,
            int recoveryBaseCredits,
            int challengeBaseCredits,
            int recoveryLevelCredits,
            int challengeLevelCredits,
            int shopBasePrice)
        {
            return new DebtScenario
            {
                chapterId = chapterId,
                levelDifficulty = levelDifficulty,
                strongholdId = strongholdId,
                recoveryBaseCredits = recoveryBaseCredits,
                challengeBaseCredits = challengeBaseCredits,
                recoveryLevelCredits = recoveryLevelCredits,
                challengeLevelCredits = challengeLevelCredits,
                shopBasePrice = shopBasePrice
            };
        }

        private struct DebtScenario
        {
            public int chapterId;
            public int levelDifficulty;
            public string strongholdId;
            public int recoveryBaseCredits;
            public int challengeBaseCredits;
            public int recoveryLevelCredits;
            public int challengeLevelCredits;
            public int shopBasePrice;
        }
    }
}
