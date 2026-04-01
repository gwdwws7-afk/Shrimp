using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace ThirdPersonController
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(EnemyHealth))]
    public class EnemyAI : MonoBehaviour, IPoolable
    {
        [Header("Detection")]
        public float detectionRange = 15f;
        public float attackRange = 2f;
        public float fieldOfView = 120f;
        public LayerMask playerLayer;
        public LayerMask obstructionLayer;

        [Header("Movement")]
        public float patrolSpeed = 2f;
        public float chaseSpeed = 5f;
        public float rotationSpeed = 5f;
        public float stoppingDistance = 1.5f;

        [Header("Navigation Safety")]
        public float navMeshAttachRadius = 4f;
        public float navMeshRetryInterval = 1f;

        [Header("Attack")]
        public float attackCooldown = 1.5f;
        public int attackDamage = 10;
        public float attackKnockback = 3f;
        public float attackWindup = 0.35f;
        public float attackActiveTime = 0.1f;
        public float attackRecovery = 0.45f;
        public float attackHitRadius = 1.1f;
        public float attackHitAngle = 120f;
        public Transform attackOrigin;

        [Header("Patrol")]
        public Transform[] patrolPoints;
        public float waitTimeAtPoint = 3f;
        public bool randomPatrol = false;

        [Header("Animation")]
        public Animator animator;
        public string moveSpeedParam = "MoveSpeed";
        public string attackTrigger = "Attack";
        public string isChasingParam = "IsChasing";
        public string hitTrigger = "Hit";
        public string knockdownTrigger = "Knockdown";

        [Header("Crowd")]
        public bool useCrowdCoordinator = true;
        public float ringStandoffDistance = 2.4f;
        
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

        [Header("Advanced Action Tuning")]
        public float dodgeDistance = 2.4f;
        public float dodgeDuration = 0.28f;
        public float dodgeCooldown = 2.2f;
        public float blockDuration = 0.45f;
        public float blockCooldown = 2.8f;
        public float blockDefenseBonus = 6f;
        [Range(0f, 1f)]
        public float chargeChance = 0.2f;
        public float chargeMinDistance = 1.8f;
        public float chargeMaxDistance = 4.2f;
        public float chargeDuration = 0.45f;
        public float chargeCooldown = 3.5f;
        public float fleeDistance = 4.8f;
        public float fleeDuration = 1.1f;
        public float fleeCooldown = 6f;

        [Header("Attack Patterns")]
        public bool useAttackPatterns = false;
        public List<string> availablePatterns = new List<string>();
        public List<EnemyAttackPattern> attackPatterns = new List<EnemyAttackPattern>();
        
        [Header("Performance")]
        public float aiUpdateInterval = 0.08f;
        public float aiUpdateJitter = 0.02f;
        public float nearUpdateInterval = 0.05f;
        public float farUpdateInterval = 0.18f;
        public float nearUpdateDistance = 8f;
        public float farUpdateDistance = 18f;
        public float farAnimationUpdateInterval = 0.2f;

        [Header("Performance LOD (P3)")]
        public bool enableDistanceLod = true;
        public float lodFullDistance = 9f;
        public float lodSimplifiedDistance = 22f;
        public float simplifiedDecisionIntervalMultiplier = 1.4f;
        public float minimalDecisionIntervalMultiplier = 2.4f;
        public float simplifiedAnimationIntervalMultiplier = 1.5f;
        public float minimalAnimationIntervalMultiplier = 2.8f;
        public float minimalTargetRescanInterval = 0.35f;
        public bool disableAdvancedActionsInMinimal = true;

        [Header("Batch Update (P3)")]
        public bool enableBatchDecisionTick = true;
        [Min(1)] public int simplifiedBatchModulo = 2;
        [Min(1)] public int minimalBatchModulo = 4;

        [Header("Crowd Scaling")]
        public bool scaleDecisionIntervalWithCrowd = true;
        public int crowdSlowdownStart = 12;
        public int crowdSlowdownFull = 40;
        public float maxCrowdUpdateMultiplier = 2f;
        public float maxDecisionInterval = 0.25f;

        [Header("Debug (Runtime)")]
        [SerializeField] private string debugCurrentState = "Patrol";
        [SerializeField] private float debugStateElapsed = 0f;
        [SerializeField] private float debugLastDecisionInterval = 0f;
        [SerializeField] private float debugLastDistanceToTarget = 0f;
        [SerializeField] private int debugDecisionCount = 0;
        [SerializeField] private int debugAttackSequenceCount = 0;
        [SerializeField] private int debugHitsAppliedCount = 0;
        [SerializeField] private int debugTokenAcquireSuccessCount = 0;
        [SerializeField] private int debugTokenAcquireFailCount = 0;
        [SerializeField] private string debugUpdateLod = "Full";
        [SerializeField] private bool debugBatchDecisionSkipped = false;

        private NavMeshAgent agent;
        private EnemyHealth health;
        private Transform player;
        private Transform overrideTarget;
        private DefenseTarget overrideDefenseTarget;
        private bool preferOverrideTarget = true;
        private Transform currentTarget;
        
        public Transform Player => player;
        public Transform CurrentTarget => GetCurrentTarget();
        private EnemyCrowdCoordinator crowdCoordinator;
        private bool isSuppressed = false;
        private bool isStunned = false;
        private float stunTimer = 0f;
        private bool hasAttackToken = false;
        private bool isAttacking = false;
        private float attackPhaseTimer = 0f;
        private bool attackHitApplied = false;
        private float nextDecisionTime = 0f;
        private float nextAnimationTime = 0f;

        private int currentPatrolIndex = 0;
        private float waitTimer;
        private float attackCooldownTimer;
        private bool isChasing = false;

        private enum State { Patrol, Chase, Circle, Attack, Dodge, Block, Charge, Flee }
        private enum UpdateLodTier { Full, Simplified, Minimal }
        private State currentState = State.Patrol;
        private UpdateLodTier currentUpdateLod = UpdateLodTier.Full;
        
        private bool isDodging = false;
        private bool isBlocking = false;
        private bool isCharging = false;
        private bool isFleeing = false;
        private Vector3 chargeTarget;
        private float chargeTimer = 0f;
        private bool chargeHitApplied = false;
        private Vector3 dodgeDestination;
        private Vector3 fleeDestination;
        private float dodgeTimer = 0f;
        private float blockTimer = 0f;
        private float fleeTimer = 0f;
        private float dodgeCooldownTimer = 0f;
        private float blockCooldownTimer = 0f;
        private float chargeCooldownTimer = 0f;
        private float fleeCooldownTimer = 0f;
        private float blockDefenseBaseline = 0f;
        private bool blockDefenseApplied = false;
        private float stateElapsed = 0f;
        private State debugLastState = State.Patrol;
        private int decisionBatchOffset = 0;
        private float nextMinimalTargetRescanTime = 0f;
        private bool navMeshMissingLogged = false;
        private float nextNavMeshRetryTime = 0f;

        private EnemyAttackPattern currentPattern;
        private readonly List<EnemyAttackPattern> patternBuffer = new List<EnemyAttackPattern>();
        private static readonly List<EnemyAI> activeInstances = new List<EnemyAI>(512);
        public static IReadOnlyList<EnemyAI> ActiveInstances => activeInstances;

        [System.Serializable]
        public struct EnemyAIDebugSnapshot
        {
            public string state;
            public float stateElapsedSeconds;
            public float lastDecisionIntervalSeconds;
            public float lastDistanceToTarget;
            public int decisionCount;
            public int attackSequenceCount;
            public int hitsAppliedCount;
            public int tokenAcquireSuccessCount;
            public int tokenAcquireFailCount;
            public string updateLod;
            public bool batchDecisionSkipped;
        }

        public EnemyAIDebugSnapshot GetDebugSnapshot()
        {
            return new EnemyAIDebugSnapshot
            {
                state = debugCurrentState,
                stateElapsedSeconds = debugStateElapsed,
                lastDecisionIntervalSeconds = debugLastDecisionInterval,
                lastDistanceToTarget = debugLastDistanceToTarget,
                decisionCount = debugDecisionCount,
                attackSequenceCount = debugAttackSequenceCount,
                hitsAppliedCount = debugHitsAppliedCount,
                tokenAcquireSuccessCount = debugTokenAcquireSuccessCount,
                tokenAcquireFailCount = debugTokenAcquireFailCount,
                updateLod = debugUpdateLod,
                batchDecisionSkipped = debugBatchDecisionSkipped
            };
        }

        public bool PrefersRangedRingLayer()
        {
            if (!useAttackPatterns || attackPatterns == null || attackPatterns.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < attackPatterns.Count; i++)
            {
                EnemyAttackPattern pattern = attackPatterns[i];
                if (pattern == null || !pattern.isRanged)
                {
                    continue;
                }

                if (IsPatternAvailable(pattern))
                {
                    return true;
                }
            }

            return false;
        }

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<EnemyHealth>();
            if (agent != null)
            {
                agent.stoppingDistance = stoppingDistance;
                EnsureAgentReady(allowSample: true);
            }

            if (animator == null)
                animator = GetComponent<Animator>();

            FindPlayer();

            if (useCrowdCoordinator)
            {
                crowdCoordinator = FindObjectOfType<EnemyCrowdCoordinator>();
                if (crowdCoordinator != null)
                {
                    crowdCoordinator.Register(this);
                }
            }

            if (nextDecisionTime <= Time.time)
            {
                nextDecisionTime = Time.time + Random.Range(0f, Mathf.Max(0.02f, aiUpdateInterval));
            }

            if (nextAnimationTime <= Time.time)
            {
                nextAnimationTime = Time.time + Random.Range(0f, Mathf.Max(0.02f, farAnimationUpdateInterval));
            }

            decisionBatchOffset = GetStableBatchOffset();
            currentUpdateLod = UpdateLodTier.Full;
            nextMinimalTargetRescanTime = 0f;
            debugUpdateLod = currentUpdateLod.ToString();
            debugBatchDecisionSkipped = false;
        }

        private void OnEnable()
        {
            RegisterActiveInstance();
            if (useCrowdCoordinator)
            {
                if (crowdCoordinator == null)
                {
                    crowdCoordinator = FindObjectOfType<EnemyCrowdCoordinator>();
                }

                if (crowdCoordinator != null)
                {
                    crowdCoordinator.Register(this);
                }
            }
        }

        private void OnDisable()
        {
            UnregisterActiveInstance();
            CancelTransientActions();
            ReleaseAttackToken();
            if (crowdCoordinator != null)
            {
                crowdCoordinator.Unregister(this);
            }
        }

        private void OnDestroy()
        {
            UnregisterActiveInstance();
        }

        private void FindPlayer()
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        public void SetOverrideTarget(Transform target, bool preferOverride)
        {
            overrideTarget = target;
            preferOverrideTarget = preferOverride;
            overrideDefenseTarget = overrideTarget != null ? overrideTarget.GetComponent<DefenseTarget>() : null;
        }

        public void ClearOverrideTarget()
        {
            overrideTarget = null;
            overrideDefenseTarget = null;
        }

        private Transform GetCurrentTarget()
        {
            if (preferOverrideTarget && overrideTarget != null)
            {
                if (overrideDefenseTarget != null && overrideDefenseTarget.IsDestroyed)
                {
                    ClearOverrideTarget();
                }
                else
                {
                    return overrideTarget;
                }
            }

            return player;
        }

        private void Update()
        {
            if (health.IsDead) return;

            UpdateDebugState();
            UpdateStun();
            if (isStunned)
            {
                UpdateAnimations();
                return;
            }

            if (isSuppressed)
            {
                UpdateAnimations();
                return;
            }

            currentTarget = GetCurrentTarget();
            if (currentTarget == null)
            {
                FindPlayer();
                currentTarget = GetCurrentTarget();
                if (currentTarget == null)
                {
                    return;
                }
                return;
            }

            debugLastDistanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
            currentUpdateLod = ResolveUpdateLodTier(debugLastDistanceToTarget);
            debugUpdateLod = currentUpdateLod.ToString();

            HandleCooldowns();
            if (isAttacking)
            {
                Attack();
                UpdateAnimations();
                return;
            }

            if (!ShouldRunDecisionThisFrame(currentUpdateLod))
            {
                debugBatchDecisionSkipped = true;
                UpdateAnimations();
                return;
            }

            debugBatchDecisionSkipped = false;
            if (Time.time < nextDecisionTime)
            {
                UpdateAnimations();
                return;
            }

            float distanceToTarget = debugLastDistanceToTarget;
            float interval = GetUpdateInterval(distanceToTarget);
            interval = ApplyCrowdScaling(interval);
            interval *= GetLodDecisionIntervalMultiplier(currentUpdateLod);
            if (maxDecisionInterval > 0f)
            {
                interval = Mathf.Min(interval, maxDecisionInterval);
            }
            float jitter = aiUpdateJitter > 0f ? Random.Range(-aiUpdateJitter, aiUpdateJitter) : 0f;
            float decisionInterval = Mathf.Max(0.02f, interval + jitter);
            nextDecisionTime = Time.time + decisionInterval;
            debugLastDecisionInterval = decisionInterval;
            debugDecisionCount++;

            if (ShouldRunTargetDetection(currentUpdateLod))
            {
                DetectTarget();
            }
            UpdateState();
            ExecuteState();
        }

        private void UpdateStun()
        {
            if (!isStunned)
            {
                return;
            }

            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f)
            {
                isStunned = false;
                if (!isSuppressed)
                {
                    SetAgentStoppedSafe(false);
                }
            }
            else
            {
                SetAgentStoppedSafe(true);
            }
        }

        private bool EnsureAgentReady(bool allowSample)
        {
            if (agent == null || !agent.enabled)
            {
                return false;
            }

            if (agent.isOnNavMesh)
            {
                return true;
            }

            if (!allowSample || Time.time < nextNavMeshRetryTime)
            {
                return false;
            }

            nextNavMeshRetryTime = Time.time + Mathf.Max(0.2f, navMeshRetryInterval);
            float sampleRadius = Mathf.Max(0.5f, navMeshAttachRadius);
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
            {
                if (agent.Warp(hit.position))
                {
                    navMeshMissingLogged = false;
                    return agent.isOnNavMesh;
                }
            }

            if (!navMeshMissingLogged)
            {
                navMeshMissingLogged = true;
                Debug.LogWarning($"[EnemyAI] {name} has no valid NavMesh within {sampleRadius:F1}m. Movement will resume automatically once NavMesh becomes available.");
            }

            return false;
        }

        private bool TrySetDestinationSafe(Vector3 destination)
        {
            if (!EnsureAgentReady(allowSample: true))
            {
                return false;
            }

            return agent.SetDestination(destination);
        }

        private void SetAgentSpeedSafe(float speed)
        {
            if (!EnsureAgentReady(allowSample: false))
            {
                return;
            }

            agent.speed = speed;
        }

        private void SetAgentStoppedSafe(bool stopped)
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            {
                return;
            }

            agent.isStopped = stopped;
        }

        private UpdateLodTier ResolveUpdateLodTier(float distanceToTarget)
        {
            if (!enableDistanceLod)
            {
                return UpdateLodTier.Full;
            }

            float fullDistance = Mathf.Max(0.5f, lodFullDistance);
            float simplifiedDistance = Mathf.Max(fullDistance + 0.1f, lodSimplifiedDistance);

            if (distanceToTarget <= fullDistance)
            {
                return UpdateLodTier.Full;
            }

            if (distanceToTarget <= simplifiedDistance)
            {
                return UpdateLodTier.Simplified;
            }

            return UpdateLodTier.Minimal;
        }

        private bool ShouldRunDecisionThisFrame(UpdateLodTier lodTier)
        {
            if (!enableBatchDecisionTick)
            {
                return true;
            }

            int modulo = 1;
            if (lodTier == UpdateLodTier.Simplified)
            {
                modulo = Mathf.Max(1, simplifiedBatchModulo);
            }
            else if (lodTier == UpdateLodTier.Minimal)
            {
                modulo = Mathf.Max(1, minimalBatchModulo);
            }

            if (modulo <= 1)
            {
                return true;
            }

            return (Time.frameCount + decisionBatchOffset) % modulo == 0;
        }

        private bool ShouldRunTargetDetection(UpdateLodTier lodTier)
        {
            if (lodTier != UpdateLodTier.Minimal)
            {
                return true;
            }

            if (isChasing)
            {
                return true;
            }

            if (Time.time < nextMinimalTargetRescanTime)
            {
                return false;
            }

            nextMinimalTargetRescanTime = Time.time + Mathf.Max(0.05f, minimalTargetRescanInterval);
            return true;
        }

        private float GetLodDecisionIntervalMultiplier(UpdateLodTier lodTier)
        {
            switch (lodTier)
            {
                case UpdateLodTier.Simplified:
                    return Mathf.Max(1f, simplifiedDecisionIntervalMultiplier);
                case UpdateLodTier.Minimal:
                    return Mathf.Max(1f, minimalDecisionIntervalMultiplier);
                default:
                    return 1f;
            }
        }

        private float GetLodAnimationIntervalMultiplier(UpdateLodTier lodTier)
        {
            switch (lodTier)
            {
                case UpdateLodTier.Simplified:
                    return Mathf.Max(1f, simplifiedAnimationIntervalMultiplier);
                case UpdateLodTier.Minimal:
                    return Mathf.Max(1f, minimalAnimationIntervalMultiplier);
                default:
                    return 1f;
            }
        }

        private bool AllowAdvancedActionsInCurrentLod()
        {
            if (!disableAdvancedActionsInMinimal)
            {
                return true;
            }

            return currentUpdateLod != UpdateLodTier.Minimal;
        }

        private int GetStableBatchOffset()
        {
            return GetInstanceID() & int.MaxValue;
        }

        private float GetUpdateInterval(float distanceToPlayer)
        {
            if (farUpdateDistance <= nearUpdateDistance)
            {
                return aiUpdateInterval;
            }

            if (distanceToPlayer <= nearUpdateDistance)
            {
                return nearUpdateInterval;
            }

            if (distanceToPlayer >= farUpdateDistance)
            {
                return farUpdateInterval;
            }

            float t = Mathf.InverseLerp(nearUpdateDistance, farUpdateDistance, distanceToPlayer);
            return Mathf.Lerp(nearUpdateInterval, farUpdateInterval, t);
        }

        private float ApplyCrowdScaling(float interval)
        {
            if (!scaleDecisionIntervalWithCrowd)
            {
                return interval;
            }

            int crowdCount = crowdCoordinator != null
                ? crowdCoordinator.NearbyEnemyCount
                : CountEstimatedCrowd();
            if (crowdCount <= crowdSlowdownStart)
            {
                return interval;
            }

            float t = Mathf.InverseLerp(crowdSlowdownStart, crowdSlowdownFull, crowdCount);
            float multiplier = Mathf.Lerp(1f, maxCrowdUpdateMultiplier, t);
            return interval * multiplier;
        }

        private int CountEstimatedCrowd()
        {
            int count = activeInstances.Count - 1;
            return count > 0 ? count : 0;
        }

        private void RegisterActiveInstance()
        {
            if (!activeInstances.Contains(this))
            {
                activeInstances.Add(this);
            }
        }

        private void UnregisterActiveInstance()
        {
            activeInstances.Remove(this);
        }

        private void HandleCooldowns()
        {
            if (attackCooldownTimer > 0f)
            {
                attackCooldownTimer -= Time.deltaTime;
            }

            if (dodgeCooldownTimer > 0f)
            {
                dodgeCooldownTimer -= Time.deltaTime;
            }

            if (blockCooldownTimer > 0f)
            {
                blockCooldownTimer -= Time.deltaTime;
            }

            if (chargeCooldownTimer > 0f)
            {
                chargeCooldownTimer -= Time.deltaTime;
            }

            if (fleeCooldownTimer > 0f)
            {
                fleeCooldownTimer -= Time.deltaTime;
            }
        }

        private void UpdateDebugState()
        {
            if (currentState != debugLastState)
            {
                debugLastState = currentState;
                stateElapsed = 0f;
            }
            else
            {
                stateElapsed += Time.deltaTime;
            }

            debugCurrentState = currentState.ToString();
            debugStateElapsed = stateElapsed;
        }

        private void DetectTarget()
        {
            if (currentTarget == null)
            {
                isChasing = false;
                return;
            }

            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);

            if (distanceToTarget > detectionRange)
            {
                isChasing = false;
                return;
            }

            Vector3 directionToTarget = (currentTarget.position - transform.position).normalized;
            float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);

            if (angleToTarget <= fieldOfView * 0.5f)
            {
                // Check if player is visible (not behind wall)
                if (!Physics.Raycast(transform.position + Vector3.up, directionToTarget,
                    distanceToTarget, obstructionLayer))
                {
                    isChasing = true;
                    return;
                }
            }

            isChasing = false;
        }

        private void UpdateState()
        {
            if (currentTarget == null)
            {
                currentState = State.Patrol;
                return;
            }

            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
            debugLastDistanceToTarget = distanceToTarget;

            if (isDodging)
            {
                currentState = State.Dodge;
                return;
            }

            if (isBlocking)
            {
                currentState = State.Block;
                return;
            }

            if (isCharging)
            {
                currentState = State.Charge;
                return;
            }

            if (isFleeing)
            {
                currentState = State.Flee;
                return;
            }

            if (isChasing && CanEnterFlee())
            {
                BeginFlee();
                currentState = State.Flee;
                return;
            }

            float desiredAttackRange = GetDecisionAttackRange();
            if (!isChasing || distanceToTarget > desiredAttackRange)
            {
                ReleaseAttackToken();
            }

            bool readyToAttack = attackCooldownTimer <= 0f;
            if (!readyToAttack)
            {
                ReleaseAttackToken();
            }

            if (isChasing)
            {
                bool allowAdvancedActions = AllowAdvancedActionsInCurrentLod();
                if (allowAdvancedActions)
                {
                    if (TryEnterCharge(distanceToTarget, readyToAttack))
                    {
                        currentState = State.Charge;
                        return;
                    }

                    if (TryEnterDodge(distanceToTarget))
                    {
                        currentState = State.Dodge;
                        return;
                    }

                    if (TryEnterBlock(distanceToTarget))
                    {
                        currentState = State.Block;
                        return;
                    }
                }

                if (distanceToTarget <= desiredAttackRange)
                {
                    if (readyToAttack && TryAcquireAttackToken())
                    {
                        currentState = State.Attack;
                    }
                    else
                    {
                        currentState = State.Circle;
                    }
                }
                else
                {
                    currentState = State.Chase;
                }
            }
            else
            {
                currentState = State.Patrol;
            }
        }

        private void ExecuteState()
        {
            switch (currentState)
            {
                case State.Patrol:
                    Patrol();
                    break;
                case State.Chase:
                    Chase();
                    break;
                case State.Circle:
                    Circle();
                    break;
                case State.Attack:
                    Attack();
                    break;
                case State.Dodge:
                    Dodge();
                    break;
                case State.Block:
                    Block();
                    break;
                case State.Charge:
                    Charge();
                    break;
                case State.Flee:
                    Flee();
                    break;
            }

            UpdateAnimations();
        }

        private void Patrol()
        {
            if (patrolPoints.Length == 0) return;
            if (!EnsureAgentReady(allowSample: true))
            {
                return;
            }

            SetAgentStoppedSafe(false);

            SetAgentSpeedSafe(patrolSpeed);

            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                waitTimer += Time.deltaTime;

                if (waitTimer >= waitTimeAtPoint)
                {
                    waitTimer = 0f;
                    MoveToNextPatrolPoint();
                }
            }
        }

        private void MoveToNextPatrolPoint()
        {
            if (randomPatrol)
            {
                currentPatrolIndex = Random.Range(0, patrolPoints.Length);
            }
            else
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            }

            TrySetDestinationSafe(patrolPoints[currentPatrolIndex].position);
        }

        private void Chase()
        {
            SetAgentStoppedSafe(false);
            SetAgentSpeedSafe(chaseSpeed);
            if (currentTarget == null)
            {
                return;
            }

            TrySetDestinationSafe(currentTarget.position);

            // Rotate towards player
            Vector3 directionToTarget = (currentTarget.position - transform.position).normalized;
            directionToTarget.y = 0;
            if (directionToTarget.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 
                    rotationSpeed * Time.deltaTime);
            }
        }

        private bool TryEnterCharge(float distanceToTarget, bool readyToAttack)
        {
            if (!readyToAttack || !canCharge || chargeCooldownTimer > 0f || currentTarget == null)
            {
                return false;
            }

            float minDistance = Mathf.Max(0.1f, chargeMinDistance);
            float maxDistance = Mathf.Max(minDistance, chargeMaxDistance);
            if (distanceToTarget < minDistance || distanceToTarget > maxDistance)
            {
                return false;
            }

            if (Random.value > Mathf.Clamp01(chargeChance))
            {
                return false;
            }

            if (!TryAcquireAttackToken())
            {
                return false;
            }

            BeginCharge();
            return true;
        }

        private bool TryEnterDodge(float distanceToTarget)
        {
            if (!canDodge || dodgeCooldownTimer > 0f || currentTarget == null)
            {
                return false;
            }

            float triggerDistance = Mathf.Max(stoppingDistance + 0.5f, attackRange * 1.35f);
            if (distanceToTarget > triggerDistance)
            {
                return false;
            }

            if (Random.value > Mathf.Clamp01(dodgeChance))
            {
                return false;
            }

            BeginDodge();
            return true;
        }

        private bool TryEnterBlock(float distanceToTarget)
        {
            if (!canBlock || blockCooldownTimer > 0f || currentTarget == null || isAttacking)
            {
                return false;
            }

            float triggerDistance = Mathf.Max(stoppingDistance + 0.4f, attackRange * 1.15f);
            if (distanceToTarget > triggerDistance)
            {
                return false;
            }

            if (Random.value > Mathf.Clamp01(blockChance))
            {
                return false;
            }

            BeginBlock();
            return true;
        }

        private bool CanEnterFlee()
        {
            if (!canFlee || fleeCooldownTimer > 0f || health == null || health.MaxHealth <= 0)
            {
                return false;
            }

            float healthRatio = (float)health.CurrentHealth / health.MaxHealth;
            return healthRatio <= Mathf.Clamp01(fleeHealthThreshold);
        }

        private void BeginDodge()
        {
            isDodging = true;
            dodgeTimer = Mathf.Max(0.05f, dodgeDuration);
            dodgeCooldownTimer = Mathf.Max(0f, dodgeCooldown);
            ReleaseAttackToken();

            Vector3 toTarget = currentTarget != null
                ? currentTarget.position - transform.position
                : transform.forward;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.01f)
            {
                toTarget = transform.forward;
            }

            Vector3 lateral = Vector3.Cross(Vector3.up, toTarget.normalized);
            if (Random.value < 0.5f)
            {
                lateral = -lateral;
            }

            if (lateral.sqrMagnitude < 0.01f)
            {
                lateral = transform.right;
            }

            dodgeDestination = transform.position + lateral.normalized * Mathf.Max(1f, dodgeDistance);
        }

        private void Dodge()
        {
            if (!isDodging)
            {
                currentState = isChasing ? State.Circle : State.Patrol;
                return;
            }

            SetAgentStoppedSafe(false);
            SetAgentSpeedSafe(Mathf.Max(chaseSpeed, chaseSpeed * 1.35f));
            TrySetDestinationSafe(dodgeDestination);
            dodgeTimer -= Time.deltaTime;

            float remaining = Vector3.Distance(transform.position, dodgeDestination);
            if (dodgeTimer <= 0f || remaining <= 0.35f)
            {
                isDodging = false;
                currentState = isChasing ? State.Circle : State.Patrol;
            }
        }

        private void BeginBlock()
        {
            isBlocking = true;
            blockTimer = Mathf.Max(0.05f, blockDuration);
            blockCooldownTimer = Mathf.Max(0f, blockCooldown);
            ReleaseAttackToken();
            ApplyBlockDefense();
        }

        private void Block()
        {
            if (!isBlocking)
            {
                currentState = isChasing ? State.Circle : State.Patrol;
                return;
            }

            SetAgentStoppedSafe(true);
            FaceCurrentTarget();
            blockTimer -= Time.deltaTime;
            if (blockTimer <= 0f)
            {
                EndBlock();
                currentState = isChasing ? State.Circle : State.Patrol;
            }
        }

        private void EndBlock()
        {
            isBlocking = false;
            blockTimer = 0f;
            RestoreBlockDefense();
        }

        private void BeginCharge()
        {
            isCharging = true;
            chargeTimer = Mathf.Max(0.1f, Mathf.Max(0f, chargeWindup) + Mathf.Max(0.1f, chargeDuration));
            chargeCooldownTimer = Mathf.Max(0f, chargeCooldown);
            chargeHitApplied = false;
            chargeTarget = currentTarget != null
                ? currentTarget.position
                : transform.position + transform.forward * Mathf.Max(chargeMinDistance, 2f);
            attackCooldownTimer = Mathf.Max(attackCooldownTimer, GetAttackCooldown());

            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetTrigger(attackTrigger);
            }
        }

        private void Charge()
        {
            if (!isCharging)
            {
                currentState = isChasing ? State.Circle : State.Patrol;
                return;
            }

            float dashDuration = Mathf.Max(0.05f, chargeDuration);
            chargeTimer -= Time.deltaTime;

            if (chargeTimer > dashDuration)
            {
                SetAgentStoppedSafe(true);
                FaceCurrentTarget();
            }
            else
            {
                SetAgentStoppedSafe(false);
                SetAgentSpeedSafe(Mathf.Max(chaseSpeed, chargeSpeed));
                if (currentTarget != null)
                {
                    chargeTarget = currentTarget.position;
                }
                TrySetDestinationSafe(chargeTarget);

                if (!chargeHitApplied && currentTarget != null)
                {
                    float hitRange = Mathf.Max(GetAttackHitRadius(), attackRange);
                    if (Vector3.Distance(transform.position, currentTarget.position) <= hitRange)
                    {
                        int damage = Mathf.RoundToInt(GetAttackDamage() * 1.15f);
                        float knockback = Mathf.Max(GetAttackKnockback(), attackKnockback);
                        ApplyDirectHit(damage, knockback);
                        debugHitsAppliedCount++;
                        chargeHitApplied = true;
                    }
                }
            }

            if (chargeTimer <= 0f)
            {
                EndCharge();
            }
        }

        private void EndCharge()
        {
            isCharging = false;
            chargeTimer = 0f;
            chargeHitApplied = false;
            ReleaseAttackToken();
            currentState = isChasing ? State.Circle : State.Patrol;
        }

        private void BeginFlee()
        {
            isFleeing = true;
            fleeTimer = Mathf.Max(0.1f, fleeDuration);
            fleeCooldownTimer = Mathf.Max(0f, fleeCooldown);
            ReleaseAttackToken();
            UpdateFleeDestination();
        }

        private void Flee()
        {
            if (!isFleeing)
            {
                currentState = isChasing ? State.Circle : State.Patrol;
                return;
            }

            SetAgentStoppedSafe(false);
            SetAgentSpeedSafe(Mathf.Max(patrolSpeed, chaseSpeed * 0.9f));
            UpdateFleeDestination();
            TrySetDestinationSafe(fleeDestination);
            fleeTimer -= Time.deltaTime;

            if (fleeTimer <= 0f)
            {
                isFleeing = false;
                currentState = isChasing ? State.Circle : State.Patrol;
            }
        }

        private void UpdateFleeDestination()
        {
            Vector3 away = currentTarget != null
                ? transform.position - currentTarget.position
                : -transform.forward;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f)
            {
                away = -transform.forward;
            }

            fleeDestination = transform.position + away.normalized * Mathf.Max(1f, fleeDistance);
        }

        private void FaceCurrentTarget()
        {
            if (currentTarget == null)
            {
                return;
            }

            Vector3 directionToTarget = (currentTarget.position - transform.position).normalized;
            directionToTarget.y = 0f;
            if (directionToTarget.sqrMagnitude <= 0.01f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        private void ApplyBlockDefense()
        {
            if (blockDefenseApplied || health == null)
            {
                return;
            }

            blockDefenseBaseline = health.defense;
            health.defense = blockDefenseBaseline + Mathf.Max(0f, blockDefenseBonus);
            blockDefenseApplied = true;
        }

        private void RestoreBlockDefense()
        {
            if (!blockDefenseApplied || health == null)
            {
                blockDefenseApplied = false;
                return;
            }

            health.defense = blockDefenseBaseline;
            blockDefenseApplied = false;
        }

        private void Attack()
        {
            if (!hasAttackToken)
            {
                currentState = State.Circle;
                return;
            }

            SetAgentStoppedSafe(true);

            // Rotate to face player
            if (currentTarget != null)
            {
                Vector3 directionToTarget = (currentTarget.position - transform.position).normalized;
                directionToTarget.y = 0;
                if (directionToTarget.magnitude > 0.1f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 
                        rotationSpeed * Time.deltaTime);
                }
            }

            if (!isAttacking && attackCooldownTimer > 0f)
            {
                ReleaseAttackToken();
                currentState = State.Circle;
                return;
            }

            if (!isAttacking && attackCooldownTimer <= 0f)
            {
                StartAttackSequence();
                attackCooldownTimer = GetAttackCooldown();
            }

            if (isAttacking)
            {
                UpdateAttackSequence();
            }
        }

        private void Circle()
        {
            SetAgentStoppedSafe(false);
            SetAgentSpeedSafe(chaseSpeed * 0.85f);

            if (currentTarget == null)
            {
                return;
            }

            Vector3 targetPosition = currentTarget.position - transform.forward * ringStandoffDistance;
            if (crowdCoordinator != null)
            {
                targetPosition = crowdCoordinator.GetRingPosition(this);
            }

            TrySetDestinationSafe(targetPosition);
        }

        private void StartAttackSequence()
        {
            currentPattern = SelectPattern();
            isAttacking = true;
            attackHitApplied = false;
            attackPhaseTimer = GetAttackWindup() + attackActiveTime + GetAttackRecovery();
            debugAttackSequenceCount++;

            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetTrigger(attackTrigger);
            }
        }

        private void UpdateAttackSequence()
        {
            if (attackPhaseTimer <= 0f)
            {
                EndAttackSequence();
                return;
            }

            float previous = attackPhaseTimer;
            attackPhaseTimer -= Time.deltaTime;

            float recovery = GetAttackRecovery();
            float activeStart = recovery + attackActiveTime;
            float activeEnd = recovery;

            bool enteredActive = previous > activeStart && attackPhaseTimer <= activeStart;
            bool inActive = attackPhaseTimer <= activeStart && attackPhaseTimer >= activeEnd;

            if ((enteredActive || inActive) && !attackHitApplied)
            {
                PerformAttackHit();
                attackHitApplied = true;
            }

            if (attackPhaseTimer <= 0f)
            {
                EndAttackSequence();
            }
        }

        private void EndAttackSequence()
        {
            isAttacking = false;
            attackPhaseTimer = 0f;
            ReleaseAttackToken();
        }

        private void PerformAttackHit()
        {
            if (currentTarget == null)
            {
                return;
            }

            Transform origin = attackOrigin != null ? attackOrigin : transform;
            Vector3 directionToTarget = (currentTarget.position - origin.position).normalized;
            directionToTarget.y = 0;

            float distanceToTarget = Vector3.Distance(origin.position, currentTarget.position);
            float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);

            float hitRadius = GetAttackHitRadius();
            float hitAngle = GetAttackHitAngle();
            if (distanceToTarget <= hitRadius && angleToTarget <= hitAngle * 0.5f)
            {
                if (currentPattern != null && currentPattern.isRanged)
                {
                    debugHitsAppliedCount++;
                    FireProjectile(directionToTarget);
                    return;
                }

                if (currentPattern != null && currentPattern.isSuicide)
                {
                    debugHitsAppliedCount++;
                    StartSuicideAttack();
                    return;
                }

                int damage = GetAttackDamage();
                float knockback = GetAttackKnockback();

                if (currentTarget.TryGetComponent<PlayerHealth>(out PlayerHealth playerHealth))
                {
                    playerHealth.TakeDamage(damage, transform.position, knockback);
                    ApplyStatusToPlayer(playerHealth, currentPattern);
                    debugHitsAppliedCount++;
                    return;
                }

                if (currentTarget.TryGetComponent<DefenseTarget>(out DefenseTarget defenseTarget))
                {
                    defenseTarget.TakeDamage(damage, transform.position, knockback);
                    debugHitsAppliedCount++;
                }
            }
        }

        private void FireProjectile(Vector3 directionToTarget)
        {
            if (currentPattern == null)
            {
                return;
            }

            if (currentPattern.projectilePrefab == null)
            {
                ApplyDirectHit(GetAttackDamage(), GetAttackKnockback());
                return;
            }

            Transform origin = attackOrigin != null ? attackOrigin : transform;
            Vector3 baseDirection = GetAimDirection(origin.position, directionToTarget);
            int shots = Mathf.Max(1, currentPattern.projectilesPerShot);
            float spread = Mathf.Max(0f, currentPattern.spreadAngle);
            float startAngle = shots > 1 ? -spread * 0.5f : 0f;
            float step = shots > 1 ? spread / (shots - 1) : 0f;

            for (int i = 0; i < shots; i++)
            {
                float angle = currentPattern.useRandomSpread
                    ? Random.Range(-spread * 0.5f, spread * 0.5f)
                    : startAngle + step * i;
                angle += Random.Range(-currentPattern.spreadJitter, currentPattern.spreadJitter);
                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * baseDirection;
                SpawnProjectile(origin, direction);
            }
        }

        private void SpawnProjectile(Transform origin, Vector3 direction)
        {
            GameObject projectileObj = ObjectPoolManager.Spawn(
                currentPattern.projectilePrefab,
                origin.position,
                Quaternion.LookRotation(direction));
            if (projectileObj == null)
            {
                return;
            }

            EnemyProjectile projectile = projectileObj.GetComponent<EnemyProjectile>();
            if (projectile != null)
            {
                projectile.damage = GetAttackDamage();
                projectile.knockback = GetAttackKnockback();
                projectile.speed = currentPattern.projectileSpeed;
                projectile.lifetime = currentPattern.projectileLifetime;
                projectile.applySlow = currentPattern.applySlow;
                projectile.slowMultiplier = currentPattern.slowMultiplier;
                projectile.slowDuration = currentPattern.slowDuration;
                projectile.applyDamageReduction = currentPattern.applyDamageReduction;
                projectile.damageReduction = currentPattern.damageReduction;
                projectile.damageReductionDuration = currentPattern.damageReductionDuration;
                projectile.Launch(direction, transform);
                return;
            }

            Rigidbody body = projectileObj.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.velocity = direction.normalized * currentPattern.projectileSpeed;
            }
        }

        private Vector3 GetAimDirection(Vector3 origin, Vector3 fallbackDirection)
        {
            if (currentTarget == null)
            {
                return fallbackDirection.sqrMagnitude > 0.01f ? fallbackDirection : transform.forward;
            }

            Vector3 targetPosition = currentTarget.position;
            Vector3 direction = (targetPosition - origin).normalized;

            if (!currentPattern.usePredictiveAim || currentPattern.projectileSpeed <= 0f)
            {
                return direction.sqrMagnitude > 0.01f ? direction : fallbackDirection;
            }

            Vector3 velocity = GetTargetVelocity();
            float distance = Vector3.Distance(origin, targetPosition);
            float time = Mathf.Min(currentPattern.maxPredictionTime, distance / currentPattern.projectileSpeed);
            Vector3 predicted = targetPosition + velocity * time * currentPattern.predictionFactor;
            Vector3 predictedDir = (predicted - origin).normalized;
            return predictedDir.sqrMagnitude > 0.01f ? predictedDir : direction;
        }

        private Vector3 GetTargetVelocity()
        {
            if (currentTarget == null)
            {
                return Vector3.zero;
            }

            if (currentTarget.TryGetComponent<Rigidbody>(out Rigidbody body))
            {
                return body.velocity;
            }

            if (currentTarget.TryGetComponent<PlayerMovement>(out PlayerMovement movement))
            {
                return movement.MoveDirection * movement.CurrentSpeed;
            }

            return Vector3.zero;
        }

        private void ApplyDirectHit(int damage, float knockback)
        {
            if (currentTarget == null)
            {
                return;
            }

            if (currentTarget.TryGetComponent<PlayerHealth>(out PlayerHealth playerHealth))
            {
                playerHealth.TakeDamage(damage, transform.position, knockback);
                ApplyStatusToPlayer(playerHealth, currentPattern);
                return;
            }

            if (currentTarget.TryGetComponent<DefenseTarget>(out DefenseTarget defenseTarget))
            {
                defenseTarget.TakeDamage(damage, transform.position, knockback);
            }
        }

        private Coroutine suicideRoutine;

        private void StartSuicideAttack()
        {
            if (suicideRoutine != null)
            {
                return;
            }

            float delay = currentPattern != null ? Mathf.Max(0f, currentPattern.selfDestructDelay) : 0f;
            suicideRoutine = StartCoroutine(SuicideRoutine(delay));
        }

        private System.Collections.IEnumerator SuicideRoutine(float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            Explode();
            suicideRoutine = null;
        }

        private void Explode()
        {
            if (currentPattern == null)
            {
                return;
            }

            float radius = Mathf.Max(0.5f, currentPattern.explosionRadius);
            Collider[] hits = Physics.OverlapSphere(transform.position, radius);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider hit = hits[i];
                if (hit == null || hit.transform == transform)
                {
                    continue;
                }

                if (hit.TryGetComponent<PlayerHealth>(out PlayerHealth playerHealth))
                {
                    playerHealth.TakeDamage(currentPattern.explosionDamage, transform.position, currentPattern.explosionKnockback);
                    ApplyStatusToPlayer(playerHealth, currentPattern);
                }
                else if (hit.TryGetComponent<DefenseTarget>(out DefenseTarget defenseTarget))
                {
                    defenseTarget.TakeDamage(currentPattern.explosionDamage, transform.position, currentPattern.explosionKnockback);
                }
            }

            if (health != null && !health.IsDead)
            {
                health.TakeDamage(health.CurrentHealth + 999, transform.position, 0f);
            }
        }

        private void ApplyStatusToPlayer(PlayerHealth playerHealth, EnemyAttackPattern pattern)
        {
            if (playerHealth == null || pattern == null)
            {
                return;
            }

            if (pattern.applyDamageReduction)
            {
                playerHealth.ApplyDamageReduction(pattern.damageReduction, pattern.damageReductionDuration);
            }

            if (pattern.applySlow && currentTarget != null)
            {
                if (currentTarget.TryGetComponent<PlayerMovement>(out PlayerMovement movement))
                {
                    movement.ApplyMoveSlow(pattern.slowMultiplier, pattern.slowDuration);
                }
                else if (currentTarget.parent != null
                    && currentTarget.parent.TryGetComponent<PlayerMovement>(out PlayerMovement parentMovement))
                {
                    parentMovement.ApplyMoveSlow(pattern.slowMultiplier, pattern.slowDuration);
                }
            }
        }

        private float GetDecisionAttackRange()
        {
            if (!useAttackPatterns || attackPatterns == null || attackPatterns.Count == 0)
            {
                return attackRange;
            }

            float maxRange = attackRange;
            for (int i = 0; i < attackPatterns.Count; i++)
            {
                EnemyAttackPattern pattern = attackPatterns[i];
                if (!IsPatternAvailable(pattern))
                {
                    continue;
                }

                maxRange = Mathf.Max(maxRange, pattern.range);
            }

            return maxRange;
        }

        private EnemyAttackPattern SelectPattern()
        {
            if (!useAttackPatterns || attackPatterns == null || attackPatterns.Count == 0)
            {
                return null;
            }

            patternBuffer.Clear();
            int highestPriority = int.MinValue;
            for (int i = 0; i < attackPatterns.Count; i++)
            {
                EnemyAttackPattern pattern = attackPatterns[i];
                if (!IsPatternAvailable(pattern))
                {
                    continue;
                }

                if (currentTarget != null)
                {
                    float distance = Vector3.Distance(transform.position, currentTarget.position);
                    if (pattern.range > 0f && distance > pattern.range)
                    {
                        continue;
                    }

                    if (pattern.minRange > 0f && distance < pattern.minRange)
                    {
                        continue;
                    }
                }

                if (pattern.priority > highestPriority)
                {
                    highestPriority = pattern.priority;
                    patternBuffer.Clear();
                    patternBuffer.Add(pattern);
                }
                else if (pattern.priority == highestPriority)
                {
                    patternBuffer.Add(pattern);
                }
            }

            if (patternBuffer.Count == 0)
            {
                return null;
            }

            return PickWeighted(patternBuffer);
        }

        private EnemyAttackPattern PickWeighted(List<EnemyAttackPattern> patterns)
        {
            if (patterns == null || patterns.Count == 0)
            {
                return null;
            }

            float total = 0f;
            for (int i = 0; i < patterns.Count; i++)
            {
                total += Mathf.Max(0.01f, patterns[i].weight);
            }

            float roll = Random.value * total;
            for (int i = 0; i < patterns.Count; i++)
            {
                roll -= Mathf.Max(0.01f, patterns[i].weight);
                if (roll <= 0f)
                {
                    return patterns[i];
                }
            }

            return patterns[patterns.Count - 1];
        }

        private bool IsPatternAvailable(EnemyAttackPattern pattern)
        {
            if (pattern == null)
            {
                return false;
            }

            if (availablePatterns == null || availablePatterns.Count == 0)
            {
                return true;
            }

            if (string.IsNullOrEmpty(pattern.patternId))
            {
                return true;
            }

            return availablePatterns.Contains(pattern.patternId);
        }

        private int GetAttackDamage()
        {
            return currentPattern != null ? currentPattern.damage : attackDamage;
        }

        private float GetAttackCooldown()
        {
            return currentPattern != null ? currentPattern.cooldown : attackCooldown;
        }

        private float GetAttackWindup()
        {
            return currentPattern != null ? currentPattern.windup : attackWindup;
        }

        private float GetAttackRecovery()
        {
            return attackRecovery;
        }

        private float GetAttackKnockback()
        {
            return currentPattern != null ? currentPattern.knockback : attackKnockback;
        }

        private float GetAttackHitRadius()
        {
            return currentPattern != null ? Mathf.Max(attackHitRadius, currentPattern.range) : attackHitRadius;
        }

        private float GetAttackHitAngle()
        {
            if (currentPattern != null && currentPattern.isRanged)
            {
                return 360f;
            }

            return attackHitAngle;
        }

        private void UpdateAnimations()
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;

            if (Time.time < nextAnimationTime)
            {
                return;
            }

            float lodMultiplier = GetLodAnimationIntervalMultiplier(currentUpdateLod);
            if (player != null)
            {
                Transform target = currentTarget != null ? currentTarget : player;
                float distance = target != null ? Vector3.Distance(transform.position, target.position) : 0f;
                bool shouldThrottle = distance >= farUpdateDistance || currentUpdateLod != UpdateLodTier.Full;
                if (shouldThrottle)
                {
                    float baseInterval = distance >= farUpdateDistance
                        ? farAnimationUpdateInterval
                        : Mathf.Max(0.02f, nearUpdateInterval);
                    nextAnimationTime = Time.time + Mathf.Max(0.02f, baseInterval * lodMultiplier);
                }
                else
                {
                    nextAnimationTime = Time.time;
                }
            }
            else
            {
                nextAnimationTime = Time.time + Mathf.Max(0.02f, farAnimationUpdateInterval * lodMultiplier);
            }

            float moveSpeed = 0f;
            if (EnsureAgentReady(allowSample: false) && chaseSpeed > 0.01f)
            {
                moveSpeed = agent.velocity.magnitude / chaseSpeed;
            }
            animator.SetFloat("MoveSpeed", moveSpeed);
            animator.SetBool("IsChasing", isChasing);
        }

        private bool TryAcquireAttackToken()
        {
            if (hasAttackToken)
            {
                return true;
            }

            if (!useCrowdCoordinator || crowdCoordinator == null)
            {
                hasAttackToken = true;
                debugTokenAcquireSuccessCount++;
                return true;
            }

            hasAttackToken = crowdCoordinator.RequestAttackToken(this);
            if (hasAttackToken)
            {
                debugTokenAcquireSuccessCount++;
            }
            else
            {
                debugTokenAcquireFailCount++;
            }
            return hasAttackToken;
        }

        private void ReleaseAttackToken()
        {
            if (!hasAttackToken)
            {
                return;
            }

            hasAttackToken = false;
            if (useCrowdCoordinator && crowdCoordinator != null)
            {
                crowdCoordinator.ReleaseAttackToken(this);
            }
        }

        private void CancelTransientActions()
        {
            isAttacking = false;
            attackPhaseTimer = 0f;
            attackHitApplied = false;

            isDodging = false;
            dodgeTimer = 0f;

            isBlocking = false;
            blockTimer = 0f;
            RestoreBlockDefense();

            isCharging = false;
            chargeTimer = 0f;
            chargeHitApplied = false;

            isFleeing = false;
            fleeTimer = 0f;
        }

        public void SetSuppressed(bool suppressed)
        {
            if (isSuppressed == suppressed)
            {
                return;
            }

            isSuppressed = suppressed;

            if (suppressed)
            {
                CancelTransientActions();
                ReleaseAttackToken();
                SetAgentStoppedSafe(true);
            }
            else
            {
                if (!isStunned)
                {
                    SetAgentStoppedSafe(false);
                }
            }
        }

        public void ApplyStun(float duration)
        {
            if (duration <= 0f || health.IsDead)
            {
                return;
            }

            stunTimer = Mathf.Max(stunTimer, duration);
            isStunned = true;
            CancelTransientActions();
            ReleaseAttackToken();
            SetAgentStoppedSafe(true);
        }
        
        public void SetStunned(bool stunned)
        {
            isStunned = stunned;
            if (stunned)
            {
                CancelTransientActions();
                ReleaseAttackToken();
                SetAgentStoppedSafe(true);
            }
            else
            {
                if (!isSuppressed)
                {
                    SetAgentStoppedSafe(false);
                }
            }
        }

        public void OnSpawned()
        {
            ResetState();
        }

        public void OnDespawned()
        {
            CancelTransientActions();
            ReleaseAttackToken();
            isSuppressed = false;
        }

        private void ResetState()
        {
            ReleaseAttackToken();
            isSuppressed = false;
            isStunned = false;
            stunTimer = 0f;
            hasAttackToken = false;
            isAttacking = false;
            attackPhaseTimer = 0f;
            attackHitApplied = false;
            isDodging = false;
            isBlocking = false;
            isCharging = false;
            isFleeing = false;
            dodgeTimer = 0f;
            blockTimer = 0f;
            chargeTimer = 0f;
            fleeTimer = 0f;
            dodgeCooldownTimer = 0f;
            blockCooldownTimer = 0f;
            chargeCooldownTimer = 0f;
            fleeCooldownTimer = 0f;
            chargeHitApplied = false;
            RestoreBlockDefense();
            isChasing = false;
            waitTimer = 0f;
            attackCooldownTimer = 0f;
            currentState = State.Patrol;
            debugLastState = State.Patrol;
            stateElapsed = 0f;
            debugCurrentState = State.Patrol.ToString();
            debugStateElapsed = 0f;
            debugLastDecisionInterval = 0f;
            debugLastDistanceToTarget = 0f;
            debugDecisionCount = 0;
            debugAttackSequenceCount = 0;
            debugHitsAppliedCount = 0;
            debugTokenAcquireSuccessCount = 0;
            debugTokenAcquireFailCount = 0;
            currentUpdateLod = UpdateLodTier.Full;
            debugUpdateLod = currentUpdateLod.ToString();
            debugBatchDecisionSkipped = false;
            decisionBatchOffset = GetStableBatchOffset();
            nextMinimalTargetRescanTime = 0f;
            nextDecisionTime = Time.time + Random.Range(0f, Mathf.Max(0.02f, aiUpdateInterval));
            nextAnimationTime = Time.time + Random.Range(0f, Mathf.Max(0.02f, farAnimationUpdateInterval));
        }

        private void OnDrawGizmosSelected()
        {
            // Detection range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            // Attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, GetDecisionAttackRange());

            // Field of view
            Vector3 leftBoundary = Quaternion.Euler(0, -fieldOfView * 0.5f, 0) * transform.forward;
            Vector3 rightBoundary = Quaternion.Euler(0, fieldOfView * 0.5f, 0) * transform.forward;

            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, leftBoundary * detectionRange);
            Gizmos.DrawRay(transform.position, rightBoundary * detectionRange);

            // Patrol points
            if (patrolPoints != null)
            {
                Gizmos.color = Color.green;
                for (int i = 0; i < patrolPoints.Length; i++)
                {
                    if (patrolPoints[i] != null)
                    {
                        Gizmos.DrawSphere(patrolPoints[i].position, 0.3f);
                        if (i < patrolPoints.Length - 1 && patrolPoints[i + 1] != null)
                        {
                            Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i + 1].position);
                        }
                    }
                }
            }
        }
    }
}
