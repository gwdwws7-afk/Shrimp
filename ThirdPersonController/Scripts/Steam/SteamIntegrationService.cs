using System;
using UnityEngine;
#if STEAMWORKS && STEAMWORKS_NET
using Steamworks;
#endif

namespace ThirdPersonController
{
    [Serializable]
    public struct SteamRuntimeStatus
    {
        public bool steamEnabledByConfig;
        public bool clientInitialized;
        public bool cloudAvailable;
        public bool stubMode;
        public bool runtimeValid;
        public bool realBackendRequired;
        public bool realBackendReady;
        public bool appIdValid;
        public string backend;
        public string validationMessage;
    }

    public interface ISteamClient
    {
        bool IsInitialized { get; }
        bool IsCloudAvailable { get; }
        void Initialize();
        void RunCallbacks();
        void Shutdown();
        void UnlockAchievement(string achievementId);
        void SetStat(string statId, int value);
        void IncrementStat(string statId, int amount);
        void StoreStats();
        bool CloudFileExists(string fileName);
        byte[] ReadCloudFile(string fileName);
        bool WriteCloudFile(string fileName, byte[] data);
        long GetCloudFileTimestamp(string fileName);
    }

    public class SteamIntegrationService : Singleton<SteamIntegrationService>
    {
        [Header("Steam")]
        public bool enableSteam = true;
        public bool logWhenUnavailable = true;
        public uint appId = 480;
        public bool requireRealBackend = false;
        public bool strictAppIdValidation = true;
        public bool requireCloudWhenSteamEnabled = false;
        public bool reportRuntimeDiagnostics = true;

        private ISteamClient client = new NullSteamClient(false);
        private SteamRuntimeStatus lastRuntimeStatus;
        private bool runtimeStatusInitialized;
        [SerializeField] private string debugRuntimeValidationMessage = string.Empty;
        [SerializeField] private int debugRuntimeValidationFailureCount = 0;

        public string LastRuntimeValidationMessage => debugRuntimeValidationMessage;
        public int RuntimeValidationFailureCount => debugRuntimeValidationFailureCount;

        public bool IsInitialized => client != null && client.IsInitialized;
        public bool IsCloudAvailable => client != null && client.IsCloudAvailable;
        public bool IsStubMode => client is NullSteamClient;
        public bool IsRuntimeValid => runtimeStatusInitialized ? lastRuntimeStatus.runtimeValid : BuildRuntimeStatus().runtimeValid;
        public event Action<SteamRuntimeStatus> OnRuntimeStatusChanged;

        protected override void OnAwake()
        {
            InitializeClient();
        }

        private void Update()
        {
            client?.RunCallbacks();
            RefreshRuntimeStatus();
        }

        protected override void OnDestroy()
        {
            client?.Shutdown();
            base.OnDestroy();
        }

        public void InitializeClient()
        {
            client?.Shutdown();
#if STEAMWORKS && STEAMWORKS_NET
            client = new SteamworksClient(appId, enableSteam, logWhenUnavailable);
#else
            client = new NullSteamClient(logWhenUnavailable);
#endif
            client.Initialize();
            RefreshRuntimeStatus(force: true);
        }

        public void ApplyConfig(SteamIntegrationConfig config, bool reinitializeClient = true)
        {
            if (config == null)
            {
                return;
            }

            bool changed = enableSteam != config.enableSteam
                || logWhenUnavailable != config.logWhenUnavailable
                || appId != config.appId
                || requireRealBackend != config.requireRealBackend
                || strictAppIdValidation != config.strictAppIdValidation
                || requireCloudWhenSteamEnabled != config.requireCloudWhenSteamEnabled
                || reportRuntimeDiagnostics != config.reportRuntimeDiagnostics;

            enableSteam = config.enableSteam;
            logWhenUnavailable = config.logWhenUnavailable;
            appId = config.appId;
            requireRealBackend = config.requireRealBackend;
            strictAppIdValidation = config.strictAppIdValidation;
            requireCloudWhenSteamEnabled = config.requireCloudWhenSteamEnabled;
            reportRuntimeDiagnostics = config.reportRuntimeDiagnostics;

            if (changed && reinitializeClient)
            {
                InitializeClient();
                return;
            }

            RefreshRuntimeStatus(force: true);
        }

        public void UnlockAchievement(string achievementId)
        {
            client?.UnlockAchievement(achievementId);
        }

        public void SetStat(string statId, int value)
        {
            client?.SetStat(statId, value);
        }

        public void IncrementStat(string statId, int amount)
        {
            client?.IncrementStat(statId, amount);
        }

        public void StoreStats()
        {
            client?.StoreStats();
        }

