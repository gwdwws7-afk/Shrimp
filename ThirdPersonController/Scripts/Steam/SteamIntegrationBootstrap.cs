using UnityEngine;

namespace ThirdPersonController
{
    public static class SteamIntegrationBootstrap
    {
        private const string ConfigResourcePath = "Steam/DefaultSteamIntegrationConfig";

        internal static System.Func<SteamIntegrationConfig> ConfigResolverOverride;

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
            }
            else
            {
                if (tracker == null)
                {
                    tracker = service.gameObject.AddComponent<SteamAchievementTracker>();
                }

                if (statsTracker == null)
                {
                    statsTracker = service.gameObject.AddComponent<SteamStatsTracker>();
                }

                if (cloudBridge == null)
                {
                    cloudBridge = service.gameObject.AddComponent<SteamCloudSaveBridge>();
                }
            }

            SteamIntegrationConfig config = ResolveConfig();
            if (config != null)
            {
                service.ApplyConfig(config, reinitializeClient: true);
                tracker?.ApplyConfig(config);
                statsTracker?.ApplyConfig(config);
                cloudBridge?.ApplyConfig(config);
            }
        }

        private static SteamIntegrationConfig ResolveConfig()
        {
            if (ConfigResolverOverride != null)
            {
                return ConfigResolverOverride();
            }

            return Resources.Load<SteamIntegrationConfig>(ConfigResourcePath);
        }
    }
}
