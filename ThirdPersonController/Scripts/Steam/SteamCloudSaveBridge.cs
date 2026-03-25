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
        public bool applySettingsAfterPull = true;
        public string saveFileName = "savegame.dat";
        public string settingsFileName = "settings.dat";
        public float uploadCooldown = 1.5f;
        public bool logDecisions = true;

        [Header("Debug (Runtime)")]
        [SerializeField] private int debugLastPullDownloadedCount;
        [SerializeField] private int debugLastPushUploadedCount;
        [SerializeField] private string debugLastSyncOperation = "";

        private SteamIntegrationService steam;
        private SaveManager saveManager;
        private float nextUploadTime;

        public int LastPullDownloadedCount => debugLastPullDownloadedCount;
        public int LastPushUploadedCount => debugLastPushUploadedCount;
        public string LastSyncOperation => debugLastSyncOperation;

        private void Awake()
        {
            steam = SteamIntegrationService.Instance;
            saveManager = SaveManager.Instance;
        }

        public void ApplyConfig(SteamIntegrationConfig config)
        {
            if (config == null)
            {
                return;
            }

            enableCloudSaves = config.enableCloudSaves;
            pullOnStart = config.pullCloudOnStart;
            priority = config.cloudPriority;
            onlyIfLocalMissing = config.cloudOnlyIfLocalMissing;
            uploadOnSave = config.uploadCloudOnSave;
            uploadSettings = config.uploadSettings;
            applySettingsAfterPull = config.applySettingsAfterPull;
            uploadCooldown = Mathf.Max(0.1f, config.cloudUploadCooldown);
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
            debugLastPullDownloadedCount = 0;
            debugLastSyncOperation = "PullSkipped";

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

            debugLastSyncOperation = "Pull";
            ResolveSync(saveFileName, saveManager.SaveFilePath, countPullDownloads: true);
            bool pulledSettings = false;
            if (uploadSettings && !string.IsNullOrEmpty(saveManager.SettingsFilePath))
            {
                ResolveSync(settingsFileName, saveManager.SettingsFilePath, countPullDownloads: true);
                pulledSettings = true;
            }

            if (pulledSettings && applySettingsAfterPull)
            {
                saveManager.LoadSettings();
            }
        }

        public void PushToCloud()
        {
            debugLastPushUploadedCount = 0;
            debugLastSyncOperation = "PushSkipped";

            if (!enableCloudSaves || steam == null || !steam.IsCloudAvailable)
            {
                return;
            }

            if (saveManager == null)
            {
                return;
            }

            debugLastSyncOperation = "Push";
            if (UploadFileIfExists(saveManager.SaveFilePath, saveFileName))
            {
                debugLastPushUploadedCount++;
            }

            if (uploadSettings && !string.IsNullOrEmpty(saveManager.SettingsFilePath))
            {
                if (UploadFileIfExists(saveManager.SettingsFilePath, settingsFileName))
                {
                    debugLastPushUploadedCount++;
                }
            }
        }

        private void ResolveSync(string cloudFileName, string localPath, bool countPullDownloads)
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
                if (DownloadFileIfExists(cloudFileName, localPath) && countPullDownloads)
                {
                    debugLastPullDownloadedCount++;
                }
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
                    if (DownloadFileIfExists(cloudFileName, localPath) && countPullDownloads)
                    {
                        debugLastPullDownloadedCount++;
                    }
                    break;
                case CloudSavePriority.LocalPreferred:
                    LogDecision($"LocalPreferred -> Cloud ({cloudFileName})", localPath);
                    UploadFileIfExists(localPath, cloudFileName);
                    break;
                default:
                    ResolveNewest(cloudFileName, localPath, countPullDownloads);
                    break;
            }
        }

        private void ResolveNewest(string cloudFileName, string localPath, bool countPullDownloads)
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
                if (DownloadFileIfExists(cloudFileName, localPath) && countPullDownloads)
                {
                    debugLastPullDownloadedCount++;
                }
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

        private bool UploadFileIfExists(string path, string cloudFileName)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(cloudFileName))
            {
                return false;
            }

            if (!File.Exists(path))
            {
                return false;
            }

            byte[] data = File.ReadAllBytes(path);
            if (data == null || data.Length == 0)
            {
                return false;
            }

            return steam.WriteCloudFile(cloudFileName, data);
        }

        private bool DownloadFileIfExists(string cloudFileName, string localPath)
        {
            if (string.IsNullOrEmpty(cloudFileName) || string.IsNullOrEmpty(localPath))
            {
                return false;
            }

            if (!steam.CloudFileExists(cloudFileName))
            {
                return false;
            }

            byte[] data = steam.ReadCloudFile(cloudFileName);
            if (data == null || data.Length == 0)
            {
                return false;
            }

            string directory = Path.GetDirectoryName(localPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(localPath, data);
            return true;
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
