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

        [Header("Profile")]
        public EnemyHitReactionProfile profile;

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
        private EnemyHitReactionType lastReactionType = EnemyHitReactionType.Flinch;

        public EnemyHitReactionType LastReactionType => lastReactionType;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            agent = GetComponent<NavMeshAgent>();
            ai = GetComponent<EnemyAI>();

            if (profile == null)
            {
                profile = EnemyHitReactionProfile.GetDefaultProfile();
            }
        }

        public EnemyHitReactionType ApplyHit(Vector3 damageSource, float knockbackForce)
        {
            EnemyHitReactionType reactionType = GetReactionType(knockbackForce);
            lastReactionType = reactionType;

            if (reactionRoutine != null)
            {
                StopCoroutine(reactionRoutine);
            }

            reactionRoutine = StartCoroutine(ReactionRoutine(reactionType, damageSource));
            return reactionType;
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

            float flinchTime = flinchDuration;
            float knockbackTime = knockbackDuration;
            float knockbackDist = knockbackDistance;
            float knockdownTime = knockdownDuration;
            float knockdownDist = knockdownDistance;
            float knockdownRecover = knockdownRecoverTime;
            float knockdownLift = 0f;
            if (profile != null)
            {
                flinchTime = profile.flinchDuration;
                knockbackTime = profile.knockbackDuration;
                knockbackDist = profile.knockbackDistance;
                knockdownTime = profile.knockdownDuration;
                knockdownDist = profile.knockdownDistance;
                knockdownRecover = profile.knockdownRecoverTime;
                knockdownLift = profile.knockdownLift;
            }

            switch (reactionType)
            {
                case EnemyHitReactionType.Flinch:
                    yield return new WaitForSeconds(flinchTime);
                    break;
                case EnemyHitReactionType.Knockback:
                    yield return MoveKnockback(knockbackDirection, knockbackDist, knockbackTime);
                    break;
                case EnemyHitReactionType.Knockdown:
                    yield return MoveKnockback(knockbackDirection, knockdownDist, knockdownTime);
                    if (knockdownLift > 0f)
                    {
                        yield return MoveLift(knockdownLift, knockdownTime);
                    }
                    yield return new WaitForSeconds(knockdownRecover);
                    break;
            }

            ResumeAgentControl();
            reactionRoutine = null;
        }

        private EnemyHitReactionType GetReactionType(float knockbackForce)
        {
            float knockback = knockbackThreshold;
            float knockdown = knockdownThreshold;
            if (profile != null)
            {
                knockback = profile.knockbackThreshold;
                knockdown = profile.knockdownThreshold;
            }

            if (knockbackForce >= knockdown)
            {
                return EnemyHitReactionType.Knockdown;
            }

            if (knockbackForce >= knockback)
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

        private IEnumerator MoveLift(float height, float duration)
        {
            if (height <= 0f || duration <= 0f)
            {
                yield break;
            }

            Vector3 start = transform.position;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float lift = Mathf.Sin(t * Mathf.PI) * height;
                transform.position = new Vector3(start.x, start.y + lift, start.z);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.position = start;
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
