using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ThirdPersonController.Tests
{
    public class QuestEconomyP1MidLateSimulationRegressionTests
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
        public void EconomyService_P1MidLateCurve_QuestAndLevelIncome_IncreasesFromMidToLateGame()
        {
            EconomyConfig config = BuildP1SimulationConfig();
            EconomyService.Configure(config);

            int midQuestExp = EconomyService.AdjustQuestExp(
                baseExp: 180,
                questType: QuestType.CompleteStronghold,
                questDifficulty: 2,
                levelDifficulty: 2,
                rewardMultiplier: 1.05f,
                rewardTier: QuestRewardTier.Mainline,
                chapterId: 2,
                strongholdId: "SH_MID");

            int lateQuestExp = EconomyService.AdjustQuestExp(
                baseExp: 180,
                questType: QuestType.BossDefeat,
                questDifficulty: 4,
                levelDifficulty: 4,
                rewardMultiplier: 1.2f,
                rewardTier: QuestRewardTier.Challenge,
                chapterId: 4,
                strongholdId: "SH_LATE");

            int midQuestCredits = EconomyService.AdjustQuestCredits(
                baseCredits: 140,
                questType: QuestType.CompleteStronghold,
                questDifficulty: 2,
                levelDifficulty: 2,
                rewardMultiplier: 1.05f,
                rewardTier: QuestRewardTier.Mainline,
                chapterId: 2,
                strongholdId: "SH_MID");

            int lateQuestCredits = EconomyService.AdjustQuestCredits(
                baseCredits: 140,
                questType: QuestType.BossDefeat,
                questDifficulty: 4,
                levelDifficulty: 4,
                rewardMultiplier: 1.2f,
                rewardTier: QuestRewardTier.Challenge,
                chapterId: 4,
                strongholdId: "SH_LATE");

            int midLevelCredits = EconomyService.AdjustLevelCredits(220, difficulty: 2, levelRewardMultiplier: 1.08f);
            int lateLevelCredits = EconomyService.AdjustLevelCredits(220, difficulty: 4, levelRewardMultiplier: 1.25f);

            Assert.Greater(lateQuestExp, midQuestExp, "Late-game quest EXP should be higher than mid-game baseline.");
            Assert.Greater(lateQuestCredits, midQuestCredits, "Late-game quest credits should be higher than mid-game baseline.");
            Assert.Greater(lateLevelCredits, midLevelCredits, "Late-game level credits should be higher than mid-game baseline.");
        }

        [Test]
        public void EconomyService_P1MidLateCurve_RewardToShopPressure_StaysInPlayableBand()
        {
            EconomyConfig config = BuildP1SimulationConfig();
            EconomyService.Configure(config);

            for (int difficulty = 2; difficulty <= 4; difficulty++)
            {
                int chapterId = difficulty;
                string strongholdId = difficulty >= 4 ? "SH_LATE" : "SH_MID";
                QuestRewardTier tier = difficulty >= 4 ? QuestRewardTier.Challenge : QuestRewardTier.Mainline;
                QuestType questType = difficulty >= 4 ? QuestType.BossDefeat : QuestType.CompleteStronghold;

                int questCredits = EconomyService.AdjustQuestCredits(
                    baseCredits: 160,
                    questType: questType,
                    questDifficulty: difficulty,
                    levelDifficulty: difficulty,
                    rewardMultiplier: 1.1f,
                    rewardTier: tier,
                    chapterId: chapterId,
                    strongholdId: strongholdId);

                int levelCredits = EconomyService.AdjustLevelCredits(
                    baseCredits: 120,
                    difficulty: difficulty,
                    levelRewardMultiplier: 1.1f);

                int income = questCredits + levelCredits;
                int shopPrice = EconomyService.AdjustShopPrice(basePrice: 200, difficulty: difficulty, priceMultiplier: 1f);
                float ratio = shopPrice > 0 ? income / (float)shopPrice : 0f;

                Assert.GreaterOrEqual(ratio, 1f, $"difficulty={difficulty} ratio too low, pressure is too harsh.");
                Assert.LessOrEqual(ratio, 4f, $"difficulty={difficulty} ratio too high, pressure is too loose.");
            }
        }

        [Test]
        public void QuestSystem_P1MidLateSimulation_CompletionChain_AccumulatesExpectedRewards()
        {
            EconomyConfig config = BuildP1SimulationConfig();
            EconomyService.Configure(config);

            GameObject root = new GameObject("QuestEconomyP1MidLate");
            createdObjects.Add(root);

            CurrencyWallet wallet = root.AddComponent<CurrencyWallet>();
            wallet.showMessages = false;
            wallet.SetCredits(0);

            PlayerExperienceSystem experienceSystem = root.AddComponent<PlayerExperienceSystem>();
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

            MethodInfo completeQuest = typeof(QuestSystem).GetMethod("CompleteQuest", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(completeQuest, "QuestSystem.CompleteQuest should exist.");

            var questInputs = new List<QuestInput>
            {
                new QuestInput
                {
                    data = NewQuestData("p1_q_01", QuestType.CompleteStronghold, QuestRewardTier.Mainline, 2, "SH_MID", 120, 90),
                    fallbackStrongholdId = "SH_MID"
                },
                new QuestInput
                {
                    data = NewQuestData("p1_q_02", QuestType.Kill, QuestRewardTier.Side, 3, string.Empty, 150, 110),
                    fallbackStrongholdId = "SH_MID"
                },
                new QuestInput
                {
                    data = NewQuestData("p1_q_03", QuestType.BossBreak, QuestRewardTier.Challenge, 3, string.Empty, 180, 140),
                    fallbackStrongholdId = "SH_LATE"
                },
                new QuestInput
                {
                    data = NewQuestData("p1_q_04", QuestType.BossDefeat, QuestRewardTier.Challenge, 4, "SH_LATE", 220, 180),
                    fallbackStrongholdId = "SH_LATE"
                }
            };

            int expectedExp = 0;
            int expectedCredits = 0;

            for (int i = 0; i < questInputs.Count; i++)
            {
                QuestInput input = questInputs[i];
                QuestProgress progress = new QuestProgress
                {
                    data = input.data,
                    status = QuestStatus.InProgress,
                    lastStrongholdId = input.fallbackStrongholdId
                };
                questSystem.activeQuests.Add(progress);

                string rewardStrongholdId = !string.IsNullOrEmpty(input.data.targetStrongholdId)
                    ? input.data.targetStrongholdId
                    : input.fallbackStrongholdId;

                expectedExp += EconomyService.AdjustQuestExp(
                    baseExp: input.data.reward.exp,
                    questType: input.data.questType,
                    questDifficulty: input.data.difficultyRating,
                    levelDifficulty: questSystem.levelDifficulty,
                    rewardMultiplier: questSystem.expRewardMultiplier * questSystem.levelRewardMultiplier,
                    rewardTier: input.data.rewardTier,
                    chapterId: questSystem.levelChapterId,
                    strongholdId: rewardStrongholdId);

                expectedCredits += EconomyService.AdjustQuestCredits(
                    baseCredits: input.data.reward.credits,
                    questType: input.data.questType,
                    questDifficulty: input.data.difficultyRating,
                    levelDifficulty: questSystem.levelDifficulty,
                    rewardMultiplier: questSystem.levelRewardMultiplier,
                    rewardTier: input.data.rewardTier,
                    chapterId: questSystem.levelChapterId,
                    strongholdId: rewardStrongholdId);

                completeQuest.Invoke(questSystem, new object[] { progress });
                Assert.AreEqual(QuestStatus.Completed, progress.status, $"Quest should complete: {input.data.questId}");
            }

            Assert.AreEqual(expectedExp, experienceSystem.currentExp, "Quest chain EXP should match mid-late routing formula.");
            Assert.AreEqual(expectedCredits, wallet.Credits, "Quest chain credits should match mid-late routing formula.");
            Assert.Greater(wallet.Credits, 0, "Wallet should have positive credits after chain completion.");
        }

        [Test]
        public void EconomyService_P1MidLateCurve_UnknownChapterStronghold_FallsBackToNeutralMultiplier()
        {
            EconomyConfig config = BuildP1SimulationConfig();
            EconomyService.Configure(config);

            int neutralExp = EconomyService.AdjustQuestExp(
                baseExp: 160,
                questType: QuestType.BossBreak,
                questDifficulty: 3,
                levelDifficulty: 3,
                rewardMultiplier: 1.1f,
                rewardTier: QuestRewardTier.Challenge,
                chapterId: 0,
                strongholdId: string.Empty);

            int unknownExp = EconomyService.AdjustQuestExp(
                baseExp: 160,
                questType: QuestType.BossBreak,
                questDifficulty: 3,
                levelDifficulty: 3,
                rewardMultiplier: 1.1f,
                rewardTier: QuestRewardTier.Challenge,
                chapterId: 99,
                strongholdId: "SH_UNKNOWN");

            int mappedExp = EconomyService.AdjustQuestExp(
                baseExp: 160,
                questType: QuestType.BossBreak,
                questDifficulty: 3,
                levelDifficulty: 3,
                rewardMultiplier: 1.1f,
                rewardTier: QuestRewardTier.Challenge,
                chapterId: 4,
                strongholdId: "SH_LATE");

            Assert.AreEqual(neutralExp, unknownExp, "Unknown chapter/stronghold should resolve as neutral multiplier.");
            Assert.Greater(mappedExp, neutralExp, "Mapped late-game chapter/stronghold should provide stronger reward than neutral fallback.");
        }

        [Test]
        public void QuestSystem_P1MidLateSimulation_CompletedQuest_DoesNotPayRewardTwice()
        {
            EconomyConfig config = BuildP1SimulationConfig();
            EconomyService.Configure(config);

            GameObject root = new GameObject("QuestEconomyP1MidLate_NoDoublePay");
            createdObjects.Add(root);

            CurrencyWallet wallet = root.AddComponent<CurrencyWallet>();
            wallet.showMessages = false;
            wallet.SetCredits(0);

            PlayerExperienceSystem experienceSystem = root.AddComponent<PlayerExperienceSystem>();
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

            MethodInfo completeQuest = typeof(QuestSystem).GetMethod("CompleteQuest", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(completeQuest, "QuestSystem.CompleteQuest should exist.");

            QuestData data = NewQuestData("p1_q_double", QuestType.BossDefeat, QuestRewardTier.Challenge, 4, "SH_LATE", 220, 180);
            QuestProgress progress = new QuestProgress
            {
                data = data,
                status = QuestStatus.InProgress,
                lastStrongholdId = "SH_LATE"
            };
            questSystem.activeQuests.Add(progress);

            completeQuest.Invoke(questSystem, new object[] { progress });
            int expAfterFirst = experienceSystem.currentExp;
            int creditsAfterFirst = wallet.Credits;
            Assert.AreEqual(QuestStatus.Completed, progress.status, "Quest should be completed after first completion.");
            Assert.Greater(expAfterFirst, 0, "First completion should grant EXP.");
            Assert.Greater(creditsAfterFirst, 0, "First completion should grant credits.");

            completeQuest.Invoke(questSystem, new object[] { progress });
            Assert.AreEqual(expAfterFirst, experienceSystem.currentExp, "Second completion call should not duplicate EXP reward.");
            Assert.AreEqual(creditsAfterFirst, wallet.Credits, "Second completion call should not duplicate credit reward.");
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

        private struct QuestInput
        {
            public QuestData data;
            public string fallbackStrongholdId;
        }
    }
}
