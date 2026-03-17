using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace ThirdPersonController
{
    public class EnemyArchetypeConfigurator : MonoBehaviour, IPoolable
    {
        public EnemyArchetype archetype;
        public bool applyOnStart = true;
        public bool applyOnSpawned = true;

        [Header("Overrides")]
        public bool applyHealth = true;
        public bool applyAI = true;
        public bool applyCrowd = true;
        public bool applyHitReaction = true;
        public bool applyNavMesh = true;

        private EnemyAI ai;
        private EnemyHealth health;
        private EnemyHitReaction hitReaction;
        private NavMeshAgent agent;

        private void Awake()
        {
            Cache();
        }

        private void Start()
        {
            if (applyOnStart)
            {
                ApplyArchetype(archetype);
            }
        }

        public void OnSpawned()
        {
            if (applyOnSpawned)
            {
                ApplyArchetype(archetype);
            }
        }

        public void OnDespawned()
        {
        }

        public void ApplyArchetype(EnemyArchetype newArchetype)
        {
            if (newArchetype == null)
            {
                return;
            }

            archetype = newArchetype;
            Apply();
        }

        private void Cache()
        {
            ai = GetComponent<EnemyAI>();
            health = GetComponent<EnemyHealth>();
            hitReaction = GetComponent<EnemyHitReaction>();
            agent = GetComponent<NavMeshAgent>();
        }

        private void Apply()
        {
            if (archetype == null)
            {
                return;
            }

            if (applyHealth && health != null)
            {
                health.maxHealth = archetype.maxHealth;
                health.hitStunDuration = archetype.hitStunDuration;
                health.defense = archetype.defense;
                health.expReward = archetype.expReward;
                health.enemyType = archetype.enemyType;
                health.dropChance = archetype.dropChance;
                health.resistPhysical = archetype.resistPhysical;
                health.resistHeat = archetype.resistHeat;
                health.resistElectric = archetype.resistElectric;
                health.resistToxin = archetype.resistToxin;
                health.resistCorrosion = archetype.resistCorrosion;
            }

            if (applyAI && ai != null)
            {
                ai.detectionRange = archetype.detectionRange;
                ai.attackRange = archetype.attackRange;
                ai.fieldOfView = archetype.fieldOfView;

                ai.patrolSpeed = archetype.patrolSpeed;
                ai.chaseSpeed = archetype.chaseSpeed;
                ai.rotationSpeed = archetype.rotationSpeed;
                ai.stoppingDistance = archetype.stoppingDistance;

                ai.attackCooldown = archetype.attackCooldown;
                ai.attackDamage = archetype.attackDamage;
                ai.attackKnockback = archetype.attackKnockback;
                ai.attackWindup = archetype.attackWindup;
                ai.attackActiveTime = archetype.attackActiveTime;
                ai.attackRecovery = archetype.attackRecovery;
                ai.attackHitRadius = archetype.attackHitRadius;
                ai.attackHitAngle = archetype.attackHitAngle;
                ai.useAttackPatterns = archetype.useAttackPatterns;
                ai.attackPatterns = new List<EnemyAttackPattern>(archetype.attackPatterns);

                ai.canDodge = archetype.canDodge;
                ai.dodgeChance = archetype.dodgeChance;
                ai.canBlock = archetype.canBlock;
                ai.blockChance = archetype.blockChance;
                ai.canCharge = archetype.canCharge;
                ai.chargeSpeed = archetype.chargeSpeed;
                ai.chargeWindup = archetype.chargeWindup;
                ai.canFlee = archetype.canFlee;
                ai.fleeHealthThreshold = archetype.fleeHealthThreshold;
                ai.dodgeDistance = archetype.dodgeDistance;
                ai.dodgeDuration = archetype.dodgeDuration;
                ai.dodgeCooldown = archetype.dodgeCooldown;
                ai.blockDuration = archetype.blockDuration;
                ai.blockCooldown = archetype.blockCooldown;
                ai.blockDefenseBonus = archetype.blockDefenseBonus;
                ai.chargeChance = archetype.chargeChance;
                ai.chargeMinDistance = archetype.chargeMinDistance;
                ai.chargeMaxDistance = archetype.chargeMaxDistance;
                ai.chargeDuration = archetype.chargeDuration;
                ai.chargeCooldown = archetype.chargeCooldown;
                ai.fleeDistance = archetype.fleeDistance;
                ai.fleeDuration = archetype.fleeDuration;
                ai.fleeCooldown = archetype.fleeCooldown;

                if (applyCrowd)
                {
                    ai.useCrowdCoordinator = archetype.useCrowdCoordinator;
                    ai.ringStandoffDistance = archetype.ringStandoffDistance;
                }

                ai.enableDistanceLod = archetype.enableDistanceLod;
                ai.lodFullDistance = archetype.lodFullDistance;
                ai.lodSimplifiedDistance = archetype.lodSimplifiedDistance;
                ai.simplifiedDecisionIntervalMultiplier = archetype.simplifiedDecisionIntervalMultiplier;
                ai.minimalDecisionIntervalMultiplier = archetype.minimalDecisionIntervalMultiplier;
                ai.simplifiedAnimationIntervalMultiplier = archetype.simplifiedAnimationIntervalMultiplier;
                ai.minimalAnimationIntervalMultiplier = archetype.minimalAnimationIntervalMultiplier;
                ai.minimalTargetRescanInterval = archetype.minimalTargetRescanInterval;
                ai.disableAdvancedActionsInMinimal = archetype.disableAdvancedActionsInMinimal;
                ai.enableBatchDecisionTick = archetype.enableBatchDecisionTick;
                ai.simplifiedBatchModulo = Mathf.Max(1, archetype.simplifiedBatchModulo);
                ai.minimalBatchModulo = Mathf.Max(1, archetype.minimalBatchModulo);
            }

            if (applyNavMesh && agent != null && archetype.overrideAgentSettings)
            {
                agent.speed = archetype.agentSpeed;
                agent.acceleration = archetype.agentAcceleration;
                agent.angularSpeed = archetype.agentAngularSpeed;
                agent.stoppingDistance = archetype.agentStoppingDistance;
                agent.radius = archetype.agentRadius;
                agent.height = archetype.agentHeight;
            }

            if (applyHitReaction && hitReaction != null && archetype.overrideHitReaction)
            {
                hitReaction.knockbackThreshold = archetype.knockbackThreshold;
                hitReaction.knockdownThreshold = archetype.knockdownThreshold;
                hitReaction.flinchDuration = archetype.flinchDuration;
                hitReaction.knockbackDuration = archetype.knockbackDuration;
                hitReaction.knockbackDistance = archetype.knockbackDistance;
                hitReaction.knockdownDuration = archetype.knockdownDuration;
                hitReaction.knockdownDistance = archetype.knockdownDistance;
                hitReaction.knockdownRecoverTime = archetype.knockdownRecoverTime;
            }

            if (health != null)
            {
                health.OnSpawned();
            }

            if (ai != null && agent != null)
            {
                agent.stoppingDistance = ai.stoppingDistance;
            }
        }
    }
}
