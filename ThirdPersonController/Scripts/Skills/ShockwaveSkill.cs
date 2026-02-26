using UnityEngine;
using System.Collections.Generic;

namespace ThirdPersonController
{
    /// <summary>
    /// 技能2: 震荡波 - 前方扇形冲击波
    /// 按键: W
    /// </summary>
    [CreateAssetMenu(fileName = "SKILL_Shockwave", menuName = "Skills/Shockwave")]
    public class ShockwaveSkill : SkillBase
    {
        [Header("冲击波设置")]
        public float coneAngle = 90f;       // 扇形角度
        public float coneRange = 8f;        // 扇形距离
        public float stunDuration = 2f;     // 眩晕时间
        public float knockbackForce = 12f;  // 击退力度

        private readonly List<Collider> hitTargets = new List<Collider>();

        private void OnEnable()
        {
            if (category == SkillCategory.None)
            {
                category = SkillCategory.CrowdControl;
            }

            if (useAnimationEvents)
            {
                impactDelay = 0.22f;
                recoveryDelay = 0.28f;
                impactShakeDuration = 0.12f;
                impactShakeStrength = 0.18f;
            }
        }
        
        public override void Execute(Transform caster, Vector3 targetPosition)
        {
            // 触发动画
            Animator animator = caster.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetTrigger("Shockwave");
            }

            Vector3 impactPosition = caster.position + caster.forward * 2f;
            StartSkillTimeline(caster, impactPosition, caster.rotation, () =>
            {
                // 检测扇形范围内敌人
                DetectAndDamage(caster);
            });
        }
        
        private void DetectAndDamage(Transform caster)
        {
            // 使用OverlapSphere获取所有敌人
            float adjustedRange = GetModifiedRange(caster, coneRange);
            int adjustedDamage = GetModifiedDamage(caster, damage);
            float adjustedKnockback = GetModifiedKnockback(caster, knockbackForce);

            HitQuery.OverlapCone(caster.position, caster.forward, adjustedRange, coneAngle, 0f,
                LayerMask.GetMask("Enemy"), hitTargets, LayerMask.GetMask("Default"));

            int hitCount = 0;

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

                if (DamageService.ApplyDamage(context, hitCollider))
                {
                    // 眩晕效果（如果敌人有AI）
                    EnemyAI ai = hitCollider.GetComponent<EnemyAI>();
                    if (ai != null)
                    {
                        ai.ApplyStun(stunDuration);
                    }

                    hitCount++;
                }
            }
            
            if (hitCount > 0)
            {
                Debug.Log($"💥 震荡波命中 {hitCount} 个敌人！");
            }
        }
    }
}
