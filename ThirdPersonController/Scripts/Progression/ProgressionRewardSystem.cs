using UnityEngine;

namespace ThirdPersonController
{
    public class ProgressionRewardSystem : MonoBehaviour
    {
        [Header("References")]
        public TalentTree talentTree;

        [Header("Kill Milestones")]
        public int killsPerPoint = 20;
        public int pointsPerMilestone = 1;

        [Header("Stage Clear")]
        public int pointsPerStageClear = 2;

        private int killsSinceLastPoint = 0;

        private void Awake()
        {
            if (talentTree == null)
            {
                talentTree = FindObjectOfType<TalentTree>();
            }

            LoadProgress();
            ApplyEconomyOverrides();
        }

        private void OnEnable()
        {
            GameEvents.OnEnemyKilled += HandleEnemyKilled;
            GameEvents.OnLevelCompleted += HandleLevelCompleted;
            ApplyEconomyOverrides();
        }

        private void OnDisable()
        {
            GameEvents.OnEnemyKilled -= HandleEnemyKilled;
            GameEvents.OnLevelCompleted -= HandleLevelCompleted;
        }

        private void HandleEnemyKilled(EnemyType type, Vector3 position, int expReward)
        {
            if (killsPerPoint <= 0 || pointsPerMilestone <= 0)
            {
                return;
            }

            killsSinceLastPoint++;
            UpdateTotalKills();

            while (killsSinceLastPoint >= killsPerPoint)
            {
                killsSinceLastPoint -= killsPerPoint;
                GrantTalentPoints(pointsPerMilestone, "获得天赋点!");
            }

            SaveProgress();
        }

        private void HandleLevelCompleted(int levelId)
        {
            if (pointsPerStageClear <= 0)
            {
                return;
            }

            GrantTalentPoints(pointsPerStageClear, "关卡奖励天赋点!");
            SaveProgress();
        }

        private void GrantTalentPoints(int amount, string message)
        {
            if (talentTree == null || amount <= 0)
            {
                return;
            }

            talentTree.availablePoints += amount;
            talentTree.NotifyChanged();
            GameEvents.ShowMessage(message, 2f);
        }

        private void UpdateTotalKills()
        {
            if (SaveManager.Instance == null || SaveManager.Instance.CurrentData == null)
            {
                return;
            }

            SaveManager.Instance.CurrentData.enemiesKilled += 1;
        }

        private void LoadProgress()
        {
            if (SaveManager.Instance == null || SaveManager.Instance.CurrentData == null)
            {
                return;
            }

            killsSinceLastPoint = SaveManager.Instance.CurrentData.killsSinceLastTalentPoint;
        }

        private void SaveProgress()
        {
            if (SaveManager.Instance == null || SaveManager.Instance.CurrentData == null)
            {
                return;
            }

            SaveManager.Instance.CurrentData.killsSinceLastTalentPoint = killsSinceLastPoint;
            SaveManager.Instance.CurrentData.talentPoints = talentTree != null ? talentTree.availablePoints : 0;
        }

        private void ApplyEconomyOverrides()
        {
            EconomyConfig config = EconomyService.Config;
            if (config == null)
            {
                return;
            }

            killsPerPoint = config.killsPerTalentPoint;
            pointsPerMilestone = config.pointsPerKillMilestone;
            pointsPerStageClear = config.pointsPerStageClear;
        }
    }
}
