using UnityEngine;

namespace ThirdPersonController
{
    public class SteamStatsTracker : MonoBehaviour
    {
        [Header("Steam")]
        public bool enableStats = true;
        public float flushInterval = 20f;

        private const string StatTotalKills = "stat_total_kills";
        private const string StatBossesDefeated = "stat_bosses_defeated";
        private const string StatPearlsCollected = "stat_pearls_collected";
        private const string StatTotalDamage = "stat_total_damage";
        private const string StatLongestCombo = "stat_longest_combo";
        private const string StatLevelsCompleted = "stat_levels_completed";
        private const string StatTotalPlaytime = "stat_total_playtime";
        private const string StatHeavyKills = "stat_heavy_kills";
        private const string StatSkillKills = "stat_skill_kills";

        private SteamIntegrationService steam;
        private SaveManager saveManager;
        private float nextFlushTime;
        private float sessionStartTime;
        private int longestCombo;

        private void Awake()
        {
            steam = SteamIntegrationService.Instance;
            saveManager = SaveManager.Instance;
            sessionStartTime = Time.time;
        }

        private void OnEnable()
        {
            GameEvents.OnEnemyKilledDetailed += HandleEnemyKilledDetailed;
            GameEvents.OnDamageDealt += HandleDamageDealt;
            GameEvents.OnComboCountChanged += HandleComboChanged;
            GameEvents.OnPearlCollected += HandlePearlCollected;
            GameEvents.OnLevelCompleted += HandleLevelCompleted;

            if (saveManager != null)
            {
                saveManager.OnSaveCompleted += HandleSaveCompleted;
                saveManager.OnLoadCompleted += HandleLoadCompleted;
            }

            SyncFromSave();
        }

        private void OnDisable()
        {
            GameEvents.OnEnemyKilledDetailed -= HandleEnemyKilledDetailed;
            GameEvents.OnDamageDealt -= HandleDamageDealt;
            GameEvents.OnComboCountChanged -= HandleComboChanged;
            GameEvents.OnPearlCollected -= HandlePearlCollected;
            GameEvents.OnLevelCompleted -= HandleLevelCompleted;

            if (saveManager != null)
            {
                saveManager.OnSaveCompleted -= HandleSaveCompleted;
                saveManager.OnLoadCompleted -= HandleLoadCompleted;
            }
        }

        private void Update()
        {
            if (!enableStats)
            {
                return;
            }

            if (flushInterval > 0f && Time.time >= nextFlushTime)
            {
                PushPlaytime();
                steam?.StoreStats();
                nextFlushTime = Time.time + flushInterval;
            }
        }

        private void HandleSaveCompleted()
        {
            SyncFromSave();
            steam?.StoreStats();
        }

        private void HandleLoadCompleted()
        {
            SyncFromSave();
        }

        private void HandleEnemyKilledDetailed(EnemyType type, Vector3 position, int expReward, DamageSourceType sourceType, bool isHeavyAttack)
        {
            if (!enableStats)
            {
                return;
            }

            steam?.IncrementStat(StatTotalKills, 1);

            if (type == EnemyType.Boss)
            {
                steam?.IncrementStat(StatBossesDefeated, 1);
            }

            if (isHeavyAttack)
            {
                steam?.IncrementStat(StatHeavyKills, 1);
            }

            if (sourceType == DamageSourceType.PlayerSkill)
            {
                steam?.IncrementStat(StatSkillKills, 1);
            }
        }

        private void HandleDamageDealt(int damage, Vector3 position, bool isCritical)
        {
            if (!enableStats || damage <= 0)
            {
                return;
            }

            steam?.IncrementStat(StatTotalDamage, damage);
        }

        private void HandleComboChanged(int comboCount)
        {
            if (!enableStats)
            {
                return;
            }

            if (comboCount > longestCombo)
            {
                longestCombo = comboCount;
                steam?.SetStat(StatLongestCombo, comboCount);
            }
        }

        private void HandlePearlCollected(string pearlId)
        {
            if (!enableStats)
            {
                return;
            }

            steam?.IncrementStat(StatPearlsCollected, 1);
        }

        private void HandleLevelCompleted(int levelId)
        {
            if (!enableStats)
            {
                return;
            }

            steam?.IncrementStat(StatLevelsCompleted, 1);
            PushPlaytime();
        }

        private void SyncFromSave()
        {
            if (!enableStats || saveManager == null || saveManager.CurrentData == null)
            {
                return;
            }

            GameData data = saveManager.CurrentData;
            steam?.SetStat(StatTotalKills, Mathf.Max(0, data.totalKills));
            steam?.SetStat(StatBossesDefeated, Mathf.Max(0, data.bossesDefeated));
            steam?.SetStat(StatPearlsCollected, Mathf.Max(0, data.pearlsCollected));
            steam?.SetStat(StatTotalDamage, Mathf.Max(0, data.totalDamage));
            longestCombo = Mathf.Max(0, data.longestCombo);
            steam?.SetStat(StatLongestCombo, longestCombo);
            steam?.SetStat(StatLevelsCompleted, Mathf.Max(0, data.totalLevelsCompleted));
            steam?.SetStat(StatTotalPlaytime, Mathf.Max(0, Mathf.RoundToInt(data.totalPlayTime)));
        }

        private void PushPlaytime()
        {
            if (saveManager == null || saveManager.CurrentData == null)
            {
                return;
            }

            float elapsed = Time.time - sessionStartTime;
            int totalSeconds = Mathf.RoundToInt(saveManager.CurrentData.totalPlayTime + elapsed);
            steam?.SetStat(StatTotalPlaytime, Mathf.Max(0, totalSeconds));
        }
    }
}