        public bool CloudFileExists(string fileName)
        {
            return client != null && client.CloudFileExists(fileName);
        }

        public byte[] ReadCloudFile(string fileName)
        {
            return client?.ReadCloudFile(fileName);
        }

        public bool WriteCloudFile(string fileName, byte[] data)
        {
            return client != null && client.WriteCloudFile(fileName, data);
        }

        public long GetCloudFileTimestamp(string fileName)
        {
            return client != null ? client.GetCloudFileTimestamp(fileName) : 0L;
        }

        public SteamRuntimeStatus GetRuntimeStatus()
        {
            return BuildRuntimeStatus();
        }

        public void RefreshRuntimeStatus(bool force = false)
        {
            SteamRuntimeStatus status = BuildRuntimeStatus();
            bool messageChanged = !runtimeStatusInitialized
                || !string.Equals(lastRuntimeStatus.validationMessage, status.validationMessage, StringComparison.Ordinal);
            if (!force && runtimeStatusInitialized && AreSameStatus(lastRuntimeStatus, status))
            {
                return;
            }

            if (!status.runtimeValid && messageChanged)
            {
                debugRuntimeValidationFailureCount++;
                if (reportRuntimeDiagnostics)
                {
                    Debug.LogWarning($"[Steam] Runtime validation failed: {status.validationMessage}");
                }
            }

            debugRuntimeValidationMessage = status.validationMessage ?? string.Empty;
            lastRuntimeStatus = status;
            runtimeStatusInitialized = true;
            OnRuntimeStatusChanged?.Invoke(status);
        }

        private SteamRuntimeStatus BuildRuntimeStatus()
        {
            SteamRuntimeStatus status = new SteamRuntimeStatus
            {
                steamEnabledByConfig = enableSteam,
                clientInitialized = IsInitialized,
                cloudAvailable = IsCloudAvailable,
                stubMode = IsStubMode,
                runtimeValid = true,
                realBackendRequired = requireRealBackend,
                realBackendReady = !IsStubMode,
                appIdValid = !strictAppIdValidation || appId != 0u,
                backend = ResolveBackendName(client)
            };

            ValidateRuntimeStatus(ref status);
            return status;
        }

        private void ValidateRuntimeStatus(ref SteamRuntimeStatus status)
        {
            if (!status.steamEnabledByConfig)
            {
                status.runtimeValid = true;
                status.validationMessage = "Steam disabled by config.";
                return;
            }

            if (!status.appIdValid)
            {
                status.runtimeValid = false;
                status.validationMessage = "Invalid Steam AppID (0).";
                return;
            }

            if (status.realBackendRequired && !status.realBackendReady)
            {
                status.runtimeValid = false;
                status.validationMessage = "Real backend required but runtime is using stub backend.";
                return;
            }

            if (requireCloudWhenSteamEnabled && !status.cloudAvailable)
            {
                status.runtimeValid = false;
                status.validationMessage = "Cloud save required but unavailable in current runtime.";
                return;
            }

            if (!status.stubMode && !status.clientInitialized)
            {
                status.runtimeValid = false;
                status.validationMessage = "Steam backend selected but client is not initialized.";
                return;
            }

            status.runtimeValid = true;
            status.validationMessage = status.stubMode
                ? "Running in stub compatibility mode."
                : "Steam runtime ready.";
        }

        private static bool AreSameStatus(SteamRuntimeStatus a, SteamRuntimeStatus b)
        {
            return a.steamEnabledByConfig == b.steamEnabledByConfig
                && a.clientInitialized == b.clientInitialized
                && a.cloudAvailable == b.cloudAvailable
                && a.stubMode == b.stubMode
                && a.runtimeValid == b.runtimeValid
                && a.realBackendRequired == b.realBackendRequired
                && a.realBackendReady == b.realBackendReady
                && a.appIdValid == b.appIdValid
                && string.Equals(a.backend, b.backend, StringComparison.Ordinal)
                && string.Equals(a.validationMessage, b.validationMessage, StringComparison.Ordinal);
        }

        private static string ResolveBackendName(ISteamClient steamClient)
        {
            if (steamClient == null)
            {
                return "None";
            }

            Type type = steamClient.GetType();
            if (type == null || string.IsNullOrEmpty(type.Name))
            {
                return "Unknown";
            }

            if (type == typeof(NullSteamClient))
            {
                return "Stub";
            }

            return type.Name;
        }
    }

    internal class NullSteamClient : ISteamClient
    {
        private readonly bool logWhenUnavailable;
        private bool logged;

        public bool IsInitialized => false;
        public bool IsCloudAvailable => false;

