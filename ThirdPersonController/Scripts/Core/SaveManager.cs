using UnityEngine;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;

namespace ThirdPersonController
{
    [Serializable]
    public class QuestStateData
    {
        public string questId = "";
        public int status = 2;
        public int currentProgress = 0;
        public int stageIndex = 0;
        public float stageElapsedTime = 0f;
        public float totalElapsedTime = 0f;
        public bool isTimerActive = false;
        public string lastStrongholdId = "";
    }

    /// <summary>
    /// SaveManager 模块的核心实现，负责统一管理关键运行流程与对外接口。
    /// </summary>
    [Serializable]
    public class GameData
    {
// 玩家关卡配置项，用于驱动模块行为并保持可调性。
        public int playerLevel = 1;
        public int currentExp = 0;
        public int maxHealth = 100;
        public int currentHealth = 100;
        
// 技能配置项，用于驱动模块行为并保持可调性。
        public int[] skillLevels = new int[6];
        public int unlockedSkills = 0;
        
// 关卡配置项，用于驱动模块行为并保持可调性。
        public int currentLevel = 1;
        public int unlockedLevels = 1;
        public int enemiesKilled = 0;
        public int highestCombo = 0;
        public int talentPoints = 0;
        public int killsSinceLastTalentPoint = 0;
        public List<string> unlockedTalentNodes = new List<string>();
        public List<string> ownedPearlIds = new List<string>();
        public List<string> equippedPearlIds = new List<string>();
        public int credits = 0;
        public List<ConsumableStack> consumables = new List<ConsumableStack>();
        public List<string> quickConsumableSlots = new List<string>();
        public List<QuestStateData> questStates = new List<QuestStateData>();
        
// 系统配置项，用于驱动模块行为并保持可调性。
        public int currentChapter = 1;
        public int unlockedChapters = 1;
        public List<int> completedChapters = new List<int>();
        public List<int> completedLevels = new List<int>();
        
// 围绕 Serializable 执行该步骤，用于保持上下文语义一致。
        [Serializable]
        public class LevelScore
        {
            public string levelId = "";
            public int stars = 0;
            public int highScore = 0;
            public float bestTime = 0f;
            public bool noDamage = false;
        }
        public List<LevelScore> levelScores = new List<LevelScore>();
        
// 系统配置项，用于驱动模块行为并保持可调性。
        public List<string> unlockedAchievements = new List<string>();
        public Dictionary<string, int> achievementProgress = new Dictionary<string, int>();
        
// 系统配置项，用于驱动模块行为并保持可调性。
        public int totalKills = 0;
        public int totalDamage = 0;
        public int longestCombo = 0;
        public int bossesDefeated = 0;
        public int pearlsCollected = 0;

// 系统配置项，用于驱动模块行为并保持可调性。
        public int unlockedPearlSlots = 3;
        public int maxPearlRarityUnlocked = 1;
        public float pearlDropRateMultiplier = 1f;
        public int totalLevelsCompleted = 0;
        public List<string> claimedProgressionMilestones = new List<string>();
        public string activeProgressionRoute = "Offense";
        
// 系统配置项，用于驱动模块行为并保持可调性。
        public bool hardModeUnlocked = false;
        public bool nightmareModeUnlocked = false;
        public bool newGamePlusUnlocked = false;
        
// 系统配置项，用于驱动模块行为并保持可调性。
        public float masterVolume = 1f;
        public float musicVolume = 0.7f;
        public float sfxVolume = 0.8f;
        public float sensitivity = 1f;
        public bool fullscreen = true;
        public int resolutionIndex = 0;
        
// 存档配置项，用于驱动模块行为并保持可调性。
        public string saveTime = "";
        public float totalPlayTime = 0f;
        public string lastPlayedDate = "";
        
        public GameData()
        {
            saveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            lastPlayedDate = DateTime.Now.ToString("yyyy-MM-dd");
        }
    }
    
