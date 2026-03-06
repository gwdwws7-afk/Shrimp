using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    [CreateAssetMenu(fileName = "EnemyArchetype_", menuName = "AI/Enemy Archetype")]
    public class EnemyArchetype : ScriptableObject
    {
        [Header("Identity")]
        public string archetypeId = "";
        public string displayName = "Enemy";
        public EnemyType enemyType = EnemyType.Grunt;

        [Header("Health & Rewards")]
        public int maxHealth = 60;
        public float hitStunDuration = 0.2f;
        public float defense = 0f;
        public int expReward = 2;
        public float dropChance = 0.25f;

        [Header("Resistances")]
        [Range(-1f, 1f)]
        public float resistPhysical = 0f;
        [Range(-1f, 1f)]
        public float resistHeat = 0f;
        [Range(-1f, 1f)]
        public float resistElectric = 0f;
        [Range(-1f, 1f)]
        public float resistToxin = 0f;
        [Range(-1f, 1f)]
        public float resistCorrosion = 0f;

        [Header("Detection")]
        public float detectionRange = 15f;
        public float attackRange = 2f;
        public float fieldOfView = 120f;

        [Header("Movement")]
        public float patrolSpeed = 2.2f;
        public float chaseSpeed = 4.2f;
        public float rotationSpeed = 6f;
        public float stoppingDistance = 1.5f;

        [Header("Attack")]
        public float attackCooldown = 1.5f;
        public int attackDamage = 10;
        public float attackKnockback = 3f;
        public float attackWindup = 0.35f;
        public float attackActiveTime = 0.1f;
        public float attackRecovery = 0.45f;
        public float attackHitRadius = 1.1f;
        public float attackHitAngle = 120f;

        [Header("Attack Patterns")]
        public bool useAttackPatterns = false;
        public List<EnemyAttackPattern> attackPatterns = new List<EnemyAttackPattern>();

        [Header("Advanced AI")]
        public bool canDodge = false;
        public float dodgeChance = 0.1f;
        public bool canBlock = false;
        public float blockChance = 0.1f;
        public bool canCharge = false;
        public float chargeSpeed = 10f;
        public float chargeWindup = 0.5f;
        public bool canFlee = false;
        public float fleeHealthThreshold = 0.2f;

        [Header("Crowd")]
        public bool useCrowdCoordinator = true;
        public float ringStandoffDistance = 2.4f;

        [Header("NavMesh Agent")]
        public bool overrideAgentSettings = true;
        public float agentSpeed = 3.5f;
        public float agentAcceleration = 8f;
        public float agentAngularSpeed = 360f;
        public float agentStoppingDistance = 1.5f;
        public float agentRadius = 0.4f;
        public float agentHeight = 1.8f;

        [Header("Hit Reaction")]
        public bool overrideHitReaction = true;
        public float knockbackThreshold = 2f;
        public float knockdownThreshold = 6f;
        public float flinchDuration = 0.2f;
        public float knockbackDuration = 0.25f;
        public float knockbackDistance = 1.2f;
        public float knockdownDuration = 0.35f;
        public float knockdownDistance = 2.5f;
        public float knockdownRecoverTime = 0.6f;
    }
}
