using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ThirdPersonController.Tests
{
    public class BossLevel10GateRegressionTests
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
        }

        [Test]
        public void LevelRuntimeConfigurator_RuntimeBossGateWiring_BindsSingleDefeatHandler()
        {
            GameObject flowGo = new GameObject("RuntimeBossGate_LevelFlow");
            createdObjects.Add(flowGo);
            LevelFlowController levelFlow = flowGo.AddComponent<LevelFlowController>();

            GameObject sequenceGo = new GameObject("RuntimeBossGate_StrongholdSequence");
            createdObjects.Add(sequenceGo);
            StrongholdSequenceController sequence = sequenceGo.AddComponent<StrongholdSequenceController>();
            sequence.autoStartFirst = false;

            GameObject bossSpawnGo = new GameObject("RuntimeBossGate_BossSpawnPoint");
            createdObjects.Add(bossSpawnGo);
            BossSpawnPoint bossSpawnPoint = bossSpawnGo.AddComponent<BossSpawnPoint>();
            bossSpawnPoint.spawnOnStart = true;

            LevelData levelData = ScriptableObject.CreateInstance<LevelData>();
            createdObjects.Add(levelData);
            levelData.levelId = "LEVEL_10";
            levelData.chapterId = 1;
            levelData.overrideBossSettings = true;
            levelData.bossName = "Boss_RuntimeGate";
            levelData.bossMaxHealth = 4200;
            levelData.bossBaseDamage = 38;
            levelData.bossKnockback = 7.2f;
            levelData.bossScaleMultiplier = 2.3f;
            levelData.bossSpawnOffset = new Vector3(0f, 0f, 1f);

            LevelRuntimeConfigurator runtimeConfigurator = flowGo.AddComponent<LevelRuntimeConfigurator>();
            runtimeConfigurator.autoApplyOnAwake = false;
            runtimeConfigurator.ensureRuntimeWiring = false;
            runtimeConfigurator.applyStrongholds = false;
            runtimeConfigurator.applyQuests = false;
            runtimeConfigurator.applyRewards = false;
            runtimeConfigurator.levelFlow = levelFlow;
            runtimeConfigurator.levelData = levelData;
            runtimeConfigurator.sequenceController = sequence;
            runtimeConfigurator.bossSpawnPoint = bossSpawnPoint;

            runtimeConfigurator.Apply();

            Assert.IsFalse(bossSpawnPoint.spawnOnStart, "Boss should not spawn at scene start for boss-gated levels.");
            Assert.IsTrue(sequence.deferCompletionUntilBoss, "Boss gate should be enabled when boss override is active.");
            Assert.AreSame(bossSpawnPoint, sequence.bossSpawnPoint, "Sequence should be wired to configured boss spawn point.");
            Assert.AreEqual(1, CountBossDefeatHandlers(bossSpawnPoint, sequence),
                "Runtime boss gate wiring should register exactly one defeat handler.");

            runtimeConfigurator.Apply();
            Assert.AreEqual(1, CountBossDefeatHandlers(bossSpawnPoint, sequence),
                "Repeated Apply calls should not duplicate boss defeat handlers.");
        }

        [UnityTest]
        public IEnumerator Level10Scene_BossGateChain_IsWiredForStrongholdThenBossFlow()
        {
            Scene baselineScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene("Level_10_HiveCore", LoadSceneMode.Additive);
            yield return null;
            yield return null;

            Scene levelScene = SceneManager.GetSceneByName("Level_10_HiveCore");
            Assert.IsTrue(levelScene.IsValid() && levelScene.isLoaded, "Level_10 scene should be loaded.");

            LevelFlowController levelFlow = FindComponentInScene<LevelFlowController>(levelScene);
            Assert.NotNull(levelFlow, "LevelFlowController should exist in Level_10 scene.");
            Assert.NotNull(levelFlow.levelData, "LevelFlowController should resolve level data.");
            Assert.AreEqual("LEVEL_10", levelFlow.levelData.levelId, "Level_10 scene should bind LevelData_Level10.");

            StrongholdSequenceController sequence = FindComponentInScene<StrongholdSequenceController>(levelScene);
            BossSpawnPoint bossSpawnPoint = FindComponentInScene<BossSpawnPoint>(levelScene);

            Assert.NotNull(sequence, "StrongholdSequenceController should exist.");
            Assert.NotNull(bossSpawnPoint, "BossSpawnPoint should exist.");
            Assert.IsTrue(sequence.deferCompletionUntilBoss, "Level_10 should defer completion until boss is defeated.");
            Assert.AreSame(bossSpawnPoint, sequence.bossSpawnPoint, "Stronghold sequence should reference scene boss spawn point.");
            Assert.IsFalse(bossSpawnPoint.spawnOnStart, "Boss should not spawn at scene start in Level_10.");

            AsyncOperation unload = SceneManager.UnloadSceneAsync(levelScene);
            Assert.NotNull(unload, "Level_10 scene unload operation should be created.");
            while (!unload.isDone)
            {
                yield return null;
            }

            if (baselineScene.IsValid())
            {
                SceneManager.SetActiveScene(baselineScene);
            }
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

        private static T FindComponentInScene<T>(Scene scene) where T : Component
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return null;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (root == null)
                {
                    continue;
                }

                T component = root.GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }
    }
}
