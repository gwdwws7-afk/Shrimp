using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ThirdPersonController.Tests
{
    public class QuestFailureEconomyRound16RegressionTests
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
        public void QuestSystem_Round16_FailureLearningCurve_RecoveryChain_IsStableAndFair()
        {
            EconomyConfig config = BuildP1SimulationConfig();
            EconomyService.Configure(config);

            QuestSystem questSystem = CreateQuestRuntime(
                "QuestFailureEconomy_Round16",
                out CurrencyWallet wallet,
                out PlayerExperienceSystem experienceSystem);

            MethodInfo completeQuest = typeof(QuestSystem).GetMethod("CompleteQuest", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(completeQuest, "QuestSystem.CompleteQuest should exist.");

            int failedEventCount = 0;
            questSystem.OnQuestFailed += _ => failedEventCount++;

            var nodes = new List<FailureRecoveryNode>
            {
                NewFailureRecoveryNode(2, 2, FailureRuleType.PlayerDeath, "SH_MID", QuestType.CompleteStronghold, QuestRewardTier.Side, 145, 105, 220),
                NewFailureRecoveryNode(3, 3, FailureRuleType.QuestTimer, "SH_MID", QuestType.Kill, QuestRewardTier.Side, 175, 130, 260),
                NewFailureRecoveryNode(4, 4, FailureRuleType.StageTimer, "SH_LATE", QuestType.BossBreak, QuestRewardTier.Challenge, 210, 165, 320),
                NewFailureRecoveryNode(4, 6, FailureRuleType.DefenseTarget, "SH_LATE", QuestType.BossDefeat, QuestRewardTier.Challenge, 235, 185, 360)
            };

            int expectedExp = 0;
            int expectedCredits = 0;
            float midRecoveryRatioSum = 0f;
            int midRecoveryCount = 0;
            float lateRecoveryRatioSum = 0f;
            int lateRecoveryCount = 0;

            for (int i = 0; i < nodes.Count; i++)
            {
                FailureRecoveryNode node = nodes[i];
                questSystem.levelDifficulty = node.levelDifficulty;
                questSystem.levelChapterId = node.chapterId;

                QuestData failureQuest = NewQuestData(
                    questId: $"r16_fail_{i}",
                    questType: QuestType.Protect,
                    rewardTier: QuestRewardTier.Mainline,
                    difficultyRating: node.levelDifficulty,
                    targetStrongholdId: node.strongholdId,
                    expReward: 120,
                    creditReward: 90,
                    description: $"Failure learning sample {i + 1}: watch timer/target cues and reprioritize objective.");
                ApplyFailureRule(failureQuest, node.failureRuleType, node.strongholdId);
                Assert.IsTrue(HasFailureRule(failureQuest), $"Failure quest should declare a failure rule: {failureQuest.questId}");
                Assert.IsTrue(IsMeaningfulLearningText(failureQuest.description), $"Failure quest should include meaningful learning text: {failureQuest.questId}");

                QuestProgress failedProgress = NewQuestProgress(failureQuest, node.strongholdId);
                questSystem.activeQuests.Add(failedProgress);

                int expBeforeFail = experienceSystem.currentExp;
                int creditsBeforeFail = wallet.Credits;

                questSystem.FailQuest(failedProgress, "Round16 failure learning simulation");
                Assert.AreEqual(QuestStatus.Failed, failedProgress.status, $"Fail quest should transition to Failed: {failedProgress.data.questId}");

                completeQuest.Invoke(questSystem, new object[] { failedProgress });
                Assert.AreEqual(expBeforeFail, experienceSystem.currentExp, $"Failed quest should not grant EXP: {failedProgress.data.questId}");
                Assert.AreEqual(creditsBeforeFail, wallet.Credits, $"Failed quest should not grant credits: {failedProgress.data.questId}");

                QuestData recoveryQuest = NewQuestData(
                    questId: $"r16_recover_{i}",
                    questType: node.recoveryQuestType,
                    rewardTier: node.recoveryRewardTier,
                    difficultyRating: node.levelDifficulty,
                    targetStrongholdId: node.strongholdId,
                    expReward: node.recoveryBaseExp,
                    creditReward: node.recoveryBaseCredits,
                    description: $"Recovery route {i + 1}: switch to safer execution path and rebuild economy buffer.");
                QuestProgress recoveryProgress = NewQuestProgress(recoveryQuest, node.strongholdId);
                questSystem.activeQuests.Add(recoveryProgress);

                int expDelta = ExpectedQuestExp(questSystem, recoveryQuest, node.strongholdId);
                int creditDelta = ExpectedQuestCredits(questSystem, recoveryQuest, node.strongholdId);
                int levelCreditDelta = EconomyService.AdjustLevelCredits(
                    baseCredits: 110 + (i * 20),
                    difficulty: node.levelDifficulty,
                    levelRewardMultiplier: questSystem.levelRewardMultiplier);

                completeQuest.Invoke(questSystem, new object[] { recoveryProgress });
                Assert.AreEqual(QuestStatus.Completed, recoveryProgress.status, $"Recovery quest should complete: {recoveryQuest.questId}");

                expectedExp += expDelta;
                expectedCredits += creditDelta;

                int shopPrice = EconomyService.AdjustShopPrice(
                    basePrice: node.shopBasePrice,
                    difficulty: node.levelDifficulty,
                    priceMultiplier: 1f);
                float recoveryRatio = shopPrice > 0 ? (creditDelta + levelCreditDelta) / (float)shopPrice : 0f;
                Assert.GreaterOrEqual(recoveryRatio, 0.9f, $"Recovery window too narrow on node {i + 1}.");
                Assert.LessOrEqual(recoveryRatio, 2.8f, $"Recovery window too loose on node {i + 1}.");

                if (node.chapterId >= 4)
                {
                    lateRecoveryRatioSum += recoveryRatio;
                    lateRecoveryCount++;
                }
                else
                {
                    midRecoveryRatioSum += recoveryRatio;
                    midRecoveryCount++;
                }
            }

            Assert.AreEqual(nodes.Count, failedEventCount, "Each failure sample should emit one quest-failed event.");
            Assert.AreEqual(expectedExp, experienceSystem.currentExp, "Round16 recovery chain EXP should equal successful recovery quests.");
            Assert.AreEqual(expectedCredits, wallet.Credits, "Round16 recovery chain credits should equal successful recovery quests.");

            float midRecoveryRatio = midRecoveryCount > 0 ? midRecoveryRatioSum / midRecoveryCount : 0f;
            float lateRecoveryRatio = lateRecoveryCount > 0 ? lateRecoveryRatioSum / lateRecoveryCount : 0f;
            Assert.GreaterOrEqual(lateRecoveryRatio, midRecoveryRatio * 0.95f, "Late recovery ratio should not collapse versus mid game.");
            Assert.LessOrEqual(lateRecoveryRatio, midRecoveryRatio * 1.85f, "Late recovery ratio should not inflate excessively.");
        }

        [Test]
        public void EconomyService_Round16_MidLateRecoveryWindow_VersusChallengeRoute_RemainsBalanced()
        {
            EconomyConfig config = BuildP1SimulationConfig();
            EconomyService.Configure(config);

            var scenarios = new List<RecoveryWindowScenario>
            {
                NewRecoveryWindowScenario(chapterId: 3, levelDifficulty: 3, strongholdId: "SH_MID", recoveryBaseCredits: 150, challengeBaseCredits: 210, baseLevelCredits: 130, baseShopPrice: 270),
                NewRecoveryWindowScenario(chapterId: 4, levelDifficulty: 4, strongholdId: "SH_LATE", recoveryBaseCredits: 170, challengeBaseCredits: 240, baseLevelCredits: 150, baseShopPrice: 320),
                NewRecoveryWindowScenario(chapterId: 4, levelDifficulty: 6, strongholdId: "SH_LATE", recoveryBaseCredits: 180, challengeBaseCredits: 255, baseLevelCredits: 170, baseShopPrice: 360)
            };

            float firstRecoveryIncome = 0f;
            float lastRecoveryIncome = 0f;

            for (int i = 0; i < scenarios.Count; i++)
            {
                RecoveryWindowScenario scenario = scenarios[i];

                int recoveryQuestCredits = EconomyService.AdjustQuestCredits(
                    baseCredits: scenario.recoveryBaseCredits,
                    questType: QuestType.CompleteStronghold,
                    questDifficulty: scenario.levelDifficulty,
                    levelDifficulty: scenario.levelDifficulty,
                    rewardMultiplier: 1.08f,
                    rewardTier: QuestRewardTier.Side,
                    chapterId: scenario.chapterId,
                    strongholdId: scenario.strongholdId);

                int recoveryLevelCredits = EconomyService.AdjustLevelCredits(
                    baseCredits: scenario.baseLevelCredits,
                    difficulty: scenario.levelDifficulty,
                    levelRewardMultiplier: 1.1f);

                int challengeQuestCredits = EconomyService.AdjustQuestCredits(
                    baseCredits: scenario.challengeBaseCredits,
                    questType: QuestType.BossDefeat,
                    questDifficulty: scenario.levelDifficulty,
                    levelDifficulty: scenario.levelDifficulty,
                    rewardMultiplier: 1.2f,
                    rewardTier: QuestRewardTier.Challenge,
                    chapterId: scenario.chapterId,
                    strongholdId: scenario.strongholdId);

                int challengeLevelCredits = EconomyService.AdjustLevelCredits(
                    baseCredits: scenario.baseLevelCredits + 20,
                    difficulty: scenario.levelDifficulty,
                    levelRewardMultiplier: 1.15f);

                float recoveryIncome = recoveryQuestCredits + recoveryLevelCredits;
                float challengeIncome = challengeQuestCredits + challengeLevelCredits;
                int shopPrice = EconomyService.AdjustShopPrice(
                    basePrice: scenario.baseShopPrice,
                    difficulty: scenario.levelDifficulty,
                    priceMultiplier: 1f);

                float recoveryRatio = shopPrice > 0 ? recoveryIncome / shopPrice : 0f;
                float challengeRatio = shopPrice > 0 ? challengeIncome / shopPrice : 0f;

                Assert.GreaterOrEqual(recoveryRatio, 1f, $"Recovery route ratio too low in scenario {i + 1}.");
                Assert.LessOrEqual(recoveryRatio, 2.8f, $"Recovery route ratio too high in scenario {i + 1}.");
                Assert.Greater(challengeRatio, recoveryRatio, $"Challenge route should yield stronger economy output in scenario {i + 1}.");
                Assert.GreaterOrEqual(challengeIncome, recoveryIncome * 1.15f, $"Challenge route premium too small in scenario {i + 1}.");
                Assert.LessOrEqual(challengeIncome, recoveryIncome * 2f, $"Challenge route premium too large in scenario {i + 1}.");

                if (i == 0)
                {
                    firstRecoveryIncome = recoveryIncome;
                }

                if (i == scenarios.Count - 1)
                {
                    lastRecoveryIncome = recoveryIncome;
                }
            }

            Assert.Greater(lastRecoveryIncome, firstRecoveryIncome, "Late-game recovery income should exceed mid-game recovery income.");
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

        private static QuestProgress NewQuestProgress(QuestData data, string fallbackStrongholdId)
        {
            return new QuestProgress
            {
                data = data,
                status = QuestStatus.InProgress,
                lastStrongholdId = fallbackStrongholdId
            };
        }

        private static void ApplyFailureRule(QuestData quest, FailureRuleType rule, string strongholdId)
        {
            if (quest == null)
            {
                return;
            }

            switch (rule)
            {
                case FailureRuleType.QuestTimer:
                    quest.timeLimit = 75f;
                    break;
                case FailureRuleType.StageTimer:
                    quest.stages = new List<QuestStage>
                    {
                        new QuestStage
                        {
                            stageId = "r16_stage_timer",
                            description = "Finish this objective before the timer expires to preserve momentum.",
                            questType = QuestType.Kill,
                            targetCount = 8,
                            useTimeLimit = true,
                            timeLimit = 20f
                        }
                    };
                    break;
                case FailureRuleType.PlayerDeath:
                    quest.failOnPlayerDeath = true;
                    break;
                case FailureRuleType.DefenseTarget:
                    quest.failOnDefenseTargetDestroyed = true;
                    quest.questType = QuestType.Protect;
                    quest.targetStrongholdId = strongholdId;
                    break;
            }
        }

        private static bool HasFailureRule(QuestData quest)
        {
            if (quest == null)
            {
                return false;
            }

            if (quest.timeLimit > 0f || quest.failOnPlayerDeath || quest.failOnGameOver || quest.failOnDefenseTargetDestroyed)
            {
                return true;
            }

            if (quest.stages == null)
            {
                return false;
            }

            for (int i = 0; i < quest.stages.Count; i++)
            {
                QuestStage stage = quest.stages[i];
                if (stage != null && stage.useTimeLimit && stage.timeLimit > 0f)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsMeaningfulLearningText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string trimmed = text.Trim();
            return trimmed.Length >= 8;
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

        private static FailureRecoveryNode NewFailureRecoveryNode(
            int chapterId,
            int levelDifficulty,
            FailureRuleType failureRuleType,
            string strongholdId,
            QuestType recoveryQuestType,
            QuestRewardTier recoveryRewardTier,
            int recoveryBaseExp,
            int recoveryBaseCredits,
            int shopBasePrice)
        {
            return new FailureRecoveryNode
            {
                chapterId = chapterId,
                levelDifficulty = levelDifficulty,
                failureRuleType = failureRuleType,
                strongholdId = strongholdId,
                recoveryQuestType = recoveryQuestType,
                recoveryRewardTier = recoveryRewardTier,
                recoveryBaseExp = recoveryBaseExp,
                recoveryBaseCredits = recoveryBaseCredits,
                shopBasePrice = shopBasePrice
            };
        }

        private static RecoveryWindowScenario NewRecoveryWindowScenario(
            int chapterId,
            int levelDifficulty,
            string strongholdId,
            int recoveryBaseCredits,
            int challengeBaseCredits,
            int baseLevelCredits,
            int baseShopPrice)
        {
            return new RecoveryWindowScenario
            {
                chapterId = chapterId,
                levelDifficulty = levelDifficulty,
                strongholdId = strongholdId,
                recoveryBaseCredits = recoveryBaseCredits,
                challengeBaseCredits = challengeBaseCredits,
                baseLevelCredits = baseLevelCredits,
                baseShopPrice = baseShopPrice
            };
        }

        private enum FailureRuleType
        {
            QuestTimer,
            StageTimer,
            PlayerDeath,
            DefenseTarget
        }

        private struct FailureRecoveryNode
        {
            public int chapterId;
            public int levelDifficulty;
            public FailureRuleType failureRuleType;
            public string strongholdId;
            public QuestType recoveryQuestType;
            public QuestRewardTier recoveryRewardTier;
            public int recoveryBaseExp;
            public int recoveryBaseCredits;
            public int shopBasePrice;
        }

        private struct RecoveryWindowScenario
        {
            public int chapterId;
            public int levelDifficulty;
            public string strongholdId;
            public int recoveryBaseCredits;
            public int challengeBaseCredits;
            public int baseLevelCredits;
            public int baseShopPrice;
        }
    }
}
