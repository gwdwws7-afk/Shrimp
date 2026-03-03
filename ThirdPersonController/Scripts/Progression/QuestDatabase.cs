using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    [CreateAssetMenu(fileName = "QuestDatabase", menuName = "Progression/Quest Database")]
    public class QuestDatabase : ScriptableObject
    {
        public List<QuestData> quests = new List<QuestData>();

        public QuestData GetQuestById(string questId)
        {
            if (string.IsNullOrEmpty(questId) || quests == null)
            {
                return null;
            }

            for (int i = 0; i < quests.Count; i++)
            {
                QuestData quest = quests[i];
                if (quest != null && quest.questId == questId)
                {
                    return quest;
                }
            }

            return null;
        }
    }
}
