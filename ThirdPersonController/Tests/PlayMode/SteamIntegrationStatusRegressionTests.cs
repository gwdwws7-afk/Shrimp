using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ThirdPersonController.Tests
{
    public class SteamIntegrationStatusRegressionTests
    {
        [Test]
        public void SteamIntegrationService_RuntimeStatus_MatchesCurrentServiceFlags()
        {
            SteamIntegrationService service = SteamIntegrationService.Instance;
            Assert.NotNull(service);

            service.InitializeClient();
            SteamRuntimeStatus status = service.GetRuntimeStatus();

            Assert.AreEqual(service.enableSteam, status.steamEnabledByConfig);
            Assert.AreEqual(service.IsInitialized, status.clientInitialized);
            Assert.AreEqual(service.IsCloudAvailable, status.cloudAvailable);
            Assert.IsFalse(string.IsNullOrEmpty(status.backend), "Backend name should be populated.");
            Assert.AreEqual(service.requireRealBackend, status.realBackendRequired);
            Assert.AreEqual(!service.IsStubMode, status.realBackendReady);
        }

        [Test]
        public void SteamIntegrationService_RefreshRuntimeStatus_EmitsStateChange()
        {
            SteamIntegrationService service = SteamIntegrationService.Instance;
            Assert.NotNull(service);

            ISteamClient oldClient = GetClient(service);
            var events = new List<SteamRuntimeStatus>();
            service.OnRuntimeStatusChanged += events.Add;

            try
            {
                SetClient(service, new FakeSteamClient(false, false));
                service.RefreshRuntimeStatus(force: true);

                SetClient(service, new FakeSteamClient(true, true));
                service.RefreshRuntimeStatus(force: true);

                Assert.GreaterOrEqual(events.Count, 2, "Status change event should fire on forced refresh.");
                SteamRuntimeStatus last = events[events.Count - 1];
                Assert.IsTrue(last.clientInitialized);
                Assert.IsTrue(last.cloudAvailable);
            }
            finally
            {
                service.OnRuntimeStatusChanged -= events.Add;
                SetClient(service, oldClient);
                service.RefreshRuntimeStatus(force: true);
            }
        }

        [Test]
        public void SteamIntegrationService_ApplyConfig_UpdatesServiceFlags()
        {
            SteamIntegrationService service = SteamIntegrationService.Instance;
            Assert.NotNull(service);

            bool oldEnableSteam = service.enableSteam;
            bool oldLogWhenUnavailable = service.logWhenUnavailable;
            uint oldAppId = service.appId;
            bool oldRequireRealBackend = service.requireRealBackend;
            bool oldStrictAppIdValidation = service.strictAppIdValidation;
            bool oldRequireCloudWhenEnabled = service.requireCloudWhenSteamEnabled;
            bool oldReportRuntimeDiagnostics = service.reportRuntimeDiagnostics;
            bool oldPreferReflectionBackend = service.preferReflectionBackend;

            SteamIntegrationConfig config = ScriptableObject.CreateInstance<SteamIntegrationConfig>();
            config.enableSteam = false;
            config.logWhenUnavailable = false;
            config.appId = 123456u;
            config.requireRealBackend = true;
            config.strictAppIdValidation = false;
            config.requireCloudWhenSteamEnabled = true;
            config.reportRuntimeDiagnostics = false;
            config.preferReflectionBackend = false;

            try
            {
                service.ApplyConfig(config, reinitializeClient: false);

                Assert.IsFalse(service.enableSteam);
                Assert.IsFalse(service.logWhenUnavailable);
                Assert.AreEqual(123456u, service.appId);
                Assert.IsTrue(service.requireRealBackend);
                Assert.IsFalse(service.strictAppIdValidation);
                Assert.IsTrue(service.requireCloudWhenSteamEnabled);
                Assert.IsFalse(service.reportRuntimeDiagnostics);
                Assert.IsFalse(service.preferReflectionBackend);

                SteamRuntimeStatus status = service.GetRuntimeStatus();
                Assert.IsFalse(status.steamEnabledByConfig, "Runtime status should reflect applied config.");
            }
            finally
            {
                config.enableSteam = oldEnableSteam;
                config.logWhenUnavailable = oldLogWhenUnavailable;
                config.appId = oldAppId;
                config.requireRealBackend = oldRequireRealBackend;
                config.strictAppIdValidation = oldStrictAppIdValidation;
                config.requireCloudWhenSteamEnabled = oldRequireCloudWhenEnabled;
                config.reportRuntimeDiagnostics = oldReportRuntimeDiagnostics;
                config.preferReflectionBackend = oldPreferReflectionBackend;
                service.ApplyConfig(config, reinitializeClient: false);
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void SteamIntegrationService_ReflectionFallbackFactory_WhenRealBackendRequired_ReportsValid()
        {
            SteamIntegrationService service = SteamIntegrationService.Instance;
            Assert.NotNull(service);

            bool oldEnableSteam = service.enableSteam;
            bool oldRequireRealBackend = service.requireRealBackend;
            bool oldStrictAppIdValidation = service.strictAppIdValidation;
            bool oldPreferReflectionBackend = service.preferReflectionBackend;
            uint oldAppId = service.appId;
            ISteamClient oldClient = GetClient(service);

            FieldInfo reflectionFactoryField = typeof(SteamIntegrationService).GetField(
                "reflectionClientFactoryOverride",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(reflectionFactoryField);
            object oldFactory = reflectionFactoryField.GetValue(null);

            try
            {
                service.enableSteam = true;
                service.requireRealBackend = true;
                service.strictAppIdValidation = false;
                service.preferReflectionBackend = true;
                service.appId = oldAppId == 0u ? 480u : oldAppId;

                reflectionFactoryField.SetValue(
                    null,
                    new System.Func<uint, bool, bool, ISteamClient>((_, __, ___) => new FakeSteamClient(true, true)));

                service.InitializeClient();
                service.RefreshRuntimeStatus(force: true);
                SteamRuntimeStatus status = service.GetRuntimeStatus();

                Assert.IsTrue(status.realBackendRequired);
                Assert.IsTrue(status.realBackendReady);
                Assert.IsFalse(status.stubMode);
                Assert.IsTrue(status.runtimeValid);
            }
            finally
            {
                service.enableSteam = oldEnableSteam;
                service.requireRealBackend = oldRequireRealBackend;
                service.strictAppIdValidation = oldStrictAppIdValidation;
                service.preferReflectionBackend = oldPreferReflectionBackend;
                service.appId = oldAppId;
                reflectionFactoryField.SetValue(null, oldFactory);
                SetClient(service, oldClient);
                service.InitializeClient();
                service.RefreshRuntimeStatus(force: true);
            }
        }

        [Test]
        public void SteamIntegrationService_RuntimeStatus_RequireRealBackend_WithStub_IsInvalid()
        {
            SteamIntegrationService service = SteamIntegrationService.Instance;
            Assert.NotNull(service);

            bool oldEnableSteam = service.enableSteam;
            bool oldRequireRealBackend = service.requireRealBackend;
            bool oldStrictAppIdValidation = service.strictAppIdValidation;
            uint oldAppId = service.appId;

            try
            {
                service.enableSteam = true;
                service.requireRealBackend = true;
                service.strictAppIdValidation = true;
                service.appId = oldAppId == 0u ? 480u : oldAppId;

                service.InitializeClient();
                service.RefreshRuntimeStatus(force: true);
                SteamRuntimeStatus status = service.GetRuntimeStatus();

                Assert.IsTrue(status.realBackendRequired);
                Assert.IsFalse(status.realBackendReady);
                Assert.IsFalse(status.runtimeValid);
                Assert.IsTrue(
                    status.validationMessage.IndexOf("Real backend required", System.StringComparison.OrdinalIgnoreCase) >= 0,
                    "Validation message should explain stub/backend mismatch.");
            }
            finally
            {
                service.enableSteam = oldEnableSteam;
                service.requireRealBackend = oldRequireRealBackend;
                service.strictAppIdValidation = oldStrictAppIdValidation;
                service.appId = oldAppId;
                service.InitializeClient();
                service.RefreshRuntimeStatus(force: true);
            }
        }

        [Test]
        public void SteamIntegrationService_RuntimeStatus_RequireRealBackend_WithRealClient_IsValid()
        {
            SteamIntegrationService service = SteamIntegrationService.Instance;
            Assert.NotNull(service);

            bool oldEnableSteam = service.enableSteam;
            bool oldRequireRealBackend = service.requireRealBackend;
            bool oldStrictAppIdValidation = service.strictAppIdValidation;
            uint oldAppId = service.appId;
            ISteamClient oldClient = GetClient(service);

            try
            {
                service.enableSteam = true;
                service.requireRealBackend = true;
                service.strictAppIdValidation = false;
                service.appId = oldAppId == 0u ? 480u : oldAppId;

                SetClient(service, new FakeSteamClient(initialized: true, cloudAvailable: true));
                service.RefreshRuntimeStatus(force: true);
                SteamRuntimeStatus status = service.GetRuntimeStatus();

                Assert.IsTrue(status.realBackendRequired);
                Assert.IsTrue(status.realBackendReady);
                Assert.IsFalse(status.stubMode);
                Assert.IsTrue(status.runtimeValid);
            }
            finally
            {
                service.enableSteam = oldEnableSteam;
                service.requireRealBackend = oldRequireRealBackend;
                service.strictAppIdValidation = oldStrictAppIdValidation;
                service.appId = oldAppId;
                SetClient(service, oldClient);
                service.RefreshRuntimeStatus(force: true);
            }
        }

        [Test]
        public void SteamIntegrationService_RuntimeStatus_RequireCloud_WithUnavailableCloud_IsInvalid()
        {
            SteamIntegrationService service = SteamIntegrationService.Instance;
            Assert.NotNull(service);

            bool oldEnableSteam = service.enableSteam;
            bool oldRequireCloudWhenSteamEnabled = service.requireCloudWhenSteamEnabled;
            bool oldStrictAppIdValidation = service.strictAppIdValidation;
            uint oldAppId = service.appId;
            ISteamClient oldClient = GetClient(service);

            try
            {
                service.enableSteam = true;
                service.requireCloudWhenSteamEnabled = true;
                service.strictAppIdValidation = false;
                service.appId = oldAppId == 0u ? 480u : oldAppId;

                SetClient(service, new FakeSteamClient(initialized: true, cloudAvailable: false));
                service.RefreshRuntimeStatus(force: true);
                SteamRuntimeStatus status = service.GetRuntimeStatus();

                Assert.IsFalse(status.cloudAvailable);
                Assert.IsFalse(status.runtimeValid);
                Assert.IsTrue(
                    status.validationMessage.IndexOf("Cloud save required", System.StringComparison.OrdinalIgnoreCase) >= 0,
                    "Validation message should explain missing cloud capability.");
            }
            finally
            {
                service.enableSteam = oldEnableSteam;
                service.requireCloudWhenSteamEnabled = oldRequireCloudWhenSteamEnabled;
                service.strictAppIdValidation = oldStrictAppIdValidation;
                service.appId = oldAppId;
                SetClient(service, oldClient);
                service.RefreshRuntimeStatus(force: true);
            }
        }

        private static ISteamClient GetClient(SteamIntegrationService service)
        {
            FieldInfo field = typeof(SteamIntegrationService).GetField("client", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            return field.GetValue(service) as ISteamClient;
        }

        private static void SetClient(SteamIntegrationService service, ISteamClient client)
        {
            FieldInfo field = typeof(SteamIntegrationService).GetField("client", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            field.SetValue(service, client);
        }

        private class FakeSteamClient : ISteamClient
        {
            public bool IsInitialized { get; }
            public bool IsCloudAvailable { get; }

            public FakeSteamClient(bool initialized, bool cloudAvailable)
            {
                IsInitialized = initialized;
                IsCloudAvailable = cloudAvailable;
            }

            public void Initialize() { }
            public void RunCallbacks() { }
            public void Shutdown() { }
            public void UnlockAchievement(string achievementId) { }
            public void SetStat(string statId, int value) { }
            public void IncrementStat(string statId, int amount) { }
            public void StoreStats() { }
            public bool CloudFileExists(string fileName) { return false; }
            public byte[] ReadCloudFile(string fileName) { return null; }
            public bool WriteCloudFile(string fileName, byte[] data) { return false; }
            public long GetCloudFileTimestamp(string fileName) { return 0L; }
        }
    }
}