    /// <summary>
    /// SaveManager 模块的核心实现，负责统一管理关键运行流程与对外接口。
    /// </summary>
    public class SaveManager : Singleton<SaveManager>
    {
        [Header("存档设置")]
        public bool encryptSave = true;
        public string encryptionKey = "AbyssHunter2026"; // 运行时配置项，用于驱动模块行为并保持可调性。
        
// 存档配置项，用于驱动模块行为并保持可调性。
        private string SavePath => Application.persistentDataPath + "/savegame.dat";
        private string SettingsPath => Application.persistentDataPath + "/settings.dat";

        public string SaveFilePath => SavePath;
        public string SettingsFilePath => SettingsPath;
        
// 系统配置项，用于驱动模块行为并保持可调性。
        public GameData CurrentData { get; private set; }
        
// 运行时状态标记，用于快速分支判定与流程保护。
        public bool HasLoadedSave => CurrentData != null;
        
// 存档配置项，用于驱动模块行为并保持可调性。
        public System.Action OnSaveCompleted;
        public System.Action OnLoadCompleted;
        
        protected override void OnAwake()
        {
            base.OnAwake();
            CurrentData = new GameData();
            EnsureProgressionLists();
            LoadSettings(); // 围绕 加载 执行该步骤，用于保证流程状态与后续分支一致。
        }
        
        #region 保存游戏
        
