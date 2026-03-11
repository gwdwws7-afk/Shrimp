using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    public class LongTermProgressionSystem : MonoBehaviour
    {
        [Header("Data")]
        public ProgressionMilestoneData milestoneData;

        [Header("Route")]
        public ProgressionRoute activeRoute = ProgressionRoute.Offense;
        public bool allowRouteSwitch = true;

        [Header("Behavior")]
        public bool autoApplyOnStart = true;
        public bool autoSaveOnMilestone = true;
        public bool showMessages = true;

        [Header("References")]
        public SaveManager saveManager;
        public StatisticsManager statisticsManager;
        public TalentTree talentTree;
        public PearlEquipment equipment;
        public PearlDropManager dropManager;

        private void Awake()
        {
            if (saveManager == null)
            {
                saveManager = FindObjectOfType<SaveManager>();
            }

            if (statisticsManager == null)
            {
                statisticsManager = FindObjectOfType<StatisticsManager>();
            }

            if (talentTree == null)
            {
                talentTree = FindObjectOfType<TalentTree>();
            }

            if (equipment == null)
            {
                equipment = FindObjectOfType<PearlEquipment>();
            }

            if (dropManager == null)
            {
                dropManager = FindObjectOfType<PearlDropManager>();
            }
        }

        private void OnEnable()
        {
            GameEvents.OnLevelCompleted += HandleLevelCompleted;
            GameEvents.OnPearlCollected += HandlePearlCollected;
            GameEvents.OnEnemyKilled += HandleEnemyKilled;
        }

        private void OnDisable()
        {
            GameEvents.OnLevelCompleted -= HandleLevelCompleted;
            GameEvents.OnPearlCollected -= HandlePearlCollected;
            GameEvents.OnEnemyKilled -= HandleEnemyKilled;
        }

        private void Start()
        {
            if (autoApplyOnStart)
            {
                LoadRouteFromSave();
                ApplySavedProgression();
                EvaluateMilestones();
            }
        }

        public void SetActiveRoute(ProgressionRoute route)
        {
            if (!allowRouteSwitch)
            {
                return;
            }

            if (activeRoute == route)
            {
                return;
            }

            activeRoute = route;
            SaveRouteToData();
            EvaluateMilestones();
        }

        public void ApplySavedProgression()
        {
            if (saveManager == null || saveManager.CurrentData == null)
            {
                return;
            }

            GameData data = saveManager.CurrentData;
            int targetSlots = Mathf.Max(0, data.unlockedPearlSlots);
            if (equipment != null && targetSlots > equipment.slotCount)
            {
                equipment.slotCount = targetSlots;
                equipment.EnsureSlotCount();
                equipment.NotifyChanged();
            }

            if (dropManager != null)
            {
                PearlRarity cap = (PearlRarity)Mathf.Clamp(data.maxPearlRarityUnlocked, 0, (int)PearlRarity.Legendary);
                float dropMultiplier = Mathf.Max(0.1f, data.pearlDropRateMultiplier);
                dropManager.ApplyProgressionCaps(cap, dropMultiplier);
            }
        }

        private void HandleLevelCompleted(int levelId)
        {
            if (saveManager == null || saveManager.CurrentData == null)
            {
                return;
            }

            GameData data = saveManager.CurrentData;
            if (!data.completedLevels.Contains(levelId))
            {
                data.completedLevels.Add(levelId);
                data.totalLevelsCompleted = data.completedLevels.Count;
            }

            EvaluateMilestones();
        }

        private void HandlePearlCollected(string pearlId)
        {
            EvaluateMilestones();
        }

        private void HandleEnemyKilled(EnemyType type, Vector3 position, int expReward)
        {
            if (statisticsManager != null && statisticsManager.totalKills % 25 == 0)
            {
                EvaluateMilestones();
            }
        }

        public void EvaluateMilestones()
        {
            if (milestoneData == null || milestoneData.milestones == null || milestoneData.milestones.Count == 0)
            {
                return;
            }

            if (saveManager == null || saveManager.CurrentData == null)
            {
                return;
            }

            GameData data = saveManager.CurrentData;
            int totalKills = statisticsManager != null ? statisticsManager.totalKills : data.totalKills;
            int pearlsCollected = statisticsManager != null ? statisticsManager.pearlsCollected : data.pearlsCollected;
            int levelsCompleted = data.completedLevels != null ? data.completedLevels.Count : data.totalLevelsCompleted;
            int longestCombo = statisticsManager != null ? statisticsManager.longestCombo : data.longestCombo;

            for (int i = 0; i < milestoneData.milestones.Count; i++)
            {
                ProgressionMilestone milestone = milestoneData.milestones[i];
                if (milestone == null || string.IsNullOrEmpty(milestone.id))
                {
                    continue;
                }

                if (data.claimedProgressionMilestones.Contains(milestone.id))
                {
                    continue;
                }

                if (milestone.route != activeRoute)
                {
                    continue;
                }

                if (totalKills < milestone.requiredTotalKills
                    || pearlsCollected < milestone.requiredPearlsCollected
                    || levelsCompleted < milestone.requiredLevelsCompleted
                    || longestCombo < milestone.requiredLongestCombo)
                {
                    continue;
                }

                ApplyMilestone(milestone, data);
            }
        }

        public string GetNextMilestoneStatus()
        {
            if (milestoneData == null || milestoneData.milestones == null || milestoneData.milestones.Count == 0)
            {
                return string.Empty;
            }

            if (saveManager == null || saveManager.CurrentData == null)
            {
                return string.Empty;
            }

            GameData data = saveManager.CurrentData;
            int totalKills = statisticsManager != null ? statisticsManager.totalKills : data.totalKills;
            int pearlsCollected = statisticsManager != null ? statisticsManager.pearlsCollected : data.pearlsCollected;
            int levelsCompleted = data.completedLevels != null ? data.completedLevels.Count : data.totalLevelsCompleted;
            int longestCombo = statisticsManager != null ? statisticsManager.longestCombo : data.longestCombo;

            ProgressionMilestone next = null;
            for (int i = 0; i < milestoneData.milestones.Count; i++)
            {
                ProgressionMilestone milestone = milestoneData.milestones[i];
                if (milestone == null || string.IsNullOrEmpty(milestone.id))
                {
                    continue;
                }

                if (milestone.route != activeRoute)
                {
                    continue;
                }

                if (data.claimedProgressionMilestones.Contains(milestone.id))
                {
                    continue;
                }

                next = milestone;
                break;
            }

            if (next == null)
            {
                return "里程碑已全部完成.";
            }

            List<string> remainingParts = new List<string>();
            if (next.requiredTotalKills > 0)
            {
                int remaining = Mathf.Max(0, next.requiredTotalKills - totalKills);
                remainingParts.Add($"击杀: {remaining}");
            }

            if (next.requiredPearlsCollected > 0)
            {
                int remaining = Mathf.Max(0, next.requiredPearlsCollected - pearlsCollected);
                remainingParts.Add($"珍珠: {remaining}");
            }

            if (next.requiredLevelsCompleted > 0)
            {
                int remaining = Mathf.Max(0, next.requiredLevelsCompleted - levelsCompleted);
                remainingParts.Add($"关卡: {remaining}");
            }

            if (next.requiredLongestCombo > 0)
            {
                int remaining = Mathf.Max(0, next.requiredLongestCombo - longestCombo);
                remainingParts.Add($"连击: {remaining}");
            }

            string remainingText = remainingParts.Count > 0
                ? string.Join(" | ", remainingParts)
                : "Unlockable";

            string title = string.IsNullOrEmpty(next.title) ? "Next Milestone" : next.title;
            return $"Next milestone: {title} ({remainingText})";
        }

        private void ApplyMilestone(ProgressionMilestone milestone, GameData data)
        {
            data.claimedProgressionMilestones.Add(milestone.id);
            GameEvents.ProgressionMilestoneClaimed(milestone.id, milestone.route);

            if (milestone.grantTalentPoints > 0 && talentTree != null)
            {
                talentTree.availablePoints += milestone.grantTalentPoints;
                talentTree.NotifyChanged();
            }

            if (milestone.grantPearlSlots > 0)
            {
                int baseSlots = Mathf.Max(data.unlockedPearlSlots, equipment != null ? equipment.slotCount : 0);
                data.unlockedPearlSlots = baseSlots + milestone.grantPearlSlots;
                if (equipment != null)
                {
                    equipment.slotCount = data.unlockedPearlSlots;
                    equipment.EnsureSlotCount();
                    equipment.NotifyChanged();
                }
            }

            int maxRarity = Mathf.Clamp((int)milestone.unlockMaxRarity, 0, (int)PearlRarity.Legendary);
            if (maxRarity > data.maxPearlRarityUnlocked)
            {
                data.maxPearlRarityUnlocked = maxRarity;
            }

            if (milestone.dropRateMultiplier > 1f)
            {
                data.pearlDropRateMultiplier = Mathf.Max(0.1f, data.pearlDropRateMultiplier * milestone.dropRateMultiplier);
            }

            ApplySavedProgression();

            if (showMessages)
            {
                string label = string.IsNullOrEmpty(milestone.title) ? "Milestone" : milestone.title;
                GameEvents.ShowMessage($"里程碑达成: {label}", 2f);
            }

            if (autoSaveOnMilestone)
            {
                saveManager.SaveGame();
            }
        }

        private void LoadRouteFromSave()
        {
            if (saveManager == null || saveManager.CurrentData == null)
            {
                return;
            }

            string saved = saveManager.CurrentData.activeProgressionRoute;
            if (!string.IsNullOrEmpty(saved) && System.Enum.TryParse(saved, out ProgressionRoute parsed))
            {
                activeRoute = parsed;
            }
        }

        private void SaveRouteToData()
        {
            if (saveManager == null || saveManager.CurrentData == null)
            {
                return;
            }

            saveManager.CurrentData.activeProgressionRoute = activeRoute.ToString();
            if (autoSaveOnMilestone)
            {
                saveManager.SaveGame();
            }
        }
    }
}
