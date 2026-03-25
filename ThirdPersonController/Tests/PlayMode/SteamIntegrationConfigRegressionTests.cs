using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ThirdPersonController.Tests
{
    public class SteamIntegrationConfigRegressionTests
    {
        private readonly List<Object> createdObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
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
        public void SteamAchievementTracker_ApplyConfig_MapsEnableFlag()
        {
            GameObject go = new GameObject("SteamAchievementTracker_Config");
            createdObjects.Add(go);
            SteamAchievementTracker tracker = go.AddComponent<SteamAchievementTracker>();

            SteamIntegrationConfig config = ScriptableObject.CreateInstance<SteamIntegrationConfig>();
            createdObjects.Add(config);
            config.enableAchievements = false;

            tracker.ApplyConfig(config);
            Assert.IsFalse(tracker.enableAchievements);
        }

        [Test]
        public void SteamStatsTracker_ApplyConfig_MapsEnableAndFlushInterval()
        {
            GameObject go = new GameObject("SteamStatsTracker_Config");
            createdObjects.Add(go);
            SteamStatsTracker tracker = go.AddComponent<SteamStatsTracker>();

            SteamIntegrationConfig config = ScriptableObject.CreateInstance<SteamIntegrationConfig>();
            createdObjects.Add(config);
            config.enableStats = false;
            config.statsFlushInterval = 5f;

            tracker.ApplyConfig(config);
            Assert.IsFalse(tracker.enableStats);
            Assert.AreEqual(5f, tracker.flushInterval, 0.0001f);
        }

        [Test]
        public void SteamCloudSaveBridge_ApplyConfig_MapsCloudSettings()
        {
            GameObject go = new GameObject("SteamCloudSaveBridge_Config");
            createdObjects.Add(go);
            SteamCloudSaveBridge bridge = go.AddComponent<SteamCloudSaveBridge>();

            SteamIntegrationConfig config = ScriptableObject.CreateInstance<SteamIntegrationConfig>();
            createdObjects.Add(config);
            config.enableCloudSaves = false;
            config.pullCloudOnStart = false;
            config.uploadCloudOnSave = false;
            config.uploadSettings = true;
            config.applySettingsAfterPull = false;
            config.cloudOnlyIfLocalMissing = false;
            config.cloudUploadCooldown = 3f;
            config.cloudPriority = SteamCloudSaveBridge.CloudSavePriority.CloudPreferred;

            bridge.ApplyConfig(config);

            Assert.IsFalse(bridge.enableCloudSaves);
            Assert.IsFalse(bridge.pullOnStart);
            Assert.IsFalse(bridge.uploadOnSave);
            Assert.IsTrue(bridge.uploadSettings);
            Assert.IsFalse(bridge.applySettingsAfterPull);
            Assert.IsFalse(bridge.onlyIfLocalMissing);
            Assert.AreEqual(3f, bridge.uploadCooldown, 0.0001f);
            Assert.AreEqual(SteamCloudSaveBridge.CloudSavePriority.CloudPreferred, bridge.priority);
        }
    }
}
