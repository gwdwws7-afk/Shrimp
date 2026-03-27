using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ThirdPersonController.Tests
{
    public class SteamIntegrationBootstrapRegressionTests
    {
        [Test]
        public void SteamIntegrationBootstrap_Bootstrap_AppliesResolvedConfigToServiceAndTrackers()
        {
            SteamIntegrationService service = SteamIntegrationService.Instance;
            Assert.NotNull(service);

            SteamAchievementTracker tracker = service.GetComponent<SteamAchievementTracker>();
            SteamStatsTracker stats = service.GetComponent<SteamStatsTracker>();
            SteamCloudSaveBridge bridge = service.GetComponent<SteamCloudSaveBridge>();

            bool oldEnableSteam = service.enableSteam;
            bool oldLogWhenUnavailable = service.logWhenUnavailable;
            uint oldAppId = service.appId;
            bool oldPreferReflectionBackend = service.preferReflectionBackend;
            bool oldEnableAchievements = tracker != null && tracker.enableAchievements;
            bool oldEnableStats = stats != null && stats.enableStats;
            float oldFlushInterval = stats != null ? stats.flushInterval : 20f;
            bool oldEnableCloudSaves = bridge != null && bridge.enableCloudSaves;
            bool oldPullOnStart = bridge != null && bridge.pullOnStart;
            bool oldUploadOnSave = bridge != null && bridge.uploadOnSave;
            bool oldUploadSettings = bridge != null && bridge.uploadSettings;
            bool oldApplySettingsAfterPull = bridge != null && bridge.applySettingsAfterPull;
            bool oldOnlyIfLocalMissing = bridge != null && bridge.onlyIfLocalMissing;
            float oldUploadCooldown = bridge != null ? bridge.uploadCooldown : 1.5f;
            SteamCloudSaveBridge.CloudSavePriority oldPriority = bridge != null
                ? bridge.priority
                : SteamCloudSaveBridge.CloudSavePriority.LocalPreferred;

            SteamIntegrationConfig config = ScriptableObject.CreateInstance<SteamIntegrationConfig>();
            config.enableSteam = false;
            config.logWhenUnavailable = false;
            config.appId = 13579u;
            config.preferReflectionBackend = false;
            config.enableAchievements = false;
            config.enableStats = false;
            config.statsFlushInterval = 4f;
            config.enableCloudSaves = false;
            config.pullCloudOnStart = false;
            config.uploadCloudOnSave = false;
            config.uploadSettings = true;
            config.applySettingsAfterPull = false;
            config.cloudOnlyIfLocalMissing = false;
            config.cloudUploadCooldown = 2.2f;
            config.cloudPriority = SteamCloudSaveBridge.CloudSavePriority.CloudPreferred;

            FieldInfo resolverField = typeof(SteamIntegrationBootstrap).GetField(
                "ConfigResolverOverride",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(resolverField, "ConfigResolverOverride field should exist.");

            MethodInfo bootstrapMethod = typeof(SteamIntegrationBootstrap).GetMethod(
                "Bootstrap",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(bootstrapMethod, "Bootstrap method should exist.");

            try
            {
                resolverField.SetValue(null, new Func<SteamIntegrationConfig>(() => config));
                bootstrapMethod.Invoke(null, null);

                tracker = service.GetComponent<SteamAchievementTracker>();
                stats = service.GetComponent<SteamStatsTracker>();
                bridge = service.GetComponent<SteamCloudSaveBridge>();

                Assert.NotNull(tracker);
                Assert.NotNull(stats);
                Assert.NotNull(bridge);

                Assert.IsFalse(service.enableSteam);
                Assert.IsFalse(service.logWhenUnavailable);
                Assert.AreEqual(13579u, service.appId);
                Assert.IsFalse(service.preferReflectionBackend);
                Assert.IsFalse(tracker.enableAchievements);
                Assert.IsFalse(stats.enableStats);
                Assert.AreEqual(4f, stats.flushInterval, 0.0001f);
                Assert.IsFalse(bridge.enableCloudSaves);
                Assert.IsFalse(bridge.pullOnStart);
                Assert.IsFalse(bridge.uploadOnSave);
                Assert.IsTrue(bridge.uploadSettings);
                Assert.IsFalse(bridge.applySettingsAfterPull);
                Assert.IsFalse(bridge.onlyIfLocalMissing);
                Assert.AreEqual(2.2f, bridge.uploadCooldown, 0.0001f);
                Assert.AreEqual(SteamCloudSaveBridge.CloudSavePriority.CloudPreferred, bridge.priority);
            }
            finally
            {
                resolverField.SetValue(null, null);
                service.enableSteam = oldEnableSteam;
                service.logWhenUnavailable = oldLogWhenUnavailable;
                service.appId = oldAppId;
                service.preferReflectionBackend = oldPreferReflectionBackend;

                tracker = service.GetComponent<SteamAchievementTracker>();
                if (tracker != null)
                {
                    tracker.enableAchievements = oldEnableAchievements;
                }

                stats = service.GetComponent<SteamStatsTracker>();
                if (stats != null)
                {
                    stats.enableStats = oldEnableStats;
                    stats.flushInterval = oldFlushInterval;
                }

                bridge = service.GetComponent<SteamCloudSaveBridge>();
                if (bridge != null)
                {
                    bridge.enableCloudSaves = oldEnableCloudSaves;
                    bridge.pullOnStart = oldPullOnStart;
                    bridge.uploadOnSave = oldUploadOnSave;
                    bridge.uploadSettings = oldUploadSettings;
                    bridge.applySettingsAfterPull = oldApplySettingsAfterPull;
                    bridge.onlyIfLocalMissing = oldOnlyIfLocalMissing;
                    bridge.uploadCooldown = oldUploadCooldown;
                    bridge.priority = oldPriority;
                }

                service.RefreshRuntimeStatus(force: true);
                UnityEngine.Object.DestroyImmediate(config);
            }
        }
    }
}
