using UnityEngine;

namespace ThirdPersonController
{
    public static class SteamIntegrationBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SteamIntegrationService service = Object.FindObjectOfType<SteamIntegrationService>();
            SteamAchievementTracker tracker = Object.FindObjectOfType<SteamAchievementTracker>();
            SteamStatsTracker statsTracker = Object.FindObjectOfType<SteamStatsTracker>();
            SteamCloudSaveBridge cloudBridge = Object.FindObjectOfType<SteamCloudSaveBridge>();

            if (service == null)
            {
                GameObject root = new GameObject("SteamIntegration");
                service = root.AddComponent<SteamIntegrationService>();
                tracker = root.AddComponent<SteamAchievementTracker>();
                statsTracker = root.AddComponent<SteamStatsTracker>();
                cloudBridge = root.AddComponent<SteamCloudSaveBridge>();
                return;
            }

            if (tracker == null)
            {
                service.gameObject.AddComponent<SteamAchievementTracker>();
            }

            if (statsTracker == null)
            {
                service.gameObject.AddComponent<SteamStatsTracker>();
            }

            if (cloudBridge == null)
            {
                service.gameObject.AddComponent<SteamCloudSaveBridge>();
            }
        }
    }
}