        public NullSteamClient(bool logWhenUnavailable)
        {
            this.logWhenUnavailable = logWhenUnavailable;
        }

        public void Initialize()
        {
            if (logWhenUnavailable && !logged)
            {
                logged = true;
                Debug.Log("[Steam] Steamworks unavailable. Using local compatibility backend.");
            }
        }

        public void RunCallbacks() { }

        public void Shutdown() { }

        public void UnlockAchievement(string achievementId) { }

        public void SetStat(string statId, int value) { }

        public void IncrementStat(string statId, int amount) { }

        public void StoreStats() { }

        public bool CloudFileExists(string fileName) => false;

        public byte[] ReadCloudFile(string fileName) => null;

        public bool WriteCloudFile(string fileName, byte[] data) => false;

        public long GetCloudFileTimestamp(string fileName) => 0L;
    }

#if STEAMWORKS && STEAMWORKS_NET
    internal class SteamworksClient : ISteamClient
    {
        private readonly uint appId;
        private readonly bool enableSteam;
        private readonly bool logWhenUnavailable;
        private bool initialized;

        public bool IsInitialized => initialized;
        public bool IsCloudAvailable => initialized
            && SteamRemoteStorage.IsCloudEnabledForAccount()
            && SteamRemoteStorage.IsCloudEnabledForApp();

        public SteamworksClient(uint appId, bool enableSteam, bool logWhenUnavailable)
        {
            this.appId = appId;
            this.enableSteam = enableSteam;
            this.logWhenUnavailable = logWhenUnavailable;
        }

        public void Initialize()
        {
            if (!enableSteam)
            {
                return;
            }

            try
            {
                if (!SteamAPI.Init())
                {
                    if (logWhenUnavailable)
                    {
                        Debug.LogWarning("[Steam] SteamAPI.Init failed.");
                    }
                    initialized = false;
                    return;
                }

                initialized = true;
                SteamUserStats.RequestCurrentStats();
            }
            catch (System.Exception ex)
            {
                if (logWhenUnavailable)
                {
                    Debug.LogWarning($"[Steam] Init exception: {ex.Message}");
                }
                initialized = false;
            }
        }

        public void RunCallbacks()
        {
            if (!initialized)
            {
                return;
            }

            SteamAPI.RunCallbacks();
        }

        public void Shutdown()
        {
            if (!initialized)
            {
                return;
            }

            SteamAPI.Shutdown();
            initialized = false;
        }

        public void UnlockAchievement(string achievementId)
        {
            if (!initialized || string.IsNullOrEmpty(achievementId))
            {
                return;
            }

            SteamUserStats.SetAchievement(achievementId);
            SteamUserStats.StoreStats();
        }

        public void SetStat(string statId, int value)
        {
            if (!initialized || string.IsNullOrEmpty(statId))
            {
                return;
            }

            SteamUserStats.SetStat(statId, value);
        }

        public void IncrementStat(string statId, int amount)
        {
            if (!initialized || string.IsNullOrEmpty(statId))
            {
                return;
            }

            if (SteamUserStats.GetStat(statId, out int current))
            {
                SteamUserStats.SetStat(statId, current + amount);
            }
        }

        public void StoreStats()
        {
            if (!initialized)
            {
                return;
            }

            SteamUserStats.StoreStats();
        }

        public bool CloudFileExists(string fileName)
        {
            if (!initialized || string.IsNullOrEmpty(fileName))
            {
                return false;
            }

            return SteamRemoteStorage.FileExists(fileName);
        }

        public byte[] ReadCloudFile(string fileName)
        {
            if (!initialized || string.IsNullOrEmpty(fileName))
            {
                return null;
            }

            if (!SteamRemoteStorage.FileExists(fileName))
            {
                return null;
            }

            int size = SteamRemoteStorage.GetFileSize(fileName);
            if (size <= 0)
            {
                return null;
            }

            byte[] buffer = new byte[size];
            int read = SteamRemoteStorage.FileRead(fileName, buffer, size);
            if (read <= 0)
            {
                return null;
            }

            if (read != size)
            {
                Array.Resize(ref buffer, read);
            }

            return buffer;
        }

        public bool WriteCloudFile(string fileName, byte[] data)
        {
            if (!initialized || string.IsNullOrEmpty(fileName) || data == null || data.Length == 0)
            {
                return false;
            }

            return SteamRemoteStorage.FileWrite(fileName, data, data.Length);
        }

        public long GetCloudFileTimestamp(string fileName)
        {
            if (!initialized || string.IsNullOrEmpty(fileName))
            {
                return 0L;
            }

            return SteamRemoteStorage.GetFileTimestamp(fileName);
        }
    }
#endif
}
