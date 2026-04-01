using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    public enum LevelDifficulty
    {
        Easy,
        Normal,
        Hard,
        Nightmare
    }

    public enum LevelType
    {
        Campaign,
        Survival,
        Challenge,
        Tutorial
    }

    [CreateAssetMenu(fileName = "LevelData_", menuName = "Progression/Level Data")]
    public class LevelData : ScriptableObject
    {
        [Header("Basic Info")]
        public string levelId = "";
        public string levelName = "New Level";
        [TextArea(2, 4)]
        public string description;
        public int chapterId = 1;
        
        [Header("Difficulty")]
        public LevelDifficulty difficulty = LevelDifficulty.Normal;
        public int recommendedLevel = 1;
        public int recommendedPower = 100;
        
        [Header("Type")]
        public LevelType levelType = LevelType.Campaign;
        
        [Header("Limits")]
        public float timeLimit = 0f;
        public int scoreTarget = 0;
        
        [Header("Strongholds")]
        public List<StrongholdConfig> strongholds = new List<StrongholdConfig>();
        
        [Header("Quests")]
        public List<QuestConfig> quests = new List<QuestConfig>();

        [Header("Stronghold Overrides")]
        public List<StrongholdOverride> strongholdOverrides = new List<StrongholdOverride>();
        
        [Header("Unlocks")]
        public string nextLevelId = "";
        public int starsRequired = 1;
        public int unlockCost = 0;
        
        [Header("Rewards")]
        public int baseExp = 100;
        public int basePearls = 1;
        public int baseCredits = 0;
        public float levelRewardMultiplier = 1f;
        public float questRewardMultiplier = 1f;
        public float dropChanceMultiplier = 1f;
        
        [Header("Scene")]
        public string sceneName = "";

        [Header("Boss")]
        public bool overrideBossSettings = false;
        public string bossName = "Boss";
        public BossPrototypeType bossPrototype = BossPrototypeType.Eel;
        public int bossMaxHealth = 3000;
        public int bossBaseDamage = 25;
        public float bossKnockback = 6f;
        public float bossScaleMultiplier = 2.2f;
        public Vector3 bossSpawnOffset = Vector3.zero;

        [Header("Boss Encounter")]
        public bool overrideBossEncounterTuning = false;
        public float bossPhase2HealthThreshold = 0.66f;
        public float bossPhase3HealthThreshold = 0.33f;
        public float bossBreakWindowDuration = 4f;
        public float bossBreakWindowCooldown = 12f;
        public float bossBreakWindowDamageMultiplier = 1.6f;
        public float bossStaggerMax = 120f;
        public float bossStaggerPerDamage = 1f;
        public float bossAttackInterval = 3.2f;
        public float bossDecisionInterval = 0.78f;
        public int bossQueuedAttackLimit = 3;
        public float bossImmediateRepeatPenalty = 0.32f;
        public bool bossEnablePostBreakPunishWindow = true;
        public float bossPostBreakPunishDuration = 5f;
        public float bossPostBreakAttackIntervalMultiplier = 0.75f;
        public float bossPostBreakDecisionIntervalMultiplier = 0.82f;
        public float bossPostBreakChaseSpeedMultiplier = 1.15f;
        public bool bossEnablePhaseComboChain = true;
        public float bossPhase2ComboChance = 0.45f;
        public float bossPhase3ComboChance = 0.65f;
        public float bossComboStartDelay = 0.08f;
        public float bossComboRepeatPenalty = 0.35f;
        public bool bossEnableInterruptRecoveryGate = true;
        public float bossInterruptRecoveryDuration = 0.2f;
        public float bossInterruptedAttackCooldownScale = 0.45f;
        public bool bossEnableTimePressure = true;
        public float bossTimePressureDelay = 75f;
        public float bossTimePressureRampDuration = 60f;
        public float bossMaxTimePressureDamageMultiplier = 1.35f;
        public float bossMaxTimePressureSpeedMultiplier = 1.2f;

        [Header("Boss Encounter Choreography")]
        public bool bossEnablePhaseTransitionOpeners = true;
        public string bossPhase2TransitionOpenerId = "";
        public string bossPhase3TransitionOpenerId = "";
        public bool bossEnablePhaseTransitionOpenerRetry = true;
        public float bossPhaseTransitionOpenerRetryDelay = 0.12f;
        public int bossPhaseTransitionOpenerMaxRetries = 3;
        public bool bossEnablePhaseTransitionFollowupChain = false;
        public string bossPhase2TransitionFollowupId = "";
        public string bossPhase3TransitionFollowupId = "";
        public bool bossEnablePhaseTransitionFollowupRetry = true;
        public float bossPhaseTransitionFollowupRetryDelay = 0.12f;
        public int bossPhaseTransitionFollowupMaxRetries = 2;
        public bool bossEnablePhase3SpecialPriorityWindow = true;
        public float bossPhase3SpecialPriorityDuration = 6f;
        public float bossPhase3SpecialPriorityWeightMultiplier = 1.7f;
        public bool bossForceSpecialQueueDuringPhase3Priority = true;
        public bool bossEnablePhaseIntentStyle = true;
        public BossPhaseIntentStyle bossPhase1IntentStyle = BossPhaseIntentStyle.Balanced;
        public BossPhaseIntentStyle bossPhase2IntentStyle = BossPhaseIntentStyle.PressureClose;
        public BossPhaseIntentStyle bossPhase3IntentStyle = BossPhaseIntentStyle.SpecialBurst;
        public float bossCloseRangeIntentThreshold = 4f;
        public float bossIntentCloseWeightBoost = 1.45f;
        public float bossIntentRangedWeightBoost = 1.35f;
        public float bossIntentAoeWeightBoost = 1.25f;
        public float bossIntentSpecialWeightBoost = 1.55f;
        public float bossIntentFastDecisionMultiplier = 0.88f;
        public float bossIntentSlowDecisionMultiplier = 1.14f;
        
        public string GetId()
        {
            if (!string.IsNullOrEmpty(levelId))
            {
                return levelId;
            }
            return name;
        }
    }

    [System.Serializable]
    public class StrongholdOverride
    {
        public string strongholdId = "";
        public List<StrongholdWaveOverride> waves = new List<StrongholdWaveOverride>();
    }

    [System.Serializable]
    public class StrongholdWaveOverride
    {
        public int waveIndex = 0;
        public bool replaceEvents = true;
        public List<WaveEventOverride> events = new List<WaveEventOverride>();
    }

    [System.Serializable]
    public class WaveEventOverride
    {
        public string name = "";
        public WaveEventType eventType = WaveEventType.Reinforcement;
        public float triggerDelay = 0.4f;
        public int triggerOnRemaining = -1;
        public float duration = 0f;
        public float spawnInterval = 0f;
        public float spawnRadius = 0f;
        public bool useReinforcementPoints = true;
        public int spawnCount = 0;
        public EnemyArchetype archetypeOverride;
        public float holdRadius = 0f;
        public float holdDuration = 0f;
        public float holdDecayRate = 1f;
        public bool showHoldMarker = true;
        public bool spawnDefenseTarget = true;
        public int defenseTargetHealth = 0;
        public bool failOnTargetDestroyed = true;
        public bool assignTargetToSpawnedEnemies = true;
    }

    [System.Serializable]
    public class StrongholdConfig
    {
        public string strongholdId = "";
        public bool required = true;
        public int order = 0;
    }

    [System.Serializable]
    public class QuestConfig
    {
        public string questId = "";
        public bool required = false;
        public int order = 0;
    }
}
