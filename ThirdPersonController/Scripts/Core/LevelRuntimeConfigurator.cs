using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        [Header("Runtime Wiring")]
        public bool ensureRuntimeWiring = true;
        public bool ensurePlayerExperienceSystem = true;
        public bool ensurePlayerSkillManager = true;
        public bool ensureQuestTracker = true;
        public bool ensureStrongholdWavePanel = true;
        public bool ensureRewardSaveWriteback = true;

        [Header("Runtime References")]
        public GameObject playerObject;
        public PlayerExperienceSystem experienceSystem;
        public SkillManager skillManager;
        public UI_QuestTracker questTracker;
        public UI_StrongholdWavePanel strongholdWavePanel;
        public ProgressionSaveBridge progressionSaveBridge;

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
            if (ensureRuntimeWiring)
            {
                EnsurePlayerRuntime();
            }

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

            bool restoredQuestState = false;
            if (applyQuests && questSystem != null)
            {
                restoredQuestState = questSystem.RestoreQuestRuntimeStateFromSave(false, false);
                if (!restoredQuestState)
                {
                    questSystem.SaveQuestRuntimeStateToData();
                }
            }

            if (applyRewards)
            {
                ConfigureRewards();
            }

            if (ensureRuntimeWiring)
            {
                EnsureUiRuntime();
                BindRuntimeReferences();
            }

            ConfigureBoss();
            ConfigureEconomy();
            EnsureEconomyRuntime();
        }

        private void ResolveReferences()
        {
            if (levelFlow == null)
            {
                levelFlow = FindComponentInOwningScene<LevelFlowController>();
            }

            if (sequenceController == null)
            {
                sequenceController = FindComponentInOwningScene<StrongholdSequenceController>();
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
                bossSpawnPoint = FindComponentInOwningScene<BossSpawnPoint>();
            }
        }

        private void EnsurePlayerRuntime()
        {
            playerObject = ResolvePlayerObject();
            if (playerObject == null)
            {
                return;
            }

            if (ensurePlayerExperienceSystem)
            {
                experienceSystem = EnsureComponent<PlayerExperienceSystem>(playerObject);
                if (experienceSystem.talentTree == null)
                {
                    experienceSystem.talentTree = EnsureTalentTree();
                }
            }
            else if (experienceSystem == null)
            {
                experienceSystem = FindObjectOfType<PlayerExperienceSystem>();
            }

            if (ensurePlayerSkillManager)
            {
                EnsureComponent<PlayerInputHandler>(playerObject);
                EnsureComponent<PlayerInputBuffer>(playerObject);
                PlayerActionController actionController = EnsureComponent<PlayerActionController>(playerObject);
                SkillTimelineController timelineController = EnsureComponent<SkillTimelineController>(playerObject);

                skillManager = EnsureComponent<SkillManager>(playerObject);
                skillManager.playerTransform = playerObject.transform;
                skillManager.staminaSystem = playerObject.GetComponent<StaminaSystem>();
                skillManager.inputHandler = playerObject.GetComponent<PlayerInputHandler>();
                skillManager.inputBuffer = playerObject.GetComponent<PlayerInputBuffer>();
                skillManager.actionController = actionController;
                skillManager.timelineController = timelineController;
            }
            else if (skillManager == null)
            {
                skillManager = FindObjectOfType<SkillManager>();
            }

            EnsureProgressionSaveBridge(playerObject);
        }

        private void EnsureUiRuntime()
        {
            if (ensureQuestTracker)
            {
                if (questTracker == null)
                {
                    questTracker = FindObjectOfType<UI_QuestTracker>();
                }

                if (questTracker == null)
                {
                    GameObject trackerObject = new GameObject("UI_QuestTracker");
                    questTracker = trackerObject.AddComponent<UI_QuestTracker>();
                }
            }

            if (ensureStrongholdWavePanel)
            {
                if (strongholdWavePanel == null)
                {
                    strongholdWavePanel = FindObjectOfType<UI_StrongholdWavePanel>();
                }

                if (strongholdWavePanel == null)
                {
                    GameObject panelObject = new GameObject("UI_StrongholdWavePanel");
                    strongholdWavePanel = panelObject.AddComponent<UI_StrongholdWavePanel>();
                }
            }
        }

        private void BindRuntimeReferences()
        {
            if (experienceSystem == null)
            {
                experienceSystem = FindObjectOfType<PlayerExperienceSystem>();
            }

            if (questSystem != null)
            {
                questSystem.questDatabase = questDatabase;
                questSystem.BindExperienceSystem(experienceSystem);
                if (questSystem.inventory == null)
                {
                    questSystem.inventory = EnsurePearlInventory();
                }

                if (questSystem.pearlDatabase == null)
                {
                    questSystem.pearlDatabase = FindPearlDatabase();
                }

                if (questSystem.wallet == null)
                {
                    questSystem.wallet = EnsureWallet();
                }

                questSystem.autoSaveOnQuestComplete = ensureRewardSaveWriteback;
            }

            if (rewardSystem != null)
            {
                if (rewardSystem.experienceSystem == null)
                {
                    rewardSystem.experienceSystem = experienceSystem;
                }

                if (rewardSystem.inventory == null)
                {
                    rewardSystem.inventory = EnsurePearlInventory();
                }

                if (rewardSystem.pearlDatabase == null)
                {
                    rewardSystem.pearlDatabase = FindPearlDatabase();
                }

                if (rewardSystem.wallet == null)
                {
                    rewardSystem.wallet = EnsureWallet();
                }

                rewardSystem.autoSaveOnReward = ensureRewardSaveWriteback;
            }

            if (progressionRewardSystem != null && progressionRewardSystem.talentTree == null)
            {
                progressionRewardSystem.talentTree = EnsureTalentTree();
            }

            if (questTracker != null)
            {
                questTracker.questSystem = questSystem;
            }

            if (strongholdWavePanel != null)
            {
                strongholdWavePanel.sequenceController = sequenceController;
            }

            if (skillManager != null && playerObject != null)
            {
                skillManager.playerTransform = playerObject.transform;
                skillManager.staminaSystem = playerObject.GetComponent<StaminaSystem>();
                skillManager.inputHandler = playerObject.GetComponent<PlayerInputHandler>();
                skillManager.inputBuffer = playerObject.GetComponent<PlayerInputBuffer>();
                skillManager.actionController = playerObject.GetComponent<PlayerActionController>();
                skillManager.timelineController = playerObject.GetComponent<SkillTimelineController>();
            }

            if (progressionSaveBridge != null)
            {
                if (progressionSaveBridge.inventory == null)
                {
                    progressionSaveBridge.inventory = EnsurePearlInventory();
                }

                if (progressionSaveBridge.equipment == null)
                {
                    progressionSaveBridge.equipment = EnsurePearlEquipment();
                }

                if (progressionSaveBridge.talentTree == null)
                {
                    progressionSaveBridge.talentTree = EnsureTalentTree();
                }

                if (progressionSaveBridge.experienceSystem == null)
                {
                    progressionSaveBridge.experienceSystem = experienceSystem;
                }

                if (progressionSaveBridge.pearlDatabase == null)
                {
                    progressionSaveBridge.pearlDatabase = FindPearlDatabase();
                }
            }
        }

        private GameObject ResolvePlayerObject()
        {
            if (playerObject != null)
            {
                return playerObject;
            }

            PlayerCombat combat = FindObjectOfType<PlayerCombat>();
            if (combat != null)
            {
                return combat.gameObject;
            }

            PlayerHealth health = FindObjectOfType<PlayerHealth>();
            if (health != null)
            {
                return health.gameObject;
            }

            GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
            return taggedPlayer;
        }

        private void EnsureProgressionSaveBridge(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (progressionSaveBridge == null)
            {
                progressionSaveBridge = FindObjectOfType<ProgressionSaveBridge>();
            }

            if (progressionSaveBridge == null)
            {
                progressionSaveBridge = target.GetComponent<ProgressionSaveBridge>();
            }

            if (progressionSaveBridge == null)
            {
                progressionSaveBridge = target.AddComponent<ProgressionSaveBridge>();
            }
        }

        private TalentTree EnsureTalentTree()
        {
            TalentTree tree = FindObjectOfType<TalentTree>();
            if (tree == null && playerObject != null)
            {
                tree = EnsureComponent<TalentTree>(playerObject);
            }

            return tree;
        }

        private PearlInventory EnsurePearlInventory()
        {
            PearlInventory inventory = FindObjectOfType<PearlInventory>();
            if (inventory == null && playerObject != null)
            {
                inventory = EnsureComponent<PearlInventory>(playerObject);
            }

            return inventory;
        }

        private PearlEquipment EnsurePearlEquipment()
        {
            PearlEquipment equipment = FindObjectOfType<PearlEquipment>();
            if (equipment == null && playerObject != null)
            {
                equipment = EnsureComponent<PearlEquipment>(playerObject);
            }

            return equipment;
        }

        private PearlDatabase FindPearlDatabase()
        {
            PearlDatabase database = FindObjectOfType<PearlDatabase>();
            if (database != null)
            {
                return database;
            }

            PearlDropManager dropManager = FindObjectOfType<PearlDropManager>();
            if (dropManager != null && dropManager.pearlDatabase != null)
            {
                return dropManager.pearlDatabase;
            }

            return Resources.Load<PearlDatabase>("PearlDatabase");
        }

        private CurrencyWallet EnsureWallet()
        {
            CurrencyWallet wallet = FindObjectOfType<CurrencyWallet>();
            if (wallet == null)
            {
                wallet = CurrencyWallet.EnsureInstance();
            }

            return wallet;
        }

        private static T EnsureComponent<T>(GameObject target) where T : Component
        {
            if (target == null)
            {
                return null;
            }

            T component = target.GetComponent<T>();
            if (component == null)
            {
                component = target.AddComponent<T>();
            }

            return component;
        }

        private void ConfigureBoss()
        {
            if (levelData == null || !levelData.overrideBossSettings)
            {
                return;
            }

            if (bossSpawnPoint == null)
            {
                bossSpawnPoint = FindComponentInOwningScene<BossSpawnPoint>();
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
            bossSpawnPoint.spawnOnStart = false;
            bossSpawnPoint.overrideEncounterTuning = levelData.overrideBossEncounterTuning;
            if (levelData.overrideBossEncounterTuning)
            {
                bossSpawnPoint.phase2HealthThreshold = levelData.bossPhase2HealthThreshold;
                bossSpawnPoint.phase3HealthThreshold = levelData.bossPhase3HealthThreshold;
                bossSpawnPoint.breakWindowDuration = levelData.bossBreakWindowDuration;
                bossSpawnPoint.breakWindowCooldown = levelData.bossBreakWindowCooldown;
                bossSpawnPoint.breakWindowDamageMultiplier = levelData.bossBreakWindowDamageMultiplier;
                bossSpawnPoint.staggerMax = levelData.bossStaggerMax;
                bossSpawnPoint.staggerPerDamage = levelData.bossStaggerPerDamage;
                bossSpawnPoint.attackInterval = levelData.bossAttackInterval;
                bossSpawnPoint.decisionInterval = levelData.bossDecisionInterval;
                bossSpawnPoint.queuedAttackLimit = levelData.bossQueuedAttackLimit;
                bossSpawnPoint.immediateRepeatPenalty = levelData.bossImmediateRepeatPenalty;
                bossSpawnPoint.enablePostBreakPunishWindow = levelData.bossEnablePostBreakPunishWindow;
                bossSpawnPoint.postBreakPunishDuration = levelData.bossPostBreakPunishDuration;
                bossSpawnPoint.postBreakAttackIntervalMultiplier = levelData.bossPostBreakAttackIntervalMultiplier;
                bossSpawnPoint.postBreakDecisionIntervalMultiplier = levelData.bossPostBreakDecisionIntervalMultiplier;
                bossSpawnPoint.postBreakChaseSpeedMultiplier = levelData.bossPostBreakChaseSpeedMultiplier;
                bossSpawnPoint.enablePhaseComboChain = levelData.bossEnablePhaseComboChain;
                bossSpawnPoint.phase2ComboChance = levelData.bossPhase2ComboChance;
                bossSpawnPoint.phase3ComboChance = levelData.bossPhase3ComboChance;
                bossSpawnPoint.comboStartDelay = levelData.bossComboStartDelay;
                bossSpawnPoint.comboRepeatPenalty = levelData.bossComboRepeatPenalty;
                bossSpawnPoint.enableInterruptRecoveryGate = levelData.bossEnableInterruptRecoveryGate;
                bossSpawnPoint.interruptRecoveryDuration = levelData.bossInterruptRecoveryDuration;
                bossSpawnPoint.interruptedAttackCooldownScale = levelData.bossInterruptedAttackCooldownScale;
                bossSpawnPoint.enableTimePressure = levelData.bossEnableTimePressure;
                bossSpawnPoint.timePressureDelay = levelData.bossTimePressureDelay;
                bossSpawnPoint.timePressureRampDuration = levelData.bossTimePressureRampDuration;
                bossSpawnPoint.maxTimePressureDamageMultiplier = levelData.bossMaxTimePressureDamageMultiplier;
                bossSpawnPoint.maxTimePressureSpeedMultiplier = levelData.bossMaxTimePressureSpeedMultiplier;
                bossSpawnPoint.enablePhaseTransitionOpeners = levelData.bossEnablePhaseTransitionOpeners;
                bossSpawnPoint.phase2TransitionOpenerId = levelData.bossPhase2TransitionOpenerId;
                bossSpawnPoint.phase3TransitionOpenerId = levelData.bossPhase3TransitionOpenerId;
                bossSpawnPoint.enablePhaseTransitionOpenerRetry = levelData.bossEnablePhaseTransitionOpenerRetry;
                bossSpawnPoint.phaseTransitionOpenerRetryDelay = levelData.bossPhaseTransitionOpenerRetryDelay;
                bossSpawnPoint.phaseTransitionOpenerMaxRetries = levelData.bossPhaseTransitionOpenerMaxRetries;
                bossSpawnPoint.enablePhaseTransitionFollowupChain = levelData.bossEnablePhaseTransitionFollowupChain;
                bossSpawnPoint.phase2TransitionFollowupId = levelData.bossPhase2TransitionFollowupId;
                bossSpawnPoint.phase3TransitionFollowupId = levelData.bossPhase3TransitionFollowupId;
                bossSpawnPoint.enablePhaseTransitionFollowupRetry = levelData.bossEnablePhaseTransitionFollowupRetry;
                bossSpawnPoint.phaseTransitionFollowupRetryDelay = levelData.bossPhaseTransitionFollowupRetryDelay;
                bossSpawnPoint.phaseTransitionFollowupMaxRetries = levelData.bossPhaseTransitionFollowupMaxRetries;
                bossSpawnPoint.enablePhase3SpecialPriorityWindow = levelData.bossEnablePhase3SpecialPriorityWindow;
                bossSpawnPoint.phase3SpecialPriorityDuration = levelData.bossPhase3SpecialPriorityDuration;
                bossSpawnPoint.phase3SpecialPriorityWeightMultiplier = levelData.bossPhase3SpecialPriorityWeightMultiplier;
                bossSpawnPoint.forceSpecialQueueDuringPhase3Priority = levelData.bossForceSpecialQueueDuringPhase3Priority;
            }

            if (sequenceController == null)
            {
                sequenceController = FindComponentInOwningScene<StrongholdSequenceController>();
            }

            if (sequenceController != null)
            {
                sequenceController.ConfigureBossGate(true, bossSpawnPoint);
            }
            else
            {
                Debug.LogWarning("StrongholdSequenceController not found. Boss gate wiring skipped.");
            }
        }

        private void ConfigureStrongholds()
        {
            if (sequenceController == null || levelData == null || levelData.strongholds == null || levelData.strongholds.Count == 0)
            {
                return;
            }

            StrongholdController[] allStrongholds = FindComponentsInOwningScene<StrongholdController>();
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

        private T FindComponentInOwningScene<T>() where T : Component
        {
            Scene owningScene = gameObject != null ? gameObject.scene : default;
            T[] all = FindObjectsOfType<T>(true);
            for (int i = 0; i < all.Length; i++)
            {
                T component = all[i];
                if (component == null)
                {
                    continue;
                }

                if (owningScene.IsValid() && component.gameObject.scene != owningScene)
                {
                    continue;
                }

                return component;
            }

            return null;
        }

        private T[] FindComponentsInOwningScene<T>() where T : Component
        {
            Scene owningScene = gameObject != null ? gameObject.scene : default;
            T[] all = FindObjectsOfType<T>(true);
            if (!owningScene.IsValid())
            {
                return all;
            }

            List<T> filtered = new List<T>(all.Length);
            for (int i = 0; i < all.Length; i++)
            {
                T component = all[i];
                if (component != null && component.gameObject.scene == owningScene)
                {
                    filtered.Add(component);
                }
            }

            return filtered.ToArray();
        }

        private void ConfigureStrongholdOverrides()
        {
            if (levelData == null || levelData.strongholdOverrides == null || levelData.strongholdOverrides.Count == 0)
            {
                return;
            }

            StrongholdController[] allStrongholds = FindComponentsInOwningScene<StrongholdController>();
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

            questSystem.SuspendQuestStateSave(true);
            try
            {
                questSystem.ResetQuests();
                questSystem.availableQuests = questsToStart;
                if (questsToStart.Count > 0)
                {
                    questSystem.StartQuests(questsToStart);
                }
            }
            finally
            {
                questSystem.SuspendQuestStateSave(false);
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
                        targetStrongholdId = stage.targetStrongholdId,
                        targetBossId = stage.targetBossId,
                        matchAnyWaveEventType = stage.matchAnyWaveEventType,
                        targetWaveEventType = stage.targetWaveEventType,
                        useTimeLimit = stage.useTimeLimit,
                        timeLimit = stage.timeLimit
                    });
                }
            }

            QuestReward sourceReward = quest.reward ?? new QuestReward();
            List<string> sourceItemIds = sourceReward.itemIds != null
                ? sourceReward.itemIds
                : new List<string>();
            List<string> sourceNextQuestIds = quest.nextQuestIds != null
                ? quest.nextQuestIds
                : new List<string>();

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
                targetStrongholdId = quest.targetStrongholdId,
                targetBossId = quest.targetBossId,
                matchAnyWaveEventType = quest.matchAnyWaveEventType,
                targetWaveEventType = quest.targetWaveEventType,
                stages = stages,
                nextQuestIds = new List<string>(sourceNextQuestIds),
                autoStartNextQuests = quest.autoStartNextQuests,
                timeLimit = quest.timeLimit,
                failOnPlayerDeath = quest.failOnPlayerDeath,
                failOnGameOver = quest.failOnGameOver,
                failOnDefenseTargetDestroyed = quest.failOnDefenseTargetDestroyed,
                reward = new QuestReward
                {
                    exp = sourceReward.exp,
                    pearls = sourceReward.pearls,
                    credits = sourceReward.credits,
                    itemIds = new List<string>(sourceItemIds)
                },
                difficultyRating = quest.difficultyRating,
                isOptional = quest.isOptional,
                rewardTier = quest.rewardTier
            };
        }
    }
}
