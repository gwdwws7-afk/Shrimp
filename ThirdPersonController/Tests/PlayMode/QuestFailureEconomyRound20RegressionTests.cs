using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ThirdPersonController.Tests
{
    public class QuestFailureEconomyRound20RegressionTests
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
        public void QuestSystem_Round20_ConsecutiveFailures_ByRuleAndChapter_OnlyRecoveryPays_AndWindowStaysPlayable()
        {
            EconomyConfig config = BuildP1SimulationConfig();
            EconomyService.Configure(config);

            QuestSystem questSystem = CreateQuestRuntime(
                "QuestFailureEconomy_Round20_StreakMatrix",
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

            List<FailureMatrixScenario> scenarios = BuildRound20StreakScenarios();
            Assert.AreEqual(10, scenarios.Count, "Round20 streak matrix should cover 2 chapters x 5 failure types.");

            var failedCounts = new Dictionary<FailureRuleType, int>();
            for (int i = 0; i < 5; i++)
            {
                failedCounts[(FailureRuleType)i] = 0;
            }

            int expectedExpTotal = 0;
            int expectedCreditsTotal = 0;
            int expectedFailureAttempts = 0;
            float chapter4AdjustedRatioSum = 0f;
            int chapter4AdjustedRatioCount = 0;
            float chapter5AdjustedRatioSum = 0f;
            int chapter5AdjustedRatioCount = 0;

            for (int i = 0; i < scenarios.Count; i++)
            {
                FailureMatrixScenario scenario = scenarios[i];
                questSystem.levelDifficulty = scenario.levelDifficulty;
                questSystem.levelChapterId = scenario.chapterId;

                int expBeforeFail = experienceSystem.currentExp;
                int creditsBeforeFail = wallet.Credits;

                for (int failIndex = 0; failIndex < scenario.failureStreak; failIndex++)
                {
                    QuestData failureQuest = NewQuestData(
                        questId: $"r20_fail_{scenario.chapterId}_{scenario.failureRuleType}_{i}_{failIndex}",
                        questType: scenario.recoveryQuestType,
                        rewardTier: QuestRewardTier.Mainline,
                        difficultyRating: scenario.levelDifficulty,
                        targetStrongholdId: scenario.strongholdId,
                        expReward: 140,
                        creditReward: 110,
                        description: $"Failure streak sample {i + 1}-{failIndex + 1}: identify fail cause and pivot to recovery plan.");
                    ApplyFailureRule(failureQuest, scenario.failureRuleType, scenario.strongholdId);
                    Assert.IsTrue(HasFailureRule(failureQuest), $"Failure scenario must have an explicit failure rule: {failureQuest.questId}");
                    Assert.IsTrue(IsMeaningfulLearningText(failureQuest.description), $"Failure scenario needs usable learning text: {failureQuest.questId}");

                    questSystem.StartQuest(failureQuest);
                    QuestProgress failureProgress = FindQuestProgress(questSystem, failureQuest.questId, QuestStatus.InProgress);
                    Assert.NotNull(failureProgress, $"Failure quest should be active: {failureQuest.questId}");

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
                    expectedFailureAttempts++;

                    completeQuest.Invoke(questSystem, new object[] { failureProgress });
                    Assert.AreEqual(expBeforeFail, experienceSystem.currentExp, $"Failed quest must not grant EXP: {failureQuest.questId}");
                    Assert.AreEqual(creditsBeforeFail, wallet.Credits, $"Failed quest must not grant credits: {failureQuest.questId}");
                }

                QuestData recoveryQuest = NewQuestData(
                    questId: $"r20_recover_{scenario.chapterId}_{scenario.failureRuleType}_{i}",
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
                float streakAdjustedRatio = ratio / Mathf.Max(1, scenario.failureStreak);
                float minAdjustedRatio = scenario.failureStreak >= 3 ? 0.3f : 0.42f;
                Assert.GreaterOrEqual(ratio, 0.9f, $"Recovery window too tight at scenario {i + 1}.");
                Assert.LessOrEqual(ratio, 2.7f, $"Recovery window too loose at scenario {i + 1}.");
                Assert.GreaterOrEqual(streakAdjustedRatio, minAdjustedRatio, $"Streak-adjusted recovery ratio too tight at scenario {i + 1}.");
                Assert.LessOrEqual(streakAdjustedRatio, 1.35f, $"Streak-adjusted recovery ratio too loose at scenario {i + 1}.");

                if (scenario.chapterId == 5)
                {
                    chapter5AdjustedRatioSum += streakAdjustedRatio;
                    chapter5AdjustedRatioCount++;
                }
                else
                {
                    chapter4AdjustedRatioSum += streakAdjustedRatio;
                    chapter4AdjustedRatioCount++;
                }
            }

            foreach (KeyValuePair<FailureRuleType, int> pair in failedCounts)
            {
                Assert.GreaterOrEqual(pair.Value, 4, $"Failure type coverage should include both chapters under streak pressure: {pair.Key}");
            }

            Assert.AreEqual(expectedExpTotal, experienceSystem.currentExp, "Round20 matrix EXP should equal successful recovery payouts only.");
            Assert.AreEqual(expectedCreditsTotal, wallet.Credits, "Round20 matrix credits should equal successful recovery payouts only.");
            Assert.GreaterOrEqual(expectedFailureAttempts, scenarios.Count * 2, "Round20 should include at least two failed attempts per scenario.");

            float chapter4AdjustedRatio = chapter4AdjustedRatioCount > 0 ? chapter4AdjustedRatioSum / chapter4AdjustedRatioCount : 0f;
            float chapter5AdjustedRatio = chapter5AdjustedRatioCount > 0 ? chapter5AdjustedRatioSum / chapter5AdjustedRatioCount : 0f;
            Assert.GreaterOrEqual(chapter5AdjustedRatio, chapter4AdjustedRatio * 0.85f, "Chapter5 adjusted recovery ratio should not collapse below chapter4 baseline.");
            Assert.LessOrEqual(chapter5AdjustedRatio, chapter4AdjustedRatio * 1.25f, "Chapter5 adjusted recovery ratio should remain in controlled band.");
        }

        [Test]
        public void EconomyService_Round20_ConsecutiveFailureDebtCurve_RemainsRecoverableWithinRunsBudget()
        {
            EconomyConfig config = BuildP1SimulationConfig();
            EconomyService.Configure(config);

            List<FailureMatrixScenario> scenarios = BuildRound20StreakScenarios();
            Assert.AreEqual(10, scenarios.Count, "Round20 streak matrix should cover 2 chapters x 5 failure types.");

            float chapter4RunsSum = 0f;
            int chapter4RunsCount = 0;
            float chapter5RunsSum = 0f;
            int chapter5RunsCount = 0;

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
                    baseCredits: scenario.recoveryBaseCredits + 85,
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
                    baseCredits: scenario.recoveryLevelCredits + 25,
                    difficulty: scenario.levelDifficulty,
                    levelRewardMultiplier: 1.15f);

                float recoveryIncome = recoveryQuestCredits + recoveryLevelCredits;
                float challengeIncome = challengeQuestCredits + challengeLevelCredits;
                float debt = challengeIncome - recoveryIncome;
                float streakDebt = debt * scenario.failureStreak;

                int shopPrice = EconomyService.AdjustShopPrice(
                    basePrice: scenario.shopBasePrice,
                    difficulty: scenario.levelDifficulty,
                    priceMultiplier: 1f);

                float recoveryShopRatio = shopPrice > 0 ? recoveryIncome / shopPrice : 0f;
                float streakDebtChallengeRatio = challengeIncome > 0f ? streakDebt / challengeIncome : 0f;
                float streakDebtShopRatio = shopPrice > 0 ? streakDebt / shopPrice : 0f;
                int requiredRecoveryRuns = Mathf.CeilToInt(streakDebt / Mathf.Max(1f, recoveryIncome));

                Assert.Greater(challengeIncome, recoveryIncome, $"Challenge route should exceed recovery route in scenario {i + 1}.");
                Assert.Greater(debt, 0f, $"Debt should be positive in scenario {i + 1}.");
                Assert.GreaterOrEqual(recoveryShopRatio, 0.95f, $"Recovery route should keep at least one purchase window in scenario {i + 1}.");
                Assert.LessOrEqual(recoveryShopRatio, 2.5f, $"Recovery route should not over-inflate economy in scenario {i + 1}.");
                Assert.GreaterOrEqual(streakDebtChallengeRatio, 0.35f, $"Streak debt/challenge ratio too weak in scenario {i + 1}.");
                Assert.LessOrEqual(streakDebtChallengeRatio, 1.25f, $"Streak debt/challenge ratio too harsh in scenario {i + 1}.");
                Assert.LessOrEqual(streakDebtShopRatio, 4.8f, $"Streak debt/shop ratio exceeds recoverable envelope in scenario {i + 1}.");
                Assert.GreaterOrEqual(requiredRecoveryRuns, 1, $"Recovery runs should be at least one in scenario {i + 1}.");
                Assert.LessOrEqual(requiredRecoveryRuns, 3, $"Recovery runs should stay within three-run budget in scenario {i + 1}.");

                if (scenario.chapterId == 5)
                {
                    chapter5RunsSum += requiredRecoveryRuns;
                    chapter5RunsCount++;
                }
                else
                {
                    chapter4RunsSum += requiredRecoveryRuns;
                    chapter4RunsCount++;
                }
            }

            float chapter4Runs = chapter4RunsCount > 0 ? chapter4RunsSum / chapter4RunsCount : 0f;
            float chapter5Runs = chapter5RunsCount > 0 ? chapter5RunsSum / chapter5RunsCount : 0f;
            Assert.GreaterOrEqual(chapter5Runs, chapter4Runs * 0.85f, "Chapter5 runs budget should not underflow chapter4 baseline.");
            Assert.LessOrEqual(chapter5Runs, chapter4Runs * 1.5f, "Chapter5 runs budget should stay in controlled escalation band.");
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
                            stageId = "r20_stage_timer",
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

        private static List<FailureMatrixScenario> BuildRound20StreakScenarios()
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

            for (int chapter = 4; chapter <= 5; chapter++)
            {
                int levelDifficulty = chapter == 4 ? 4 : 5;
                string strongholdId = chapter == 4 ? "SH_LATE" : "SH_END";
                int chapterOffset = chapter == 4 ? 28 : 58;

                for (int i = 0; i < rules.Length; i++)
                {
                    FailureRuleType rule = rules[i];
                    QuestType recoveryType = ResolveRecoveryType(rule);
                    QuestRewardTier recoveryTier = QuestRewardTier.Challenge;
                    int failureStreak = (i % 2 == 0) ? 2 : 3;

                    list.Add(new FailureMatrixScenario
                    {
                        chapterId = chapter,
                        levelDifficulty = levelDifficulty,
                        strongholdId = strongholdId,
                        failureRuleType = rule,
                        recoveryQuestType = recoveryType,
                        recoveryRewardTier = recoveryTier,
                        recoveryBaseExp = 188 + chapterOffset + (i * 11),
                        recoveryBaseCredits = 165 + chapterOffset + (i * 10),
                        recoveryLevelCredits = 152 + chapterOffset + (i * 8),
                        shopBasePrice = 380 + (chapter - 4) * 110 + (i * 18),
                        failureStreak = failureStreak
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
            public int failureStreak;
        }
    }
}


