using UnityEngine;

namespace ThirdPersonController
{
    public static class EconomyService
    {
        public static EconomyConfig Config { get; private set; }

        public static void Configure(EconomyConfig config)
        {
            Config = config;
        }

        public static int AdjustEnemyExp(int baseExp, EnemyType type)
        {
            if (baseExp <= 0)
            {
                return 0;
            }

            if (Config == null)
            {
                return baseExp;
            }

            float typeMultiplier = type switch
            {
                EnemyType.Rusher => Config.rusherExpMultiplier,
                EnemyType.Tank => Config.tankExpMultiplier,
                EnemyType.Elite => Config.eliteExpMultiplier,
                EnemyType.Mutant => Config.mutantExpMultiplier,
                EnemyType.Boss => Config.bossExpMultiplier,
                _ => Config.gruntExpMultiplier
            };

            float multiplier = Mathf.Max(0f, Config.enemyExpMultiplier) * Mathf.Max(0f, typeMultiplier);
            return Mathf.Max(0, Mathf.RoundToInt(baseExp * multiplier));
        }

        public static int AdjustLevelExp(int baseExp)
        {
            return AdjustLevelExp(baseExp, 1, 1f);
        }

        public static int AdjustQuestExp(int baseExp)
        {
            return AdjustQuestExp(baseExp, QuestType.Kill, 1, 1, 1f);
        }

        public static int AdjustLevelExp(int baseExp, int difficulty, float levelRewardMultiplier)
        {
            if (baseExp <= 0)
            {
                return 0;
            }

            float rewardMultiplier = Mathf.Max(0f, levelRewardMultiplier);
            if (Config == null)
            {
                return Mathf.Max(0, Mathf.RoundToInt(baseExp * rewardMultiplier));
            }

            float difficultyMultiplier = GetDifficultyMultiplier(Config.levelExpDifficultyMultipliers, difficulty);
            float multiplier = Mathf.Max(0f, Config.levelExpMultiplier) * Mathf.Max(0f, difficultyMultiplier) * rewardMultiplier;
            return Mathf.Max(0, Mathf.RoundToInt(baseExp * multiplier));
        }

        public static int AdjustLevelPearls(int baseCount, int difficulty, float levelRewardMultiplier)
        {
            if (baseCount <= 0)
            {
                return 0;
            }

            float rewardMultiplier = Mathf.Max(0f, levelRewardMultiplier);
            if (Config == null)
            {
                return Mathf.Max(0, Mathf.RoundToInt(baseCount * rewardMultiplier));
            }

            float difficultyMultiplier = GetDifficultyMultiplier(Config.levelPearlDifficultyMultipliers, difficulty);
            float multiplier = Mathf.Max(0f, Config.levelPearlMultiplier) * Mathf.Max(0f, difficultyMultiplier) * rewardMultiplier;
            return Mathf.Max(0, Mathf.RoundToInt(baseCount * multiplier));
        }

        public static int AdjustLevelCredits(int baseCredits, int difficulty, float levelRewardMultiplier)
        {
            if (baseCredits <= 0)
            {
                return 0;
            }

            float rewardMultiplier = Mathf.Max(0f, levelRewardMultiplier);
            if (Config == null)
            {
                return Mathf.Max(0, Mathf.RoundToInt(baseCredits * rewardMultiplier));
            }

            float difficultyMultiplier = GetDifficultyMultiplier(Config.levelCreditDifficultyMultipliers, difficulty);
            float multiplier = Mathf.Max(0f, Config.levelCreditMultiplier) * Mathf.Max(0f, difficultyMultiplier) * rewardMultiplier;
            return Mathf.Max(0, Mathf.RoundToInt(baseCredits * multiplier));
        }

        public static int AdjustQuestExp(int baseExp, QuestType questType, int questDifficulty, int levelDifficulty, float rewardMultiplier)
        {
            if (baseExp <= 0)
            {
                return 0;
            }

            float externalMultiplier = Mathf.Max(0f, rewardMultiplier);
            if (Config == null)
            {
                return Mathf.Max(0, Mathf.RoundToInt(baseExp * externalMultiplier));
            }

            float typeMultiplier = GetQuestTypeMultiplier(questType, multiplier => multiplier.expMultiplier);
            float difficultyMultiplier = GetDifficultyMultiplier(Config.questExpDifficultyMultipliers, questDifficulty);
            float levelMultiplier = GetDifficultyMultiplier(Config.levelExpDifficultyMultipliers, levelDifficulty);
            float multiplier = Mathf.Max(0f, Config.questExpMultiplier) * Mathf.Max(0f, typeMultiplier) * Mathf.Max(0f, difficultyMultiplier) * Mathf.Max(0f, levelMultiplier) * externalMultiplier;
            return Mathf.Max(0, Mathf.RoundToInt(baseExp * multiplier));
        }

