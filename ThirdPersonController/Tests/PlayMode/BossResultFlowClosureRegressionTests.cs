using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThirdPersonController.Tests
{
    public class BossResultFlowClosureRegressionTests
    {
        private readonly List<UnityEngine.Object> createdObjects = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                UnityEngine.Object obj = createdObjects[i];
                if (obj != null)
                {
                    UnityEngine.Object.DestroyImmediate(obj);
                }
            }

            createdObjects.Clear();
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        [Test]
        public void LevelFlowUI_ResultHint_UsesUnifiedActionLabels()
        {
            GameObject root = new GameObject("BossResultHintLabels");
            createdObjects.Add(root);

            PlayerInputHandler input = root.AddComponent<PlayerInputHandler>();
            LevelFlowUIController flowUi = root.AddComponent<LevelFlowUIController>();
            flowUi.inputHandler = input;
            flowUi.continueActionName = "MenuConfirm";
            flowUi.retryActionName = "MenuRetry";
            flowUi.continueKey = KeyCode.Return;
            flowUi.retryKey = KeyCode.R;

            string hint = InvokePrivateString(flowUi, "GetResultHintLabel");
            string continueLabel = ResolveExpectedActionLabel(input, flowUi.continueActionName, flowUi.continueKey);
            string retryLabel = ResolveExpectedActionLabel(input, flowUi.retryActionName, flowUi.retryKey);

            Assert.IsFalse(string.IsNullOrEmpty(hint), "Result hint should not be empty.");
            Assert.IsTrue(hint.Contains(continueLabel), $"Result hint should include continue action label '{continueLabel}'.");
            Assert.IsTrue(hint.Contains(retryLabel), $"Result hint should include retry action label '{retryLabel}'.");
        }

        [Test]
        public void LevelFlowUI_ResultHint_FallsBackToFriendlyKeysWithoutInputHandler()
        {
            GameObject root = new GameObject("BossResultHintFallback");
            createdObjects.Add(root);

            LevelFlowUIController flowUi = root.AddComponent<LevelFlowUIController>();
            flowUi.inputHandler = null;
            flowUi.continueKey = KeyCode.Return;
            flowUi.retryKey = KeyCode.R;

            string hint = InvokePrivateString(flowUi, "GetResultHintLabel");
            string continueKeyLabel = PlayerInputHandler.GetFriendlyKeyLabel(flowUi.continueKey);
            string retryKeyLabel = PlayerInputHandler.GetFriendlyKeyLabel(flowUi.retryKey);

            Assert.IsFalse(string.IsNullOrEmpty(hint), "Fallback result hint should not be empty.");
            Assert.IsTrue(hint.Contains(continueKeyLabel), $"Fallback hint should include continue key label '{continueKeyLabel}'.");
            Assert.IsTrue(hint.Contains(retryKeyLabel), $"Fallback hint should include retry key label '{retryKeyLabel}'.");
        }

        [Test]
        public void LevelFlowUI_ResultState_GameOverThenPlayerDeath_KeepsInitialVictoryState()
        {
            GameObject root = new GameObject("BossResultStateVictoryFirst");
            createdObjects.Add(root);

            LevelFlowUIController flowUi = root.AddComponent<LevelFlowUIController>();
            flowUi.pauseDuringResult = false;

            InvokePrivateMethod(flowUi, "HandleGameOver", true);
            Assert.IsTrue(GetPrivateBool(flowUi, "showResult"), "GameOver should open result state.");
            Assert.IsTrue(GetPrivateBool(flowUi, "lastVictory"), "Initial victory game-over should mark victory state.");

            InvokePrivateMethod(flowUi, "HandlePlayerDeath");
            Assert.IsTrue(GetPrivateBool(flowUi, "showResult"), "Result state should stay open after subsequent player death event.");
            Assert.IsTrue(GetPrivateBool(flowUi, "lastVictory"),
                "Result state should keep the first terminal outcome once panel is shown.");
        }

        [Test]
        public void LevelFlowUI_ResultState_PlayerDeathThenGameOver_KeepsInitialDefeatState()
        {
            GameObject root = new GameObject("BossResultStateDefeatFirst");
            createdObjects.Add(root);

            LevelFlowUIController flowUi = root.AddComponent<LevelFlowUIController>();
            flowUi.pauseDuringResult = false;

            InvokePrivateMethod(flowUi, "HandlePlayerDeath");
            Assert.IsTrue(GetPrivateBool(flowUi, "showResult"), "Player death should open result state.");
            Assert.IsFalse(GetPrivateBool(flowUi, "lastVictory"), "Player death should mark defeat state.");

            InvokePrivateMethod(flowUi, "HandleGameOver", true);
            Assert.IsTrue(GetPrivateBool(flowUi, "showResult"), "Result state should stay open after later game-over event.");
            Assert.IsFalse(GetPrivateBool(flowUi, "lastVictory"),
                "Result state should preserve initial defeat outcome and avoid victory overwrite.");
        }

        [Test]
        public void LevelFlowUI_ResultState_EventStorm_DoesNotOverrideOutcomeOrRecacheSnapshot()
        {
            GameObject root = new GameObject("BossResultEventStormSnapshot");
            createdObjects.Add(root);

            StatisticsManager statistics = root.AddComponent<StatisticsManager>();
            statistics.sessionKills = 7;
            statistics.sessionHighestCombo = 5;

            LevelFlowUIController flowUi = root.AddComponent<LevelFlowUIController>();
            flowUi.pauseDuringResult = false;
            flowUi.rewardTracker = null;
            flowUi.statisticsManager = statistics;
            flowUi.longTermProgression = null;

            flowUi.ShowResult(true);

            int initialCachedKills = GetPrivateInt(flowUi, "cachedKills");
            int initialCachedCombo = GetPrivateInt(flowUi, "cachedCombo");
            Assert.AreEqual(7, initialCachedKills, "Initial result snapshot should cache current session kills.");
            Assert.AreEqual(5, initialCachedCombo, "Initial result snapshot should cache current highest combo.");
            Assert.IsTrue(GetPrivateBool(flowUi, "lastVictory"), "First terminal event should define victory state.");

            statistics.sessionKills = 99;
            statistics.sessionHighestCombo = 99;

            InvokePrivateMethod(flowUi, "HandlePlayerDeath");
            InvokePrivateMethod(flowUi, "HandleGameOver", false);

            Assert.IsTrue(GetPrivateBool(flowUi, "showResult"), "Event storm should not close result panel unexpectedly.");
            Assert.IsTrue(GetPrivateBool(flowUi, "lastVictory"), "Late terminal events should not overwrite first outcome.");
            Assert.AreEqual(initialCachedKills, GetPrivateInt(flowUi, "cachedKills"),
                "Late terminal events should not rebuild cached result snapshot while result panel is open.");
            Assert.AreEqual(initialCachedCombo, GetPrivateInt(flowUi, "cachedCombo"),
                "Late terminal events should not rebuild cached combo snapshot while result panel is open.");
        }

        [Test]
        public void LevelFlowUI_ContinueFromResult_VictoryMarksLevelCompleteOnlyOnce()
        {
            GameObject root = new GameObject("BossResultContinueVictory");
            createdObjects.Add(root);

            LevelFlowController levelFlow = root.AddComponent<LevelFlowController>();
            levelFlow.levelId = 910;
            levelFlow.mainMenuSceneName = "MainMenu_Test";
            levelFlow.suppressSceneLoadForTests = true;
            levelFlow.suppressSaveForTests = true;

            LevelFlowUIController flowUi = root.AddComponent<LevelFlowUIController>();
            flowUi.levelFlow = levelFlow;
            flowUi.pauseDuringResult = false;

            int completedCount = 0;
            int lastCompletedLevelId = -1;
            Action<int> onLevelCompleted = levelId =>
            {
                completedCount++;
                lastCompletedLevelId = levelId;
            };

            GameEvents.OnLevelCompleted += onLevelCompleted;
            try
            {
                flowUi.ShowResult(true);
                InvokePrivateMethod(flowUi, "ContinueFromResult");

                Assert.AreEqual(1, completedCount, "Victory continue should mark level complete exactly once on first continue.");
                Assert.AreEqual(910, lastCompletedLevelId);
                Assert.IsTrue(GetPrivateBool(levelFlow, "levelCompleted"));
                Assert.AreEqual("MainMenu_Test", levelFlow.DebugLastRequestedScene);
                Assert.AreEqual(1, levelFlow.DebugSaveRequestCount, "Continue path should request save once.");
                Assert.AreEqual(1, levelFlow.DebugSceneRequestCount, "Continue path should request scene transition once.");

                InvokePrivateMethod(flowUi, "ContinueFromResult");
                Assert.AreEqual(1, completedCount, "Repeated continue should not emit duplicate LevelCompleted event.");
                Assert.AreEqual(1, levelFlow.DebugSaveRequestCount, "Repeated continue should be debounced after first action.");
                Assert.AreEqual(1, levelFlow.DebugSceneRequestCount, "Repeated continue should not request scene transition again.");
            }
            finally
            {
                GameEvents.OnLevelCompleted -= onLevelCompleted;
            }
        }

        [Test]
        public void LevelFlowUI_ContinueFromResult_IgnoresLateTerminalEventsAfterActionConsumed()
        {
            GameObject root = new GameObject("BossResultContinueLateEventIgnore");
            createdObjects.Add(root);

            LevelFlowController levelFlow = root.AddComponent<LevelFlowController>();
            levelFlow.mainMenuSceneName = "MainMenu_Test";
            levelFlow.suppressSceneLoadForTests = true;
            levelFlow.suppressSaveForTests = true;

            LevelFlowUIController flowUi = root.AddComponent<LevelFlowUIController>();
            flowUi.levelFlow = levelFlow;
            flowUi.pauseDuringResult = false;

            flowUi.ShowResult(true);
            InvokePrivateMethod(flowUi, "ContinueFromResult");

            Assert.IsFalse(GetPrivateBool(flowUi, "showResult"), "Continue should close result panel.");
            Assert.AreEqual(1, levelFlow.DebugSaveRequestCount);
            Assert.AreEqual(1, levelFlow.DebugSceneRequestCount);

            InvokePrivateMethod(flowUi, "HandlePlayerDeath");
            InvokePrivateMethod(flowUi, "HandleGameOver", false);

            Assert.IsFalse(GetPrivateBool(flowUi, "showResult"),
                "Late terminal events should be ignored after continue action is consumed.");
            Assert.AreEqual(1, levelFlow.DebugSaveRequestCount,
                "Late terminal events after continue should not trigger extra save requests.");
            Assert.AreEqual(1, levelFlow.DebugSceneRequestCount,
                "Late terminal events after continue should not trigger extra scene requests.");
        }

        [Test]
        public void LevelFlowUI_ContinueFromResult_DefeatDoesNotMarkLevelComplete()
        {
            GameObject root = new GameObject("BossResultContinueDefeat");
            createdObjects.Add(root);

            LevelFlowController levelFlow = root.AddComponent<LevelFlowController>();
            levelFlow.levelId = 911;
            levelFlow.mainMenuSceneName = "MainMenu_Test";
            levelFlow.suppressSceneLoadForTests = true;
            levelFlow.suppressSaveForTests = true;

            LevelFlowUIController flowUi = root.AddComponent<LevelFlowUIController>();
            flowUi.levelFlow = levelFlow;
            flowUi.pauseDuringResult = false;

            int completedCount = 0;
            Action<int> onLevelCompleted = _ => completedCount++;
            GameEvents.OnLevelCompleted += onLevelCompleted;
            try
            {
                flowUi.ShowResult(false);
                InvokePrivateMethod(flowUi, "ContinueFromResult");

                Assert.AreEqual(0, completedCount, "Defeat continue should not mark level as completed.");
                Assert.IsFalse(GetPrivateBool(levelFlow, "levelCompleted"));
                Assert.AreEqual("MainMenu_Test", levelFlow.DebugLastRequestedScene);
                Assert.AreEqual(1, levelFlow.DebugSaveRequestCount, "Defeat continue should still request save before returning to menu.");
                Assert.AreEqual(1, levelFlow.DebugSceneRequestCount, "Defeat continue should request scene transition once.");

                InvokePrivateMethod(flowUi, "ContinueFromResult");
                Assert.AreEqual(0, completedCount, "Debounced continue should keep defeat state without completion event.");
                Assert.AreEqual(1, levelFlow.DebugSaveRequestCount, "Repeated continue should not save again after debounce.");
                Assert.AreEqual(1, levelFlow.DebugSceneRequestCount, "Repeated continue should not request scene transition again.");
            }
            finally
            {
                GameEvents.OnLevelCompleted -= onLevelCompleted;
            }
        }

        [Test]
        public void LevelFlowUI_RetryFromResult_IgnoresLateTerminalEventsAfterActionConsumed()
        {
            GameObject root = new GameObject("BossResultRetryLateEventIgnore");
            createdObjects.Add(root);

            LevelFlowController levelFlow = root.AddComponent<LevelFlowController>();
            levelFlow.suppressSceneLoadForTests = true;
            levelFlow.suppressSaveForTests = true;

            LevelFlowUIController flowUi = root.AddComponent<LevelFlowUIController>();
            flowUi.levelFlow = levelFlow;
            flowUi.pauseDuringResult = false;

            flowUi.ShowResult(false);
            InvokePrivateMethod(flowUi, "RetryFromResult");

            Assert.IsFalse(GetPrivateBool(flowUi, "showResult"), "Retry should close result panel.");
            Assert.AreEqual(0, levelFlow.DebugSaveRequestCount);
            Assert.AreEqual(1, levelFlow.DebugSceneRequestCount);

            InvokePrivateMethod(flowUi, "HandlePlayerDeath");
            InvokePrivateMethod(flowUi, "HandleGameOver", true);

            Assert.IsFalse(GetPrivateBool(flowUi, "showResult"),
                "Late terminal events should be ignored after retry action is consumed.");
            Assert.AreEqual(0, levelFlow.DebugSaveRequestCount,
                "Late terminal events after retry should still avoid save requests.");
            Assert.AreEqual(1, levelFlow.DebugSceneRequestCount,
                "Late terminal events after retry should not add scene requests.");
        }

        [Test]
        public void LevelFlowUI_RetryFromResult_ReloadsActiveSceneWithoutCompletion()
        {
            GameObject root = new GameObject("BossResultRetry");
            createdObjects.Add(root);

            LevelFlowController levelFlow = root.AddComponent<LevelFlowController>();
            levelFlow.levelId = 912;
            levelFlow.suppressSceneLoadForTests = true;
            levelFlow.suppressSaveForTests = true;

            LevelFlowUIController flowUi = root.AddComponent<LevelFlowUIController>();
            flowUi.levelFlow = levelFlow;
            flowUi.pauseDuringResult = false;

            int completedCount = 0;
            Action<int> onLevelCompleted = _ => completedCount++;
            GameEvents.OnLevelCompleted += onLevelCompleted;
            try
            {
                string activeSceneName = SceneManager.GetActiveScene().name;
                flowUi.ShowResult(false);
                InvokePrivateMethod(flowUi, "RetryFromResult");

                Assert.AreEqual(0, completedCount, "Retry should not mark level complete.");
                Assert.AreEqual(activeSceneName, levelFlow.DebugLastRequestedScene, "Retry should request reloading current active scene.");
                Assert.AreEqual(0, levelFlow.DebugSaveRequestCount, "Retry path should not request save.");
                Assert.AreEqual(1, levelFlow.DebugSceneRequestCount, "Retry path should request scene reload once.");

                InvokePrivateMethod(flowUi, "RetryFromResult");
                Assert.AreEqual(0, levelFlow.DebugSaveRequestCount, "Debounced retry should not request save.");
                Assert.AreEqual(1, levelFlow.DebugSceneRequestCount, "Debounced retry should not request additional scene reloads.");
            }
            finally
            {
                GameEvents.OnLevelCompleted -= onLevelCompleted;
            }
        }

        [Test]
        public void LevelFlowUI_ContinueFromResult_WritesSaveToIsolatedPath()
        {
            RunWithIsolatedSaveStorage((saveManager, savePath, _) =>
            {
                GameObject root = new GameObject("BossResultContinueSaveIsolated");
                createdObjects.Add(root);

                LevelFlowController levelFlow = root.AddComponent<LevelFlowController>();
                levelFlow.levelId = 913;
                levelFlow.mainMenuSceneName = "MainMenu_Test";
                levelFlow.suppressSceneLoadForTests = true;
                levelFlow.suppressSaveForTests = false;

                LevelFlowUIController flowUi = root.AddComponent<LevelFlowUIController>();
                flowUi.levelFlow = levelFlow;
                flowUi.pauseDuringResult = false;

                saveManager.CurrentData.currentLevel = levelFlow.levelId;
                flowUi.ShowResult(true);
                InvokePrivateMethod(flowUi, "ContinueFromResult");

                Assert.IsTrue(File.Exists(savePath), "Continue should write save data to configured isolated path.");
                string content = File.ReadAllText(savePath);
                Assert.IsTrue(content.Contains("\"currentLevel\": 913") || content.Contains("\"currentLevel\":913"),
                    "Isolated save file should persist current level payload.");
                Assert.AreEqual(1, levelFlow.DebugSaveRequestCount);
                Assert.AreEqual("MainMenu_Test", levelFlow.DebugLastRequestedScene);
                Assert.AreEqual(1, levelFlow.DebugSceneRequestCount);

                InvokePrivateMethod(flowUi, "ContinueFromResult");
                Assert.AreEqual(1, levelFlow.DebugSaveRequestCount, "Debounced continue should not rewrite isolated save.");
                Assert.AreEqual(1, levelFlow.DebugSceneRequestCount, "Debounced continue should not request extra scene loads.");
            });
        }

        [Test]
        public void LevelFlowUI_RetryFromResult_DoesNotWriteSaveToIsolatedPath()
        {
            RunWithIsolatedSaveStorage((saveManager, savePath, _) =>
            {
                GameObject root = new GameObject("BossResultRetryNoSave");
                createdObjects.Add(root);

                LevelFlowController levelFlow = root.AddComponent<LevelFlowController>();
                levelFlow.levelId = 914;
                levelFlow.suppressSceneLoadForTests = true;
                levelFlow.suppressSaveForTests = false;

                LevelFlowUIController flowUi = root.AddComponent<LevelFlowUIController>();
                flowUi.levelFlow = levelFlow;
                flowUi.pauseDuringResult = false;

                string activeSceneName = SceneManager.GetActiveScene().name;
                flowUi.ShowResult(false);
                InvokePrivateMethod(flowUi, "RetryFromResult");

                Assert.IsFalse(File.Exists(savePath), "Retry should not trigger save write.");
                Assert.AreEqual(0, levelFlow.DebugSaveRequestCount);
                Assert.AreEqual(activeSceneName, levelFlow.DebugLastRequestedScene);
                Assert.AreEqual(1, levelFlow.DebugSceneRequestCount);

                InvokePrivateMethod(flowUi, "RetryFromResult");
                Assert.IsFalse(File.Exists(savePath), "Debounced retry should still avoid save writes.");
                Assert.AreEqual(0, levelFlow.DebugSaveRequestCount);
                Assert.AreEqual(1, levelFlow.DebugSceneRequestCount, "Debounced retry should not request extra reloads.");
            });
        }

        [Test]
        public void LevelFlowUI_ResultActions_WhenPanelClosed_AreIgnored()
        {
            GameObject root = new GameObject("BossResultClosedPanelIgnore");
            createdObjects.Add(root);

            LevelFlowController levelFlow = root.AddComponent<LevelFlowController>();
            levelFlow.suppressSceneLoadForTests = true;
            levelFlow.suppressSaveForTests = true;

            LevelFlowUIController flowUi = root.AddComponent<LevelFlowUIController>();
            flowUi.levelFlow = levelFlow;
            flowUi.pauseDuringResult = false;

            // Panel never opened: actions must be ignored.
            InvokePrivateMethod(flowUi, "ContinueFromResult");
            InvokePrivateMethod(flowUi, "RetryFromResult");

            Assert.AreEqual(0, levelFlow.DebugSaveRequestCount, "Closed panel should ignore continue/retry and avoid save requests.");
            Assert.AreEqual(0, levelFlow.DebugSceneRequestCount, "Closed panel should ignore continue/retry and avoid scene requests.");
            Assert.IsFalse(GetPrivateBool(flowUi, "showResult"), "Result panel should remain closed.");
        }

        [Test]
        public void LevelFlowUI_ResultActions_ReopenPanel_ResetsOneShotGate()
        {
            GameObject root = new GameObject("BossResultReopenReset");
            createdObjects.Add(root);

            LevelFlowController levelFlow = root.AddComponent<LevelFlowController>();
            levelFlow.mainMenuSceneName = "MainMenu_Test";
            levelFlow.suppressSceneLoadForTests = true;
            levelFlow.suppressSaveForTests = true;

            LevelFlowUIController flowUi = root.AddComponent<LevelFlowUIController>();
            flowUi.levelFlow = levelFlow;
            flowUi.pauseDuringResult = false;

            flowUi.ShowResult(true);
            InvokePrivateMethod(flowUi, "ContinueFromResult");
            Assert.IsFalse(GetPrivateBool(flowUi, "showResult"), "Continue should close result panel.");
            Assert.AreEqual(1, levelFlow.DebugSceneRequestCount, "First cycle should consume one scene request.");

            // Re-open result panel: one-shot gate should reset for new cycle.
            flowUi.ShowResult(false);
            InvokePrivateMethod(flowUi, "RetryFromResult");
            Assert.IsFalse(GetPrivateBool(flowUi, "showResult"), "Retry should close result panel.");
            Assert.AreEqual(2, levelFlow.DebugSceneRequestCount, "Reopened cycle should allow one new scene request.");
        }

        [Test]
        public void LevelFlowUI_ShowResult_PauseEnabled_FreezesTimeAndUnlocksCursor()
        {
            GameObject root = new GameObject("BossResultPauseEnter");
            createdObjects.Add(root);

            LevelFlowUIController flowUi = root.AddComponent<LevelFlowUIController>();
            flowUi.pauseDuringResult = true;

            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            flowUi.ShowResult(true);

            Assert.IsTrue(GetPrivateBool(flowUi, "showResult"), "ShowResult should display result panel.");
            Assert.AreEqual(0f, Time.timeScale, 0.0001f, "pauseDuringResult=true should freeze time.");
            Assert.AreEqual(CursorLockMode.None, Cursor.lockState, "Result panel should unlock cursor.");
            Assert.IsTrue(Cursor.visible, "Result panel should show cursor.");
        }

        [Test]
        public void LevelFlowUI_ContinueFromResult_PauseEnabled_RestoresTimeAndCursor_WhenSceneLoadSuppressed()
        {
            GameObject root = new GameObject("BossResultPauseExitContinue");
            createdObjects.Add(root);

            LevelFlowController levelFlow = root.AddComponent<LevelFlowController>();
            levelFlow.mainMenuSceneName = "MainMenu_Test";
            levelFlow.suppressSceneLoadForTests = true;
            levelFlow.suppressSaveForTests = true;

            LevelFlowUIController flowUi = root.AddComponent<LevelFlowUIController>();
            flowUi.levelFlow = levelFlow;
            flowUi.pauseDuringResult = true;

            flowUi.ShowResult(true);
            InvokePrivateMethod(flowUi, "ContinueFromResult");

            Assert.IsFalse(GetPrivateBool(flowUi, "showResult"), "Continue should close result panel.");
            Assert.AreEqual(1f, Time.timeScale, 0.0001f, "Continue should restore time scale even without scene transition.");
            Assert.IsFalse(Cursor.visible, "Continue should hide cursor after panel closes.");
        }

        [Test]
        public void LevelFlowUI_RetryFromResult_PauseEnabled_RestoresTimeAndCursor_WhenSceneLoadSuppressed()
        {
            GameObject root = new GameObject("BossResultPauseExitRetry");
            createdObjects.Add(root);

            LevelFlowController levelFlow = root.AddComponent<LevelFlowController>();
            levelFlow.suppressSceneLoadForTests = true;
            levelFlow.suppressSaveForTests = true;

            LevelFlowUIController flowUi = root.AddComponent<LevelFlowUIController>();
            flowUi.levelFlow = levelFlow;
            flowUi.pauseDuringResult = true;

            flowUi.ShowResult(false);
            InvokePrivateMethod(flowUi, "RetryFromResult");

            Assert.IsFalse(GetPrivateBool(flowUi, "showResult"), "Retry should close result panel.");
            Assert.AreEqual(1f, Time.timeScale, 0.0001f, "Retry should restore time scale even without scene transition.");
            Assert.IsFalse(Cursor.visible, "Retry should hide cursor after panel closes.");
        }

        private static string ResolveExpectedActionLabel(PlayerInputHandler input, string actionName, KeyCode fallback)
        {
            string label = input != null ? input.GetActionBindingLabel(actionName, fallback) : string.Empty;
            if (!string.IsNullOrEmpty(label))
            {
                return label;
            }

            return PlayerInputHandler.GetFriendlyKeyLabel(fallback);
        }

        private static string InvokePrivateString(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method, $"{methodName} should exist.");
            return method.Invoke(target, null) as string;
        }

        private static void InvokePrivateMethod(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method, $"{methodName} should exist.");
            method.Invoke(target, args);
        }

        private static bool GetPrivateBool(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"{fieldName} should exist.");
            return (bool)field.GetValue(target);
        }

        private static int GetPrivateInt(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"{fieldName} should exist.");
            return (int)field.GetValue(target);
        }

        private static void RunWithIsolatedSaveStorage(Action<SaveManager, string, string> body)
        {
            SaveManager saveManager = SaveManager.Instance;
            Assert.NotNull(saveManager, "SaveManager singleton should be available.");

            string oldSavePath = saveManager.overrideSavePathForTests;
            string oldSettingsPath = saveManager.overrideSettingsPathForTests;
            bool oldEncryptSave = saveManager.encryptSave;
            int oldCurrentLevel = saveManager.CurrentData != null ? saveManager.CurrentData.currentLevel : 1;

            string tempRoot = Path.Combine(Path.GetTempPath(), $"shrimp-boss-result-{Guid.NewGuid():N}");
            string tempSavePath = Path.Combine(tempRoot, "savegame.dat");
            string tempSettingsPath = Path.Combine(tempRoot, "settings.dat");
            Directory.CreateDirectory(tempRoot);

            try
            {
                saveManager.ConfigureTestStoragePaths(tempSavePath, tempSettingsPath);
                saveManager.encryptSave = false;
                if (saveManager.CurrentData == null)
                {
                    saveManager.LoadGame();
                }

                body(saveManager, tempSavePath, tempSettingsPath);
            }
            finally
            {
                saveManager.encryptSave = oldEncryptSave;
                if (saveManager.CurrentData != null)
                {
                    saveManager.CurrentData.currentLevel = oldCurrentLevel;
                }

                saveManager.ConfigureTestStoragePaths(oldSavePath, oldSettingsPath);

                if (Directory.Exists(tempRoot))
                {
                    try
                    {
                        Directory.Delete(tempRoot, true);
                    }
                    catch
                    {
                    }
                }
            }
        }
    }
}
