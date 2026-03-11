using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;

namespace ThirdPersonController
{
    /// <summary>
    /// Mobility skill: dashes forward and applies damage along the dash path.
    /// </summary>
    [CreateAssetMenu(fileName = "SKILL_DashAttack", menuName = "Skills/DashAttack")]
    public class DashAttackSkill : SkillBase
    {
        [Header("Dash")]
        public float dashDistance = 8f;
        public float dashSpeed = 20f;
        public float hitBoxWidth = 2f;
        public int pathDamage = 30;
        public float pathKnockback = 5f;

        [Header("Invincibility")]
        public bool invincibleDuringDash = true;
        [FormerlySerializedAs("invincibilityDuration")]
        public float dashInvincibilityDuration = 0.5f;

        private readonly List<Collider> hitTargets = new List<Collider>();
        private readonly HashSet<int> dashHitTargetIds = new HashSet<int>();
        [System.NonSerialized] private Coroutine dashRoutine;
        [System.NonSerialized] private MonoBehaviour activeRunner;
        [System.NonSerialized] private PlayerMovement cachedMovement;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (category == SkillCategory.None)
            {
                category = SkillCategory.Mobility;
            }

            endsOnRecovery = false;

            if (useAnimationEvents)
            {
                impactDelay = 0.08f;
                recoveryDelay = 0.22f;
                impactShakeDuration = 0.1f;
                impactShakeStrength = 0.2f;
            }
        }

        public override void Execute(Transform caster, Vector3 targetPosition)
        {
            StartSkillTimeline(caster, caster.position, caster.rotation, () =>
            {
                activeRunner = caster.GetComponent<MonoBehaviour>();
                if (activeRunner != null)
                {
                    if (dashRoutine != null)
                    {
                        activeRunner.StopCoroutine(dashRoutine);
                    }

                    dashRoutine = activeRunner.StartCoroutine(DashCoroutine(caster));
                }
            });
        }

        public override float GetActionDuration()
        {
            float dashDuration = dashSpeed > 0f ? dashDistance / dashSpeed : 0f;
            return Mathf.Max(base.GetActionDuration(), dashDuration);
        }

        public override void OnInterrupted(Transform caster)
        {
            if (activeRunner != null && dashRoutine != null)
            {
                activeRunner.StopCoroutine(dashRoutine);
            }

            if (cachedMovement != null)
            {
                cachedMovement.enabled = true;
            }

            dashRoutine = null;
            activeRunner = null;
            cachedMovement = null;
            dashHitTargetIds.Clear();
        }

        private System.Collections.IEnumerator DashCoroutine(Transform caster)
        {
            Animator animator = caster.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetTrigger("Dash");
            }

            PlayerHealth health = caster.GetComponent<PlayerHealth>();
            if (health != null && invincibleDuringDash)
            {
                health.ApplyInvincibility(dashInvincibilityDuration);
            }

            cachedMovement = caster.GetComponent<PlayerMovement>();
            if (cachedMovement != null)
            {
                cachedMovement.enabled = false;
            }

            Vector3 startPos = caster.position;
            Vector3 dashDirection = caster.forward;
            Vector3 endPos = startPos + dashDirection * dashDistance;

            if (Physics.Raycast(startPos + Vector3.up, dashDirection, out RaycastHit hit, dashDistance, LayerMask.GetMask("Default")))
            {
                endPos = hit.point - dashDirection * 0.5f;
            }

            float traveled = 0f;
            Vector3 lastPos = startPos;
            dashHitTargetIds.Clear();

            while (traveled < dashDistance)
            {
                float moveDistance = dashSpeed * Time.deltaTime;
                caster.position += dashDirection * moveDistance;
                traveled += moveDistance;

                DetectEnemiesInPath(lastPos, caster.position, hitBoxWidth, caster);
                lastPos = caster.position;
                yield return null;
            }

            caster.position = endPos;

            if (cachedMovement != null)
            {
                cachedMovement.enabled = true;
            }

            SpawnEffect(endPos, caster.rotation);

            dashRoutine = null;
            activeRunner = null;
            cachedMovement = null;
            dashHitTargetIds.Clear();
            NotifySkillEnded(caster);
        }

        private void DetectEnemiesInPath(Vector3 from, Vector3 to, float width, Transform caster)
        {
            int adjustedDamage = GetModifiedDamage(caster, pathDamage);
            float adjustedKnockback = GetModifiedKnockback(caster, pathKnockback);
            float adjustedWidth = GetModifiedRange(caster, width);

            HitQuery.BoxCastPath(from, to, Vector3.one * adjustedWidth * 0.5f, LayerMask.GetMask("Enemy"), hitTargets);

            for (int i = 0; i < hitTargets.Count; i++)
            {
                Collider hitCollider = hitTargets[i];
                if (hitCollider == null)
                {
                    continue;
                }

                if (HasHitTargetThisDash(hitCollider))
                {
                    continue;
                }

                DamageContext context = new DamageContext
                {
                    source = caster,
                    sourceType = DamageSourceType.PlayerSkill,
                    damage = adjustedDamage,
                    elementType = ResolveSkillElement(caster),
                    category = damageCategory,
                    knockback = adjustedKnockback,
                    breakValue = GetModifiedBreakValue(caster, adjustedKnockback),
                    damageOrigin = caster.position,
                    hitPoint = hitCollider.bounds.center,
                    hasHitPoint = true,
                    isCritical = false,
                    showDamageText = true,
                    hitStopDuration = 0f,
                    isHeavyAttack = false
                };

                if (DamageService.ApplyDamage(context, hitCollider))
                {
                    RegisterDashHitTarget(hitCollider);
                }
            }
        }

        private bool HasHitTargetThisDash(Collider hitCollider)
        {
            int targetId = ResolveDashHitTargetId(hitCollider);
            return targetId != 0 && dashHitTargetIds.Contains(targetId);
        }

        private void RegisterDashHitTarget(Collider hitCollider)
        {
            int targetId = ResolveDashHitTargetId(hitCollider);
            if (targetId != 0)
            {
                dashHitTargetIds.Add(targetId);
            }
        }

        private int ResolveDashHitTargetId(Collider hitCollider)
        {
            if (hitCollider == null)
            {
                return 0;
            }

            EnemyHealth enemyHealth = hitCollider.GetComponent<EnemyHealth>();
            if (enemyHealth == null)
            {
                enemyHealth = hitCollider.GetComponentInParent<EnemyHealth>();
            }

            if (enemyHealth != null)
            {
                return enemyHealth.GetInstanceID();
            }

            return hitCollider.transform.root != null
                ? hitCollider.transform.root.GetInstanceID()
                : hitCollider.GetInstanceID();
        }
    }
}