        public static int AdjustQuestPearls(int baseCount, QuestType questType, int questDifficulty, int levelDifficulty, float rewardMultiplier)
        {
            if (baseCount <= 0)
            {
                return 0;
            }

            float externalMultiplier = Mathf.Max(0f, rewardMultiplier);
            if (Config == null)
            {
                return Mathf.Max(0, Mathf.RoundToInt(baseCount * externalMultiplier));
            }

            float typeMultiplier = GetQuestTypeMultiplier(questType, multiplier => multiplier.pearlMultiplier);
            float difficultyMultiplier = GetDifficultyMultiplier(Config.questPearlDifficultyMultipliers, questDifficulty);
            float levelMultiplier = GetDifficultyMultiplier(Config.levelPearlDifficultyMultipliers, levelDifficulty);
            float multiplier = Mathf.Max(0f, Config.questPearlMultiplier) * Mathf.Max(0f, typeMultiplier) * Mathf.Max(0f, difficultyMultiplier) * Mathf.Max(0f, levelMultiplier) * externalMultiplier;
            return Mathf.Max(0, Mathf.RoundToInt(baseCount * multiplier));
        }

        public static int AdjustQuestCredits(int baseCredits, QuestType questType, int questDifficulty, int levelDifficulty, float rewardMultiplier)
        {
            if (baseCredits <= 0)
            {
                return 0;
            }

            float externalMultiplier = Mathf.Max(0f, rewardMultiplier);
            if (Config == null)
            {
                return Mathf.Max(0, Mathf.RoundToInt(baseCredits * externalMultiplier));
            }

            float typeMultiplier = GetQuestTypeMultiplier(questType, multiplier => multiplier.creditMultiplier);
            float difficultyMultiplier = GetDifficultyMultiplier(Config.questCreditDifficultyMultipliers, questDifficulty);
            float levelMultiplier = GetDifficultyMultiplier(Config.levelCreditDifficultyMultipliers, levelDifficulty);
            float multiplier = Mathf.Max(0f, Config.questCreditMultiplier) * Mathf.Max(0f, typeMultiplier) * Mathf.Max(0f, difficultyMultiplier) * Mathf.Max(0f, levelMultiplier) * externalMultiplier;
            return Mathf.Max(0, Mathf.RoundToInt(baseCredits * multiplier));
        }

        public static int AdjustPearlReward(int baseCount, float rewardMultiplier)
        {
            if (baseCount <= 0)
            {
                return 0;
            }

            float multiplier = Mathf.Max(0f, rewardMultiplier);
            return Mathf.Max(0, Mathf.RoundToInt(baseCount * multiplier));
        }

        public static float GetPearlDropMultiplier()
        {
            if (Config == null)
            {
                return 1f;
            }

            return Mathf.Max(0f, Config.pearlDropMultiplier);
        }

        public static float GetDropChanceMultiplier(int difficulty)
        {
            if (Config == null)
            {
                return 1f;
            }

            return Mathf.Max(0f, GetDifficultyMultiplier(Config.dropChanceDifficultyMultipliers, difficulty));
        }

        public static int AdjustShopPrice(int basePrice, int difficulty, float priceMultiplier)
        {
            if (basePrice <= 0)
            {
                return 0;
            }

            float externalMultiplier = Mathf.Max(0f, priceMultiplier);
            if (Config == null)
            {
                return Mathf.Max(1, Mathf.RoundToInt(basePrice * externalMultiplier));
            }

            float difficultyMultiplier = GetDifficultyMultiplier(Config.shopPriceDifficultyMultipliers, difficulty);
            float multiplier = Mathf.Max(0f, Config.shopPriceMultiplier) * Mathf.Max(0f, difficultyMultiplier) * externalMultiplier;
            return Mathf.Max(1, Mathf.RoundToInt(basePrice * multiplier));
        }

        private static float GetDifficultyMultiplier(float[] table, int difficulty)
        {
            if (table == null || table.Length == 0)
            {
                return 1f;
            }

            int index = Mathf.Clamp(difficulty, 0, table.Length - 1);
            return Mathf.Max(0f, table[index]);
        }

        private static float GetQuestTypeMultiplier(QuestType questType, System.Func<QuestTypeRewardMultiplier, float> selector)
        {
            if (Config == null || Config.questTypeMultipliers == null || selector == null)
            {
                return 1f;
            }

            for (int i = 0; i < Config.questTypeMultipliers.Count; i++)
            {
                QuestTypeRewardMultiplier entry = Config.questTypeMultipliers[i];
                if (entry != null && entry.questType == questType)
                {
                    return Mathf.Max(0f, selector(entry));
                }
            }

            return 1f;
        }
    }
}
