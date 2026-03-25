using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ThirdPersonController.Tests
{
    public class BossEncounterClosureRegressionTests
    {
        private readonly List<UnityEngine.Object> createdObjects = new List<UnityEngine.Object>();
        private int completedLevelId = -1;
        private int completedCount;
        private int gameOverCount;
        private bool lastGameOverVictory;

        [SetUp]
        public void SetUp()
        {
            completedLevelId = -1;
            completedCount = 0;
            gameOverCount = 0;
            lastGameOverVictory = false;

            GameEvents.OnLevelCompleted += HandleLevelCompleted;
            GameEvents.OnGameOver += HandleGameOver;
        }

        [TearDown]
        public void TearDown()
        {
            GameEvents.OnLevelCompleted -= HandleLevelCompleted;
            GameEvents.OnGameOver -= HandleGameOver;

            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                UnityEngine.Object obj = createdObjects[i];
                if (obj != null)
                {
                    UnityEngine.Object.DestroyImmediate(obj);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void StrongholdSequence_BossGate_WaitsForBossBeforeCompletion()
        {
            StrongholdSequenceController sequence = CreateSequence("BossClosure_WaitForBoss");
            sequence.levelId = 10;
            sequence.triggerLevelCompleteOnFinish = true;
            sequence.triggerVictoryOnFinish = true;

            StrongholdController stronghold = CreateStronghold("BossClosure_StrongholdA");
            sequence.ConfigureStrongholds(new List<StrongholdController> { stronghold });
            sequence.ActivateStronghold(0);

            BossSpawnPoint spawnPoint = CreateBossSpawnPoint("BossClosure_SpawnA");
            sequence.ConfigureBossGate(true, spawnPoint);

            InvokePrivateMethod(sequence, "HandleStrongholdCompleted", stronghold);
            Assert.AreEqual(0, completedCount, "Completing final stronghold should not complete level before boss is defeated.");
            Assert.AreEqual(0, gameOverCount, "Victory should not trigger before boss is defeated.");

            spawnPoint.OnBossDefeated?.Invoke(spawnPoint);
            Assert.AreEqual(1, completedCount, "Boss defeat should unlock deferred level completion.");
            Assert.AreEqual(10, completedLevelId, "Completed level id should match configured sequence level id.");
            Assert.AreEqual(1, gameOverCount, "Boss defeat should trigger exactly one game-over(victory) event.");
            Assert.IsTrue(lastGameOverVictory, "Deferred completion path should resolve as victory.");
        }

        [Test]
        public void StrongholdSequence_BossDefeatedFirst_CompletesAfterStrongholdChain()
        {
            StrongholdSequenceController sequence = CreateSequence("BossClosure_BossFirst");
            sequence.levelId = 9;
            sequence.triggerLevelCompleteOnFinish = true;
            sequence.triggerVictoryOnFinish = true;

            StrongholdController stronghold = CreateStronghold("BossClosure_StrongholdB");
            sequence.ConfigureStrongholds(new List<StrongholdController> { stronghold });
            sequence.ActivateStronghold(0);

            BossSpawnPoint spawnPoint = CreateBossSpawnPoint("BossClosure_SpawnB");
            sequence.ConfigureBossGate(true, spawnPoint);

            spawnPoint.OnBossDefeated?.Invoke(spawnPoint);
            Assert.AreEqual(0, completedCount, "Early boss defeat should not complete level while strongholds are unfinished.");
            Assert.AreEqual(0, gameOverCount, "Victory should remain blocked until stronghold chain completes.");

            InvokePrivateMethod(sequence, "HandleStrongholdCompleted", stronghold);
            Assert.AreEqual(1, completedCount, "Stronghold completion should flush deferred completion once boss is already defeated.");
            Assert.AreEqual(9, completedLevelId);
            Assert.AreEqual(1, gameOverCount);
            Assert.IsTrue(lastGameOverVictory);
        }

        [Test]
        public void StrongholdSequence_BossDefeatEventStorm_CompletesOnlyOnce()
        {
            StrongholdSequenceController sequence = CreateSequence("BossClosure_EventStorm");
            sequence.levelId = 11;
            sequence.triggerLevelCompleteOnFinish = true;
            sequence.triggerVictoryOnFinish = true;

            StrongholdController stronghold = CreateStronghold("BossClosure_EventStorm_Stronghold");
            sequence.ConfigureStrongholds(new List<StrongholdController> { stronghold });
            sequence.ActivateStronghold(0);

            BossSpawnPoint spawnPoint = CreateBossSpawnPoint("BossClosure_EventStorm_Spawn");
            sequence.ConfigureBossGate(true, spawnPoint);

            InvokePrivateMethod(sequence, "HandleStrongholdCompleted", stronghold);
            Assert.AreEqual(0, completedCount, "Level should still wait for boss after stronghold chain.");

            spawnPoint.OnBossDefeated?.Invoke(spawnPoint);
            spawnPoint.OnBossDefeated?.Invoke(spawnPoint);
            spawnPoint.OnBossDefeated?.Invoke(spawnPoint);

            Assert.AreEqual(1, completedCount, "Repeated boss defeated events should not duplicate completion.");
            Assert.AreEqual(1, gameOverCount, "Repeated boss defeated events should not duplicate victory event.");
            Assert.AreEqual(11, completedLevelId);
            Assert.IsTrue(lastGameOverVictory);
        }

        [Test]
        public void StrongholdSequence_ConfigureBossGate_RebindsWithoutDuplicateHandlers()
        {
            StrongholdSequenceController sequence = CreateSequence("BossClosure_Rebind");
            BossSpawnPoint spawnA = CreateBossSpawnPoint("BossClosure_RebindA");
            BossSpawnPoint spawnB = CreateBossSpawnPoint("BossClosure_RebindB");

            sequence.ConfigureBossGate(true, spawnA);
            Assert.AreEqual(1, CountBossDefeatHandlers(spawnA, sequence), "First bind should add exactly one defeat handler.");

            sequence.ConfigureBossGate(true, spawnB);
            Assert.AreEqual(0, CountBossDefeatHandlers(spawnA, sequence), "Switching boss gate should unbind old spawn point.");
            Assert.AreEqual(1, CountBossDefeatHandlers(spawnB, sequence), "Switching boss gate should bind new spawn point exactly once.");

            sequence.ConfigureBossGate(true, spawnB);
            Assert.AreEqual(1, CountBossDefeatHandlers(spawnB, sequence), "Repeated configure with same spawn point should not duplicate handlers.");
        }

        [Test]
        public void StrongholdSequence_ConfigureStrongholds_RebindsWithoutDuplicateHandlers()
        {
            StrongholdSequenceController sequence = CreateSequence("BossClosure_StrongholdRebind");
            StrongholdController strongholdA = CreateStronghold("BossClosure_StrongholdRebindA");
            StrongholdController strongholdB = CreateStronghold("BossClosure_StrongholdRebindB");

            sequence.ConfigureStrongholds(new List<StrongholdController> { strongholdA });
            Assert.AreEqual(1, CountStrongholdCompletedHandlers(strongholdA, sequence),
                "First stronghold bind should add exactly one completion handler.");

            sequence.ConfigureStrongholds(new List<StrongholdController> { strongholdB });
            Assert.AreEqual(0, CountStrongholdCompletedHandlers(strongholdA, sequence),
                "Rebinding stronghold list should remove handler from old stronghold.");
            Assert.AreEqual(1, CountStrongholdCompletedHandlers(strongholdB, sequence),
                "Rebinding stronghold list should add one handler to new stronghold.");

            sequence.ConfigureStrongholds(new List<StrongholdController> { strongholdB });
            Assert.AreEqual(1, CountStrongholdCompletedHandlers(strongholdB, sequence),
                "Repeated configure with same stronghold should not duplicate completion handlers.");
        }

        private StrongholdSequenceController CreateSequence(string name)
        {
            GameObject go = new GameObject(name);
            createdObjects.Add(go);
            StrongholdSequenceController sequence = go.AddComponent<StrongholdSequenceController>();
            sequence.autoStartFirst = false;
            return sequence;
        }

        private StrongholdController CreateStronghold(string name)
        {
            GameObject go = new GameObject(name);
            createdObjects.Add(go);
            StrongholdController stronghold = go.AddComponent<StrongholdController>();
            stronghold.activeOnStart = false;
            stronghold.startOnPlayerEnter = false;
            return stronghold;
        }

        private BossSpawnPoint CreateBossSpawnPoint(string name)
        {
            GameObject go = new GameObject(name);
            createdObjects.Add(go);
            BossSpawnPoint spawn = go.AddComponent<BossSpawnPoint>();
            spawn.spawnOnStart = false;
            return spawn;
        }

        private void HandleLevelCompleted(int levelId)
        {
            completedCount++;
            completedLevelId = levelId;
        }

        private void HandleGameOver(bool victory)
        {
            gameOverCount++;
            lastGameOverVictory = victory;
        }

        private static int CountBossDefeatHandlers(BossSpawnPoint spawnPoint, StrongholdSequenceController sequence)
        {
            Delegate callback = spawnPoint != null ? spawnPoint.OnBossDefeated : null;
            if (callback == null || sequence == null)
            {
                return 0;
            }

            int count = 0;
            Delegate[] delegates = callback.GetInvocationList();
            for (int i = 0; i < delegates.Length; i++)
            {
                Delegate item = delegates[i];
                if (ReferenceEquals(item.Target, sequence) &&
                    string.Equals(item.Method.Name, "HandleBossDefeated", StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountStrongholdCompletedHandlers(StrongholdController stronghold, StrongholdSequenceController sequence)
        {
            if (stronghold == null || sequence == null)
            {
                return 0;
            }

            FieldInfo field = typeof(StrongholdController).GetField(
                "OnStrongholdCompleted",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                return 0;
            }

            Delegate callback = field.GetValue(stronghold) as Delegate;
            if (callback == null)
            {
                return 0;
            }

            int count = 0;
            Delegate[] delegates = callback.GetInvocationList();
            for (int i = 0; i < delegates.Length; i++)
            {
                Delegate item = delegates[i];
                if (ReferenceEquals(item.Target, sequence) &&
                    string.Equals(item.Method.Name, "HandleStrongholdCompleted", StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

        private static object InvokePrivateMethod(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method, $"{methodName} should exist.");
            return method.Invoke(target, args);
        }
    }
}
