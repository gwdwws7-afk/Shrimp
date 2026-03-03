using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    public enum ProgressionRoute
    {
        Offense,
        Control,
        Survival
    }

    [System.Serializable]
    public class ProgressionMilestone
    {
        public string id = "";
        public string title = "New Milestone";
        [TextArea(2, 4)]
        public string description;

        [Header("Route")]
        public ProgressionRoute route = ProgressionRoute.Offense;

        [Header("Requirements")]
        public int requiredTotalKills = 0;
        public int requiredPearlsCollected = 0;
        public int requiredLevelsCompleted = 0;
        public int requiredLongestCombo = 0;

        [Header("Rewards")]
        public int grantTalentPoints = 0;
        public int grantPearlSlots = 0;
        public PearlRarity unlockMaxRarity = PearlRarity.Common;
        public float dropRateMultiplier = 1f;
    }

    [CreateAssetMenu(fileName = "ProgressionMilestones", menuName = "Progression/Progression Milestones")]
    public class ProgressionMilestoneData : ScriptableObject
    {
        public List<ProgressionMilestone> milestones = new List<ProgressionMilestone>();
    }
}
