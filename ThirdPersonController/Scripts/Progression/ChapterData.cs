using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    [CreateAssetMenu(fileName = "ChapterData_", menuName = "Progression/Chapter Data")]
    public class ChapterData : ScriptableObject
    {
        [Header("Basic Info")]
        public string chapterId = "";
        public string chapterName = "New Chapter";
        [TextArea(2, 4)]
        public string description;
        
        [Header("Progression")]
        public int chapterNumber = 1;
        public string previousChapterId = "";
        public string nextChapterId = "";
        
        [Header("Levels")]
        public List<LevelData> levels = new List<LevelData>();
        
        [Header("Boss")]
        public LevelData bossLevel;
        
        [Header("Unlocks")]
        public int unlockCost = 0;
        public int requiredPlayerLevel = 1;
        
        [Header("Rewards")]
        public int chapterCompleteBonus = 500;
        public int chapterCompletePearls = 5;
        
        [Header("Theme")]
        public string themeName = "Abyss";
        public Color themeColor = new Color(0.2f, 0.3f, 0.5f);
        
        public string GetId()
        {
            if (!string.IsNullOrEmpty(chapterId))
            {
                return chapterId;
            }
            return name;
        }
    }
}
