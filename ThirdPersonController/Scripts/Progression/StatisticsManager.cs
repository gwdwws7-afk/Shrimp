using UnityEngine;

namespace ThirdPersonController
{
    public class StatisticsManager : MonoBehaviour
    {
        [Header("Session Statistics")]
        public int sessionKills = 0;
        public int sessionDamageDealt = 0;
        public int sessionHighestCombo = 0;
        public float sessionStartTime = 0f;
        
        [Header("Lifetime Statistics (from Save)")]
        public int totalKills = 0;
        public int totalDamage = 0;
        public int longestCombo = 0;
        public int bossesDefeated = 0;
        public int pearlsCollected = 0;
        public float totalPlayTime = 0f;
        
        private SaveManager saveManager;
        private PlayerCombat playerCombat;
        
        private void Awake()
        {
            saveManager = FindObjectOfType<SaveManager>();
            playerCombat = FindObjectOfType<PlayerCombat>();
            LoadStatistics();
        }
        
        private void OnEnable()
        {
            GameEvents.OnEnemyKilled += HandleEnemyKilled;
            GameEvents.OnComboCountChanged += HandleComboChanged;
            GameEvents.OnDamageDealt += HandleDamageDealt;
            GameEvents.OnPearlCollected += HandlePearlCollected;
            sessionStartTime = Time.time;
        }
        
        private void OnDisable()
        {
            GameEvents.OnEnemyKilled -= HandleEnemyKilled;
            GameEvents.OnComboCountChanged -= HandleComboChanged;
            GameEvents.OnDamageDealt -= HandleDamageDealt;
            GameEvents.OnPearlCollected -= HandlePearlCollected;
        }
        
        private void Update()
        {
            if (saveManager?.CurrentData != null)
            {
                saveManager.CurrentData.totalPlayTime = totalPlayTime + (Time.time - sessionStartTime);
            }
        }
        
        private void LoadStatistics()
        {
            if (saveManager?.CurrentData == null) return;
            
            totalKills = saveManager.CurrentData.totalKills;
            totalDamage = saveManager.CurrentData.totalDamage;
            longestCombo = saveManager.CurrentData.longestCombo;
            bossesDefeated = saveManager.CurrentData.bossesDefeated;
            pearlsCollected = saveManager.CurrentData.pearlsCollected;
            totalPlayTime = saveManager.CurrentData.totalPlayTime;
        }
        
        public void SaveStatistics()
        {
            if (saveManager?.CurrentData == null) return;
            
            saveManager.CurrentData.totalKills = totalKills;
            saveManager.CurrentData.totalDamage = totalDamage;
            saveManager.CurrentData.longestCombo = Mathf.Max(longestCombo, sessionHighestCombo);
            saveManager.CurrentData.bossesDefeated = bossesDefeated;
            saveManager.CurrentData.pearlsCollected = pearlsCollected;
            saveManager.CurrentData.totalPlayTime = totalPlayTime + (Time.time - sessionStartTime);
            
            saveManager.SaveGame();
        }
        
        public void AddKill(EnemyType type)
        {
            sessionKills++;
            totalKills++;
            
            if (type == EnemyType.Boss)
            {
                bossesDefeated++;
            }
            
            TryAutoSave();
        }
        
        public void AddDamage(int damage)
        {
            sessionDamageDealt += damage;
            totalDamage += damage;
        }
        
        public void UpdateHighestCombo(int combo)
        {
            if (combo > sessionHighestCombo)
            {
                sessionHighestCombo = combo;
            }
            if (combo > longestCombo)
            {
                longestCombo = combo;
            }
        }
        
        public void AddPearl()
        {
            pearlsCollected++;
        }
        
        public string GetSessionTimeFormatted()
        {
            float sessionTime = Time.time - sessionStartTime;
            return FormatTime(sessionTime);
        }
        
        public string GetTotalTimeFormatted()
        {
            return FormatTime(totalPlayTime + (Time.time - sessionStartTime));
        }
        
        public string FormatTime(float seconds)
        {
            int hours = Mathf.FloorToInt(seconds / 3600f);
            int minutes = Mathf.FloorToInt((seconds % 3600f) / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            
            if (hours > 0)
            {
                return $"{hours}h {minutes}m {secs}s";
            }
            else if (minutes > 0)
            {
                return $"{minutes}m {secs}s";
            }
            else
            {
                return $"{secs}s";
            }
        }
        
        public float GetKillsPerMinute()
        {
            float sessionTime = Time.time - sessionStartTime;
            if (sessionTime < 1f) return 0f;
            return sessionKills / (sessionTime / 60f);
        }
        
        public float GetAverageDamagePerHit()
        {
            if (sessionKills < 1) return 0f;
            return (float)sessionDamageDealt / sessionKills;
        }
        
        private void TryAutoSave()
        {
            if (sessionKills % 50 == 0)
            {
                SaveStatistics();
            }
        }
        
        private void HandleEnemyKilled(EnemyType type, Vector3 position, int expReward)
        {
            AddKill(type);
        }
        
        private void HandleDamageDealt(int damage, Vector3 position, bool isCritical)
        {
            AddDamage(damage);
        }
        
        private void HandlePearlCollected(string pearlId)
        {
            AddPearl();
        }
        
        private void HandleComboChanged(int comboCount)
        {
            UpdateHighestCombo(comboCount);
        }
        
        private void OnDestroy()
        {
            SaveStatistics();
        }
    }
}
