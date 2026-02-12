using UnityEngine;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;

namespace ThirdPersonController
{
    /// <summary>
    /// 游戏数据 - 可序列化的存档数据
    /// </summary>
    [Serializable]
    public class GameData
    {
        // 玩家数据
        public int playerLevel = 1;
        public int currentExp = 0;
        public int maxHealth = 100;
        public int currentHealth = 100;
        
        // 技能数据
        public int[] skillLevels = new int[6];
        public int unlockedSkills = 0;
        
        // 进度数据
        public int currentLevel = 1;
        public int unlockedLevels = 1;
        public int enemiesKilled = 0;
        public int highestCombo = 0;
        public int talentPoints = 0;
        public int killsSinceLastTalentPoint = 0;
        public List<string> unlockedTalentNodes = new List<string>();
        public List<string> ownedPearlIds = new List<string>();
        public List<string> equippedPearlIds = new List<string>();
        
        // 设置数据
        public float masterVolume = 1f;
        public float musicVolume = 0.7f;
        public float sfxVolume = 0.8f;
        public float sensitivity = 1f;
        public bool fullscreen = true;
        public int resolutionIndex = 0;
        
        // 时间戳
        public string saveTime = "";
        public float totalPlayTime = 0f;
        
        public GameData()
        {
            saveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
    
    /// <summary>
    /// 存档管理器 - 处理游戏存档的保存和加载
    /// </summary>
    public class SaveManager : Singleton<SaveManager>
    {
        [Header("存档设置")]
        public bool encryptSave = true;
        public string encryptionKey = "AbyssHunter2026"; // 简单的加密密钥
        
        // 存档文件路径
        private string SavePath => Application.persistentDataPath + "/savegame.dat";
        private string SettingsPath => Application.persistentDataPath + "/settings.dat";
        
        // 当前游戏数据
        public GameData CurrentData { get; private set; }
        
        // 是否已加载存档
        public bool HasLoadedSave => CurrentData != null;
        
        // 事件
        public System.Action OnSaveCompleted;
        public System.Action OnLoadCompleted;
        
        protected override void OnAwake()
        {
            base.OnAwake();
            CurrentData = new GameData();
            EnsureProgressionLists();
            LoadSettings(); // 启动时加载设置
        }
        
        #region 保存游戏
        
        /// <summary>
        /// 保存游戏进度
        /// </summary>
        public void SaveGame()
        {
            try
            {
                // 更新数据
                UpdateGameData();
                
                // 序列化
                string json = JsonUtility.ToJson(CurrentData, true);
                
                // 加密（如果启用）
                if (encryptSave)
                {
                    json = EncryptString(json, encryptionKey);
                }
                
                // 写入文件
                File.WriteAllText(SavePath, json);
                
                Debug.Log($"✅ 游戏已保存: {SavePath}");
                OnSaveCompleted?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ 保存游戏失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// 自动保存
        /// </summary>
        public void AutoSave()
        {
            SaveGame();
            Debug.Log("💾 自动保存完成");
        }
        
        /// <summary>
        /// 更新游戏数据
        /// </summary>
        private void UpdateGameData()
        {
            // 从游戏中收集数据
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
            
            // 更新时间
            CurrentData.saveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
        
        #endregion
        
        #region 加载游戏
        
        /// <summary>
        /// 加载游戏进度
        /// </summary>
        public bool LoadGame()
        {
            try
            {
                if (!File.Exists(SavePath))
                {
                    Debug.Log("⚠️ 没有找到存档文件，创建新游戏");
                    CurrentData = new GameData();
                    EnsureProgressionLists();
                    return false;
                }
                
                // 读取文件
                string json = File.ReadAllText(SavePath);
                
                // 解密（如果启用）
                if (encryptSave)
                {
                    json = DecryptString(json, encryptionKey);
                }
                
                // 反序列化
                CurrentData = JsonUtility.FromJson<GameData>(json);
                EnsureProgressionLists();
                
                Debug.Log($"✅ 游戏已加载: {CurrentData.saveTime}");
                OnLoadCompleted?.Invoke();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ 加载游戏失败: {e.Message}");
                CurrentData = new GameData();
                EnsureProgressionLists();
                return false;
            }
        }
        
        /// <summary>
        /// 应用加载的数据到游戏
        /// </summary>
        public void ApplyLoadedData()
        {
            if (CurrentData == null) return;
            
            // 应用到玩家
            PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
            if (playerHealth != null)
            {
                // 使用反射或方法设置血量
                // playerHealth.SetHealth(CurrentData.currentHealth);
            }
            
            // 应用音量设置
            AudioManager.Instance?.SetMasterVolume(CurrentData.masterVolume);
            AudioManager.Instance?.SetMusicVolume(CurrentData.musicVolume);
            AudioManager.Instance?.SetSFXVolume(CurrentData.sfxVolume);
            
            Debug.Log("✅ 存档数据已应用到游戏");
        }
        
        #endregion
        
        #region 设置保存
        
        /// <summary>
        /// 保存设置（音量、分辨率等）
        /// </summary>
        public void SaveSettings()
        {
            try
            {
                // 更新设置数据
                CurrentData.masterVolume = AudioManager.Instance?.masterVolume ?? 1f;
                CurrentData.musicVolume = AudioManager.Instance?.musicVolume ?? 0.7f;
                CurrentData.sfxVolume = AudioManager.Instance?.sfxVolume ?? 0.8f;
                
                // 保存到单独文件
                string json = JsonUtility.ToJson(CurrentData);
                File.WriteAllText(SettingsPath, json);
                
                Debug.Log("✅ 设置已保存");
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ 保存设置失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// 加载设置
        /// </summary>
        public void LoadSettings()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                {
                    Debug.Log("⚠️ 没有找到设置文件，使用默认设置");
                    return;
                }
                
                string json = File.ReadAllText(SettingsPath);
                GameData settings = JsonUtility.FromJson<GameData>(json);
                
                // 应用设置
                CurrentData.masterVolume = settings.masterVolume;
                CurrentData.musicVolume = settings.musicVolume;
                CurrentData.sfxVolume = settings.sfxVolume;
                CurrentData.sensitivity = settings.sensitivity;
                CurrentData.fullscreen = settings.fullscreen;
                CurrentData.resolutionIndex = settings.resolutionIndex;
                
                Debug.Log("✅ 设置已加载");
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ 加载设置失败: {e.Message}");
            }
        }
        
        #endregion
        
        #region 删除存档
        
        /// <summary>
        /// 删除存档
        /// </summary>
        public void DeleteSave()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    File.Delete(SavePath);
                    Debug.Log("🗑️ 存档已删除");
                }
                
                CurrentData = new GameData();
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ 删除存档失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// 检查是否有存档
        /// </summary>
        public bool HasSaveFile()
        {
            return File.Exists(SavePath);
        }
        
        #endregion
        
        #region 加密/解密
        
        /// <summary>
        /// 加密字符串
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
                return text; // 加密失败返回原文
            }
        }
        
        /// <summary>
        /// 解密字符串
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
                return encryptedText; // 解密失败返回原文
            }
        }
        
        #endregion
        
        #region 调试
        
        /// <summary>
        /// 打印存档信息
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
                Debug.Log($"天赋点: {CurrentData.talentPoints}");
                Debug.Log($"已解锁天赋: {CurrentData.unlockedTalentNodes?.Count ?? 0}");
                Debug.Log($"珍珠数量: {CurrentData.ownedPearlIds?.Count ?? 0}");
                Debug.Log($"游戏时长: {CurrentData.totalPlayTime:F1}秒");
                Debug.Log($"最后保存: {CurrentData.saveTime}");
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
        }
        
        #endregion
    }
}
