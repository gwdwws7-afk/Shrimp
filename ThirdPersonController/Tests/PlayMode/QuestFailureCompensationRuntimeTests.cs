using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ThirdPersonController.Tests
{
    public class QuestFailureCompensationRuntimeTests
    {
        private readonly List<Object> createdObjects = new List<Object>();
        private EconomyConfig oldEconomyConfig;

        [SetUp]
        public void SetUp()
        {
            oldEconomyConfig = EconomyService.Config;
            EconomyService.Configure(null);

            SaveManager save = SaveManager.Instance;
            if (save != null && save.CurrentData != null)
            {
                save.CurrentData.questStates = save.CurrentData.questStates ?? new List<QuestStateData>();
                save.CurrentData.questStates.Clear();
                save.CurrentData.questFailureStreak = 0;
                save.CurrentData.questFailureDebtExp = 0f;
                save.CurrentData.questFailureDebtCredits = 0f;
                save.CurrentData.questFailureLastChapterId = 0;
                save.CurrentData.questFailureLastStrongholdId = string.Empty;
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
        public void QuestSystem_FailureCompensation_OnlyTaggedRecoveryQuestGetsBonus()
        {
            QuestSystem questSystem = CreateQuestRuntime(
                rootName: "QuestFailureCompensationRuntime_SelectiveBonus",
                out CurrencyWallet wallet,
                out PlayerExperienceSystem experienceSystem,
                saveQuestRuntimeState: false);

            MethodInfo completeQuest = typeof(QuestSystem).GetMethod("CompleteQuest", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(completeQuest, "QuestSystem.CompleteQuest should exist.");

            QuestData failedQuest = BuildQuest(
                questId: "runtime_fail_01",
                expReward: 140,
                creditReward: 120,
                allowCompensation: false);

            questSystem.StartQuest(failedQuest);
            QuestProgress failedProgress = FindQuestProgress(questSystem, failedQuest.questId, QuestStatus.InProgress);
            Assert.NotNull(failedProgress, "Failure quest should start.");

            questSystem.FailQuest(failedProgress, "runtime failure sample");
            Assert.AreEqual(QuestStatus.Failed, failedProgress.status, "Failure quest should become Failed.");

            QuestData regularQuest = BuildQuest(
                questId: "runtime_regular_01",
                expReward: 90,
                creditReward: 80,
                allowCompensation: false);

            int regularBaseExp = ExpectedQuestExp(questSystem, regularQuest);
            int regularBaseCredits = ExpectedQuestCredits(questSystem, regularQuest);

            questSystem.StartQuest(regularQuest);
            QuestProgress regularProgress = FindQuestProgress(questSystem, regularQuest.questId, QuestStatus.InProgress);
            Assert.NotNull(regularProgress, "Regular quest should start.");

            completeQuest.Invoke(questSystem, new object[] { regularProgress });
            Assert.AreEqual(regularBaseExp, experienceSystem.currentExp, "Unmarked recovery quest should grant only base EXP.");
            Assert.AreEqual(regularBaseCredits, wallet.Credits, "Unmarked recovery quest should grant only base credits.");

            QuestData recoveryQuest = BuildQuest(
                questId: "runtime_recovery_01",
                expReward: 90,
                creditReward: 80,
                allowCompensation: true,
                minFailureStreak: 1,
                bonusPerFailure: 0.5f,
                bonusCap: 0.5f,
                debtPayoutCap: 1f,
                chapterWindow: 2,
                streakDecayOnComplete: 1);

            int recoveryBaseExp = ExpectedQuestExp(questSystem, recoveryQuest);
            int recoveryBaseCredits = ExpectedQuestCredits(questSystem, recoveryQuest);
            int failedQuestDebtExp = ExpectedQuestExp(questSystem, failedQuest);
            int failedQuestDebtCredits = ExpectedQuestCredits(questSystem, failedQuest);

            int expectedBonusExp = Mathf.Min(Mathf.RoundToInt(recoveryBaseExp * 0.5f), failedQuestDebtExp);
            int expectedBonusCredits = Mathf.Min(Mathf.RoundToInt(recoveryBaseCredits * 0.5f), failedQuestDebtCredits);

            questSystem.StartQuest(recoveryQuest);
            QuestProgress recoveryProgress = FindQuestProgress(questSystem, recoveryQuest.questId, QuestStatus.InProgress);
            Assert.NotNull(recoveryProgress, "Recovery quest should start.");

            completeQuest.Invoke(questSystem, new object[] { recoveryProgress });

            int expectedTotalExp = regularBaseExp + recoveryBaseExp + expectedBonusExp;
            int expectedTotalCredits = regularBaseCredits + recoveryBaseCredits + expectedBonusCredits;
            Assert.AreEqual(expectedTotalExp, experienceSystem.currentExp, "Recovery quest should include compensation EXP bonus.");
            Assert.AreEqual(expectedTotalCredits, wallet.Credits, "Recovery quest should include compensation credit bonus.");
        }

        [Test]
        public void QuestSystem_FailureCompensation_StateRestoresFromSaveData()
        {
            SaveManager save = SaveManager.Instance;
            Assert.NotNull(save, "SaveManager singleton should exist.");

            QuestSystem sourceQuestSystem = CreateQuestRuntime(
                rootName: "QuestFailureCompensationRuntime_SaveSource",
                out CurrencyWallet sourceWallet,
                out PlayerExperienceSystem sourceExperience,
                saveQuestRuntimeState: true);

            MethodInfo completeQuest = typeof(QuestSystem).GetMethod("CompleteQuest", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(completeQuest, "QuestSystem.CompleteQuest should exist.");

            QuestData failedQuest = BuildQuest(
                questId: "runtime_fail_save_01",
                expReward: 160,
                creditReward: 150,
                allowCompensation: false);

            sourceQuestSystem.StartQuest(failedQuest);
            QuestProgress failedProgress = FindQuestProgress(sourceQuestSystem, failedQuest.questId, QuestStatus.InProgress);
            Assert.NotNull(failedProgress, "Failure quest should start before save snapshot.");

            sourceQuestSystem.FailQuest(failedProgress, "snapshot failure");
            sourceQuestSystem.SaveQuestRuntimeStateToData();

            Assert.Greater(save.CurrentData.questFailureStreak, 0, "Failure streak should be stored in save data.");
            Assert.Greater(save.CurrentData.questFailureDebtExp, 0f, "Failure EXP debt should be stored in save data.");
            Assert.Greater(save.CurrentData.questFailureDebtCredits, 0f, "Failure credit debt should be stored in save data.");

            QuestSystem restoredQuestSystem = CreateQuestRuntime(
                rootName: "QuestFailureCompensationRuntime_SaveRestore",
                out CurrencyWallet restoredWallet,
                out PlayerExperienceSystem restoredExperience,
                saveQuestRuntimeState: true);

            bool restored = restoredQuestSystem.RestoreQuestRuntimeStateFromSave(notifyListeners: false, addMissingQuests: false);
            Assert.IsTrue(restored, "Restore should succeed when save data carries failure compensation state.");

            int restoredStreak = ReadPrivateInt(restoredQuestSystem, "consecutiveFailureCount");
            float restoredDebtExp = ReadPrivateFloat(restoredQuestSystem, "pendingFailureDebtExp");
            float restoredDebtCredits = ReadPrivateFloat(restoredQuestSystem, "pendingFailureDebtCredits");
            Assert.Greater(restoredStreak, 0, "Restored quest system should recover failure streak.");
            Assert.Greater(restoredDebtExp, 0f, "Restored quest system should recover EXP debt.");
            Assert.Greater(restoredDebtCredits, 0f, "Restored quest system should recover credit debt.");

            QuestData recoveryQuest = BuildQuest(
                questId: "runtime_recovery_save_01",
                expReward: 100,
                creditReward: 95,
                allowCompensation: true,
                minFailureStreak: 1,
                bonusPerFailure: 0.35f,
                bonusCap: 0.35f,
                debtPayoutCap: 0.8f,
                chapterWindow: 2,
                streakDecayOnComplete: 1);

            int baseExp = ExpectedQuestExp(restoredQuestSystem, recoveryQuest);
            int baseCredits = ExpectedQuestCredits(restoredQuestSystem, recoveryQuest);

            restoredQuestSystem.StartQuest(recoveryQuest);
            QuestProgress recoveryProgress = FindQuestProgress(restoredQuestSystem, recoveryQuest.questId, QuestStatus.InProgress);
            Assert.NotNull(recoveryProgress, "Recovery quest should start after restore.");

            completeQuest.Invoke(restoredQuestSystem, new object[] { recoveryProgress });

            Assert.Greater(restoredExperience.currentExp, baseExp, "Restored runtime should pay compensation EXP bonus.");
            Assert.Greater(restoredWallet.Credits, baseCredits, "Restored runtime should pay compensation credit bonus.");
            Assert.LessOrEqual(restoredExperience.currentExp, Mathf.RoundToInt(baseExp * 1.35f), "Compensation EXP should respect bonus cap.");
            Assert.LessOrEqual(restoredWallet.Credits, Mathf.RoundToInt(baseCredits * 1.35f), "Compensation credits should respect bonus cap.");
        }

        private QuestSystem CreateQuestRuntime(string rootName, out CurrencyWallet wallet, out PlayerExperienceSystem experienceSystem, bool saveQuestRuntimeState)
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
            questSystem.saveQuestRuntimeState = saveQuestRuntimeState;
            questSystem.autoStartGuidedQuests = false;
            questSystem.wallet = wallet;
            questSystem.BindExperienceSystem(experienceSystem);
            questSystem.expRewardMultiplier = 1f;
            questSystem.pearlRewardMultiplier = 1f;
            questSystem.levelRewardMultiplier = 1f;
            questSystem.levelDifficulty = 3;
            questSystem.levelChapterId = 4;
            questSystem.enableFailureCompensation = true;
            questSystem.maxTrackedFailureStreak = 8;
            questSystem.logFailureCompensation = false;
            return questSystem;
        }

        private static QuestData BuildQuest(
            string questId,
            int expReward,
            int creditReward,
            bool allowCompensation,
            int minFailureStreak = 2,
            float bonusPerFailure = 0.08f,
            float bonusCap = 0.35f,
            float debtPayoutCap = 0.7f,
            int chapterWindow = 1,
            int streakDecayOnComplete = 1)
        {
            return new QuestData
            {
                questId = questId,
                questName = questId,
                questType = QuestType.CompleteStronghold,
                rewardTier = QuestRewardTier.Side,
                difficultyRating = 3,
                targetStrongholdId = "SH_LATE",
                allowFailureCompensation = allowCompensation,
                compensationMinFailureStreak = minFailureStreak,
                compensationBonusPerFailure = bonusPerFailure,
                compensationBonusCap = bonusCap,
                compensationDebtPayoutCap = debtPayoutCap,
                compensationChapterWindow = chapterWindow,
                compensationStreakDecayOnComplete = streakDecayOnComplete,
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

        private static int ExpectedQuestExp(QuestSystem questSystem, QuestData data)
        {
            return EconomyService.AdjustQuestExp(
                baseExp: data.reward.exp,
                questType: data.questType,
                questDifficulty: data.difficultyRating,
                levelDifficulty: questSystem.levelDifficulty,
                rewardMultiplier: questSystem.expRewardMultiplier * questSystem.levelRewardMultiplier,
                rewardTier: data.rewardTier,
                chapterId: questSystem.levelChapterId,
                strongholdId: data.targetStrongholdId);
        }

        private static int ExpectedQuestCredits(QuestSystem questSystem, QuestData data)
        {
            return EconomyService.AdjustQuestCredits(
                baseCredits: data.reward.credits,
                questType: data.questType,
                questDifficulty: data.difficultyRating,
                levelDifficulty: questSystem.levelDifficulty,
                rewardMultiplier: questSystem.levelRewardMultiplier,
                rewardTier: data.rewardTier,
                chapterId: questSystem.levelChapterId,
                strongholdId: data.targetStrongholdId);
        }

        private static int ReadPrivateInt(object instance, string fieldName)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"Expected private field: {fieldName}");
            return (int)field.GetValue(instance);
        }

        private static float ReadPrivateFloat(object instance, string fieldName)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"Expected private field: {fieldName}");
            return (float)field.GetValue(instance);
        }
    }
}
