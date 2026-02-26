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
            
            if (nameText != null)
            {
                nameText.text = quest.data.questName;
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
                progressText.text = $"{quest.currentProgress}/{quest.data.targetCount}";
            }
        }
        
        private string GetQuestTypeText(QuestProgress quest)
        {
            switch (quest.data.questType)
            {
                case QuestType.Kill:
                    return $"Kill {quest.data.targetCount} enemies";
                case QuestType.KillEnemyType:
                    return $"Kill {quest.data.targetCount} {quest.data.targetEnemyType}";
                case QuestType.Survive:
                    return $"Survive {quest.data.targetTime} seconds";
                case QuestType.CompleteWave:
                    return $"Complete {quest.data.targetCount} waves";
                case QuestType.CompleteStronghold:
                    return "Clear the stronghold";
                default:
                    return quest.data.description;
            }
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
