using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    [CreateAssetMenu(fileName = "EnemyWavePrefabFallbackConfig", menuName = "AI/Enemy Wave Prefab Fallback Config")]
    public sealed class EnemyWavePrefabFallbackConfig : ScriptableObject
    {
        [System.Serializable]
        public sealed class StageFallbackRule
        {
            public string stagePrefix = "Wave";
            public List<string> archetypeIds = new List<string>();
        }

        [Header("Stage Prefix Rules (first match wins)")]
        public List<StageFallbackRule> stageRules = new List<StageFallbackRule>();

        [Header("Default Priority")]
        public List<string> defaultArchetypeIds = new List<string>();

        public IReadOnlyList<string> GetPreferredArchetypeIds(string stageLabel)
        {
            if (!string.IsNullOrEmpty(stageLabel) && stageRules != null)
            {
                for (int i = 0; i < stageRules.Count; i++)
                {
                    StageFallbackRule rule = stageRules[i];
                    if (rule == null
                        || string.IsNullOrEmpty(rule.stagePrefix)
                        || rule.archetypeIds == null
                        || rule.archetypeIds.Count == 0)
                    {
                        continue;
                    }

                    if (stageLabel.StartsWith(rule.stagePrefix, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return rule.archetypeIds;
                    }
                }
            }

            if (defaultArchetypeIds != null && defaultArchetypeIds.Count > 0)
            {
                return defaultArchetypeIds;
            }

            return EmptyIds;
        }

        private static readonly List<string> EmptyIds = new List<string>();
    }
}
