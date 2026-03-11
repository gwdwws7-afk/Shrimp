using System;
using UnityEngine;

namespace ThirdPersonController
{
    /// <summary>
    /// GameEvents 模块的核心实现，负责统一管理关键运行流程与对外接口。
    /// </summary>
    public static class GameEvents
    {
        #region 玩家事件
        
// 玩家事件声明，用于在运行时向监听方广播关键状态变化。
        public static event Action<float, Vector3> OnPlayerDamaged;
        public static void PlayerDamaged(float damage, Vector3 source) => OnPlayerDamaged?.Invoke(damage, source);
        
// 玩家事件声明，用于在运行时向监听方广播关键状态变化。
        public static event Action<int> OnPlayerHealed;
        public static void PlayerHealed(int amount) => OnPlayerHealed?.Invoke(amount);
        
// 玩家事件声明，用于在运行时向监听方广播关键状态变化。
        public static event Action OnPlayerDeath;
        public static void PlayerDeath() => OnPlayerDeath?.Invoke();
        
// 玩家事件声明，用于在运行时向监听方广播关键状态变化。
        public static event Action OnPlayerRespawn;
        public static void PlayerRespawn() => OnPlayerRespawn?.Invoke();
        
        #endregion

        #region 战斗事件
        
// 连击事件声明，用于在运行时向监听方广播关键状态变化。
        public static event Action<int> OnComboChanged;
        public static void ComboChanged(int combo) => OnComboChanged?.Invoke(combo);
        
// 连击事件声明，用于在运行时向监听方广播关键状态变化。
        public static event Action<int> OnComboCountChanged;
        public static void ComboCountChanged(int comboCount) => OnComboCountChanged?.Invoke(comboCount);
        
// 狂暴事件声明，用于在运行时向监听方广播关键状态变化。
        public static event Action<bool> OnBerserkStateChanged;
        public static void BerserkStateChanged(bool isActive) => OnBerserkStateChanged?.Invoke(isActive);

// 无双事件声明，用于在运行时向监听方广播关键状态变化。
        public static event Action<float, float> OnMusouChanged;
        public static void MusouChanged(float current, float max) => OnMusouChanged?.Invoke(current, max);

// 无双事件声明，用于在运行时向监听方广播关键状态变化。
        public static event Action<bool> OnMusouStateChanged;
        public static void MusouStateChanged(bool isActive) => OnMusouStateChanged?.Invoke(isActive);

// 无双事件声明，用于在运行时向监听方广播关键状态变化。
        public static event Action<bool> OnMusouFatigueStateChanged;
        public static void MusouFatigueStateChanged(bool isActive) => OnMusouFatigueStateChanged?.Invoke(isActive);
        
// 伤害事件声明，用于在运行时向监听方广播关键状态变化。
        public static event Action<int, Vector3, bool> OnDamageDealt;
        public static void DamageDealt(int damage, Vector3 position, bool isCritical = false) 
            => OnDamageDealt?.Invoke(damage, position, isCritical);

// 敌人事件声明，用于在运行时向监听方广播关键状态变化。
        public static event Action<int, Vector3, EnemyHitReactionType> OnEnemyHit;
        public static void EnemyHit(int damage, Vector3 position, EnemyHitReactionType reactionType) 
            => OnEnemyHit?.Invoke(damage, position, reactionType);
        
// 敌人事件声明，用于在运行时向监听方广播关键状态变化。
        public static event Action<EnemyType, Vector3, int> OnEnemyKilled;
        public static void EnemyKilled(EnemyType type, Vector3 position, int expReward) 
            => OnEnemyKilled?.Invoke(type, position, expReward);

// 敌人事件声明，用于在运行时向监听方广播关键状态变化。
        public static event Action<EnemyType, Vector3, int, DamageSourceType, bool> OnEnemyKilledDetailed;
        public static void EnemyKilledDetailed(EnemyType type, Vector3 position, int expReward, DamageSourceType sourceType, bool isHeavyAttack)
            => OnEnemyKilledDetailed?.Invoke(type, position, expReward, sourceType, isHeavyAttack);
        
        #endregion

        #region 耐力事件
        
// 耐力事件声明，用于在运行时向监听方广播关键状态变化。
        public static event Action<float, float> OnStaminaChanged;
        public static void StaminaChanged(float current, float max) => OnStaminaChanged?.Invoke(current, max);
        
// 耐力事件声明，用于在运行时向监听方广播关键状态变化。
        public static event Action OnStaminaDepleted;
        public static void StaminaDepleted() => OnStaminaDepleted?.Invoke();
        
        #endregion

        #region 技能事件
        
// 技能事件声明，用于在运行时向监听方广播关键状态变化。
        public static event Action<string, float> OnSkillUsed;
        public static void SkillUsed(string skillName, float cooldown) => OnSkillUsed?.Invoke(skillName, cooldown);
        
// 技能事件声明，用于在运行时向监听方广播关键状态变化。
        public static event Action<string> OnSkillReady;
        public static void SkillReady(string skillName) => OnSkillReady?.Invoke(skillName);
        
        #endregion

