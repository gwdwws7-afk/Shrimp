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
        CompleteStronghold
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
        
        [Header("Rewards")]
        public QuestReward reward = new QuestReward();
        
        [Header("Difficulty")]
        public int difficultyRating = 1;
        public bool isOptional = false;
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
        
        public QuestStage CurrentStage => data != null && data.stages != null && data.stages.Count > 0 && stageIndex < data.stages.Count
            ? data.stages[Mathf.Max(0, stageIndex)]
            : null;
        public bool HasStages => data != null && data.stages != null && data.stages.Count > 0;

        public QuestType CurrentType => CurrentStage != null ? CurrentStage.questType : (data != null ? data.questType : QuestType.Kill);
        public int CurrentTargetCount => CurrentStage != null ? CurrentStage.targetCount : (data != null ? data.targetCount : 0);
        public float CurrentTargetTime => CurrentStage != null ? CurrentStage.targetTime : (data != null ? data.targetTime : 0f);
        public EnemyType CurrentTargetEnemyType => CurrentStage != null ? CurrentStage.targetEnemyType : (data != null ? data.targetEnemyType : EnemyType.Grunt);
        public string CurrentTargetLocationId => CurrentStage != null ? CurrentStage.targetLocationId : (data != null ? data.targetLocationId : string.Empty);

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
        public CurrencyWallet wallet;
        
        [Header("State")]
        public List<QuestProgress> activeQuests = new List<QuestProgress>();
        
        [Header("Events")]
        public System.Action<QuestProgress> OnQuestStarted;
        public System.Action<QuestProgress> OnQuestProgress;
        public System.Action<QuestProgress> OnQuestCompleted;
        public System.Action<QuestProgress> OnQuestFailed;
        
        private PlayerExperienceSystem experienceSystem;
        
        private void Awake()
        {
            experienceSystem = FindObjectOfType<PlayerExperienceSystem>();
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
        
        private void OnEnable()
        {
            GameEvents.OnEnemyKilled += HandleEnemyKilled;
            GameEvents.OnWaveCompleted += HandleWaveCompleted;
            GameEvents.OnStrongholdCompleted += HandleStrongholdCompleted;
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
            GameEvents.OnComboCountChanged -= HandleComboChanged;
            GameEvents.OnPearlCollected -= HandlePearlCollected;
            GameEvents.OnLocationReached -= HandleLocationReached;
            GameEvents.OnDefenseTargetDestroyed -= HandleDefenseTargetDestroyed;
            GameEvents.OnPlayerDeath -= HandlePlayerDeath;
            GameEvents.OnGameOver -= HandleGameOver;
        }
        
        private void Update()
        {
            UpdateQuestTimers();
        }
        
        public void StartQuest(QuestData questData)
        {
            if (questData == null)
            {
                return;
            }

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
            OnQuestStarted?.Invoke(progress);
            
            GameEvents.ShowMessage($"Quest Started: {questData.questName}", 3f);
            ShowStageStartMessage(progress, true);
        }
        
        public void StartQuests(List<QuestData> quests)
        {
            for (int i = 0; i < quests.Count; i++)
            {
                StartQuest(quests[i]);
            }
        }

        public void ResetQuests()
        {
            activeQuests.Clear();
        }
        
        private void UpdateQuestTimers()
        {
            float deltaTime = Time.deltaTime;
            
            for (int i = 0; i < activeQuests.Count; i++)
            {
                QuestProgress quest = activeQuests[i];
                
                if (quest.status == QuestStatus.InProgress)
                {
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
        }
        
        private void HandleEnemyKilled(EnemyType type, Vector3 position, int expReward)
        {
            for (int i = 0; i < activeQuests.Count; i++)
            {
                QuestProgress quest = activeQuests[i];
                
                if (quest.status != QuestStatus.InProgress)
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
                    OnQuestProgress?.Invoke(quest);
                    
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
                
                if (quest.status == QuestStatus.InProgress && quest.CurrentType == QuestType.CompleteWave)
                {
                    quest.currentProgress++;
                    OnQuestProgress?.Invoke(quest);
                    
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
                
                if (quest.status == QuestStatus.InProgress && quest.CurrentType == QuestType.CompleteStronghold)
                {
                    quest.currentProgress = quest.CurrentTargetCount;
                    OnQuestProgress?.Invoke(quest);
                    
                    if (quest.IsStageComplete)
                    {
                        AdvanceStageOrComplete(quest);
                    }
                }
            }
        }

        private void HandleComboChanged(int combo)
        {
            for (int i = 0; i < activeQuests.Count; i++)
            {
                QuestProgress quest = activeQuests[i];
                if (quest.status != QuestStatus.InProgress)
                {
                    continue;
                }

                if (quest.CurrentType == QuestType.Combo)
                {
                    quest.currentProgress = Mathf.Max(quest.currentProgress, combo);
                    OnQuestProgress?.Invoke(quest);

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
                if (quest.status != QuestStatus.InProgress)
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
                OnQuestProgress?.Invoke(quest);
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
                if (quest.status != QuestStatus.InProgress)
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
                OnQuestProgress?.Invoke(quest);
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
                if (quest.status != QuestStatus.InProgress)
                {
                    continue;
                }

                if (!quest.data.failOnDefenseTargetDestroyed && quest.CurrentType != QuestType.Protect)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(quest.CurrentTargetLocationId) && quest.CurrentTargetLocationId != targetId)
                {
                    continue;
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
            quest.status = QuestStatus.Completed;

            float rewardMultiplier = Mathf.Max(0f, expRewardMultiplier) * Mathf.Max(0f, levelRewardMultiplier);
            int expReward = EconomyService.AdjustQuestExp(quest.data.reward.exp, quest.data.questType, quest.data.difficultyRating, levelDifficulty, rewardMultiplier);
            if (experienceSystem != null && expReward > 0)
            {
                experienceSystem.GrantExperience(expReward);
            }

            float pearlMultiplier = Mathf.Max(0f, pearlRewardMultiplier) * Mathf.Max(0f, levelRewardMultiplier);
            int pearlReward = EconomyService.AdjustQuestPearls(quest.data.reward.pearls, quest.data.questType, quest.data.difficultyRating, levelDifficulty, pearlMultiplier);
            if (pearlReward > 0)
            {
                GrantQuestPearls(pearlReward);
            }

            int creditReward = EconomyService.AdjustQuestCredits(quest.data.reward.credits, quest.data.questType, quest.data.difficultyRating, levelDifficulty, levelRewardMultiplier);
            if (wallet != null && creditReward > 0)
            {
                wallet.AddCredits(creditReward);
            }

            GameEvents.ShowMessage($"Quest Complete: {quest.data.questName}!", 3f);
            OnQuestCompleted?.Invoke(quest);

            if (autoStartGuidedQuests && quest.data.autoStartNextQuests)
            {
                StartNextQuests(quest.data.nextQuestIds);
            }
        }

        private void GrantQuestPearls(int count)
        {
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
            if (pearlDatabase == null || pearlDatabase.pearls == null || pearlDatabase.pearls.Count == 0)
            {
                return null;
            }

            int index = Random.Range(0, pearlDatabase.pearls.Count);
            return pearlDatabase.pearls[index];
        }
        
        public void FailQuest(QuestProgress quest, string reason = "")
        {
            quest.status = QuestStatus.Failed;
            if (quest.data != null)
            {
                string label = string.IsNullOrEmpty(reason)
                    ? $"Quest Failed: {quest.data.questName}"
                    : $"Quest Failed: {quest.data.questName} ({reason})";
                GameEvents.ShowMessage(label, 2f);
            }
            OnQuestFailed?.Invoke(quest);
        }

        private void FailQuestsOnCondition(System.Func<QuestProgress, bool> predicate, string reason)
        {
            for (int i = 0; i < activeQuests.Count; i++)
            {
                QuestProgress quest = activeQuests[i];
                if (quest.status != QuestStatus.InProgress)
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
            OnQuestProgress?.Invoke(quest);
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
            if (quest == null || quest.data == null)
            {
                return string.Empty;
            }

            float rewardMultiplier = Mathf.Max(0f, expRewardMultiplier) * Mathf.Max(0f, levelRewardMultiplier);
            float pearlMultiplier = Mathf.Max(0f, pearlRewardMultiplier) * Mathf.Max(0f, levelRewardMultiplier);
            int expReward = EconomyService.AdjustQuestExp(quest.data.reward.exp, quest.data.questType, quest.data.difficultyRating, levelDifficulty, rewardMultiplier);
            int pearlReward = EconomyService.AdjustQuestPearls(quest.data.reward.pearls, quest.data.questType, quest.data.difficultyRating, levelDifficulty, pearlMultiplier);
            int creditReward = EconomyService.AdjustQuestCredits(quest.data.reward.credits, quest.data.questType, quest.data.difficultyRating, levelDifficulty, levelRewardMultiplier);

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
                parts.Add($"深渊币 +{creditReward}");
            }

            return parts.Count > 0 ? string.Join(" | ", parts) : string.Empty;
        }
        
        public List<QuestProgress> GetActiveQuests()
        {
            return activeQuests.FindAll(q => q.status == QuestStatus.InProgress);
        }
        
        public List<QuestProgress> GetCompletedQuests()
        {
            return activeQuests.FindAll(q => q.status == QuestStatus.Completed);
        }
    }
}
