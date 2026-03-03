using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ThirdPersonController
{
    public class UI_QuestTracker : MonoBehaviour
    {
        [Header("References")]
        public QuestSystem questSystem;
        public RectTransform questListContainer;
        public GameObject questItemPrefab;
        
        [Header("Settings")]
        public int maxVisibleQuests = 3;
        public bool showCompletedQuests = false;
        
        private List<QuestProgress> displayedQuests = new List<QuestProgress>();
        
        private void Awake()
        {
            if (questSystem == null)
            {
                questSystem = FindObjectOfType<QuestSystem>();
            }
        }
        
        private void OnEnable()
        {
            if (questSystem == null)
            {
                questSystem = FindObjectOfType<QuestSystem>();
            }

            if (questSystem != null)
            {
                questSystem.OnQuestStarted += HandleQuestStarted;
                questSystem.OnQuestProgress += HandleQuestProgress;
                questSystem.OnQuestCompleted += HandleQuestCompleted;
            }
            
            RefreshQuests();
        }
        
        private void OnDisable()
        {
            if (questSystem != null)
            {
                questSystem.OnQuestStarted -= HandleQuestStarted;
                questSystem.OnQuestProgress -= HandleQuestProgress;
                questSystem.OnQuestCompleted -= HandleQuestCompleted;
            }
        }
        
        private void HandleQuestStarted(QuestProgress quest)
        {
            RefreshQuests();
        }
        
        private void HandleQuestProgress(QuestProgress quest)
        {
            UpdateQuestItem(quest);
        }
        
        private void HandleQuestCompleted(QuestProgress quest)
        {
            RefreshQuests();
        }
        
        private void RefreshQuests()
        {
            ClearQuestItems();
            
            if (questSystem == null) return;
            
            List<QuestProgress> quests = questSystem.GetActiveQuests();
            
            for (int i = 0; i < Mathf.Min(quests.Count, maxVisibleQuests); i++)
            {
                CreateQuestItem(quests[i]);
            }
        }
        
        private void CreateQuestItem(QuestProgress quest)
        {
            if (questItemPrefab == null || questListContainer == null) return;
            
            GameObject itemObj = Instantiate(questItemPrefab, questListContainer);
            displayedQuests.Add(quest);
            
            UpdateQuestItem(quest);
        }
        
        private void UpdateQuestItem(QuestProgress quest)
        {
            int index = displayedQuests.IndexOf(quest);
            if (index < 0) return;
            
            Transform itemTransform = questListContainer.GetChild(index);
            if (itemTransform == null) return;
            
            TextMeshProUGUI nameText = itemTransform.Find("QuestName")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI descText = itemTransform.Find("QuestDesc")?.GetComponent<TextMeshProUGUI>();
            Slider progressBar = itemTransform.Find("ProgressBar")?.GetComponent<Slider>();
            TextMeshProUGUI progressText = itemTransform.Find("ProgressText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI rewardText = itemTransform.Find("RewardText")?.GetComponent<TextMeshProUGUI>();
            
            if (nameText != null)
            {
                nameText.text = GetQuestDisplayName(quest);
            }
            
            if (descText != null)
            {
                descText.text = GetQuestTypeText(quest);
            }
            
            if (progressBar != null)
            {
                progressBar.value = quest.ProgressPercent;
            }
            
            if (progressText != null)
            {
                progressText.text = GetQuestProgressText(quest);
            }

            if (rewardText != null && questSystem != null)
            {
                rewardText.text = questSystem.GetRewardPreview(quest);
            }
        }

        private string GetQuestDisplayName(QuestProgress quest)
        {
            if (quest == null || quest.data == null)
            {
                return string.Empty;
            }

            if (quest.HasStages)
            {
                int stageCount = quest.data.stages != null ? quest.data.stages.Count : 0;
                return $"{quest.data.questName} ({quest.stageIndex + 1}/{stageCount})";
            }

            return quest.data.questName;
        }
        
        private string GetQuestTypeText(QuestProgress quest)
        {
            if (quest.CurrentStage != null && !string.IsNullOrEmpty(quest.CurrentStage.description))
            {
                return quest.CurrentStage.description;
            }

            switch (quest.CurrentType)
            {
                case QuestType.Kill:
                    return $"Kill {quest.CurrentTargetCount} enemies";
                case QuestType.KillEnemyType:
                    return $"Kill {quest.CurrentTargetCount} {quest.CurrentTargetEnemyType}";
                case QuestType.Survive:
                    return $"Survive {quest.CurrentTargetTime} seconds";
                case QuestType.Protect:
                    return $"Protect target for {quest.CurrentTargetTime} seconds";
                case QuestType.CompleteWave:
                    return $"Complete {quest.CurrentTargetCount} waves";
                case QuestType.CompleteStronghold:
                    return "Clear the stronghold";
                case QuestType.Collect:
                    return $"Collect {quest.CurrentTargetCount} items";
                case QuestType.Reach:
                    return "Reach the marked location";
                case QuestType.Combo:
                    return $"Reach {quest.CurrentTargetCount} combo";
                default:
                    return quest.data.description;
            }
        }

        private string GetQuestProgressText(QuestProgress quest)
        {
            if (quest == null)
            {
                return string.Empty;
            }

            if (quest.CurrentType == QuestType.Survive || quest.CurrentType == QuestType.Protect)
            {
                int current = Mathf.FloorToInt(quest.stageElapsedTime);
                int target = Mathf.CeilToInt(quest.CurrentTargetTime);
                return $"{current}/{target}s";
            }

            int targetCount = quest.CurrentTargetCount;
            if (targetCount <= 0)
            {
                targetCount = quest.currentProgress;
            }

            return $"{quest.currentProgress}/{targetCount}";
        }
        
        private void ClearQuestItems()
        {
            if (questListContainer == null) return;
            
            for (int i = questListContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(questListContainer.GetChild(i).gameObject);
            }
            
            displayedQuests.Clear();
        }
    }
}