        #region 游戏状态事件
        
// 游戏事件声明，用于在运行时向监听方广播关键状态变化。
        public static event Action<bool> OnGamePaused;
        public static void GamePaused(bool isPaused) => OnGamePaused?.Invoke(isPaused);
        
// 关卡事件声明，用于在运行时向监听方广播关键状态变化。
        public static event Action<int> OnLevelStarted;
        public static void LevelStarted(int levelId) => OnLevelStarted?.Invoke(levelId);
        
// 关卡事件声明，用于在运行时向监听方广播关键状态变化。
        public static event Action<int> OnLevelCompleted;
        public static void LevelCompleted(int levelId) => OnLevelCompleted?.Invoke(levelId);
        
// 游戏事件声明，用于在运行时向监听方广播关键状态变化。
        public static event Action<bool> OnGameOver;
        public static void GameOver(bool isVictory) => OnGameOver?.Invoke(isVictory);
        
// 波次事件声明，用于在运行时向监听方广播关键状态变化。
        public static event Action<StrongholdController, int> OnWaveCompleted;
        public static void WaveCompleted(StrongholdController stronghold, int waveIndex) => OnWaveCompleted?.Invoke(stronghold, waveIndex);

// 波次事件声明，用于在运行时向监听方广播关键状态变化。
        public static event Action<StrongholdController, int> OnWaveStarted;
        public static void WaveStarted(StrongholdController stronghold, int waveIndex) => OnWaveStarted?.Invoke(stronghold, waveIndex);
        
// 据点事件声明，用于在运行时向监听方广播关键状态变化。
        public static event Action<StrongholdController> OnStrongholdCompleted;
        public static void StrongholdCompleted(StrongholdController stronghold) => OnStrongholdCompleted?.Invoke(stronghold);

// 据点事件声明，用于在运行时向监听方广播关键状态变化。
        public static event Action<StrongholdController> OnStrongholdStarted;
        public static void StrongholdStarted(StrongholdController stronghold) => OnStrongholdStarted?.Invoke(stronghold);

// 波次事件声明，用于在运行时向监听方广播关键状态变化。
        public static event Action<StrongholdController, int, WaveEventType> OnWaveEventCompleted;
        public static void WaveEventCompleted(StrongholdController stronghold, int waveIndex, WaveEventType eventType)
            => OnWaveEventCompleted?.Invoke(stronghold, waveIndex, eventType);
        
        #endregion

        #region 经验/成长事件
        
// 经验事件声明，用于在运行时向监听方广播关键状态变化。
        public static event Action<int> OnExperienceGained;
        public static void ExperienceGained(int amount) => OnExperienceGained?.Invoke(amount);
        
// 关卡事件声明，用于在运行时向监听方广播关键状态变化。
        public static event Action<int> OnLevelUp;
        public static void LevelUp(int newLevel) => OnLevelUp?.Invoke(newLevel);
        
        #endregion

        #region UI事件
        
// 伤害事件声明，用于在运行时向监听方广播关键状态变化。
        public static event Action<int, Vector3, bool> OnShowDamageText;
        public static void ShowDamageText(int damage, Vector3 position, bool isCritical = false) 
            => OnShowDamageText?.Invoke(damage, position, isCritical);
        
// 提示事件声明，用于在运行时向监听方广播关键状态变化。
        public static event Action<string, float> OnShowMessage;
        public static void ShowMessage(string message, float duration = 2f) 
            => OnShowMessage?.Invoke(message, duration);
        
// 系统事件声明，用于在运行时向监听方广播关键状态变化。
        public static event Action<string> OnPearlCollected;
        public static void PearlCollected(string pearlId) => OnPearlCollected?.Invoke(pearlId);

// 系统事件声明，用于在运行时向监听方广播关键状态变化。
        public static event Action<string> OnLocationReached;
        public static void LocationReached(string locationId) => OnLocationReached?.Invoke(locationId);

// 系统事件声明，用于在运行时向监听方广播关键状态变化。
        public static event Action<string> OnDefenseTargetDestroyed;
        public static void DefenseTargetDestroyed(string targetId) => OnDefenseTargetDestroyed?.Invoke(targetId);

// Boss事件声明，用于在运行时向监听方广播关键状态变化。
        public static event Action OnBossBreakWindowStart;
        public static void BossBreakWindowStart() => OnBossBreakWindowStart?.Invoke();

// Boss事件声明，用于在运行时向监听方广播关键状态变化。
        public static event Action<BossSpawnPoint> OnBossDefeated;
        public static void BossDefeated(BossSpawnPoint boss) => OnBossDefeated?.Invoke(boss);

// 天赋事件声明，用于在运行时向监听方广播关键状态变化。
        public static event Action<string, int> OnTalentUnlocked;
        public static void TalentUnlocked(string nodeId, int cost) => OnTalentUnlocked?.Invoke(nodeId, cost);

// 系统事件声明，用于在运行时向监听方广播关键状态变化。
        public static event Action<string, ProgressionRoute> OnProgressionMilestoneClaimed;
        public static void ProgressionMilestoneClaimed(string milestoneId, ProgressionRoute route)
            => OnProgressionMilestoneClaimed?.Invoke(milestoneId, route);
        
        #endregion
    }

    /// <summary>
    /// 枚举定义，用于约束状态取值并保持分支语义清晰。
    /// </summary>
    public enum EnemyType
    {
        Grunt, // 敌人级别枚举值，用于驱动不同强度策略。
        Rusher, // 枚举成员，用于分支选择与状态判定。
        Tank, // 枚举成员，用于分支选择与状态判定。
        Elite, // 敌人级别枚举值，用于驱动不同强度策略。
        Mutant, // 枚举成员，用于分支选择与状态判定。
        Boss        // Boss
    }
}
