using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    public enum QuestType
    {
        Kill,
        KillEnemyType,
        Survive,
        Protect,
        Collect,
        Reach,
        Combo,
        CompleteWave,
        CompleteStronghold,
        CompleteWaveEvent,
        BossBreak,
        BossDefeat
    }

    public enum QuestRewardTier
    {
        Mainline,
        Side,
        Challenge
    }

    public enum QuestStatus
    {
        Locked,
        Available,
        InProgress,
        Completed,
        Failed
    }

    [System.Serializable]
    public class QuestReward
    {
        public int exp = 50;
        public int pearls = 0;
        public int credits = 0;
        public List<string> itemIds = new List<string>();
    }

    [System.Serializable]
    public class QuestStage
    {
        public string stageId = "";
        public string title = "Stage";
        [TextArea(2, 3)]
        public string description;
        public QuestType questType = QuestType.Kill;

        [Header("Targets")]
        public int targetCount = 10;
        public EnemyType targetEnemyType = EnemyType.Grunt;
        public float targetTime = 60f;
        public string targetLocationId = "";
        public string targetStrongholdId = "";
        public string targetBossId = "";
        public bool matchAnyWaveEventType = true;
        public WaveEventType targetWaveEventType = WaveEventType.Reinforcement;

        [Header("Failure")]
        public bool useTimeLimit = false;
        public float timeLimit = 0f;
    }

    [System.Serializable]
    public class QuestData
    {
        public string questId = "";
        public string questName = "New Quest";
        [TextArea(2, 3)]
        public string description;
        public QuestType questType = QuestType.Kill;
        
        [Header("Targets")]
        public int targetCount = 10;
        public EnemyType targetEnemyType = EnemyType.Grunt;
        public float targetTime = 60f;
        public string targetLocationId = "";
        public string targetStrongholdId = "";
        public string targetBossId = "";
        public bool matchAnyWaveEventType = true;
        public WaveEventType targetWaveEventType = WaveEventType.Reinforcement;

        [Header("Stages")]
        public List<QuestStage> stages = new List<QuestStage>();

        [Header("Guidance")]
        public List<string> nextQuestIds = new List<string>();
        public bool autoStartNextQuests = true;

        [Header("Failure")]
        public float timeLimit = 0f;
        public bool failOnPlayerDeath = false;
        public bool failOnGameOver = false;
        public bool failOnDefenseTargetDestroyed = false;

        [Header("Failure Compensation")]
        public bool allowFailureCompensation = false;
        public int compensationMinFailureStreak = 2;
        public float compensationBonusPerFailure = 0.08f;
        public float compensationBonusCap = 0.35f;
        public float compensationDebtPayoutCap = 0.7f;
        public int compensationChapterWindow = 1;
        public int compensationStreakDecayOnComplete = 1;
        
        [Header("Rewards")]
        public QuestReward reward = new QuestReward();
        
        [Header("Difficulty")]
        public int difficultyRating = 1;
        public bool isOptional = false;

        [Header("Category")]
        public QuestRewardTier rewardTier = QuestRewardTier.Mainline;
    }

    public class QuestProgress
    {
        public QuestData data;
        public QuestStatus status = QuestStatus.Locked;
        public int currentProgress = 0;
        public float elapsedTime = 0f;
        public float totalElapsedTime = 0f;
        public bool isTimerActive = false;
        public int stageIndex = 0;
        public float stageElapsedTime = 0f;
        public string lastStrongholdId = "";
        
        public QuestStage CurrentStage => data != null && data.stages != null && data.stages.Count > 0 && stageIndex < data.stages.Count
            ? data.stages[Mathf.Max(0, stageIndex)]
            : null;
        public bool HasStages => data != null && data.stages != null && data.stages.Count > 0;

        public QuestType CurrentType => CurrentStage != null ? CurrentStage.questType : (data != null ? data.questType : QuestType.Kill);
        public int CurrentTargetCount => CurrentStage != null ? CurrentStage.targetCount : (data != null ? data.targetCount : 0);
        public float CurrentTargetTime => CurrentStage != null ? CurrentStage.targetTime : (data != null ? data.targetTime : 0f);
        public EnemyType CurrentTargetEnemyType => CurrentStage != null ? CurrentStage.targetEnemyType : (data != null ? data.targetEnemyType : EnemyType.Grunt);
        public string CurrentTargetLocationId => CurrentStage != null ? CurrentStage.targetLocationId : (data != null ? data.targetLocationId : string.Empty);
        public string CurrentTargetStrongholdId => CurrentStage != null ? CurrentStage.targetStrongholdId : (data != null ? data.targetStrongholdId : string.Empty);
        public string CurrentTargetBossId => CurrentStage != null ? CurrentStage.targetBossId : (data != null ? data.targetBossId : string.Empty);
        public bool MatchAnyWaveEventType => CurrentStage != null ? CurrentStage.matchAnyWaveEventType : (data != null ? data.matchAnyWaveEventType : true);
        public WaveEventType CurrentTargetWaveEventType => CurrentStage != null ? CurrentStage.targetWaveEventType : (data != null ? data.targetWaveEventType : WaveEventType.Reinforcement);
        public QuestRewardTier RewardTier => data != null ? data.rewardTier : QuestRewardTier.Mainline;

        public float ProgressPercent
        {
            get
            {
                if (CurrentType == QuestType.Survive || CurrentType == QuestType.Protect)
                {
                    float timeTarget = CurrentTargetTime;
                    return timeTarget > 0f ? Mathf.Clamp01(stageElapsedTime / timeTarget) : 0f;
                }

                int target = CurrentTargetCount;
                if (target <= 0)
                {
                    target = 1;
                }
                return Mathf.Clamp01((float)currentProgress / target);
            }
        }

        public bool IsStageComplete
        {
            get
            {
                if (CurrentType == QuestType.Survive || CurrentType == QuestType.Protect)
                {
                    return CurrentTargetTime > 0f && stageElapsedTime >= CurrentTargetTime;
                }

                int target = CurrentTargetCount;
                if (target <= 0)
                {
                    target = 1;
                }
                return currentProgress >= target;
            }
        }

        public bool IsQuestComplete => HasStages ? stageIndex >= data.stages.Count : IsStageComplete;
    }

    public class QuestSystem : MonoBehaviour
    {
        [Header("Configuration")]
        public List<QuestData> availableQuests = new List<QuestData>();
        public QuestDatabase questDatabase;
        public bool autoStartGuidedQuests = true;

        [Header("Rewards")]
        public PearlInventory inventory;
        public PearlDatabase pearlDatabase;
        public bool showRewardMessages = true;
        public float expRewardMultiplier = 1f;
        public float pearlRewardMultiplier = 1f;
        public float levelRewardMultiplier = 1f;
        public int levelDifficulty = 1;
        public int levelChapterId = 1;
        public CurrencyWallet wallet;
        public bool autoSaveOnQuestComplete = true;
        public bool saveQuestRuntimeState = true;
        public bool autoRestoreQuestStateOnStart = false;
        public float questStateWriteInterval = 0.5f;
        public bool logQuestStateSync = false;
        public bool logStartupStatus = true;
        public bool enableFailureCompensation = true;
        public int maxTrackedFailureStreak = 8;
        public bool logFailureCompensation = false;
        
        [Header("State")]
        public List<QuestProgress> activeQuests = new List<QuestProgress>();
        
        [Header("Events")]
        public System.Action<QuestProgress> OnQuestStarted;
        public System.Action<QuestProgress> OnQuestProgress;
        public System.Action<QuestProgress> OnQuestCompleted;
        public System.Action<QuestProgress> OnQuestFailed;
        
        private PlayerExperienceSystem experienceSystem;
        private bool startupLogged;
        private int questStateSaveSuspendCount;
        private float questStateWriteTimer;
        private int consecutiveFailureCount;
        private float pendingFailureDebtExp;
        private float pendingFailureDebtCredits;
        private int lastFailureChapterId;
        private string lastFailureStrongholdId = string.Empty;
        
        private void Awake()
        {
            EnsureCollections();
            EnsureExperienceSystem();
            EnsureRewardReferences();
        }

        private void Start()
        {
            EnsureCollections();
            EnsureRewardReferences();
            if (autoRestoreQuestStateOnStart)
            {
                RestoreQuestRuntimeStateFromSave(false);
            }
            LogStartupStatus();
        }
        
        private void OnEnable()
        {
            EnsureCollections();
            EnsureExperienceSystem();
            EnsureRewardReferences();
            GameEvents.OnEnemyKilled += HandleEnemyKilled;
            GameEvents.OnWaveCompleted += HandleWaveCompleted;
            GameEvents.OnStrongholdCompleted += HandleStrongholdCompleted;
            GameEvents.OnWaveEventCompleted += HandleWaveEventCompleted;
            GameEvents.OnBossBreakWindowStart += HandleBossBreakWindowStart;
            GameEvents.OnBossDefeated += HandleBossDefeated;
            GameEvents.OnComboCountChanged += HandleComboChanged;
            GameEvents.OnPearlCollected += HandlePearlCollected;
            GameEvents.OnLocationReached += HandleLocationReached;
            GameEvents.OnDefenseTargetDestroyed += HandleDefenseTargetDestroyed;
            GameEvents.OnPlayerDeath += HandlePlayerDeath;
            GameEvents.OnGameOver += HandleGameOver;
        }
        
        private void OnDisable()
        {
            GameEvents.OnEnemyKilled -= HandleEnemyKilled;
            GameEvents.OnWaveCompleted -= HandleWaveCompleted;
            GameEvents.OnStrongholdCompleted -= HandleStrongholdCompleted;
            GameEvents.OnWaveEventCompleted -= HandleWaveEventCompleted;
            GameEvents.OnBossBreakWindowStart -= HandleBossBreakWindowStart;
            GameEvents.OnBossDefeated -= HandleBossDefeated;
            GameEvents.OnComboCountChanged -= HandleComboChanged;
            GameEvents.OnPearlCollected -= HandlePearlCollected;
            GameEvents.OnLocationReached -= HandleLocationReached;
            GameEvents.OnDefenseTargetDestroyed -= HandleDefenseTargetDestroyed;
            GameEvents.OnPlayerDeath -= HandlePlayerDeath;
            GameEvents.OnGameOver -= HandleGameOver;
        }

        public void BindExperienceSystem(PlayerExperienceSystem system)
        {
            if (system != null)
            {
                experienceSystem = system;
            }
        }

        private void EnsureExperienceSystem()
        {
            if (experienceSystem == null)
            {
                experienceSystem = FindObjectOfType<PlayerExperienceSystem>();
            }
        }

        public void SuspendQuestStateSave(bool suspend)
        {
            if (suspend)
            {
                questStateSaveSuspendCount++;
            }
            else
            {
                questStateSaveSuspendCount = Mathf.Max(0, questStateSaveSuspendCount - 1);
            }
        }

        public void SaveQuestRuntimeStateToData()
        {
            EnsureCollections();
            if (!saveQuestRuntimeState || questStateSaveSuspendCount > 0)
            {
                return;
            }

            SaveManager save = SaveManager.Instance;
            if (save == null || save.CurrentData == null)
            {
                return;
            }

            if (save.CurrentData.questStates == null)
            {
                save.CurrentData.questStates = new List<QuestStateData>();
            }

            List<QuestStateData> states = save.CurrentData.questStates;
            states.Clear();
            HashSet<string> savedIds = new HashSet<string>();

            for (int i = 0; i < activeQuests.Count; i++)
            {
                QuestProgress quest = activeQuests[i];
                if (!IsValidQuest(quest))
                {
                    continue;
                }

                string questId = quest.data.questId;
                if (string.IsNullOrEmpty(questId) || !savedIds.Add(questId))
                {
                    continue;
                }

                states.Add(new QuestStateData
                {
                    questId = questId,
                    status = (int)quest.status,
                    currentProgress = Mathf.Max(0, quest.currentProgress),
                    stageIndex = Mathf.Max(0, quest.stageIndex),
                    stageElapsedTime = Mathf.Max(0f, quest.stageElapsedTime),
                    totalElapsedTime = Mathf.Max(0f, quest.totalElapsedTime),
                    isTimerActive = quest.isTimerActive,
                    lastStrongholdId = quest.lastStrongholdId ?? string.Empty
                });
            }

            SaveFailureCompensationState(save.CurrentData);

            if (logQuestStateSync)
            {
                Debug.Log($"[QuestSystem] Saved quest runtime state entries={states.Count}");
            }
        }

        public bool RestoreQuestRuntimeStateFromSave(bool notifyListeners = true, bool addMissingQuests = true)
        {
            EnsureCollections();
            SaveManager save = SaveManager.Instance;
            if (save == null || save.CurrentData == null)
            {
                return false;
            }

            bool restoredFailureCompensationState = RestoreFailureCompensationState(save.CurrentData);
            if (save.CurrentData.questStates == null || save.CurrentData.questStates.Count == 0)
            {
                return restoredFailureCompensationState;
            }

            int restoredCount = 0;
            SuspendQuestStateSave(true);
            try
            {
                Dictionary<string, QuestProgress> byId = new Dictionary<string, QuestProgress>();
                for (int i = 0; i < activeQuests.Count; i++)
                {
                    QuestProgress quest = activeQuests[i];
                    if (!IsValidQuest(quest))
                    {
                        continue;
                    }

                    string id = quest.data.questId;
                    if (!string.IsNullOrEmpty(id) && !byId.ContainsKey(id))
                    {
                        byId.Add(id, quest);
                    }
                }

                List<QuestStateData> states = save.CurrentData.questStates;
                for (int i = 0; i < states.Count; i++)
                {
                    QuestStateData state = states[i];
                    if (state == null || string.IsNullOrEmpty(state.questId))
                    {
                        continue;
                    }

                    if (!byId.TryGetValue(state.questId, out QuestProgress quest))
                    {
                        if (!addMissingQuests)
                        {
                            continue;
                        }

                        QuestData data = FindQuestById(state.questId);
                        if (data == null)
                        {
                            continue;
                        }

                        EnsureRewardData(data);
                        quest = new QuestProgress
                        {
                            data = data
                        };
                        activeQuests.Add(quest);
                        byId[state.questId] = quest;
                    }

                    ApplySavedState(quest, state);
                    restoredCount++;
                }
            }
            finally
            {
                SuspendQuestStateSave(false);
            }

            if (restoredCount <= 0)
            {
                return restoredFailureCompensationState;
            }

            if (notifyListeners)
            {
                for (int i = 0; i < activeQuests.Count; i++)
                {
                    QuestProgress quest = activeQuests[i];
                    if (IsInProgressQuest(quest))
                    {
                        OnQuestProgress?.Invoke(quest);
                    }
                }
            }

            if (logQuestStateSync)
            {
                Debug.Log($"[QuestSystem] Restored quest runtime state entries={restoredCount}");
            }

            return true;
        }

        private static void ApplySavedState(QuestProgress quest, QuestStateData state)
        {
            if (quest == null || quest.data == null || state == null)
            {
                return;
            }

            int minStatus = (int)QuestStatus.Locked;
            int maxStatus = (int)QuestStatus.Failed;
            quest.status = (QuestStatus)Mathf.Clamp(state.status, minStatus, maxStatus);
            quest.currentProgress = Mathf.Max(0, state.currentProgress);
            quest.totalElapsedTime = Mathf.Max(0f, state.totalElapsedTime);
            quest.stageElapsedTime = Mathf.Max(0f, state.stageElapsedTime);
            quest.elapsedTime = quest.stageElapsedTime;
            quest.lastStrongholdId = state.lastStrongholdId ?? string.Empty;

            int stageCount = quest.data.stages != null ? quest.data.stages.Count : 0;
            if (stageCount <= 0)
            {
                quest.stageIndex = 0;
            }
            else
            {
                int maxIndex = Mathf.Max(0, stageCount);
                quest.stageIndex = Mathf.Clamp(state.stageIndex, 0, maxIndex);
                if (quest.status == QuestStatus.InProgress && quest.stageIndex >= stageCount)
                {
                    quest.stageIndex = stageCount - 1;
                }
            }

            bool timerType = quest.CurrentType == QuestType.Survive
                || quest.CurrentType == QuestType.Reach
                || quest.CurrentType == QuestType.Protect;
            quest.isTimerActive = quest.status == QuestStatus.InProgress && timerType;
        }
        
        private void Update()
        {
            EnsureCollections();
            UpdateQuestTimers();

            if (saveQuestRuntimeState && questStateSaveSuspendCount == 0)
            {
                questStateWriteTimer += Time.deltaTime;
                float interval = Mathf.Max(0.1f, questStateWriteInterval);
                if (questStateWriteTimer >= interval)
                {
                    questStateWriteTimer = 0f;
                    SaveQuestRuntimeStateToData();
                }
            }
        }
        
        public void StartQuest(QuestData questData)
        {
            if (questData == null)
            {
                return;
            }

            EnsureCollections();
            EnsureRewardData(questData);
            QuestProgress progress = new QuestProgress
            {
                data = questData,
                status = QuestStatus.InProgress,
                currentProgress = 0,
                stageIndex = 0,
                stageElapsedTime = 0f,
                totalElapsedTime = 0f
            };

            QuestType type = progress.CurrentType;
            if (type == QuestType.Survive || type == QuestType.Reach || type == QuestType.Protect)
            {
                progress.isTimerActive = true;
            }
            
            activeQuests.Add(progress);
            NotifyQuestStarted(progress);
            
            GameEvents.ShowMessage($"Quest Started: {questData.questName}", 3f);
            ShowStageStartMessage(progress, true);
        }
        
        public void StartQuests(List<QuestData> quests)
        {
            if (quests == null || quests.Count == 0)
            {
                return;
            }

            EnsureCollections();
            for (int i = 0; i < quests.Count; i++)
            {
                StartQuest(quests[i]);
            }
        }

        public void ResetQuests()
        {
            EnsureCollections();
            activeQuests.Clear();
            SaveQuestRuntimeStateIfEnabled();
        }
        
        private void UpdateQuestTimers()
        {
            float deltaTime = Time.deltaTime;
            
            for (int i = 0; i < activeQuests.Count; i++)
            {
                QuestProgress quest = activeQuests[i];
                if (!IsInProgressQuest(quest))
                {
                    continue;
                }
                
                quest.totalElapsedTime += deltaTime;
                quest.stageElapsedTime += deltaTime;

                if (quest.data.timeLimit > 0f && quest.totalElapsedTime >= quest.data.timeLimit)
                {
                    FailQuest(quest, "Time limit exceeded");
                    continue;
                }

                QuestStage stage = quest.CurrentStage;
                if (stage != null && stage.useTimeLimit && stage.timeLimit > 0f && quest.stageElapsedTime >= stage.timeLimit)
                {
                    FailQuest(quest, "Stage time limit exceeded");
                    continue;
                }

                if (quest.CurrentType == QuestType.Survive || quest.CurrentType == QuestType.Protect)
                {
                    quest.currentProgress = Mathf.FloorToInt(quest.stageElapsedTime);

                    if (quest.IsStageComplete)
                    {
                        AdvanceStageOrComplete(quest);
                    }
                }
            }
        }
        
        private void HandleEnemyKilled(EnemyType type, Vector3 position, int expReward)
        {
            for (int i = 0; i < activeQuests.Count; i++)
            {
                QuestProgress quest = activeQuests[i];

                if (!IsInProgressQuest(quest))
                {
                    continue;
                }
                
                bool progressUpdated = false;

                if (quest.CurrentType == QuestType.Kill)
                {
                    quest.currentProgress++;
                    progressUpdated = true;
                }
                else if (quest.CurrentType == QuestType.KillEnemyType && type == quest.CurrentTargetEnemyType)
                {
                    quest.currentProgress++;
                    progressUpdated = true;
                }
                
                if (progressUpdated)
                {
                    NotifyQuestProgressChanged(quest);
                    
                    if (quest.IsStageComplete)
                    {
                        AdvanceStageOrComplete(quest);
                    }
                }
            }
        }
        
        private void HandleWaveCompleted(StrongholdController stronghold, int waveIndex)
        {
            for (int i = 0; i < activeQuests.Count; i++)
            {
                QuestProgress quest = activeQuests[i];
                
                if (IsInProgressQuest(quest) && quest.CurrentType == QuestType.CompleteWave)
                {
                    if (!MatchesStronghold(quest, stronghold))
                    {
                        continue;
                    }

                    quest.currentProgress++;
                    if (stronghold != null)
                    {
                        quest.lastStrongholdId = stronghold.StrongholdId;
                    }
                    NotifyQuestProgressChanged(quest);
                    
                    if (quest.IsStageComplete)
                    {
                        AdvanceStageOrComplete(quest);
                    }
                }
            }
        }
        
        private void HandleStrongholdCompleted(StrongholdController stronghold)
        {
            for (int i = 0; i < activeQuests.Count; i++)
            {
                QuestProgress quest = activeQuests[i];
                
                if (IsInProgressQuest(quest) && quest.CurrentType == QuestType.CompleteStronghold)
                {
                    if (!MatchesStronghold(quest, stronghold))
                    {
                        continue;
                    }

                    quest.currentProgress = quest.CurrentTargetCount;
                    if (stronghold != null)
                    {
                        quest.lastStrongholdId = stronghold.StrongholdId;
                    }
                    NotifyQuestProgressChanged(quest);
                    
                    if (quest.IsStageComplete)
                    {
                        AdvanceStageOrComplete(quest);
                    }
                }
            }
        }

        private void HandleWaveEventCompleted(StrongholdController stronghold, int waveIndex, WaveEventType eventType)
        {
            for (int i = 0; i < activeQuests.Count; i++)
            {
                QuestProgress quest = activeQuests[i];
                if (!IsInProgressQuest(quest) || quest.CurrentType != QuestType.CompleteWaveEvent)
                {
                    continue;
                }

                if (!MatchesStronghold(quest, stronghold))
                {
                    continue;
                }

                if (!quest.MatchAnyWaveEventType && quest.CurrentTargetWaveEventType != eventType)
                {
                    continue;
                }

                quest.currentProgress++;
                if (stronghold != null)
                {
                    quest.lastStrongholdId = stronghold.StrongholdId;
                }
                NotifyQuestProgressChanged(quest);

                if (quest.IsStageComplete)
                {
                    AdvanceStageOrComplete(quest);
                }
            }
        }

        private void HandleBossBreakWindowStart()
        {
            for (int i = 0; i < activeQuests.Count; i++)
            {
                QuestProgress quest = activeQuests[i];
                if (!IsInProgressQuest(quest) || quest.CurrentType != QuestType.BossBreak)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(quest.CurrentTargetBossId) && !IsBossActive(quest.CurrentTargetBossId))
                {
                    continue;
                }

                quest.currentProgress++;
                NotifyQuestProgressChanged(quest);

                if (quest.IsStageComplete)
                {
                    AdvanceStageOrComplete(quest);
                }
            }
        }

        private void HandleBossDefeated(BossSpawnPoint boss)
        {
            for (int i = 0; i < activeQuests.Count; i++)
            {
                QuestProgress quest = activeQuests[i];
                if (!IsInProgressQuest(quest) || quest.CurrentType != QuestType.BossDefeat)
                {
                    continue;
                }

                if (!MatchesBoss(quest, boss))
                {
                    continue;
                }

                quest.currentProgress++;
                NotifyQuestProgressChanged(quest);

                if (quest.IsStageComplete)
                {
                    AdvanceStageOrComplete(quest);
                }
            }
        }

        private void HandleComboChanged(int combo)
        {
            for (int i = 0; i < activeQuests.Count; i++)
            {
                QuestProgress quest = activeQuests[i];
                if (!IsInProgressQuest(quest))
                {
                    continue;
                }

                if (quest.CurrentType == QuestType.Combo)
                {
                    quest.currentProgress = Mathf.Max(quest.currentProgress, combo);
                    NotifyQuestProgressChanged(quest);

                    if (quest.IsStageComplete)
                    {
                        AdvanceStageOrComplete(quest);
                    }
                }
            }
        }

        private void HandlePearlCollected(string pearlId)
        {
            for (int i = 0; i < activeQuests.Count; i++)
            {
                QuestProgress quest = activeQuests[i];
                if (!IsInProgressQuest(quest))
                {
                    continue;
                }

                if (quest.CurrentType != QuestType.Collect)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(quest.CurrentTargetLocationId) && quest.CurrentTargetLocationId != pearlId)
                {
                    continue;
                }

                quest.currentProgress++;
                NotifyQuestProgressChanged(quest);
                if (quest.IsStageComplete)
                {
                    AdvanceStageOrComplete(quest);
                }
            }
        }

        private void HandleLocationReached(string locationId)
        {
            for (int i = 0; i < activeQuests.Count; i++)
            {
                QuestProgress quest = activeQuests[i];
                if (!IsInProgressQuest(quest))
                {
                    continue;
                }

                if (quest.CurrentType != QuestType.Reach)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(quest.CurrentTargetLocationId) && quest.CurrentTargetLocationId != locationId)
                {
                    continue;
                }

                quest.currentProgress++;
                NotifyQuestProgressChanged(quest);
                if (quest.IsStageComplete)
                {
                    AdvanceStageOrComplete(quest);
                }
            }
        }

        private void HandleDefenseTargetDestroyed(string targetId)
        {
            for (int i = 0; i < activeQuests.Count; i++)
            {
                QuestProgress quest = activeQuests[i];
                if (!IsInProgressQuest(quest))
                {
                    continue;
                }

                bool isProtectStage = quest.CurrentType == QuestType.Protect
                    || (quest.CurrentType == QuestType.CompleteWaveEvent
                        && !quest.MatchAnyWaveEventType
                        && quest.CurrentTargetWaveEventType == WaveEventType.ProtectTarget);

                if (!quest.data.failOnDefenseTargetDestroyed || !isProtectStage)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(quest.CurrentTargetLocationId) && quest.CurrentTargetLocationId != targetId)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(quest.CurrentTargetStrongholdId))
                {
                    if (string.IsNullOrEmpty(targetId))
                    {
                        continue;
                    }

                    string strongholdId = quest.CurrentTargetStrongholdId;
                    if (targetId != strongholdId && !targetId.StartsWith(strongholdId + "_"))
                    {
                        continue;
                    }
                }

                FailQuest(quest, "Defense target destroyed");
            }
        }

        private void HandlePlayerDeath()
        {
            FailQuestsOnCondition(q => q.data.failOnPlayerDeath, "Player defeated");
        }

        private void HandleGameOver(bool victory)
        {
            if (!victory)
            {
                FailQuestsOnCondition(q => q.data.failOnGameOver, "Stage failed");
            }
        }
        
        private void CompleteQuest(QuestProgress quest)
        {
            if (!IsValidQuest(quest))
            {
                return;
            }

            if (quest.status != QuestStatus.InProgress)
            {
                return;
            }

            EnsureRewardReferences();
            quest.status = QuestStatus.Completed;
            EnsureExperienceSystem();
            QuestReward reward = EnsureRewardData(quest.data);

            string rewardStrongholdId = ResolveQuestStrongholdId(quest);
            float rewardMultiplier = Mathf.Max(0f, expRewardMultiplier) * Mathf.Max(0f, levelRewardMultiplier);
            int expReward = EconomyService.AdjustQuestExp(reward.exp, quest.data.questType, quest.data.difficultyRating, levelDifficulty, rewardMultiplier, quest.data.rewardTier, levelChapterId, rewardStrongholdId);

            float pearlMultiplier = Mathf.Max(0f, pearlRewardMultiplier) * Mathf.Max(0f, levelRewardMultiplier);
            int pearlReward = EconomyService.AdjustQuestPearls(reward.pearls, quest.data.questType, quest.data.difficultyRating, levelDifficulty, pearlMultiplier, quest.data.rewardTier, levelChapterId, rewardStrongholdId);
            if (pearlReward > 0)
            {
                GrantQuestPearls(pearlReward);
            }

            int creditReward = EconomyService.AdjustQuestCredits(reward.credits, quest.data.questType, quest.data.difficultyRating, levelDifficulty, levelRewardMultiplier, quest.data.rewardTier, levelChapterId, rewardStrongholdId);

            int compensationBonusExp = 0;
            int compensationBonusCredits = 0;
            TryApplyFailureCompensationBonus(quest, rewardStrongholdId, expReward, creditReward, out compensationBonusExp, out compensationBonusCredits);
            expReward += compensationBonusExp;
            creditReward += compensationBonusCredits;

            if (experienceSystem != null && expReward > 0)
            {
                experienceSystem.GrantExperience(expReward);
            }

            if (wallet != null && creditReward > 0)
            {
                wallet.AddCredits(creditReward);
            }

            if (showRewardMessages && (compensationBonusExp > 0 || compensationBonusCredits > 0))
            {
                string bonusMsg = $"Recovery Bonus: EXP +{compensationBonusExp}";
                if (compensationBonusCredits > 0)
                {
                    bonusMsg += $" | Credits +{compensationBonusCredits}";
                }
                GameEvents.ShowMessage(bonusMsg, 2f);
            }

            GameEvents.ShowMessage($"Quest Complete: {quest.data.questName}!", 3f);
            NotifyQuestCompleted(quest);

            if (autoStartGuidedQuests && quest.data.autoStartNextQuests)
            {
                StartNextQuests(quest.data.nextQuestIds);
            }

            if (autoSaveOnQuestComplete && SaveManager.Instance != null)
            {
                SaveManager.Instance.SaveGame();
            }
        }

        private void GrantQuestPearls(int count)
        {
            EnsureRewardReferences();
            if (count <= 0 || inventory == null)
            {
                return;
            }

            int granted = 0;
            for (int i = 0; i < count; i++)
            {
                PearlItem pearl = PickRandomPearl();
                if (pearl == null)
                {
                    continue;
                }

                if (inventory.AddPearl(pearl))
                {
                    granted++;
                    GameEvents.PearlCollected(pearl.GetId());
                }
            }

            if (showRewardMessages && granted > 0)
            {
                GameEvents.ShowMessage($"+{granted} Pearls", 1.4f);
            }
        }

        private PearlItem PickRandomPearl()
        {
            EnsureRewardReferences();
            if (pearlDatabase == null || pearlDatabase.pearls == null || pearlDatabase.pearls.Count == 0)
            {
                return null;
            }

            int index = Random.Range(0, pearlDatabase.pearls.Count);
            return pearlDatabase.pearls[index];
        }
        
        public void FailQuest(QuestProgress quest, string reason = "")
        {
            if (!IsInProgressQuest(quest))
            {
                return;
            }

            CaptureFailureDebt(quest);
            quest.status = QuestStatus.Failed;
            if (quest.data != null)
            {
                string label = string.IsNullOrEmpty(reason)
                    ? $"Quest Failed: {quest.data.questName}"
                    : $"Quest Failed: {quest.data.questName} ({reason})";
                GameEvents.ShowMessage(label, 2f);
            }
            NotifyQuestFailed(quest);
        }

        private void FailQuestsOnCondition(System.Func<QuestProgress, bool> predicate, string reason)
        {
            if (predicate == null)
            {
                return;
            }

            EnsureCollections();
            for (int i = 0; i < activeQuests.Count; i++)
            {
                QuestProgress quest = activeQuests[i];
                if (!IsInProgressQuest(quest))
                {
                    continue;
                }

                if (predicate(quest))
                {
                    FailQuest(quest, reason);
                }
            }
        }

        private void AdvanceStageOrComplete(QuestProgress quest)
        {
            if (!IsValidQuest(quest))
            {
                return;
            }

            if (!quest.HasStages)
            {
                CompleteQuest(quest);
                return;
            }

            QuestStage completedStage = quest.CurrentStage;
            if (completedStage != null)
            {
                ShowStageCompleteMessage(quest, completedStage);
            }

            quest.stageIndex++;
            quest.currentProgress = 0;
            quest.stageElapsedTime = 0f;

            if (quest.stageIndex >= quest.data.stages.Count)
            {
                CompleteQuest(quest);
                return;
            }

            QuestType type = quest.CurrentType;
            quest.isTimerActive = type == QuestType.Survive || type == QuestType.Reach || type == QuestType.Protect;
            NotifyQuestProgressChanged(quest);
            ShowStageStartMessage(quest, false);
        }

        private void ShowStageStartMessage(QuestProgress quest, bool isQuestStart)
        {
            if (quest == null)
            {
                return;
            }

            if (quest.HasStages && quest.CurrentStage != null)
            {
                string stageTitle = string.IsNullOrEmpty(quest.CurrentStage.title)
                    ? $"Stage {quest.stageIndex + 1}"
                    : quest.CurrentStage.title;
                string prefix = isQuestStart ? "Stage Start" : "Next Stage";
                string description = quest.CurrentStage.description;
                string label = string.IsNullOrEmpty(description)
                    ? $"{prefix}: {stageTitle}"
                    : $"{prefix}: {stageTitle} - {description}";
                GameEvents.ShowMessage(label, 2f);
            }
        }

        private void ShowStageCompleteMessage(QuestProgress quest, QuestStage stage)
        {
            if (quest == null || stage == null)
            {
                return;
            }

            string stageTitle = string.IsNullOrEmpty(stage.title)
                ? $"Stage {quest.stageIndex + 1}"
                : stage.title;
            GameEvents.ShowMessage($"Stage Complete: {stageTitle}", 2f);
        }

        private bool MatchesStronghold(QuestProgress quest, StrongholdController stronghold)
        {
            string targetId = quest.CurrentTargetStrongholdId;
            if (string.IsNullOrEmpty(targetId))
            {
                return true;
            }

            if (stronghold == null)
            {
                return false;
            }

            return stronghold.StrongholdId == targetId || stronghold.name == targetId;
        }

        private bool MatchesBoss(QuestProgress quest, BossSpawnPoint boss)
        {
            string targetId = quest.CurrentTargetBossId;
            if (string.IsNullOrEmpty(targetId))
            {
                return true;
            }

            if (boss == null)
            {
                return false;
            }

            return boss.bossName == targetId || boss.name == targetId;
        }

        private string ResolveQuestStrongholdId(QuestProgress quest)
        {
            if (quest == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrEmpty(quest.CurrentTargetStrongholdId))
            {
                return quest.CurrentTargetStrongholdId;
            }

            return quest.lastStrongholdId ?? string.Empty;
        }

        private bool IsBossActive(string targetId)
        {
            BossSpawnPoint[] bosses = FindObjectsOfType<BossSpawnPoint>();
            for (int i = 0; i < bosses.Length; i++)
            {
                BossSpawnPoint boss = bosses[i];
                if (boss == null)
                {
                    continue;
                }

                if (boss.bossName != targetId && boss.name != targetId)
                {
                    continue;
                }

                if (boss.HasSpawned && !boss.IsDefeated)
                {
                    return true;
                }
            }

            return false;
        }

        private void StartNextQuests(List<string> nextQuestIds)
        {
            if (nextQuestIds == null || nextQuestIds.Count == 0)
            {
                return;
            }

            for (int i = 0; i < nextQuestIds.Count; i++)
            {
                string id = nextQuestIds[i];
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                if (IsQuestActiveOrCompleted(id))
                {
                    continue;
                }

                QuestData quest = FindQuestById(id);
                if (quest != null)
                {
                    StartQuest(quest);
                }
            }
        }

        private QuestData FindQuestById(string id)
        {
            EnsureCollections();

            if (questDatabase != null)
            {
                QuestData fromDb = questDatabase.GetQuestById(id);
                if (fromDb != null)
                {
                    return fromDb;
                }
            }

            if (availableQuests == null)
            {
                return null;
            }

            for (int i = 0; i < availableQuests.Count; i++)
            {
                QuestData quest = availableQuests[i];
                if (quest != null && quest.questId == id)
                {
                    return quest;
                }
            }

            return null;
        }

        private bool IsQuestActiveOrCompleted(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            EnsureCollections();
            for (int i = 0; i < activeQuests.Count; i++)
            {
                QuestProgress quest = activeQuests[i];
                if (quest != null && quest.data != null && quest.data.questId == id)
                {
                    if (quest.status == QuestStatus.InProgress || quest.status == QuestStatus.Completed)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public string GetRewardPreview(QuestProgress quest)
        {
            if (!IsValidQuest(quest))
            {
                return string.Empty;
            }

            QuestReward reward = EnsureRewardData(quest.data);
            string rewardStrongholdId = ResolveQuestStrongholdId(quest);
            float rewardMultiplier = Mathf.Max(0f, expRewardMultiplier) * Mathf.Max(0f, levelRewardMultiplier);
            float pearlMultiplier = Mathf.Max(0f, pearlRewardMultiplier) * Mathf.Max(0f, levelRewardMultiplier);
            int expReward = EconomyService.AdjustQuestExp(reward.exp, quest.data.questType, quest.data.difficultyRating, levelDifficulty, rewardMultiplier, quest.data.rewardTier, levelChapterId, rewardStrongholdId);
            int pearlReward = EconomyService.AdjustQuestPearls(reward.pearls, quest.data.questType, quest.data.difficultyRating, levelDifficulty, pearlMultiplier, quest.data.rewardTier, levelChapterId, rewardStrongholdId);
            int creditReward = EconomyService.AdjustQuestCredits(reward.credits, quest.data.questType, quest.data.difficultyRating, levelDifficulty, levelRewardMultiplier, quest.data.rewardTier, levelChapterId, rewardStrongholdId);

            List<string> parts = new List<string>();
            if (expReward > 0)
            {
                parts.Add($"EXP +{expReward}");
            }
            if (pearlReward > 0)
            {
                parts.Add($"Pearls +{pearlReward}");
            }
            if (creditReward > 0)
            {
                parts.Add($"Credits +{creditReward}");
            }

            if (CanQuestUseFailureCompensation(quest.data, rewardStrongholdId) && (pendingFailureDebtExp > 0f || pendingFailureDebtCredits > 0f))
            {
                parts.Add("Recovery Bonus Ready");
            }

            return parts.Count > 0 ? string.Join(" | ", parts) : string.Empty;
        }

        public List<QuestProgress> GetActiveQuests()
        {
            EnsureCollections();
            return activeQuests.FindAll(q => q != null && q.status == QuestStatus.InProgress);
        }
        
        public List<QuestProgress> GetCompletedQuests()
        {
            EnsureCollections();
            return activeQuests.FindAll(q => q != null && q.status == QuestStatus.Completed);
        }

        private void EnsureCollections()
        {
            if (availableQuests == null)
            {
                availableQuests = new List<QuestData>();
            }

            if (activeQuests == null)
            {
                activeQuests = new List<QuestProgress>();
            }
        }

        private void EnsureRewardReferences()
        {
            if (inventory == null)
            {
                inventory = FindObjectOfType<PearlInventory>();
            }

            if (pearlDatabase == null)
            {
                PearlDropManager dropManager = FindObjectOfType<PearlDropManager>();
                if (dropManager != null)
                {
                    pearlDatabase = dropManager.pearlDatabase;
                }
            }

            if (pearlDatabase == null)
            {
                pearlDatabase = Resources.Load<PearlDatabase>("PearlDatabase");
            }

            if (wallet == null)
            {
                wallet = FindObjectOfType<CurrencyWallet>();
                if (wallet == null)
                {
                    wallet = CurrencyWallet.EnsureInstance();
                }
            }
        }

        private static bool IsValidQuest(QuestProgress quest)
        {
            return quest != null && quest.data != null;
        }

        private static bool IsInProgressQuest(QuestProgress quest)
        {
            return IsValidQuest(quest) && quest.status == QuestStatus.InProgress;
        }

        private QuestReward EnsureRewardData(QuestData data)
        {
            if (data == null)
            {
                return new QuestReward();
            }

            if (data.reward == null)
            {
                data.reward = new QuestReward();
            }

            if (data.nextQuestIds == null)
            {
                data.nextQuestIds = new List<string>();
            }

            if (data.stages == null)
            {
                data.stages = new List<QuestStage>();
            }

            return data.reward;
        }

        private void CaptureFailureDebt(QuestProgress quest)
        {
            if (!enableFailureCompensation || !IsValidQuest(quest))
            {
                return;
            }

            QuestReward reward = EnsureRewardData(quest.data);
            string rewardStrongholdId = ResolveQuestStrongholdId(quest);
            float expMultiplier = Mathf.Max(0f, expRewardMultiplier) * Mathf.Max(0f, levelRewardMultiplier);
            int expectedExp = EconomyService.AdjustQuestExp(
                reward.exp,
                quest.data.questType,
                quest.data.difficultyRating,
                levelDifficulty,
                expMultiplier,
                quest.data.rewardTier,
                levelChapterId,
                rewardStrongholdId);

            int expectedCredits = EconomyService.AdjustQuestCredits(
                reward.credits,
                quest.data.questType,
                quest.data.difficultyRating,
                levelDifficulty,
                levelRewardMultiplier,
                quest.data.rewardTier,
                levelChapterId,
                rewardStrongholdId);

            if (expectedExp <= 0 && expectedCredits <= 0)
            {
                return;
            }

            int maxStreak = Mathf.Max(1, maxTrackedFailureStreak);
            consecutiveFailureCount = Mathf.Clamp(consecutiveFailureCount + 1, 0, maxStreak);
            pendingFailureDebtExp = Mathf.Max(0f, pendingFailureDebtExp + expectedExp);
            pendingFailureDebtCredits = Mathf.Max(0f, pendingFailureDebtCredits + expectedCredits);
            lastFailureChapterId = levelChapterId;
            lastFailureStrongholdId = rewardStrongholdId ?? string.Empty;

            if (logFailureCompensation)
            {
                Debug.Log($"[QuestSystem] Failure debt captured | streak={consecutiveFailureCount} expDebt={pendingFailureDebtExp} creditDebt={pendingFailureDebtCredits} chapter={lastFailureChapterId} stronghold={lastFailureStrongholdId}");
            }
        }

        private bool TryApplyFailureCompensationBonus(QuestProgress quest, string rewardStrongholdId, int baseExpReward, int baseCreditReward, out int bonusExp, out int bonusCredits)
        {
            bonusExp = 0;
            bonusCredits = 0;

            if (!enableFailureCompensation || !IsValidQuest(quest))
            {
                return false;
            }

            if (!CanQuestUseFailureCompensation(quest.data, rewardStrongholdId))
            {
                return false;
            }

            if (pendingFailureDebtExp <= 0f && pendingFailureDebtCredits <= 0f)
            {
                return false;
            }

            int minStreak = Mathf.Max(1, quest.data.compensationMinFailureStreak);
            if (consecutiveFailureCount < minStreak)
            {
                return false;
            }

            float bonusPerFailure = Mathf.Clamp01(quest.data.compensationBonusPerFailure);
            float bonusCap = Mathf.Clamp01(quest.data.compensationBonusCap);
            float payoutCap = Mathf.Clamp01(quest.data.compensationDebtPayoutCap);
            int effectiveStreak = Mathf.Clamp(consecutiveFailureCount - minStreak + 1, 1, Mathf.Max(1, maxTrackedFailureStreak));
            float streakBonus = Mathf.Min(bonusCap, effectiveStreak * bonusPerFailure);

            int maxBonusExpByReward = Mathf.RoundToInt(Mathf.Max(0, baseExpReward) * streakBonus);
            int maxBonusCreditsByReward = Mathf.RoundToInt(Mathf.Max(0, baseCreditReward) * streakBonus);
            int maxBonusExpByDebt = Mathf.RoundToInt(Mathf.Max(0f, pendingFailureDebtExp) * payoutCap);
            int maxBonusCreditsByDebt = Mathf.RoundToInt(Mathf.Max(0f, pendingFailureDebtCredits) * payoutCap);

            bonusExp = Mathf.Max(0, Mathf.Min(maxBonusExpByReward, maxBonusExpByDebt));
            bonusCredits = Mathf.Max(0, Mathf.Min(maxBonusCreditsByReward, maxBonusCreditsByDebt));

            if (bonusExp <= 0 && bonusCredits <= 0)
            {
                return false;
            }

            pendingFailureDebtExp = Mathf.Max(0f, pendingFailureDebtExp - bonusExp);
            pendingFailureDebtCredits = Mathf.Max(0f, pendingFailureDebtCredits - bonusCredits);
            DecayFailureCompensationStreak(quest.data);

            if (pendingFailureDebtExp <= 0.001f && pendingFailureDebtCredits <= 0.001f)
            {
                ResetFailureCompensationState();
            }

            if (logFailureCompensation)
            {
                Debug.Log($"[QuestSystem] Failure compensation applied | bonusExp={bonusExp} bonusCredits={bonusCredits} remainingExpDebt={pendingFailureDebtExp} remainingCreditDebt={pendingFailureDebtCredits} streak={consecutiveFailureCount}");
            }

            return true;
        }

        private bool CanQuestUseFailureCompensation(QuestData data, string rewardStrongholdId)
        {
            if (!enableFailureCompensation || data == null || !data.allowFailureCompensation)
            {
                return false;
            }

            int chapterWindow = Mathf.Max(0, data.compensationChapterWindow);
            if (lastFailureChapterId > 0 && levelChapterId > 0)
            {
                int chapterDelta = Mathf.Abs(levelChapterId - lastFailureChapterId);
                if (chapterDelta > chapterWindow)
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(lastFailureStrongholdId) && !string.IsNullOrEmpty(rewardStrongholdId) && chapterWindow <= 0)
            {
                // chapterWindow=0 时要求同据点补偿，避免把局部失败债务扩散到其他战区。
                return rewardStrongholdId == lastFailureStrongholdId;
            }

            return true;
        }

        private void DecayFailureCompensationStreak(QuestData data)
        {
            int decay = 1;
            if (data != null)
            {
                decay = Mathf.Max(0, data.compensationStreakDecayOnComplete);
            }

            consecutiveFailureCount = Mathf.Max(0, consecutiveFailureCount - decay);
        }

        private void ResetFailureCompensationState()
        {
            consecutiveFailureCount = 0;
            pendingFailureDebtExp = 0f;
            pendingFailureDebtCredits = 0f;
            lastFailureChapterId = 0;
            lastFailureStrongholdId = string.Empty;
        }

        private void SaveFailureCompensationState(GameData data)
        {
            if (data == null)
            {
                return;
            }

            data.questFailureStreak = Mathf.Max(0, consecutiveFailureCount);
            data.questFailureDebtExp = Mathf.Max(0f, pendingFailureDebtExp);
            data.questFailureDebtCredits = Mathf.Max(0f, pendingFailureDebtCredits);
            data.questFailureLastChapterId = Mathf.Max(0, lastFailureChapterId);
            data.questFailureLastStrongholdId = lastFailureStrongholdId ?? string.Empty;
        }

        private bool RestoreFailureCompensationState(GameData data)
        {
            if (data == null)
            {
                ResetFailureCompensationState();
                return false;
            }

            consecutiveFailureCount = Mathf.Max(0, data.questFailureStreak);
            pendingFailureDebtExp = Mathf.Max(0f, data.questFailureDebtExp);
            pendingFailureDebtCredits = Mathf.Max(0f, data.questFailureDebtCredits);
            lastFailureChapterId = Mathf.Max(0, data.questFailureLastChapterId);
            lastFailureStrongholdId = data.questFailureLastStrongholdId ?? string.Empty;

            return consecutiveFailureCount > 0 || pendingFailureDebtExp > 0f || pendingFailureDebtCredits > 0f;
        }

        private void SaveQuestRuntimeStateIfEnabled()
        {
            if (saveQuestRuntimeState)
            {
                SaveQuestRuntimeStateToData();
            }
        }

        private void NotifyQuestStarted(QuestProgress quest)
        {
            OnQuestStarted?.Invoke(quest);
            SaveQuestRuntimeStateIfEnabled();
        }

        private void NotifyQuestProgressChanged(QuestProgress quest)
        {
            OnQuestProgress?.Invoke(quest);
            SaveQuestRuntimeStateIfEnabled();
        }

        private void NotifyQuestCompleted(QuestProgress quest)
        {
            OnQuestCompleted?.Invoke(quest);
            SaveQuestRuntimeStateIfEnabled();
        }

        private void NotifyQuestFailed(QuestProgress quest)
        {
            OnQuestFailed?.Invoke(quest);
            SaveQuestRuntimeStateIfEnabled();
        }

        private void LogStartupStatus()
        {
            if (!logStartupStatus || startupLogged)
            {
                return;
            }

            startupLogged = true;
            Debug.Log($"[QuestSystem] Startup | questDb={(questDatabase != null)} exp={(experienceSystem != null)} inventory={(inventory != null)} pearlDb={(pearlDatabase != null)} wallet={(wallet != null)} available={availableQuests.Count} active={activeQuests.Count} autoSave={autoSaveOnQuestComplete} saveState={saveQuestRuntimeState}");
        }
    }
}
