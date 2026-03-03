using UnityEngine;

namespace ThirdPersonController
{
    public class LevelRewardSystem : MonoBehaviour
    {
        [Header("Config")]
        public LevelData levelData;
        public int levelIdOverride = 0;
        public bool grantOnVictoryOnly = true;
        public bool showMessages = true;

        [Header("Economy")]
        public float expRewardMultiplier = 1f;
        public float pearlRewardMultiplier = 1f;
        public float levelRewardMultiplier = 1f;
        public int levelDifficulty = 1;

        [Header("References")]
        public PlayerExperienceSystem experienceSystem;
        public PearlDatabase pearlDatabase;
        public PearlInventory inventory;
        public CurrencyWallet wallet;

        private bool granted;

        private void Awake()
        {
            if (experienceSystem == null)
            {
                experienceSystem = FindObjectOfType<PlayerExperienceSystem>();
            }

            if (inventory == null)
            {
                inventory = FindObjectOfType<PearlInventory>();
            }

            if (pearlDatabase == null)
            {
                pearlDatabase = FindObjectOfType<PearlDatabase>();
            }

            if (wallet == null)
            {
                wallet = FindObjectOfType<CurrencyWallet>();
                if (wallet == null)
                {
                    wallet = CurrencyWallet.EnsureInstance();
                }
            }

            if (levelData != null)
            {
                levelDifficulty = Mathf.Max(0, (int)levelData.difficulty);
            }
        }

        private void OnEnable()
        {
            GameEvents.OnLevelCompleted += HandleLevelCompleted;
        }

        private void OnDisable()
        {
            GameEvents.OnLevelCompleted -= HandleLevelCompleted;
        }

        private void HandleLevelCompleted(int completedLevelId)
        {
            if (grantOnVictoryOnly && completedLevelId <= 0)
            {
                return;
            }

            if (granted)
            {
                return;
            }

            int expectedId = ResolveLevelId();
            if (expectedId > 0 && completedLevelId != expectedId)
            {
                return;
            }

            GrantRewards();
        }

        public void GrantRewards()
        {
            if (levelData == null)
            {
                return;
            }

            granted = true;

            float rewardMultiplier = Mathf.Max(0f, expRewardMultiplier) * Mathf.Max(0f, levelRewardMultiplier);
            int expReward = EconomyService.AdjustLevelExp(levelData.baseExp, levelDifficulty, rewardMultiplier);
            if (experienceSystem != null && expReward > 0)
            {
                experienceSystem.GrantExperience(expReward);
                if (showMessages)
                {
                    GameEvents.ShowMessage($"+{expReward} EXP", 1.4f);
                }
            }

            float pearlMultiplier = Mathf.Max(0f, pearlRewardMultiplier) * Mathf.Max(0f, levelRewardMultiplier);
            int pearlReward = EconomyService.AdjustLevelPearls(levelData.basePearls, levelDifficulty, pearlMultiplier);
            if (pearlReward > 0)
            {
                GrantPearls(pearlReward);
            }

            int creditReward = EconomyService.AdjustLevelCredits(levelData.baseCredits, levelDifficulty, levelRewardMultiplier);
            if (wallet != null && creditReward > 0)
            {
                wallet.AddCredits(creditReward);
            }
        }

        private void GrantPearls(int count)
        {
            if (count <= 0)
            {
                return;
            }

            if (inventory == null)
            {
                return;
            }

            int grantedCount = 0;
            for (int i = 0; i < count; i++)
            {
                PearlItem pearl = PickRandomPearl();
                if (pearl == null)
                {
                    continue;
                }

                if (inventory.AddPearl(pearl))
                {
                    grantedCount++;
                }
            }

            if (showMessages && grantedCount > 0)
            {
                GameEvents.ShowMessage($"+{grantedCount} Pearls", 1.4f);
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

        private int ResolveLevelId()
        {
            if (levelIdOverride > 0)
            {
                return levelIdOverride;
            }

            if (levelData == null || levelData.chapterId <= 0)
            {
                return 0;
            }

            if (!string.IsNullOrEmpty(levelData.levelId) && levelData.levelId.StartsWith("LEVEL_"))
            {
                if (int.TryParse(levelData.levelId.Replace("LEVEL_", string.Empty), out int parsed))
                {
                    return levelData.chapterId * 100 + parsed;
                }
            }

            return 0;
        }
    }
}