        /// <summary>
        /// 保存Game，将关键数据持久化到本地。
        /// </summary>
        public void SaveGame()
        {
            try
            {
// 围绕 游戏 执行该步骤，用于保证流程状态与后续分支一致。
                UpdateGameData();
                
// 围绕 string 执行该步骤，用于保证流程状态与后续分支一致。
                string json = JsonUtility.ToJson(CurrentData, true);
                
// 围绕 存档 执行该步骤，用于保证流程状态与后续分支一致。
                if (encryptSave)
                {
                    json = EncryptString(json, encryptionKey);
                }
                
// 围绕 存档 执行该步骤，用于保证流程状态与后续分支一致。
                File.WriteAllText(SavePath, json);
                
                Debug.Log($"[Save] 游戏已保存: {SavePath}");
                OnSaveCompleted?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] 保存游戏失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// 执行 Auto Save 相关逻辑，并保证模块状态与外部调用约定一致。
        /// </summary>
        public void AutoSave()
        {
            SaveGame();
            Debug.Log("💾 自动保存完成");
        }
        
        /// <summary>
        /// 更新Game Data，保持显示与运行数据一致。
        /// </summary>
        private void UpdateGameData()
        {
// 场景级兜底查找依赖，降低手动绑定遗漏风险。
            PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
            if (playerHealth != null)
            {
                CurrentData.currentHealth = playerHealth.CurrentHealth;
                CurrentData.maxHealth = playerHealth.MaxHealth;
            }

            PlayerExperienceSystem experienceSystem = FindObjectOfType<PlayerExperienceSystem>();
            if (experienceSystem != null)
            {
                CurrentData.playerLevel = experienceSystem.level;
                CurrentData.currentExp = experienceSystem.currentExp;
            }
            
// 围绕 CurrentData 执行该步骤，用于保证流程状态与后续分支一致。
            CurrentData.saveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
        
        #endregion
        
        #region 加载游戏
        
        /// <summary>
        /// 加载Game，从持久化数据恢复运行状态。
        /// </summary>
        public bool LoadGame()
        {
            try
            {
                if (!File.Exists(SavePath))
                {
                    Debug.Log("[Save] Save file not found. Creating new game data.");
                    CurrentData = new GameData();
                    EnsureProgressionLists();
                    return false;
                }
                
// 围绕 存档 执行该步骤，用于保证流程状态与后续分支一致。
                string json = File.ReadAllText(SavePath);
                
// 围绕 存档 执行该步骤，用于保证流程状态与后续分支一致。
                if (encryptSave)
                {
                    json = DecryptString(json, encryptionKey);
                }
                
// 围绕 游戏 执行该步骤，用于保持上下文语义一致。
                CurrentData = JsonUtility.FromJson<GameData>(json);
                EnsureProgressionLists();
                
                Debug.Log($"[Save] 游戏已加载: {CurrentData.saveTime}");
                OnLoadCompleted?.Invoke();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] 加载游戏失败: {e.Message}");
                CurrentData = new GameData();
                EnsureProgressionLists();
                return false;
            }
        }
        
        /// <summary>
        /// 应用Loaded Data，统一入口下发效果并便于后续扩展。
        /// </summary>
        public void ApplyLoadedData()
        {
            if (CurrentData == null) return;
            
// 场景级兜底查找依赖，降低手动绑定遗漏风险。
            PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
            if (playerHealth != null)
            {
// 围绕 当前步骤 执行该步骤，用于保持上下文语义一致。
                // playerHealth.SetHealth(CurrentData.currentHealth);
            }
            
// 围绕 音频 执行该步骤，用于保证流程状态与后续分支一致。
            AudioManager.Instance?.SetMasterVolume(CurrentData.masterVolume);
            AudioManager.Instance?.SetMusicVolume(CurrentData.musicVolume);
            AudioManager.Instance?.SetSFXVolume(CurrentData.sfxVolume);
            
            Debug.Log("[Save] Applied save data to runtime systems.");
        }
        
        #endregion
        
        #region 设置保存
        
        /// <summary>
        /// 保存Settings，将关键数据持久化到本地。
        /// </summary>
        public void SaveSettings()
        {
            try
            {
// 围绕 音频 执行该步骤，用于保持上下文语义一致。
                CurrentData.masterVolume = AudioManager.Instance?.masterVolume ?? 1f;
                CurrentData.musicVolume = AudioManager.Instance?.musicVolume ?? 0.7f;
                CurrentData.sfxVolume = AudioManager.Instance?.sfxVolume ?? 0.8f;
                
// 围绕 string 执行该步骤，用于保证流程状态与后续分支一致。
                string json = JsonUtility.ToJson(CurrentData);
                File.WriteAllText(SettingsPath, json);
                
                Debug.Log($"[Save] 设置已保存: {SettingsPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] 保存设置失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// 加载Settings，从持久化数据恢复运行状态。
        /// </summary>
        public void LoadSettings()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                {
                    Debug.Log("[Save] Settings file not found. Using defaults.");
                    return;
                }
                
                string json = File.ReadAllText(SettingsPath);
                GameData settings = JsonUtility.FromJson<GameData>(json);
                
// 围绕 CurrentData 执行该步骤，用于保持上下文语义一致。
                CurrentData.masterVolume = settings.masterVolume;
                CurrentData.musicVolume = settings.musicVolume;
                CurrentData.sfxVolume = settings.sfxVolume;
                CurrentData.sensitivity = settings.sensitivity;
                CurrentData.fullscreen = settings.fullscreen;
                CurrentData.resolutionIndex = settings.resolutionIndex;
                
                Debug.Log("[Save] Loaded local settings.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] 加载设置失败: {e.Message}");
            }
        }
        
        #endregion
        
        #region 删除存档
        
        /// <summary>
        /// 执行 Delete Save 相关逻辑，并保证模块状态与外部调用约定一致。
        /// </summary>
        public void DeleteSave()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    File.Delete(SavePath);
                    Debug.Log("[Save] Save file deleted.");
                }
                
                CurrentData = new GameData();
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] 删除存档失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// 执行 Has Save File 相关逻辑，并保证模块状态与外部调用约定一致。
        /// </summary>
        public bool HasSaveFile()
        {
            return File.Exists(SavePath);
        }
        
        #endregion
        
        #region 加密/解密
        
        /// <summary>
        /// 执行 Encrypt String 相关逻辑，并保证模块状态与外部调用约定一致。
        /// </summary>
        private string EncryptString(string text, string key)
        {
            try
            {
                byte[] keyBytes = Encoding.UTF8.GetBytes(key);
                byte[] textBytes = Encoding.UTF8.GetBytes(text);
                
                using (Aes aes = Aes.Create())
                {
                    aes.Key = keyBytes;
                    aes.Mode = CipherMode.ECB;
                    aes.Padding = PaddingMode.PKCS7;
                    
                    ICryptoTransform encryptor = aes.CreateEncryptor();
                    byte[] encrypted = encryptor.TransformFinalBlock(textBytes, 0, textBytes.Length);
                    return Convert.ToBase64String(encrypted);
                }
            }
            catch
            {
                return text; // 围绕 return 执行该步骤，用于保持上下文语义一致。
            }
        }
        
