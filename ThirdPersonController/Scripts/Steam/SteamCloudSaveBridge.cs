using System.Collections;
using System.IO;
using UnityEngine;

namespace ThirdPersonController
{
    public class SteamCloudSaveBridge : MonoBehaviour
    {
        public enum CloudSavePriority
        {
            Newest,
            CloudPreferred,
            LocalPreferred
        }

        [Header("Cloud Save")]
        public bool enableCloudSaves = true;
        public bool pullOnStart = true;
        public CloudSavePriority priority = CloudSavePriority.LocalPreferred;
        public bool onlyIfLocalMissing = true;
        public bool uploadOnSave = true;
        public bool uploadSettings = false;
        public string saveFileName = "savegame.dat";
        public string settingsFileName = "settings.dat";
        public float uploadCooldown = 1.5f;
        public bool logDecisions = true;

        private SteamIntegrationService steam;
        private SaveManager saveManager;
        private float nextUploadTime;

        private void Awake()
        {
            steam = SteamIntegrationService.Instance;
            saveManager = SaveManager.Instance;
        }

        private void Start()
        {
            if (pullOnStart)
            {
                StartCoroutine(WaitForSteamAndPull());
            }
        }

        private IEnumerator WaitForSteamAndPull()
        {
            float timeout = 3f;
            float timer = 0f;
            while (timer < timeout)
            {
                if (steam != null && steam.IsInitialized)
                {
                    break;
                }

                timer += Time.unscaledDeltaTime;
                yield return null;
            }

            TryPullFromCloud();
        }

        private void OnEnable()
        {
            if (saveManager != null)
            {
                saveManager.OnSaveCompleted += HandleSaveCompleted;
            }
        }

        private void OnDisable()
        {
            if (saveManager != null)
            {
                saveManager.OnSaveCompleted -= HandleSaveCompleted;
            }
        }

        private void HandleSaveCompleted()
        {
            if (!uploadOnSave)
            {
                return;
            }

            if (Time.time < nextUploadTime)
            {
                return;
            }

            nextUploadTime = Time.time + Mathf.Max(0.1f, uploadCooldown);
            PushToCloud();
        }

        public void TryPullFromCloud()
        {
            if (!enableCloudSaves || steam == null || !steam.IsCloudAvailable)
            {
                return;
            }

            if (saveManager == null)
            {
                return;
            }

            if (onlyIfLocalMissing && File.Exists(saveManager.SaveFilePath))
            {
                return;
            }

            ResolveSync(saveFileName, saveManager.SaveFilePath);
            if (uploadSettings && !string.IsNullOrEmpty(saveManager.SettingsFilePath))
            {
                ResolveSync(settingsFileName, saveManager.SettingsFilePath);
            }
        }

        public void PushToCloud()
        {
            if (!enableCloudSaves || steam == null || !steam.IsCloudAvailable)
            {
                return;
            }

            if (saveManager == null)
            {
                return;
            }

            UploadFileIfExists(saveManager.SaveFilePath, saveFileName);
            if (uploadSettings && !string.IsNullOrEmpty(saveManager.SettingsFilePath))
            {
                UploadFileIfExists(saveManager.SettingsFilePath, settingsFileName);
            }
        }

        private void ResolveSync(string cloudFileName, string localPath)
        {
            if (string.IsNullOrEmpty(cloudFileName) || string.IsNullOrEmpty(localPath))
            {
                return;
            }

            bool localExists = File.Exists(localPath);
            bool cloudExists = steam.CloudFileExists(cloudFileName);

            if (!localExists && !cloudExists)
            {
                return;
            }

            if (!localExists && cloudExists)
            {
                LogDecision($"Cloud -> Local ({cloudFileName})", localPath);
                DownloadFileIfExists(cloudFileName, localPath);
                return;
            }

            if (localExists && !cloudExists)
            {
                LogDecision($"Local -> Cloud ({cloudFileName})", localPath);
                UploadFileIfExists(localPath, cloudFileName);
                return;
            }

            switch (priority)
            {
                case CloudSavePriority.CloudPreferred:
                    LogDecision($"CloudPreferred -> Local ({cloudFileName})", localPath);
                    DownloadFileIfExists(cloudFileName, localPath);
                    break;
                case CloudSavePriority.LocalPreferred:
                    LogDecision($"LocalPreferred -> Cloud ({cloudFileName})", localPath);
                    UploadFileIfExists(localPath, cloudFileName);
                    break;
                default:
                    ResolveNewest(cloudFileName, localPath);
                    break;
            }
        }

        private void ResolveNewest(string cloudFileName, string localPath)
        {
            long cloudTimestamp = steam.GetCloudFileTimestamp(cloudFileName);
            long localTimestamp = GetLocalTimestamp(localPath);

            if (cloudTimestamp <= 0 && localTimestamp <= 0)
            {
                return;
            }

            if (cloudTimestamp > localTimestamp)
            {
                LogDecision($"Newest -> Cloud ({cloudFileName})", localPath);
                DownloadFileIfExists(cloudFileName, localPath);
            }
            else if (localTimestamp > cloudTimestamp)
            {
                LogDecision($"Newest -> Local ({cloudFileName})", localPath);
                UploadFileIfExists(localPath, cloudFileName);
            }
        }

        private long GetLocalTimestamp(string localPath)
        {
            if (!File.Exists(localPath))
            {
                return 0L;
            }

            System.DateTime utc = File.GetLastWriteTimeUtc(localPath);
            return new System.DateTimeOffset(utc).ToUnixTimeSeconds();
        }

        private void UploadFileIfExists(string path, string cloudFileName)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(cloudFileName))
            {
                return;
            }

            if (!File.Exists(path))
            {
                return;
            }

            byte[] data = File.ReadAllBytes(path);
            if (data == null || data.Length == 0)
            {
                return;
            }

            steam.WriteCloudFile(cloudFileName, data);
        }

        private void DownloadFileIfExists(string cloudFileName, string localPath)
        {
            if (string.IsNullOrEmpty(cloudFileName) || string.IsNullOrEmpty(localPath))
            {
                return;
            }

            if (!steam.CloudFileExists(cloudFileName))
            {
                return;
            }

            byte[] data = steam.ReadCloudFile(cloudFileName);
            if (data == null || data.Length == 0)
            {
                return;
            }

            string directory = Path.GetDirectoryName(localPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(localPath, data);
        }

        private void LogDecision(string message, string localPath)
        {
            if (!logDecisions)
            {
                return;
            }

            Debug.Log($"[SteamCloud] {message} | {localPath}");
        }
    }
}
