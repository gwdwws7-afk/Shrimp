using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ThirdPersonController.Tests
{
    public class QuestFailureEconomyRound18RegressionTests
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
        public void QuestSystem_Round18_FailureTypeByChapterMatrix_TriggersNativeFailPaths_AndKeepsRecoveryWindowPlayable()
        {
            EconomyConfig config = BuildP1SimulationConfig();
            EconomyService.Configure(config);

            QuestSystem questSystem = CreateQuestRuntime(
                "QuestFailureEconomy_Round18_Matrix",
                out CurrencyWallet wallet,
                out PlayerExperienceSystem experienceSystem);

            MethodInfo completeQuest = typeof(QuestSystem).GetMethod("CompleteQuest", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo updateQuestTimers = typeof(QuestSystem).GetMethod("UpdateQuestTimers", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo handleDefenseTargetDestroyed = typeof(QuestSystem).GetMethod("HandleDefenseTargetDestroyed", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo handlePlayerDeath = typeof(QuestSystem).GetMethod("HandlePlayerDeath", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo handleGameOver = typeof(QuestSystem).GetMethod("HandleGameOver", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(completeQuest, "QuestSystem.CompleteQuest should exist.");
            Assert.NotNull(updateQuestTimers, "QuestSystem.UpdateQuestTimers should exist.");
            Assert.NotNull(handleDefenseTargetDestroyed, "QuestSystem.HandleDefenseTargetDestroyed should exist.");
            Assert.NotNull(handlePlayerDeath, "QuestSystem.HandlePlayerDeath should exist.");
            Assert.NotNull(handleGameOver, "QuestSystem.HandleGameOver should exist.");

            List<FailureMatrixScenario> scenarios = BuildRound18MatrixScenarios();

            var failedCounts = new Dictionary<FailureRuleType, int>();
            for (int i = 0; i < 5; i++)
            {
                failedCounts[(FailureRuleType)i] = 0;
            }

            int expectedExpTotal = 0;
            int expectedCreditsTotal = 0;
            float midRatioSum = 0f;
            int midRatioCount = 0;
            float lateRatioSum = 0f;
            int lateRatioCount = 0;

            for (int i = 0; i < scenarios.Count; i++)
            {
                FailureMatrixScenario scenario = scenarios[i];
                questSystem.levelDifficulty = scenario.levelDifficulty;
                questSystem.levelChapterId = scenario.chapterId;

                QuestData failureQuest = NewQuestData(
                    questId: $"r18_fail_{scenario.chapterId}_{scenario.failureRuleType}_{i}",
                    questType: scenario.recoveryQuestType,
                    rewardTier: QuestRewardTier.Mainline,
                    difficultyRating: scenario.levelDifficulty,
                    targetStrongholdId: scenario.strongholdId,
                    expReward: 140,
                    creditReward: 110,
                    description: $"Failure learning matrix sample {i + 1}: identify fail cause and pivot to recovery plan.");
                ApplyFailureRule(failureQuest, scenario.failureRuleType, scenario.strongholdId);
                Assert.IsTrue(HasFailureRule(failureQuest), $"Failure scenario must have an explicit failure rule: {failureQuest.questId}");
                Assert.IsTrue(IsMeaningfulLearningText(failureQuest.description), $"Failure scenario needs usable learning text: {failureQuest.questId}");

                questSystem.StartQuest(failureQuest);
                QuestProgress failureProgress = FindQuestProgress(questSystem, failureQuest.questId, QuestStatus.InProgress);
                Assert.NotNull(failureProgress, $"Failure quest should be active: {failureQuest.questId}");

                int expBeforeFail = experienceSystem.currentExp;
                int creditsBeforeFail = wallet.Credits;

                TriggerFailure(
                    questSystem,
                    failureProgress,
                    scenario,
                    updateQuestTimers,
                    handleDefenseTargetDestroyed,
                    handlePlayerDeath,
                    handleGameOver);

                Assert.AreEqual(QuestStatus.Failed, failureProgress.status, $"Failure rule should transition to Failed: {failureQuest.questId}");
                failedCounts[scenario.failureRuleType]++;

                completeQuest.Invoke(questSystem, new object[] { failureProgress });
                Assert.AreEqual(expBeforeFail, experienceSystem.currentExp, $"Failed quest must not grant EXP: {failureQuest.questId}");
                Assert.AreEqual(creditsBeforeFail, wallet.Credits, $"Failed quest must not grant credits: {failureQuest.questId}");

                QuestData recoveryQuest = NewQuestData(
                    questId: $"r18_recover_{scenario.chapterId}_{scenario.failureRuleType}_{i}",
                    questType: scenario.recoveryQuestType,
                    rewardTier: scenario.recoveryRewardTier,
                    difficultyRating: scenario.levelDifficulty,
                    targetStrongholdId: scenario.strongholdId,
                    expReward: scenario.recoveryBaseExp,
                    creditReward: scenario.recoveryBaseCredits,
                    description: $"Recovery route {i + 1}: stabilize economy and rebuild tempo after failure.");

                questSystem.StartQuest(recoveryQuest);
                QuestProgress recoveryProgress = FindQuestProgress(questSystem, recoveryQuest.questId, QuestStatus.InProgress);
                Assert.NotNull(recoveryProgress, $"Recovery quest should be active: {recoveryQuest.questId}");

                int expDelta = ExpectedQuestExp(questSystem, recoveryQuest, scenario.strongholdId);
                int creditDelta = ExpectedQuestCredits(questSystem, recoveryQuest, scenario.strongholdId);
                int levelCreditDelta = EconomyService.AdjustLevelCredits(
                    baseCredits: scenario.recoveryLevelCredits,
                    difficulty: scenario.levelDifficulty,
                    levelRewardMultiplier: questSystem.levelRewardMultiplier);

                completeQuest.Invoke(questSystem, new object[] { recoveryProgress });
                Assert.AreEqual(QuestStatus.Completed, recoveryProgress.status, $"Recovery quest should complete: {recoveryQuest.questId}");

                expectedExpTotal += expDelta;
                expectedCreditsTotal += creditDelta;

                int shopPrice = EconomyService.AdjustShopPrice(
                    basePrice: scenario.shopBasePrice,
                    difficulty: scenario.levelDifficulty,
                    priceMultiplier: 1f);
                float ratio = shopPrice > 0 ? (creditDelta + levelCreditDelta) / (float)shopPrice : 0f;
                Assert.GreaterOrEqual(ratio, 0.9f, $"Recovery window too tight at scenario {i + 1}.");
                Assert.LessOrEqual(ratio, 2.7f, $"Recovery window too loose at scenario {i + 1}.");

                if (scenario.chapterId >= 4)
                {
                    lateRatioSum += ratio;
                    lateRatioCount++;
                }
                else
                {
                    midRatioSum += ratio;
                    midRatioCount++;
                }
            }

            foreach (KeyValuePair<FailureRuleType, int> pair in failedCounts)
            {
                Assert.GreaterOrEqual(pair.Value, 2, $"Failure type coverage should include both chapters: {pair.Key}");
            }

            Assert.AreEqual(expectedExpTotal, experienceSystem.currentExp, "Round18 matrix EXP should equal successful recovery payouts only.");
            Assert.AreEqual(expectedCreditsTotal, wallet.Credits, "Round18 matrix credits should equal successful recovery payouts only.");

            float midRatio = midRatioCount > 0 ? midRatioSum / midRatioCount : 0f;
            float lateRatio = lateRatioCount > 0 ? lateRatioSum / lateRatioCount : 0f;
            Assert.GreaterOrEqual(lateRatio, midRatio * 0.9f, "Late recovery ratio should not collapse below mid baseline.");
            Assert.LessOrEqual(lateRatio, midRatio * 1.95f, "Late recovery ratio should not inflate out of band.");
        }

        [Test]
        public void EconomyService_Round18_FailureTypeByChapterRecoveryDebtMatrix_RemainsRecoverable()
        {
            EconomyConfig config = BuildP1SimulationConfig();
            EconomyService.Configure(config);

            List<FailureMatrixScenario> scenarios = BuildRound18MatrixScenarios();

            float midDebtShopRatioSum = 0f;
            int midDebtCount = 0;
            float lateDebtShopRatioSum = 0f;
            int lateDebtCount = 0;

            for (int i = 0; i < scenarios.Count; i++)
            {
                FailureMatrixScenario scenario = scenarios[i];

                int recoveryQuestCredits = EconomyService.AdjustQuestCredits(
                    baseCredits: scenario.recoveryBaseCredits,
                    questType: scenario.recoveryQuestType,
                    questDifficulty: scenario.levelDifficulty,
                    levelDifficulty: scenario.levelDifficulty,
                    rewardMultiplier: 1.08f,
                    rewardTier: scenario.recoveryRewardTier,
                    chapterId: scenario.chapterId,
                    strongholdId: scenario.strongholdId);

                int challengeQuestCredits = EconomyService.AdjustQuestCredits(
                    baseCredits: scenario.recoveryBaseCredits + 75,
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
                    baseCredits: scenario.recoveryLevelCredits + 22,
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

                Assert.Greater(challengeIncome, recoveryIncome, $"Challenge route should exceed recovery route in scenario {i + 1}.");
                Assert.Greater(debt, 0f, $"Debt should be positive in scenario {i + 1}.");
                Assert.GreaterOrEqual(recoveryShopRatio, 0.95f, $"Recovery route should keep at least one purchase window in scenario {i + 1}.");
                Assert.LessOrEqual(recoveryShopRatio, 2.5f, $"Recovery route should not over-inflate economy in scenario {i + 1}.");
                Assert.GreaterOrEqual(debtChallengeRatio, 0.18f, $"Debt/challenge ratio too weak in scenario {i + 1}.");
                Assert.LessOrEqual(debtChallengeRatio, 0.68f, $"Debt/challenge ratio too harsh in scenario {i + 1}.");
                Assert.LessOrEqual(debtShopRatio, 1.7f, $"Debt/shop ratio exceeds recoverable envelope in scenario {i + 1}.");

                if (scenario.chapterId >= 4)
                {
                    lateDebtShopRatioSum += debtShopRatio;
                    lateDebtCount++;
                }
                else
                {
                    midDebtShopRatioSum += debtShopRatio;
                    midDebtCount++;
                }
            }

            float midDebtShopRatio = midDebtCount > 0 ? midDebtShopRatioSum / midDebtCount : 0f;
            float lateDebtShopRatio = lateDebtCount > 0 ? lateDebtShopRatioSum / lateDebtCount : 0f;
            Assert.GreaterOrEqual(lateDebtShopRatio, midDebtShopRatio * 0.95f, "Late debt pressure should not underflow relative to mid baseline.");
            Assert.LessOrEqual(lateDebtShopRatio, midDebtShopRatio * 1.9f, "Late debt pressure should not spike beyond recoverable design band.");
        }

        private static void TriggerFailure(
            QuestSystem questSystem,
            QuestProgress progress,
            FailureMatrixScenario scenario,
            MethodInfo updateQuestTimers,
            MethodInfo handleDefenseTargetDestroyed,
            MethodInfo handlePlayerDeath,
            MethodInfo handleGameOver)
        {
            switch (scenario.failureRuleType)
            {
                case FailureRuleType.QuestTimer:
                    progress.totalElapsedTime = Mathf.Max(progress.totalElapsedTime, progress.data.timeLimit);
                    updateQuestTimers.Invoke(questSystem, null);
                    break;
                case FailureRuleType.StageTimer:
                    progress.stageElapsedTime = Mathf.Max(progress.stageElapsedTime, 12f);
                    updateQuestTimers.Invoke(questSystem, null);
                    break;
                case FailureRuleType.PlayerDeath:
                    handlePlayerDeath.Invoke(questSystem, null);
                    break;
                case FailureRuleType.GameOver:
                    handleGameOver.Invoke(questSystem, new object[] { false });
                    break;
                case FailureRuleType.DefenseTarget:
                    handleDefenseTargetDestroyed.Invoke(questSystem, new object[] { scenario.strongholdId });
                    break;
            }
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

        private static void ApplyFailureRule(QuestData quest, FailureRuleType rule, string strongholdId)
        {
            if (quest == null)
            {
                return;
            }

            quest.timeLimit = 0f;
            quest.failOnPlayerDeath = false;
            quest.failOnGameOver = false;
            quest.failOnDefenseTargetDestroyed = false;
            quest.stages = new List<QuestStage>();

            switch (rule)
            {
                case FailureRuleType.QuestTimer:
                    quest.questType = QuestType.Kill;
                    quest.targetCount = 12;
                    quest.timeLimit = 30f;
                    break;
                case FailureRuleType.StageTimer:
                    quest.questType = QuestType.Kill;
                    quest.targetCount = 12;
                    quest.stages = new List<QuestStage>
                    {
                        new QuestStage
                        {
                            stageId = "r18_stage_timer",
                            description = "Clear this stage before timer expires.",
                            questType = QuestType.Kill,
                            targetCount = 10,
                            useTimeLimit = true,
                            timeLimit = 12f
                        }
                    };
                    break;
                case FailureRuleType.PlayerDeath:
                    quest.questType = QuestType.Kill;
                    quest.targetCount = 12;
                    quest.failOnPlayerDeath = true;
                    break;
                case FailureRuleType.GameOver:
                    quest.questType = QuestType.Kill;
                    quest.targetCount = 12;
                    quest.failOnGameOver = true;
                    break;
                case FailureRuleType.DefenseTarget:
                    quest.questType = QuestType.Protect;
                    quest.targetStrongholdId = strongholdId;
                    quest.failOnDefenseTargetDestroyed = true;
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

            return text.Trim().Length >= 8;
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

        private static List<FailureMatrixScenario> BuildRound18MatrixScenarios()
        {
            var list = new List<FailureMatrixScenario>();
            FailureRuleType[] rules =
            {
                FailureRuleType.PlayerDeath,
                FailureRuleType.GameOver,
                FailureRuleType.QuestTimer,
                FailureRuleType.StageTimer,
                FailureRuleType.DefenseTarget
            };

            for (int chapter = 3; chapter <= 4; chapter++)
            {
                int levelDifficulty = chapter == 3 ? 3 : 4;
                string strongholdId = chapter == 3 ? "SH_MID" : "SH_LATE";
                int chapterOffset = chapter == 3 ? 0 : 24;

                for (int i = 0; i < rules.Length; i++)
                {
                    FailureRuleType rule = rules[i];
                    QuestType recoveryType = ResolveRecoveryType(rule);
                    QuestRewardTier recoveryTier = chapter >= 4 ? QuestRewardTier.Challenge : QuestRewardTier.Side;

                    list.Add(new FailureMatrixScenario
                    {
                        chapterId = chapter,
                        levelDifficulty = levelDifficulty,
                        strongholdId = strongholdId,
                        failureRuleType = rule,
                        recoveryQuestType = recoveryType,
                        recoveryRewardTier = recoveryTier,
                        recoveryBaseExp = 165 + chapterOffset + (i * 10),
                        recoveryBaseCredits = 145 + chapterOffset + (i * 9),
                        recoveryLevelCredits = 135 + chapterOffset + (i * 7),
                        shopBasePrice = 300 + (chapter - 3) * 95 + (i * 16)
                    });
                }
            }

            return list;
        }

        private static QuestType ResolveRecoveryType(FailureRuleType rule)
        {
            switch (rule)
            {
                case FailureRuleType.DefenseTarget:
                    return QuestType.Protect;
                case FailureRuleType.QuestTimer:
                case FailureRuleType.StageTimer:
                    return QuestType.Kill;
                default:
                    return QuestType.CompleteStronghold;
            }
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

        private enum FailureRuleType
        {
            PlayerDeath,
            GameOver,
            QuestTimer,
            StageTimer,
            DefenseTarget
        }

        private struct FailureMatrixScenario
        {
            public int chapterId;
            public int levelDifficulty;
            public string strongholdId;
            public FailureRuleType failureRuleType;
            public QuestType recoveryQuestType;
            public QuestRewardTier recoveryRewardTier;
            public int recoveryBaseExp;
            public int recoveryBaseCredits;
            public int recoveryLevelCredits;
            public int shopBasePrice;
        }
    }
}