        /// <summary>
        /// 执行 Decrypt String 相关逻辑，并保证模块状态与外部调用约定一致。
        /// </summary>
        private string DecryptString(string encryptedText, string key)
        {
            try
            {
                byte[] keyBytes = Encoding.UTF8.GetBytes(key);
                byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
                
                using (Aes aes = Aes.Create())
                {
                    aes.Key = keyBytes;
                    aes.Mode = CipherMode.ECB;
                    aes.Padding = PaddingMode.PKCS7;
                    
                    ICryptoTransform decryptor = aes.CreateDecryptor();
                    byte[] decrypted = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                    return Encoding.UTF8.GetString(decrypted);
                }
            }
            catch
            {
                return encryptedText; // 围绕 return 执行该步骤，用于保持上下文语义一致。
            }
        }
        
        #endregion
        
        #region 调试
        
        /// <summary>
        /// 执行 Print Save Info 相关逻辑，并保证模块状态与外部调用约定一致。
        /// </summary>
        public void PrintSaveInfo()
        {
            Debug.Log($"=== 存档信息 ===");
            Debug.Log($"存档路径: {SavePath}");
            Debug.Log($"是否存在: {HasSaveFile()}");
            if (CurrentData != null)
            {
                Debug.Log($"玩家等级: {CurrentData.playerLevel}");
                Debug.Log($"当前关卡: {CurrentData.currentLevel}");
                Debug.Log($"击杀数: {CurrentData.enemiesKilled}");
                Debug.Log($"最高连击: {CurrentData.highestCombo}");
                Debug.Log($"未分配天赋点: {CurrentData.talentPoints}");
                Debug.Log($"已解锁天赋节点数: {CurrentData.unlockedTalentNodes.Count}");
                Debug.Log($"拥有珍珠数: {CurrentData.ownedPearlIds.Count}");
                Debug.Log($"货币: {CurrentData.credits}");
                Debug.Log($"已装备珍珠数: {CurrentData.equippedPearlIds.Count}");
                Debug.Log($"背包道具种类数: {CurrentData.consumables.Count}");
                Debug.Log($"Total play time: {CurrentData.totalPlayTime:0.0}s");
                Debug.Log($"最近存档时间: {CurrentData.saveTime}");
            }
        }

        private void EnsureProgressionLists()
        {
            if (CurrentData == null)
            {
                return;
            }

            if (CurrentData.unlockedTalentNodes == null)
            {
                CurrentData.unlockedTalentNodes = new List<string>();
            }

            if (CurrentData.ownedPearlIds == null)
            {
                CurrentData.ownedPearlIds = new List<string>();
            }

            if (CurrentData.equippedPearlIds == null)
            {
                CurrentData.equippedPearlIds = new List<string>();
            }

            if (CurrentData.completedLevels == null)
            {
                CurrentData.completedLevels = new List<int>();
            }

            if (CurrentData.consumables == null)
            {
                CurrentData.consumables = new List<ConsumableStack>();
            }

            if (CurrentData.quickConsumableSlots == null)
            {
                CurrentData.quickConsumableSlots = new List<string>();
            }

            if (CurrentData.questStates == null)
            {
                CurrentData.questStates = new List<QuestStateData>();
            }

            while (CurrentData.quickConsumableSlots.Count < 3)
            {
                CurrentData.quickConsumableSlots.Add(string.Empty);
            }

            if (CurrentData.claimedProgressionMilestones == null)
            {
                CurrentData.claimedProgressionMilestones = new List<string>();
            }

            if (string.IsNullOrEmpty(CurrentData.activeProgressionRoute))
            {
                CurrentData.activeProgressionRoute = "Offense";
            }
        }
        
        #endregion
    }
}


