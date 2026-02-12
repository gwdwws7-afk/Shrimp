using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;

namespace ThirdPersonController
{
    /// <summary>
    /// 技能3: 深渊突袭 - 瞬间移动并攻击路径敌人
    /// 按键: E
    /// </summary>
    [CreateAssetMenu(fileName = "SKILL_DashAttack", menuName = "Skills/DashAttack")]
    public class DashAttackSkill : SkillBase
    {
        [Header("突袭设置")]
        public float dashDistance = 8f;
        public float dashSpeed = 20f;
        public float hitBoxWidth = 2f;
        public int pathDamage = 30;
        public float pathKnockback = 5f;
        
        [Header("无敌")]
        public bool invincibleDuringDash = true;
        [FormerlySerializedAs("invincibilityDuration")]
        public float dashInvincibilityDuration = 0.5f;

        private readonly List<Collider> hitTargets = new List<Collider>();
        [System.NonSerialized] private Coroutine dashRoutine;
        [System.NonSerialized] private MonoBehaviour activeRunner;
        [System.NonSerialized] private PlayerMovement cachedMovement;

        private void OnEnable()
        {
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
        }
        
        private System.Collections.IEnumerator DashCoroutine(Transform caster)
        {
            // 触发动画
            Animator animator = caster.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetTrigger("Dash");
            }
            
            // 无敌状态
            PlayerHealth health = caster.GetComponent<PlayerHealth>();
            if (health != null && invincibleDuringDash)
            {
                health.ApplyInvincibility(dashInvincibilityDuration);
            }
            
            // 禁用玩家控制
            cachedMovement = caster.GetComponent<PlayerMovement>();
            if (cachedMovement != null) cachedMovement.enabled = false;
            
            // 计算起点和终点
            Vector3 startPos = caster.position;
            Vector3 dashDirection = caster.forward;
            Vector3 endPos = startPos + dashDirection * dashDistance;
            
            // 检查终点是否有效（不穿透墙壁）
            if (Physics.Raycast(startPos + Vector3.up, dashDirection, out RaycastHit hit, dashDistance, LayerMask.GetMask("Default")))
            {
                endPos = hit.point - dashDirection * 0.5f;
            }
            
            // 突袭过程中检测敌人
            float traveled = 0f;
            Vector3 lastPos = startPos;
            
            while (traveled < dashDistance)
            {
                float moveDistance = dashSpeed * Time.deltaTime;
                caster.position += dashDirection * moveDistance;
                traveled += moveDistance;
                
                // 检测路径上的敌人
                DetectEnemiesInPath(lastPos, caster.position, hitBoxWidth, caster);
                
                lastPos = caster.position;
                yield return null;
            }
            
            // 确保到达终点
            caster.position = endPos;
            
            // 恢复控制
            if (cachedMovement != null) cachedMovement.enabled = true;

            // 播放结束特效
            SpawnEffect(endPos, caster.rotation);

            dashRoutine = null;
            activeRunner = null;
            cachedMovement = null;
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

                DamageContext context = new DamageContext
                {
                    source = caster,
                    sourceType = DamageSourceType.PlayerSkill,
                    damage = adjustedDamage,
                    knockback = adjustedKnockback,
                    damageOrigin = caster.position,
                    hitPoint = hitCollider.bounds.center,
                    hasHitPoint = true,
                    isCritical = false,
                    showDamageText = true,
                    hitStopDuration = 0f
                };

                DamageService.ApplyDamage(context, hitCollider);
            }
        }
    }
}
