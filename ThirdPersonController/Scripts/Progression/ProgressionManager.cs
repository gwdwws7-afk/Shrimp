using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    public enum Difficulty
    {
        Easy,
        Normal,
        Hard,
        Nightmare
    }

    public class ProgressionManager : MonoBehaviour
    {
        [Header("Configuration")]
        public int totalChapters = 5;
        public int levelsPerChapter = 4;
        
        [Header("State")]
        public int currentChapter = 1;
        public int currentLevel = 1;
        public Difficulty currentDifficulty = Difficulty.Normal;
        
        [Header("References")]
        public List<ChapterData> chapterDataList = new List<ChapterData>();
        
        public System.Action<int> OnChapterUnlocked;
        public System.Action<int> OnLevelUnlocked;
        public System.Action OnChapterCompleted;
        public System.Action OnLevelCompleted;
        public System.Action<Difficulty> OnDifficultyUnlocked;

        private SaveManager saveManager;
        private void Awake()
        {
            saveManager = FindObjectOfType<SaveManager>();
        }

        private void OnEnable()
        {
            GameEvents.OnLevelCompleted += HandleLevelCompleted;
            GameEvents.OnEnemyKilled += HandleEnemyKilled;
        }

        private void OnDisable()
        {
            GameEvents.OnLevelCompleted -= HandleLevelCompleted;
            GameEvents.OnEnemyKilled -= HandleEnemyKilled;
        }

        public bool IsChapterUnlocked(int chapter)
        {
            if (saveManager?.CurrentData == null) return chapter == 1;
            return saveManager.CurrentData.unlockedChapters >= chapter;
        }

        public bool IsLevelUnlocked(int chapter, int level)
        {
            if (saveManager?.CurrentData == null) return chapter == 1 && level == 1;
            
            string levelId = $"CH{chapter}_LV{level}";
            return saveManager.CurrentData.completedLevels.Exists(l => l == chapter * 100 + level) ||
                   (chapter == 1 && level == 1);
        }

        public bool CanPlayLevel(int chapter, int level, Difficulty difficulty)
        {
            if (!IsChapterUnlocked(chapter)) return false;
            if (!IsLevelUnlocked(chapter, level)) return false;
            
            if (difficulty == Difficulty.Hard && !IsHardModeUnlocked()) return false;
            if (difficulty == Difficulty.Nightmare && !IsNightmareModeUnlocked()) return false;
            
            return true;
        }

        public void CompleteLevel(int chapter, int level, int stars, int score, float time, bool noDamage)
        {
            if (saveManager?.CurrentData == null) return;

            int levelKey = chapter * 100 + level;
            
            GameData.LevelScore levelScore = saveManager.CurrentData.levelScores.Find(l => l.levelId == levelKey.ToString());
            if (levelScore == null)
            {
                levelScore = new GameData.LevelScore { levelId = levelKey.ToString() };
                saveManager.CurrentData.levelScores.Add(levelScore);
            }
            
            if (stars > levelScore.stars)
            {
                levelScore.stars = stars;
            }
            if (score > levelScore.highScore)
            {
                levelScore.highScore = score;
            }
            if (time > 0 && (levelScore.bestTime == 0 || time < levelScore.bestTime))
            {
                levelScore.bestTime = time;
            }
            if (noDamage)
            {
                levelScore.noDamage = true;
            }
            
            if (!saveManager.CurrentData.completedLevels.Contains(levelKey))
            {
                saveManager.CurrentData.completedLevels.Add(levelKey);
            }
            
            int nextLevel = level + 1;
            if (nextLevel > levelsPerChapter)
            {
                CompleteChapter(chapter);
            }
            
            OnLevelCompleted?.Invoke();
            saveManager.SaveGame();
        }

        public void CompleteChapter(int chapter)
        {
            if (saveManager?.CurrentData == null) return;

            if (!saveManager.CurrentData.completedChapters.Contains(chapter))
            {
                saveManager.CurrentData.completedChapters.Add(chapter);
            }
            
            if (chapter >= saveManager.CurrentData.unlockedChapters && chapter < totalChapters)
            {
                saveManager.CurrentData.unlockedChapters = chapter + 1;
                OnChapterUnlocked?.Invoke(chapter + 1);
            }
            
            if (chapter == 1 && !saveManager.CurrentData.hardModeUnlocked)
            {
                saveManager.CurrentData.hardModeUnlocked = true;
                OnDifficultyUnlocked?.Invoke(Difficulty.Hard);
            }
            
            if (chapter == 2 && !saveManager.CurrentData.nightmareModeUnlocked)
            {
                saveManager.CurrentData.nightmareModeUnlocked = true;
                OnDifficultyUnlocked?.Invoke(Difficulty.Nightmare);
            }
            
            OnChapterCompleted?.Invoke();
            saveManager.SaveGame();
        }

        public bool IsHardModeUnlocked()
        {
            return saveManager?.CurrentData?.hardModeUnlocked ?? false;
        }

        public bool IsNightmareModeUnlocked()
        {
            return saveManager?.CurrentData?.nightmareModeUnlocked ?? false;
        }

        public int GetTotalStars()
        {
            if (saveManager?.CurrentData == null) return 0;
            
            int total = 0;
            foreach (var score in saveManager.CurrentData.levelScores)
            {
                total += score.stars;
            }
            return total;
        }

        public int GetMaxStars()
        {
            return totalChapters * levelsPerChapter * 3;
        }

        public float GetProgressPercent()
        {
            int maxLevels = totalChapters * levelsPerChapter;
            if (saveManager?.CurrentData == null || maxLevels == 0) return 0f;
            
            return (float)saveManager.CurrentData.completedLevels.Count / maxLevels * 100f;
        }

        public void UnlockChapter(int chapter)
        {
            if (saveManager?.CurrentData == null) return;
            
            if (chapter > saveManager.CurrentData.unlockedChapters)
            {
                saveManager.CurrentData.unlockedChapters = chapter;
                OnChapterUnlocked?.Invoke(chapter);
            }
        }

        public void UnlockLevel(int chapter, int level)
        {
            if (saveManager?.CurrentData == null) return;
            
            int levelKey = chapter * 100 + level;
            if (!saveManager.CurrentData.completedLevels.Contains(levelKey))
            {
                OnLevelUnlocked?.Invoke(levelKey);
            }
        }

        private void HandleLevelCompleted(int levelId)
        {
            int chapter = (levelId / 100) % 10;
            int level = levelId % 100;
            CompleteLevel(chapter, level, 3, 1000, 0, false);
        }

        private void HandleEnemyKilled(EnemyType type, Vector3 position, int expReward)
        {
            if (type == EnemyType.Boss && saveManager?.CurrentData != null)
            {
                saveManager.CurrentData.bossesDefeated++;
            }
        }

        public void ResetProgress()
        {
            if (saveManager?.CurrentData == null) return;
            
            saveManager.CurrentData.currentChapter = 1;
            saveManager.CurrentData.currentLevel = 1;
            saveManager.CurrentData.unlockedChapters = 1;
            saveManager.CurrentData.completedChapters.Clear();
            saveManager.CurrentData.completedLevels.Clear();
            saveManager.CurrentData.levelScores.Clear();
            saveManager.CurrentData.unlockedAchievements.Clear();
            saveManager.CurrentData.achievementProgress.Clear();
            saveManager.CurrentData.totalKills = 0;
            saveManager.CurrentData.bossesDefeated = 0;
            saveManager.CurrentData.highestCombo = 0;
            
            saveManager.SaveGame();
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }
}
