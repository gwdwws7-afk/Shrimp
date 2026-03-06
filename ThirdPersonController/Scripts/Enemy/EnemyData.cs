using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    [System.Serializable]
    public class EnemyAttackPattern
    {
        public string patternId = "";
        public string patternName = "Attack";
        public int priority = 0;
        public float weight = 1f;
        public int damage = 10;
        public float range = 2f;
        public float minRange = 0f;
        public float cooldown = 2f;
        public float windup = 0.3f;
        public float knockback = 5f;
        public bool isRanged = false;
        public GameObject projectilePrefab;
        public float projectileSpeed = 12f;
        public float projectileLifetime = 4f;
        public int projectilesPerShot = 1;
        public float spreadAngle = 0f;
        public bool useRandomSpread = false;
        public float spreadJitter = 0f;
        public bool usePredictiveAim = false;
        public float predictionFactor = 1f;
        public float maxPredictionTime = 1f;

        [Header("Status Effects")]
        public bool applySlow = false;
        public float slowMultiplier = 0.6f;
        public float slowDuration = 2f;
        public bool applyDamageReduction = false;
        public float damageReduction = 0.2f;
        public float damageReductionDuration = 2f;

        [Header("Suicide")]
        public bool isSuicide = false;
        public float explosionRadius = 3f;
        public int explosionDamage = 25;
        public float explosionKnockback = 5f;
        public float selfDestructDelay = 0f;
    }

    [System.Serializable]
    public class LootTableEntry
    {
        public string itemId = "";
        public int quantity = 1;
        [Range(0f, 1f)]
        public float dropChance = 0.1f;
    }

    [CreateAssetMenu(fileName = "EnemyData_", menuName = "Enemies/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [Header("Basic Info")]
        public string enemyId = "";
        public string enemyName = "Enemy";
        [TextArea(2, 4)]
        public string description;
        public EnemyType enemyType = EnemyType.Grunt;
        
        [Header("Stats")]
        public int baseHealth = 30;
        public int baseDamage = 5;
        public float moveSpeed = 3f;
        public float attackSpeed = 1f;
        public float attackRange = 2f;
        
        [Header("Combat")]
        public float defense = 0f;
        public float dodgeChance = 0f;
        public float blockChance = 0f;
        public float criticalChance = 0f;
        
        [Header("AI")]
        public float detectionRange = 15f;
        public float fieldOfView = 120f;
        public bool canPatrol = true;
        public bool canChase = true;
        public bool canDodge = false;
        public bool canBlock = false;
        
        [Header("Attack Patterns")]
        public List<EnemyAttackPattern> attackPatterns = new List<EnemyAttackPattern>();
        
        [Header("Loot")]
        public int experienceReward = 10;
        public List<LootTableEntry> lootTable = new List<LootTableEntry>();
        
        [Header("Difficulty Scaling")]
        public float healthPerLevel = 10f;
        public float damagePerLevel = 2f;
        
        [Header("Visuals")]
        public GameObject prefab;
        public GameObject elitePrefab;
        public GameObject bossPrefab;
        
        public string GetId()
        {
            if (!string.IsNullOrEmpty(enemyId))
            {
                return enemyId;
            }
            return name;
        }

        public EnemyData GetEliteVersion()
        {
            if (elitePrefab != null)
            {
                EnemyData elite = CreateInstance<EnemyData>();
                elite.enemyId = enemyId + "_elite";
                elite.enemyName = "Elite " + enemyName;
                elite.enemyType = EnemyType.Elite;
                elite.baseHealth = Mathf.RoundToInt(baseHealth * 2f);
                elite.baseDamage = Mathf.RoundToInt(baseDamage * 1.5f);
                elite.dodgeChance = dodgeChance * 1.5f;
                elite.experienceReward = experienceReward * 3;
                elite.prefab = elitePrefab;
                return elite;
            }
            return this;
        }

        public EnemyData GetBossVersion()
        {
            if (bossPrefab != null)
            {
                EnemyData boss = CreateInstance<EnemyData>();
                boss.enemyId = enemyId + "_boss";
                boss.enemyName = enemyName + " Boss";
                boss.enemyType = EnemyType.Boss;
                boss.baseHealth = Mathf.RoundToInt(baseHealth * 20f);
                boss.baseDamage = Mathf.RoundToInt(baseDamage * 3f);
                boss.defense = defense * 2f;
                boss.experienceReward = experienceReward * 10;
                boss.prefab = bossPrefab;
                return boss;
            }
            return this;
        }

        public EnemyData ApplyDifficulty(float multiplier)
        {
            EnemyData scaled = CreateInstance<EnemyData>();
            scaled.CopyFrom(this);
            scaled.baseHealth = Mathf.RoundToInt(baseHealth * multiplier);
            scaled.baseDamage = Mathf.RoundToInt(baseDamage * multiplier);
            return scaled;
        }

        private void CopyFrom(EnemyData source)
        {
            enemyId = source.enemyId;
            enemyName = source.enemyName;
            description = source.description;
            enemyType = source.enemyType;
            baseHealth = source.baseHealth;
            baseDamage = source.baseDamage;
            moveSpeed = source.moveSpeed;
            attackSpeed = source.attackSpeed;
            attackRange = source.attackRange;
            defense = source.defense;
            dodgeChance = source.dodgeChance;
            blockChance = source.blockChance;
            criticalChance = source.criticalChance;
            detectionRange = source.detectionRange;
            fieldOfView = source.fieldOfView;
            canPatrol = source.canPatrol;
            canChase = source.canChase;
            canDodge = source.canDodge;
            canBlock = source.canBlock;
            attackPatterns = new List<EnemyAttackPattern>(source.attackPatterns);
            experienceReward = source.experienceReward;
            lootTable = new List<LootTableEntry>(source.lootTable);
            healthPerLevel = source.healthPerLevel;
            damagePerLevel = source.damagePerLevel;
            prefab = source.prefab;
            elitePrefab = source.elitePrefab;
            bossPrefab = source.bossPrefab;
        }
    }
}
