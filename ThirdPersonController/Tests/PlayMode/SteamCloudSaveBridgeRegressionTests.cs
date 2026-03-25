using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ThirdPersonController.Tests
{
    public class SteamCloudSaveBridgeRegressionTests
    {
        private readonly List<UnityEngine.Object> createdObjects = new List<UnityEngine.Object>();

        private string savePath;
        private string settingsPath;
        private bool hadLocalSave;
        private byte[] oldLocalSaveBytes;
        private bool hadLocalSettings;
        private byte[] oldLocalSettingsBytes;
        private ISteamClient oldClient;
        private LocalizationLanguage oldLanguage;

        [SetUp]
        public void SetUp()
        {
            SaveManager saveManager = SaveManager.Instance;
            Assert.NotNull(saveManager);

            savePath = saveManager.SaveFilePath;
            settingsPath = saveManager.SettingsFilePath;
            hadLocalSave = File.Exists(savePath);
            oldLocalSaveBytes = hadLocalSave ? File.ReadAllBytes(savePath) : null;
            hadLocalSettings = File.Exists(settingsPath);
            oldLocalSettingsBytes = hadLocalSettings ? File.ReadAllBytes(settingsPath) : null;

            oldClient = GetSteamClient();
            oldLanguage = LocalizationService.Instance != null
                ? LocalizationService.Instance.CurrentLanguage
                : LocalizationLanguage.SimplifiedChinese;
        }

        [TearDown]
        public void TearDown()
        {
            RestoreSteamClient();
            RestoreLocalSaveFile();
            RestoreLocalSettingsFile();
            RestoreLanguage();

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
        public void SteamCloudSaveBridge_TryPullFromCloud_DownloadsMissingLocalSave()
        {
            FakeSteamClient fakeClient = new FakeSteamClient();
            byte[] cloudPayload = { 0x11, 0x22, 0x33, 0x44 };
            fakeClient.SetCloudFile("savegame.dat", cloudPayload, timestamp: 1000);
            SetSteamClient(fakeClient);

            if (File.Exists(savePath))
            {
                File.Delete(savePath);
            }

            SteamCloudSaveBridge bridge = CreateBridge();
            bridge.enableCloudSaves = true;
            bridge.pullOnStart = false;
            bridge.onlyIfLocalMissing = true;
            bridge.priority = SteamCloudSaveBridge.CloudSavePriority.CloudPreferred;

            bridge.TryPullFromCloud();

            Assert.IsTrue(File.Exists(savePath), "Cloud pull should create local save file when missing.");
            CollectionAssert.AreEqual(cloudPayload, File.ReadAllBytes(savePath));
            Assert.AreEqual(1, bridge.LastPullDownloadedCount, "Pull debug counter should count downloaded save file.");
            Assert.AreEqual("Pull", bridge.LastSyncOperation);
        }

        [Test]
        public void SteamCloudSaveBridge_PushToCloud_UploadsLocalSaveFile()
        {
            FakeSteamClient fakeClient = new FakeSteamClient();
            SetSteamClient(fakeClient);

            byte[] localPayload = { 0x9A, 0xBC, 0xDE };
            EnsureLocalDirectoryExists();
            File.WriteAllBytes(savePath, localPayload);

            SteamCloudSaveBridge bridge = CreateBridge();
            bridge.enableCloudSaves = true;
            bridge.pullOnStart = false;

            bridge.PushToCloud();

            Assert.IsTrue(fakeClient.CloudFileExists("savegame.dat"), "Cloud save file should be uploaded.");
            CollectionAssert.AreEqual(localPayload, fakeClient.ReadCloudFile("savegame.dat"));
            Assert.AreEqual(1, bridge.LastPushUploadedCount, "Push debug counter should count uploaded save file.");
            Assert.AreEqual("Push", bridge.LastSyncOperation);
        }

        [Test]
        public void SteamCloudSaveBridge_PullSettings_AppliesLocalizationLanguage()
        {
            FakeSteamClient fakeClient = new FakeSteamClient();
            SetSteamClient(fakeClient);

            GameData cloudSettings = new GameData
            {
                localizationLanguage = (int)LocalizationLanguage.English
            };
            byte[] settingsPayload = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(cloudSettings));
            fakeClient.SetCloudFile("settings.dat", settingsPayload, timestamp: 2000);

            if (File.Exists(settingsPath))
            {
                File.Delete(settingsPath);
            }

            LocalizationService service = LocalizationService.Instance;
            Assert.NotNull(service);
            service.SetLanguage(LocalizationLanguage.SimplifiedChinese);

            SteamCloudSaveBridge bridge = CreateBridge();
            bridge.enableCloudSaves = true;
            bridge.pullOnStart = false;
            bridge.onlyIfLocalMissing = false;
            bridge.uploadSettings = true;
            bridge.applySettingsAfterPull = true;
            bridge.priority = SteamCloudSaveBridge.CloudSavePriority.CloudPreferred;

            bridge.TryPullFromCloud();

            Assert.IsTrue(File.Exists(settingsPath), "Cloud settings should be downloaded to local file.");
            Assert.AreEqual(LocalizationLanguage.English, service.CurrentLanguage, "Pulled settings should apply language.");
        }

        [Test]
        public void SteamCloudSaveBridge_NewestPriority_DownloadsCloudWhenCloudTimestampNewer()
        {
            FakeSteamClient fakeClient = new FakeSteamClient();
            SetSteamClient(fakeClient);

            byte[] localPayload = { 0x01, 0x02, 0x03 };
            byte[] cloudPayload = { 0x0A, 0x0B, 0x0C };

            EnsureLocalDirectoryExists();
            File.WriteAllBytes(savePath, localPayload);
            File.SetLastWriteTimeUtc(savePath, DateTime.UtcNow.AddMinutes(-15));
            fakeClient.SetCloudFile("savegame.dat", cloudPayload, timestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            SteamCloudSaveBridge bridge = CreateBridge();
            bridge.enableCloudSaves = true;
            bridge.pullOnStart = false;
            bridge.onlyIfLocalMissing = false;
            bridge.priority = SteamCloudSaveBridge.CloudSavePriority.Newest;

            bridge.TryPullFromCloud();

            CollectionAssert.AreEqual(cloudPayload, File.ReadAllBytes(savePath), "Newest mode should pull cloud file when cloud is newer.");
            Assert.AreEqual(1, bridge.LastPullDownloadedCount, "Newest cloud path should count one pull download.");
        }

        [Test]
        public void SteamCloudSaveBridge_NewestPriority_UploadsLocalWhenLocalTimestampNewer()
        {
            FakeSteamClient fakeClient = new FakeSteamClient();
            SetSteamClient(fakeClient);

            byte[] localPayload = { 0x5A, 0x5B, 0x5C };
            byte[] cloudPayload = { 0x6A, 0x6B, 0x6C };

            EnsureLocalDirectoryExists();
            File.WriteAllBytes(savePath, localPayload);
            File.SetLastWriteTimeUtc(savePath, DateTime.UtcNow.AddMinutes(-1));
            fakeClient.SetCloudFile("savegame.dat", cloudPayload, timestamp: DateTimeOffset.UtcNow.AddMinutes(-30).ToUnixTimeSeconds());

            SteamCloudSaveBridge bridge = CreateBridge();
            bridge.enableCloudSaves = true;
            bridge.pullOnStart = false;
            bridge.onlyIfLocalMissing = false;
            bridge.priority = SteamCloudSaveBridge.CloudSavePriority.Newest;

            bridge.TryPullFromCloud();

            CollectionAssert.AreEqual(localPayload, fakeClient.ReadCloudFile("savegame.dat"), "Newest mode should push local file when local is newer.");
            Assert.AreEqual(0, bridge.LastPullDownloadedCount, "Newest local path should not count cloud download.");
        }

        private SteamCloudSaveBridge CreateBridge()
        {
            GameObject bridgeObject = new GameObject("SteamCloudSaveBridge_Test");
            createdObjects.Add(bridgeObject);
            return bridgeObject.AddComponent<SteamCloudSaveBridge>();
        }

        private ISteamClient GetSteamClient()
        {
            SteamIntegrationService service = SteamIntegrationService.Instance;
            Assert.NotNull(service);

            FieldInfo clientField = typeof(SteamIntegrationService).GetField("client", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(clientField);
            return clientField.GetValue(service) as ISteamClient;
        }

        private void SetSteamClient(ISteamClient client)
        {
            SteamIntegrationService service = SteamIntegrationService.Instance;
            Assert.NotNull(service);

            FieldInfo clientField = typeof(SteamIntegrationService).GetField("client", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(clientField);
            clientField.SetValue(service, client);
        }

        private void RestoreSteamClient()
        {
            if (oldClient == null)
            {
                return;
            }

            SetSteamClient(oldClient);
        }

        private void EnsureLocalDirectoryExists()
        {
            string directory = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        private void EnsureSettingsDirectoryExists()
        {
            string directory = Path.GetDirectoryName(settingsPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        private void RestoreLocalSaveFile()
        {
            if (string.IsNullOrEmpty(savePath))
            {
                return;
            }

            if (hadLocalSave)
            {
                EnsureLocalDirectoryExists();
                File.WriteAllBytes(savePath, oldLocalSaveBytes ?? Array.Empty<byte>());
                return;
            }

            if (File.Exists(savePath))
            {
                File.Delete(savePath);
            }
        }

        private void RestoreLocalSettingsFile()
        {
            if (string.IsNullOrEmpty(settingsPath))
            {
                return;
            }

            if (hadLocalSettings)
            {
                EnsureSettingsDirectoryExists();
                File.WriteAllBytes(settingsPath, oldLocalSettingsBytes ?? Array.Empty<byte>());
                return;
            }

            if (File.Exists(settingsPath))
            {
                File.Delete(settingsPath);
            }
        }

        private void RestoreLanguage()
        {
            LocalizationService service = LocalizationService.Instance;
            if (service != null)
            {
                service.SetLanguage(oldLanguage);
            }
        }

        private class FakeSteamClient : ISteamClient
        {
            private readonly Dictionary<string, byte[]> files = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            private readonly Dictionary<string, long> timestamps = new Dictionary<string, long>(StringComparer.Ordinal);
            private long tick = 1;

            public bool IsInitialized => true;
            public bool IsCloudAvailable => true;

            public void Initialize() { }

            public void RunCallbacks() { }

            public void Shutdown() { }

            public void UnlockAchievement(string achievementId) { }

            public void SetStat(string statId, int value) { }

            public void IncrementStat(string statId, int amount) { }

            public void StoreStats() { }

            public bool CloudFileExists(string fileName)
            {
                return !string.IsNullOrEmpty(fileName) && files.ContainsKey(fileName);
            }

            public byte[] ReadCloudFile(string fileName)
            {
                if (string.IsNullOrEmpty(fileName) || !files.TryGetValue(fileName, out byte[] data))
                {
                    return null;
                }

                byte[] copy = new byte[data.Length];
                Buffer.BlockCopy(data, 0, copy, 0, data.Length);
                return copy;
            }

            public bool WriteCloudFile(string fileName, byte[] data)
            {
                if (string.IsNullOrEmpty(fileName) || data == null || data.Length == 0)
                {
                    return false;
                }

                byte[] copy = new byte[data.Length];
                Buffer.BlockCopy(data, 0, copy, 0, data.Length);
                files[fileName] = copy;
                timestamps[fileName] = tick++;
                return true;
            }

            public long GetCloudFileTimestamp(string fileName)
            {
                if (string.IsNullOrEmpty(fileName) || !timestamps.TryGetValue(fileName, out long timestamp))
                {
                    return 0L;
                }

                return timestamp;
            }

            public void SetCloudFile(string fileName, byte[] data, long timestamp)
            {
                WriteCloudFile(fileName, data);
                timestamps[fileName] = timestamp;
            }
        }
    }
}
