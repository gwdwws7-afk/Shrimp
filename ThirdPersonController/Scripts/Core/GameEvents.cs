using System;
using UnityEngine;

namespace ThirdPersonController
{
    /// <summary>
    /// 全局游戏事件系统
    /// 用于解耦各个系统之间的通信
    /// </summary>
    public static class GameEvents
    {
        #region 玩家事件
        
        // 玩家受伤
        public static event Action<float, Vector3> OnPlayerDamaged;
        public static void PlayerDamaged(float damage, Vector3 source) => OnPlayerDamaged?.Invoke(damage, source);
        
        // 玩家治疗
        public static event Action<int> OnPlayerHealed;
        public static void PlayerHealed(int amount) => OnPlayerHealed?.Invoke(amount);
        
        // 玩家死亡
        public static event Action OnPlayerDeath;
        public static void PlayerDeath() => OnPlayerDeath?.Invoke();
        
        // 玩家复活
        public static event Action OnPlayerRespawn;
        public static void PlayerRespawn() => OnPlayerRespawn?.Invoke();
        
        #endregion

        #region 战斗事件
        
        // 连击变化
        public static event Action<int> OnComboChanged;
        public static void ComboChanged(int combo) => OnComboChanged?.Invoke(combo);
        
        // 狂暴模式变化
        public static event Action<bool> OnBerserkStateChanged;
        public static void BerserkStateChanged(bool isActive) => OnBerserkStateChanged?.Invoke(isActive);

        // 无双值变化
        public static event Action<float, float> OnMusouChanged;
        public static void MusouChanged(float current, float max) => OnMusouChanged?.Invoke(current, max);

        // 无双状态变化
        public static event Action<bool> OnMusouStateChanged;
        public static void MusouStateChanged(bool isActive) => OnMusouStateChanged?.Invoke(isActive);

        // 无双疲劳状态变化
        public static event Action<bool> OnMusouFatigueStateChanged;
        public static void MusouFatigueStateChanged(bool isActive) => OnMusouFatigueStateChanged?.Invoke(isActive);
        
        // 造成伤害
        public static event Action<int, Vector3, bool> OnDamageDealt;
        public static void DamageDealt(int damage, Vector3 position, bool isCritical = false) 
            => OnDamageDealt?.Invoke(damage, position, isCritical);

        // 敌人受击反馈
        public static event Action<int, Vector3, EnemyHitReactionType> OnEnemyHit;
        public static void EnemyHit(int damage, Vector3 position, EnemyHitReactionType reactionType) 
            => OnEnemyHit?.Invoke(damage, position, reactionType);
        
        // 击杀敌人
        public static event Action<EnemyType, Vector3, int> OnEnemyKilled;
        public static void EnemyKilled(EnemyType type, Vector3 position, int expReward) 
            => OnEnemyKilled?.Invoke(type, position, expReward);
        
        #endregion

        #region 耐力事件
        
        // 耐力变化
        public static event Action<float, float> OnStaminaChanged;
        public static void StaminaChanged(float current, float max) => OnStaminaChanged?.Invoke(current, max);
        
        // 耐力耗尽
        public static event Action OnStaminaDepleted;
        public static void StaminaDepleted() => OnStaminaDepleted?.Invoke();
        
        #endregion

        #region 技能事件
        
        // 技能释放
        public static event Action<string, float> OnSkillUsed;
        public static void SkillUsed(string skillName, float cooldown) => OnSkillUsed?.Invoke(skillName, cooldown);
        
        // 技能冷却完成
        public static event Action<string> OnSkillReady;
        public static void SkillReady(string skillName) => OnSkillReady?.Invoke(skillName);
        
        #endregion

        #region 游戏状态事件
        
        // 游戏暂停
        public static event Action<bool> OnGamePaused;
        public static void GamePaused(bool isPaused) => OnGamePaused?.Invoke(isPaused);
        
        // 关卡开始
        public static event Action<int> OnLevelStarted;
        public static void LevelStarted(int levelId) => OnLevelStarted?.Invoke(levelId);
        
        // 关卡完成
        public static event Action<int> OnLevelCompleted;
        public static void LevelCompleted(int levelId) => OnLevelCompleted?.Invoke(levelId);
        
        // 游戏结束
        public static event Action<bool> OnGameOver;
        public static void GameOver(bool isVictory) => OnGameOver?.Invoke(isVictory);
        
        #endregion

        #region 经验/成长事件
        
        // 获得经验
        public static event Action<int> OnExperienceGained;
        public static void ExperienceGained(int amount) => OnExperienceGained?.Invoke(amount);
        
        // 升级
        public static event Action<int> OnLevelUp;
        public static void LevelUp(int newLevel) => OnLevelUp?.Invoke(newLevel);
        
        #endregion

        #region UI事件
        
        // 显示伤害数字
        public static event Action<int, Vector3, bool> OnShowDamageText;
        public static void ShowDamageText(int damage, Vector3 position, bool isCritical = false) 
            => OnShowDamageText?.Invoke(damage, position, isCritical);
        
        // 显示提示信息
        public static event Action<string, float> OnShowMessage;
        public static void ShowMessage(string message, float duration = 2f) 
            => OnShowMessage?.Invoke(message, duration);
        
        #endregion
    }

    /// <summary>
    /// 敌人类型枚举
    /// </summary>
    public enum EnemyType
    {
        Grunt,      // 杂兵
        Rusher,     // 突击兵
        Tank,       // 重装兵
        Elite,      // 精英
        Mutant,     // 变异体
        Boss        // Boss
    }
}
