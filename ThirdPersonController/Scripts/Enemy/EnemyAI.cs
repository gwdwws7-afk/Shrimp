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
        
        [Header("Attack Patterns")]
        public bool useAttackPatterns = false;
        public List<string> availablePatterns = new List<string>();
        
        [Header("Performance")]
        public float aiUpdateInterval = 0.08f;
        public float aiUpdateJitter = 0.02f;
        public float nearUpdateInterval = 0.05f;
        public float farUpdateInterval = 0.18f;
        public float nearUpdateDistance = 8f;
        public float farUpdateDistance = 18f;
        public float farAnimationUpdateInterval = 0.2f;

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
        private State currentState = State.Patrol;
        
        private bool isDodging = false;
        private bool isBlocking = false;
        private bool isCharging = false;
        private bool isFleeing = false;
        private Vector3 chargeTarget;
        private float chargeTimer = 0f;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<EnemyHealth>();
            agent.stoppingDistance = stoppingDistance;

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
        }

        private void OnEnable()
        {
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
            ReleaseAttackToken();
            if (crowdCoordinator != null)
            {
                crowdCoordinator.Unregister(this);
            }
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

            UpdateStun();
            if (isStunned)
            {
                UpdateAnimations();
                return;
            }

            if (isSuppressed) return;

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

            HandleCooldowns();
            if (isAttacking)
            {
                Attack();
                UpdateAnimations();
                return;
            }

            if (Time.time < nextDecisionTime)
            {
                UpdateAnimations();
                return;
            }

            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
            float interval = GetUpdateInterval(distanceToTarget);
            float jitter = aiUpdateJitter > 0f ? Random.Range(-aiUpdateJitter, aiUpdateJitter) : 0f;
            nextDecisionTime = Time.time + Mathf.Max(0.02f, interval + jitter);

            DetectTarget();
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
                agent.isStopped = false;
            }
            else
            {
                agent.isStopped = true;
            }
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

        private void HandleCooldowns()
        {
            if (attackCooldownTimer > 0)
                attackCooldownTimer -= Time.deltaTime;
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

            if (!isChasing || distanceToTarget > attackRange)
            {
                ReleaseAttackToken();
            }

            if (isChasing)
            {
                if (distanceToTarget <= attackRange)
                {
                    if (TryAcquireAttackToken())
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
            }

            UpdateAnimations();
        }

        private void Patrol()
        {
            if (patrolPoints.Length == 0) return;

            agent.isStopped = false;

            agent.speed = patrolSpeed;

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

            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        }

        private void Chase()
        {
            agent.isStopped = false;
            agent.speed = chaseSpeed;
            if (currentTarget == null)
            {
                return;
            }

            agent.SetDestination(currentTarget.position);

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

        private void Attack()
        {
            if (!hasAttackToken)
            {
                currentState = State.Circle;
                return;
            }

            agent.isStopped = true;

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

            if (!isAttacking && attackCooldownTimer <= 0f)
            {
                StartAttackSequence();
                attackCooldownTimer = attackCooldown;
            }

            if (isAttacking)
            {
                UpdateAttackSequence();
            }
        }

        private void Circle()
        {
            agent.isStopped = false;
            agent.speed = chaseSpeed * 0.85f;

            if (currentTarget == null)
            {
                return;
            }

            Vector3 targetPosition = currentTarget.position - transform.forward * ringStandoffDistance;
            if (crowdCoordinator != null)
            {
                targetPosition = crowdCoordinator.GetRingPosition(this);
            }

            agent.SetDestination(targetPosition);
        }

        private void StartAttackSequence()
        {
            isAttacking = true;
            attackHitApplied = false;
            attackPhaseTimer = attackWindup + attackActiveTime + attackRecovery;

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

            float activeStart = attackRecovery + attackActiveTime;
            float activeEnd = attackRecovery;

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

            if (distanceToTarget <= attackHitRadius && angleToTarget <= attackHitAngle * 0.5f)
            {
                if (currentTarget.TryGetComponent<PlayerHealth>(out PlayerHealth playerHealth))
                {
                    playerHealth.TakeDamage(attackDamage, transform.position, attackKnockback);
                    return;
                }

                if (currentTarget.TryGetComponent<DefenseTarget>(out DefenseTarget defenseTarget))
                {
                    defenseTarget.TakeDamage(attackDamage, transform.position, attackKnockback);
                }
            }
        }

        private void UpdateAnimations()
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;

            if (Time.time < nextAnimationTime)
            {
                return;
            }

            if (player != null)
            {
                Transform target = currentTarget != null ? currentTarget : player;
                float distance = target != null ? Vector3.Distance(transform.position, target.position) : 0f;
                if (distance >= farUpdateDistance)
                {
                    nextAnimationTime = Time.time + Mathf.Max(0.02f, farAnimationUpdateInterval);
                }
                else
                {
                    nextAnimationTime = Time.time;
                }
            }
            else
            {
                nextAnimationTime = Time.time;
            }

            float moveSpeed = agent.velocity.magnitude / chaseSpeed;
            animator.SetFloat("MoveSpeed", moveSpeed);
            animator.SetBool("IsChasing", isChasing);
        }

        private bool TryAcquireAttackToken()
        {
            if (!useCrowdCoordinator || crowdCoordinator == null)
            {
                hasAttackToken = true;
                return true;
            }

            hasAttackToken = crowdCoordinator.RequestAttackToken(this);
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

        public void SetSuppressed(bool suppressed)
        {
            if (isSuppressed == suppressed)
            {
                return;
            }

            isSuppressed = suppressed;

            if (suppressed)
            {
                ReleaseAttackToken();
                agent.isStopped = true;
            }
            else
            {
                if (!isStunned)
                {
                    agent.isStopped = false;
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
            ReleaseAttackToken();
            isAttacking = false;
            attackPhaseTimer = 0f;
            attackHitApplied = false;
            agent.isStopped = true;
        }
        
        public void SetStunned(bool stunned)
        {
            isStunned = stunned;
            if (stunned)
            {
                agent.isStopped = true;
            }
            else
            {
                agent.isStopped = false;
            }
        }

        public void OnSpawned()
        {
            ResetState();
        }

        public void OnDespawned()
        {
            ReleaseAttackToken();
            isSuppressed = false;
        }

        private void ResetState()
        {
            isSuppressed = false;
            isStunned = false;
            stunTimer = 0f;
            hasAttackToken = false;
            isAttacking = false;
            attackPhaseTimer = 0f;
            attackHitApplied = false;
            isChasing = false;
            waitTimer = 0f;
            attackCooldownTimer = 0f;
            currentState = State.Patrol;
            nextDecisionTime = 0f;
            nextAnimationTime = 0f;
        }

        private void OnDrawGizmosSelected()
        {
            // Detection range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            // Attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);

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
