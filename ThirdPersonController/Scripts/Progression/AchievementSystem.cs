using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    public enum AchievementRarity
    {
        Common,
        Rare,
        Epic,
        Legendary,
        Secret
    }

    [System.Serializable]
    public class AchievementReward
    {
        public int exp = 0;
        public int pearls = 0;
    }

    [System.Serializable]
    public class AchievementData
    {
        public string achievementId = "";
        public string title = "";
        public string description = "";
        public AchievementRarity rarity = AchievementRarity.Common;
        public int targetValue = 1;
        public AchievementReward reward = new AchievementReward();
        public bool isSecret = false;
    }

    public class AchievementSystem : MonoBehaviour
    {
        [Header("Configuration")]
        public List<AchievementData> achievements = new List<AchievementData>();
        
        [Header("State")]
        public Dictionary<string, int> progress = new Dictionary<string, int>();
        public HashSet<string> unlockedAchievements = new HashSet<string>();
        
        [Header("Events")]
        public System.Action<AchievementData> OnAchievementUnlocked;
        public System.Action<AchievementData, int> OnProgressUpdated;

        private SaveManager saveManager;
        private PlayerExperienceSystem experienceSystem;

        private void Awake()
        {
            saveManager = FindObjectOfType<SaveManager>();
            experienceSystem = FindObjectOfType<PlayerExperienceSystem>();
            InitializeAchievements();
        }

        private void OnEnable()
        {
            GameEvents.OnEnemyKilled += HandleEnemyKilled;
            GameEvents.OnLevelCompleted += HandleLevelCompleted;
            GameEvents.OnComboCountChanged += HandleComboChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnEnemyKilled -= HandleEnemyKilled;
            GameEvents.OnLevelCompleted -= HandleLevelCompleted;
            GameEvents.OnComboCountChanged -= HandleComboChanged;
        }

        private void InitializeAchievements()
        {
            if (achievements.Count == 0)
            {
                CreateDefaultAchievements();
            }
            
            LoadProgress();
        }

        private void CreateDefaultAchievements()
        {
            achievements.AddRange(new AchievementData[]
            {
                new AchievementData { achievementId = "KILL_10", title = "初出茅庐", description = "击杀10个敌人", targetValue = 10, rarity = AchievementRarity.Common, reward = new AchievementReward { exp = 50 } },
                new AchievementData { achievementId = "KILL_100", title = "小有名气", description = "击杀100个敌人", targetValue = 100, rarity = AchievementRarity.Common, reward = new AchievementReward { exp = 100 } },
                new AchievementData { achievementId = "KILL_500", title = "深海屠夫", description = "击杀500个敌人", targetValue = 500, rarity = AchievementRarity.Rare, reward = new AchievementReward { exp = 200, pearls = 1 } },
                new AchievementData { achievementId = "KILL_1000", title = "战场收割机", description = "击杀1000个敌人", targetValue = 1000, rarity = AchievementRarity.Epic, reward = new AchievementReward { exp = 500, pearls = 2 } },
                
                new AchievementData { achievementId = "COMBO_25", title = "连击初学者", description = "达成25连击", targetValue = 25, rarity = AchievementRarity.Common, reward = new AchievementReward { exp = 50 } },
                new AchievementData { achievementId = "COMBO_50", title = "深渊狂暴", description = "达成50连击", targetValue = 50, rarity = AchievementRarity.Rare, reward = new AchievementReward { exp = 100, pearls = 1 } },
                new AchievementData { achievementId = "COMBO_100", title = "连击机器", description = "达成100连击", targetValue = 100, rarity = AchievementRarity.Epic, reward = new AchievementReward { exp = 300, pearls = 2 } },
                
                new AchievementData { achievementId = "BOSS_1", title = "Boss杀手", description = "击败1个Boss", targetValue = 1, rarity = AchievementRarity.Rare, reward = new AchievementReward { exp = 200 } },
                new AchievementData { achievementId = "BOSS_5", title = "Boss猎人", description = "击败5个Boss", targetValue = 5, rarity = AchievementRarity.Epic, reward = new AchievementReward { exp = 500, pearls = 2 } },
                
                new AchievementData { achievementId = "CHAPTER_1", title = "首战告捷", description = "通关第一章", targetValue = 1, rarity = AchievementRarity.Common, reward = new AchievementReward { exp = 100 } },
                new AchievementData { achievementId = "CHAPTER_3", title = "深入敌后", description = "通关第三章", targetValue = 3, rarity = AchievementRarity.Rare, reward = new AchievementReward { exp = 300, pearls = 1 } },
                new AchievementData { achievementId = "CHAPTER_5", title = "深渊征服者", description = "通关第五章", targetValue = 5, rarity = AchievementRarity.Legendary, reward = new AchievementReward { exp = 1000, pearls = 5 } },
                
                new AchievementData { achievementId = "PEARL_10", title = "收藏新手", description = "收集10颗珍珠", targetValue = 10, rarity = AchievementRarity.Rare, reward = new AchievementReward { exp = 100 } },
                new AchievementData { achievementId = "PEARL_50", title = "珍珠大亨", description = "收集50颗珍珠", targetValue = 50, rarity = AchievementRarity.Epic, reward = new AchievementReward { exp = 300, pearls = 3 } },
                
                new AchievementData { achievementId = "SURVIVE_5", title = "生存初体验", description = "存活5分钟", targetValue = 300, rarity = AchievementRarity.Common, reward = new AchievementReward { exp = 50 } },
                new AchievementData { achievementId = "SURVIVE_30", title = "老兵", description = "存活30分钟", targetValue = 1800, rarity = AchievementRarity.Rare, reward = new AchievementReward { exp = 200 } },
                
                new AchievementData { achievementId = "STARS_10", title = "初见星芒", description = "获得10颗星星", targetValue = 10, rarity = AchievementRarity.Rare, reward = new AchievementReward { exp = 100 } },
                new AchievementData { achievementId = "STARS_50", title = "星辰征服者", description = "获得50颗星星", targetValue = 50, rarity = AchievementRarity.Epic, reward = new AchievementReward { exp = 500, pearls = 2 } },
            });
        }

        public void Progress(string achievementId, int amount = 1)
        {
            if (string.IsNullOrEmpty(achievementId)) return;
            
            AchievementData achievement = GetAchievement(achievementId);
            if (achievement == null) return;
            
            if (unlockedAchievements.Contains(achievementId)) return;
            
            if (!progress.ContainsKey(achievementId))
            {
                progress[achievementId] = 0;
            }
            
            progress[achievementId] += amount;
            
            OnProgressUpdated?.Invoke(achievement, progress[achievementId]);
            
            if (progress[achievementId] >= achievement.targetValue)
            {
                UnlockAchievement(achievementId);
            }
            
            SaveProgress();
        }

        public void UnlockAchievement(string achievementId)
        {
            if (unlockedAchievements.Contains(achievementId)) return;
            
            AchievementData achievement = GetAchievement(achievementId);
            if (achievement == null) return;
            
            unlockedAchievements.Add(achievementId);
            
            if (experienceSystem != null && achievement.reward.exp > 0)
            {
                experienceSystem.GrantExperience(achievement.reward.exp);
            }
            
            SaveManager.Instance?.CurrentData?.unlockedAchievements.Add(achievementId);
            
            GameEvents.ShowMessage($"🏆 成就解锁: {achievement.title}!", 5f);
            
            OnAchievementUnlocked?.Invoke(achievement);
            
            SaveManager.Instance?.SaveGame();
        }

        public bool IsUnlocked(string achievementId)
        {
            return unlockedAchievements.Contains(achievementId);
        }

        public int GetProgress(string achievementId)
        {
            return progress.ContainsKey(achievementId) ? progress[achievementId] : 0;
        }

        public float GetProgressPercent(string achievementId)
        {
            AchievementData achievement = GetAchievement(achievementId);
            if (achievement == null || achievement.targetValue == 0) return 0f;
            
            int current = GetProgress(achievementId);
            return (float)current / achievement.targetValue;
        }

        public AchievementData GetAchievement(string achievementId)
        {
            return achievements.Find(a => a.achievementId == achievementId);
        }

        public List<AchievementData> GetUnlockedAchievements()
        {
            return achievements.FindAll(a => unlockedAchievements.Contains(a.achievementId));
        }

        public List<AchievementData> GetLockedAchievements()
        {
            return achievements.FindAll(a => !unlockedAchievements.Contains(a.achievementId));
        }

        private void LoadProgress()
        {
            if (saveManager?.CurrentData == null) return;
            
            unlockedAchievements = new HashSet<string>(saveManager.CurrentData.unlockedAchievements);
            
            progress = new Dictionary<string, int>(saveManager.CurrentData.achievementProgress);
        }

        private void SaveProgress()
        {
            if (saveManager?.CurrentData == null) return;
            
            saveManager.CurrentData.unlockedAchievements = new List<string>(unlockedAchievements);
            saveManager.CurrentData.achievementProgress = new Dictionary<string, int>(progress);
        }

        private void HandleEnemyKilled(EnemyType type, Vector3 position, int expReward)
        {
            Progress("KILL_10");
            Progress("KILL_100");
            Progress("KILL_500");
            Progress("KILL_1000");
            
            if (type == EnemyType.Boss)
            {
                Progress("BOSS_1");
                Progress("BOSS_5");
            }
        }

        private void HandleLevelCompleted(int levelId)
        {
            int chapter = (levelId / 100) % 10;
            Progress("CHAPTER_1");
            if (chapter >= 3) Progress("CHAPTER_3");
            if (chapter >= 5) Progress("CHAPTER_5");
        }

        private void HandleComboChanged(int comboCount)
        {
            if (comboCount >= 25) Progress("COMBO_25");
            if (comboCount >= 50) Progress("COMBO_50");
            if (comboCount >= 100) Progress("COMBO_100");
        }
    }
}
