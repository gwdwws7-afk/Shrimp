using System;
using System.Reflection;
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
        public bool preferReflectionBackend = true;

        private ISteamClient client = new NullSteamClient(false);
        private static Func<uint, bool, bool, ISteamClient> reflectionClientFactoryOverride;
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
            ISteamClient resolvedClient = null;
#if STEAMWORKS && STEAMWORKS_NET
            resolvedClient = new SteamworksClient(appId, enableSteam, logWhenUnavailable);
#endif

            if (resolvedClient == null && preferReflectionBackend)
            {
                resolvedClient = CreateReflectionClient();
            }

            client = resolvedClient ?? new NullSteamClient(logWhenUnavailable);
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
                || reportRuntimeDiagnostics != config.reportRuntimeDiagnostics
                || preferReflectionBackend != config.preferReflectionBackend;

            enableSteam = config.enableSteam;
            logWhenUnavailable = config.logWhenUnavailable;
            appId = config.appId;
            requireRealBackend = config.requireRealBackend;
            strictAppIdValidation = config.strictAppIdValidation;
            requireCloudWhenSteamEnabled = config.requireCloudWhenSteamEnabled;
            reportRuntimeDiagnostics = config.reportRuntimeDiagnostics;
            preferReflectionBackend = config.preferReflectionBackend;

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

        private ISteamClient CreateReflectionClient()
        {
            if (reflectionClientFactoryOverride != null)
            {
                return reflectionClientFactoryOverride(appId, enableSteam, logWhenUnavailable);
            }

            return ReflectionSteamClient.TryCreate(appId, enableSteam, logWhenUnavailable);
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

    internal class ReflectionSteamClient : ISteamClient
    {
        private readonly bool enableSteam;
        private readonly bool logWhenUnavailable;
        private readonly MethodInfo steamApiInit;
        private readonly MethodInfo steamApiRunCallbacks;
        private readonly MethodInfo steamApiShutdown;
        private readonly MethodInfo requestCurrentStats;
        private readonly MethodInfo setAchievement;
        private readonly MethodInfo setStat;
        private readonly MethodInfo getStat;
        private readonly MethodInfo storeStats;
        private readonly MethodInfo isCloudEnabledForAccount;
        private readonly MethodInfo isCloudEnabledForApp;
        private readonly MethodInfo cloudFileExists;
        private readonly MethodInfo cloudFileRead;
        private readonly MethodInfo cloudFileWrite;
        private readonly MethodInfo cloudGetFileSize;
        private readonly MethodInfo cloudGetFileTimestamp;
        private bool initialized;

        public bool IsInitialized => initialized;
        public bool IsCloudAvailable => initialized
            && InvokeBool(isCloudEnabledForAccount, false)
            && InvokeBool(isCloudEnabledForApp, false);

        private ReflectionSteamClient(
            bool enableSteam,
            bool logWhenUnavailable,
            MethodInfo steamApiInit,
            MethodInfo steamApiRunCallbacks,
            MethodInfo steamApiShutdown,
            MethodInfo requestCurrentStats,
            MethodInfo setAchievement,
            MethodInfo setStat,
            MethodInfo getStat,
            MethodInfo storeStats,
            MethodInfo isCloudEnabledForAccount,
            MethodInfo isCloudEnabledForApp,
            MethodInfo cloudFileExists,
            MethodInfo cloudFileRead,
            MethodInfo cloudFileWrite,
            MethodInfo cloudGetFileSize,
            MethodInfo cloudGetFileTimestamp)
        {
            this.enableSteam = enableSteam;
            this.logWhenUnavailable = logWhenUnavailable;
            this.steamApiInit = steamApiInit;
            this.steamApiRunCallbacks = steamApiRunCallbacks;
            this.steamApiShutdown = steamApiShutdown;
            this.requestCurrentStats = requestCurrentStats;
            this.setAchievement = setAchievement;
            this.setStat = setStat;
            this.getStat = getStat;
            this.storeStats = storeStats;
            this.isCloudEnabledForAccount = isCloudEnabledForAccount;
            this.isCloudEnabledForApp = isCloudEnabledForApp;
            this.cloudFileExists = cloudFileExists;
            this.cloudFileRead = cloudFileRead;
            this.cloudFileWrite = cloudFileWrite;
            this.cloudGetFileSize = cloudGetFileSize;
            this.cloudGetFileTimestamp = cloudGetFileTimestamp;
        }

        public static ISteamClient TryCreate(uint appId, bool enableSteam, bool logWhenUnavailable)
        {
            Type steamApiType = FindType("Steamworks.SteamAPI");
            Type userStatsType = FindType("Steamworks.SteamUserStats");
            Type remoteStorageType = FindType("Steamworks.SteamRemoteStorage");

            if (steamApiType == null || userStatsType == null || remoteStorageType == null)
            {
                return null;
            }

            MethodInfo init = steamApiType.GetMethod("Init", BindingFlags.Public | BindingFlags.Static);
            MethodInfo runCallbacks = steamApiType.GetMethod("RunCallbacks", BindingFlags.Public | BindingFlags.Static);
            MethodInfo shutdown = steamApiType.GetMethod("Shutdown", BindingFlags.Public | BindingFlags.Static);

            if (init == null || runCallbacks == null || shutdown == null)
            {
                return null;
            }

            MethodInfo requestStats = userStatsType.GetMethod("RequestCurrentStats", BindingFlags.Public | BindingFlags.Static);
            MethodInfo achievement = userStatsType.GetMethod(
                "SetAchievement",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string) },
                null);
            MethodInfo setStat = userStatsType.GetMethod(
                "SetStat",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string), typeof(int) },
                null);
            MethodInfo getStat = userStatsType.GetMethod(
                "GetStat",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string), typeof(int).MakeByRefType() },
                null);
            MethodInfo storeStats = userStatsType.GetMethod("StoreStats", BindingFlags.Public | BindingFlags.Static);

            MethodInfo cloudAccount = remoteStorageType.GetMethod("IsCloudEnabledForAccount", BindingFlags.Public | BindingFlags.Static);
            MethodInfo cloudApp = remoteStorageType.GetMethod("IsCloudEnabledForApp", BindingFlags.Public | BindingFlags.Static);
            MethodInfo fileExists = remoteStorageType.GetMethod(
                "FileExists",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string) },
                null);
            MethodInfo fileRead = remoteStorageType.GetMethod(
                "FileRead",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string), typeof(byte[]), typeof(int) },
                null);
            MethodInfo fileWrite = remoteStorageType.GetMethod(
                "FileWrite",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string), typeof(byte[]), typeof(int) },
                null);
            MethodInfo fileSize = remoteStorageType.GetMethod(
                "GetFileSize",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string) },
                null);
            MethodInfo fileTimestamp = remoteStorageType.GetMethod(
                "GetFileTimestamp",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string) },
                null);

            return new ReflectionSteamClient(
                enableSteam,
                logWhenUnavailable,
                init,
                runCallbacks,
                shutdown,
                requestStats,
                achievement,
                setStat,
                getStat,
                storeStats,
                cloudAccount,
                cloudApp,
                fileExists,
                fileRead,
                fileWrite,
                fileSize,
                fileTimestamp);
        }

        public void Initialize()
        {
            if (!enableSteam)
            {
                initialized = false;
                return;
            }

            bool ok = InvokeBool(steamApiInit, false);
            initialized = ok;
            if (!ok)
            {
                if (logWhenUnavailable)
                {
                    Debug.LogWarning("[Steam] Reflection SteamAPI.Init failed.");
                }
                return;
            }

            try
            {
                requestCurrentStats?.Invoke(null, null);
            }
            catch
            {
            }
        }

        public void RunCallbacks()
        {
            if (!initialized)
            {
                return;
            }

            try
            {
                steamApiRunCallbacks?.Invoke(null, null);
            }
            catch
            {
            }
        }

        public void Shutdown()
        {
            if (!initialized)
            {
                return;
            }

            try
            {
                steamApiShutdown?.Invoke(null, null);
            }
            catch
            {
            }

            initialized = false;
        }

        public void UnlockAchievement(string achievementId)
        {
            if (!initialized || string.IsNullOrWhiteSpace(achievementId) || setAchievement == null)
            {
                return;
            }

            try
            {
                setAchievement.Invoke(null, new object[] { achievementId });
                storeStats?.Invoke(null, null);
            }
            catch
            {
            }
        }

        public void SetStat(string statId, int value)
        {
            if (!initialized || string.IsNullOrWhiteSpace(statId) || setStat == null)
            {
                return;
            }

            try
            {
                setStat.Invoke(null, new object[] { statId, value });
            }
            catch
            {
            }
        }

        public void IncrementStat(string statId, int amount)
        {
            if (!initialized || string.IsNullOrWhiteSpace(statId) || setStat == null)
            {
                return;
            }

            int nextValue = amount;
            if (getStat != null)
            {
                try
                {
                    object[] args = { statId, 0 };
                    bool gotCurrent = ConvertToBool(getStat.Invoke(null, args));
                    if (gotCurrent)
                    {
                        nextValue = Convert.ToInt32(args[1]) + amount;
                    }
                }
                catch
                {
                }
            }

            SetStat(statId, nextValue);
        }

        public void StoreStats()
        {
            if (!initialized || storeStats == null)
            {
                return;
            }

            try
            {
                storeStats.Invoke(null, null);
            }
            catch
            {
            }
        }

        public bool CloudFileExists(string fileName)
        {
            if (!initialized || string.IsNullOrWhiteSpace(fileName) || cloudFileExists == null)
            {
                return false;
            }

            try
            {
                return ConvertToBool(cloudFileExists.Invoke(null, new object[] { fileName }));
            }
            catch
            {
                return false;
            }
        }

        public byte[] ReadCloudFile(string fileName)
        {
            if (!CloudFileExists(fileName) || cloudFileRead == null || cloudGetFileSize == null)
            {
                return null;
            }

            int size;
            try
            {
                size = Convert.ToInt32(cloudGetFileSize.Invoke(null, new object[] { fileName }));
            }
            catch
            {
                return null;
            }

            if (size <= 0)
            {
                return null;
            }

            byte[] buffer = new byte[size];
            try
            {
                object readResult = cloudFileRead.Invoke(null, new object[] { fileName, buffer, size });
                if (readResult is int readBytes)
                {
                    if (readBytes <= 0)
                    {
                        return null;
                    }

                    if (readBytes != size)
                    {
                        Array.Resize(ref buffer, readBytes);
                    }

                    return buffer;
                }

                if (ConvertToBool(readResult))
                {
                    return buffer;
                }
            }
            catch
            {
            }

            return null;
        }

        public bool WriteCloudFile(string fileName, byte[] data)
        {
            if (!initialized || string.IsNullOrWhiteSpace(fileName) || data == null || data.Length == 0 || cloudFileWrite == null)
            {
                return false;
            }

            try
            {
                object result = cloudFileWrite.Invoke(null, new object[] { fileName, data, data.Length });
                return ConvertToBool(result);
            }
            catch
            {
                return false;
            }
        }

        public long GetCloudFileTimestamp(string fileName)
        {
            if (!initialized || string.IsNullOrWhiteSpace(fileName) || cloudGetFileTimestamp == null)
            {
                return 0L;
            }

            try
            {
                object value = cloudGetFileTimestamp.Invoke(null, new object[] { fileName });
                return Convert.ToInt64(value);
            }
            catch
            {
                return 0L;
            }
        }

        private bool InvokeBool(MethodInfo method, bool fallback)
        {
            if (method == null)
            {
                return fallback;
            }

            try
            {
                return ConvertToBool(method.Invoke(null, null));
            }
            catch
            {
                return fallback;
            }
        }

        private static bool ConvertToBool(object value)
        {
            if (value == null)
            {
                return false;
            }

            if (value is bool boolValue)
            {
                return boolValue;
            }

            if (value is int intValue)
            {
                return intValue != 0;
            }

            try
            {
                return Convert.ToBoolean(value);
            }
            catch
            {
                return false;
            }
        }

        private static Type FindType(string fullName)
        {
            Type direct = Type.GetType(fullName, throwOnError: false);
            if (direct != null)
            {
                return direct;
            }

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Assembly assembly = assemblies[i];
                if (assembly == null)
                {
                    continue;
                }

                Type match = assembly.GetType(fullName, throwOnError: false);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }
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
