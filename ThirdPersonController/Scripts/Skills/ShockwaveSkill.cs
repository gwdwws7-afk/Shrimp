using UnityEngine;
using System.Collections.Generic;

namespace ThirdPersonController
{
    /// <summary>
    /// ShockwaveSkill 模块的核心实现，负责统一管理关键运行流程与对外接口。
    /// 对前方扇形区域敌人造成伤害、击退并附带短暂眩晕。
    /// </summary>
    [CreateAssetMenu(fileName = "SKILL_Shockwave", menuName = "Skills/Shockwave")]
    public class ShockwaveSkill : SkillBase
    {
        [Header("设置")]
        public float coneAngle = 90f; // 运行时配置项，用于驱动模块行为并保持可调性。
        public float coneRange = 8f; // 扇形范围半径，用于约束判定覆盖面并避免越界命中。
        public float stunDuration = 2f; // 眩晕时长，用于定义效果生效窗口。
        public float knockbackForce = 12f; // 运行时配置项，用于驱动模块行为并保持可调性。

        private readonly List<Collider> hitTargets = new List<Collider>();

        protected override void OnEnable()
        {
            base.OnEnable();
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
            // 播放震荡波施法动画
            Animator animator = caster.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetTrigger("Shockwave");
            }

            Vector3 impactPosition = caster.position + caster.forward * 2f;
            StartSkillTimeline(caster, impactPosition, caster.rotation, () =>
            {
                // 进入命中检测与伤害结算
                DetectAndDamage(caster);
            });
        }
        
        private void DetectAndDamage(Transform caster)
        {
            // 计算当前释放的修正参数
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
                    // 命中后附加 AI 眩晕控制
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
                Debug.Log($"[Skill] Shockwave hit {hitCount} enemies.");
            }
        }
    }
}
