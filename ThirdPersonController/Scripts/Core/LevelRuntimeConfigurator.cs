using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    public class LevelRuntimeConfigurator : MonoBehaviour
    {
        [Header("Data")]
        public LevelData levelData;
        public ChapterData chapterData;

        [Header("Systems")]
        public LevelFlowController levelFlow;
        public StrongholdSequenceController sequenceController;
        public QuestSystem questSystem;
        public QuestDatabase questDatabase;
        public LevelRewardSystem rewardSystem;
        public ProgressionRewardSystem progressionRewardSystem;
        public EconomyConfig economyConfig;
        public BossSpawnPoint bossSpawnPoint;
        public bool ensureShopAndConsumables = true;
        public bool ensureEconomyUI = true;
        public bool ensureHudHints = true;

        [Header("Apply")]
        public bool autoApplyOnAwake = true;
        public bool applyStrongholds = true;
        public bool includeOptionalStrongholds = false;
        public bool applyQuests = true;
        public bool applyRewards = true;

        private void Awake()
        {
            if (autoApplyOnAwake)
            {
                Apply();
            }
        }

        public void Apply()
        {
            ResolveReferences();

            if (levelFlow != null)
            {
                levelData = levelData != null ? levelData : levelFlow.levelData;
                chapterData = chapterData != null ? chapterData : levelFlow.chapterData;
            }

            if (applyStrongholds)
            {
                ConfigureStrongholds();
            }

            ConfigureStrongholdOverrides();

            if (applyQuests)
            {
                ConfigureQuests();
            }

            if (applyRewards)
            {
                ConfigureRewards();
            }

            ConfigureBoss();
            ConfigureEconomy();
            EnsureEconomyRuntime();
        }

        private void ResolveReferences()
        {
            if (levelFlow == null)
            {
                levelFlow = FindObjectOfType<LevelFlowController>();
            }

            if (sequenceController == null)
            {
                sequenceController = FindObjectOfType<StrongholdSequenceController>();
            }

            if (questSystem == null)
            {
                questSystem = FindObjectOfType<QuestSystem>();
                if (questSystem == null)
                {
                    GameObject questObject = new GameObject("QuestSystem");
                    questSystem = questObject.AddComponent<QuestSystem>();
                }
            }

            if (questDatabase == null)
            {
                if (levelFlow != null && levelFlow.questDatabase != null)
                {
                    questDatabase = levelFlow.questDatabase;
                }
                else
                {
                    questDatabase = FindObjectOfType<QuestDatabase>();
                }
            }

            if (questSystem != null)
            {
                questSystem.questDatabase = questDatabase;
            }

            if (rewardSystem == null)
            {
                rewardSystem = FindObjectOfType<LevelRewardSystem>();
            }

            if (progressionRewardSystem == null)
            {
                progressionRewardSystem = FindObjectOfType<ProgressionRewardSystem>();
            }

            if (economyConfig == null)
            {
                economyConfig = FindObjectOfType<EconomyConfig>();
            }

            if (bossSpawnPoint == null)
            {
                bossSpawnPoint = FindObjectOfType<BossSpawnPoint>();
            }
        }

        private void ConfigureBoss()
        {
            if (levelData == null || !levelData.overrideBossSettings)
            {
                return;
            }

            if (bossSpawnPoint == null)
            {
                bossSpawnPoint = FindObjectOfType<BossSpawnPoint>();
            }

            if (bossSpawnPoint == null)
            {
                Debug.LogWarning("BossSpawnPoint not found. Skipping boss configuration.");
                return;
            }

            bossSpawnPoint.prototype = levelData.bossPrototype;
            bossSpawnPoint.bossName = string.IsNullOrEmpty(levelData.bossName) ? bossSpawnPoint.bossName : levelData.bossName;
            bossSpawnPoint.maxHealth = Mathf.Max(1, levelData.bossMaxHealth);
            bossSpawnPoint.baseDamage = Mathf.Max(1, levelData.bossBaseDamage);
            bossSpawnPoint.knockback = levelData.bossKnockback;
            bossSpawnPoint.scaleMultiplier = Mathf.Max(0.1f, levelData.bossScaleMultiplier);
            bossSpawnPoint.spawnOffset = levelData.bossSpawnOffset;
            bossSpawnPoint.spawnOnStart = true;
        }

        private void ConfigureStrongholds()
        {
            if (sequenceController == null || levelData == null || levelData.strongholds == null || levelData.strongholds.Count == 0)
            {
                return;
            }

            StrongholdController[] allStrongholds = FindObjectsOfType<StrongholdController>();
            Dictionary<string, StrongholdController> lookup = new Dictionary<string, StrongholdController>();
            for (int i = 0; i < allStrongholds.Length; i++)
            {
                StrongholdController stronghold = allStrongholds[i];
                if (stronghold == null)
                {
                    continue;
                }

                string id = stronghold.StrongholdId;
                if (!lookup.ContainsKey(id))
                {
                    lookup.Add(id, stronghold);
                }
            }

            List<StrongholdConfig> configs = new List<StrongholdConfig>(levelData.strongholds);
            configs.Sort((a, b) => a.order.CompareTo(b.order));

            List<StrongholdController> orderedStrongholds = new List<StrongholdController>();
            for (int i = 0; i < configs.Count; i++)
            {
                StrongholdConfig config = configs[i];
                if (config == null)
                {
                    continue;
                }

                if (!config.required && !includeOptionalStrongholds)
                {
                    continue;
                }

                if (!lookup.TryGetValue(config.strongholdId, out StrongholdController stronghold))
                {
                    Debug.LogWarning($"Stronghold not found for id: {config.strongholdId}");
                    continue;
                }

                orderedStrongholds.Add(stronghold);
            }

            if (orderedStrongholds.Count > 0)
            {
                sequenceController.ConfigureStrongholds(orderedStrongholds);
                int resolvedId = ResolveLevelId();
                if (resolvedId > 0)
                {
                    sequenceController.levelId = resolvedId;
                }
            }
        }

        private void ConfigureStrongholdOverrides()
        {
            if (levelData == null || levelData.strongholdOverrides == null || levelData.strongholdOverrides.Count == 0)
            {
                return;
            }

            StrongholdController[] allStrongholds = FindObjectsOfType<StrongholdController>();
            Dictionary<string, StrongholdController> lookup = new Dictionary<string, StrongholdController>();
            for (int i = 0; i < allStrongholds.Length; i++)
            {
                StrongholdController stronghold = allStrongholds[i];
                if (stronghold == null)
                {
                    continue;
                }

                string id = stronghold.StrongholdId;
                if (!lookup.ContainsKey(id))
                {
                    lookup.Add(id, stronghold);
                }
            }

            for (int i = 0; i < levelData.strongholdOverrides.Count; i++)
            {
                StrongholdOverride overrideData = levelData.strongholdOverrides[i];
                if (overrideData == null || string.IsNullOrEmpty(overrideData.strongholdId))
                {
                    continue;
                }

                if (!lookup.TryGetValue(overrideData.strongholdId, out StrongholdController stronghold))
                {
                    Debug.LogWarning($"Stronghold override target not found: {overrideData.strongholdId}");
                    continue;
                }

                ApplyStrongholdOverride(stronghold, overrideData);
            }
        }

        private void ApplyStrongholdOverride(StrongholdController stronghold, StrongholdOverride overrideData)
        {
            if (stronghold == null || overrideData == null || stronghold.waves == null || stronghold.waves.Count == 0)
            {
                return;
            }

            if (overrideData.waves == null || overrideData.waves.Count == 0)
            {
                return;
            }

            for (int i = 0; i < overrideData.waves.Count; i++)
            {
                StrongholdWaveOverride waveOverride = overrideData.waves[i];
                if (waveOverride == null)
                {
                    continue;
                }

                int waveIndex = waveOverride.waveIndex;
                if (waveIndex < 0 || waveIndex >= stronghold.waves.Count)
                {
                    Debug.LogWarning($"Wave override index out of range for {overrideData.strongholdId}: {waveIndex}");
                    continue;
                }

                StrongholdWave wave = stronghold.waves[waveIndex];
                if (wave == null)
                {
                    continue;
                }

                if (waveOverride.replaceEvents || wave.events == null)
                {
                    wave.events = new List<WaveEvent>();
                }

                if (waveOverride.events == null)
                {
                    continue;
                }

                for (int e = 0; e < waveOverride.events.Count; e++)
                {
                    WaveEventOverride eventOverride = waveOverride.events[e];
                    if (eventOverride == null)
                    {
                        continue;
                    }

                    WaveEvent waveEvent = BuildWaveEvent(eventOverride, stronghold, wave, waveIndex);
                    if (waveEvent != null)
                    {
                        wave.events.Add(waveEvent);
                    }
                }
            }
        }

        private WaveEvent BuildWaveEvent(WaveEventOverride overrideData, StrongholdController stronghold, StrongholdWave wave, int waveIndex)
        {
            if (overrideData == null)
            {
                return null;
            }

            WaveEventType eventType = overrideData.eventType;
            WaveEvent waveEvent = new WaveEvent
            {
                name = string.IsNullOrEmpty(overrideData.name) ? eventType.ToString() : overrideData.name,
                eventType = eventType,
                enabled = true,
                triggerDelay = overrideData.triggerDelay,
                triggerOnRemaining = overrideData.triggerOnRemaining,
                duration = overrideData.duration > 0f ? overrideData.duration : GetDefaultEventDuration(eventType, waveIndex),
                spawnInterval = overrideData.spawnInterval > 0f ? overrideData.spawnInterval : GetDefaultSpawnInterval(eventType),
                spawnRadius = overrideData.spawnRadius > 0f ? overrideData.spawnRadius : GetDefaultSpawnRadius(eventType, stronghold),
                useReinforcementPoints = overrideData.useReinforcementPoints,
                holdRadius = overrideData.holdRadius > 0f ? overrideData.holdRadius : GetDefaultHoldRadius(stronghold),
                holdDuration = overrideData.holdDuration > 0f ? overrideData.holdDuration : GetDefaultHoldDuration(waveIndex),
                holdDecayRate = overrideData.holdDecayRate > 0f ? overrideData.holdDecayRate : 1f,
                showHoldMarker = overrideData.showHoldMarker,
                spawnDefenseTarget = overrideData.spawnDefenseTarget,
                defenseTargetHealth = overrideData.defenseTargetHealth > 0 ? overrideData.defenseTargetHealth : GetDefaultDefenseTargetHealth(waveIndex),
                failOnTargetDestroyed = overrideData.failOnTargetDestroyed,
                assignTargetToSpawnedEnemies = overrideData.assignTargetToSpawnedEnemies
            };

            int spawnCount = overrideData.spawnCount > 0 ? overrideData.spawnCount : GetDefaultEventSpawnCount(eventType, waveIndex);
            if (spawnCount > 0)
            {
                GameObject prefab = ResolveEventPrefab(stronghold, wave);
                if (prefab != null)
                {
                    WaveSpawnGroup group = new WaveSpawnGroup
                    {
                        prefab = prefab,
                        count = spawnCount,
                        spawnIntervalOverride = -1f,
                        archetypeOverride = overrideData.archetypeOverride
                    };
                    waveEvent.groups.Add(group);
                }
            }

            return waveEvent;
        }

        private GameObject ResolveEventPrefab(StrongholdController stronghold, StrongholdWave wave)
        {
            if (wave != null && wave.groups != null)
            {
                for (int i = 0; i < wave.groups.Count; i++)
                {
                    WaveSpawnGroup group = wave.groups[i];
                    if (group != null && group.prefab != null)
                    {
                        return group.prefab;
                    }
                }
            }

            if (stronghold != null && stronghold.waves != null)
            {
                for (int w = 0; w < stronghold.waves.Count; w++)
                {
                    StrongholdWave search = stronghold.waves[w];
                    if (search == null || search.groups == null)
                    {
                        continue;
                    }

                    for (int i = 0; i < search.groups.Count; i++)
                    {
                        WaveSpawnGroup group = search.groups[i];
                        if (group != null && group.prefab != null)
                        {
                            return group.prefab;
                        }
                    }
                }
            }

            return null;
        }

        private float GetDefaultEventDuration(WaveEventType eventType, int waveIndex)
        {
            switch (eventType)
            {
                case WaveEventType.Chase:
                    return 8f + waveIndex * 2f;
                case WaveEventType.ProtectTarget:
                    return 8f + waveIndex * 1.5f;
                default:
                    return 6f;
            }
        }

        private float GetDefaultSpawnInterval(WaveEventType eventType)
        {
            switch (eventType)
            {
                case WaveEventType.Chase:
                    return 1.1f;
                case WaveEventType.Reinforcement:
                    return 0.35f;
                case WaveEventType.ProtectTarget:
                    return 0.4f;
                default:
                    return 0.4f;
            }
        }

        private float GetDefaultSpawnRadius(WaveEventType eventType, StrongholdController stronghold)
        {
            float baseRadius = stronghold != null ? Mathf.Max(3f, stronghold.spawnRadius) : 5f;
            return eventType == WaveEventType.Chase ? baseRadius + 2f : baseRadius;
        }

        private float GetDefaultHoldRadius(StrongholdController stronghold)
        {
            return stronghold != null ? Mathf.Max(4f, stronghold.spawnRadius) : 5f;
        }

        private float GetDefaultHoldDuration(int waveIndex)
        {
            return 6f + waveIndex * 2f;
        }

        private int GetDefaultDefenseTargetHealth(int waveIndex)
        {
            return 240 + waveIndex * 40;
        }

        private int GetDefaultEventSpawnCount(WaveEventType eventType, int waveIndex)
        {
            switch (eventType)
            {
                case WaveEventType.Chase:
                    return Mathf.Clamp(1 + waveIndex / 2, 1, 3);
                case WaveEventType.Reinforcement:
                    return Mathf.Clamp(3 + waveIndex, 3, 8);
                case WaveEventType.ProtectTarget:
                    return Mathf.Clamp(4 + waveIndex, 4, 10);
                default:
                    return 0;
            }
        }

        private void ConfigureQuests()
        {
            if (questSystem == null || levelData == null || levelData.quests == null || levelData.quests.Count == 0)
            {
                return;
            }

            if (questDatabase == null)
            {
                Debug.LogWarning("QuestDatabase not found. Skipping quest configuration.");
                return;
            }

            List<QuestConfig> configs = new List<QuestConfig>(levelData.quests);
            configs.Sort((a, b) => a.order.CompareTo(b.order));

            List<QuestData> questsToStart = new List<QuestData>();
            for (int i = 0; i < configs.Count; i++)
            {
                QuestConfig config = configs[i];
                if (config == null)
                {
                    continue;
                }

                QuestData baseQuest = questDatabase.GetQuestById(config.questId);
                if (baseQuest == null)
                {
                    Debug.LogWarning($"Quest not found for id: {config.questId}");
                    continue;
                }

                QuestData questCopy = CloneQuest(baseQuest);
                questCopy.isOptional = !config.required;
                questsToStart.Add(questCopy);
            }

            if (questsToStart.Count > 0)
            {
                questSystem.ResetQuests();
                questSystem.StartQuests(questsToStart);
            }
        }

        private void ConfigureRewards()
        {
            if (levelData == null)
            {
                return;
            }

            if (rewardSystem == null)
            {
                rewardSystem = gameObject.AddComponent<LevelRewardSystem>();
            }

            rewardSystem.levelData = levelData;
            int resolvedId = ResolveLevelId();
            if (resolvedId > 0)
            {
                rewardSystem.levelIdOverride = resolvedId;
            }

            if (rewardSystem.inventory == null)
            {
                PearlDropManager dropManager = FindObjectOfType<PearlDropManager>();
                if (dropManager != null)
                {
                    rewardSystem.inventory = dropManager.inventory;
                }
            }

            if (rewardSystem.pearlDatabase == null)
            {
                PearlDropManager dropManager = FindObjectOfType<PearlDropManager>();
                if (dropManager != null)
                {
                    rewardSystem.pearlDatabase = dropManager.pearlDatabase;
                }

                if (rewardSystem.pearlDatabase == null)
                {
                    StrongholdRewardSystem rewardSystemRef = FindObjectOfType<StrongholdRewardSystem>();
                    if (rewardSystemRef != null)
                    {
                        rewardSystem.pearlDatabase = rewardSystemRef.pearlDatabase;
                    }
                }
            }
        }

        private void ConfigureEconomy()
        {
            if (economyConfig != null)
            {
                EconomyService.Configure(economyConfig);

                if (progressionRewardSystem != null)
                {
                    progressionRewardSystem.killsPerPoint = economyConfig.killsPerTalentPoint;
                    progressionRewardSystem.pointsPerMilestone = economyConfig.pointsPerKillMilestone;
                    progressionRewardSystem.pointsPerStageClear = economyConfig.pointsPerStageClear;
                }
            }

            if (rewardSystem != null)
            {
                rewardSystem.expRewardMultiplier = 1f;
                rewardSystem.pearlRewardMultiplier = 1f;
                if (levelData != null)
                {
                    rewardSystem.levelRewardMultiplier = levelData.levelRewardMultiplier;
                    rewardSystem.levelDifficulty = Mathf.Max(0, (int)levelData.difficulty);
                }
            }

            if (questSystem != null)
            {
                questSystem.expRewardMultiplier = 1f;
                questSystem.pearlRewardMultiplier = 1f;
                if (levelData != null)
                {
                    questSystem.levelRewardMultiplier = levelData.questRewardMultiplier;
                    questSystem.levelDifficulty = Mathf.Max(0, (int)levelData.difficulty);
                    questSystem.levelChapterId = Mathf.Max(1, levelData.chapterId);
                }
            }

            PearlDropManager dropManager = FindObjectOfType<PearlDropManager>();
            if (dropManager != null)
            {
                if (economyConfig != null)
                {
                    dropManager.ApplyEconomyMultiplier(economyConfig.pearlDropMultiplier);
                }
                dropManager.ApplyLevelContext(levelData);
            }
        }

        private void EnsureEconomyRuntime()
        {
            if (!ensureShopAndConsumables)
            {
                return;
            }

            CurrencyWallet.EnsureInstance();
            ConsumableInventory.EnsureInstance();

            if (FindObjectOfType<ConsumableUseSystem>() == null)
            {
                GameObject useSystem = new GameObject("ConsumableUseSystem");
                useSystem.AddComponent<ConsumableUseSystem>();
            }

            if (FindObjectOfType<ShopManager>() == null)
            {
                GameObject shop = new GameObject("ShopManager");
                ShopManager shopManager = shop.AddComponent<ShopManager>();
                if (levelData != null)
                {
                    shopManager.levelDifficulty = Mathf.Max(0, (int)levelData.difficulty);
                }
            }

            if (FindObjectOfType<ConsumableQuickSlots>() == null)
            {
                GameObject quickSlots = new GameObject("ConsumableQuickSlots");
                quickSlots.AddComponent<ConsumableQuickSlots>();
            }

            if (ensureEconomyUI && FindObjectOfType<UI_EconomyOverlay>() == null)
            {
                GameObject ui = new GameObject("UI_EconomyOverlay");
                ui.AddComponent<UI_EconomyOverlay>();
            }

            if (ensureHudHints && FindObjectOfType<UI_HudHints>() == null)
            {
                GameObject hints = new GameObject("UI_HudHints");
                hints.AddComponent<UI_HudHints>();
            }
        }

        private int ResolveLevelId()
        {
            if (levelData == null || levelData.chapterId <= 0)
            {
                return levelFlow != null ? levelFlow.levelId : 0;
            }

            if (!string.IsNullOrEmpty(levelData.levelId) && levelData.levelId.StartsWith("LEVEL_"))
            {
                if (int.TryParse(levelData.levelId.Replace("LEVEL_", string.Empty), out int parsed))
                {
                    return levelData.chapterId * 100 + parsed;
                }
            }

            return levelFlow != null ? levelFlow.levelId : 0;
        }

        private QuestData CloneQuest(QuestData quest)
        {
            List<QuestStage> stages = new List<QuestStage>();
            if (quest.stages != null)
            {
                for (int i = 0; i < quest.stages.Count; i++)
                {
                    QuestStage stage = quest.stages[i];
                    if (stage == null)
                    {
                        continue;
                    }

                    stages.Add(new QuestStage
                    {
                        stageId = stage.stageId,
                        title = stage.title,
                        description = stage.description,
                        questType = stage.questType,
                        targetCount = stage.targetCount,
                        targetEnemyType = stage.targetEnemyType,
                        targetTime = stage.targetTime,
                        targetLocationId = stage.targetLocationId,
                        useTimeLimit = stage.useTimeLimit,
                        timeLimit = stage.timeLimit
                    });
                }
            }

            return new QuestData
            {
                questId = quest.questId,
                questName = quest.questName,
                description = quest.description,
                questType = quest.questType,
                targetCount = quest.targetCount,
                targetEnemyType = quest.targetEnemyType,
                targetTime = quest.targetTime,
                targetLocationId = quest.targetLocationId,
                stages = stages,
                nextQuestIds = new List<string>(quest.nextQuestIds),
                autoStartNextQuests = quest.autoStartNextQuests,
                timeLimit = quest.timeLimit,
                failOnPlayerDeath = quest.failOnPlayerDeath,
                failOnGameOver = quest.failOnGameOver,
                failOnDefenseTargetDestroyed = quest.failOnDefenseTargetDestroyed,
                reward = new QuestReward
                {
                    exp = quest.reward.exp,
                    pearls = quest.reward.pearls,
                    credits = quest.reward.credits,
                    itemIds = new List<string>(quest.reward.itemIds)
                },
                difficultyRating = quest.difficultyRating,
                isOptional = quest.isOptional
            };
        }
    }
}
