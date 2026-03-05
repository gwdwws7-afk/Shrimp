using System;
using UnityEngine;
#if STEAMWORKS
using Steamworks;
#endif

namespace ThirdPersonController
{
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

        private ISteamClient client = new NullSteamClient(false);

        public bool IsInitialized => client != null && client.IsInitialized;
        public bool IsCloudAvailable => client != null && client.IsCloudAvailable;

        protected override void OnAwake()
        {
            InitializeClient();
        }

        private void Update()
        {
            client?.RunCallbacks();
        }

        protected override void OnDestroy()
        {
            client?.Shutdown();
            base.OnDestroy();
        }

        public void InitializeClient()
        {
            client?.Shutdown();
#if STEAMWORKS
            client = new SteamworksClient(appId, enableSteam, logWhenUnavailable);
#else
            client = new NullSteamClient(logWhenUnavailable);
#endif
            client.Initialize();
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
                Debug.Log("[Steam] Steamworks not enabled. Running in stub mode.");
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

#if STEAMWORKS
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
