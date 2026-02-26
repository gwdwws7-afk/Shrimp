using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    [CreateAssetMenu(fileName = "EnemyDatabase", menuName = "Enemies/Enemy Database")]
    public class EnemyDatabase : ScriptableObject
    {
        public List<EnemyData> enemies = new List<EnemyData>();

        private Dictionary<string, EnemyData> lookup;
        private Dictionary<EnemyType, List<EnemyData>> typeLookup;

        public EnemyData GetEnemyById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            BuildLookup();
            if (lookup != null && lookup.TryGetValue(id, out EnemyData enemy))
            {
                return enemy;
            }

            return null;
        }

        public List<EnemyData> GetEnemiesByType(EnemyType type)
        {
            BuildTypeLookup();
            
            if (typeLookup != null && typeLookup.TryGetValue(type, out List<EnemyData> list))
            {
                return list;
            }

            return new List<EnemyData>();
        }

        public EnemyData GetRandomEnemy(EnemyType type = EnemyType.Grunt)
        {
            List<EnemyData> enemiesOfType = GetEnemiesByType(type);
            
            if (enemiesOfType.Count == 0)
            {
                return null;
            }

            return enemiesOfType[Random.Range(0, enemiesOfType.Count)];
        }

        public EnemyData GetEliteVersion(EnemyType baseType)
        {
            EnemyData baseEnemy = GetRandomEnemy(baseType);
            if (baseEnemy == null) return null;

            return baseEnemy.GetEliteVersion();
        }

        public EnemyData GetBossVersion(EnemyType baseType)
        {
            EnemyData baseEnemy = GetRandomEnemy(baseType);
            if (baseEnemy == null) return null;

            return baseEnemy.GetBossVersion();
        }

        public EnemyData ApplyDifficulty(EnemyData baseEnemy, LevelDifficulty difficulty)
        {
            float multiplier = GetDifficultyMultiplier(difficulty);
            return baseEnemy.ApplyDifficulty(multiplier);
        }

        public float GetDifficultyMultiplier(LevelDifficulty difficulty)
        {
            switch (difficulty)
            {
                case LevelDifficulty.Easy:
                    return 0.8f;
                case LevelDifficulty.Normal:
                    return 1.0f;
                case LevelDifficulty.Hard:
                    return 1.5f;
                case LevelDifficulty.Nightmare:
                    return 2.0f;
                default:
                    return 1.0f;
            }
        }

        private void BuildLookup()
        {
            if (lookup != null) return;

            lookup = new Dictionary<string, EnemyData>();
            if (enemies == null) return;

            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyData enemy = enemies[i];
                if (enemy == null) continue;

                string id = enemy.GetId();
                if (!lookup.ContainsKey(id))
                {
                    lookup.Add(id, enemy);
                }
            }
        }

        private void BuildTypeLookup()
        {
            if (typeLookup != null) return;

            typeLookup = new Dictionary<EnemyType, List<EnemyData>>();
            
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyData enemy = enemies[i];
                if (enemy == null) continue;

                if (!typeLookup.ContainsKey(enemy.enemyType))
                {
                    typeLookup.Add(enemy.enemyType, new List<EnemyData>());
                }

                typeLookup[enemy.enemyType].Add(enemy);
            }
        }

        public void RefreshDatabase()
        {
            lookup = null;
            typeLookup = null;
            BuildLookup();
            BuildTypeLookup();
        }
    }
}
