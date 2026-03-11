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
        public bool logStartupStatus = true;

        [Header("Fallback Overlay")]
        public bool useFallbackOverlay = true;
        public Vector2 fallbackPosition = new Vector2(20f, 180f);
        public float fallbackWidth = 420f;
        public int fallbackFontSize = 14;
        
        private List<QuestProgress> displayedQuests = new List<QuestProgress>();
        private GUIStyle fallbackTitleStyle;
        private GUIStyle fallbackBodyStyle;
        private bool startupLogged;
        
        private void Awake()
        {
            EnsureQuestSystem();
        }

        private void Start()
        {
            LogStartupStatus();
        }
        
        private void OnEnable()
        {
            EnsureQuestSystem();

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

            EnsureQuestSystem();
            
            if (questSystem == null) return;
             
            List<QuestProgress> quests = questSystem.GetActiveQuests();
            if (quests == null || quests.Count == 0)
            {
                return;
            }
             
            for (int i = 0; i < Mathf.Min(quests.Count, maxVisibleQuests); i++)
            {
                CreateQuestItem(quests[i]);
            }
        }
        
        private void CreateQuestItem(QuestProgress quest)
        {
            if (questItemPrefab == null || questListContainer == null) return;
            
            GameObject itemObj = Instantiate(questItemPrefab, questListContainer);
            EnsureQuestItemTextComponents(itemObj);
            displayedQuests.Add(quest);
            
            UpdateQuestItem(quest);
        }
        
        private void UpdateQuestItem(QuestProgress quest)
        {
            if (quest == null || questListContainer == null)
            {
                return;
            }

            int index = displayedQuests.IndexOf(quest);
            if (index < 0 || index >= questListContainer.childCount)
            {
                return;
            }
            
            Transform itemTransform = questListContainer.GetChild(index);
            if (itemTransform == null) return;
            
            TextMeshProUGUI nameText = itemTransform.Find("QuestName")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI descText = itemTransform.Find("QuestDesc")?.GetComponent<TextMeshProUGUI>();
            Slider progressBar = itemTransform.Find("ProgressBar")?.GetComponent<Slider>();
            TextMeshProUGUI progressText = itemTransform.Find("ProgressText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI rewardText = itemTransform.Find("RewardText")?.GetComponent<TextMeshProUGUI>();

            EnsureTmpTextSafe(nameText);
            EnsureTmpTextSafe(descText);
            EnsureTmpTextSafe(progressText);
            EnsureTmpTextSafe(rewardText);
            
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
            if (quest == null || quest.data == null)
            {
                return string.Empty;
            }

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
                case QuestType.CompleteWaveEvent:
                    return quest.MatchAnyWaveEventType
                        ? $"Complete {quest.CurrentTargetCount} wave events"
                        : $"Complete {quest.CurrentTargetCount} {quest.CurrentTargetWaveEventType} events";
                case QuestType.BossBreak:
                    return string.IsNullOrEmpty(quest.CurrentTargetBossId)
                        ? $"Trigger {quest.CurrentTargetCount} boss breaks"
                        : $"Trigger {quest.CurrentTargetCount} boss breaks ({quest.CurrentTargetBossId})";
                case QuestType.BossDefeat:
                    return string.IsNullOrEmpty(quest.CurrentTargetBossId)
                        ? "Defeat the boss"
                        : $"Defeat {quest.CurrentTargetBossId}";
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

        private void OnGUI()
        {
            if (!ShouldUseFallbackOverlay())
            {
                return;
            }

            if (questSystem == null)
            {
                questSystem = FindObjectOfType<QuestSystem>();
            }

            if (questSystem == null)
            {
                return;
            }

            List<QuestProgress> quests = questSystem.GetActiveQuests();
            if (quests == null || quests.Count == 0)
            {
                return;
            }

            EnsureFallbackStyles();

            int visibleCount = Mathf.Min(maxVisibleQuests, quests.Count);
            float lineHeight = fallbackFontSize + 6f;
            float panelHeight = 18f + visibleCount * (lineHeight * 2f + 6f);
            Rect panelRect = new Rect(fallbackPosition.x, fallbackPosition.y, fallbackWidth, panelHeight);

            GUILayout.BeginArea(panelRect, GUI.skin.box);
            GUILayout.Label("任务追踪", fallbackTitleStyle);

            for (int i = 0; i < visibleCount; i++)
            {
                QuestProgress quest = quests[i];
                if (quest == null || quest.data == null)
                {
                    continue;
                }

                GUILayout.Label(GetQuestDisplayName(quest), fallbackBodyStyle);
                GUILayout.Label($"{GetQuestProgressText(quest)} · {GetQuestTypeText(quest)}", fallbackBodyStyle);
                if (i < visibleCount - 1)
                {
                    GUILayout.Space(4f);
                }
            }

            GUILayout.EndArea();
        }

        private bool ShouldUseFallbackOverlay()
        {
            if (!useFallbackOverlay)
            {
                return false;
            }

            return questListContainer == null || questItemPrefab == null;
        }

        private void EnsureFallbackStyles()
        {
            if (fallbackTitleStyle == null)
            {
                fallbackTitleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = fallbackFontSize + 1,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white }
                };
            }

            if (fallbackBodyStyle == null)
            {
                fallbackBodyStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = fallbackFontSize,
                    wordWrap = true,
                    normal = { textColor = new Color(0.9f, 0.95f, 1f, 1f) }
                };
            }
        }

        private void EnsureQuestItemTextComponents(GameObject itemObj)
        {
            if (itemObj == null)
            {
                return;
            }

            TextMeshProUGUI[] texts = itemObj.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                EnsureTmpTextSafe(texts[i]);
            }
        }

        private static void EnsureTmpTextSafe(TextMeshProUGUI text)
        {
            if (text == null)
            {
                return;
            }

            if (text.font == null)
            {
                text.font = TMP_Settings.defaultFontAsset;
            }

            if (text.font != null && text.fontSharedMaterial == null)
            {
                text.fontSharedMaterial = text.font.material;
            }
        }

        private void EnsureQuestSystem()
        {
            if (questSystem == null)
            {
                questSystem = FindObjectOfType<QuestSystem>();
            }
        }

        private void LogStartupStatus()
        {
            if (!logStartupStatus || startupLogged)
            {
                return;
            }

            startupLogged = true;
            bool fallback = ShouldUseFallbackOverlay();
            Debug.Log($"[UI_QuestTracker] Startup | questSystem={(questSystem != null)} container={(questListContainer != null)} itemPrefab={(questItemPrefab != null)} fallback={fallback}");
        }
    }
}
