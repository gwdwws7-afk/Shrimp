using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    [CreateAssetMenu(fileName = "EconomyConfig", menuName = "Progression/Economy Config")]
    public class EconomyConfig : ScriptableObject
    {
        [Header("Experience Multipliers")]
        public float enemyExpMultiplier = 1f;
        public float levelExpMultiplier = 1f;
        public float questExpMultiplier = 1f;

        [Header("Enemy Type Multipliers")]
        public float gruntExpMultiplier = 1f;
        public float rusherExpMultiplier = 1.1f;
        public float tankExpMultiplier = 1.3f;
        public float eliteExpMultiplier = 2f;
        public float mutantExpMultiplier = 1.6f;
        public float bossExpMultiplier = 6f;

        [Header("Talent Points")]
        public int killsPerTalentPoint = 30;
        public int pointsPerKillMilestone = 1;
        public int pointsPerStageClear = 2;

        [Header("Pearl Rewards")]
        public float pearlDropMultiplier = 1f;
        public float levelPearlMultiplier = 1f;
        public float questPearlMultiplier = 1f;

        [Header("Credit Rewards")]
        public float levelCreditMultiplier = 1f;
        public float questCreditMultiplier = 1f;

        [Header("Difficulty Multipliers (Index = Difficulty)")]
        public float[] levelExpDifficultyMultipliers = new float[] { 1f, 1f, 1.1f, 1.25f, 1.4f };
        public float[] levelPearlDifficultyMultipliers = new float[] { 1f, 1f, 1.05f, 1.1f, 1.2f };
        public float[] levelCreditDifficultyMultipliers = new float[] { 1f, 1f, 1.1f, 1.25f, 1.4f };
        public float[] questExpDifficultyMultipliers = new float[] { 1f, 1f, 1.05f, 1.1f, 1.2f };
        public float[] questPearlDifficultyMultipliers = new float[] { 1f, 1f, 1.05f, 1.1f, 1.2f };
        public float[] questCreditDifficultyMultipliers = new float[] { 1f, 1f, 1.1f, 1.2f, 1.3f };
        public float[] dropChanceDifficultyMultipliers = new float[] { 1f, 1f, 1.05f, 1.1f, 1.15f };
        public float[] shopPriceDifficultyMultipliers = new float[] { 1f, 1f, 1.05f, 1.1f, 1.2f };

        [Header("Quest Type Multipliers")]
        public List<QuestTypeRewardMultiplier> questTypeMultipliers = new List<QuestTypeRewardMultiplier>();

        [Header("Shop Pricing")]
        public float shopPriceMultiplier = 1f;
    }

    [System.Serializable]
    public class QuestTypeRewardMultiplier
    {
        public QuestType questType = QuestType.Kill;
        public float expMultiplier = 1f;
        public float pearlMultiplier = 1f;
        public float creditMultiplier = 1f;
    }
}
