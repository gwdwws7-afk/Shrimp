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
        public List<string> itemIds = new List<string>();
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
        public bool isTimerActive = false;
        
        public float ProgressPercent => data.targetCount > 0 ? (float)currentProgress / data.targetCount : 0f;
        public bool IsComplete => currentProgress >= data.targetCount;
    }

    public class QuestSystem : MonoBehaviour
    {
        [Header("Configuration")]
        public List<QuestData> availableQuests = new List<QuestData>();
        
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
        }
        
        private void OnEnable()
        {
            GameEvents.OnEnemyKilled += HandleEnemyKilled;
            GameEvents.OnWaveCompleted += HandleWaveCompleted;
            GameEvents.OnStrongholdCompleted += HandleStrongholdCompleted;
        }
        
        private void OnDisable()
        {
            GameEvents.OnEnemyKilled -= HandleEnemyKilled;
            GameEvents.OnWaveCompleted -= HandleWaveCompleted;
            GameEvents.OnStrongholdCompleted -= HandleStrongholdCompleted;
        }
        
        private void Update()
        {
            UpdateQuestTimers();
        }
        
        public void StartQuest(QuestData questData)
        {
            QuestProgress progress = new QuestProgress
            {
                data = questData,
                status = QuestStatus.InProgress,
                currentProgress = 0
            };
            
            if (questData.questType == QuestType.Survive || questData.questType == QuestType.Reach)
            {
                progress.isTimerActive = true;
            }
            
            activeQuests.Add(progress);
            OnQuestStarted?.Invoke(progress);
            
            GameEvents.ShowMessage($"Quest Started: {questData.questName}", 3f);
        }
        
        public void StartQuests(List<QuestData> quests)
        {
            for (int i = 0; i < quests.Count; i++)
            {
                StartQuest(quests[i]);
            }
        }
        
        private void UpdateQuestTimers()
        {
            float deltaTime = Time.deltaTime;
            
            for (int i = 0; i < activeQuests.Count; i++)
            {
                QuestProgress quest = activeQuests[i];
                
                if (quest.isTimerActive && quest.status == QuestStatus.InProgress)
                {
                    quest.elapsedTime += deltaTime;
                    
                    if (quest.data.questType == QuestType.Survive)
                    {
                        quest.currentProgress = Mathf.FloorToInt(quest.elapsedTime);
                        
                        if (quest.IsComplete)
                        {
                            CompleteQuest(quest);
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
                
                if (quest.data.questType == QuestType.Kill)
                {
                    quest.currentProgress++;
                    progressUpdated = true;
                }
                else if (quest.data.questType == QuestType.KillEnemyType && type == quest.data.targetEnemyType)
                {
                    quest.currentProgress++;
                    progressUpdated = true;
                }
                
                if (progressUpdated)
                {
                    OnQuestProgress?.Invoke(quest);
                    
                    if (quest.IsComplete)
                    {
                        CompleteQuest(quest);
                    }
                }
            }
        }
        
        private void HandleWaveCompleted(StrongholdController stronghold, int waveIndex)
        {
            for (int i = 0; i < activeQuests.Count; i++)
            {
                QuestProgress quest = activeQuests[i];
                
                if (quest.status == QuestStatus.InProgress && quest.data.questType == QuestType.CompleteWave)
                {
                    quest.currentProgress++;
                    OnQuestProgress?.Invoke(quest);
                    
                    if (quest.IsComplete)
                    {
                        CompleteQuest(quest);
                    }
                }
            }
        }
        
        private void HandleStrongholdCompleted(StrongholdController stronghold)
        {
            for (int i = 0; i < activeQuests.Count; i++)
            {
                QuestProgress quest = activeQuests[i];
                
                if (quest.status == QuestStatus.InProgress && quest.data.questType == QuestType.CompleteStronghold)
                {
                    quest.currentProgress = quest.data.targetCount;
                    OnQuestProgress?.Invoke(quest);
                    
                    if (quest.IsComplete)
                    {
                        CompleteQuest(quest);
                    }
                }
            }
        }
        
        private void CompleteQuest(QuestProgress quest)
        {
            quest.status = QuestStatus.Completed;
            
            if (experienceSystem != null && quest.data.reward.exp > 0)
            {
                experienceSystem.GrantExperience(quest.data.reward.exp);
            }
            
            GameEvents.ShowMessage($"Quest Complete: {quest.data.questName}!", 3f);
            OnQuestCompleted?.Invoke(quest);
        }
        
        public void FailQuest(QuestProgress quest)
        {
            quest.status = QuestStatus.Failed;
            OnQuestFailed?.Invoke(quest);
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
