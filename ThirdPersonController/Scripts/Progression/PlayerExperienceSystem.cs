using UnityEngine;

namespace ThirdPersonController
{
    public class PlayerExperienceSystem : MonoBehaviour
    {
        [Header("Progression")]
        public int level = 1;
        public int currentExp = 0;
        public int maxLevel = 50;
        public int baseExpToNext = 120;
        public float expGrowth = 1.2f;
        public bool useCurve = false;
        public AnimationCurve expCurve = AnimationCurve.EaseInOut(1f, 120f, 50f, 3000f);

        [Header("Rewards")]
        public int talentPointsPerLevel = 1;

        [Header("References")]
        public TalentTree talentTree;

        public int ExpToNext => GetExpToNextLevel(level);

        private void Awake()
        {
            if (talentTree == null)
            {
                talentTree = FindObjectOfType<TalentTree>();
            }

            LoadFromSave();
        }

        private void OnEnable()
        {
            GameEvents.OnEnemyKilled += HandleEnemyKilled;
        }

        private void OnDisable()
        {
            GameEvents.OnEnemyKilled -= HandleEnemyKilled;
        }

        public void GrantExperience(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            ApplyExperience(amount);
            GameEvents.ExperienceGained(amount);
            SaveToData();
        }

        private void ApplyExperience(int amount)
        {
            if (maxLevel > 0 && level >= maxLevel)
            {
                currentExp = Mathf.Min(currentExp, ExpToNext - 1);
                return;
            }

            currentExp = Mathf.Max(0, currentExp + amount);
            int expToNext = ExpToNext;
            while (expToNext > 0 && currentExp >= expToNext && (maxLevel <= 0 || level < maxLevel))
            {
                currentExp -= expToNext;
                LevelUp();
                expToNext = ExpToNext;
            }
        }

        private void LevelUp()
        {
            level += 1;
            GameEvents.LevelUp(level);

            if (talentTree != null && talentPointsPerLevel > 0)
            {
                talentTree.availablePoints += talentPointsPerLevel;
                talentTree.NotifyChanged();
            }

            GameEvents.ShowMessage($"Level up! {level}", 2f);
        }

        private void HandleEnemyKilled(EnemyType type, Vector3 position, int expReward)
        {
            if (expReward <= 0)
            {
                return;
            }

            GrantExperience(expReward);
        }

        private int GetExpToNextLevel(int currentLevel)
        {
            int clampedLevel = Mathf.Max(1, currentLevel);
            if (useCurve && expCurve != null)
            {
                return Mathf.Max(1, Mathf.RoundToInt(expCurve.Evaluate(clampedLevel)));
            }

            return Mathf.Max(1, Mathf.RoundToInt(baseExpToNext * Mathf.Pow(expGrowth, clampedLevel - 1)));
        }

        private void LoadFromSave()
        {
            if (SaveManager.Instance == null || SaveManager.Instance.CurrentData == null)
            {
                return;
            }

            level = Mathf.Max(1, SaveManager.Instance.CurrentData.playerLevel);
            currentExp = Mathf.Max(0, SaveManager.Instance.CurrentData.currentExp);
        }

        private void SaveToData()
        {
            if (SaveManager.Instance == null || SaveManager.Instance.CurrentData == null)
            {
                return;
            }

            SaveManager.Instance.CurrentData.playerLevel = level;
            SaveManager.Instance.CurrentData.currentExp = currentExp;
        }
    }
}
