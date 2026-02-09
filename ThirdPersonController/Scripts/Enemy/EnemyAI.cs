using UnityEngine;
using UnityEngine.AI;

namespace ThirdPersonController
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(EnemyHealth))]
    public class EnemyAI : MonoBehaviour
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

        [Header("Patrol")]
        public Transform[] patrolPoints;
        public float waitTimeAtPoint = 3f;
        public bool randomPatrol = false;

        [Header("Animation")]
        public Animator animator;
        public string moveSpeedParam = "MoveSpeed";
        public string attackTrigger = "Attack";
        public string isChasingParam = "IsChasing";

        private NavMeshAgent agent;
        private EnemyHealth health;
        private Transform player;

        private int currentPatrolIndex = 0;
        private float waitTimer;
        private float attackCooldownTimer;
        private bool isChasing = false;

        private enum State { Patrol, Chase, Attack }
        private State currentState = State.Patrol;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<EnemyHealth>();
            agent.stoppingDistance = stoppingDistance;

            if (animator == null)
                animator = GetComponent<Animator>();

            FindPlayer();
        }

        private void FindPlayer()
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        private void Update()
        {
            if (health.IsDead) return;

            if (player == null)
            {
                FindPlayer();
                return;
            }

            HandleCooldowns();
            DetectPlayer();
            UpdateState();
            ExecuteState();
        }

        private void HandleCooldowns()
        {
            if (attackCooldownTimer > 0)
                attackCooldownTimer -= Time.deltaTime;
        }

        private void DetectPlayer()
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            if (distanceToPlayer <= detectionRange)
            {
                Vector3 directionToPlayer = (player.position - transform.position).normalized;
                float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);

                if (angleToPlayer <= fieldOfView * 0.5f)
                {
                    // Check if player is visible (not behind wall)
                    if (!Physics.Raycast(transform.position + Vector3.up, directionToPlayer, 
                        distanceToPlayer, obstructionLayer))
                    {
                        isChasing = true;
                    }
                }
            }
        }

        private void UpdateState()
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            if (isChasing)
            {
                if (distanceToPlayer <= attackRange)
                {
                    currentState = State.Attack;
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
                case State.Attack:
                    Attack();
                    break;
            }

            UpdateAnimations();
        }

        private void Patrol()
        {
            if (patrolPoints.Length == 0) return;

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
            agent.speed = chaseSpeed;
            agent.SetDestination(player.position);

            // Rotate towards player
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            directionToPlayer.y = 0;
            if (directionToPlayer.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 
                    rotationSpeed * Time.deltaTime);
            }
        }

        private void Attack()
        {
            agent.isStopped = true;

            // Rotate to face player
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            directionToPlayer.y = 0;
            if (directionToPlayer.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 
                    rotationSpeed * Time.deltaTime);
            }

            // Perform attack
            if (attackCooldownTimer <= 0)
            {
                PerformAttack();
                attackCooldownTimer = attackCooldown;
            }
        }

        private void PerformAttack()
        {
            // Trigger animation
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetTrigger("Attack");
            }

            // Damage player
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage, transform.position, attackKnockback);
            }
        }

        private void UpdateAnimations()
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;

            float moveSpeed = agent.velocity.magnitude / chaseSpeed;
            animator.SetFloat("MoveSpeed", moveSpeed);
            animator.SetBool("IsChasing", isChasing);
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
