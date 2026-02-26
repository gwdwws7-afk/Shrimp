using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    public enum LevelDifficulty
    {
        Easy,
        Normal,
        Hard,
        Nightmare
    }

    public enum LevelType
    {
        Campaign,
        Survival,
        Challenge,
        Tutorial
    }

    [CreateAssetMenu(fileName = "LevelData_", menuName = "Progression/Level Data")]
    public class LevelData : ScriptableObject
    {
        [Header("Basic Info")]
        public string levelId = "";
        public string levelName = "New Level";
        [TextArea(2, 4)]
        public string description;
        public int chapterId = 1;
        
        [Header("Difficulty")]
        public LevelDifficulty difficulty = LevelDifficulty.Normal;
        public int recommendedLevel = 1;
        public int recommendedPower = 100;
        
        [Header("Type")]
        public LevelType levelType = LevelType.Campaign;
        
        [Header("Limits")]
        public float timeLimit = 0f;
        public int scoreTarget = 0;
        
        [Header("Strongholds")]
        public List<StrongholdConfig> strongholds = new List<StrongholdConfig>();
        
        [Header("Quests")]
        public List<QuestConfig> quests = new List<QuestConfig>();
        
        [Header("Unlocks")]
        public string nextLevelId = "";
        public int starsRequired = 1;
        public int unlockCost = 0;
        
        [Header("Rewards")]
        public int baseExp = 100;
        public int basePearls = 1;
        
        [Header("Scene")]
        public string sceneName = "";
        
        public string GetId()
        {
            if (!string.IsNullOrEmpty(levelId))
            {
                return levelId;
            }
            return name;
        }
    }

    [System.Serializable]
    public class StrongholdConfig
    {
        public string strongholdId = "";
        public bool required = true;
        public int order = 0;
    }

    [System.Serializable]
    public class QuestConfig
    {
        public string questId = "";
        public bool required = false;
        public int order = 0;
    }
}
