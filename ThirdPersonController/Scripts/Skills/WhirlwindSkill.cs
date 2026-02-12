using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ThirdPersonController
{
    /// <summary>
    /// 技能1: 深海旋风 - 360度旋转攻击
    /// 按键: Q
    /// </summary>
    [CreateAssetMenu(fileName = "SKILL_Whirlwind", menuName = "Skills/Whirlwind")]
    public class WhirlwindSkill : SkillBase
    {
        [Header("旋风设置")]
        public float duration = 2f;
        public float tickRate = 0.3f;
        public float radius = 3f;
        public float rotationSpeed = 720f; // 每秒旋转角度
        public int tickDamage = 15;
        
        [Header("击退")]
        public float knockbackForce = 8f;

        private readonly List<Collider> hitTargets = new List<Collider>();
        [System.NonSerialized] private Coroutine whirlwindRoutine;
        [System.NonSerialized] private MonoBehaviour activeRunner;

        private void OnEnable()
        {
            if (category == SkillCategory.None)
            {
                category = SkillCategory.Burst;
            }

            endsOnRecovery = false;

            if (damageReduction <= 0f)
            {
                damageReduction = 0.25f;
            }

            if (damageReductionTiming == SkillDefenseTiming.None)
            {
                damageReductionTiming = SkillDefenseTiming.OnCast;
            }

            if (damageReductionDuration <= 0f)
            {
                damageReductionDuration = duration;
            }

            if (useAnimationEvents)
            {
                impactDelay = 0.12f;
                recoveryDelay = 0.2f;
                impactShakeDuration = 0.1f;
                impactShakeStrength = 0.15f;
            }
        }
        
        public override void Execute(Transform caster, Vector3 targetPosition)
        {
            // 触发动画
            Animator animator = caster.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetTrigger("Whirlwind");
            }

            StartSkillTimeline(caster, caster.position, caster.rotation, () =>
            {
                // 开始旋风Coroutine
                activeRunner = caster.GetComponent<MonoBehaviour>();
                if (activeRunner != null)
                {
                    if (whirlwindRoutine != null)
                    {
                        activeRunner.StopCoroutine(whirlwindRoutine);
                    }
                    whirlwindRoutine = activeRunner.StartCoroutine(WhirlwindCoroutine(caster));
                }
            });
        }

        public override float GetActionDuration()
        {
            return Mathf.Max(base.GetActionDuration(), duration);
        }

        public override void OnInterrupted(Transform caster)
        {
            if (activeRunner != null && whirlwindRoutine != null)
            {
                activeRunner.StopCoroutine(whirlwindRoutine);
            }

            whirlwindRoutine = null;
            activeRunner = null;
        }
        
        private IEnumerator WhirlwindCoroutine(Transform caster)
        {
            float elapsed = 0f;
            float tickTimer = 0f;
            
            while (elapsed < duration)
            {
                // 旋转
                caster.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
                
                // 定时伤害
                tickTimer += Time.deltaTime;
                if (tickTimer >= tickRate)
                {
                    tickTimer = 0f;
                    DealDamage(caster);
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // 恢复移动
            whirlwindRoutine = null;
            activeRunner = null;
            NotifySkillEnded(caster);
        }
        
        private void DealDamage(Transform caster)
        {
            // 查找范围内敌人
            float adjustedRadius = GetModifiedRange(caster, radius);
            int adjustedDamage = GetModifiedDamage(caster, tickDamage);
            float adjustedKnockback = GetModifiedKnockback(caster, knockbackForce);

            HitQuery.OverlapSphere(caster.position, adjustedRadius, LayerMask.GetMask("Enemy"), hitTargets);

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
                    hitCount++;
                }
            }
            
            // 播放命中音效
            if (hitCount > 0 && hitSound != null)
            {
                PlaySound(hitSound, caster.position);
            }
        }
    }
}
