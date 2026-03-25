using UnityEngine;

namespace ThirdPersonController
{
    [CreateAssetMenu(
        fileName = "DefaultSteamIntegrationConfig",
        menuName = "ThirdPersonController/Steam/Steam Integration Config")]
    public class SteamIntegrationConfig : ScriptableObject
    {
        [Header("Service")]
        public bool enableSteam = true;
        public bool logWhenUnavailable = true;
        public uint appId = 480;
        public bool requireRealBackend = false;
        public bool strictAppIdValidation = true;
        public bool requireCloudWhenSteamEnabled = false;
        public bool reportRuntimeDiagnostics = true;

        [Header("Achievement / Stats")]
        public bool enableAchievements = true;
        public bool enableStats = true;
        [Min(1f)] public float statsFlushInterval = 20f;

        [Header("Cloud Save")]
        public bool enableCloudSaves = true;
        public bool pullCloudOnStart = true;
        public bool uploadCloudOnSave = true;
        public bool uploadSettings = false;
        public bool applySettingsAfterPull = true;
        public bool cloudOnlyIfLocalMissing = true;
        [Min(0.1f)] public float cloudUploadCooldown = 1.5f;
        public SteamCloudSaveBridge.CloudSavePriority cloudPriority = SteamCloudSaveBridge.CloudSavePriority.LocalPreferred;
    }
}
