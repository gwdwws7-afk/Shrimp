using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ThirdPersonController.Tests
{
    public class QuestFailureEconomyRound21RegressionTests
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
        public void QuestSystem_Round21_CrossChapterFailureStreak_CompensationPacing_OnlyRecoveryPays()
        {
            EconomyConfig config = BuildP1SimulationConfig();
            EconomyService.Configure(config);

            QuestSystem questSystem = CreateQuestRuntime(
                "QuestFailureEconomy_Round21_CrossChapter",
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

            List<CrossChapterScenario> scenarios = BuildRound21CrossChapterScenarios();
            Assert.AreEqual(5, scenarios.Count, "Round21 cross-chapter matrix should cover 5 failure types.");

            var failedCounts = new Dictionary<FailureRuleType, int>();
            for (int i = 0; i < 5; i++)
            {
                failedCounts[(FailureRuleType)i] = 0;
            }

            int expectedExpTotal = 0;
            int expectedCreditsTotal = 0;
            int expectedFailureAttempts = 0;
            float chapter5RecoveryRatioSum = 0f;
            int chapter5RecoveryRatioCount = 0;

            for (int i = 0; i < scenarios.Count; i++)
            {
                CrossChapterScenario scenario = scenarios[i];

                questSystem.levelDifficulty = scenario.failureLevelDifficulty;
                questSystem.levelChapterId = scenario.failureChapterId;

                int expBeforeFail = experienceSystem.currentExp;
                int creditsBeforeFail = wallet.Credits;

                for (int failIndex = 0; failIndex < scenario.failureStreak; failIndex++)
                {
                    QuestData failureQuest = NewQuestData(
                        questId: $"r21_fail_{scenario.failureRuleType}_{i}_{failIndex}",
                        questType: scenario.recoveryQuestType,
                        rewardTier: QuestRewardTier.Mainline,
                        difficultyRating: scenario.failureLevelDifficulty,
                        targetStrongholdId: scenario.failureStrongholdId,
                        expReward: 145,
                        creditReward: 120,
                        description: $"Cross-chapter failure sample {i + 1}-{failIndex + 1}: fail safely and learn recovery pacing.");
                    ApplyFailureRule(failureQuest, scenario.failureRuleType, scenario.failureStrongholdId);

                    Assert.IsTrue(HasFailureRule(failureQuest), $"Failure scenario must have an explicit failure rule: {failureQuest.questId}");
                    Assert.IsTrue(IsMeaningfulLearningText(failureQuest.description), $"Failure scenario needs usable learning text: {failureQuest.questId}");

                    questSystem.StartQuest(failureQuest);
                    QuestProgress failureProgress = FindQuestProgress(questSystem, failureQuest.questId, QuestStatus.InProgress);
                    Assert.NotNull(failureProgress, $"Failure quest should be active: {failureQuest.questId}");

                    TriggerFailure(
                        questSystem,
                        failureProgress,
                        scenario.failureRuleType,
                        scenario.failureStrongholdId,
                        updateQuestTimers,
                        handleDefenseTargetDestroyed,
                        handlePlayerDeath,
                        handleGameOver);

                    Assert.AreEqual(QuestStatus.Failed, failureProgress.status, $"Failure rule should transition to Failed: {failureQuest.questId}");
                    failedCounts[scenario.failureRuleType]++;
                    expectedFailureAttempts++;

                    completeQuest.Invoke(questSystem, new object[] { failureProgress });
                    Assert.AreEqual(expBeforeFail, experienceSystem.currentExp, $"Failed quest must not grant EXP: {failureQuest.questId}");
                    Assert.AreEqual(creditsBeforeFail, wallet.Credits, $"Failed quest must not grant credits: {failureQuest.questId}");
                }

                questSystem.levelDifficulty = scenario.recoveryLevelDifficulty;
                questSystem.levelChapterId = scenario.recoveryChapterId;

                QuestData compensationQuest = NewQuestData(
                    questId: $"r21_compensate_{scenario.failureRuleType}_{i}",
                    questType: scenario.recoveryQuestType,
                    rewardTier: scenario.compensationTier,
                    difficultyRating: scenario.recoveryLevelDifficulty,
                    targetStrongholdId: scenario.recoveryStrongholdId,
                    expReward: scenario.compensationBaseExp,
                    creditReward: scenario.compensationBaseCredits,
                    description: $"Compensation step {i + 1}: re-open the economy window after streak failures.");

                questSystem.StartQuest(compensationQuest);
                QuestProgress compensationProgress = FindQuestProgress(questSystem, compensationQuest.questId, QuestStatus.InProgress);
                Assert.NotNull(compensationProgress, $"Compensation quest should be active: {compensationQuest.questId}");

                int compensationExp = ExpectedQuestExp(questSystem, compensationQuest, scenario.recoveryStrongholdId);
                int compensationCredits = ExpectedQuestCredits(questSystem, compensationQuest, scenario.recoveryStrongholdId);

                completeQuest.Invoke(questSystem, new object[] { compensationProgress });
                Assert.AreEqual(QuestStatus.Completed, compensationProgress.status, $"Compensation quest should complete: {compensationQuest.questId}");

                QuestData stabilizationQuest = NewQuestData(
                    questId: $"r21_stabilize_{scenario.failureRuleType}_{i}",
                    questType: QuestType.BossBreak,
                    rewardTier: scenario.stabilizationTier,
                    difficultyRating: scenario.recoveryLevelDifficulty,
                    targetStrongholdId: scenario.recoveryStrongholdId,
                    expReward: scenario.stabilizationBaseExp,
                    creditReward: scenario.stabilizationBaseCredits,
                    description: $"Stabilization step {i + 1}: convert compensation into late-game tempo recovery.");

                questSystem.StartQuest(stabilizationQuest);
                QuestProgress stabilizationProgress = FindQuestProgress(questSystem, stabilizationQuest.questId, QuestStatus.InProgress);
                Assert.NotNull(stabilizationProgress, $"Stabilization quest should be active: {stabilizationQuest.questId}");

                int stabilizationExp = ExpectedQuestExp(questSystem, stabilizationQuest, scenario.recoveryStrongholdId);
                int stabilizationCredits = ExpectedQuestCredits(questSystem, stabilizationQuest, scenario.recoveryStrongholdId);

                completeQuest.Invoke(questSystem, new object[] { stabilizationProgress });
                Assert.AreEqual(QuestStatus.Completed, stabilizationProgress.status, $"Stabilization quest should complete: {stabilizationQuest.questId}");

                expectedExpTotal += compensationExp + stabilizationExp;
                expectedCreditsTotal += compensationCredits + stabilizationCredits;

                int levelCreditProbe = EconomyService.AdjustLevelCredits(
                    baseCredits: scenario.recoveryLevelCredits,
                    difficulty: scenario.recoveryLevelDifficulty,
                    levelRewardMultiplier: questSystem.levelRewardMultiplier);
                int shopPrice = EconomyService.AdjustShopPrice(
                    basePrice: scenario.shopBasePrice,
                    difficulty: scenario.recoveryLevelDifficulty,
                    priceMultiplier: 1f);

                float totalRecoveryRatio = shopPrice > 0
                    ? (compensationCredits + stabilizationCredits + levelCreditProbe) / (float)shopPrice
                    : 0f;

                Assert.Greater(compensationCredits, 0, $"Compensation credits should be positive in scenario {i + 1}.");
                Assert.Greater(stabilizationCredits, compensationCredits * 0.9f, $"Stabilization credits should not fall behind compensation in scenario {i + 1}.");
                Assert.GreaterOrEqual(totalRecoveryRatio, 1.05f, $"Cross-chapter recovery ratio too tight in scenario {i + 1}.");
                Assert.LessOrEqual(totalRecoveryRatio, 4.8f, $"Cross-chapter recovery ratio too loose in scenario {i + 1}.");

                int expGainAfterRecovery = experienceSystem.currentExp - expBeforeFail;
                int creditsGainAfterRecovery = wallet.Credits - creditsBeforeFail;
                Assert.AreEqual(compensationExp + stabilizationExp, expGainAfterRecovery, $"Only compensation+stabilization EXP should be granted in scenario {i + 1}.");
                Assert.AreEqual(compensationCredits + stabilizationCredits, creditsGainAfterRecovery, $"Only compensation+stabilization credits should be granted in scenario {i + 1}.");

                chapter5RecoveryRatioSum += totalRecoveryRatio;
                chapter5RecoveryRatioCount++;
            }

            foreach (KeyValuePair<FailureRuleType, int> pair in failedCounts)
            {
                Assert.GreaterOrEqual(pair.Value, 2, $"Each failure type should be covered by at least two streak attempts: {pair.Key}");
            }

            Assert.AreEqual(expectedExpTotal, experienceSystem.currentExp, "Round21 EXP should equal cross-chapter compensation+stabilization payouts only.");
            Assert.AreEqual(expectedCreditsTotal, wallet.Credits, "Round21 credits should equal cross-chapter compensation+stabilization payouts only.");
            Assert.GreaterOrEqual(expectedFailureAttempts, 12, "Round21 should include enough failure attempts to stress cross-chapter recovery pacing.");

            float chapter5RecoveryRatio = chapter5RecoveryRatioCount > 0 ? chapter5RecoveryRatioSum / chapter5RecoveryRatioCount : 0f;
            Assert.GreaterOrEqual(chapter5RecoveryRatio, 1.15f, "Average chapter5 recovery ratio should stay above minimum playable target.");
            Assert.LessOrEqual(chapter5RecoveryRatio, 4.2f, "Average chapter5 recovery ratio should stay below over-reward target.");
        }

        [Test]
        public void EconomyService_Round21_CrossChapterCompensationPacing_DebtWindowRemainsRecoverable()
        {
            EconomyConfig config = BuildP1SimulationConfig();
            EconomyService.Configure(config);

            List<CrossChapterScenario> scenarios = BuildRound21CrossChapterScenarios();
            Assert.AreEqual(5, scenarios.Count, "Round21 cross-chapter matrix should cover 5 failure types.");

            float coverageSum = 0f;
            int coverageCount = 0;

            for (int i = 0; i < scenarios.Count; i++)
            {
                CrossChapterScenario scenario = scenarios[i];

                int compensationCredits = EconomyService.AdjustQuestCredits(
                    baseCredits: scenario.compensationBaseCredits,
                    questType: scenario.recoveryQuestType,
                    questDifficulty: scenario.recoveryLevelDifficulty,
                    levelDifficulty: scenario.recoveryLevelDifficulty,
                    rewardMultiplier: 1.08f,
                    rewardTier: scenario.compensationTier,
                    chapterId: scenario.recoveryChapterId,
                    strongholdId: scenario.recoveryStrongholdId);

                int stabilizationCredits = EconomyService.AdjustQuestCredits(
                    baseCredits: scenario.stabilizationBaseCredits,
                    questType: QuestType.BossBreak,
                    questDifficulty: scenario.recoveryLevelDifficulty,
                    levelDifficulty: scenario.recoveryLevelDifficulty,
                    rewardMultiplier: 1.15f,
                    rewardTier: scenario.stabilizationTier,
                    chapterId: scenario.recoveryChapterId,
                    strongholdId: scenario.recoveryStrongholdId);

                int failureChallengeCredits = EconomyService.AdjustQuestCredits(
                    baseCredits: scenario.stabilizationBaseCredits + 45,
                    questType: QuestType.BossDefeat,
                    questDifficulty: scenario.failureLevelDifficulty,
                    levelDifficulty: scenario.failureLevelDifficulty,
                    rewardMultiplier: 1.2f,
                    rewardTier: QuestRewardTier.Challenge,
                    chapterId: scenario.failureChapterId,
                    strongholdId: scenario.failureStrongholdId);

                float streakDebt = failureChallengeCredits * scenario.failureStreak;
                float recoveryTotal = compensationCredits + stabilizationCredits;
                float recoveryCoverage = streakDebt > 0f ? recoveryTotal / streakDebt : 0f;

                int shopPrice = EconomyService.AdjustShopPrice(
                    basePrice: scenario.shopBasePrice,
                    difficulty: scenario.recoveryLevelDifficulty,
                    priceMultiplier: 1f);
                float recoveryShopRatio = shopPrice > 0 ? recoveryTotal / shopPrice : 0f;

                int requiredCompensationOnlyRuns = Mathf.CeilToInt(streakDebt / Mathf.Max(1f, compensationCredits));
                int requiredTwoPhaseRuns = Mathf.CeilToInt(streakDebt / Mathf.Max(1f, recoveryTotal));

                Assert.Greater(compensationCredits, 0, $"Compensation credits should be positive in scenario {i + 1}.");
                Assert.Greater(stabilizationCredits, 0, $"Stabilization credits should be positive in scenario {i + 1}.");
                Assert.Greater(streakDebt, compensationCredits, $"Streak debt should remain larger than single compensation step in scenario {i + 1}.");

                Assert.GreaterOrEqual(recoveryCoverage, 0.45f, $"Recovery coverage too weak in scenario {i + 1}.");
                Assert.LessOrEqual(recoveryCoverage, 1.8f, $"Recovery coverage too strong in scenario {i + 1}.");
                Assert.GreaterOrEqual(recoveryShopRatio, 0.95f, $"Recovery/shop ratio too tight in scenario {i + 1}.");
                Assert.LessOrEqual(recoveryShopRatio, 3.9f, $"Recovery/shop ratio too loose in scenario {i + 1}.");

                Assert.GreaterOrEqual(requiredCompensationOnlyRuns, 2, $"Compensation-only runs should stay meaningful in scenario {i + 1}.");
                Assert.LessOrEqual(requiredCompensationOnlyRuns, 6, $"Compensation-only runs should stay recoverable in scenario {i + 1}.");
                Assert.GreaterOrEqual(requiredTwoPhaseRuns, 1, $"Two-phase runs should be at least one in scenario {i + 1}.");
                Assert.LessOrEqual(requiredTwoPhaseRuns, 3, $"Two-phase runs should remain within release pacing in scenario {i + 1}.");

                coverageSum += recoveryCoverage;
                coverageCount++;
            }

            float avgCoverage = coverageCount > 0 ? coverageSum / coverageCount : 0f;
            Assert.GreaterOrEqual(avgCoverage, 0.6f, "Average cross-chapter coverage should not collapse below target.");
            Assert.LessOrEqual(avgCoverage, 1.35f, "Average cross-chapter coverage should not exceed target.");
        }

        private static void TriggerFailure(
            QuestSystem questSystem,
            QuestProgress progress,
            FailureRuleType failureRuleType,
            string strongholdId,
            MethodInfo updateQuestTimers,
            MethodInfo handleDefenseTargetDestroyed,
            MethodInfo handlePlayerDeath,
            MethodInfo handleGameOver)
        {
            switch (failureRuleType)
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
                    handleDefenseTargetDestroyed.Invoke(questSystem, new object[] { strongholdId });
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
            questSystem.levelDifficulty = 4;
            questSystem.levelChapterId = 4;
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
                            stageId = "r21_stage_timer",
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

        private static List<CrossChapterScenario> BuildRound21CrossChapterScenarios()
        {
            var list = new List<CrossChapterScenario>();
            FailureRuleType[] rules =
            {
                FailureRuleType.PlayerDeath,
                FailureRuleType.GameOver,
                FailureRuleType.QuestTimer,
                FailureRuleType.StageTimer,
                FailureRuleType.DefenseTarget
            };

            for (int i = 0; i < rules.Length; i++)
            {
                FailureRuleType rule = rules[i];
                QuestType recoveryType = ResolveRecoveryType(rule);
                int failureStreak = (i % 2 == 0) ? 2 : 3;

                list.Add(new CrossChapterScenario
                {
                    failureRuleType = rule,
                    failureStreak = failureStreak,
                    failureChapterId = 4,
                    failureLevelDifficulty = 4,
                    failureStrongholdId = "SH_LATE",
                    recoveryChapterId = 5,
                    recoveryLevelDifficulty = 5,
                    recoveryStrongholdId = "SH_END",
                    recoveryQuestType = recoveryType,
                    compensationTier = QuestRewardTier.Side,
                    stabilizationTier = QuestRewardTier.Challenge,
                    compensationBaseExp = 205 + (i * 12),
                    compensationBaseCredits = 188 + (i * 12),
                    stabilizationBaseExp = 248 + (i * 14),
                    stabilizationBaseCredits = 230 + (i * 14),
                    recoveryLevelCredits = 175 + (i * 10),
                    shopBasePrice = 430 + (i * 20)
                });
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
                },
                new QuestChapterRewardMultiplier
                {
                    chapterId = 5,
                    expMultiplier = 1.3f,
                    pearlMultiplier = 1f,
                    creditMultiplier = 1.34f
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
                },
                new QuestStrongholdRewardMultiplier
                {
                    strongholdId = "SH_END",
                    expMultiplier = 1.18f,
                    pearlMultiplier = 1f,
                    creditMultiplier = 1.22f
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

        private struct CrossChapterScenario
        {
            public FailureRuleType failureRuleType;
            public int failureStreak;
            public int failureChapterId;
            public int failureLevelDifficulty;
            public string failureStrongholdId;
            public int recoveryChapterId;
            public int recoveryLevelDifficulty;
            public string recoveryStrongholdId;
            public QuestType recoveryQuestType;
            public QuestRewardTier compensationTier;
            public QuestRewardTier stabilizationTier;
            public int compensationBaseExp;
            public int compensationBaseCredits;
            public int stabilizationBaseExp;
            public int stabilizationBaseCredits;
            public int recoveryLevelCredits;
            public int shopBasePrice;
        }
    }
}
