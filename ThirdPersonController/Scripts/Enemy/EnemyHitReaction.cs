using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace ThirdPersonController
{
    public enum EnemyHitReactionType
    {
        Flinch,
        Knockback,
        Knockdown
    }

    public class EnemyHitReaction : MonoBehaviour
    {
        [Header("Thresholds")]
        public float knockbackThreshold = 2f;
        public float knockdownThreshold = 6f;

        [Header("Flinch")]
        public float flinchDuration = 0.2f;

        [Header("Knockback")]
        public float knockbackDuration = 0.25f;
        public float knockbackDistance = 1.2f;

        [Header("Knockdown")]
        public float knockdownDuration = 0.35f;
        public float knockdownDistance = 2.5f;
        public float knockdownRecoverTime = 0.6f;

        [Header("Animation")]
        public string flinchTrigger = "Hit";
        public string knockdownTrigger = "Knockdown";

        private Animator animator;
        private NavMeshAgent agent;
        private EnemyAI ai;
        private Coroutine reactionRoutine;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            agent = GetComponent<NavMeshAgent>();
            ai = GetComponent<EnemyAI>();
        }

        public void ApplyHit(Vector3 damageSource, float knockbackForce)
        {
            EnemyHitReactionType reactionType = GetReactionType(knockbackForce);

            if (reactionRoutine != null)
            {
                StopCoroutine(reactionRoutine);
            }

            reactionRoutine = StartCoroutine(ReactionRoutine(reactionType, damageSource));
        }

        public void CancelReaction()
        {
            if (reactionRoutine != null)
            {
                StopCoroutine(reactionRoutine);
                reactionRoutine = null;
            }

            ResumeAgentControl();
        }

        private IEnumerator ReactionRoutine(EnemyHitReactionType reactionType, Vector3 damageSource)
        {
            SuspendAgentControl();

            TriggerAnimation(reactionType);

            Vector3 knockbackDirection = GetKnockbackDirection(damageSource);

            switch (reactionType)
            {
                case EnemyHitReactionType.Flinch:
                    yield return new WaitForSeconds(flinchDuration);
                    break;
                case EnemyHitReactionType.Knockback:
                    yield return MoveKnockback(knockbackDirection, knockbackDistance, knockbackDuration);
                    break;
                case EnemyHitReactionType.Knockdown:
                    yield return MoveKnockback(knockbackDirection, knockdownDistance, knockdownDuration);
                    yield return new WaitForSeconds(knockdownRecoverTime);
                    break;
            }

            ResumeAgentControl();
            reactionRoutine = null;
        }

        private EnemyHitReactionType GetReactionType(float knockbackForce)
        {
            if (knockbackForce >= knockdownThreshold)
            {
                return EnemyHitReactionType.Knockdown;
            }

            if (knockbackForce >= knockbackThreshold)
            {
                return EnemyHitReactionType.Knockback;
            }

            return EnemyHitReactionType.Flinch;
        }

        private void TriggerAnimation(EnemyHitReactionType reactionType)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            switch (reactionType)
            {
                case EnemyHitReactionType.Knockdown:
                    if (!string.IsNullOrEmpty(knockdownTrigger))
                    {
                        animator.SetTrigger(knockdownTrigger);
                    }
                    break;
                default:
                    if (!string.IsNullOrEmpty(flinchTrigger))
                    {
                        animator.SetTrigger(flinchTrigger);
                    }
                    break;
            }
        }

        private Vector3 GetKnockbackDirection(Vector3 damageSource)
        {
            Vector3 direction = (transform.position - damageSource).normalized;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
            {
                direction = -transform.forward;
                direction.y = 0f;
            }

            return direction.normalized;
        }

        private IEnumerator MoveKnockback(Vector3 direction, float distance, float duration)
        {
            if (distance <= 0f || duration <= 0f)
            {
                yield break;
            }

            Vector3 startPosition = transform.position;
            Vector3 targetPosition = startPosition + direction * distance;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.position = targetPosition;
        }

        private void SuspendAgentControl()
        {
            if (ai != null)
            {
                ai.SetSuppressed(true);
            }

            if (agent != null)
            {
                agent.isStopped = true;
                agent.updatePosition = false;
                agent.updateRotation = false;
            }
        }

        private void ResumeAgentControl()
        {
            if (agent != null)
            {
                agent.Warp(transform.position);
                agent.updatePosition = true;
                agent.updateRotation = true;
                agent.isStopped = false;
            }

            if (ai != null)
            {
                ai.SetSuppressed(false);
            }
        }
    }
}
